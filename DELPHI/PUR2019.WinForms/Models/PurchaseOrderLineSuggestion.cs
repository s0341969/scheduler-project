namespace PUR2019.WinForms.Models;

public sealed record PurchaseOrderLineSuggestion
{
    public required string SourceOrderNo { get; init; }

    public required string ItemNo { get; init; }

    public required string ItemName { get; init; }

    public decimal SuggestedQuantity { get; init; }

    public decimal SuggestedUnitPrice { get; init; }

    public int MinimumOrderQty { get; init; }

    public string ProcessFrom { get; init; } = "0";

    public string ProcessTo { get; init; } = "0";
}
