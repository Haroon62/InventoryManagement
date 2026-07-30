using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Data;

/// <summary>
/// ApplicationDbContext is the bridge between your C# code and SQL Server.
/// 
/// It inherits from DbContext (provided by EF Core), which gives it the ability to:
///   - Open/close database connections
///   - Track changes to your objects
///   - Translate LINQ queries into SQL
///   - Save changes to the database
/// 
/// You never create this class with "new" — ASP.NET Core's Dependency Injection
/// creates it for you and passes it to your Services automatically.
/// </summary>
public class ApplicationDbContext : DbContext
{
    // ── Constructor ──────────────────────────────────────────────────
    // This constructor receives DbContextOptions from Dependency Injection.
    // The options contain the connection string and database provider (SQL Server).
    // We pass them up to the base DbContext class using ": base(options)".
    //
    // You'll never call this constructor yourself — ASP.NET Core does it
    // automatically when a Service asks for ApplicationDbContext.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ── DbSets (Tables) ─────────────────────────────────────────────
    // Each DbSet<T> maps to a TABLE in the database.
    // The property name becomes the table name: "Products", "StockMovements".
    //
    // You use these to query and save data:
    //   _context.Products.Where(p => p.IsActive).ToList();
    //   → SELECT * FROM Products WHERE IsActive = 1

    /// <summary>
    /// Represents the Products table in the database.
    /// </summary>
    public DbSet<Product> Products { get; set; }

    /// <summary>
    /// Represents the StockMovements table in the database.
    /// </summary>
    public DbSet<StockMovement> StockMovements { get; set; }

    // ── OnModelCreating (Fluent API Configuration) ───────────────────
    // This method is called ONCE when EF Core first builds its internal
    // model of your database. Override it to configure things that
    // DataAnnotations cannot handle:
    //   - Unique indexes
    //   - Relationships between tables
    //   - Composite keys
    //   - Default values at the database level
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Always call the base method first — it may have its own configuration.
        base.OnModelCreating(modelBuilder);

        // ── Product Configuration ────────────────────────────────────

        // Make SKU unique at the DATABASE level.
        // DataAnnotations have no [Unique] attribute — this can ONLY be done
        // via Fluent API. This creates a unique index in SQL Server:
        //   CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku])
        // If someone tries to insert a duplicate SKU, SQL Server will reject it.
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        // ── StockMovement → Product Relationship ─────────────────────
        //
        // This configures the One-to-Many relationship explicitly:
        //   ONE Product has MANY StockMovements.
        //   Each StockMovement has ONE Product.
        //   The foreign key is ProductId.
        //
        // Read this chain like an English sentence:
        //   "A Product HAS MANY StockMovements.
        //    Each StockMovement HAS ONE Product.
        //    The FOREIGN KEY is ProductId.
        //    When a Product is DELETED, RESTRICT (block) the delete
        //    if it still has StockMovements."
        //
        // Why Restrict?
        //   DeleteBehavior.Restrict means: "Don't allow deleting a Product
        //   if StockMovements reference it." This prevents accidentally
        //   losing historical stock data. The user must delete or reassign
        //   the movements first (or use soft-delete via IsActive = false).
        modelBuilder.Entity<Product>()
            .HasMany(p => p.StockMovements)
            .WithOne(sm => sm.Product)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
