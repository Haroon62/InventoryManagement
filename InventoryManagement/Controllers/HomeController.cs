using System.Diagnostics;
using InventoryManagement.Services;
using InventoryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Controllers;

/// <summary>
/// The HomeController handles the landing page (Dashboard) and global error display.
/// </summary>
public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly IStockMovementService _stockService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IProductService productService, IStockMovementService stockService, ILogger<HomeController> logger)
    {
        _productService = productService;
        _stockService = stockService;
        _logger = logger;
    }

    /// <summary>
    /// Renders the Tiny Dashboard with aggregate stats.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllProductsAsync();
        
        int lowStockCount = 0;
        var lowStockProducts = new List<ProductListViewModel>();

        foreach (var p in products)
        {
            int currentStock = await _stockService.GetCurrentStockAsync(p.Id);
            
            if (currentStock <= p.ReorderLevel)
            {
                lowStockCount++;
                lowStockProducts.Add(new ProductListViewModel
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    CurrentStock = currentStock,
                    ReorderLevel = p.ReorderLevel
                });
            }
        }
        
        var viewModel = new DashboardViewModel
        {
            TotalSkus = products.Count,
            LowStockCount = lowStockCount,
            LowStockProducts = lowStockProducts
        };
        
        return View(viewModel);
    }

    /// <summary>
    /// Global Error Handler.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        _logger.LogError("A global exception was caught.");
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
