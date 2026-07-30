using InventoryManagement.Models;

namespace InventoryManagement.ViewModels;

/// <summary>
/// Aggregates all the data needed for the Product Details screen.
/// This includes the product info, the computed current stock, 
/// the history of movements, and a blank form to add a new movement.
/// </summary>
public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;
    
    public int CurrentStock { get; set; }
    
    public List<StockMovement> MovementHistory { get; set; } = new List<StockMovement>();

    public MovementFormViewModel NewMovement { get; set; } = new MovementFormViewModel();
}
