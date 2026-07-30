namespace InventoryManagement.Enums;

/// <summary>
/// Represents the direction of a stock movement.
/// EF Core stores this as INT in SQL Server: In = 0, Out = 1.
/// </summary>
public enum MovementType
{
    /// <summary>
    /// Stock coming IN — e.g., delivery received from supplier.
    /// Stored as 0 in the database.
    /// </summary>
    In,

    /// <summary>
    /// Stock going OUT — e.g., sold to customer, damaged, or returned.
    /// Stored as 1 in the database.
    /// </summary>
    Out
}
