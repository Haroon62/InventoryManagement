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
    
    // The computed current stock (sum of In - sum of Out)
    public int CurrentStock { get; set; }
    
    // The chronological history of stock movements
    public List<StockMovement> MovementHistory { get; set; } = new List<StockMovement>();

    // This is used for the "Add Movement" inline form on the detail page
    public MovementFormViewModel NewMovement { get; set; } = new MovementFormViewModel();
}
