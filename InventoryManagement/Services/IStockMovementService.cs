using InventoryManagement.Models;

namespace InventoryManagement.Services;

public interface IStockMovementService
{

    Task<int> GetCurrentStockAsync(int productId);
    Task<List<StockMovement>> GetMovementHistoryAsync(int productId);
    Task<(bool CanRemove, int CurrentStock)> CanRemoveStockAsync(int productId, int quantity);
    Task<(bool Success, string? ErrorMessage)> AddMovementAsync(StockMovement movement);
}
