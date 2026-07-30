namespace InventoryManagement.ViewModels;

/// <summary>
/// Aggregates data needed for the tiny dashboard on the home page.
/// </summary>
public class DashboardViewModel
{
    public int TotalSkus { get; set; }
    public int LowStockCount { get; set; }
    
    public List<ProductListViewModel> LowStockProducts { get; set; } = new List<ProductListViewModel>();
}
