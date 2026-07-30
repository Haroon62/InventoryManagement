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
    // ── Dependency ──────────────────────────────────────────────────
    // We store the DbContext in a private readonly field.
    // "readonly" means it can only be set in the constructor — this
    // prevents accidentally overwriting it later.
    // The underscore prefix (_) is a C# convention for private fields.
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

    /// <summary>
    /// Gets all active products, ordered alphabetically by name.
    /// 
    /// Why filter by IsActive?
    /// Because we use soft-delete — deactivated products still exist
    /// in the database but should not appear in the product list.
    /// 
    /// Why async?
    /// Database calls go over the network (even to LocalDB). While waiting
    /// for SQL Server to respond, async frees up the thread to handle
    /// other HTTP requests. This improves scalability under load.
    /// </summary>
    public async Task<List<Product>> GetAllProductsAsync()
    {
        // .Where()      → SQL: WHERE IsActive = 1
        // .OrderBy()    → SQL: ORDER BY Name
        // .ToListAsync() → Executes the query and returns results
        //
        // EF Core translates this entire LINQ chain into a single SQL query:
        //   SELECT * FROM Products WHERE IsActive = 1 ORDER BY Name
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a single product by its primary key.
    /// 
    /// Returns null if no product with that ID exists.
    /// The Controller checks for null and returns a 404 page.
    /// 
    /// FindAsync is optimized — it first checks EF Core's local cache
    /// (change tracker) before hitting the database.
    /// </summary>
    public async Task<Product?> GetByIdAsync(int id)
    {
        // FindAsync looks up by primary key.
        // It's the fastest way to get a single record because:
        // 1. It checks the in-memory cache first
        // 2. It uses a parameterized query (safe from SQL injection)
        //
        // SQL: SELECT TOP 1 * FROM Products WHERE Id = @id
        return await _context.Products.FindAsync(id);
    }

    /// <summary>
    /// Creates a new product after checking that the SKU is unique.
    /// 
    /// Returns a tuple:
    ///   Success = true  → product was created
    ///   Success = false → SKU already exists, ErrorMessage explains why
    /// 
    /// Why a tuple instead of throwing an exception?
    /// A duplicate SKU is a BUSINESS rule violation, not an unexpected error.
    /// Exceptions should be for truly unexpected situations (database down,
    /// network error). A user entering a duplicate SKU is expected — we
    /// handle it gracefully with a friendly message.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> CreateProductAsync(Product product)
    {
        // Check if another product already uses this SKU.
        // AnyAsync returns true if at least one match exists.
        // SQL: SELECT CASE WHEN EXISTS(SELECT 1 FROM Products WHERE Sku = @sku) 
        //      THEN 1 ELSE 0 END
        bool skuExists = await _context.Products
            .AnyAsync(p => p.Sku == product.Sku);

        if (skuExists)
        {
            return (false, $"A product with SKU '{product.Sku}' already exists.");
        }

        // Set server-side defaults.
        // We set these here (not in the Controller) because this is business logic:
        // "Every new product should be active and timestamped."
        product.IsActive = true;
        product.CreatedUtc = DateTime.UtcNow;

        // Add the product to EF Core's change tracker.
        // This marks it as "needs to be INSERTed" but doesn't hit the DB yet.
        _context.Products.Add(product);

        // SaveChangesAsync sends the actual INSERT SQL to the database.
        // SQL: INSERT INTO Products (Sku, Name, Description, ...) VALUES (@p0, @p1, ...)
        await _context.SaveChangesAsync();

        return (true, null);
    }

    /// <summary>
    /// Updates an existing product.
    /// 
    /// Key difference from Create: when checking for duplicate SKU,
    /// we must EXCLUDE the current product. Otherwise, saving a product
    /// without changing its SKU would falsely trigger "SKU already exists"
    /// because it would find... itself.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(Product product)
    {
        // Check if ANOTHER product (different Id) already uses this SKU.
        // The "p.Id != product.Id" part excludes the product being edited.
        // Without this, editing a product without changing its SKU would fail.
        //
        // SQL: SELECT CASE WHEN EXISTS(
        //        SELECT 1 FROM Products WHERE Sku = @sku AND Id != @id
        //      ) THEN 1 ELSE 0 END
        bool skuExists = await _context.Products
            .AnyAsync(p => p.Sku == product.Sku && p.Id != product.Id);

        if (skuExists)
        {
            return (false, $"A product with SKU '{product.Sku}' already exists.");
        }

        // Tell EF Core: "This object has been modified — generate an UPDATE."
        // 
        // _context.Update() marks ALL properties as modified.
        // EF Core will generate: UPDATE Products SET Sku=@p0, Name=@p1, ... WHERE Id=@id
        _context.Products.Update(product);

        await _context.SaveChangesAsync();

        return (true, null);
    }

    /// <summary>
    /// Soft-deletes a product by setting IsActive = false.
    /// 
    /// Why soft-delete instead of hard-delete?
    /// If we DELETE the product from the database, all its StockMovements
    /// would either be deleted too (cascade) or become orphans (broken FK).
    /// Both are bad. Soft-delete preserves the history while hiding the
    /// product from the UI.
    /// 
    /// The assignment specifically says: "prefer a soft deactivate (IsActive)
    /// over a hard delete."
    /// </summary>
    public async Task<bool> DeactivateProductAsync(int id)
    {
        // Find the product first
        var product = await _context.Products.FindAsync(id);

        // If no product found with this ID, return false.
        // The Controller will check this and show a 404 page.
        if (product is null)
        {
            return false;
        }

        // Flip the flag — the product is now "deactivated"
        product.IsActive = false;

        // SaveChangesAsync detects that IsActive changed and generates:
        // SQL: UPDATE Products SET IsActive = 0 WHERE Id = @id
        //
        // Note: We don't need _context.Update() here because EF Core's
        // change tracker already knows about this product (we loaded it
        // with FindAsync above). It automatically detects property changes.
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Searches active products by SKU or Name.
    /// 
    /// Uses case-insensitive partial matching via Contains().
    /// EF Core translates Contains() to SQL LIKE:
    ///   WHERE Sku LIKE '%term%' OR Name LIKE '%term%'
    /// 
    /// Returns an empty list if the search term is empty/null.
    /// </summary>
    public async Task<PagedResult<Product>> SearchProductsAsync(string searchTerm, int page, int pageSize)
    {
        var query = _context.Products.Where(p => p.IsActive);

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
