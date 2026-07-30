using System.ComponentModel.DataAnnotations;
using InventoryManagement.Enums;

namespace InventoryManagement.Models;

public class StockMovement
{
    // Primary Key — auto-incremented by SQL Server, just like Product.Id.
    public int Id { get; set; }

    // Foreign Key — stores the Id of the related Product.
    // EF Core convention: a property named "{ClassName}Id" is automatically
    // recognized as a FK to that class. No extra configuration needed.
    // [Required] is technically redundant for int (value types can't be null),
    // but it makes the intent crystal clear to other developers reading your code.
    public int ProductId { get; set; }

    public MovementType MovementType { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }

    // When this movement was recorded, in UTC.
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // ── Navigation Property ──────────────────────────────────────────
    // This is NOT a database column. EF Core uses this to let you write
    // things like: movement.Product.Name
    // 
    // Behind the scenes, EF Core translates that into a SQL JOIN:
    //   SELECT ... FROM StockMovements 
    //   INNER JOIN Products ON StockMovements.ProductId = Products.Id
    //
    // The 'null!' tells the compiler: "I know this looks null, but EF Core
    // will populate it when loading from the database." This avoids the
    // nullable warning without making the property nullable.
    public Product Product { get; set; } = null!;
}
