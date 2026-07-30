using System.ComponentModel.DataAnnotations;
using InventoryManagement.Models;

namespace InventoryManagement.ViewModels;

/// <summary>
/// Used for Create and Edit forms.
/// </summary>
public class ProductFormViewModel
{
    public int Id { get; set; } // 0 for Create, >0 for Edit

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ReorderLevel { get; set; }

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
