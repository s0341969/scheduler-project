using AutoPcScheduler.Services;

var options = CliOptions.Parse(args);
var connectionString = Environment.GetEnvironmentVariable("AUTO_PC_CONN");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("缺少連線字串，請先設定環境變數 AUTO_PC_CONN。");
    return 2;
}

var repository = new SqlSchedulingRepository(connectionString);
var scheduler = new AutoPcSchedulerEngine();

Console.WriteLine($"開始排程，排程起日: {options.PlanDate:yyyy-MM-dd}，視窗天數: {options.HorizonDays}");

var context = await repository.LoadSchedulingContextAsync(options.PlanDate, options.HorizonDays, CancellationToken.None);

Console.WriteLine($"讀取完成：機台 {context.Machines.Count} 台、定品定機 {context.Routes.Count} 筆、可排程工作 {context.Works.Count} 筆、既有指派 {context.ExistingAssignments.Count} 筆");

var result = scheduler.Schedule(context, options.PlanDate, options.HorizonDays, options.Assigner);

Console.WriteLine($"排程完成：新排程 {result.Assignments.Count} 筆，未排入 {result.Unscheduled.Count} 筆");

if (options.DryRun)
{
    Console.WriteLine("Dry run 模式，不寫入資料庫。以下顯示前 20 筆排程結果：");
    foreach (var assignment in result.Assignments.Take(20))
    {
        Console.WriteLine($"{assignment.MachineId} | {assignment.StartTime:yyyy-MM-dd HH:mm} -> {assignment.EndTime:yyyy-MM-dd HH:mm} | {assignment.OrdTp}-{assignment.OrdNo}-{assignment.OrdSq}-{assignment.OrdSq1}");
    }

    if (result.Unscheduled.Count > 0)
    {
        Console.WriteLine("未排入工作：");
        foreach (var item in result.Unscheduled.Take(20))
        {
            Console.WriteLine($"{item.Work.OrdTp}-{item.Work.OrdNo}-{item.Work.OrdSq}-{item.Work.OrdSq1} | 原因: {item.Reason}");
        }
    }

    return 0;
}

var insertedRows = await repository.SaveAssignmentsAsync(result.Assignments, CancellationToken.None);
Console.WriteLine($"已寫入指派時間 {insertedRows} 筆。\n");

if (result.Unscheduled.Count > 0)
{
    Console.WriteLine("仍有未排入工作（最多顯示 20 筆）：");
    foreach (var item in result.Unscheduled.Take(20))
    {
        Console.WriteLine($"{item.Work.OrdTp}-{item.Work.OrdNo}-{item.Work.OrdSq}-{item.Work.OrdSq1} | 原因: {item.Reason}");
    }
}

return 0;

internal sealed record CliOptions(DateOnly PlanDate, int HorizonDays, bool DryRun, string Assigner)
{
    public static CliOptions Parse(string[] args)
    {
        var planDate = DateOnly.FromDateTime(DateTime.Today);
        var horizonDays = 7;
        var dryRun = false;
        var assigner = string.IsNullOrWhiteSpace(Environment.UserName) ? "AutoPc" : Environment.UserName;

        foreach (var arg in args)
        {
            if (arg.StartsWith("--plan-date=", StringComparison.OrdinalIgnoreCase))
            {
                var value = arg.Split('=', 2)[1];
                if (DateOnly.TryParse(value, out var parsed))
                {
                    planDate = parsed;
                }
            }
            else if (arg.StartsWith("--horizon-days=", StringComparison.OrdinalIgnoreCase))
            {
                var value = arg.Split('=', 2)[1];
                if (int.TryParse(value, out var parsedDays) && parsedDays > 0)
                {
                    horizonDays = parsedDays;
                }
            }
            else if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                dryRun = true;
            }
            else if (arg.StartsWith("--assigner=", StringComparison.OrdinalIgnoreCase))
            {
                var value = arg.Split('=', 2)[1].Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    assigner = value;
                }
            }
        }

        return new CliOptions(planDate, horizonDays, dryRun, assigner);
    }
}
