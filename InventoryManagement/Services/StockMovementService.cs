using InventoryManagement.Data;
using InventoryManagement.Enums;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

/// <summary>
/// Contains all business logic for stock movements.
/// 
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

    /// <summary>
    /// Calculates the current stock level for a product.
    /// 
    /// ── The Formula ──
    /// Current Stock = Total quantity of "In" movements
    ///               - Total quantity of "Out" movements
    /// 
    /// ── Why two separate queries instead of one? ──
    /// It's simpler to read and understand. EF Core is smart enough to
    /// optimize these into efficient SQL. For a more complex app you could
    /// use a single query with conditional sums, but clarity wins here.
    /// 
    /// ── The SQL that EF Core generates ──
    /// For totalIn:
    ///   SELECT COALESCE(SUM([s].[Quantity]), 0)
    ///   FROM [StockMovements] AS [s]
    ///   WHERE [s].[ProductId] = @productId AND [s].[MovementType] = 0
    /// 
    /// For totalOut:
    ///   SELECT COALESCE(SUM([s].[Quantity]), 0)
    ///   FROM [StockMovements] AS [s]
    ///   WHERE [s].[ProductId] = @productId AND [s].[MovementType] = 1
    /// </summary>
    public async Task<int> GetCurrentStockAsync(int productId)
    {
        // ── LINQ Query 1: Sum all "In" quantities ──
        //
        // Step by step:
        // .Where(sm => sm.ProductId == productId)
        //     → Filter: only movements for THIS product
        //     → SQL: WHERE ProductId = @productId
        //
        // .Where(sm => sm.MovementType == MovementType.In)
        //     → Filter further: only "In" movements (enum value 0)
        //     → SQL: AND MovementType = 0
        //
        // .SumAsync(sm => sm.Quantity)
        //     → Add up all the Quantity values
        //     → SQL: SELECT SUM(Quantity)
        //     → Returns 0 if there are no matching rows
        int totalIn = await _context.StockMovements
            .Where(sm => sm.ProductId == productId)
            .Where(sm => sm.MovementType == MovementType.In)
            .SumAsync(sm => sm.Quantity);

        // ── LINQ Query 2: Sum all "Out" quantities ──
        //
        // Same pattern, but filtering for MovementType.Out (enum value 1).
        // If no "Out" movements exist yet, SumAsync returns 0.
        int totalOut = await _context.StockMovements
            .Where(sm => sm.ProductId == productId)
            .Where(sm => sm.MovementType == MovementType.Out)
            .SumAsync(sm => sm.Quantity);

        // The result: what came in minus what went out.
        // Example: 100 In - 30 Out = 70 units currently in stock.
        return totalIn - totalOut;
    }

    /// <summary>
    /// Gets the complete movement history for a product.
    /// 
    /// Used on the Product Detail screen to show a chronological log:
    ///   "Jan 1: +100 (Initial delivery)"
    ///   "Jan 15: -30 (Sold to Customer X)"
    ///   "Feb 1: -20 (Damaged)"
    /// 
    /// ── The LINQ query explained ──
    ///
    /// .Where(sm => sm.ProductId == productId)
    ///     → Only movements for this specific product.
    ///     → SQL: WHERE ProductId = @productId
    ///
    /// .OrderByDescending(sm => sm.CreatedUtc)
    ///     → Newest movements first (most recent at the top).
    ///     → SQL: ORDER BY CreatedUtc DESC
    ///
    /// .Include(sm => sm.Product)
    ///     → Eager Loading — tells EF Core to JOIN the Products table
    ///       so we can access sm.Product.Name in the view without
    ///       triggering a second database query.
    ///     → SQL: INNER JOIN Products p ON sm.ProductId = p.Id
    ///
    /// .ToListAsync()
    ///     → Execute the query and return results as a List.
    ///     → Without this, the query is just a definition — nothing runs
    ///       until you call ToListAsync (this is "deferred execution").
    /// </summary>
    public async Task<List<StockMovement>> GetMovementHistoryAsync(int productId)
    {
        return await _context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.ProductId == productId)
            .OrderByDescending(sm => sm.CreatedUtc)
            .Include(sm => sm.Product)
            .ToListAsync();
    }

    /// <summary>
    /// Checks whether the requested quantity can be removed from stock.
    /// 
    /// This method is SEPARATE from AddMovement because:
    /// 1. The Controller can call this to show a warning BEFORE submitting.
    /// 2. Unit tests can test the check logic independently.
    /// 3. It follows the Single Responsibility Principle — one method, one job.
    /// 
    /// Returns a tuple:
    ///   CanRemove    = true if currentStock >= quantity
    ///   CurrentStock = the actual stock level (shown in error messages)
    /// </summary>
    public async Task<(bool CanRemove, int CurrentStock)> CanRemoveStockAsync(
        int productId, int quantity)
    {
        // ── Validation 1: Quantity must be positive ──
        // This is a sanity check. You can't remove 0 or negative items.
        // DataAnnotations already enforce this on the form, but we double-check
        // here because services should NEVER trust incoming data — the request
        // might come from an API call that bypasses form validation.
        if (quantity <= 0)
        {
            return (false, 0);
        }

        int currentStock = await GetCurrentStockAsync(productId);

        // ── Validation 2: THE KEY RULE ──
        // Can we remove the requested quantity without going negative?
        // Example: currentStock = 35, quantity = 50 → 35 >= 50 is false → can't remove.
        bool canRemove = currentStock >= quantity;

        return (canRemove, currentStock);
    }

    /// <summary>
    /// Records a new stock movement (In or Out).
    /// 
    /// ── Validation chain ──
    /// 1. Verify the product exists
    /// 2. If "Out": check that stock won't go below zero (THE KEY RULE)
    /// 3. If "Out": use SERIALIZABLE transaction to prevent race conditions
    /// 4. Save the movement
    /// 
    /// ── What is a race condition? ──
    /// Two users both see "50 in stock" and both try to take 40:
    ///   Without protection: both succeed → stock becomes -30 (BROKEN!)
    ///   With SERIALIZABLE: second user waits → sees only 10 left → rejected ✅
    /// 
    /// ── What is a SERIALIZABLE transaction? ──
    /// The strictest isolation level in SQL Server. It LOCKS the rows we read
    /// so no other transaction can read or modify them until we commit/rollback.
    /// This guarantees that the stock we read is still accurate when we save.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> AddMovementAsync(
        StockMovement movement)
    {
        // ── Validation 1: Does the product exist? ──
        // We check this before doing anything else to give a clear error
        // instead of a cryptic foreign key violation from SQL Server.
        //
        // .AnyAsync() is faster than .FindAsync() here because we don't
        // need the full Product object — just a yes/no answer.
        // SQL: SELECT CASE WHEN EXISTS(SELECT 1 FROM Products WHERE Id = @id)
        //      THEN 1 ELSE 0 END
        bool productExists = await _context.Products
            .AnyAsync(p => p.Id == movement.ProductId);

        if (!productExists)
        {
            return (false, "Product not found.");
        }

        // ── Validation 2: Set server-side timestamp ──
        // We ALWAYS set this in the service, never trust the client.
        // This ensures the timestamp is accurate and consistent (UTC).
        movement.CreatedUtc = DateTime.UtcNow;

        // ── "In" movements: always allowed, no special checks ──
        // You can always receive more stock — there's no business rule
        // that limits how much stock you can have.
        if (movement.MovementType == MovementType.In)
        {
            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        // ── "Out" movements: THE KEY RULE with concurrency protection ──
        //
        // We use a SERIALIZABLE transaction to prevent this scenario:
        //
        //   TIME    USER A                    USER B
        //   ────    ──────                    ──────
        //   T1      Read stock: 50
        //   T2                                Read stock: 50  ← STALE!
        //   T3      Take 40 → save → OK
        //   T4                                Take 40 → save → OK ← BUG!
        //   Result: Stock = 50 - 40 - 40 = -30 💥
        //
        // With SERIALIZABLE:
        //   T1      LOCK + Read stock: 50
        //   T2                                WAIT... (locked)
        //   T3      Take 40 → save → commit
        //   T4                                LOCK + Read stock: 10
        //   T5                                Take 40 → 10 < 40 → REJECTED ✅
        //
        // "using var" ensures the transaction is disposed (cleaned up)
        // even if an exception occurs, thanks to the IDisposable pattern.
        using var transaction = await _context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            // ── Check stock INSIDE the transaction ──
            // The SERIALIZABLE lock guarantees this value won't change
            // between now and when we save.
            var (canRemove, currentStock) = await CanRemoveStockAsync(
                movement.ProductId, movement.Quantity);

            if (!canRemove)
            {
                // ── Validation 3: THE KEY RULE — reject with friendly message ──
                // The assignment says: "Reject it with a clear message that tells
                // the user how much is actually available."
                await transaction.RollbackAsync();

                return (false,
                    $"Cannot remove {movement.Quantity} units. " +
                    $"Only {currentStock} currently in stock.");
            }

            // ── Stock is sufficient — record the movement ──
            // .Add() marks it as "pending INSERT" in EF Core's change tracker.
            _context.StockMovements.Add(movement);

            // .SaveChangesAsync() sends the INSERT to the database:
            // SQL: INSERT INTO StockMovements 
            //      (ProductId, MovementType, Quantity, Note, CreatedUtc)
            //      VALUES (@p0, @p1, @p2, @p3, @p4)
            await _context.SaveChangesAsync();

            // .CommitAsync() makes the changes permanent and releases the lock.
            // If we didn't commit, the transaction would automatically rollback
            // when disposed (the "using" block ends).
            await transaction.CommitAsync();

            return (true, null);
        }
        catch
        {
            // If ANYTHING unexpected happens (network error, SQL timeout, etc.),
            // rollback the entire transaction — no partial data gets saved.
            await transaction.RollbackAsync();
            throw; // Re-throw so the error is logged by ASP.NET Core
        }
    }
}
