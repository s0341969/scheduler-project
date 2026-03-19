using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Services;

public interface IPurchaseOrderService
{
    IReadOnlyList<PurchaseOrderHeader> QueryHeaders(DateTime fromDate, DateTime toDate, string? department);

    IReadOnlyList<PurchaseOrderLine> QueryLines(string orderNo);

    PurchaseOrderHeader CreateOrder(CreatePurchaseOrderRequest request);

    void UpdateOrder(UpdatePurchaseOrderRequest request);

    void DeleteOrder(string orderNo);

    void ConfirmOrder(string orderNo, string userId);

    void UnconfirmOrder(string orderNo, string userId);

    void VoidOrder(string orderNo, string userId);

    PurchaseOrderLine AddLine(CreatePurchaseOrderLineRequest request);

    void DeleteLine(string orderNo, int sequence);
}
