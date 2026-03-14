/*
檔名: 產生ORDE3剩餘製程_5輪修正.sql
用途: 針對 dbo.產生ORDE3剩餘製程 的 5 輪檢查與修正（最小侵入）。
套用方式: 請先備份原 SP，再依序套用下列片段。
*/

/* =========================
   第 1 輪：參數與交易安全
   ========================= */

-- [修正1-1] 程式一開始加入 XACT_ABORT，避免執行中斷留下不一致交易。
-- 原本: SET NOCOUNT ON
-- 改為:
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- [修正1-2] 參數正規化，避免空白字串造成全表掃描或條件誤判。
-- 原本:
-- IF (@INPART = '')
--   SET @INPART = '%'
-- 改為:
SET @INPART = NULLIF(LTRIM(RTRIM(@INPART)), '');
IF (@INPART IS NULL)
    SET @INPART = '%';


/* =========================
   第 2 輪：暫存表索引補強
   ========================= */

-- [修正2-1] #SOPNAME 高頻 Join 鍵索引（原註解改啟用）
IF OBJECT_ID('tempdb..#SOPNAME') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM tempdb.sys.indexes
        WHERE object_id = OBJECT_ID('tempdb..#SOPNAME') AND name = 'IX_#SOPNAME_PRDOPNO'
    )
    CREATE CLUSTERED INDEX IX_#SOPNAME_PRDOPNO ON #SOPNAME(PRDOPNO);
END;

-- [修正2-2] #指派時間 高頻 Join 鍵索引（原註解改啟用）
IF OBJECT_ID('tempdb..#指派時間') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM tempdb.sys.indexes
        WHERE object_id = OBJECT_ID('tempdb..#指派時間') AND name = 'IX_#指派時間_Key'
    )
    CREATE NONCLUSTERED INDEX IX_#指派時間_Key
    ON #指派時間(ORDTP, ORDNO, ORDSQ, ORDSQ1, ORDSQ2, INPART, Applier, INDWG)
    INCLUDE (StartTime, EndTime, PRTFM, 人或機台, Remark);
END;

-- [修正2-3] #TEMP2/#QA1 補索引（避免後續大量 Join/更新掃描）
IF OBJECT_ID('tempdb..#TEMP2') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_#TEMP2_Key
    ON #TEMP2(ORDTP, ORDNO, ORDSQ, ORDSQ1, ORDSQ2, INPART, ORDFO);
END;

IF OBJECT_ID('tempdb..#QA1') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_#QA1_Key
    ON #QA1(OLDPART, INPART, ORDSQ2, CARDNO);
END;


/* =========================
   第 3 輪：非決定性 UPDATE 修正
   ========================= */

-- [修正3-1] 修正 #TEMP3 更新 WKNO/DEPTNO 的多筆來源問題。
-- 問題: 原 SQL 對 PRODTM 可能回傳同鍵多筆，UPDATE 結果不穩定。
-- 方式: 以 OUTER APPLY 取最新一筆 PRTFM 對應的 WKNO/DEPT。

/*
原本:
UPDATE #TEMP3
SET WKNO = B.WKNO,DEPTNO=B.DEPT
FROM #TEMP3 A LEFT OUTER JOIN (
  SELECT PTPNO,PTPSQ,PRTFO,PRTFM,WKNO,B.DEPT
  FROM PRODTM A LEFT OUTER JOIN PERSON B ON A.WKNO = B.PENNO
  WHERE PTPSQ > 0
) B ON A.ORDFO = B.PRTFO AND A.INPART = B.PTPNO AND A.PRTFM = B.PRTFM
*/

UPDATE T
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
) AS X;


/* =========================
   第 4 輪：全表操作範圍收斂
   ========================= */

-- [修正4-1] 「校正製程類別」更新增加 INPART 範圍條件，避免每次全表掃描。
/*
原本:
UPDATE ORDDE4 SET ORDDTP = B.PRDTYPE
FROM ORDDE4 A,SOPNAME B
WHERE A.ORDFO = B.PRDOPNO
  AND A.ORDFCO IN ('N','D','P')
  AND A.ORDNO > '18'
  AND A.ORDDTP <> B.PRDTYPE
*/

UPDATE A
SET A.ORDDTP = B.PRDTYPE
FROM ORDDE4 A
JOIN SOPNAME B ON A.ORDFO = B.PRDOPNO
WHERE A.ORDFCO IN ('N','D','P')
  AND A.ORDNO > '18'
  AND A.ORDDTP <> B.PRDTYPE
  AND (
      @INPART = '%'
      OR EXISTS (
          SELECT 1
          FROM ORDE3 O
          WHERE O.INPART LIKE @INPART
            AND O.ORDTP = A.ORDTP
            AND O.ORDNO = A.ORDNO
            AND O.ORDSQ = A.ORDSQ
            AND O.ORDSQ1 = A.ORDSQ1
      )
  );

-- [修正4-2] 重要批次更新語句改 ANSI JOIN，避免漏條件形成隱性 Cartesian Join。
-- 說明: 本檔不逐段展開整支 SP，建議優先改「UPDATE + 多表」區塊。


/* =========================
   第 5 輪：邏輯一致性與資料品質
   ========================= */

-- [修正5-1] 可用工時為負且在站為 NULL 時，回填在站製程序前先排除已結案製卡。
-- 避免 C/Y 狀態被誤回填在站。

UPDATE D
SET D.在站製程序 = X.ORDSQ2
FROM ORDDE4_剩餘製程明細_D AS D
JOIN (
    SELECT A.INPART, MIN(A.ORDSQ2) AS ORDSQ2
    FROM ORDDE4_剩餘製程明細_直式_D A
    JOIN #SOPNAME B ON A.ORDFO = B.PRDOPNO
    WHERE A.ORDSQ3 = 0
      AND A.ORDSQ2 > 0
      AND A.ORDFCO = 'N'
      AND (B.ISACTIVE = 0 OR B.SOPKIND = '會驗')
    GROUP BY A.INPART
) AS X ON D.INPART = X.INPART
WHERE D.在站製程序 IS NULL
  AND D.剩餘工時 > 0;

-- [修正5-2] 停駐工時一致化：全部最後統一歸零一次，避免分段更新後出現負值回寫。
UPDATE ORDDE4_剩餘製程明細_直式_D
SET DLYTIME = 0
WHERE DLYTIME < 0;

UPDATE ORDDE4_剩餘製程明細_直式_D
SET DLYTIME_O = 0
WHERE DLYTIME_O < 0;


/* 驗證建議
1) 先用單一製卡測試：EXEC dbo.產生ORDE3剩餘製程 '24X01008MT-0%'
2) 再跑全量：EXEC dbo.產生ORDE3剩餘製程 ''
3) 比對修正前後：
   - 在站製程序
   - 剩餘工時/可用工時
   - 目前排程順序 / A1 建立日
   - 主要輸出表筆數與關鍵欄位差異
*/
