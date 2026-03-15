CREATE OR ALTER PROCEDURE dbo.usp_AutoPc_LoadSchedulingContext
    @PlanDate date,
    @HorizonDays int = 7
AS
BEGIN
    SET NOCOUNT ON;

    IF @HorizonDays <= 0
    BEGIN
        SET @HorizonDays = 7;
    END;

    -- Result set 1: 機台基本資料
    DECLARE @machineObjectId int = OBJECT_ID(N'dbo.[機台基本資料]');
    IF @machineObjectId IS NULL
    BEGIN
        THROW 51001, N'找不到資料表 dbo.[機台基本資料]。', 1;
    END;

    DECLARE @machineIdColumn sysname =
    (
        SELECT TOP (1) c.name
        FROM sys.columns c
        WHERE c.object_id = @machineObjectId
          AND (
                c.name LIKE N'%機台%編%' OR
                c.name LIKE N'%機台%號%' OR
                c.name = N'MachineId' OR
                c.name LIKE N'%機台%'
              )
        ORDER BY
            CASE
                WHEN c.name LIKE N'%機台%編%' THEN 0
                WHEN c.name LIKE N'%機台%號%' THEN 1
                WHEN c.name = N'MachineId' THEN 2
                ELSE 3
            END,
            c.column_id
    );

    DECLARE @machineGroupColumn sysname =
    (
        SELECT TOP (1) c.name
        FROM sys.columns c
        WHERE c.object_id = @machineObjectId
          AND (c.name LIKE N'%組別%' OR c.name LIKE N'%群組%' OR c.name = N'MachineGroup')
        ORDER BY c.column_id
    );

    DECLARE @processCodeColumn sysname =
    (
        SELECT TOP (1) c.name
        FROM sys.columns c
        WHERE c.object_id = @machineObjectId
          AND (c.name LIKE N'%製程%代%' OR c.name = N'ProcessCode' OR c.name = N'ORDFO')
        ORDER BY c.column_id
    );

    DECLARE @dailyHoursColumn sysname =
    (
        SELECT TOP (1) c.name
        FROM sys.columns c
        JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = @machineObjectId
          AND (
                c.name LIKE N'%工時%' OR
                c.name = N'DailyHours' OR
                c.name = N'WKTIME'
              )
          AND t.name IN (N'int', N'bigint', N'smallint', N'tinyint', N'numeric', N'decimal', N'float', N'real')
        ORDER BY c.column_id
    );

    IF @machineIdColumn IS NULL
    BEGIN
        THROW 51002, N'機台基本資料找不到機台欄位。', 1;
    END;

    IF @dailyHoursColumn IS NULL
    BEGIN
        THROW 51003, N'機台基本資料找不到每日工時欄位。', 1;
    END;

    DECLARE @sqlMachines nvarchar(max) = N'
        SELECT
            CAST(m.' + QUOTENAME(@machineIdColumn) + N' AS varchar(50)) AS MachineId,
            ' + CASE
                    WHEN @machineGroupColumn IS NULL THEN N'CAST(NULL AS varchar(50))'
                    ELSE N'CAST(m.' + QUOTENAME(@machineGroupColumn) + N' AS varchar(50))'
                END + N' AS MachineGroup,
            ' + CASE
                    WHEN @processCodeColumn IS NULL THEN N'CAST(NULL AS varchar(50))'
                    ELSE N'CAST(m.' + QUOTENAME(@processCodeColumn) + N' AS varchar(50))'
                END + N' AS ProcessCode,
            TRY_CONVERT(decimal(18,4), m.' + QUOTENAME(@dailyHoursColumn) + N') AS DailyHours
        FROM dbo.[機台基本資料] m
        WHERE TRY_CONVERT(decimal(18,4), m.' + QUOTENAME(@dailyHoursColumn) + N') > 0;
    ';

    EXEC sys.sp_executesql @sqlMachines;

    -- Result set 2: WORKFIXM 定品定機
    SELECT
        CAST(w.INDWG AS varchar(50)) AS PartNo,
        CAST(w.PRDNAME AS nvarchar(50)) AS ProductName,
        CAST(w.MTYPE AS varchar(50)) AS ProcessCode,
        CAST(w.MAHNO AS varchar(50)) AS MachineId,
        CAST(w.MAHNO_GP AS varchar(50)) AS MachineGroup
    FROM dbo.WORKFIXM w
    WHERE NULLIF(CAST(w.MAHNO AS varchar(50)), '') IS NOT NULL;

    -- Result set 3: 可排程工作（含 ORDDE4 優先序）
    DECLARE @ordde4ObjectId int = OBJECT_ID(N'dbo.[ORDDE4_剩餘製程明細]');
    DECLARE @priorityColumn sysname = NULL;
    DECLARE @priorityExpr nvarchar(max) = N'CAST(NULL AS decimal(18,4))';
    DECLARE @joinOrdde4 nvarchar(max) = N'';

    IF @ordde4ObjectId IS NOT NULL
    BEGIN
        SELECT TOP (1) @priorityColumn = c.name
        FROM sys.columns c
        JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = @ordde4ObjectId
          AND c.name LIKE N'%可用%工時%'
          AND t.name IN (N'int', N'bigint', N'smallint', N'tinyint', N'numeric', N'decimal', N'float', N'real', N'varchar', N'nvarchar')
        ORDER BY
            CASE WHEN c.name = N'可用工時' THEN 0 ELSE 1 END,
            c.column_id;

        SET @joinOrdde4 = N'
            LEFT JOIN dbo.[ORDDE4_剩餘製程明細] r
                ON r.ORDTP = w.ORDTP
               AND r.ORDNO = w.ORDNO
               AND r.ORDSQ = w.ORDSQ
               AND r.ORDSQ1 = w.ORDSQ1
        ';

        IF @priorityColumn IS NOT NULL
        BEGIN
            SET @priorityExpr = N'TRY_CONVERT(decimal(18,4), r.' + QUOTENAME(@priorityColumn) + N')';
        END;
    END;

    DECLARE @sqlWorks nvarchar(max) = N'
        SELECT
            CAST(w.ORDTP AS varchar(1)) AS OrdTp,
            CAST(w.ORDNO AS varchar(20)) AS OrdNo,
            TRY_CONVERT(int, w.ORDSQ) AS OrdSq,
            TRY_CONVERT(int, w.ORDSQ1) AS OrdSq1,
            TRY_CONVERT(int, w.ORDSQ2) AS OrdSq2,
            CAST(w.INPART AS varchar(50)) AS PartNo,
            CAST(w.PRDNAME AS nvarchar(50)) AS ProductName,
            CAST(w.ORDFO AS varchar(50)) AS ProcessCode,
            TRY_CONVERT(decimal(18,4), w.WKTIME) AS RequiredHours,
            TRY_CONVERT(datetime2(0), COALESCE(w.PDATE, w.ORDDY)) AS DueDate,
            ' + @priorityExpr + N' AS PriorityAvailableHours
        FROM dbo.[可排程工作] w
        ' + @joinOrdde4 + N'
        WHERE TRY_CONVERT(decimal(18,4), w.WKTIME) > 0;
    ';

    EXEC sys.sp_executesql @sqlWorks;

    -- Result set 4: 指派時間（既有排程）
    DECLARE @assignmentObjectId int = OBJECT_ID(N'dbo.[指派時間]');
    IF @assignmentObjectId IS NULL
    BEGIN
        SELECT
            CAST(NULL AS varchar(50)) AS MachineId,
            CAST(NULL AS datetime2(0)) AS StartTime,
            CAST(NULL AS datetime2(0)) AS EndTime
        WHERE 1 = 0;

        RETURN;
    END;

    DECLARE @assignmentMachineColumn sysname =
    (
        SELECT TOP (1) c.name
        FROM sys.columns c
        WHERE c.object_id = @assignmentObjectId
          AND (
                c.name LIKE N'%機台%' OR
                c.name = N'MachineId' OR
                c.name = N'MAHNO'
              )
        ORDER BY
            CASE
                WHEN c.name LIKE N'%機台%' THEN 0
                WHEN c.name = N'MachineId' THEN 1
                ELSE 2
            END,
            c.column_id
    );

    DECLARE @startTimeColumn sysname =
    (
        SELECT TOP (1) c.name
        FROM sys.columns c
        WHERE c.object_id = @assignmentObjectId
          AND (c.name = N'StartTime' OR c.name = N'SETTIME' OR c.name LIKE N'%Start%')
        ORDER BY c.column_id
    );

    DECLARE @endTimeColumn sysname =
    (
        SELECT TOP (1) c.name
        FROM sys.columns c
        WHERE c.object_id = @assignmentObjectId
          AND (c.name = N'EndTime' OR c.name = N'OUTTIME' OR c.name LIKE N'%End%')
        ORDER BY c.column_id
    );

    IF @assignmentMachineColumn IS NULL OR @startTimeColumn IS NULL OR @endTimeColumn IS NULL
    BEGIN
        SELECT
            CAST(NULL AS varchar(50)) AS MachineId,
            CAST(NULL AS datetime2(0)) AS StartTime,
            CAST(NULL AS datetime2(0)) AS EndTime
        WHERE 1 = 0;

        RETURN;
    END;

    DECLARE @sqlExisting nvarchar(max) = N'
        SELECT
            CAST(a.' + QUOTENAME(@assignmentMachineColumn) + N' AS varchar(50)) AS MachineId,
            TRY_CONVERT(datetime2(0), a.' + QUOTENAME(@startTimeColumn) + N') AS StartTime,
            TRY_CONVERT(datetime2(0), a.' + QUOTENAME(@endTimeColumn) + N') AS EndTime
        FROM dbo.[指派時間] a
        WHERE TRY_CONVERT(datetime2(0), a.' + QUOTENAME(@startTimeColumn) + N') >= @PlanDate
          AND TRY_CONVERT(datetime2(0), a.' + QUOTENAME(@startTimeColumn) + N') < DATEADD(day, @HorizonDays + 60, @PlanDate)
          AND TRY_CONVERT(datetime2(0), a.' + QUOTENAME(@endTimeColumn) + N') > TRY_CONVERT(datetime2(0), a.' + QUOTENAME(@startTimeColumn) + N');
    ';

    EXEC sys.sp_executesql
        @sqlExisting,
        N'@PlanDate date, @HorizonDays int',
        @PlanDate = @PlanDate,
        @HorizonDays = @HorizonDays;
END;
