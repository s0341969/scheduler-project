namespace PUR2019.WinForms.Models;

public sealed record CreatePurchaseOrderRequest
{
    public required DateTime OrderDate { get; init; }

    public required string Department { get; init; }

    public required string Buyer { get; init; }

    public required string UserId { get; init; }
}

public sealed record UpdatePurchaseOrderRequest
{
    public required string OrderNo { get; init; }

    public required DateTime OrderDate { get; init; }

    public required string Department { get; init; }

    public required string Buyer { get; init; }

    public required string UserId { get; init; }
}

public sealed record CreatePurchaseOrderLineRequest
{
    public required string OrderNo { get; init; }

    public required string ItemNo { get; init; }

    public required string ItemName { get; init; }

    public required string SourceOrderNo { get; init; }

    public required string ProcessFrom { get; init; }

    public required string ProcessTo { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public DateTime? DueDate { get; init; }

    public required string UserId { get; init; }
}
