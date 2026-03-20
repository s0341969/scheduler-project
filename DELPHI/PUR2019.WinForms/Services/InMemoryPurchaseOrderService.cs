using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Services;

public sealed class InMemoryPurchaseOrderService : IPurchaseOrderService
{
    private readonly object _lockObject = new();

    private readonly List<PurchaseOrderHeader> _headers =
    [
        new PurchaseOrderHeader { OrderNo = "PO20260301001", OrderDate = new DateTime(2026, 3, 1), Department = "A01", Buyer = "王小明", StatusCode = "Y", TotalAmount = 0M },
        new PurchaseOrderHeader { OrderNo = "PO20260305009", OrderDate = new DateTime(2026, 3, 5), Department = "B02", Buyer = "陳雅婷", StatusCode = "N", TotalAmount = 0M },
        new PurchaseOrderHeader { OrderNo = "PO20260312003", OrderDate = new DateTime(2026, 3, 12), Department = "A01", Buyer = "林冠宇", StatusCode = "N", TotalAmount = 0M }
    ];

    private readonly List<PurchaseOrderLine> _lines =
    [
        new PurchaseOrderLine { OrderNo = "PO20260301001", Sequence = 1, ItemNo = "MAT-1001", ItemName = "鋼板", SourceOrderNo = "", ProcessFrom = "0", ProcessTo = "0", Quantity = 120M, UnitPrice = 350M, ReferenceAmount = 0M, CostRatio = 0M, MinimumOrderQty = 0, DueDate = new DateTime(2026, 3, 25), StatusCode = "Y" },
        new PurchaseOrderLine { OrderNo = "PO20260301001", Sequence = 2, ItemNo = "MAT-3108", ItemName = "螺絲", SourceOrderNo = "", ProcessFrom = "0", ProcessTo = "0", Quantity = 10000M, UnitPrice = 2.8M, ReferenceAmount = 0M, CostRatio = 0M, MinimumOrderQty = 0, DueDate = new DateTime(2026, 3, 26), StatusCode = "Y" },
        new PurchaseOrderLine { OrderNo = "PO20260305009", Sequence = 1, ItemNo = "PKG-2210", ItemName = "包材", SourceOrderNo = "MO2503001", ProcessFrom = "0", ProcessTo = "0", Quantity = 4500M, UnitPrice = 8.5M, ReferenceAmount = 51000M, CostRatio = 0.75M, MinimumOrderQty = 500, DueDate = new DateTime(2026, 3, 28), StatusCode = "N" },
        new PurchaseOrderLine { OrderNo = "PO20260312003", Sequence = 1, ItemNo = "EQP-0102", ItemName = "治具", SourceOrderNo = "", ProcessFrom = "0", ProcessTo = "0", Quantity = 15M, UnitPrice = 8200M, ReferenceAmount = 0M, CostRatio = 0M, MinimumOrderQty = 0, DueDate = new DateTime(2026, 4, 5), StatusCode = "N" },
        new PurchaseOrderLine { OrderNo = "PO20260312003", Sequence = 2, ItemNo = "MAT-7777", ItemName = "銅管", SourceOrderNo = "", ProcessFrom = "0", ProcessTo = "0", Quantity = 300M, UnitPrice = 322M, ReferenceAmount = 0M, CostRatio = 0M, MinimumOrderQty = 100, DueDate = new DateTime(2026, 4, 1), StatusCode = "N" }
    ];

    public IReadOnlyList<PurchaseOrderHeader> QueryHeaders(DateTime fromDate, DateTime toDate, string? department)
    {
        if (fromDate.Date > toDate.Date)
        {
            throw new ArgumentException("開始日期不可大於結束日期。", nameof(fromDate));
        }

        lock (_lockObject)
        {
            IEnumerable<PurchaseOrderHeader> query = _headers.Where(x => x.OrderDate.Date >= fromDate.Date && x.OrderDate.Date <= toDate.Date);

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(x => string.Equals(x.Department, department.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return query.Select(RefreshTotalAmount).OrderByDescending(x => x.OrderDate).ThenBy(x => x.OrderNo).ToArray();
        }
    }

    public IReadOnlyList<PurchaseOrderLine> QueryLines(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            return Array.Empty<PurchaseOrderLine>();
        }

        lock (_lockObject)
        {
            return _lines.Where(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Sequence).ToArray();
        }
    }

    public PurchaseOrderHeader CreateOrder(CreatePurchaseOrderRequest request)
    {
        ValidateHeaderFields(request.OrderDate, request.Department, request.Buyer, request.UserId);

        lock (_lockObject)
        {
            var orderNo = GenerateNextOrderNo(request.OrderDate);
            var created = new PurchaseOrderHeader
            {
                OrderNo = orderNo,
                OrderDate = request.OrderDate.Date,
                Department = request.Department.Trim(),
                Buyer = request.Buyer.Trim(),
                StatusCode = "N",
                TotalAmount = 0M
            };

            _headers.Add(created);
            return created;
        }
    }

    public void UpdateOrder(UpdatePurchaseOrderRequest request)
    {
        ValidateHeaderFields(request.OrderDate, request.Department, request.Buyer, request.UserId);

        lock (_lockObject)
        {
            var current = FindOrder(request.OrderNo);
            EnsureEditableStatus(current.StatusCode);
            ReplaceHeader(current with
            {
                OrderDate = request.OrderDate.Date,
                Department = request.Department.Trim(),
                Buyer = request.Buyer.Trim()
            });
        }
    }

    public void DeleteOrder(string orderNo)
    {
        lock (_lockObject)
        {
            var current = FindOrder(orderNo);
            EnsureEditableStatus(current.StatusCode);

            if (_lines.Any(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("單頭尚有單身資料，不能刪除。請先刪除單身。");
            }

            _headers.RemoveAll(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void ConfirmOrder(string orderNo, string userId)
    {
        ValidateUser(userId);

        lock (_lockObject)
        {
            var current = FindOrder(orderNo);
            if (current.StatusCode == "X")
            {
                throw new InvalidOperationException("已作廢單據不可確認。");
            }

            ReplaceHeader(current with { StatusCode = "Y" });
            MarkLines(orderNo, "Y");
        }
    }

    public void UnconfirmOrder(string orderNo, string userId)
    {
        ValidateUser(userId);

        lock (_lockObject)
        {
            var current = FindOrder(orderNo);
            if (current.StatusCode == "X")
            {
                throw new InvalidOperationException("已作廢單據不可取消確認。");
            }

            if (current.StatusCode != "Y")
            {
                throw new InvalidOperationException("目前狀態不是已確認，不能取消確認。");
            }

            ReplaceHeader(current with { StatusCode = "N" });
            MarkLines(orderNo, "N");
        }
    }

    public void VoidOrder(string orderNo, string userId)
    {
        ValidateUser(userId);

        lock (_lockObject)
        {
            var current = FindOrder(orderNo);
            if (current.StatusCode == "X")
            {
                throw new InvalidOperationException("單據已作廢。");
            }

            if (current.StatusCode == "Y")
            {
                throw new InvalidOperationException("已確認單據不可直接作廢，請先取消確認。");
            }

            ReplaceHeader(current with { StatusCode = "X" });
            _lines.RemoveAll(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase));
        }
    }

    public PurchaseOrderLine AddLine(CreatePurchaseOrderLineRequest request)
    {
        ValidateLineFields(request);

        lock (_lockObject)
        {
            var header = FindOrder(request.OrderNo);
            EnsureEditableStatus(header.StatusCode);

            var nextSequence = _lines.Where(x => x.OrderNo.Equals(request.OrderNo, StringComparison.OrdinalIgnoreCase)).Select(x => x.Sequence).DefaultIfEmpty(0).Max() + 1;
            var normalizedProcess = NormalizeProcessRange(request.ProcessFrom, request.ProcessTo);
            var referenceAmount = normalizedProcess.ProcessFrom == "0" && normalizedProcess.ProcessTo == "0" ? 0M : Math.Round(request.Quantity * request.UnitPrice * 1.2M, 2, MidpointRounding.AwayFromZero);
            var costRatio = referenceAmount > 0 ? Math.Round((request.Quantity * request.UnitPrice) / referenceAmount, 3, MidpointRounding.AwayFromZero) : 0M;
            var moq = EstimateMoq(request.ItemNo);
            if (moq > 0 && request.Quantity < moq)
            {
                throw new InvalidOperationException($"數量不可小於 MOQ({moq})。");
            }

            var created = new PurchaseOrderLine
            {
                OrderNo = request.OrderNo,
                Sequence = nextSequence,
                ItemNo = request.ItemNo.Trim(),
                ItemName = request.ItemName.Trim(),
                SourceOrderNo = request.SourceOrderNo.Trim(),
                ProcessFrom = normalizedProcess.ProcessFrom,
                ProcessTo = normalizedProcess.ProcessTo,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                ReferenceAmount = referenceAmount,
                CostRatio = costRatio,
                MinimumOrderQty = moq,
                DueDate = request.DueDate?.Date,
                StatusCode = "N"
            };

            _lines.Add(created);
            return created;
        }
    }

    public void DeleteLine(string orderNo, int sequence)
    {
        lock (_lockObject)
        {
            var header = FindOrder(orderNo);
            EnsureEditableStatus(header.StatusCode);

            var removed = _lines.RemoveAll(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase) && x.Sequence == sequence);
            if (removed == 0)
            {
                throw new InvalidOperationException("找不到要刪除的單身資料。");
            }
        }
    }

    public PurchaseOrderLineSuggestion? GetLineSuggestion(string sourceOrderNo)
    {
        if (string.IsNullOrWhiteSpace(sourceOrderNo))
        {
            return null;
        }

        var key = sourceOrderNo.Trim().ToUpperInvariant();
        return key switch
        {
            "MO2503001" => new PurchaseOrderLineSuggestion
            {
                SourceOrderNo = sourceOrderNo,
                ItemNo = "PKG-2210",
                ItemName = "包材",
                SuggestedQuantity = 4500M,
                SuggestedUnitPrice = 8.5M,
                MinimumOrderQty = 500,
                ProcessFrom = "0",
                ProcessTo = "0"
            },
            _ => new PurchaseOrderLineSuggestion
            {
                SourceOrderNo = sourceOrderNo,
                ItemNo = "MAT-1001",
                ItemName = "料件-" + key,
                SuggestedQuantity = 100M,
                SuggestedUnitPrice = 10M,
                MinimumOrderQty = 0,
                ProcessFrom = "0",
                ProcessTo = "0"
            }
        };
    }

    public PurchaseOrderLinePreview PreviewLine(string sourceOrderNo, string itemNo, string processFrom, string processTo, decimal quantity, decimal unitPrice)
    {
        _ = sourceOrderNo;
        var normalized = NormalizeProcessRange(processFrom, processTo);
        var moq = EstimateMoq(itemNo);
        var amount = quantity * unitPrice;
        var referenceAmount = normalized.ProcessFrom == "0" && normalized.ProcessTo == "0"
            ? 0M
            : Math.Round(amount * 1.2M, 2, MidpointRounding.AwayFromZero);
        var costRatio = referenceAmount > 0M ? Math.Round(amount / referenceAmount, 3, MidpointRounding.AwayFromZero) : 0M;

        return new PurchaseOrderLinePreview
        {
            ProcessFrom = normalized.ProcessFrom,
            ProcessTo = normalized.ProcessTo,
            MinimumOrderQty = moq,
            ReferenceAmount = referenceAmount,
            CostRatio = costRatio,
            Amount = amount
        };
    }

    public string BuildOrderReportText(string orderNo)
    {
        var header = FindOrder(orderNo);
        var lines = _lines.Where(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Sequence).ToArray();
        var totalAmount = lines.Sum(x => x.Amount);
        var totalQty = lines.Sum(x => x.Quantity);

        var output = new List<string>
        {
            "PUR2019 採購單報表",
            $"單號: {header.OrderNo}",
            $"日期: {header.OrderDate:yyyy/MM/dd}",
            $"部門: {header.Department}",
            $"採購員: {header.Buyer}",
            $"狀態: {header.Status} ({header.StatusCode})",
            $"總數量: {totalQty:N2}",
            $"總金額: {totalAmount:N2}",
            "",
            "明細:"
        };

        foreach (var line in lines)
        {
            output.Add($"- {line.Sequence:000} {line.ItemNo} {line.ItemName} Qty={line.Quantity:N2} Price={line.UnitPrice:N2} Amt={line.Amount:N2}");
        }

        return string.Join(Environment.NewLine, output);
    }

    private static (string ProcessFrom, string ProcessTo) NormalizeProcessRange(string processFromRaw, string processToRaw)
    {
        var processFrom = string.IsNullOrWhiteSpace(processFromRaw) ? "0" : processFromRaw.Trim();
        var processTo = string.IsNullOrWhiteSpace(processToRaw) ? "0" : processToRaw.Trim();

        if (!int.TryParse(processFrom, out var p1) || !int.TryParse(processTo, out var p2))
        {
            throw new InvalidOperationException("製程區間必須是數字。");
        }

        if (p1 == 99)
        {
            return ("99", "99");
        }

        if (p1 == 0 && p2 == 0)
        {
            return ("0", "0");
        }

        if (p1 <= 0 || p2 <= 0 || p1 > p2)
        {
            throw new InvalidOperationException("製程區間不合法。請輸入 0-0、99-99 或正確區間。");
        }

        return (p1.ToString(), p2.ToString());
    }

    private static int EstimateMoq(string itemNo)
    {
        return itemNo.Trim().ToUpperInvariant() switch
        {
            "MAT-7777" => 100,
            "PKG-2210" => 500,
            _ => 0
        };
    }

    private void MarkLines(string orderNo, string statusCode)
    {
        for (var i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase))
            {
                _lines[i] = _lines[i] with { StatusCode = statusCode };
            }
        }
    }

    private PurchaseOrderHeader FindOrder(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            throw new ArgumentException("單號不可空白。", nameof(orderNo));
        }

        return _headers.FirstOrDefault(x => x.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"找不到單號：{orderNo}");
    }

    private PurchaseOrderHeader RefreshTotalAmount(PurchaseOrderHeader header)
    {
        var total = _lines.Where(x => x.OrderNo.Equals(header.OrderNo, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount);
        return header with { TotalAmount = total };
    }

    private string GenerateNextOrderNo(DateTime orderDate)
    {
        var prefix = $"PO{orderDate:yyyyMMdd}";
        var maxSeq = _headers.Where(x => x.OrderNo.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Select(x => int.TryParse(x.OrderNo[prefix.Length..], out var seq) ? seq : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{maxSeq + 1:000}";
    }

    private static void ValidateHeaderFields(DateTime orderDate, string department, string buyer, string userId)
    {
        _ = orderDate;

        if (string.IsNullOrWhiteSpace(department))
        {
            throw new InvalidOperationException("部門不可空白。");
        }

        if (string.IsNullOrWhiteSpace(buyer))
        {
            throw new InvalidOperationException("採購員不可空白。");
        }

        ValidateUser(userId);
    }

    private static void ValidateLineFields(CreatePurchaseOrderLineRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderNo))
        {
            throw new InvalidOperationException("單號不可空白。");
        }

        if (string.IsNullOrWhiteSpace(request.ItemNo))
        {
            throw new InvalidOperationException("料號不可空白。");
        }

        if (string.IsNullOrWhiteSpace(request.ItemName))
        {
            throw new InvalidOperationException("品名不可空白。");
        }

        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("數量必須大於 0。");
        }

        if (request.UnitPrice < 0)
        {
            throw new InvalidOperationException("單價不可為負數。");
        }

        ValidateUser(request.UserId);
    }

    private static void ValidateUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("使用者代號不可空白。");
        }
    }

    private static void EnsureEditableStatus(string statusCode)
    {
        if (statusCode == "Y")
        {
            throw new InvalidOperationException("已確認單據不可修改。請先取消確認。");
        }

        if (statusCode == "X")
        {
            throw new InvalidOperationException("已作廢單據不可修改。");
        }
    }

    private void ReplaceHeader(PurchaseOrderHeader header)
    {
        for (var i = 0; i < _headers.Count; i++)
        {
            if (_headers[i].OrderNo.Equals(header.OrderNo, StringComparison.OrdinalIgnoreCase))
            {
                _headers[i] = header;
                return;
            }
        }

        throw new InvalidOperationException($"找不到單號：{header.OrderNo}");
    }
}
