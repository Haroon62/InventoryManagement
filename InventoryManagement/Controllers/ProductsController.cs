using InventoryManagement.Models;
using InventoryManagement.Services;
using InventoryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Controllers;

/// <summary>
/// The ProductsController is "thin". It doesn't contain business logic.
/// It receives HTTP requests, calls the Services to do the actual work,
/// and returns the appropriate View (HTML) or a Redirect.
/// </summary>
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IStockMovementService _stockMovementService;

    // Both services are injected automatically by ASP.NET Core DI
    public ProductsController(IProductService productService, IStockMovementService stockMovementService)
    {
        _productService = productService;
        _stockMovementService = stockMovementService;
    }

    // GET: /Products
    // Allows searching via a query string: /Products?search=widget
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] int page = 1)
    {
        int pageSize = 10;
        if (page < 1) page = 1;

        // 1. Call Service to get paginated data
        var pagedResult = await _productService.SearchProductsAsync(search ?? "", page, pageSize);
        
        // 2. Map Domain Models to ViewModels
        var viewModels = new List<ProductListViewModel>();
        foreach (var p in pagedResult.Items)
        {
            viewModels.Add(new ProductListViewModel
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                ReorderLevel = p.ReorderLevel,
                CurrentStock = await _stockMovementService.GetCurrentStockAsync(p.Id) // Fetch current stock
            });
        }

        var pagedViewModel = new PagedViewModel<ProductListViewModel>
        {
            Items = viewModels,
            TotalCount = pagedResult.TotalCount,
            CurrentPage = page,
            PageSize = pageSize,
            SearchTerm = search
        };

        // 3. Return View
        return View(pagedViewModel);
    }

    // GET: /api/stock/{id}
    [HttpGet("/api/stock/{id}")]
    [InventoryManagement.Filters.ApiKey]
    public async Task<IActionResult> GetStockApi(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null || !product.IsActive) return NotFound();

        var stock = await _stockMovementService.GetCurrentStockAsync(id);
        return Json(new { productId = id, sku = product.Sku, currentStock = stock });
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        // 1. Get Product
        var product = await _productService.GetByIdAsync(id);
        if (product == null || !product.IsActive)
        {
            return NotFound(); // Returns 404 page if product doesn't exist or is soft-deleted
        }

        // 2. Build ViewModel with all required pieces (Product, Stock, History)
        var viewModel = new ProductDetailViewModel
        {
            Product = product,
            CurrentStock = await _stockMovementService.GetCurrentStockAsync(id),
            MovementHistory = await _stockMovementService.GetMovementHistoryAsync(id),
            NewMovement = new MovementFormViewModel { ProductId = id } // Pre-fill the ProductId for the form
        };

        return View(viewModel);
    }

    // GET: /Products/AddEdit/5 (or /Products/AddEdit/0 for Create)
    public async Task<IActionResult> AddEdit(int id = 0)
    {
        if (id == 0)
        {
            // Create mode: return empty form
            return View(new ProductFormViewModel());
        }
        else
        {
            // Edit mode: fetch existing data
            var product = await _productService.GetByIdAsync(id);
            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            var viewModel = new ProductFormViewModel
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel
            };

            return View(viewModel);
        }
    }

    // POST: /Products/AddEdit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEdit(ProductFormViewModel viewModel)
    {
        // 1. Basic Form Validation
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Please ensure all fields are correct." });
        }

        if (viewModel.Id == 0)
        {
            // ── CREATE NEW PRODUCT ──
            var product = viewModel.ToProductModel();
            var result = await _productService.CreateProductAsync(product);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Product created successfully.";
                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }
            
            return Json(new { success = false, message = result.ErrorMessage });
        }
        else
        {
            // ── EDIT EXISTING PRODUCT ──
            var existingProduct = await _productService.GetByIdAsync(viewModel.Id);
            if (existingProduct == null || !existingProduct.IsActive)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            existingProduct.Sku = viewModel.Sku;
            existingProduct.Name = viewModel.Name;
            existingProduct.Description = viewModel.Description;
            existingProduct.ReorderLevel = viewModel.ReorderLevel;

            var result = await _productService.UpdateProductAsync(existingProduct);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Product updated successfully.";
                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }
            
            return Json(new { success = false, message = result.ErrorMessage });
        }
    }

    // POST: /Products/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _productService.DeactivateProductAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "Product deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not find the product to delete.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    // POST: /Products/AddMovement
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMovement([Bind(Prefix = "NewMovement")] MovementFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Please ensure all fields are filled out correctly (Quantity > 0)." });
        }

        var movement = viewModel.ToStockMovementModel();
        var result = await _stockMovementService.AddMovementAsync(movement);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Stock movement recorded successfully.";
            return Json(new { success = true });
        }
        
        return Json(new { success = false, message = result.ErrorMessage });
    }
}
