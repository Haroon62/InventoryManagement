using System.ComponentModel.DataAnnotations;
using InventoryManagement.Enums;
using InventoryManagement.Models;

namespace InventoryManagement.ViewModels;

/// <summary>
/// Used for the form where users record a stock movement (In or Out).
/// </summary>
public class MovementFormViewModel
{
    public int ProductId { get; set; }

    public MovementType MovementType { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public StockMovement ToStockMovementModel()
    {
        return new StockMovement
        {
            ProductId = this.ProductId,
            MovementType = this.MovementType,
            Quantity = this.Quantity,
            Note = this.Note
        };
    }
}
