namespace PUR2019.WinForms.Models;

public sealed record PurchaseOrderLine
{
    public required string OrderNo { get; init; }

    public required int Sequence { get; init; }

    public required string ItemNo { get; init; }

    public required string ItemName { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal Amount => Quantity * UnitPrice;

    public DateTime? DueDate { get; init; }

    public required string StatusCode { get; init; }
}
