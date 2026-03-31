USE [TEST]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[產生ORDE3剩餘製程_v2]
    @INPART VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @INPART = NULLIF(LTRIM(RTRIM(@INPART)), '');
    IF @INPART IS NULL SET @INPART = '%';

    BEGIN TRY
        BEGIN TRAN;

        IF OBJECT_ID('tempdb..#Card') IS NOT NULL DROP TABLE #Card;
        IF OBJECT_ID('tempdb..#SOP') IS NOT NULL DROP TABLE #SOP;
        IF OBJECT_ID('tempdb..#ProdAgg') IS NOT NULL DROP TABLE #ProdAgg;
        IF OBJECT_ID('tempdb..#Dispatch') IS NOT NULL DROP TABLE #Dispatch;
        IF OBJECT_ID('tempdb..#Flow') IS NOT NULL DROP TABLE #Flow;
        IF OBJECT_ID('tempdb..#FlowDedup') IS NOT NULL DROP TABLE #FlowDedup;
        IF OBJECT_ID('tempdb..#Summary') IS NOT NULL DROP TABLE #Summary;

        SELECT
            O.ORDTP,
            O.ORDNO,
            O.ORDSQ,
            O.ORDSQ1,
            O.INPART,
            O.ORDSNO,
            O.ORDQTY,
            O.INDWG,
            O.LINE,
            O.INFIN
        INTO #Card
        FROM ORDE3 O
        WHERE O.INPART LIKE @INPART
          AND O.ORDQTY > 0
          AND O.ORDTP NOT IN ('C')
          AND O.LINE NOT IN ('U')
          AND O.INFIN IN ('N','P','C','Y');

        IF NOT EXISTS (SELECT 1 FROM #Card)
        BEGIN
            COMMIT;
            RETURN;
        END

        CREATE CLUSTERED INDEX IX_Card_PK ON #Card(ORDTP, ORDNO, ORDSQ, ORDSQ1, INPART);
        CREATE NONCLUSTERED INDEX IX_Card_INPART ON #Card(INPART);

        SELECT
            S.PRDOPNO,
            PRDNAME = CAST(S.PRDNAME AS VARCHAR(100)),
            S.SOPKIND,
            S.PRDTYPE
        INTO #SOP
        FROM SOPNAME S
        WHERE S.PRDNAME NOT IN ('lo','uld','LD','ULD','am')
          AND S.SOPKIND NOT IN ('其它','其它1');

        CREATE CLUSTERED INDEX IX_SOP_OPNO ON #SOP(PRDOPNO);

        SELECT
            P.PTPNO,
            P.PTPSQ,
            P.PRTFO,
            RPT_QTY = SUM(P.PRFQY),
            LAST_PRTFM = MAX(P.PRTFM)
        INTO #ProdAgg
        FROM PRODTM P
        JOIN #Card C ON C.INPART = P.PTPNO
        WHERE P.PTPSQ > 0
        GROUP BY P.PTPNO, P.PTPSQ, P.PRTFO;

        CREATE CLUSTERED INDEX IX_ProdAgg_Key ON #ProdAgg(PTPNO, PTPSQ, PRTFO);

        SELECT
            D.INPART,
            D.ORDTP,
            D.ORDNO,
            D.ORDSQ,
            D.ORDSQ1,
            D.ORDSQ2,
            Applier = MIN(D.Applier),
            FirstStartTime = MIN(D.StartTime)
        INTO #Dispatch
        FROM 指派時間 D
        JOIN #Card C ON C.INPART = D.INPART
                    AND C.ORDTP = D.ORDTP
                    AND C.ORDNO = D.ORDNO
                    AND C.ORDSQ = D.ORDSQ
                    AND C.ORDSQ1 = D.ORDSQ1
        WHERE D.人或機台 = 1
          AND D.Remark <> '16'
          AND D.StartTime >= DATEADD(YEAR,-1,GETDATE())
        GROUP BY D.INPART, D.ORDTP, D.ORDNO, D.ORDSQ, D.ORDSQ1, D.ORDSQ2;

        CREATE CLUSTERED INDEX IX_Dispatch_Key ON #Dispatch(INPART, ORDTP, ORDNO, ORDSQ, ORDSQ1, ORDSQ2);

        SELECT
            B.ORDTP,
            B.ORDNO,
            B.ORDSQ,
            B.ORDSQ1,
            B.ORDSQ2,
            ORDSQ3 = CAST(0 AS INT),
            [及時順序ORDSQ2] = ROW_NUMBER() OVER (PARTITION BY C.INPART ORDER BY B.ORDSQ2),
            C.INPART,
            C.ORDSNO,
            B.ORDFO,
            B.ORDQY2,
            B.ORDDTP,
            ORDFM1 = ISNULL(B.ORDFM1,0) + ISNULL(B.ORDFM2,0),
            B.ORDUPR,
            ORDFCO = CASE
                        WHEN B.ORDFCO = 'N' AND B.ORDQY2 > 0 AND ISNULL(PA.RPT_QTY,0) >= B.ORDQY2 THEN 'Y'
                        ELSE B.ORDFCO
                     END,
            PRDNAME = CASE
                        WHEN B.ORDFCO IN ('Y','C') THEN SS.PRDNAME + '●'
                        WHEN B.ORDFCO = 'N' THEN '■' + SS.PRDNAME
                        ELSE SS.PRDNAME
                      END,
            B.ORDAMT,
            DLYTIME = CASE WHEN ISNULL(B.DLYTIME,0) < 0 THEN 0 ELSE ISNULL(B.DLYTIME,0) END,
            B.ORDDY1,
            B.ORDDY2,
            B.ORDDY4,
            B.ORDDY5,
            B.MP5CODE,
            SS.SOPKIND,
            [目前排程順序] = CAST('' AS VARCHAR(20)),
            [目前A1排程順序建立日] = CAST(NULL AS DATETIME),
            A1DLYTIME = CAST(0 AS DECIMAL(9,2)),
            PRDATE1 = M.CRDATE,
            PRTFM = PA.LAST_PRTFM,
            WKNO = CAST('' AS VARCHAR(20)),
            DEPTNO = CAST('' AS VARCHAR(20)),
            Applier = ISNULL(DP.Applier, ''),
            [下機日延遲備註] = CAST('' AS VARCHAR(10)),
            CARDNO = CAST('' AS VARCHAR(10)),
            [外包預計天數] = CAST('' AS VARCHAR(100)),
            DLYTIME2 = CAST(NULL AS DATETIME),
            DLYTIME_O = CAST(0 AS DECIMAL(9,2)),
            [總超前工時最大目前排程順序] = CAST('' AS VARCHAR(500)),
            [上關製程序] = CAST(NULL AS INT),
            [上關製程] = CAST('' AS VARCHAR(10)),
            [下關機加製程] = CAST('' AS VARCHAR(10)),
            [下關機加工時] = CAST(0 AS DECIMAL(9,2)),
            [機加大帶小] = CAST('' AS VARCHAR(10)),
            [下關目前排程順序] = CAST('' AS VARCHAR(10))
        INTO #Flow
        FROM #Card C
        JOIN ORDDE4 B ON B.ORDTP = C.ORDTP
                      AND B.ORDNO = C.ORDNO
                      AND B.ORDSQ = C.ORDSQ
                      AND B.ORDSQ1 = C.ORDSQ1
        JOIN #SOP SS ON SS.PRDOPNO = B.ORDFO
        LEFT JOIN #ProdAgg PA ON PA.PTPNO = C.INPART
                             AND PA.PTPSQ = B.ORDSQ2
                             AND PA.PRTFO = B.ORDFO
        LEFT JOIN ORDMENO M ON M.ORDTP = C.ORDTP
                           AND M.ORDNO = C.ORDNO
                           AND M.ORDSQ = C.ORDSQ
                           AND M.ORDSQ1 = C.ORDSQ1
        LEFT JOIN #Dispatch DP ON DP.INPART = C.INPART
                              AND DP.ORDTP = C.ORDTP
                              AND DP.ORDNO = C.ORDNO
                              AND DP.ORDSQ = C.ORDSQ
                              AND DP.ORDSQ1 = C.ORDSQ1
                              AND DP.ORDSQ2 = B.ORDSQ2
        WHERE (SS.PRDTYPE <> '4' OR (B.ORDAMT > 0 AND B.ORDSQ2 = 1));

        CREATE CLUSTERED INDEX IX_Flow_PK ON #Flow(ORDTP, ORDNO, ORDSQ, ORDSQ1, ORDSQ2, ORDSQ3, INPART);

        ;WITH D AS (
            SELECT *,
                   RN = ROW_NUMBER() OVER (PARTITION BY ORDTP, ORDNO, ORDSQ, ORDSQ1, ORDSQ2, ORDSQ3, INPART ORDER BY ORDSQ2)
            FROM #Flow
        )
        SELECT * INTO #FlowDedup FROM D WHERE RN = 1;

        CREATE CLUSTERED INDEX IX_FlowDedup_PK ON #FlowDedup(ORDTP, ORDNO, ORDSQ, ORDSQ1, ORDSQ2, ORDSQ3, INPART);

        DELETE T
        FROM ORDDE4_剩餘製程明細_直式_D T
        JOIN (SELECT DISTINCT INPART FROM #FlowDedup) D ON D.INPART = T.INPART;

        INSERT INTO ORDDE4_剩餘製程明細_直式_D (
            ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDSQ3,[及時順序ORDSQ2],INPART,ORDSNO,ORDFO,ORDQY2,ORDDTP,ORDFM1,ORDUPR,ORDFCO,PRDNAME,ORDAMT,DLYTIME,ORDDY1,ORDDY2,ORDDY4,ORDDY5,MP5CODE,SOPKIND,[目前排程順序],[目前A1排程順序建立日],A1DLYTIME,PRDATE1,PRTFM,WKNO,DEPTNO,Applier,[下機日延遲備註],CARDNO,[外包預計天數],DLYTIME2,DLYTIME_O,[總超前工時最大目前排程順序],[上關製程序],[上關製程],[下關機加製程],[下關機加工時],[機加大帶小],[下關目前排程順序]
        )
        SELECT
            ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDSQ3,[及時順序ORDSQ2],INPART,ORDSNO,ORDFO,ORDQY2,ORDDTP,ORDFM1,ORDUPR,ORDFCO,PRDNAME,ORDAMT,DLYTIME,ORDDY1,ORDDY2,ORDDY4,ORDDY5,MP5CODE,SOPKIND,[目前排程順序],[目前A1排程順序建立日],A1DLYTIME,PRDATE1,PRTFM,WKNO,DEPTNO,Applier,[下機日延遲備註],CARDNO,[外包預計天數],DLYTIME2,DLYTIME_O,[總超前工時最大目前排程順序],[上關製程序],[上關製程],[下關機加製程],[下關機加工時],[機加大帶小],[下關目前排程順序]
        FROM #FlowDedup;

        ;WITH S AS (
            SELECT
                F.ORDTP,
                F.ORDNO,
                F.ORDSQ,
                F.ORDSQ1,
                F.INPART,
                ORDSNO = MIN(F.ORDSNO),
                RemainCnt = SUM(CASE WHEN F.ORDFCO = 'N' AND F.ORDSQ3 = 0 THEN 1 ELSE 0 END),
                RemainHours = SUM(CASE WHEN F.ORDFCO = 'N' THEN ISNULL(F.DLYTIME,0) ELSE 0 END),
                MaterialAmt = SUM(CASE WHEN F.ORDFO LIKE '%料%' THEN ISNULL(F.ORDAMT,0) ELSE 0 END),
                OnStation = MIN(CASE WHEN F.ORDFCO = 'N' AND F.ORDSQ3 = 0 THEN F.ORDSQ2 END)
            FROM #FlowDedup F
            GROUP BY F.ORDTP, F.ORDNO, F.ORDSQ, F.ORDSQ1, F.INPART
        )
        SELECT
            S.ORDTP,
            S.ORDNO,
            S.ORDSQ,
            S.ORDSQ1,
            S.INPART,
            S.ORDSNO,
            NEW_ORDSNO = CAST(NULL AS VARCHAR(10)),
            [剩餘製程明細] = STUFF((
                SELECT N'→' + CAST(F2.PRDNAME AS NVARCHAR(100))
                FROM #FlowDedup F2
                WHERE F2.INPART = S.INPART AND F2.ORDSQ3 = 0
                ORDER BY F2.ORDSQ2
                FOR XML PATH(''), TYPE
            ).value('.','nvarchar(max)'),1,1,''),
            [剩餘製程明細2] = STUFF((
                SELECT N',' + CAST(F2.PRDNAME AS NVARCHAR(100))
                FROM #FlowDedup F2
                WHERE F2.INPART = S.INPART AND F2.ORDSQ3 = 0
                ORDER BY F2.ORDSQ2
                FOR XML PATH(''), TYPE
            ).value('.','nvarchar(max)'),1,1,''),
            [剩餘製程明細3] = CAST(NULL AS NVARCHAR(MAX)),
            [剩餘數] = S.RemainCnt,
            [單件TOTAL] = CAST(NULL AS NVARCHAR(MAX)),
            TOTAL = CAST(NULL AS NVARCHAR(MAX)),
            [前置設計工時] = CAST(0 AS NUMERIC(15,2)),
            [前置設計預計完成日] = CAST(NULL AS NVARCHAR(20)),
            [TOTAL工時] = CAST(S.RemainHours AS NUMERIC(15,2)),
            [TOTAL工時預計完成日] = CAST(NULL AS NVARCHAR(20)),
            CUS工時 = CAST(0 AS NUMERIC(15,2)),
            [前日剩餘工時] = CAST(0 AS NUMERIC(15,2)),
            [剩餘工時] = CAST(S.RemainHours AS NUMERIC(15,2)),
            [有效工時] = CAST(0 AS NUMERIC(15,2)),
            [可用工時] = CAST(0 AS NUMERIC(15,2)),
            [特別可用工時] = CAST(0 AS NUMERIC(15,2)),
            AutoPc = 'N',
            [剩餘製程明細4] = CAST(NULL AS NVARCHAR(MAX)),
            [全部製程明細含DLYTIME] = CAST(NULL AS NVARCHAR(MAX)),
            [全部製程明細含DLYTIME_不含設計] = CAST(NULL AS NVARCHAR(MAX)),
            [最後機加關] = CAST(NULL AS INT),
            [剩餘機加製程] = CAST(NULL AS NVARCHAR(MAX)),
            [剩餘排程正推] = CAST(NULL AS VARCHAR(MAX)),
            [製造數] = CAST(0 AS DECIMAL(18,4)),
            [材料費] = CAST(S.MaterialAmt AS DECIMAL(15,2)),
            [QC註記] = CAST(NULL AS VARCHAR(4)),
            [在站製程序] = S.OnStation,
            [在站製程序前兩關DLYTIME] = CAST(0 AS INT),
            U_INPART = CAST(NULL AS VARCHAR(40)),
            [AKT大機產品顆數] = CAST(NULL AS VARCHAR(40)),
            [昨日報工工時] = CAST(0 AS DECIMAL(15,2)),
            [開單製卡] = CAST(NULL AS VARCHAR(40)),
            [開單前製卡製程明細] = CAST(NULL AS NVARCHAR(MAX)),
            [工件位置] = CAST(NULL AS VARCHAR(20)),
            [位置時間] = CAST(NULL AS VARCHAR(20)),
            [單件標準TOTAL] = CAST(NULL AS NVARCHAR(MAX)),
            [上階最大焊接製卡剩餘工時] = CAST(0 AS NUMERIC(15,2)),
            [當站焊接製卡狀態] = CAST(NULL AS VARCHAR(40)),
            [剩餘工時同批會驗] = CAST(0 AS NUMERIC(15,2)),
            [難易度等級] = CAST(NULL AS VARCHAR(10)),
            [要件序] = CAST(NULL AS VARCHAR(10)),
            [下階同交期剩餘總工時] = CAST(0 AS DECIMAL(15,2)),
            [訂單單價] = CAST(0 AS DECIMAL(15,2))
        INTO #Summary
        FROM S;

        CREATE CLUSTERED INDEX IX_Summary_PK ON #Summary(ORDTP, ORDNO, ORDSQ, ORDSQ1, INPART);

        DELETE T
        FROM ORDDE4_剩餘製程明細_D T
        JOIN (SELECT DISTINCT INPART FROM #Summary) D ON D.INPART = T.INPART;

        INSERT INTO ORDDE4_剩餘製程明細_D (
            ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,ORDSNO,NEW_ORDSNO,[剩餘製程明細],[剩餘製程明細2],[剩餘製程明細3],[剩餘數],[單件TOTAL],TOTAL,[前置設計工時],[前置設計預計完成日],[TOTAL工時],[TOTAL工時預計完成日],CUS工時,[前日剩餘工時],[剩餘工時],[有效工時],[可用工時],[特別可用工時],AutoPc,[剩餘製程明細4],[全部製程明細含DLYTIME],[全部製程明細含DLYTIME_不含設計],[最後機加關],[剩餘機加製程],[剩餘排程正推],[製造數],[材料費],[QC註記],[在站製程序],[在站製程序前兩關DLYTIME],U_INPART,[AKT大機產品顆數],[昨日報工工時],[開單製卡],[開單前製卡製程明細],[工件位置],[位置時間],[單件標準TOTAL],[上階最大焊接製卡剩餘工時],[當站焊接製卡狀態],[剩餘工時同批會驗],[難易度等級],[要件序],[下階同交期剩餘總工時],[訂單單價]
        )
        SELECT
            ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,ORDSNO,NEW_ORDSNO,[剩餘製程明細],[剩餘製程明細2],[剩餘製程明細3],[剩餘數],[單件TOTAL],TOTAL,[前置設計工時],[前置設計預計完成日],[TOTAL工時],[TOTAL工時預計完成日],CUS工時,[前日剩餘工時],[剩餘工時],[有效工時],[可用工時],[特別可用工時],AutoPc,[剩餘製程明細4],[全部製程明細含DLYTIME],[全部製程明細含DLYTIME_不含設計],[最後機加關],[剩餘機加製程],[剩餘排程正推],[製造數],[材料費],[QC註記],[在站製程序],[在站製程序前兩關DLYTIME],U_INPART,[AKT大機產品顆數],[昨日報工工時],[開單製卡],[開單前製卡製程明細],[工件位置],[位置時間],[單件標準TOTAL],[上階最大焊接製卡剩餘工時],[當站焊接製卡狀態],[剩餘工時同批會驗],[難易度等級],[要件序],[下階同交期剩餘總工時],[訂單單價]
        FROM #Summary;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
