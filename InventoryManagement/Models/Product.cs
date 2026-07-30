using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models;

public class Product
{
    // Primary Key — EF Core auto-detects "Id" as the PK by convention.
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // We initialize it as an empty List to avoid null reference exceptions
    // when accessing it before EF Core loads the data.
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
