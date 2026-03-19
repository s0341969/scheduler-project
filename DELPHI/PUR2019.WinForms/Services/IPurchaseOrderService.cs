using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Services;

public interface IPurchaseOrderService
{
    IReadOnlyList<PurchaseOrderHeader> QueryHeaders(DateTime fromDate, DateTime toDate, string? department);

    IReadOnlyList<PurchaseOrderLine> QueryLines(string orderNo);
}
