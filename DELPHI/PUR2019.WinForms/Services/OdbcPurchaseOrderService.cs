using System.Data;
using System.Data.Odbc;
using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Services;

public sealed class OdbcPurchaseOrderService : IPurchaseOrderService
{
    private readonly string _connectionString;
    private readonly bool _enableLegacyStoredProcedureChecks;

    public OdbcPurchaseOrderService(string connectionString, bool enableLegacyStoredProcedureChecks)
    {
        _connectionString = connectionString;
        _enableLegacyStoredProcedureChecks = enableLegacyStoredProcedureChecks;
    }

    public IReadOnlyList<PurchaseOrderHeader> QueryHeaders(DateTime fromDate, DateTime toDate, string? department)
    {
        if (fromDate.Date > toDate.Date)
        {
            throw new ArgumentException("開始日期不可大於結束日期。", nameof(fromDate));
        }

        var headers = new List<PurchaseOrderHeader>();
        using var connection = new OdbcConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT T.PURNO, T.PURDY, T.PURDP, T.PUUSR, T.SCTRL, ISNULL(S.SUM_AMT, 0) AS SUM_AMT " +
            "FROM PURTM T " +
            "LEFT JOIN (SELECT PURNO, SUM(ISNULL(PURM1,0)) AS SUM_AMT FROM PURTD GROUP BY PURNO) S ON T.PURNO=S.PURNO " +
            "WHERE T.PURTP='0' AND T.PURDY>=? AND T.PURDY<=?";
        command.Parameters.Add("@from", OdbcType.DateTime).Value = fromDate.Date;
        command.Parameters.Add("@to", OdbcType.DateTime).Value = toDate.Date;

        if (!string.IsNullOrWhiteSpace(department))
        {
            command.CommandText += " AND T.PURDP=?";
            command.Parameters.Add("@dep", OdbcType.VarChar).Value = department.Trim();
        }

        command.CommandText += " ORDER BY T.PURNO";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            headers.Add(new PurchaseOrderHeader
            {
                OrderNo = reader["PURNO"].ToString()?.Trim() ?? string.Empty,
                OrderDate = reader["PURDY"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["PURDY"]),
                Department = reader["PURDP"].ToString()?.Trim() ?? string.Empty,
                Buyer = reader["PUUSR"].ToString()?.Trim() ?? string.Empty,
                StatusCode = NormalizeStatusCode(reader["SCTRL"].ToString()),
                TotalAmount = reader["SUM_AMT"] is DBNull ? 0M : Convert.ToDecimal(reader["SUM_AMT"])
            });
        }

        return headers;
    }

    public IReadOnlyList<PurchaseOrderLine> QueryLines(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            return Array.Empty<PurchaseOrderLine>();
        }

        var lines = new List<PurchaseOrderLine>();

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT T.PURNO, T.PURSQ, T.INDWG, T.PURCL, T.PUPRP, T.PUPA1, T.PUPA2, " +
            "T.PUQY1, T.PURP1, T.PUNDY, T.SCTRL, ISNULL(T.PURM2,0) AS PURM2, ISNULL(T.PURP2,0) AS PURP2, " +
            "ISNULL(S.MOQ,0) AS MOQ " +
            "FROM PURTD T " +
            "LEFT JOIN INVMAST_SUPPLIER S ON S.INDWG=T.INDWG AND S.SQ='001' " +
            "WHERE T.PURNO=? ORDER BY T.PURNO, T.PURSQ";
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var sequenceText = reader["PURSQ"].ToString()?.Trim() ?? "0";
            lines.Add(new PurchaseOrderLine
            {
                OrderNo = reader["PURNO"].ToString()?.Trim() ?? string.Empty,
                Sequence = int.TryParse(sequenceText, out var seq) ? seq : 0,
                ItemNo = reader["INDWG"].ToString()?.Trim() ?? string.Empty,
                ItemName = reader["PURCL"].ToString()?.Trim() ?? string.Empty,
                SourceOrderNo = reader["PUPRP"].ToString()?.Trim() ?? string.Empty,
                ProcessFrom = reader["PUPA1"].ToString()?.Trim() ?? "0",
                ProcessTo = reader["PUPA2"].ToString()?.Trim() ?? "0",
                Quantity = reader["PUQY1"] is DBNull ? 0M : Convert.ToDecimal(reader["PUQY1"]),
                UnitPrice = reader["PURP1"] is DBNull ? 0M : Convert.ToDecimal(reader["PURP1"]),
                ReferenceAmount = reader["PURM2"] is DBNull ? 0M : Convert.ToDecimal(reader["PURM2"]),
                CostRatio = reader["PURP2"] is DBNull ? 0M : Convert.ToDecimal(reader["PURP2"]),
                MinimumOrderQty = reader["MOQ"] is DBNull ? 0 : Convert.ToInt32(reader["MOQ"]),
                DueDate = reader["PUNDY"] is DBNull ? null : Convert.ToDateTime(reader["PUNDY"]),
                StatusCode = NormalizeStatusCode(reader["SCTRL"].ToString())
            });
        }

        return lines;
    }

    public PurchaseOrderHeader CreateOrder(CreatePurchaseOrderRequest request)
    {
        ValidateHeaderRequest(request.OrderDate, request.Department, request.Buyer, request.UserId);

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var orderNo = GenerateNextOrderNo(connection, transaction, request.OrderDate);

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO PURTM (PURNO, PURDY, PURDP, PUUSR, PURTP, SCTRL, CRUSER, CRDATE, AMDUSR, AMDDAY, CFUSER, CFDAY, PRUSER, PNNO, FACT) " +
            "VALUES (?, ?, ?, ?, '0', 'N', ?, GETDATE(), '', NULL, '', NULL, '', 0, '21')";
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo;
        command.Parameters.Add("@purdy", OdbcType.DateTime).Value = request.OrderDate.Date;
        command.Parameters.Add("@purdp", OdbcType.VarChar).Value = request.Department.Trim();
        command.Parameters.Add("@puusr", OdbcType.VarChar).Value = request.Buyer.Trim();
        command.Parameters.Add("@cruser", OdbcType.VarChar).Value = request.UserId.Trim();
        command.ExecuteNonQuery();

        transaction.Commit();

        return new PurchaseOrderHeader
        {
            OrderNo = orderNo,
            OrderDate = request.OrderDate.Date,
            Department = request.Department.Trim(),
            Buyer = request.Buyer.Trim(),
            StatusCode = "N",
            TotalAmount = 0M
        };
    }

    public void UpdateOrder(UpdatePurchaseOrderRequest request)
    {
        ValidateHeaderRequest(request.OrderDate, request.Department, request.Buyer, request.UserId);

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var status = GetOrderStatus(connection, transaction, request.OrderNo);
        EnsureEditableStatus(status);

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE PURTM SET PURDY=?, PURDP=?, PUUSR=?, AMDUSR=?, AMDDAY=GETDATE() WHERE PURNO=?";
        command.Parameters.Add("@purdy", OdbcType.DateTime).Value = request.OrderDate.Date;
        command.Parameters.Add("@purdp", OdbcType.VarChar).Value = request.Department.Trim();
        command.Parameters.Add("@puusr", OdbcType.VarChar).Value = request.Buyer.Trim();
        command.Parameters.Add("@amdusr", OdbcType.VarChar).Value = request.UserId.Trim();
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = request.OrderNo.Trim();

        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException("更新失敗：找不到單頭資料。");
        }

        transaction.Commit();
    }

    public void DeleteOrder(string orderNo)
    {
        EnsureOrderNo(orderNo);

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var status = GetOrderStatus(connection, transaction, orderNo);
        EnsureEditableStatus(status);

        using var countCommand = connection.CreateCommand();
        countCommand.Transaction = transaction;
        countCommand.CommandText = "SELECT COUNT(1) FROM PURTD WHERE PURNO=?";
        countCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        var lineCount = Convert.ToInt32(countCommand.ExecuteScalar());

        if (lineCount > 0)
        {
            throw new InvalidOperationException("單頭尚有單身資料，不能刪除。請先刪除單身。");
        }

        using var deleteCommand = connection.CreateCommand();
        deleteCommand.Transaction = transaction;
        deleteCommand.CommandText = "DELETE FROM PURTM WHERE PURNO=?";
        deleteCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        deleteCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    public void ConfirmOrder(string orderNo, string userId)
    {
        EnsureOrderNo(orderNo);
        EnsureUser(userId);

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var status = GetOrderStatus(connection, transaction, orderNo);
        if (status == "X")
        {
            throw new InvalidOperationException("已作廢單據不可確認。");
        }

        if (_enableLegacyStoredProcedureChecks)
        {
            ExecuteLegacyCheckProcedure(connection, transaction, "EXEC dbo.PUR_採購料號檢核 ?", orderNo, "採購料號檢核未通過");
            ExecuteLegacyCheckProcedure(connection, transaction, "EXEC dbo.PUR2019_檢核訂單變更 ?, ?", orderNo, "訂單變更檢核未通過", "PUR2019");
        }

        using var lineCommand = connection.CreateCommand();
        lineCommand.Transaction = transaction;
        lineCommand.CommandText = "UPDATE PURTD SET SCTRL='Y', CFMUSER=?, CFMDATE=GETDATE() WHERE PURNO=? AND SCTRL='N'";
        lineCommand.Parameters.Add("@user", OdbcType.VarChar).Value = userId.Trim();
        lineCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        lineCommand.ExecuteNonQuery();

        using var headerCommand = connection.CreateCommand();
        headerCommand.Transaction = transaction;
        headerCommand.CommandText = "UPDATE PURTM SET SCTRL='Y', CFUSER=?, CFDAY=GETDATE() WHERE PURNO=?";
        headerCommand.Parameters.Add("@user", OdbcType.VarChar).Value = userId.Trim();
        headerCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        headerCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    public void UnconfirmOrder(string orderNo, string userId)
    {
        EnsureOrderNo(orderNo);
        EnsureUser(userId);

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var status = GetOrderStatus(connection, transaction, orderNo);
        if (status == "X")
        {
            throw new InvalidOperationException("已作廢單據不可取消確認。");
        }

        if (status != "Y")
        {
            throw new InvalidOperationException("目前狀態不是已確認，不能取消確認。");
        }

        using var protectCommand = connection.CreateCommand();
        protectCommand.Transaction = transaction;
        protectCommand.CommandText =
            "SELECT TOP 1 1 FROM PURDEL D INNER JOIN PURTD T ON D.PURNO=T.PA1NO AND D.PURSQ=T.PA1SQ WHERE T.PURNO=?";
        protectCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        var hasIssued = protectCommand.ExecuteScalar() is not null;
        if (hasIssued)
        {
            throw new InvalidOperationException("此採購單已有發料紀錄，不能取消確認。");
        }

        using var lineCommand = connection.CreateCommand();
        lineCommand.Transaction = transaction;
        lineCommand.CommandText = "UPDATE PURTD SET SCTRL='N', CFMUSER=?, CFMDATE=GETDATE() WHERE PURNO=? AND SCTRL='Y'";
        lineCommand.Parameters.Add("@user", OdbcType.VarChar).Value = userId.Trim();
        lineCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        lineCommand.ExecuteNonQuery();

        using var headerCommand = connection.CreateCommand();
        headerCommand.Transaction = transaction;
        headerCommand.CommandText = "UPDATE PURTM SET SCTRL='N', CFUSER=?, CFDAY=GETDATE() WHERE PURNO=?";
        headerCommand.Parameters.Add("@user", OdbcType.VarChar).Value = userId.Trim();
        headerCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        headerCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    public void VoidOrder(string orderNo, string userId)
    {
        EnsureOrderNo(orderNo);
        EnsureUser(userId);

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var status = GetOrderStatus(connection, transaction, orderNo);
        if (status == "X")
        {
            throw new InvalidOperationException("單據已作廢。");
        }

        if (status == "Y")
        {
            throw new InvalidOperationException("已確認單據不可直接作廢，請先取消確認。");
        }

        using var headerCommand = connection.CreateCommand();
        headerCommand.Transaction = transaction;
        headerCommand.CommandText = "UPDATE PURTM SET SCTRL='X', AMDUSR=?, AMDDAY=GETDATE() WHERE PURNO=?";
        headerCommand.Parameters.Add("@user", OdbcType.VarChar).Value = userId.Trim();
        headerCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        headerCommand.ExecuteNonQuery();

        using var lineCommand = connection.CreateCommand();
        lineCommand.Transaction = transaction;
        lineCommand.CommandText = "DELETE FROM PURTD WHERE PURNO=?";
        lineCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        lineCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    public PurchaseOrderLine AddLine(CreatePurchaseOrderLineRequest request)
    {
        ValidateLineRequest(request);

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var status = GetOrderStatus(connection, transaction, request.OrderNo);
        EnsureEditableStatus(status);

        var source = ValidateSourceOrderForLine(connection, transaction, request.SourceOrderNo);
        var process = NormalizeProcessRange(request.ProcessFrom, request.ProcessTo, source.MaxProcessSq);
        var moq = GetMoq(connection, transaction, request.ItemNo);
        if (moq > 0 && request.Quantity < moq)
        {
            throw new InvalidOperationException($"數量不可小於 MOQ({moq})。");
        }

        var sequence = GetNextSequence(connection, transaction, request.OrderNo);
        var amount = request.Quantity * request.UnitPrice;
        var referenceAmount = CalculateReferenceAmount(connection, transaction, request.SourceOrderNo, process.ProcessFrom, process.ProcessTo);
        var costRatio = referenceAmount > 0M ? Math.Round(amount / referenceAmount, 3, MidpointRounding.AwayFromZero) : 0M;
        var dueDate = request.DueDate?.Date ?? DateTime.Today;
        var remark = string.IsNullOrWhiteSpace(request.SourceOrderNo) ? string.Empty : $"製令:{request.SourceOrderNo.Trim()} 數量={source.OrderQty:0.##}";

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO PURTD " +
            "(PURNO, PURSQ, INDWG, PURSZ, PURCL, PUMTP, PUWAY, PUSET1, PUSET2, PUQY1, PUQY2, PUNDY, PUPRP, PUROD, PUPA1, PUPA2, " +
            " PUUSR, PUCUS, PURDP, PURCA, PURRA, PURP1, PURM1, PURP2, PURM2, PA1NO, PA1SQ, PA1SQ1, PUREM, SCTRL, CRUSER, CRDATE, AMDUSR, AMDDAY, PUUAP, PURTP, PUBUG) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'N', ?, GETDATE(), '', GETDATE(), 'N', '1', '')";
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = request.OrderNo.Trim();
        command.Parameters.Add("@pursq", OdbcType.VarChar).Value = sequence.ToString("000");
        command.Parameters.Add("@indwg", OdbcType.VarChar).Value = request.ItemNo.Trim();
        command.Parameters.Add("@pursz", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@purcl", OdbcType.VarChar).Value = request.ItemName.Trim();
        command.Parameters.Add("@pumtp", OdbcType.VarChar).Value = "00";
        command.Parameters.Add("@puway", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@puset1", OdbcType.VarChar).Value = "PCS";
        command.Parameters.Add("@puset2", OdbcType.VarChar).Value = "PCS";
        command.Parameters.Add("@puqy1", OdbcType.Decimal).Value = request.Quantity;
        command.Parameters.Add("@puqy2", OdbcType.Decimal).Value = 0M;
        command.Parameters.Add("@pundy", OdbcType.DateTime).Value = dueDate;
        command.Parameters.Add("@puprp", OdbcType.VarChar).Value = request.SourceOrderNo.Trim();
        command.Parameters.Add("@purod", OdbcType.VarChar).Value = request.SourceOrderNo.Trim();
        command.Parameters.Add("@pupa1", OdbcType.VarChar).Value = process.ProcessFrom;
        command.Parameters.Add("@pupa2", OdbcType.VarChar).Value = process.ProcessTo;
        command.Parameters.Add("@puusr", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@pucus", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@purdp", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@purca", OdbcType.VarChar).Value = "NT";
        command.Parameters.Add("@purra", OdbcType.Decimal).Value = 1M;
        command.Parameters.Add("@purp1", OdbcType.Decimal).Value = request.UnitPrice;
        command.Parameters.Add("@purm1", OdbcType.Decimal).Value = amount;
        command.Parameters.Add("@purp2", OdbcType.Decimal).Value = costRatio;
        command.Parameters.Add("@purm2", OdbcType.Decimal).Value = referenceAmount;
        command.Parameters.Add("@pa1no", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@pa1sq", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@pa1sq1", OdbcType.VarChar).Value = string.Empty;
        command.Parameters.Add("@purem", OdbcType.VarChar).Value = remark;
        command.Parameters.Add("@cruser", OdbcType.VarChar).Value = request.UserId.Trim();
        command.ExecuteNonQuery();

        if (!string.IsNullOrWhiteSpace(request.SourceOrderNo))
        {
            using var markCommand = connection.CreateCommand();
            markCommand.Transaction = transaction;
            markCommand.CommandText = "UPDATE ORDMENO SET MPCHK='Y', MPDATE=?, MPNO=?, MPSQ=? WHERE INPART=?";
            markCommand.Parameters.Add("@mpdate", OdbcType.DateTime).Value = DateTime.Today;
            markCommand.Parameters.Add("@mpno", OdbcType.VarChar).Value = request.OrderNo.Trim();
            markCommand.Parameters.Add("@mpsq", OdbcType.VarChar).Value = sequence.ToString("000");
            markCommand.Parameters.Add("@inpart", OdbcType.VarChar).Value = request.SourceOrderNo.Trim();
            markCommand.ExecuteNonQuery();
        }

        transaction.Commit();

        return new PurchaseOrderLine
        {
            OrderNo = request.OrderNo.Trim(),
            Sequence = sequence,
            ItemNo = request.ItemNo.Trim(),
            ItemName = request.ItemName.Trim(),
            SourceOrderNo = request.SourceOrderNo.Trim(),
            ProcessFrom = process.ProcessFrom,
            ProcessTo = process.ProcessTo,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            ReferenceAmount = referenceAmount,
            CostRatio = costRatio,
            MinimumOrderQty = moq,
            DueDate = dueDate,
            StatusCode = "N"
        };
    }

    public void DeleteLine(string orderNo, int sequence)
    {
        EnsureOrderNo(orderNo);
        if (sequence <= 0)
        {
            throw new InvalidOperationException("單身序號必須大於 0。");
        }

        using var connection = new OdbcConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var status = GetOrderStatus(connection, transaction, orderNo);
        EnsureEditableStatus(status);

        string sourceOrderNo;
        string issueOrderNo;
        string issueOrderSq;
        using (var getCommand = connection.CreateCommand())
        {
            getCommand.Transaction = transaction;
            getCommand.CommandText = "SELECT PUPRP, PA1NO, PA1SQ FROM PURTD WHERE PURNO=? AND PURSQ=?";
            getCommand.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
            getCommand.Parameters.Add("@pursq", OdbcType.VarChar).Value = sequence.ToString("000");
            using var reader = getCommand.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException("找不到要刪除的單身資料。");
            }

            sourceOrderNo = reader["PUPRP"]?.ToString()?.Trim() ?? string.Empty;
            issueOrderNo = reader["PA1NO"]?.ToString()?.Trim() ?? string.Empty;
            issueOrderSq = reader["PA1SQ"]?.ToString()?.Trim() ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(issueOrderNo) && !string.IsNullOrWhiteSpace(issueOrderSq))
        {
            using var issueCheck = connection.CreateCommand();
            issueCheck.Transaction = transaction;
            issueCheck.CommandText = "SELECT TOP 1 1 FROM PURDEL WHERE PURNO=? AND PURSQ=?";
            issueCheck.Parameters.Add("@purno", OdbcType.VarChar).Value = issueOrderNo;
            issueCheck.Parameters.Add("@pursq", OdbcType.VarChar).Value = issueOrderSq;
            if (issueCheck.ExecuteScalar() is not null)
            {
                throw new InvalidOperationException("此單身已有發料紀錄，不能刪除。");
            }
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM PURTD WHERE PURNO=? AND PURSQ=?";
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        command.Parameters.Add("@pursq", OdbcType.VarChar).Value = sequence.ToString("000");

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("找不到要刪除的單身資料。");
        }

        if (!string.IsNullOrWhiteSpace(sourceOrderNo))
        {
            using var countCommand = connection.CreateCommand();
            countCommand.Transaction = transaction;
            countCommand.CommandText = "SELECT COUNT(1) FROM PURTD WHERE PUPRP=?";
            countCommand.Parameters.Add("@puprop", OdbcType.VarChar).Value = sourceOrderNo;
            var remainCount = Convert.ToInt32(countCommand.ExecuteScalar());

            if (remainCount == 0)
            {
                using var unmarkCommand = connection.CreateCommand();
                unmarkCommand.Transaction = transaction;
                unmarkCommand.CommandText = "UPDATE ORDMENO SET MPCHK='N' WHERE INPART=?";
                unmarkCommand.Parameters.Add("@inpart", OdbcType.VarChar).Value = sourceOrderNo;
                unmarkCommand.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    private (decimal OrderQty, int MaxProcessSq) ValidateSourceOrderForLine(OdbcConnection connection, OdbcTransaction transaction, string sourceOrderNo)
    {
        if (string.IsNullOrWhiteSpace(sourceOrderNo))
        {
            return (0M, 0);
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT TOP 1 A.SCTRL, ISNULL(B.ORDQTY,0) AS ORDQTY FROM ORDE2 A " +
            "INNER JOIN ORDE3 B ON B.ORDTP=A.ORDTP AND B.ORDNO=A.ORDNO AND B.ORDSQ=A.ORDSQ " +
            "WHERE B.INPART=?";
        command.Parameters.Add("@inpart", OdbcType.VarChar).Value = sourceOrderNo.Trim();

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("製令單號不存在，不能新增單身。");
        }

        var orderStatus = reader["SCTRL"].ToString()?.Trim().ToUpperInvariant() ?? string.Empty;
        if (orderStatus != "Y")
        {
            throw new InvalidOperationException("製令狀態非可採購狀態，不能新增單身。");
        }

        var orderQty = reader["ORDQTY"] is DBNull ? 0M : Convert.ToDecimal(reader["ORDQTY"]);

        using var sqCommand = connection.CreateCommand();
        sqCommand.Transaction = transaction;
        sqCommand.CommandText = "SELECT ISNULL(MAX(ORDSQ2),0) FROM ORDDE4 WHERE ORDFNO=?";
        sqCommand.Parameters.Add("@ordfno", OdbcType.VarChar).Value = sourceOrderNo.Trim();
        var maxSq = Convert.ToInt32(sqCommand.ExecuteScalar());

        return (orderQty, maxSq);
    }

    private static (string ProcessFrom, string ProcessTo) NormalizeProcessRange(string processFromRaw, string processToRaw, int maxSq)
    {
        var fromText = string.IsNullOrWhiteSpace(processFromRaw) ? "0" : processFromRaw.Trim();
        var toText = string.IsNullOrWhiteSpace(processToRaw) ? "0" : processToRaw.Trim();

        if (!int.TryParse(fromText, out var p1) || !int.TryParse(toText, out var p2))
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

        if (maxSq > 0 && p2 > maxSq)
        {
            throw new InvalidOperationException($"製程上限不可超過最大站次({maxSq})。");
        }

        return (p1.ToString(), p2.ToString());
    }

    private static decimal CalculateReferenceAmount(OdbcConnection connection, OdbcTransaction transaction, string sourceOrderNo, string processFrom, string processTo)
    {
        if (string.IsNullOrWhiteSpace(sourceOrderNo))
        {
            return 0M;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        if (processFrom == "0" && processTo == "0")
        {
            command.CommandText = "SELECT ISNULL(SUM(ISNULL(ORDAMT,0)),0) FROM ORDDE4 WHERE ORDFNO=?";
            command.Parameters.Add("@ordfno", OdbcType.VarChar).Value = sourceOrderNo.Trim();
            return Convert.ToDecimal(command.ExecuteScalar());
        }

        if (processFrom == "99" && processTo == "99")
        {
            command.CommandText = "SELECT ISNULL(SUM(ISNULL(AMT,0)-ISNULL(ORDEMT,0)),0) FROM ORDDE4 WHERE ORDFNO=?";
            command.Parameters.Add("@ordfno", OdbcType.VarChar).Value = sourceOrderNo.Trim();
            return Convert.ToDecimal(command.ExecuteScalar());
        }

        command.CommandText = "SELECT ISNULL(SUM(ISNULL(AMT,0)-ISNULL(ORDEMT,0)),0) FROM ORDDE4 WHERE ORDFNO=? AND ORDSQ2 BETWEEN ? AND ?";
        command.Parameters.Add("@ordfno", OdbcType.VarChar).Value = sourceOrderNo.Trim();
        command.Parameters.Add("@fromSq", OdbcType.Int).Value = int.Parse(processFrom);
        command.Parameters.Add("@toSq", OdbcType.Int).Value = int.Parse(processTo);
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    private static int GetMoq(OdbcConnection connection, OdbcTransaction transaction, string itemNo)
    {
        if (string.IsNullOrWhiteSpace(itemNo))
        {
            return 0;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT TOP 1 ISNULL(MOQ,0) FROM INVMAST_SUPPLIER WHERE INDWG=? AND SQ='001'";
        command.Parameters.Add("@indwg", OdbcType.VarChar).Value = itemNo.Trim();
        var result = command.ExecuteScalar();
        return result is null || result is DBNull ? 0 : Convert.ToInt32(result);
    }

    private static string NormalizeStatusCode(string? statusCode)
    {
        var code = statusCode?.Trim().ToUpperInvariant();
        return code is "Y" or "X" ? code : "N";
    }

    private static void ValidateHeaderRequest(DateTime orderDate, string department, string buyer, string userId)
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

        EnsureUser(userId);
    }

    private static void ValidateLineRequest(CreatePurchaseOrderLineRequest request)
    {
        EnsureOrderNo(request.OrderNo);

        if (string.IsNullOrWhiteSpace(request.ItemNo))
        {
            throw new InvalidOperationException("料號不可空白。");
        }

        if (string.IsNullOrWhiteSpace(request.ItemName))
        {
            throw new InvalidOperationException("品名不可空白。");
        }

        if (string.IsNullOrWhiteSpace(request.ProcessFrom) || string.IsNullOrWhiteSpace(request.ProcessTo))
        {
            throw new InvalidOperationException("製程區間不可空白。");
        }

        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("數量必須大於 0。");
        }

        if (request.UnitPrice < 0)
        {
            throw new InvalidOperationException("單價不可為負數。");
        }

        EnsureUser(request.UserId);
    }

    private static void EnsureOrderNo(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            throw new InvalidOperationException("單號不可空白。");
        }
    }

    private static void EnsureUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("使用者代號不可空白。");
        }
    }

    private static void EnsureEditableStatus(string status)
    {
        if (status == "Y")
        {
            throw new InvalidOperationException("已確認單據不可修改。請先取消確認。");
        }

        if (status == "X")
        {
            throw new InvalidOperationException("已作廢單據不可修改。");
        }
    }

    private static string GetOrderStatus(OdbcConnection connection, OdbcTransaction transaction, string orderNo)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT SCTRL FROM PURTM WHERE PURNO=?";
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        var result = command.ExecuteScalar();
        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException($"找不到單號：{orderNo}");
        }

        return NormalizeStatusCode(Convert.ToString(result));
    }

    private static int GetNextSequence(OdbcConnection connection, OdbcTransaction transaction, string orderNo)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT MAX(PURSQ) FROM PURTD WHERE PURNO=?";
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo.Trim();
        var raw = command.ExecuteScalar();
        var maxText = raw is null || raw is DBNull ? "0" : Convert.ToString(raw);
        return int.TryParse(maxText, out var maxValue) ? maxValue + 1 : 1;
    }

    private static string GenerateNextOrderNo(OdbcConnection connection, OdbcTransaction transaction, DateTime orderDate)
    {
        var prefix = orderDate.ToString("yyMMdd");

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT ISNULL(MAX(PURNO), '') FROM PURTM WHERE SUBSTRING(PURNO,1,6)=?";
        command.Parameters.Add("@prefix", OdbcType.VarChar).Value = prefix;

        var raw = Convert.ToString(command.ExecuteScalar())?.Trim() ?? string.Empty;
        var next = 1;

        if (raw.Length >= 10 && int.TryParse(raw.Substring(6, 4), out var seq))
        {
            next = seq + 1;
        }

        return $"{prefix}{next:0000}";
    }

    private static void ExecuteLegacyCheckProcedure(
        OdbcConnection connection,
        OdbcTransaction transaction,
        string sql,
        string orderNo,
        string defaultError,
        string? programCode = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.Add("@purno", OdbcType.VarChar).Value = orderNo;

        if (!string.IsNullOrWhiteSpace(programCode))
        {
            command.Parameters.Add("@prog", OdbcType.VarChar).Value = programCode;
        }

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return;
        }

        var message = reader[0]?.ToString()?.Trim();
        if (!string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException(message);
        }

        if (!reader.IsClosed && reader.FieldCount > 1)
        {
            var secondMessage = reader[1]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(secondMessage))
            {
                throw new InvalidOperationException(secondMessage);
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        throw new InvalidOperationException(defaultError);
    }
}
