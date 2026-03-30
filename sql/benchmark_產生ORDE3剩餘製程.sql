SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
  檔名：benchmark_產生ORDE3剩餘製程.sql
  目的：量測 dbo.產生ORDE3剩餘製程 在不同 INPART 情境下的耗時與資源使用
  量測指標：
    - 每次 wall-clock duration (ms)
    - dm_exec_procedure_stats 差值：CPU、elapsed、logical reads、physical reads、writes
    - 每情境彙總：AVG、P95、MIN、MAX

  注意事項：
    1) 建議在 TEST / 離峰執行，避免併發干擾。
    2) dm_exec_procedure_stats 為累積計數，腳本以「前後差值」估算單次成本。
    3) 若同時有其他 session 執行同一 SP，差值會被污染。
*/

USE [TEST];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ProcSchema SYSNAME = N'dbo';
DECLARE @ProcName SYSNAME = N'產生ORDE3剩餘製程';
DECLARE @QualifiedProc NVARCHAR(300) = QUOTENAME(@ProcSchema) + N'.' + QUOTENAME(@ProcName);
DECLARE @ProcObjectId INT = OBJECT_ID(@QualifiedProc, N'P');

IF @ProcObjectId IS NULL
BEGIN
    THROW 50001, N'找不到目標預存程序 dbo.產生ORDE3剩餘製程。', 1;
END;

/* 測試情境：可依需求調整 */
DECLARE @Scenarios TABLE (
    ScenarioNo INT IDENTITY(1,1) PRIMARY KEY,
    ScenarioName NVARCHAR(100) NOT NULL,
    InpartPattern VARCHAR(40) NOT NULL,
    Runs INT NOT NULL
);

INSERT INTO @Scenarios (ScenarioName, InpartPattern, Runs)
VALUES
(N'單一製卡（建議替換為實務常用）', '24X01008MT-0%', 3),
(N'特定前綴批次', '23G%', 3),
(N'全量（高負載）', '%', 3);

/* 每次執行明細 */
CREATE TABLE #RunMetrics (
    RunId INT IDENTITY(1,1) PRIMARY KEY,
    ScenarioName NVARCHAR(100) NOT NULL,
    InpartPattern VARCHAR(40) NOT NULL,
    RunNo INT NOT NULL,
    StartAt DATETIME2(3) NOT NULL,
    EndAt DATETIME2(3) NOT NULL,
    WallClockMs BIGINT NOT NULL,
    DeltaExecCount BIGINT NOT NULL,
    DeltaCpuUs BIGINT NOT NULL,
    DeltaElapsedUs BIGINT NOT NULL,
    DeltaLogicalReads BIGINT NOT NULL,
    DeltaPhysicalReads BIGINT NOT NULL,
    DeltaWrites BIGINT NOT NULL,
    CpuMsPerExec DECIMAL(18,2) NULL,
    ElapsedMsPerExec DECIMAL(18,2) NULL,
    LogicalReadsPerExec DECIMAL(18,2) NULL
);

DECLARE @ScenarioNo INT = 1;
DECLARE @ScenarioMax INT = (SELECT MAX(ScenarioNo) FROM @Scenarios);

WHILE @ScenarioNo <= @ScenarioMax
BEGIN
    DECLARE @ScenarioName NVARCHAR(100);
    DECLARE @InpartPattern VARCHAR(40);
    DECLARE @Runs INT;

    SELECT
        @ScenarioName = ScenarioName,
        @InpartPattern = InpartPattern,
        @Runs = Runs
    FROM @Scenarios
    WHERE ScenarioNo = @ScenarioNo;

    DECLARE @i INT = 1;
    WHILE @i <= @Runs
    BEGIN
        DECLARE @BeforeExecCount BIGINT = 0;
        DECLARE @BeforeCpuUs BIGINT = 0;
        DECLARE @BeforeElapsedUs BIGINT = 0;
        DECLARE @BeforeLogicalReads BIGINT = 0;
        DECLARE @BeforePhysicalReads BIGINT = 0;
        DECLARE @BeforeWrites BIGINT = 0;

        DECLARE @AfterExecCount BIGINT = 0;
        DECLARE @AfterCpuUs BIGINT = 0;
        DECLARE @AfterElapsedUs BIGINT = 0;
        DECLARE @AfterLogicalReads BIGINT = 0;
        DECLARE @AfterPhysicalReads BIGINT = 0;
        DECLARE @AfterWrites BIGINT = 0;

        DECLARE @StartAt DATETIME2(3) = SYSDATETIME();
        DECLARE @EndAt DATETIME2(3);

        SELECT
            @BeforeExecCount = ISNULL(execution_count, 0),
            @BeforeCpuUs = ISNULL(total_worker_time, 0),
            @BeforeElapsedUs = ISNULL(total_elapsed_time, 0),
            @BeforeLogicalReads = ISNULL(total_logical_reads, 0),
            @BeforePhysicalReads = ISNULL(total_physical_reads, 0),
            @BeforeWrites = ISNULL(total_logical_writes, 0)
        FROM sys.dm_exec_procedure_stats
        WHERE database_id = DB_ID()
          AND object_id = @ProcObjectId;

        EXEC dbo.產生ORDE3剩餘製程 @INPART = @InpartPattern;

        SET @EndAt = SYSDATETIME();

        SELECT
            @AfterExecCount = ISNULL(execution_count, 0),
            @AfterCpuUs = ISNULL(total_worker_time, 0),
            @AfterElapsedUs = ISNULL(total_elapsed_time, 0),
            @AfterLogicalReads = ISNULL(total_logical_reads, 0),
            @AfterPhysicalReads = ISNULL(total_physical_reads, 0),
            @AfterWrites = ISNULL(total_logical_writes, 0)
        FROM sys.dm_exec_procedure_stats
        WHERE database_id = DB_ID()
          AND object_id = @ProcObjectId;

        DECLARE @DeltaExecCount BIGINT = @AfterExecCount - @BeforeExecCount;
        DECLARE @DeltaCpuUs BIGINT = @AfterCpuUs - @BeforeCpuUs;
        DECLARE @DeltaElapsedUs BIGINT = @AfterElapsedUs - @BeforeElapsedUs;
        DECLARE @DeltaLogicalReads BIGINT = @AfterLogicalReads - @BeforeLogicalReads;
        DECLARE @DeltaPhysicalReads BIGINT = @AfterPhysicalReads - @BeforePhysicalReads;
        DECLARE @DeltaWrites BIGINT = @AfterWrites - @BeforeWrites;

        INSERT INTO #RunMetrics (
            ScenarioName, InpartPattern, RunNo,
            StartAt, EndAt, WallClockMs,
            DeltaExecCount, DeltaCpuUs, DeltaElapsedUs,
            DeltaLogicalReads, DeltaPhysicalReads, DeltaWrites,
            CpuMsPerExec, ElapsedMsPerExec, LogicalReadsPerExec
        )
        VALUES (
            @ScenarioName, @InpartPattern, @i,
            @StartAt, @EndAt, DATEDIFF(MILLISECOND, @StartAt, @EndAt),
            @DeltaExecCount, @DeltaCpuUs, @DeltaElapsedUs,
            @DeltaLogicalReads, @DeltaPhysicalReads, @DeltaWrites,
            CASE WHEN @DeltaExecCount > 0 THEN CAST((@DeltaCpuUs / 1000.0) / @DeltaExecCount AS DECIMAL(18,2)) END,
            CASE WHEN @DeltaExecCount > 0 THEN CAST((@DeltaElapsedUs / 1000.0) / @DeltaExecCount AS DECIMAL(18,2)) END,
            CASE WHEN @DeltaExecCount > 0 THEN CAST((@DeltaLogicalReads * 1.0) / @DeltaExecCount AS DECIMAL(18,2)) END
        );

        SET @i += 1;
    END;

    SET @ScenarioNo += 1;
END;

/* 明細（每次執行） */
SELECT
    ScenarioName,
    InpartPattern,
    RunNo,
    StartAt,
    EndAt,
    WallClockMs,
    DeltaExecCount,
    DeltaCpuUs,
    DeltaElapsedUs,
    DeltaLogicalReads,
    DeltaPhysicalReads,
    DeltaWrites,
    CpuMsPerExec,
    ElapsedMsPerExec,
    LogicalReadsPerExec
FROM #RunMetrics
ORDER BY ScenarioName, RunNo;

/* 彙總（AVG / P95 / MIN / MAX） */
;WITH S AS (
    SELECT
        ScenarioName,
        InpartPattern,
        WallClockMs,
        CpuMsPerExec,
        LogicalReadsPerExec,
        P95WallClockMs = PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY WallClockMs) OVER (PARTITION BY ScenarioName, InpartPattern),
        P95CpuMsPerExec = PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY CpuMsPerExec) OVER (PARTITION BY ScenarioName, InpartPattern),
        P95LogicalReadsPerExec = PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY LogicalReadsPerExec) OVER (PARTITION BY ScenarioName, InpartPattern)
    FROM #RunMetrics
)
SELECT DISTINCT
    ScenarioName,
    InpartPattern,
    Runs = COUNT(*) OVER (PARTITION BY ScenarioName, InpartPattern),
    AvgWallClockMs = CAST(AVG(WallClockMs * 1.0) OVER (PARTITION BY ScenarioName, InpartPattern) AS DECIMAL(18,2)),
    P95WallClockMs = CAST(P95WallClockMs AS DECIMAL(18,2)),
    MinWallClockMs = MIN(WallClockMs) OVER (PARTITION BY ScenarioName, InpartPattern),
    MaxWallClockMs = MAX(WallClockMs) OVER (PARTITION BY ScenarioName, InpartPattern),
    AvgCpuMsPerExec = CAST(AVG(CpuMsPerExec * 1.0) OVER (PARTITION BY ScenarioName, InpartPattern) AS DECIMAL(18,2)),
    P95CpuMsPerExec = CAST(P95CpuMsPerExec AS DECIMAL(18,2)),
    AvgLogicalReadsPerExec = CAST(AVG(LogicalReadsPerExec * 1.0) OVER (PARTITION BY ScenarioName, InpartPattern) AS DECIMAL(18,2)),
    P95LogicalReadsPerExec = CAST(P95LogicalReadsPerExec AS DECIMAL(18,2))
FROM S
ORDER BY ScenarioName;

/* 執行環境標記 */
SELECT
    DatabaseName = DB_NAME(),
    ProcName = @QualifiedProc,
    MeasuredAt = SYSDATETIME(),
    HostName = HOST_NAME(),
    LoginName = SUSER_SNAME();
GO
