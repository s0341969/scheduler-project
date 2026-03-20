namespace PUR2019.WinForms.Models;

public sealed record PurchaseOrderLine
{
    public required string OrderNo { get; init; }

    public required int Sequence { get; init; }

    public required string ItemNo { get; init; }

    public required string ItemName { get; init; }

    public required string SourceOrderNo { get; init; }

    public required string ProcessFrom { get; init; }

    public required string ProcessTo { get; init; }

    public string ProcessRange => $"{ProcessFrom}-{ProcessTo}";

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal Amount => Quantity * UnitPrice;

    public decimal ReferenceAmount { get; init; }

    public decimal CostRatio { get; init; }

    public int MinimumOrderQty { get; init; }

    public DateTime? DueDate { get; init; }

    public required string StatusCode { get; init; }
}
