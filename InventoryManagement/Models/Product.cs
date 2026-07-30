using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models;

public class Product
{
    // Primary Key — EF Core auto-detects "Id" as the PK by convention.
    // SQL Server will make this an IDENTITY column (auto-increment: 1, 2, 3...).
    public int Id { get; set; }

    // Stock Keeping Unit — a unique human-readable code like "WIDGET-001".
    // [Required] = cannot be null/empty (both in form validation AND database: NOT NULL).
    // [MaxLength(50)] = limits to 50 characters → becomes NVARCHAR(50) in SQL Server.
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ReorderLevel { get; set; }

    // Soft delete flag — false means "hidden" but not deleted from the database.
    // This preserves historical stock movement data.
    public bool IsActive { get; set; } = true;

    // When this product record was created, stored in UTC to avoid timezone issues.
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // ── Navigation Collection (the "many" side) ─────────────────────
    // One Product can have MANY StockMovements.
    // This is NOT a database column — it's a C# collection that EF Core
    // populates when you use .Include(p => p.StockMovements) in a query.
    //
    // We initialize it as an empty List to avoid null reference exceptions
    // when accessing it before EF Core loads the data.
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
