CREATE OR ALTER PROCEDURE dbo.usp_AutoPc_SaveAssignments
    @Assignments dbo.AutoPcAssignmentTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM @Assignments)
    BEGIN
        SELECT CAST(0 AS int) AS InsertedRows;
        RETURN;
    END;

    DECLARE @assignmentObjectId int = OBJECT_ID(N'dbo.[指派時間]');
    IF @assignmentObjectId IS NULL
    BEGIN
        THROW 51011, N'找不到資料表 dbo.[指派時間]。', 1;
    END;

    DECLARE @machineColumn sysname =
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

    DECLARE @insertColumns nvarchar(max) = N'';
    DECLARE @selectColumns nvarchar(max) = N'';

    IF @machineColumn IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + QUOTENAME(@machineColumn);
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.MachineId';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'Applier') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[Applier]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'''AutoPc''';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'StartTime') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[StartTime]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.StartTime';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'EndTime') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[EndTime]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.EndTime';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'WKTIME') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[WKTIME]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.WorkHours';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'Content') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[Content]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.Content';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'Assigner') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[Assigner]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.Assigner';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'Remark') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[Remark]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.Remark';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'SetUpTime') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[SetUpTime]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.CreatedAt';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'WKType') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[WKType]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'1';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'INPART') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[INPART]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.InPart';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'ORDFO') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[ORDFO]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.ProcessCode';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'PRDNAME') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[PRDNAME]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.ProductName';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'PDATE') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[PDATE]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'CONVERT(datetime2(0), src.StartTime)';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'ORDTP') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[ORDTP]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.OrdTp';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'ORDNO') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[ORDNO]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.OrdNo';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'ORDSQ') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[ORDSQ]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.OrdSq';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'ORDSQ1') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[ORDSQ1]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.OrdSq1';
    END;

    IF COL_LENGTH(N'dbo.[指派時間]', N'ORDSQ2') IS NOT NULL
    BEGIN
        SET @insertColumns += CASE WHEN LEN(@insertColumns) = 0 THEN N'' ELSE N', ' END + N'[ORDSQ2]';
        SET @selectColumns += CASE WHEN LEN(@selectColumns) = 0 THEN N'' ELSE N', ' END + N'src.OrdSq2';
    END;

    IF LEN(@insertColumns) = 0 OR LEN(@selectColumns) = 0
    BEGIN
        THROW 51012, N'dbo.[指派時間] 與 TVP 欄位無法對應。請調整 usp_AutoPc_SaveAssignments。', 1;
    END;

    DECLARE @sql nvarchar(max) = N'
        INSERT INTO dbo.[指派時間] (' + @insertColumns + N')
        SELECT ' + @selectColumns + N'
        FROM @Assignments src;

        SELECT @InsertedRows_OUT = @@ROWCOUNT;
    ';

    DECLARE @insertedRows int = 0;
    EXEC sys.sp_executesql
        @sql,
        N'@Assignments dbo.AutoPcAssignmentTvp READONLY, @InsertedRows_OUT int OUTPUT',
        @Assignments = @Assignments,
        @InsertedRows_OUT = @insertedRows OUTPUT;

    SELECT @insertedRows AS InsertedRows;
END;
