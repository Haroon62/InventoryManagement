using InventoryManagement.Data;
using InventoryManagement.Enums;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

/// <summary>
/// KEY BUSINESS RULES:
/// 1. Current Stock = sum(In) - sum(Out) — always computed, never stored.
/// 2. An "Out" movement must NEVER reduce stock below zero.
/// 3. Concurrent "Out" requests must not both succeed when only enough
///    stock exists for one (race condition prevention).
/// </summary>
public class StockMovementService : IStockMovementService
{
    private readonly ApplicationDbContext _context;

    // Constructor — DI injects the DbContext automatically.
    public StockMovementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetCurrentStockAsync(int productId)
    {
       
        int totalIn = await _context.StockMovements
            .Where(sm => sm.ProductId == productId)
            .Where(sm => sm.MovementType == MovementType.In)
            .SumAsync(sm => sm.Quantity);

        int totalOut = await _context.StockMovements
            .Where(sm => sm.ProductId == productId)
            .Where(sm => sm.MovementType == MovementType.Out)
            .SumAsync(sm => sm.Quantity);

        return totalIn - totalOut;
    }

    public async Task<List<StockMovement>> GetMovementHistoryAsync(int productId)
    {
        return await _context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.ProductId == productId)
            .OrderByDescending(sm => sm.CreatedUtc)
            .Include(sm => sm.Product)
            .ToListAsync();
    }

    
    public async Task<(bool CanRemove, int CurrentStock)> CanRemoveStockAsync(
        int productId, int quantity)
    {
        if (quantity <= 0)
        {
            return (false, 0);
        }

        int currentStock = await GetCurrentStockAsync(productId);

        bool canRemove = currentStock >= quantity;

        return (canRemove, currentStock);
    }

    // Records a new stock movement (In or Out).
    public async Task<(bool Success, string? ErrorMessage)> AddMovementAsync(
        StockMovement movement)
    {
        // ── Validation 1: Does the product exist? ──
        bool productExists = await _context.Products
            .AnyAsync(p => p.Id == movement.ProductId);

        if (!productExists)
        {
            return (false, "Product not found.");
        }

        // ── Validation 2: Set server-side timestamp ──
        movement.CreatedUtc = DateTime.UtcNow;

        if (movement.MovementType == MovementType.In)
        {
            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
            return (true, null);
        }

      
        using var transaction = await _context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {

            var (canRemove, currentStock) = await CanRemoveStockAsync(
                movement.ProductId, movement.Quantity);

            if (!canRemove)
            {
                await transaction.RollbackAsync();

                return (false,
                    $"Cannot remove {movement.Quantity} units. " +
                    $"Only {currentStock} currently in stock.");
            }
            _context.StockMovements.Add(movement);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; 
        }
    }
}
