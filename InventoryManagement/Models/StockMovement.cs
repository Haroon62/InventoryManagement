using System.ComponentModel.DataAnnotations;
using InventoryManagement.Enums;

namespace InventoryManagement.Models;

public class StockMovement
{
    // Primary Key — auto-incremented by SQL Server.
    public int Id { get; set; }

    // Foreign Key — stores the Id of the related Product.
    public int ProductId { get; set; }

    public MovementType MovementType { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // The 'null!' tells the compiler: "I know this looks null, but EF Core
    // will populate it when loading from the database." This avoids the
    // nullable warning without making the property nullable.
    public Product Product { get; set; } = null!;
}
