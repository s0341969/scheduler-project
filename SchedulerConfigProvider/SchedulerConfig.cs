namespace SchedulerConfigProvider;

public static class SchedulerConfig
{
    public static string DefaultConnectionString =>
        "Server=10.1.1.76;Database=TEST;User ID=sa;Password=GONGIN;TrustServerCertificate=True;Encrypt=False";

    public static string DefaultWorkTable => "指派時間_TEMP";

    // 指派時間_TEMP 欄位映射
    public static string OutputSortColumn => "輸出排序";
    public static string CardColumn => "INPART";       // 製卡
    public static string PartNoColumn => "INDWG";      // 圖號
    public static string DueDateColumn => "PDATE0";    // 交期
    public static string ProcessColumn => "PRDNAME";   // 製程
    public static string ProcessGroupColumn => "PRDOPGP"; // 製程群組
    public static string MachineColumn => "Applier";   // 機台（可 ; 分隔）
    public static string WorkMinutesColumn => "WKTIME"; // 工時分鐘
    public static string QuantityColumn => "QTY";      // 數量
    public static string SequenceColumn => "新ORDSQ2";  // 製程排序

    public static int OptimizationIterations => 20;
}


