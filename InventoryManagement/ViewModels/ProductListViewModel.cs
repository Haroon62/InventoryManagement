namespace InventoryManagement.ViewModels;

/// <summary>
/// Represents a product in a list or table view.
/// </summary>
public class ProductListViewModel
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }

    public bool IsLowStock => CurrentStock <= ReorderLevel;
}
