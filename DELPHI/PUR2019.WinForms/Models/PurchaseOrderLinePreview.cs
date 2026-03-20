namespace PUR2019.WinForms.Models;

public sealed record PurchaseOrderLinePreview
{
    public string ProcessFrom { get; init; } = "0";

    public string ProcessTo { get; init; } = "0";

    public int MinimumOrderQty { get; init; }

    public decimal ReferenceAmount { get; init; }

    public decimal CostRatio { get; init; }

    public decimal Amount { get; init; }
}
