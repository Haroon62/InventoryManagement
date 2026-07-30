namespace InventoryManagement.ViewModels;

/// <summary>
/// Used to pass error information to the global Error view.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
