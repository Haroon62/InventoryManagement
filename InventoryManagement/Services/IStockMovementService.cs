using InventoryManagement.Models;

namespace InventoryManagement.Services;

/// <summary>
/// Defines the contract for stock movement operations.
/// 
/// The most critical business rule lives here:
/// Current Stock = Total IN - Total OUT, and it can NEVER go below zero.
/// </summary>
public interface IStockMovementService
{
    /// <summary>
    /// Calculates current stock for a product.
    /// Formula: sum(In) - sum(Out). Never stored, always computed.
    /// </summary>
    Task<int> GetCurrentStockAsync(int productId);

    /// <summary>
    /// Gets all movements for a product, newest first (chronological history).
    /// </summary>
    Task<List<StockMovement>> GetMovementHistoryAsync(int productId);

    /// <summary>
    /// Checks whether the requested quantity can be removed from stock.
    /// Returns (true, currentStock) if allowed, (false, currentStock) if not.
    /// </summary>
    Task<(bool CanRemove, int CurrentStock)> CanRemoveStockAsync(int productId, int quantity);

    /// <summary>
    /// Records a new stock movement (In or Out).
    /// For "Out" movements, enforces the rule: stock cannot go below zero.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> AddMovementAsync(StockMovement movement);
}
