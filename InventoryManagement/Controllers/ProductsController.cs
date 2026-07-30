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
    public async Task<IActionResult> Index(string search)
    {
        // 1. Call Service to get data
        var products = await _productService.SearchProductsAsync(search);
        
        // 2. Map Domain Models to ViewModels
        var viewModels = new List<ProductListViewModel>();
        foreach (var p in products)
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

        // Pass the search term back to the view so the search box stays populated
        ViewData["SearchTerm"] = search;

        // 3. Return View
        return View(viewModels);
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
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        if (viewModel.Id == 0)
        {
            // CREATE LOGIC
            var product = viewModel.ToProductModel();
            var result = await _productService.CreateProductAsync(product);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Product created successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("Sku", result.ErrorMessage!);
                return View(viewModel);
            }
        }
        else
        {
            // EDIT LOGIC
            var existingProduct = await _productService.GetByIdAsync(viewModel.Id);
            if (existingProduct == null || !existingProduct.IsActive)
            {
                return NotFound();
            }

            existingProduct.Sku = viewModel.Sku;
            existingProduct.Name = viewModel.Name;
            existingProduct.Description = viewModel.Description;
            existingProduct.ReorderLevel = viewModel.ReorderLevel;

            var result = await _productService.UpdateProductAsync(existingProduct);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Product updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("Sku", result.ErrorMessage!);
                return View(viewModel);
            }
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
        // If basic form validation fails, we redirect back to Details.
        // We use TempData to pass the error message across the redirect.
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please ensure all fields are filled out correctly (Quantity > 0).";
            return RedirectToAction(nameof(Details), new { id = viewModel.ProductId });
        }

        var movement = viewModel.ToStockMovementModel();
        
        // Call the service — THIS IS WHERE THE KEY RULE IS ENFORCED
        var result = await _stockMovementService.AddMovementAsync(movement);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Stock movement recorded successfully.";
        }
        else
        {
            // The service rejected it (e.g., tried to take out more than is in stock).
            // This satisfies the assignment requirement: "Reject it with a clear message 
            // that tells the user how much is actually available."
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        // Always redirect back to the product details page so they can see the updated stock
        return RedirectToAction(nameof(Details), new { id = viewModel.ProductId });
    }
}
