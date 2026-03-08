using SchedulerConfigProvider;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Data.Odbc;
using System.Globalization;

namespace SchedulerDragDrop;

public static class DatabaseScheduleLoader
{
    private const string MachinePoolType = "__MACHINE_POOL__";

    public static bool TryLoadFromEnvironment(
        DateTime asOf,
        IReadOnlyCollection<string>? processGroupFilters,
        out List<Machine> machines,
        out List<WorkOrder> orders,
        out string reason)
    {
        machines = [];
        orders = [];

        var conn = ReadEnv("PROD_SCHEDULER_CONN", SchedulerConfig.DefaultConnectionString);
        var workTable = ReadEnv("PROD_SCHEDULER_WORK_TABLE", SchedulerConfig.DefaultWorkTable);

        var outputSortCol = ReadEnv("PROD_SCHEDULER_OUTPUT_SORT_COL", SchedulerConfig.OutputSortColumn);
        var cardCol = ReadEnv("PROD_SCHEDULER_CARD_COL", SchedulerConfig.CardColumn);
        var partNoCol = ReadEnv("PROD_SCHEDULER_PARTNO_COL", SchedulerConfig.PartNoColumn);
        var dueCol = ReadEnv("PROD_SCHEDULER_DUE_COL", SchedulerConfig.DueDateColumn);
        var processCol = ReadEnv("PROD_SCHEDULER_PROCESS_COL", SchedulerConfig.ProcessColumn);
        var processGroupCol = ReadEnv("PROD_SCHEDULER_PROCESS_GROUP_COL", SchedulerConfig.ProcessGroupColumn);
        var machineCol = ReadEnv("PROD_SCHEDULER_MACHINE_COL", SchedulerConfig.MachineColumn);
        var workMinutesCol = ReadEnv("PROD_SCHEDULER_WORK_MINUTES_COL", SchedulerConfig.WorkMinutesColumn);
        var qtyCol = ReadEnv("PROD_SCHEDULER_QTY_COL", SchedulerConfig.QuantityColumn);
        var seqCol = ReadEnv("PROD_SCHEDULER_SEQ_COL", SchedulerConfig.SequenceColumn);

        try
        {
            using var connection = CreateConnection(conn);
            connection.Open();

            var rows = ReadRows(connection, $"SELECT * FROM {Qid(workTable)}");
            if (rows.Count == 0)
            {
                reason = $"資料表 {workTable} 沒有資料。";
                return false;
            }

            var machineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sortedRows = rows
                .OrderBy(r => ReadInt(r, outputSortCol))
                .ThenBy(r => ReadInt(r, seqCol))
                .ThenBy(r => ReadString(r, cardCol), StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var row in sortedRows)
            {
                var outputSort = ReadInt(row, outputSortCol);
                var card = ReadString(row, cardCol);        // 製卡
                var partNo = ReadString(row, partNoCol);     // 圖號
                var process = ReadString(row, processCol);    // 製程
                var processGroup = ReadString(row, processGroupCol); // PRDOPGP
                var sequence = ReadInt(row, seqCol);          // 製程排序
                var dueAt = ReadDateTime(row, dueCol);

                if (processGroupFilters is not null && processGroupFilters.Count > 0)
                {
                    var hit = processGroupFilters.Contains(processGroup, StringComparer.OrdinalIgnoreCase);
                    if (!hit)
                        continue;
                }

                var workMinutes = ReadDouble(row, workMinutesCol);
                var qty = ReadDouble(row, qtyCol);
                var processHours = Math.Max(0.0, workMinutes / 60.0);

                if (string.IsNullOrWhiteSpace(card) || string.IsNullOrWhiteSpace(process) || processHours <= 0)
                    continue;

                var machineCandidates = SplitMachines(ReadString(row, machineCol));
                if (machineCandidates.Count == 0)
                    machineCandidates.Add("UNASSIGNED");

                foreach (var machineId in machineCandidates)
                    machineIds.Add(machineId);

                orders.Add(new WorkOrder
                {
                    Id = $"{card}-{sequence}-{process}-{outputSort}",
                    ProcessCode = process,
                    MachineType = MachinePoolType,
                    ProcessHours = processHours,
                    Priority = 1,
                    SortOrder = outputSort <= 0 ? int.MaxValue : outputSort,
                    DueAt = dueAt,
                    ReleaseAt = null,
                    PreferredMachineIds = machineCandidates,
                    JobId = card,
                    Sequence = sequence <= 0 ? outputSort : sequence,
                    PartNo = partNo,
                    Quantity = qty
                });
            }

            foreach (var machineId in machineIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                machines.Add(new Machine
                {
                    Id = machineId,
                    MachineType = MachinePoolType,
                    AvailableAt = asOf
                });
            }

            if (orders.Count == 0 || machines.Count == 0)
            {
                var groupText = (processGroupFilters is null || processGroupFilters.Count == 0) ? "全部" : string.Join(",", processGroupFilters);
                reason = $"指派時間_TEMP 無可排程資料（PRDOPGP={groupText}）。";
                return false;
            }

            reason = "已使用指派時間_TEMP 單表模式載入。";
            return true;
        }
        catch (Exception ex)
        {
            reason = $"讀取指派時間_TEMP 失敗。原因：{ex.Message}";
            machines = [];
            orders = [];
            return false;
        }
    }


    public static bool TryLoadProcessGroups(out List<string> groups, out string reason)
    {
        groups = [];
        reason = string.Empty;

        var conn = ReadEnv("PROD_SCHEDULER_CONN", SchedulerConfig.DefaultConnectionString);
        var workTable = ReadEnv("PROD_SCHEDULER_WORK_TABLE", SchedulerConfig.DefaultWorkTable);
        var processGroupCol = ReadEnv("PROD_SCHEDULER_PROCESS_GROUP_COL", SchedulerConfig.ProcessGroupColumn);

        try
        {
            using var connection = CreateConnection(conn);
            connection.Open();

            var rows = ReadRows(connection, $"SELECT DISTINCT {Qid(processGroupCol)} AS 群組 FROM {Qid(workTable)}");
            groups = rows
                .Select(r => ReadString(r, "群組"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return groups.Count > 0;
        }
        catch (Exception ex)
        {
            reason = $"讀取 PRDOPGP 群組失敗：{ex.Message}";
            groups = [];
            return false;
        }
    }
    private static List<string> SplitMachines(string raw)
    {
        return raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DbConnection CreateConnection(string connectionString)
    {
        if (connectionString.Contains("Driver=", StringComparison.OrdinalIgnoreCase))
            return new OdbcConnection(connectionString);
        return new SqlConnection(connectionString);
    }

    private static string ReadEnv(string name, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static string Qid(string name) => $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static List<Dictionary<string, object?>> ReadRows(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        var rows = new List<Dictionary<string, object?>>();

        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        return rows;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? (value?.ToString() ?? string.Empty).Trim() : string.Empty;

    private static int ReadInt(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
            return 0;

        if (int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0;
    }

    private static double ReadDouble(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
            return 0;

        if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
            return invariant;
        if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var current))
            return current;

        return 0;
    }

    private static DateTime? ReadDateTime(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
            return null;

        if (value is DateTime dt)
            return dt;

        return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }
}





