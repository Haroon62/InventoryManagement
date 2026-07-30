namespace InventoryManagement.ViewModels;

/// <summary>
/// Aggregates data needed for the tiny dashboard on the home page.
/// </summary>
public class DashboardViewModel
{
    public int TotalSkus { get; set; }
    public int LowStockCount { get; set; }
    
    // We can also include a list of the specific low stock items to display on the dashboard
    public List<ProductListViewModel> LowStockProducts { get; set; } = new List<ProductListViewModel>();
}
