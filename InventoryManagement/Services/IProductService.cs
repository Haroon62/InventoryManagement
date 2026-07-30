using InventoryManagement.Models;

namespace InventoryManagement.Services;

public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage)> CreateProductAsync(Product product);
    Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(Product product);
    Task<bool> DeactivateProductAsync(int id);
    Task<PagedResult<Product>> SearchProductsAsync(string searchTerm, int page, int pageSize);
}
