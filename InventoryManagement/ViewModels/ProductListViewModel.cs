namespace InventoryManagement.ViewModels;

/// <summary>
/// Represents a product in a list or table view.
/// We use a ViewModel instead of the raw Model to include computed properties
/// like "IsLowStock" that the UI needs for styling, which shouldn't be in the database model.
/// </summary>
public class ProductListViewModel
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }

    /// <summary>
    /// Computed property. Returns true if current stock is at or below the reorder level.
    /// This is used in the View to easily show a "Low Stock" badge without complex logic in the view.
    /// </summary>
    public bool IsLowStock => CurrentStock <= ReorderLevel;
}
