using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

/// <summary>
/// Contains all business logic related to Products.
/// 
/// This class receives ApplicationDbContext through its constructor via
/// Dependency Injection. It never creates the DbContext with "new" — 
/// ASP.NET Core handles that automatically.
/// </summary>
public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    // ── Constructor ─────────────────────────────────────────────────
    // ASP.NET Core's DI sees this constructor and says:
    // "This class needs ApplicationDbContext — I registered that in 
    //  Program.cs, so I'll create one and pass it in."
    //
    // You never call: new ProductService(new ApplicationDbContext(...))
    // The framework does it for you.
    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {

        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }


    public async Task<(bool Success, string? ErrorMessage)> CreateProductAsync(Product product)
    {
        // Check if another product already uses this SKU.
        bool skuExists = await _context.Products
            .AnyAsync(p => p.Sku == product.Sku);

        if (skuExists)
        {
            return (false, $"A product with SKU '{product.Sku}' already exists.");
        }

        product.IsActive = true;
        product.CreatedUtc = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(Product product)
    {
        // Check if ANOTHER product (different Id) already uses this SKU.
        bool skuExists = await _context.Products
            .AnyAsync(p => p.Sku == product.Sku && p.Id != product.Id);

        if (skuExists)
        {
            return (false, $"A product with SKU '{product.Sku}' already exists.");
        }

        _context.Products.Update(product);

        await _context.SaveChangesAsync();

        return (true, null);
    }
    public async Task<bool> DeactivateProductAsync(int id)
    {
        // Find the product first
        var product = await _context.Products.FindAsync(id);

        if (product is null)
        {
            return false;
        }

        product.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PagedResult<Product>> SearchProductsAsync(string searchTerm, int page, int pageSize)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(p => p.Sku.Contains(searchTerm) || p.Name.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product> 
        { 
            Items = products, 
            TotalCount = totalCount 
        };
    }
}
