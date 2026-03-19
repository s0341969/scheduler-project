namespace PUR2019.WinForms.Models;

public sealed record PurchaseOrderHeader
{
    public required string OrderNo { get; init; }

    public required DateTime OrderDate { get; init; }

    public required string Department { get; init; }

    public required string Buyer { get; init; }

    public required string StatusCode { get; init; }

    public string Status => StatusCode switch
    {
        "Y" => "已確認",
        "X" => "已作廢",
        _ => "未確認"
    };

    public decimal TotalAmount { get; init; }
}
