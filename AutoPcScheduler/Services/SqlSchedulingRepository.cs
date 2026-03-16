using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace AutoPcScheduler.Services;

public sealed class SqlSchedulingRepository : ISchedulingRepository
{
    private readonly string _connectionString;

    public SqlSchedulingRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SchedulingContext> LoadSchedulingContextAsync(DateOnly planDate, int horizonDays, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.usp_AutoPc_LoadSchedulingContext", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.Add(new SqlParameter("@PlanDate", SqlDbType.Date) { Value = planDate.ToDateTime(TimeOnly.MinValue) });
        command.Parameters.Add(new SqlParameter("@HorizonDays", SqlDbType.Int) { Value = horizonDays });

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rawMachines = await ReadMachinesAsync(reader, cancellationToken);
        var useMinuteUnit = ShouldUseMinuteUnit(rawMachines);
        var machines = NormalizeMachineCapacities(rawMachines, useMinuteUnit);

        await EnsureNextResultAsync(reader, "定品定機結果集", cancellationToken);
        var routes = await ReadRoutesAsync(reader, cancellationToken);

        await EnsureNextResultAsync(reader, "可排程工作結果集", cancellationToken);
        var works = await ReadWorksAsync(reader, cancellationToken, useMinuteUnit);

        await EnsureNextResultAsync(reader, "既有指派結果集", cancellationToken);
        var existingAssignments = await ReadExistingAssignmentsAsync(reader, cancellationToken);

        return new SchedulingContext(machines, routes, works, existingAssignments);
    }

    public async Task<int> SaveAssignmentsAsync(IReadOnlyList<PlannedAssignment> assignments, CancellationToken cancellationToken)
    {
        if (assignments.Count == 0)
        {
            return 0;
        }

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.usp_AutoPc_SaveAssignments", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        var tvp = BuildAssignmentsDataTable(assignments);
        var tvpParameter = command.Parameters.Add("@Assignments", SqlDbType.Structured);
        tvpParameter.TypeName = "dbo.AutoPcAssignmentTvp";
        tvpParameter.Value = tvp;

        await connection.OpenAsync(cancellationToken);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);

        return ToInt32(scalar, assignments.Count);
    }

    private static async Task<List<MachineCapacity>> ReadMachinesAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var machineIdIndex = GetRequiredOrdinal(reader, "MachineId");
        var machineGroupIndex = GetOptionalOrdinal(reader, "MachineGroup");
        var processCodeIndex = GetOptionalOrdinal(reader, "ProcessCode");
        var dailyHoursIndex = GetRequiredOrdinal(reader, "DailyHours");

        var rows = new List<MachineCapacity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var machineId = ReadString(reader, machineIdIndex);
            if (string.IsNullOrWhiteSpace(machineId))
            {
                continue;
            }

            var dailyHours = ReadDecimal(reader, dailyHoursIndex);
            if (dailyHours <= 0)
            {
                continue;
            }

            rows.Add(new MachineCapacity(
                machineId,
                ReadStringOrNull(reader, machineGroupIndex),
                ReadStringOrNull(reader, processCodeIndex),
                dailyHours));
        }

        return rows;
    }

    private static async Task<List<WorkMachineRoute>> ReadRoutesAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var partNoIndex = GetOptionalOrdinal(reader, "PartNo");
        var productNameIndex = GetOptionalOrdinal(reader, "ProductName");
        var processCodeIndex = GetOptionalOrdinal(reader, "ProcessCode");
        var machineIdIndex = GetOptionalOrdinal(reader, "MachineId");
        var machineGroupIndex = GetOptionalOrdinal(reader, "MachineGroup");

        var rows = new List<WorkMachineRoute>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new WorkMachineRoute(
                ReadStringOrNull(reader, partNoIndex),
                ReadStringOrNull(reader, productNameIndex),
                ReadStringOrNull(reader, processCodeIndex),
                ReadStringOrNull(reader, machineIdIndex),
                ReadStringOrNull(reader, machineGroupIndex)));
        }

        return rows;
    }

    private static async Task<List<SchedulableWork>> ReadWorksAsync(SqlDataReader reader, CancellationToken cancellationToken, bool useMinuteUnit)
    {
        var ordTpIndex = GetRequiredOrdinal(reader, "OrdTp");
        var ordNoIndex = GetRequiredOrdinal(reader, "OrdNo");
        var ordSqIndex = GetRequiredOrdinal(reader, "OrdSq");
        var ordSq1Index = GetRequiredOrdinal(reader, "OrdSq1");
        var ordSq2Index = GetOptionalOrdinal(reader, "OrdSq2");
        var partNoIndex = GetOptionalOrdinal(reader, "PartNo");
        var productNameIndex = GetOptionalOrdinal(reader, "ProductName");
        var processCodeIndex = GetOptionalOrdinal(reader, "ProcessCode");
        var requiredHoursIndex = GetRequiredOrdinal(reader, "RequiredHours");
        var dueDateIndex = GetOptionalOrdinal(reader, "DueDate");
        var priorityHoursIndex = GetOptionalOrdinal(reader, "PriorityAvailableHours");

        var rows = new List<SchedulableWork>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var requiredHours = NormalizeWorkHours(ReadDecimal(reader, requiredHoursIndex), useMinuteUnit);
            if (requiredHours <= 0)
            {
                continue;
            }

            rows.Add(new SchedulableWork(
                ReadString(reader, ordTpIndex),
                ReadString(reader, ordNoIndex),
                ReadInt32(reader, ordSqIndex),
                ReadInt32(reader, ordSq1Index),
                ReadInt32OrNull(reader, ordSq2Index),
                ReadStringOrNull(reader, partNoIndex),
                ReadStringOrNull(reader, productNameIndex),
                ReadStringOrNull(reader, processCodeIndex),
                requiredHours,
                ReadDateTimeOrNull(reader, dueDateIndex),
                NormalizeWorkHours(ReadDecimalOrNull(reader, priorityHoursIndex), useMinuteUnit)));
        }

        return rows;
    }

    private static async Task<List<ExistingAssignment>> ReadExistingAssignmentsAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var machineIdIndex = GetRequiredOrdinal(reader, "MachineId");
        var startTimeIndex = GetRequiredOrdinal(reader, "StartTime");
        var endTimeIndex = GetRequiredOrdinal(reader, "EndTime");

        var rows = new List<ExistingAssignment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var machineId = ReadString(reader, machineIdIndex);
            var startTime = ReadDateTime(reader, startTimeIndex);
            var endTime = ReadDateTime(reader, endTimeIndex);

            if (string.IsNullOrWhiteSpace(machineId) || endTime < startTime)
            {
                continue;
            }

            rows.Add(new ExistingAssignment(machineId, startTime, endTime));
        }

        return rows;
    }

    private static bool ShouldUseMinuteUnit(IReadOnlyList<MachineCapacity> machines)
    {
        if (machines.Count == 0)
        {
            return false;
        }

        var averageDailyHours = machines.Average(x => x.DailyHours);
        var largeValueCount = machines.Count(x => x.DailyHours > 24m);
        return averageDailyHours > 24m || largeValueCount > machines.Count / 2;
    }

    private static List<MachineCapacity> NormalizeMachineCapacities(IReadOnlyList<MachineCapacity> machines, bool useMinuteUnit)
    {
        if (!useMinuteUnit)
        {
            return machines.ToList();
        }

        return machines
            .Select(x => x with { DailyHours = NormalizeMachineDailyHours(x.DailyHours) })
            .Where(x => x.DailyHours > 0)
            .ToList();
    }

    private static decimal NormalizeMachineDailyHours(decimal value)
    {
        if (value <= 0)
        {
            return 0m;
        }

        return value > 24m ? value / 60m : value;
    }

    private static decimal NormalizeWorkHours(decimal value, bool useMinuteUnit)
    {
        return useMinuteUnit ? value / 60m : value;
    }

    private static decimal? NormalizeWorkHours(decimal? value, bool useMinuteUnit)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return NormalizeWorkHours(value.Value, useMinuteUnit);
    }

    private static DataTable BuildAssignmentsDataTable(IReadOnlyList<PlannedAssignment> assignments)
    {
        var table = new DataTable();
        table.Columns.Add("OrdTp", typeof(string));
        table.Columns.Add("OrdNo", typeof(string));
        table.Columns.Add("OrdSq", typeof(int));
        table.Columns.Add("OrdSq1", typeof(int));
        table.Columns.Add("OrdSq2", typeof(int));
        table.Columns.Add("InPart", typeof(string));
        table.Columns.Add("ProcessCode", typeof(string));
        table.Columns.Add("ProductName", typeof(string));
        table.Columns.Add("MachineId", typeof(string));
        table.Columns.Add("StartTime", typeof(DateTime));
        table.Columns.Add("EndTime", typeof(DateTime));
        table.Columns.Add("WorkHours", typeof(decimal));
        table.Columns.Add("Assigner", typeof(string));
        table.Columns.Add("Content", typeof(string));
        table.Columns.Add("Remark", typeof(int));
        table.Columns.Add("CreatedAt", typeof(DateTime));

        var now = DateTime.Now;

        foreach (var assignment in assignments)
        {
            var row = table.NewRow();
            row["OrdTp"] = assignment.OrdTp;
            row["OrdNo"] = assignment.OrdNo;
            row["OrdSq"] = assignment.OrdSq;
            row["OrdSq1"] = assignment.OrdSq1;
            row["OrdSq2"] = assignment.OrdSq2.HasValue ? assignment.OrdSq2.Value : DBNull.Value;
            row["InPart"] = assignment.InPart ?? string.Empty;
            row["ProcessCode"] = assignment.ProcessCode ?? string.Empty;
            row["ProductName"] = assignment.ProductName ?? string.Empty;
            row["MachineId"] = assignment.MachineId;
            row["StartTime"] = assignment.StartTime;
            row["EndTime"] = assignment.EndTime;
            row["WorkHours"] = assignment.WorkHours;
            row["Assigner"] = assignment.Assigner;
            row["Content"] = assignment.Content;
            row["Remark"] = DBNull.Value;
            row["CreatedAt"] = now;
            table.Rows.Add(row);
        }

        return table;
    }

    private static async Task EnsureNextResultAsync(SqlDataReader reader, string name, CancellationToken cancellationToken)
    {
        if (!await reader.NextResultAsync(cancellationToken))
        {
            throw new InvalidOperationException($"資料載入 SP 缺少 {name}。請確認 dbo.usp_AutoPc_LoadSchedulingContext。");
        }
    }

    private static int GetRequiredOrdinal(SqlDataReader reader, string columnName)
    {
        try
        {
            return reader.GetOrdinal(columnName);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new InvalidOperationException($"SP 回傳缺少欄位 {columnName}。", ex);
        }
    }

    private static int? GetOptionalOrdinal(SqlDataReader reader, string columnName)
    {
        for (var index = 0; index < reader.FieldCount; index++)
        {
            if (string.Equals(reader.GetName(index), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return null;
    }

    private static string ReadString(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return string.Empty;
        }

        return Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static string? ReadStringOrNull(SqlDataReader reader, int? ordinal)
    {
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        var value = Convert.ToString(reader.GetValue(ordinal.Value), CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal ReadDecimal(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0m;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            decimal d => d,
            double d => Convert.ToDecimal(d, CultureInfo.InvariantCulture),
            float f => Convert.ToDecimal(f, CultureInfo.InvariantCulture),
            int i => i,
            long l => l,
            _ => decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m
        };
    }

    private static decimal? ReadDecimalOrNull(SqlDataReader reader, int? ordinal)
    {
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        return ReadDecimal(reader, ordinal.Value);
    }

    private static int ReadInt32(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            int i => i,
            short s => s,
            long l => Convert.ToInt32(l, CultureInfo.InvariantCulture),
            decimal d => Convert.ToInt32(d, CultureInfo.InvariantCulture),
            _ => int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0
        };
    }

    private static int? ReadInt32OrNull(SqlDataReader reader, int? ordinal)
    {
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        return ReadInt32(reader, ordinal.Value);
    }

    private static DateTime ReadDateTime(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return DateTime.MinValue;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTime dt => dt,
            _ => DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                ? parsed
                : DateTime.MinValue
        };
    }

    private static DateTime? ReadDateTimeOrNull(SqlDataReader reader, int? ordinal)
    {
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        var value = ReadDateTime(reader, ordinal.Value);
        return value == DateTime.MinValue ? null : value;
    }

    private static int ToInt32(object? value, int fallback)
    {
        if (value is null || value is DBNull)
        {
            return fallback;
        }

        return value switch
        {
            int i => i,
            long l => Convert.ToInt32(l, CultureInfo.InvariantCulture),
            decimal d => Convert.ToInt32(d, CultureInfo.InvariantCulture),
            _ => int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback
        };
    }
}
