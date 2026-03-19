using System.Data.Odbc;
using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Services;

public sealed class OdbcPurchaseOrderService : IPurchaseOrderService
{
    private readonly string _connectionString;

    public OdbcPurchaseOrderService(string connectionString)
    {
        _connectionString = connectionString;
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
            "SELECT PURNO, PURDY, PURDP, PUUSR, SCTRL " +
            "FROM PURTM " +
            "WHERE PURTP='0' AND PURDY>=? AND PURDY<=?";

        command.Parameters.Add("@from", OdbcType.DateTime).Value = fromDate.Date;
        command.Parameters.Add("@to", OdbcType.DateTime).Value = toDate.Date;

        if (!string.IsNullOrWhiteSpace(department))
        {
            command.CommandText += " AND PURDP=?";
            command.Parameters.Add("@dep", OdbcType.VarChar).Value = department.Trim();
        }

        command.CommandText += " ORDER BY PURNO";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            headers.Add(new PurchaseOrderHeader
            {
                OrderNo = reader["PURNO"].ToString()?.Trim() ?? string.Empty,
                OrderDate = reader["PURDY"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["PURDY"]),
                Department = reader["PURDP"].ToString()?.Trim() ?? string.Empty,
                Buyer = reader["PUUSR"].ToString()?.Trim() ?? string.Empty,
                Status = MapStatus(reader["SCTRL"].ToString()),
                TotalAmount = 0M
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
            "SELECT PURNO, PURSQ, INDWG, PURCL, PUQY1, PURP1, PUNDY " +
            "FROM PURTD " +
            "WHERE PURNO=? " +
            "ORDER BY PURNO, PURSQ";
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
                Quantity = reader["PUQY1"] is DBNull ? 0M : Convert.ToDecimal(reader["PUQY1"]),
                UnitPrice = reader["PURP1"] is DBNull ? 0M : Convert.ToDecimal(reader["PURP1"]),
                DueDate = reader["PUNDY"] is DBNull ? null : Convert.ToDateTime(reader["PUNDY"])
            });
        }

        return lines;
    }

    private static string MapStatus(string? sctrl)
    {
        return sctrl?.Trim().ToUpperInvariant() switch
        {
            "Y" => "已確認",
            "X" => "已作廢",
            _ => "未確認"
        };
    }
}
