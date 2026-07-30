using System.ComponentModel.DataAnnotations;
using InventoryManagement.Models;

namespace InventoryManagement.ViewModels;

/// <summary>
/// Used for Create and Edit forms.
/// We use a separate ViewModel for forms instead of the database model (Product) because:
/// 1. We might only want the user to edit a subset of properties (e.g., they can't edit CreatedUtc).
/// 2. The validation rules for the UI might differ slightly from the database schema.
/// </summary>
public class ProductFormViewModel
{
    public int Id { get; set; } // 0 for Create, >0 for Edit

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ReorderLevel { get; set; }

    /// <summary>
    /// Helper method to convert this ViewModel back into a Domain Model.
    /// This keeps the Controller clean.
    /// </summary>
    public Product ToProductModel()
    {
        return new Product
        {
            Id = this.Id,
            Sku = this.Sku,
            Name = this.Name,
            Description = this.Description,
            ReorderLevel = this.ReorderLevel
            // IsActive and CreatedUtc are handled by the Service layer
        };
    }
}
