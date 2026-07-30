using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ── DbSets (Tables) ─────────────────────────────────────────────
    public DbSet<Product> Products { get; set; }

    public DbSet<StockMovement> StockMovements { get; set; }

    // ── OnModelCreating (Fluent API Configuration) ───────────────────
    // This method is called ONCE when EF Core first builds its internal
    // model of your database.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Always call the base method first — it may have its own configuration.
        base.OnModelCreating(modelBuilder);

        // Make SKU unique at the DATABASE level.
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        // ── StockMovement → Product Relationship ─────────────────────
        //
        // This configures the One-to-Many relationship explicitly:
        //   ONE Product has MANY StockMovements.
        //   Each StockMovement has ONE Product.
        //   The foreign key is ProductId.

        modelBuilder.Entity<Product>()
            .HasMany(p => p.StockMovements)
            .WithOne(sm => sm.Product)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
