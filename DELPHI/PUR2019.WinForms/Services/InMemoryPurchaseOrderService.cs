using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Services;

public sealed class InMemoryPurchaseOrderService : IPurchaseOrderService
{
    private static readonly IReadOnlyList<PurchaseOrderHeader> Headers =
    [
        new PurchaseOrderHeader { OrderNo = "PO20260301001", OrderDate = new DateTime(2026, 3, 1), Department = "A01", Buyer = "王小明", Status = "確認", TotalAmount = 125000M },
        new PurchaseOrderHeader { OrderNo = "PO20260305009", OrderDate = new DateTime(2026, 3, 5), Department = "B02", Buyer = "陳雅婷", Status = "草稿", TotalAmount = 74800M },
        new PurchaseOrderHeader { OrderNo = "PO20260312003", OrderDate = new DateTime(2026, 3, 12), Department = "A01", Buyer = "林冠宇", Status = "審核中", TotalAmount = 219500M }
    ];

    private static readonly IReadOnlyList<PurchaseOrderLine> Lines =
    [
        new PurchaseOrderLine { OrderNo = "PO20260301001", Sequence = 1, ItemNo = "MAT-1001", ItemName = "鋼板", Quantity = 120M, UnitPrice = 350M, DueDate = new DateTime(2026, 3, 25) },
        new PurchaseOrderLine { OrderNo = "PO20260301001", Sequence = 2, ItemNo = "MAT-3108", ItemName = "螺絲", Quantity = 10000M, UnitPrice = 2.8M, DueDate = new DateTime(2026, 3, 26) },
        new PurchaseOrderLine { OrderNo = "PO20260305009", Sequence = 1, ItemNo = "PKG-2210", ItemName = "包材", Quantity = 4500M, UnitPrice = 8.5M, DueDate = new DateTime(2026, 3, 28) },
        new PurchaseOrderLine { OrderNo = "PO20260312003", Sequence = 1, ItemNo = "EQP-0102", ItemName = "治具", Quantity = 15M, UnitPrice = 8200M, DueDate = new DateTime(2026, 4, 5) },
        new PurchaseOrderLine { OrderNo = "PO20260312003", Sequence = 2, ItemNo = "MAT-7777", ItemName = "銅管", Quantity = 300M, UnitPrice = 322M, DueDate = new DateTime(2026, 4, 1) }
    ];

    public IReadOnlyList<PurchaseOrderHeader> QueryHeaders(DateTime fromDate, DateTime toDate, string? department)
    {
        if (fromDate.Date > toDate.Date)
        {
            throw new ArgumentException("開始日期不可大於結束日期。", nameof(fromDate));
        }

        IEnumerable<PurchaseOrderHeader> query = Headers.Where(x => x.OrderDate.Date >= fromDate.Date && x.OrderDate.Date <= toDate.Date);

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(x => string.Equals(x.Department, department.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(x => x.OrderDate).ThenBy(x => x.OrderNo).ToArray();
    }

    public IReadOnlyList<PurchaseOrderLine> QueryLines(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            return Array.Empty<PurchaseOrderLine>();
        }

        return Lines.Where(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Sequence).ToArray();
    }
}
