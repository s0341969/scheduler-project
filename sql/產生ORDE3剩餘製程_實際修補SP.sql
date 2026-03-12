USE [TEST]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
  目的：直接修改既有 Stored Procedure 本體（dbo.產生ORDE3剩餘製程）
  方式：讀取現有定義 -> 套用 5 輪修正 -> 重新 ALTER PROCEDURE
*/

DECLARE @ProcName SYSNAME = N'dbo.產生ORDE3剩餘製程';
DECLARE @Sql NVARCHAR(MAX);
DECLARE @Old NVARCHAR(MAX);
DECLARE @New NVARCHAR(MAX);
DECLARE @Missing TABLE (StepNo INT, StepName NVARCHAR(200), MissingFragment NVARCHAR(200));

SET @Sql = OBJECT_DEFINITION(OBJECT_ID(@ProcName));
IF @Sql IS NULL
BEGIN
    THROW 50001, N'找不到 Stored Procedure：dbo.產生ORDE3剩餘製程', 1;
END;

/* 保險：確保最後是 ALTER 不是 CREATE */
SET @Sql = REPLACE(@Sql, N'CREATE PROCEDURE', N'ALTER PROCEDURE');
SET @Sql = REPLACE(@Sql, N'CREATE  PROCEDURE', N'ALTER  PROCEDURE');

/* =====================================================
   第 1 輪：安全性 / 參數正規化
   ===================================================== */
SET @Old = N'SET NOCOUNT ON';
SET @New = N'SET NOCOUNT ON
SET XACT_ABORT ON';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (1, N'加入 XACT_ABORT', LEFT(@Old, 200));
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

SET @Old = N'IF (@INPART = '''')
  SET @INPART = ''%''
  --SET @INPART = ''22F01272-0%''  ';
SET @New = N'SET @INPART = NULLIF(LTRIM(RTRIM(@INPART)), '''')
IF (@INPART IS NULL)
  SET @INPART = ''%''
  --SET @INPART = ''22F01272-0%''  ';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (1, N'@INPART 正規化', N'IF (@INPART = '''') ...');
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

/* =====================================================
   第 2 輪：暫存表索引（效能）
   ===================================================== */
SET @Old = N'SELECT * INTO #SOPNAME FROM SOPNAME
--202409/21 Techup ADD
---CREATE CLUSTERED INDEX #SOPNAME_Index1 ON #SOPNAME(PRDOPNO,PRDNAME)';
SET @New = N'SELECT * INTO #SOPNAME FROM SOPNAME
--202409/21 Techup ADD
CREATE CLUSTERED INDEX IX_#SOPNAME_PRDOPNO ON #SOPNAME(PRDOPNO)';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (2, N'#SOPNAME 索引', N'SELECT * INTO #SOPNAME ...');
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

SET @Old = N'SELECT * INTO #指派時間 FROM 指派時間 WHERE 人或機台 = 1 AND StartTime >= DATEADD(YEAR,-1, GETDATE())
AND Applier NOT IN (''CMM08'',''CMM09'') ----不含首件及抽檢機台 2025/08/07 Techup
--202409/21 Techup ADD
--CREATE CLUSTERED INDEX #指派時間_Index1 ON #指派時間(ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,Applier,INDWG)';
SET @New = N'SELECT * INTO #指派時間 FROM 指派時間 WHERE 人或機台 = 1 AND StartTime >= DATEADD(YEAR,-1, GETDATE())
AND Applier NOT IN (''CMM08'',''CMM09'') ----不含首件及抽檢機台 2025/08/07 Techup
--202409/21 Techup ADD
CREATE CLUSTERED INDEX IX_#指派時間_Key ON #指派時間(ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,Applier,INDWG)';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (2, N'#指派時間 索引', N'SELECT * INTO #指派時間 ...');
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

/* =====================================================
   第 3 輪：非決定性 UPDATE 修正（邏輯穩定）
   ===================================================== */
SET @Old = N'UPDATE #TEMP3
SET WKNO = B.WKNO,DEPTNO=B.DEPT
FROM #TEMP3 A LEFT OUTER JOIN (SELECT PTPNO,PTPSQ,PRTFO,PRTFM,WKNO,B.DEPT FROM PRODTM A LEFT OUTER JOIN PERSON B ON A.WKNO = B.PENNO WHERE PTPSQ > 0 ) B ON A.ORDFO = B.PRTFO AND A.INPART = B.PTPNO AND A.PRTFM = B.PRTFM';
SET @New = N'UPDATE T
SET T.WKNO = X.WKNO,
    T.DEPTNO = X.DEPT
FROM #TEMP3 AS T
OUTER APPLY (
    SELECT TOP (1)
           P.WKNO,
           R.DEPT
    FROM PRODTM AS P
    LEFT JOIN PERSON AS R ON R.PENNO = P.WKNO
    WHERE P.PTPSQ > 0
      AND P.PRTFO = T.ORDFO
      AND P.PTPNO = T.INPART
      AND P.PRTFM = T.PRTFM
    ORDER BY P.PRTFM DESC, P.CRDATE DESC
) AS X';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (3, N'#TEMP3 WKNO/DEPTNO 更新穩定化', N'UPDATE #TEMP3 SET WKNO...');
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

/* =====================================================
   第 4 輪：收斂全表更新範圍（效能）
   ===================================================== */
SET @Old = N'UPDATE ORDDE4 SET ORDDTP = B.PRDTYPE
 FROM ORDDE4 A,SOPNAME B
WHERE A.ORDFO = B.PRDOPNO
  AND A.ORDFCO IN (''N'',''D'',''P'')
  AND A.ORDNO > ''18''
  AND A.ORDDTP <> B.PRDTYPE';
SET @New = N'UPDATE A
SET A.ORDDTP = B.PRDTYPE
FROM ORDDE4 A
JOIN SOPNAME B ON A.ORDFO = B.PRDOPNO
WHERE A.ORDFCO IN (''N'',''D'',''P'')
  AND A.ORDNO > ''18''
  AND A.ORDDTP <> B.PRDTYPE
  AND (
      @INPART = ''%''
      OR EXISTS (
          SELECT 1
          FROM ORDE3 O
          WHERE O.INPART LIKE @INPART
            AND O.ORDTP = A.ORDTP
            AND O.ORDNO = A.ORDNO
            AND O.ORDSQ = A.ORDSQ
            AND O.ORDSQ1 = A.ORDSQ1
      )
  )';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (4, N'ORDDTP 校正範圍收斂', N'UPDATE ORDDE4 SET ORDDTP...');
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

/* =====================================================
   第 5 輪：一致性修正
   ===================================================== */
SET @Old = N'UPDATE ORDDE4_剩餘製程明細_直式_D SET DLYTIME = 0 WHERE DLYTIME < 0 AND INPART LIKE @INPART';
SET @New = N'UPDATE ORDDE4_剩餘製程明細_直式_D SET DLYTIME = 0 WHERE DLYTIME < 0 AND INPART LIKE @INPART
UPDATE ORDDE4_剩餘製程明細_直式_D SET DLYTIME_O = 0 WHERE DLYTIME_O < 0 AND INPART LIKE @INPART';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (5, N'DLYTIME 負值歸零一致化', N'UPDATE ... DLYTIME < 0 ...');
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

SET @Old = N'WHERE 在站製程序 IS NULL AND 剩餘製程明細4 NOT LIKE ''%AT%''
AND A.INPART = B.INPART AND A.INPART = C.INPART AND C.INFIN = ''N''
AND C.LINE <> ''Z''';
SET @New = N'WHERE 在站製程序 IS NULL AND 剩餘製程明細4 NOT LIKE ''%AT%''
AND A.剩餘工時 > 0
AND A.INPART = B.INPART AND A.INPART = C.INPART AND C.INFIN = ''N''
AND C.LINE <> ''Z''';
IF CHARINDEX(@Old, @Sql) = 0
    INSERT INTO @Missing VALUES (5, N'在站回填條件防呆', N'WHERE 在站製程序 IS NULL ...');
ELSE
    SET @Sql = REPLACE(@Sql, @Old, @New);

/* 任一關鍵片段找不到時中止，避免誤改 */
IF EXISTS (SELECT 1 FROM @Missing)
BEGIN
    SELECT StepNo, StepName, MissingFragment FROM @Missing;
    THROW 50002, N'SP 修補中止：有替換片段未匹配，請比對目前資料庫版本。', 1;
END;

EXEC sp_executesql @Sql;

PRINT N'已完成：dbo.產生ORDE3剩餘製程 5 輪實際修補。';
