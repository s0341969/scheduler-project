namespace PUR2019.WinForms.Models;

public sealed class PurchaseOrderHeader
{
    public required string OrderNo { get; init; }

    public required DateTime OrderDate { get; init; }

    public required string Department { get; init; }

    public required string Buyer { get; init; }

    public required string Status { get; init; }

    public decimal TotalAmount { get; init; }
}
