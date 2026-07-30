using System.Collections.Generic;

namespace InventoryManagement.ViewModels;

public class PagedViewModel<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public string? SearchTerm { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
