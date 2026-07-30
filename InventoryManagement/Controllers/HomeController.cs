using System.Diagnostics;
using InventoryManagement.Services;
using InventoryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Controllers;

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


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode.HasValue)
        {
            if (statusCode == 404)
            {
                _logger.LogWarning("404 Error occurred.");
            }
            ViewData["StatusCode"] = statusCode.Value;
        }
        else
        {
            _logger.LogError("A global exception was caught.");
        }
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
