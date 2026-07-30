using InventoryManagement.Models;

namespace InventoryManagement.Services;

/// <summary>
/// Defines the contract for Product-related business operations.
/// 
/// The Controller only knows about this INTERFACE, not the concrete class.
/// This means we can swap out the implementation (e.g., for testing)
/// without changing the Controller code.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets all active products, ordered by name.
    /// </summary>
    Task<List<Product>> GetAllProductsAsync();

    /// <summary>
    /// Gets a single product by its ID. Returns null if not found.
    /// </summary>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new product. Returns a result indicating success or failure.
    /// Failure happens when the SKU already exists.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> CreateProductAsync(Product product);

    /// <summary>
    /// Updates an existing product. Returns a result indicating success or failure.
    /// Failure happens when the SKU conflicts with another product.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(Product product);

    /// <summary>
    /// Soft-deletes a product by setting IsActive = false.
    /// Returns false if the product was not found.
    /// </summary>
    Task<bool> DeactivateProductAsync(int id);

    /// <summary>
    /// Searches products by SKU or Name (case-insensitive partial match).
    /// Returns only active products.
    /// </summary>
    Task<List<Product>> SearchProductsAsync(string searchTerm);
}
