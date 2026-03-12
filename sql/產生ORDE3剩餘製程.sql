USE [TEST]
GO
/****** Object:  StoredProcedure [dbo].[����ORDE3�Ѿl�s�{]    Script Date: 2026/03/12 �U�� 22:51:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



ALTER  PROCEDURE [dbo].[����ORDE3�Ѿl�s�{]
      @INPART Varchar(40)     -- �s�d              
      AS
 SET NOCOUNT ON
 SET XACT_ABORT ON
 -----------------------------------------------------------------------------------------
-- EXEC  dbo.[����ORDE3�Ѿl�s�{]
-- �s���覡�B�z���_1
-- �ϥε{�� : �C�b�p�ɰ���Ƶ{ �C�i�s�d���u���z�@��
-- �]�p�� : �i�i��
-- �ɶ� : 2018/05/10
-- EXEC dbo.����ORDE3�Ѿl�s�{ '24X01008MT-0%'
-- EXEC dbo.����ORDE3�Ѿl�s�{ '22Y03344-000#5R1'
-- EXEC dbo.����ORDE3�Ѿl�s�{ '23G04777SL-6-001#1'
-- EXEC dbo.����ORDE3�Ѿl�s�{ '24D03782AF-000'
-- EXEC dbo.����ORDE3�Ѿl�s�{ '23Q03364-000R8'
-- EXEC dbo.����ORDE3�Ѿl�s�{ '13L9045GR-000'
-- EXEC dbo.����ORDE3�Ѿl�s�{ ''
-----------------------------------------------------------------------------------------
----- 2025/09/09 ADD �ץ����~���s�{���O
-- SELECT B.PRDNAME,B.PRDTYPE,A.*
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
  )
----- 2025/09/09 END


SET @INPART = NULLIF(LTRIM(RTRIM(@INPART)), '')
IF (@INPART IS NULL)
  SET @INPART = '%'
  --SET @INPART = '22F01272-0%'  
 

DECLARE @DLYTIME�C��u�@�p��  INT

SET @DLYTIME�C��u�@�p�� = 10

SELECT INPART INTO #�n�R�����s�d FROM ORDE3 WHERE 1 = 0

SELECT * INTO #SOPNAME FROM SOPNAME
--202409/21 Techup ADD
CREATE CLUSTERED INDEX IX_#SOPNAME_PRDOPNO ON #SOPNAME(PRDOPNO)

SELECT * INTO #�����ɶ� FROM �����ɶ� WHERE �H�ξ��x = 1 AND StartTime >= DATEADD(YEAR,-1, GETDATE())
AND Applier NOT IN ('CMM08','CMM09') ----���t����Ω��˾��x 2025/08/07 Techup
--202409/21 Techup ADD
CREATE CLUSTERED INDEX IX_AssignTime_Key ON #�����ɶ�(ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,Applier,INDWG)

----2023/09/21 �v ���� DLYTIME2
SELECT INPART ,ORDSQ2,ORDSQ3 ,DLYTIME2 INTO #ORDDE4_�Ѿl�s�{����_����_DLYTIME2 FROM ORDDE4_�Ѿl�s�{����_���� WHERE DLYTIME2 IS NOT NULL

----�O���쥻�����A 2020/06/08 Techup
SELECT  INPART,ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDSQ3,�ثe�Ƶ{����,Applier,ORDFO,PRDNAME,�ثeA1�Ƶ{���ǫإߤ�,A1DLYTIME,DLYTIME2
INTO #�O��ORDDE4_�Ѿl�s�{����_����_D
FROM ORDDE4_�Ѿl�s�{����_���� WHERE 1 = 0

INSERT INTO #�O��ORDDE4_�Ѿl�s�{����_����_D
SELECT
INPART,ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDSQ3,�ثe�Ƶ{����,Applier,ORDFO,PRDNAME,�ثeA1�Ƶ{���ǫإߤ�,A1DLYTIME,DLYTIME2
FROM ORDDE4_�Ѿl�s�{����_���� WHERE INPART LIKE @INPART
----�O���쥻�����A 2020/06/08 Techup

--�B�z�w�������u ���O���A�X�|�����ת���� 2021/08/03 Techup 2024/1/08 Techup
--SELECT B.*,A.ORDFO,A.ORDDY2,A.ORDQY4,A.ORDFCO
UPDATE ORDDE4 SET ORDQY4 = B.�w�g���u��
FROM ORDDE4 A,
(SELECT PTPNO,PTPSQ,ORDTP,ORDNO,ORDSQ,ORDSQ1,PRTFO,SUM(PRFQY) �w�g���u�� FROM PRODTM GROUP BY PTPNO,PTPSQ,ORDTP,ORDNO,ORDSQ,ORDSQ1,PRTFO) B
WHERE --ORDFNO = '22Q04119-001#2' AND
A.ORDFNO = B.PTPNO AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.PTPSQ
AND A.ORDFO = B.PRTFO
AND A.ORDQY2 = B.�w�g���u�� AND A.ORDFCO = 'N'
AND A.ORDQY2 <> A.ORDQY4
---ORDER BY A.ORDSQ2

---��s�s�{�ƶq�M���u�ƶq�ۦP �������N�令Y 2024/1/08 Techup
---SELECT ORDFNO,ORDFO,ORDSQ2,ORDFCO,ORDQY2,ORDQY4,ORDQY5 FROM ORDDE4
UPDATE ORDDE4 SET ORDFCO = 'Y'
WHERE ORDQY2 = ORDQY4 AND ORDFCO = 'N'
AND ORDQY2 > 0 AND ORDQY5 >= 0
---ORDER BY ORDFNO,ORDSQ2


--SELECT ORDFNO,ORDFO,B.PRDNAME,ORDSQ2,ORDFCO,ORDQY2,ORDQY4,ORDQY5
--UPDATE ORDDE4
--SET ORDFCO = 'Y'
--FROM ORDDE4 A,#SOPNAME B
--WHERE ORDQY2 = ORDQY4 AND ORDQY2 > 0  AND ORDFCO = 'N'
--AND ORDQY5 = 0 AND A.ORDFO = B.PRDOPNO
--ORDER BY ORDFNO,ORDSQ2

-----(�����M��s�{Z5XMT / ZTg) �������u �j������ 2024/08/07 Techup
UPDATE ORDDE4 SET ORDFCO = 'C' WHERE ORDFO IN ('84A','84C') AND ORDFCO = 'N'

----�����������u���e�� �R�m ����(�j�a�p�ƶq�p) ����1�� ����2�� 2024/09/21 Techup
UPDATE ORDDE4 SET ORDFCO = 'C'
WHERE ORDFO IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('F','f','F1','F2')) AND ORDFCO = 'D'


----�B�z�w�g
--UPDATE ORDE3
--SET ��ڮƪp = ''
----WHERE INFIN NOT IN ('N','P') AND ORDSQY > 0  

--2019/12/30 �Ѿl�s�{ �w��u�ɭn��w���u��  �w���u�� > �w��u��
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,ORDSQ2,ORDSQ3 = CAST( 0 AS INT),�ήɶ���ORDSQ2 = CAST( 0 AS INT),INPART,A.ORDSNO,ORDFO ,B.ORDQY2,B.ORDDTP,
ORDFM1 --= (CASE WHEN  B.ORDFM1 <= ISNULL(B.ORDMT3,0) THEN B.ORDFM1 ELSE B.ORDFM1-ISNULL(B.ORDMT3,0) END )
,B.ORDUPR,B.ORDFCO,PRDNAME=CAST(C.PRDNAME AS VARCHAR(100)),
B.ORDAMT,B.DLYTIME,B.ORDDY1,B.ORDDY2,B.ORDDY4,B.ORDDY5, B.MP5CODE,C.SOPKIND,CONVERT(varchar(20), '') �ثe�Ƶ{����,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�, A1DLYTIME = 0
INTO #TEMP2
FROM ORDE3 A,ORDDE4  B,#SOPNAME C
WHERE
INFIN IN ('N','P','C') AND --2018/12/19 �[�JC�����s�d
A.ORDTP NOT IN  ('C')   -- ,'Z'
--AND B.ORDFCO <> 'C'   -----�s�{C������� 2024/09/12 Techup
--AND B.ORDFCO = 'N'
--AND B.ORDQY2 > 0
AND A.LINE NOT IN ('U')   -- 'Z',
AND A.ORDQTY > 0 AND A.ORDTP = B.ORDTP
AND A.ORDNO = B.ORDNO
AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1
AND INPART LIKE @INPART
AND B.ORDFO = C.PRDOPNO
--AND C.PRDNAME NOT LIKE 'Z%'
--AND C.PRDNAME NOT IN ('lo','uld','LO','ULD')
--AND C.PRDOPGP NOT IN ('N01') --NF�����
AND C.PRDNAME NOT IN ('lo','uld','LD','ULD','am')
AND C.SOPKIND NOT IN ('�䥦','�䥦1')
AND (A.ORDNO >= '1801' OR A.INPART = '13L9045GR-000')
AND (C.PRDTYPE <> '4' OR (B.ORDAMT > 0 AND B.ORDSQ2 = 1)) --�O�Τ���� ���O�Ĥ@�� �ƶO>0 �����~
--AND A.ORDTP NOT IN ('4')
--and A.INPART = '20F01190-0-000'
ORDER BY INPART,ORDSQ2


INSERT INTO #TEMP2
--2019/12/30 �Ѿl�s�{ �w��u�ɭn��w���u��  �w���u�� > �w��u��
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,ORDSQ2,ORDSQ3 = CAST( 0 AS INT),�ήɶ���ORDSQ2 = CAST( 0 AS INT),INPART,A.ORDSNO,ORDFO ,B.ORDQY2,B.ORDDTP,
ORDFM1 --= (CASE WHEN  B.ORDFM1 <= ISNULL(B.ORDMT3,0) THEN B.ORDFM1 ELSE B.ORDFM1-ISNULL(B.ORDMT3,0) END )
,B.ORDUPR,B.ORDFCO,PRDNAME=CAST(C.PRDNAME AS VARCHAR(100)),
B.ORDAMT,B.DLYTIME,B.ORDDY1,B.ORDDY2,B.ORDDY4,B.ORDDY5, B.MP5CODE,C.SOPKIND,CONVERT(varchar(20), '') �ثe�Ƶ{����,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�, A1DLYTIME = 0

FROM ORDE3 A,ORDDE4  B,#SOPNAME C
WHERE
INFIN IN ('N','P','C','Y') AND --2018/12/19 �[�JC�����s�d
A.ORDTP NOT IN  ('C')   -- ,'Z'
--AND B.ORDFCO <> 'C'   -----�s�{C������� 2024/09/12 Techup
--AND B.ORDFCO = 'N'
--AND B.ORDQY2 > 0
AND A.LINE IN ('Z')   -- 'Z',
AND A.ORDQTY > 0 AND A.ORDTP = B.ORDTP
AND A.ORDNO = B.ORDNO
AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1
AND INPART LIKE @INPART
AND B.ORDFO = C.PRDOPNO
--AND C.PRDNAME NOT LIKE 'Z%'
--AND C.PRDNAME NOT IN ('lo','uld','LO','ULD')
--AND C.PRDOPGP NOT IN ('N01') --NF�����
AND C.PRDNAME NOT IN ('lo','uld','LD','ULD','am','DG2','DC2','DF2')
AND C.SOPKIND NOT IN ('�䥦','�䥦1')
AND (C.PRDTYPE <> '4' OR (B.ORDAMT > 0 AND B.ORDSQ2 = 1)) --�O�Τ���� ���O�Ĥ@�� �ƶO>0 �����~
AND A.ORDNO >= '1801'
AND A.INPART NOT IN (SELECT distinct INPART FROM #TEMP2)
--AND A.ORDTP NOT IN ('4')
--and A.INPART = '20F01190-0-000'
ORDER BY INPART,ORDSQ2

-----���`��|�����ͪ��]�n�@�_�X�{
INSERT INTO #TEMP2
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,ORDSQ2,ORDSQ3 = CAST( 0 AS INT),�ήɶ���ORDSQ2 = CAST( 0 AS INT),INPART,A.ORDSNO,ORDFO ,B.ORDQY2,B.ORDDTP,
ORDFM1 --= (CASE WHEN  B.ORDFM1 <= ISNULL(B.ORDMT3,0) THEN B.ORDFM1 ELSE B.ORDFM1-ISNULL(B.ORDMT3,0) END )
,B.ORDUPR,B.ORDFCO,PRDNAME=CAST(C.PRDNAME AS VARCHAR(100)),
B.ORDAMT,B.DLYTIME,B.ORDDY1,B.ORDDY2,B.ORDDY4,B.ORDDY5, B.MP5CODE,C.SOPKIND,CONVERT(varchar(20), '') �ثe�Ƶ{����,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�, A1DLYTIME = 0
FROM ORDE3 A,ORDDE4  B,SOPNAME C
WHERE
INFIN IN ('Y','N','P','C') AND --2018/12/19 �[�JC�����s�d
A.ORDTP NOT IN  ('C')   -- ,'Z'
--AND B.ORDFCO <> 'C'   -----�s�{C������� 2024/09/12 Techup
--AND B.ORDFCO = 'N'
--AND B.ORDQY2 > 0
AND A.LINE NOT IN ('U')   -- 'Z',
AND A.ORDQTY > 0 AND A.ORDTP = B.ORDTP
AND A.ORDNO = B.ORDNO
AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1
AND INPART IN (SELECT INPART FROM ���`�楼���ͷs�s�d_DB)
AND INPART NOT IN (SELECT INPART FROM #TEMP2)
AND B.ORDFO = C.PRDOPNO
--AND C.PRDNAME NOT LIKE 'Z%'
--AND C.PRDNAME NOT IN ('lo','uld','LO','ULD')
--AND C.PRDOPGP NOT IN ('N01') --NF�����
AND C.PRDNAME NOT IN ('lo','uld','LD','ULD','am')
AND C.SOPKIND NOT IN ('�䥦','�䥦1')
AND (A.ORDNO >= '1801' OR A.INPART = '13L9045GR-000')
AND (C.PRDTYPE <> '4' OR (B.ORDAMT > 0 AND B.ORDSQ2 = 1)) --�O�Τ���� ���O�Ĥ@�� �ƶO>0 �����~
--AND A.ORDTP NOT IN ('4')
--and A.INPART = '20F01190-0-000'
ORDER BY INPART,ORDSQ2





--20200724�v �[�JQo------------------------------------------------------------------------------------------------------------------------------------
INSERT INTO #TEMP2
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,ORDSQ2,ORDSQ3 = CAST( 0 AS INT),�ήɶ���ORDSQ2 = CAST( 0 AS INT),INPART,A.ORDSNO,ORDFO ,B.ORDQY2,B.ORDDTP,
ORDFM1 --= (CASE WHEN  B.ORDFM1 <= ISNULL(B.ORDMT3,0) THEN B.ORDFM1 ELSE B.ORDFM1-ISNULL(B.ORDMT3,0) END )
,B.ORDUPR,B.ORDFCO,PRDNAME=CAST(C.PRDNAME AS VARCHAR(100)),
B.ORDAMT,B.DLYTIME,B.ORDDY1,B.ORDDY2,B.ORDDY4,B.ORDDY5, B.MP5CODE,C.SOPKIND,CONVERT(varchar(20), '') �ثe�Ƶ{����,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�, A1DLYTIME = 0
FROM ORDE3 A,ORDDE4  B,#SOPNAME C
WHERE
INFIN IN ('Y','N') AND --2018/12/19 �[�JC�����s�d
A.ORDTP NOT IN  ('C')   -- ,'Z'
---AND B.ORDFCO <> 'C'   -----�s�{C������� 2024/09/12 Techup
--AND B.ORDQY2 > 0
AND A.LINE NOT IN ('U')   -- 'Z',
AND A.ORDQTY > 0 AND A.ORDTP = B.ORDTP
AND A.ORDNO = B.ORDNO
AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1
AND INPART LIKE @INPART
AND B.ORDFO = C.PRDOPNO
AND C.PRDNAME ='Qo'
AND A.ORDNO >= '1901'
AND (A.INDWG LIKE '%���%' OR LINE = 'Z')
AND (C.PRDTYPE <> '4' OR (B.ORDAMT > 0 AND B.ORDSQ2 = 1)) --�O�Τ���� ���O�Ĥ@�� �ƶO>0 �����~
--AND A.ORDTP NOT IN ('4')


UPDATE #TEMP2 SET SOPKIND = 'Qo' WHERE PRDNAME  = 'Qo'
------------------------------------------------------------------------------------------------------------------------------------------------------


-----------------���Ͳ��`�檺����-------------------------------------------------------------------------------
--SELECT * INTO #NZ_SOPNAME FROM #SOPNAME WHERE ISACTIVE = '0' OR SOPKIND = '�|��' ---�|��]�n�i�h���Ͳ��`�i�� 2024/03/27 Techup
------2024/09/04 Techup ��ΥH�U���
SELECT * INTO #NZ_SOPNAME FROM SOPNAME WHERE ISACTIVE NOT IN ('1') OR SOPKIND = '�|��' ---�|��]�n�i�h���Ͳ��`�i�� 2024/03/27 Techup
SELECT * INTO #Z_SOPNAME FROM #SOPNAME WHERE PRDNAME LIKE 'Z%' OR PRDNAME = 'lo'



SELECT CARDNO,A.INPART,QATP,DATATP,OLDPART,INDWG,ORDSQ2,SPNO,ISNULL(B.PRDNAME,'') PRDNAME,NGQTY,A.CRDATE,A.CFMDATE,A.GMCFM2  --�إߤ�N�}�l�p�� 2023/02/13 Techup
  INTO #QA0
  FROM QA011 A LEFT OUTER JOIN #NZ_SOPNAME B ON A.SPNO = B.PRDOPNO--,(SELECT DISTINCT INPART FROM #TEMP3) C
 WHERE QATP IN ('Y','W','B','G')
AND SCRL IN ('Y','N')
AND A.CRDATE >= DATEADD(YEAR,-1,GETDATE())
--AND OLDPART = C.INPART


SELECT A.CARDNO,ISNULL(C.INPART1,A.INPART)INPART,OLDPART,A.INDWG,A.ORDSQ2,SPNO,A.PRDNAME,NGQTY,A.CRDATE CFMDATE,A.GMCFM2, ---�إߤ��@�}�l�p�� 2024/05/14 Techup
REWORK = (SELECT  DESCR  FROM AMDKIND WHERE KINDNO='QG' AND B.REWORK = SEQ),
PCCODE=CASE WHEN (B.REWORK IN ('M','N','X','J') AND D.SCRL = 'Y') OR A.QATP = 'G'
--OR A.DATATP = '3' ----���b�Ѧ�DATATP 2024/07/24 Techup
THEN 'Y' ELSE  C.PCCODE  END,
PCDATE=CASE WHEN A.QATP = 'G' --OR A.DATATP = '3'  ----���b�Ѧ�DATATP 2024/07/24 Techup
THEN A.CFMDATE
WHEN B.REWORK IN ('M','N','X','J') THEN B.CFMDATE ELSE ISNULL(C.PCDATE,GETDATE()) END,
--���� = CAST(0 AS BIT),
�}�橵��ɶ� = CAST(0 AS int) --,C.INPART1 QA009_INPART
  INTO #QA1
  FROM #QA0 A LEFT OUTER JOIN QA002B B ON A.CARDNO= B.CARDNO
  LEFT OUTER JOIN QA009 C  ON A.CARDNO= C.CARDNO
  LEFT OUTER JOIN QA001 D  ON A.CARDNO= D.CARDNO
               
  -----�b��z�@�� 2024/11/20 Techup �P�w�i�� QC�ߧY�B�z �Ȩѫ~���}(�i��) ���o���� �B�T�{���]�P�ɧ�s���A
UPDATE #QA1
SET PCCODE = B.SCRL
FROM #QA1 A , QA011 B
WHERE ISNULL(A.PCCODE,'') = '' AND ISNULL(A.PCDATE,'') <> ''
AND A.CARDNO = B.CARDNO AND B.SCRL = 'Y'
AND A.REWORK IN (SELECT DESCR FROM AMDKIND WHERE KINDNO='QG' AND SEQ IN ('M','N','X','J'))

----�p�G���`���٨S�T�{ �h�M��PCCODE 2024/12/17 Techup
UPDATE #QA1
SET PCCODE = NULL,PCDATE = GETDATE()
FROM #QA1 A,���`�楼���ͷs�s�d_DB B
WHERE A.CARDNO = B.CARDNO AND PCCODE = 'Y'

  ------�S�O�B�z��d �O�_�k�� 2025/09/17 Techup
  UPDATE #QA1
  SET PCCODE = 'N',PCDATE = GETDATE()
  WHERE CARDNO LIKE '%G%' AND ISNULL(GMCFM2,'') = ''


--���إ߼Ȧs
SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,CARDNO,ORDSQ2),* INTO #����zDLYTIME_A FROM #QA1 WHERE 1 = 0
SELECT ID = CAST(0 AS INT)  , TIME1 =CAST('' AS datetime),TIME2 = CAST('' AS datetime),MM = CAST(0 AS INT)
INTO #����zDLYTIME_NEW_A FROM #����zDLYTIME_A WHERE 1 = 0

  ----�ק�g�k
  --UPDATE #QA1
  --SET �}�橵��ɶ� = dbo.�ɶ��t_�̤W�Z�ɶ�(CFMDATE ,PCDATE,@DLYTIME�C��u�@�p��)/60.00

INSERT INTO  #����zDLYTIME_A
SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,CARDNO,ORDSQ2),* FROM #QA1
INSERT INTO #����zDLYTIME_NEW_A
SELECT ID,TIME1 = CFMDATE,TIME2 = PCDATE,MM = CAST(0 AS INT) FROM #����zDLYTIME_A



--SELECT *
--INTO ����zDLYTIME_NEW_A
--FROM #����zDLYTIME_NEW_A

EXEC [dbo].[�ɶ������t_�̤W�Z�ɶ�] @DLYTIME�C��u�@�p��,'#����zDLYTIME_NEW_A'

--SELECT * FROM #����zDLYTIME_NEW_A
   
UPDATE #QA1 SET �}�橵��ɶ� = (B.MM-240)/60.00 ---���ƪ��n�D ���`�檺�u��4�p�� �n�B�z�X�� �G�N����ɶ�������4hr 2023/04/19 Techup
FROM #����zDLYTIME_A A,#����zDLYTIME_NEW_A B,#QA1 C
WHERE A.ID = B.ID AND A.CARDNO = C.CARDNO
AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2

DROP TABLE #����zDLYTIME_NEW_A
DROP TABLE #����zDLYTIME_A
-----------------���Ͳ��`�檺����-------------------------------------------------------------------------------









----SELECT 'AAAAAAAAVBVVVVVVVBBBBBB',* FROM #QA1





--SELECT A.INPART
--INTO #���ݭp�⪺�s�d
--FROM (SELECT INPART,COUNT(*) �`���� FROM #TEMP2 GROUP BY INPART ) A,
--(SELECT INPART,COUNT(*) �`���� FROM #TEMP2 WHERE ORDFCO IN ('C','A','Y') GROUP BY INPART) B  
--WHERE A.INPART = B.INPART AND A.�`���� = B.�`����
-----���� �P C A Y���� ���ƬۦP �N�R�� 2022/12/28 Techup
--DELETE #TEMP2
--FROM #TEMP2 A,#���ݭp�⪺�s�d B
--WHERE A.INPART = B.INPART
--AND A.INPART NOT IN (SELECT OLDPART FROM #QA1)-----���`��|���}�ߧ������٬O�n��ܥX�� 2024/07/31

----�S���ƶq���N�R�� 2023/04/13 Techup
--DELETE #TEMP2
--WHERE ORDQY2 = 0
--AND INPART NOT IN (SELECT OLDPART FROM #QA1)-----���`��|���}�ߧ������٬O�n��ܥX��  2024/07/31

----SELECT * FROM #TEMP2 WHERE ORDFCO IN ('C','A','Y') ORDER BY ORDSQ2
----EXEC dbo.����ORDE3�Ѿl�s�{ '21Y03199-000#1'




SELECT A.*,B.CRDATE PRDATE1,C.PRTFM,WKNO=CAST('' AS VARCHAR(20)),DEPTNO=CAST('' AS VARCHAR(20)),D.Applier,CONVERT(varchar(10), '') �U���驵��Ƶ�
,CARDNO = CAST('' AS varchar(10)) ,�~�]�w�p�Ѽ� = CAST('' AS varchar(100)) ,DLYTIME2 = CAST(NULL AS DATETIME) ,
DLYTIME_O = CAST(0 AS decimal(9, 2)),�`�W�e�u�ɳ̤j�ثe�Ƶ{���� = CAST('' AS varchar(100)),
�W���s�{�� = CAST('' AS INT),
�W���s�{ = CAST('' AS varchar(10)),
�U�����[�s�{ = CAST('' AS varchar(10)),�U�����[�u�� = CAST(0 AS decimal(9, 2)),���[�j�a�p = CAST('' AS varchar(10))
,�U���ثe�Ƶ{���� = CAST('' AS varchar(10))
INTO #TEMP3
FROM #TEMP2 A LEFT OUTER JOIN ORDMENO B ON A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
   LEFT OUTER JOIN (SELECT PTPNO,PTPSQ,PRTFO,MAX(PRTFM) PRTFM FROM PRODTM WHERE PTPSQ > 0 GROUP BY PTPNO,PTPSQ,PRTFO ) C
ON A.ORDFO = C.PRTFO AND A.INPART = C.PTPNO  AND A.ORDSQ2 = C.PTPSQ
LEFT OUTER JOIN
(SELECT distinct MIN(Applier) Applier,INPART,ORDNO,ORDSQ,ORDSQ1,ORDSQ2 FROM #�����ɶ�
WHERE �H�ξ��x = 1 AND Remark NOT IN ('2','16') GROUP BY INPART,ORDNO,ORDSQ,ORDSQ1,ORDSQ2 ) D
ON A.INPART = D.INPART AND A.ORDSQ = D.ORDSQ AND A.ORDNO = D.ORDNO AND A.ORDSQ1 = D.ORDSQ1 AND A.ORDSQ2 = D.ORDSQ2

    ---EXEC dbo.����ORDE3�Ѿl�s�{ ''
-----�إ߯��� 2024/09/21 Techup
CREATE CLUSTERED INDEX tmp_Index1 ON #TEMP3(ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,Applier,PRTFM,WKNO,INPART,ORDFO)

--/****** Object:  Index [PK_ORDDE4]    Script Date: 2024/09/21 �U�� 13:25:27 ******/
--ALTER TABLE [dbo].[ORDDE4] ADD  CONSTRAINT [PK_ORDDE4] PRIMARY KEY CLUSTERED
--(
--[ORDTP] ASC,
--[ORDNO] ASC,
--[ORDSQ] ASC,
--[ORDSQ1] ASC,
--[ORDSQ2] ASC
--)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90) ON [PRIMARY]
--GO



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
) AS X
--WHERE INPART = '18D3887AF-013' AND ORDFO = '04'


--WHERE INPART = '18D3887AF-013' AND ORDFO = '04'



--SELECT A.*,B.PRTFM FROM #TEMP3 A LEFT OUTER JOIN (SELECT PTPNO,PTPSQ,PRTFO,MAX(PRTFM) PRTFM FROM PRODTM  WHERE PTPSQ > 0 GROUP BY PTPNO,PTPSQ,PRTFO ) B
--ON A.ORDFO = B.PRTFO AND A.INPART = B.PTPNO
--WHERE A.INPART = '18D3887AF-013' AND ORDFO = '04'
 


--SELECT PTPNO,PTPSQ,PRTFO,MAX(PRTFM) PRTFM FROM PRODTM GROUP BY PTPNO,PTPSQ,PRTFO

--#TEMP3

----�B�z�~�s�禬������ 2019/12/12
--UPDATE #TEMP3 SET ORDFCO ='N' --��ڪ��A�X�~�אּ���u
----SELECT *
--FROM #TEMP3  A,(
--SELECT ISNULL(B.AMDDAY,B.CRUDAY) �إߤ�,A.INPART,PS,PE FROM PURIND A,PURINM B,PURDEL C WHERE A.PUINO = B.PUINO AND A.PURNO = C.PURNO AND A.PURSQ = C.PURSQ
--AND B.SCTRL = 'N' --�T�{��~�⧹�u 2019/12/12 Techup �إ�
--) B
--WHERE ORDDTP = 2 AND A.INPART = B.INPART AND A.ORDSQ2 BETWEEN B.PS AND B.PE
----SELECT * FROM #TEMP3


      --EXEC dbo.����ORDE3�Ѿl�s�{ ''


  --�w���u�ε��ץ[�W���O
  UPDATE #TEMP3
  SET PRDNAME = PRDNAME+'��'
  --WHERE ORDFCO IN ('Y','C','D')
  WHERE ORDFCO IN ('Y','C') ----�������]�⥼���� 2025/10/21 Techup

  ----�ҥ~�B�z�ӵ���� 2024/03/05 Techup
  UPDATE #TEMP3
  SET ORDFCO = 'Y'
  WHERE INPART = '24G03079SL-000' AND ORDFO = '15N'


  --UPDATE #TEMP3
  --SET PRDNAME = PRDNAME+'��'
  --WHERE ORDFCO = 'N' AND SOPKIND = '���[' AND SUBSTRING(PRDNAME,1,1) <> 'Z' AND ORDDY

  ----2020/05/11 �s�W��ORDDY2�Ƶ{���N�n���O
  --UPDATE #TEMP3 SET PRDNAME = '��'+PRDNAME
  ----SELECT *
  --FROM #TEMP3
  --WHERE
  --ORDFCO = 'N' --AND A.SOPKIND = '���['
  --AND ISNULL(ORDDY2 ,'') <> ''
  --AND SUBSTRING(PRDNAME,1,1) <> 'Z'

  --�S���ƪ��~�Ρ�
  UPDATE #TEMP3
  SET PRDNAME = '��'+PRDNAME
  WHERE ORDSQ2 > 0 AND ORDFCO = 'N'
  AND ISNULL(ORDDY2 ,'') = ''
  AND SUBSTRING(PRDNAME,1,1) <> 'Z'
  AND PRDNAME NOT IN ('lo','uld','LD','ULD','am','PK','QF','SC','CP','OS')


 

  --SELECT * FROM ORDDE4
  --WHERE ORDFNO = '19L09538SL-002'
  --ORDER BY ORDSQ2

  ----EXEC dbo.����ORDE3�Ѿl�s�{ ''



--SELECT * FROM ORDDE4_�Ѿl�s�{����_D
-- WHERE INPART = '20D03806AF-000'

--�쥻��bORDDE4����MP5CODE '��' �쥻�n���O GM�n�D���ݭn���O�F 2020/05/14 �إ� Techup
  --UPDATE #TEMP3
  --SET PRDNAME = PRDNAME+MP5CODE WHERE ISNULL(MP5CODE,'') <> ''

  --�P�_�q���Ѿl�s�{�ѤUZED��lo �h�R���ӻs�d
 
 ----�u�ѤUZED���s�d�N�R��
 --INSERT INTO #�n�R�����s�d
 --SELECT A.INPART
 --FROM
 --(SELECT INPART,COUNT(*) SQ FROM #TEMP3 WHERE INPART LIKE '%-E%' AND ORDFCO IN ('N') GROUP BY INPART ) A LEFT OUTER JOIN
 --(SELECT INPART,COUNT(*) SQ FROM #TEMP3 WHERE INPART LIKE '%-E%' AND ORDFCO IN ('N') AND PRDNAME = 'ZED' GROUP BY INPART ) B
 -- ON A.INPART = B.INPART AND A.SQ = B.SQ
 ----WHERE ISNULL(B.INPART,'') <> ''

 --DELETE #TEMP3
 --WHERE INPART IN (SELECT INPART FROM #�n�R�����s�d)



 
-- EXEC dbo.����ORDE3�Ѿl�s�{ ''

---------------------------------------------------------------------------------

--SELECT DISTINCT INPART FROM #TEMP3 WHERE INPART LIKE '23H03055-000%'

----EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT * FROM #QA1
--WHERE INPART = '22G05972MT-000#2R1'


--�P�_table�O�_�s�b
if exists (select name from sysobjects where name = 'ORDDE4_�Ѿl�s�{����_���`��')
BEGIN
   DELETE ORDDE4_�Ѿl�s�{����_���`�� WHERE OLDPART LIKE @INPART
INSERT INTO ORDDE4_�Ѿl�s�{����_���`��
SELECT CARDNO, INPART, OLDPART, INDWG, ORDSQ2, SPNO, PRDNAME, NGQTY, CFMDATE, REWORK, PCCODE, PCDATE,
            �}�橵��ɶ� FROM #QA1 WHERE OLDPART LIKE @INPART
END
ELSE
BEGIN
   SELECT * INTO ORDDE4_�Ѿl�s�{����_���`�� FROM #QA1 WHERE OLDPART LIKE @INPART
END





 
--SELECT B.ORDTP,B.ORDNO,B.ORDSQ,B.ORDSQ1,B.ORDSQ2,ORDSQ3=0,B.INPART,A.INDWG,B.ORDSNO,B.ORDQY2,B.ORDFO,B.PRDNAME,B.PRTFM,
SELECT ORDSQ31 = CAST( 0 AS INT),CARDNO1=ISNULL(A.CARDNO,''),NGQTY=ISNULL(A.NGQTY,0),A.CFMDATE,NEWPART=A.INPART,A.REWORK,A.PCCODE,A.PCDATE,B.*
  INTO #RST
  FROM #QA1 A,#TEMP3 B
 WHERE 1 = 0




--SELECT B.ORDTP,B.ORDNO,B.ORDSQ,B.ORDSQ1,B.ORDSQ2,ORDSQ3=0,B.INPART,A.INDWG,B.ORDSNO,B.ORDQY2,B.ORDFO,B.PRDNAME,B.PRTFM
SELECT B.*
  INTO #QA2
  FROM (SELECT DISTINCT OLDPART FROM #QA1) A,#TEMP3 B
 WHERE A.OLDPART = B.INPART
   AND B.ORDFCO <> 'C'
AND B.ORDFO NOT IN (SELECT PRDOPNO FROM #Z_SOPNAME)

 


 
INSERT INTO #RST
SELECT ORDSQ31=ROW_NUMBER() OVER(PARTITION BY A.INPART,A.ORDSQ2 ORDER BY A.INPART,A.ORDSQ2,B.CFMDATE ),
CARDNO1=ISNULL(B.CARDNO,''),NGQTY=ISNULL(B.NGQTY,0),B.CFMDATE,B.INPART,B.REWORK,B.PCCODE,B.PCDATE,A.*
  FROM #QA2 A,#QA1 B
 WHERE A.INPART = B.OLDPART
   AND A.ORDSQ2 = B.ORDSQ2
 
--INSERT INTO #RST SELECT 0,'',0,NULL,NULL,NULL,NULL,NULL,* FROM #QA2
 
    INSERT INTO #TEMP3
SELECT DISTINCT B.ORDTP,B.ORDNO,B.ORDSQ,B.ORDSQ1,B.ORDSQ2,A.ORDSQ31,�ήɶ���ORDSQ2 = CAST( 0 AS INT),B.INPART,B.ORDSNO, B.ORDFO, B.ORDQY2, B.ORDDTP,
                            B.ORDFM1, B.ORDUPR, B.ORDFCO, B.PRDNAME, B.ORDAMT, B.DLYTIME, B.ORDDY1, B.ORDDY2, B.ORDDY4, B.ORDDY5, B.MP5CODE,
                            B.SOPKIND, B.�ثe�Ƶ{����, B.�ثeA1�Ƶ{���ǫإߤ�, B.A1DLYTIME, B.PRDATE1, B.PRTFM, B.WKNO, B.DEPTNO, B.Applier,
                            B.�U���驵��Ƶ�,A.CARDNO1,'' �~�]�w�p�Ѽ� , NULL DLYTIME2,DLYTIME_O = CAST(0 AS decimal(9, 2)),
�`�W�e�u�ɳ̤j�ثe�Ƶ{���� = CAST('' AS varchar(100)),
�W���s�{�� = CAST('' AS INT),
�W���s�{ = CAST('' AS varchar(10)),�U�����[�s�{ = CAST('' AS varchar(10)),
�U�����[�u�� = CAST(0 AS decimal(9, 2)),���[�j�a�p = CAST('' AS varchar(10)),
�U���ثe�Ƶ{���� = CAST('' AS varchar(10))
 FROM #RST A,#TEMP3 B
WHERE A.INPART = B.INPART
  AND A.ORDSQ2 = B.ORDSQ2
  AND A.ORDSQ31 <> 0

 

--------------------------------------------------------------------------------

--------------------------------------------------------------------------------



---------------------------------------------------------------------------------
---�J�줤��|�� CQ�L���󳣵�7�� 72�p�� 4320���� 2023/06/14 Techup
---�J�줤��|�� CQ�L���󳣵�10�� 100�p�� 6000���� 2023/07/06 Techup �R�w�q��
UPDATE #TEMP3
SET ORDFM1 = '6000'
FROM #TEMP3 A,ORDE1 B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO
AND B.ORDCU IN (SELECT CUSTNO FROM CUSTOME
WHERE CUSTGP = 'CISTL' AND ISNULL(SCRL,'') = 'N')
AND PRDNAME LIKE '%CQ%' AND ORDFCO = 'N'


 
 
 --�P�_table�O�_�s�b
 if exists (select name from sysobjects where name = 'ORDDE4_�Ѿl�s�{����_����_D')
    DELETE ORDDE4_�Ѿl�s�{����_����_D WHERE INPART LIKE @INPART
 --DROP TABLE ORDDE4_�Ѿl�s�{����_����_D
 


  --SELECT 'BBB'
-- EXEC dbo.����ORDE3�Ѿl�s�{ ''



 --SELECT 'CCC',*  
 --FROM ORDDE4_�Ѿl�s�{����_����_D
 
 INSERT INTO ORDDE4_�Ѿl�s�{����_����_D
 SELECT *  
 FROM #TEMP3
 --WHERE ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA')  --2025/10/28 Techup




--SELECT 'AAA'
-- EXEC dbo.����ORDE3�Ѿl�s�{ ''



--��z
----2023/09/01 �v �ȨѮƤ]�n���ƪ�����
SELECT A.*,�ȨѮ� INTO #�ƶO����
FROM ORDDE4_�Ѿl�s�{����_����_D A , ORDE3 B--A,ORDE3 B
WHERE A.INPART = B.INPART
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 ----2024/09/21 Techup ADD
AND
(A.ORDAMT > 1  -----���ƶO�N�n��� 2024/09/18 Techup  
--B.��ڮƪp = '�ȨѮ�'
--OR B.�ȨѮ� = 'Y' Techup 2024/03/12 ���ɫȨѮ�
) --10���H�W�Ӻ�ƶO Techup 2020/12/14
AND A.INPART LIKE @INPART  
--  --��z
--SELECT * INTO #�ƶO����
--FROM ORDDE4_�Ѿl�s�{����_����_D --A,ORDE3 B
--WHERE ORDAMT > 10 --10���H�W�Ӻ�ƶO Techup 2020/12/14
--  AND INPART LIKE @INPART
 

--�R���h���ƶO �u���䤤�@��
DELETE #�ƶO����
FROM #�ƶO���� A LEFT OUTER JOIN (
SELECT MIN(ORDSQ2) ORDSQ2,INPART FROM #�ƶO���� GROUP BY INPART) B
ON A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
WHERE ISNULL(B.INPART,'') = ''


UPDATE #�ƶO����
SET ORDSQ2 = 0,ORDDTP= 2,ORDFM1=ORDAMT,ORDUPR=0,ORDDY2 = NULL,ORDDY4 = NULL,
ORDFO= CASE WHEN ORDFCO IN ('Y','C') THEN '��' ELSE '��' END
,PRDNAME= '��',ORDFCO = 'N',SOPKIND = '��'
--,PRDNAME=CASE WHEN ORDFCO IN ('Y','C') THEN '�ơ�' ELSE '��' END

--2018/10/24 techup
SELECT ORDPN,MAX(B.CFMDAY) CFMDAY INTO #�w�o�ƻs�d FROM INVTAD A,INVTAM B
WHERE A.INVTTP = B.INVTTP AND A.INVTNO = B.INVTNO AND A.INVTTP IN ('301','303') AND B.SCTRL = 'Y'
AND ORDPN IN (SELECT INPART FROM #�ƶO����)
GROUP BY ORDPN
UNION
SELECT INVREM ORDPN,MAX(CFMDAY) CFMDAY  ----�q���o�� 2024/07/23 Techup
FROM TMPTAD1 A ,TMPTAM1 B
WHERE A.INVTTP = B.INVTTP AND A.INVTNO = B.INVTNO AND A.INVREM IN (SELECT INPART FROM #�ƶO����) AND A.INVTTP = 'E99' AND B.SCTRL = 'Y'
GROUP BY INVREM

-- SELECT A.INVTNO,A.INVSEQ,ORDPN,MAX(B.CFMDAY) CFMDAY
-- INTO #�w�o�ƻs�d
-- FROM INVTAD A,INVTAM B
-- WHERE A.INVTTP = B.INVTTP AND A.INVTNO = B.INVTNO AND A.INVTTP IN ('301','303') AND B.SCTRL = 'Y'
-- AND ORDPN IN (SELECT INPART FROM #�ƶO����)
-- GROUP BY A.INVTNO,A.INVSEQ,ORDPN
-- UNION
-- SELECT A.INVTNO,A.INVSEQ,INVREM ORDPN,MAX(CFMDAY) CFMDAY  ----�q���o�� 2024/07/23 Techup
-- FROM TMPTAD1 A ,TMPTAM1 B
-- WHERE A.INVTTP = B.INVTTP AND A.INVTNO = B.INVTNO AND A.INVREM IN (SELECT INPART FROM #�ƶO����) AND A.INVTTP = 'E99' AND B.SCTRL = 'Y'
-- GROUP BY A.INVTNO,A.INVSEQ,INVREM

---- SELECT * FROM INV�o�Ƭ��u
----WHERE INVTNO = '2408190052'

--        UPDATE #�w�o�ƻs�d
-- SET CFMDAY = B.CFMDAY
-- --SELECT A.*,B.CFMDAY
-- FROM #�w�o�ƻs�d A,INV�o�Ƭ��u B
-- WHERE --ORDPN = '20L03005' AND
-- B.SCTRL = 'Y'
-- AND A.INVTNO = B.INVTNO AND A.INVSEQ = B.INVSEQ
-- AND A.CFMDAY < B.CFMDAY AND A.INVTNO >='24'



UPDATE #�ƶO����
SET PRDNAME= '�ơ�' ,ORDFCO = 'Y',PRTFM = A.CFMDAY
FROM #�w�o�ƻs�d A,#�ƶO���� B
WHERE A.ORDPN = B.INPART


----�|���o�� �N�R�����u��� 2023/09/03 Techup
UPDATE #�ƶO����
SET PRTFM = NULL
WHERE ORDFCO = 'N'



----�ȨѮ� ���O�P�w���o���s�� �N�n�R���Ƴo�� 2023/09/12 Techup
DELETE #�ƶO����
FROM #�ƶO���� A,ORDDE4_�Ѿl�s�{����_���`�� B
WHERE A.INPART LIKE @INPART AND A.INPART = B.INPART AND B.INPART <> B.OLDPART
AND B.REWORK NOT LIKE '%���o���s%'
AND A.�ȨѮ� = 'Y'

--�R�����
   alter table #�ƶO���� drop column �ȨѮ�

--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D WHERE INPART = @INPART


--��^#TEMP3
INSERT INTO ORDDE4_�Ѿl�s�{����_����_D
SELECT * FROM #�ƶO����




--��z��o��CAM����� 2020/07/17 Techup-----------------------------------------------------------------------------------------------------------------
 ---------------------------------------------------------------------------------------------------------  
DELETE ORDDE4_�Ѿl�s�{����_����_D
WHERE ORDSQ2 < 0 AND INPART LIKE @INPART


SELECT item
INTO #��o�s�{
--FROM �ഫ�r��ܸ�ƪ�('DB;DD;DS;DP;D9;SOP;EDD;PD;Qo;DW;D;DI;',';')
FROM �ഫ�r��ܸ�ƪ�('DB;DD;DS;DP;D9;SOP;EDD;PD;Qo;DW;D;DI;DN;TM;TT;',';') ----�s�W DN;TM;TT; 2024/05/30 Techup

SELECT item
INTO #CAM�s�{
FROM �ഫ�r��ܸ�ƪ�('Em;Ecl;Elm;Els;Ewj;Ev;Eam;',';')




SELECT A.* ,C.�Ѿl�u��,������������ = CAST(NULL  AS VARCHAR)
INTO #TEMP_��oCAM
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_D C
WHERE SOPKIND = '�]�p' AND ORDFCO = 'N' AND PRDNAME NOT LIKE '%��%'
AND A.INPART = B.INPART
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 ---2024/09/21 Techup ADD
AND B.INFIN IN ('N','P')
AND B.LINE <> 'Z'
AND (REPLACE(PRDNAME,'��','') IN (SELECT item FROM #��o�s�{)
OR REPLACE(PRDNAME,'��','') IN (SELECT item FROM #CAM�s�{)
OR REPLACE(PRDNAME,'��','') IN ('DG','DG2','DC','DC2','DF','DF2') )
AND A.INPART = C.INPART
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1 ---2024/09/21 Techup ADD
AND A.INPART LIKE @INPART

   --select * from #TEMP_��oCAM


UPDATE #TEMP_��oCAM SET ORDFM1 = 0 WHERE REPLACE(PRDNAME,'��','') IN ('DG2','DC2','DF2')

----20200724�[�JQo�v------------------------------------------------------------------------------------------------------------------------------------
--INSERT INTO #TEMP_��oCAM
--SELECT A.* ,C.�Ѿl�u��,������������ = CAST(NULL  AS VARCHAR)
--FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_D C
--WHERE A.PRDNAME LIKE '%Qo%'
--AND ORDFCO = 'N' AND PRDNAME NOT LIKE '%��%'
--AND A.INPART = B.INPART AND B.INFIN = 'N'
----AND B.LINE <> 'Z'
--AND (REPLACE(PRDNAME,'��','') IN (SELECT item FROM #��o�s�{)
--OR REPLACE(PRDNAME,'��','') IN (SELECT item FROM #CAM�s�{))
----AND A.INPART = '20K01163AF-0-003'
--AND A.INPART = C.INPART
-----------------------------------------------------------------------------------------------------------------------------------------------

--�ثe���[�J�P�w
--EDD
--D9

----�ʮƫe�m�@�~
--UPDATE #TEMP_��oCAM
--SET SOPKIND = 'DP'
--WHERE REPLACE(PRDNAME,'��','') IN ('DP')

----ø��
--UPDATE #TEMP_��oCAM
--SET SOPKIND = 'DW'
--WHERE REPLACE(PRDNAME,'��','') IN ('D','DW','PD','DB','DD')

--SOP
UPDATE #TEMP_��oCAM
SET SOPKIND = 'SOP'
WHERE REPLACE(PRDNAME,'��','') IN ('SOP','DS','D9','EDD','D','DW','PD','DB','DD','DP','DI','DF','DC','DG')




----�ͺ�
--UPDATE #TEMP_��oCAM
--SET SOPKIND = 'DQ'
--WHERE REPLACE(PRDNAME,'��','') IN ('DQ','DF2','DC2','DG2')


UPDATE #TEMP_��oCAM
SET SOPKIND = 'CAM'
WHERE REPLACE(PRDNAME,'��','') IN (SELECT item FROM #CAM�s�{)

UPDATE #TEMP_��oCAM
SET SOPKIND = 'DF2'
WHERE REPLACE(PRDNAME,'��','') IN ('DF2')

UPDATE #TEMP_��oCAM
SET SOPKIND = 'DC2'
WHERE REPLACE(PRDNAME,'��','') IN ('DC2')

UPDATE #TEMP_��oCAM
SET SOPKIND = 'DG2'
WHERE REPLACE(PRDNAME,'��','') IN ('DG2')

---- 2022/03/25 �v ORDDY2 �M��l���Ĭ�ҥH����------------------------------------------------------------------------
---- �ץ�[������������]�@     (�H�U����s�d�]�����ץ�,�Y���惡�B,�]�n�P�ɧﭺ��s�d)
--UPDATE #TEMP_��oCAM SET ������������=CASE WHEN ISNULL(�Ѿl�u��,0) <= 10 THEN CONVERT(VARCHAR(10),DATEADD(DAY,(CEILING(ISNULL(�Ѿl�u��,0)/10)+1)*-1,CONVERT(DATETIME,ORDSNO)),111)
-- WHEN ISNULL(�Ѿl�u��,0) > 10  AND ISNULL(�Ѿl�u��,0) <=  20 THEN CONVERT(VARCHAR(10),DATEADD(DAY,(CEILING(ISNULL(�Ѿl�u��,0)/10)+2)*-1,CONVERT(DATETIME,ORDSNO)),111)
-- WHEN ISNULL(�Ѿl�u��,0) > 20  AND ISNULL(�Ѿl�u��,0) <=  30 THEN CONVERT(VARCHAR(10),DATEADD(DAY,(CEILING(ISNULL(�Ѿl�u��,0)/10)+3)*-1,CONVERT(DATETIME,ORDSNO)),111)
-- WHEN ISNULL(�Ѿl�u��,0) > 30  AND ISNULL(�Ѿl�u��,0) <=  50 THEN CONVERT(VARCHAR(10),DATEADD(DAY,(CEILING(ISNULL(�Ѿl�u��,0)/10)+5)*-1,CONVERT(DATETIME,ORDSNO)),111)
-- WHEN ISNULL(�Ѿl�u��,0) > 50  AND ISNULL(�Ѿl�u��,0) <= 100 THEN CONVERT(VARCHAR(10),DATEADD(DAY,(CEILING(ISNULL(�Ѿl�u��,0)/10)+6)*-1,CONVERT(DATETIME,ORDSNO)),111)
-- WHEN ISNULL(�Ѿl�u��,0) > 100 AND ISNULL(�Ѿl�u��,0) <= 200 THEN CONVERT(VARCHAR(10),DATEADD(DAY,(CEILING(ISNULL(�Ѿl�u��,0)/10)+8)*-1,CONVERT(DATETIME,ORDSNO)),111)
-- ELSE CONVERT(VARCHAR(10),DATEADD(DAY,(CEILING(ISNULL(�Ѿl�u��,0)/10)+10)*-1,CONVERT(DATETIME,ORDSNO)),111) END

           
----�B�zORDDY2�S���۩㪺�� �N�έ�����������
--UPDATE #TEMP_��oCAM
--SET ORDDY2 = ������������
--WHERE ISNULL(ORDDY2,'') = ''
----------------------------------------------------------------------------------------------------------------------------------
---- 2022/03/30 �v ��CAM ���ɶ�
UPDATE #TEMP_��oCAM
SET ORDDY2 = ISNULL(ORDDY2,ORDDY1)
WHERE ISNULL(ORDDY2,'') = ''


--���̤j��o�s�{

--INTO #DW�U�s�d�̤j��o�s�{

--��s�ۦP�էO���w��u��
UPDATE #TEMP_��oCAM
SET ORDFM1 = B.ORDFM1
FROM #TEMP_��oCAM A,(
SELECT INPART,SUM(ORDFM1) ORDFM1,SOPKIND FROM #TEMP_��oCAM
--WHERE SOPKIND <> '�]�p'
GROUP BY INPART,SOPKIND) B
WHERE A.SOPKIND = B.SOPKIND AND A.INPART = B.INPART
AND A.ORDFM1 <> B.ORDFM1
--AND A.SOPKIND <> '�]�p'



-- EXEC dbo.����ORDE3�Ѿl�s�{_T '21F01215A-0-027'

--SELECT * FROM #TEMP_��oCAM
--WHERE SOPKIND <> '�]�p'
--ORDER BY SOPKIND



SELECT A.*
INTO #�U�s�d�̤j��o�s�{
FROM #TEMP_��oCAM A ,
(SELECT A.INPART,MAX(ORDSQ2) ORDSQ2,A.SOPKIND
FROM #TEMP_��oCAM A, (
SELECT INPART,MAX(ISNULL(ORDDY2,ORDDY1)) ORDDY2,SOPKIND FROM #TEMP_��oCAM GROUP BY INPART,SOPKIND
) B
WHERE A.SOPKIND = B.SOPKIND AND A.INPART = B.INPART
GROUP BY A.INPART,A.SOPKIND
) B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.SOPKIND = B.SOPKIND


--SELECT * FROM #�U�s�d�̤j��o�s�{




--���̤jCMA�s�{
SELECT INPART,MAX(ORDDY2) ORDDY2
INTO #�U�s�d�̤jCAM�s�{
FROM #TEMP_��oCAM
WHERE REPLACE(PRDNAME,'��','') IN (SELECT item FROM #CAM�s�{)
GROUP BY INPART


SELECT INPART,MAX(ORDSQ2) ORDSQ2
INTO #��o�U�s�d�̤j�����骺�̤j�s�{��
FROM #�U�s�d�̤j��o�s�{
WHERE SOPKIND <> 'CAM'
GROUP BY INPART,ORDSQ2
ORDER BY INPART



SELECT A.INPART,MAX(ORDSQ2) ORDSQ2
INTO #CAM�U�s�d�̤j�����骺�̤j�s�{��
FROM #TEMP_��oCAM A,#�U�s�d�̤jCAM�s�{ B
WHERE A.INPART = B.INPART AND A.ORDDY2 = B.ORDDY2 AND A.SOPKIND = 'CAM'
GROUP BY A.INPART
ORDER BY A.INPART


--EXEC dbo.����ORDE3�Ѿl�s�{ '22M00114-0-Q04'


SELECT ORDTP, ORDNO, ORDSQ, ORDSQ1, A.ORDSQ2,A.ORDSQ3,A.�ήɶ���ORDSQ2, A.INPART, ORDSNO, ORDFO, ORDQY2, ORDDTP, ORDFM1,
ORDUPR, ORDFCO, PRDNAME, ORDAMT, DLYTIME, ORDDY1, ORDDY2, ORDDY4,ORDDY5, MP5CODE, SOPKIND,
�ثe�Ƶ{����,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�, A1DLYTIME = 0, PRDATE1, PRTFM, WKNO, DEPTNO, Applier,
CONVERT(varchar(10), '') �U���驵��Ƶ�,CARDNO ,'' �~�]�w�p�Ѽ� , NULL DLYTIME2, NULL DLYTIME_O,NULL �`�W�e�u�ɳ̤j�ثe�Ƶ{����,
NULL �W���s�{��,NULL �W���s�{,NULL �U�����[�s�{,0 �U�����[�u��,NULL ���[�j�a�p,NULL �U���ثe�Ƶ{����
INTO #TEMP_ALL
FROM #TEMP_��oCAM A,#��o�U�s�d�̤j�����骺�̤j�s�{�� B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
UNION
SELECT ORDTP, ORDNO, ORDSQ, ORDSQ1, A.ORDSQ2,A.ORDSQ3,A.�ήɶ���ORDSQ2, A.INPART, ORDSNO, ORDFO, ORDQY2, ORDDTP, ORDFM1,
ORDUPR, ORDFCO, PRDNAME, ORDAMT, DLYTIME, ORDDY1, ORDDY2, ORDDY4,ORDDY5, MP5CODE, SOPKIND,
�ثe�Ƶ{����,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�, A1DLYTIME = 0, PRDATE1, PRTFM, WKNO, DEPTNO, Applier,
CONVERT(varchar(10), '') �U���驵��Ƶ�,CARDNO ,'' �~�]�w�p�Ѽ� , NULL DLYTIME2, NULL DLYTIME_O,NULL �`�W�e�u�ɳ̤j�ثe�Ƶ{����,
NULL �W���s�{�� ,NULL �W���s�{,NULL �U�����[�s�{,0 �U�����[�u��,NULL ���[�j�a�p,NULL �U���ثe�Ƶ{����
FROM #TEMP_��oCAM A,#CAM�U�s�d�̤j�����骺�̤j�s�{�� B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
ORDER BY A.INPART,A.ORDSQ2




---------------------------------�쥻����ܥ�����---2024/05/3 Techup-----------------------------------
UPDATE #TEMP_ALL
SET ORDSQ2 = '-2'
WHERE SOPKIND = 'DP'

UPDATE #TEMP_ALL
SET ORDSQ2 = '-3'
WHERE SOPKIND = 'DW'

UPDATE #TEMP_ALL
SET ORDSQ2 = '-4'
WHERE SOPKIND = 'SOP'

UPDATE #TEMP_ALL
SET ORDSQ2 = '-5'
WHERE SOPKIND = 'DQ'

UPDATE #TEMP_ALL
SET ORDSQ2 = '-6'
WHERE SOPKIND = 'CAM'

UPDATE #TEMP_ALL
SET ORDSQ2 = '-7'
WHERE SOPKIND = 'DF2'

UPDATE #TEMP_ALL
SET ORDSQ2 = '-8'
WHERE SOPKIND = 'DC2'

UPDATE #TEMP_ALL
SET ORDSQ2 = '-9'
WHERE SOPKIND = 'DG2'
---------------------------------�쥻����ܥ�����---2024/05/3 Techup-----------------------------------

--------�s�W�H�U�s�{ ��ܳ]�p�e�m 2024/05/3 Techup
--UPDATE #TEMP_ALL
--SET ORDSQ2 = '-20'
--WHERE REPLACE(PRDNAME,'��','') = 'DS'

--UPDATE #TEMP_ALL
--SET ORDSQ2 = '-19'
--WHERE REPLACE(PRDNAME,'��','') = 'DN'

--UPDATE #TEMP_ALL
--SET ORDSQ2 = '-18'
--WHERE REPLACE(PRDNAME,'��','') = 'TM'

--UPDATE #TEMP_ALL
--SET ORDSQ2 = '-17'
--WHERE REPLACE(PRDNAME,'��','') = 'TT'

-- EXEC dbo.����ORDE3�Ѿl�s�{ '24X01008MT-0-000'
-- EXEC dbo.����ORDE3�Ѿl�s�{ ''

--SELECT * FROM #TEMP_ALL
   --         WHERE INPART = '24X01008MT-0-000'


INSERT INTO ORDDE4_�Ѿl�s�{����_����_D
SELECT * FROM #TEMP_ALL  
WHERE ORDSQ2 < 0
ORDER BY INPART,ORDSQ2 DESC
--��z��o��CAM����� 2020/07/17 Techup-----------------------------------------------------------------------------------------------------------------




--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D
   --         WHERE INPART = '22G06013MT-004'

--EXEC dbo.����ORDE3�Ѿl�s�{ '22G06013MT-004'






--DROP TABLE #TEMP_��
--�B�z�|���ʮ�
SELECT A.* ,C.�Ѿl�u��,������������ = CAST(NULL  AS VARCHAR)
INTO #TEMP_��
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_D C
WHERE ORDFCO = 'N' AND PRDNAME LIKE '%��%'
AND A.INPART = B.INPART AND B.INFIN = 'N'
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 ---2024/09/21 Techup ADD
AND B.LINE <> 'Z'
AND A.INPART = C.INPART AND B.INPART LIKE '%-0-%'
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1 ---2024/09/21 Techup ADD
AND A.INPART LIKE @INPART

UPDATE #TEMP_��
SET SOPKIND = '��',DLYTIME = 0,ORDSQ2 = -1

--SELECT * FROM #TEMP_��
--WHERE INPART = '20H01002-0-000-E1'

--�w�g�o�ƴN����|���ʮ�
DELETE #TEMP_��
FROM #TEMP_�� A,(SELECT B.ORDPN FROM INVTAM A,INVTAD B WHERE A.INVTTP = '301' AND A.INVTNO = B.INVTNO AND A.INVTTP = B.INVTTP AND A.SCTRL <> 'X') B
WHERE A.INPART = B.ORDPN

--��s�{(�D�]�p)�w�g���u�N��w�g�o�ƻs�@
DELETE #TEMP_��
FROM #TEMP_�� A,(SELECT distinct PTPNO FROM PRODTM A,#SOPNAME B WHERE PTPSQ >0 AND A.PRTFO = B.PRDOPNO AND B.SOPKIND NOT IN ('�]�p','�䥦','�䥦1')) B
WHERE A.INPART = B.PTPNO

--��s�{(�D�]�p)�w�g���u�N��w�g�o�ƻs�@ 2021/04/15 Techup
DELETE #TEMP_��
FROM #TEMP_�� A,(SELECT distinct ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDFNO FROM ORDDE4  A,#SOPNAME B WHERE ORDSQ2 > 0 AND A.ORDFO = B.PRDOPNO AND B.PRDTYPE = '2' AND A.ORDFCO = 'Y' ) B
WHERE A.INPART = B.ORDFNO AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2


-- EXEC dbo.����ORDE3�Ѿl�s�{ ''


--�|���ʮ�
INSERT INTO ORDDE4_�Ѿl�s�{����_����_D
SELECT ORDTP, ORDNO, ORDSQ, ORDSQ1, A.ORDSQ2,A.ORDSQ3,�ήɶ���ORDSQ2, A.INPART, ORDSNO, ORDFO, ORDQY2, ORDDTP, ORDFM1,
ORDUPR, ORDFCO, PRDNAME, ORDAMT, DLYTIME, ORDDY1, ORDDY2, ORDDY4,ORDDY5, MP5CODE, SOPKIND,
�ثe�Ƶ{����,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�, A1DLYTIME = 0, PRDATE1, PRTFM, WKNO, DEPTNO,
Applier,CONVERT(varchar(10), '') �U���驵��Ƶ� ,CARDNO,'',NULL,NULL,NULL,NULL,NULL,NULL,0,NULL,NULL
FROM #TEMP_�� A
LEFT OUTER JOIN (SELECT distinct A.INDWG,A.PUPRP FROM PURTD A,PURTM B WHERE A.PURNO = B.PURNO AND B.SCTRL <> 'X') B ON A.INPART = B.PUPRP
WHERE ISNULL(PUPRP,'') = ''



--SELECT * FROM #TEMP_��
--WHERE INPART = '20M01137-0-000'
--EXEC dbo.����ORDE3�Ѿl�s�{ '20M01137-0-000'

SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,ORDSQ2),�s�d�إߤ� = CAST('' AS datetime),*
INTO #����zDLYTIME_B FROM ORDDE4_�Ѿl�s�{����_����_D WHERE 1 = 0
       SELECT ID = CAST(0 AS INT)  , TIME1 =CAST('' AS datetime),TIME2 = CAST('' AS datetime),MM = CAST(0 AS INT)
       INTO #����zDLYTIME_NEW_B FROM #����zDLYTIME_B WHERE 1 = 0


----��U�H�U�覡
----�p��DLYTIME �q�s�d�C�L�줵��}�l�p��
--UPDATE ORDDE4_�Ѿl�s�{����_����_D
--SET DLYTIME=dbo.�ɶ��t_�̤W�Z�ɶ�(ISNULL(B.AMDDATE,B.CRDATE),GETDATE(),@DLYTIME�C��u�@�p��)/60.00
--FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDMENO B
--WHERE ORDSQ2 < 0 AND A.INPART = B.INPART AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
--AND A.ORDSQ1 = B.ORDSQ1 AND ISNULL(A.Applier,'') NOT IN (SELECT MAHNO FROM MACPRD)


INSERT INTO  #����zDLYTIME_B
SELECT ID = ROW_NUMBER() OVER (ORDER BY A.INPART,ORDSQ2),ISNULL(B.AMDDATE,B.CRDATE) �s�d�إߤ�,A.*
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDMENO B
WHERE ORDSQ2 < 0 AND ORDSQ2 NOT IN ('-500','-1000')
AND A.INPART = B.INPART AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1 AND ISNULL(A.Applier,'') NOT IN (SELECT MAHNO FROM MACPRD)
AND A.INPART LIKE @INPART

INSERT INTO #����zDLYTIME_NEW_B
SELECT ID,TIME1 = �s�d�إߤ�,TIME2 = GETDATE(),MM = CAST(0 AS INT) FROM #����zDLYTIME_B

EXEC [dbo].[�ɶ������t_�̤W�Z�ɶ�] @DLYTIME�C��u�@�p��,'#����zDLYTIME_NEW_B'
   
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = B.MM/60.00
FROM #����zDLYTIME_B A,#����zDLYTIME_NEW_B B,ORDDE4_�Ѿl�s�{����_����_D C
WHERE A.ID = B.ID AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2

DROP TABLE #����zDLYTIME_NEW_B
DROP TABLE #����zDLYTIME_B





---- 2022/09/20 -----------------------------------------------------------------------------------------------------------
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME=dbo.�ɶ��t_�̤W�Z�ɶ�(ISNULL(B.AMDDATE,B.CRDATE),GETDATE(),C.MASTM)/60.00
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDMENO B ,(SELECT MAHNO,MASTM/60 MASTM FROM MACPRD) C
WHERE ORDSQ2 < 0 AND ORDSQ2 NOT IN ('-500','-1000')
AND A.INPART = B.INPART AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO
AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND ISNULL(A.Applier,'') IN (SELECT MAHNO FROM MACPRD)
AND C.MAHNO = A.Applier
AND A.INPART LIKE @INPART



---2019/04/10
--����X�C�@�i�s�d���Ĥ@���D�]�p�s�{��Z�s�{��lo���Ĥ@���s�{
SELECT ORDTP,ORDNO,ORDSQ,ORDSQ1,MIN(ORDSQ2)ORDSQ2,INPART
INTO #ORDDE4_�Ѿl�s�{����_����_D_�Ĥ@���s�{
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE PRDNAME NOT LIKE '��%' AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT LIKE 'lo%' AND PRDNAME NOT LIKE 'SC' AND SOPKIND NOT IN ('�]�p','�~�s')
AND INPART LIKE @INPART
GROUP BY ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART
ORDER BY INPART,ORDSQ2

--���C�@�i�s�d���ƶO��
SELECT * INTO #���ƶO�D�ƻs�{����
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE ORDAMT > 10 AND PRDNAME NOT LIKE '��%'
AND INPART LIKE @INPART


--����s���ƶO���B
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET ORDAMT = 0
WHERE PRDNAME NOT LIKE '��%' AND ORDAMT <= 10
AND INPART LIKE @INPART

SELECT A.*
INTO #INPART�Ĥ@���s�{����
FROM ORDDE4_�Ѿl�s�{����_����_D A , #ORDDE4_�Ѿl�s�{����_����_D_�Ĥ@���s�{ B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2 AND A.INPART = B.INPART
ORDER BY A.INPART,A.ORDSQ2




UPDATE #INPART�Ĥ@���s�{���� SET ORDAMT = B.ORDAMT
FROM #INPART�Ĥ@���s�{���� A,#���ƶO�D�ƻs�{���� B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.INPART = B.INPART

UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET ORDAMT = B.ORDAMT
FROM ORDDE4_�Ѿl�s�{����_����_D A,#INPART�Ĥ@���s�{���� B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2 AND A.INPART = B.INPART

---2019/04/10





--�s�W��Ƶ{����  2019/04/18
SELECT A.*
INTO #ORDDE4_�Ѿl�s�{����_����_D
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B
WHERE ORDFCO = 'N' --AND
--B.CRDATE >='2020/01/01'
--AND SOPKIND NOT IN ('�]�p','�~�s','�u�t�u�{')
AND SOPKIND NOT IN ('�]�p','�~�s','�u�t�u�{')
AND A.INPART = B.INPART
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 ---2024/09/21 Techup ADD
AND B.INFIN = 'N' AND PRDNAME <> '��' AND B.LINE <> 'Z'
AND A.INPART LIKE @INPART
ORDER BY INPART,ORDSQ2


--�����x�����[�Ƶ{
SELECT A.Applier ���x,min(StartTime)�W����,max(EndTime)�U����,�Ʃw�� = Replace (Assigner, '��z', ''),
A.INPART �s�d,A.INDWG �ϸ�,A.ORDSQ2 �s�{�Ǹ�,A.ORDFO �s�{,A.PRDNAME �s�{�W��,B.ORDFCO �s�{���A�X,Convert(varchar(10),A.PDATE,111) ���,QTY �ƶq,Convert(varchar(10),SetUpTime,111)�Ƶ{��
,SUM(WKTIME)�s�{�u��
INTO #TEMP1
FROM #�����ɶ� A,#ORDDE4_�Ѿl�s�{����_����_D B--,ORDDE4_�Ѿl�s�{����_D C
where
StartTime >= DATEADD(YYYY,-1, GETDATE())
AND A.INPART = B.INPART
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 ---2024/09/21 Techup ADD
AND A.ORDSQ2 = B.ORDSQ2
AND �H�ξ��x = 1 AND Remark <> '16'
--AND B.INPART = C.INPART AND B.ORDSQ2 = C.�b���s�{��
---AND (�۰ʱƵ{�s�� <> 'Q' OR (�۰ʱƵ{�s�� = 'Q' AND A.PRDNAME IN ('TP','HTP'))) ----�Ȯ����� 2025/07/24 Techup
AND A.PRDNAME NOT LIKE '3Q%' ---2025/06/10 Techup 3Q���n�i�h�]
--AND B.ORDFCO = 'N' ---�|���������~�X�{ 2025/07/24 Techup
GROUP BY A.Applier,Remark,Replace (Assigner, '��z', ''),A.INPART,A.ORDFO,A.PRDNAME,B.ORDFCO,A.PDATE,A.INDWG,A.ORDSQ2,QTY,Convert(varchar(10),SetUpTime,111)
ORDER BY �W����



SELECT ROW_NUMBER() OVER(Partition By A.���x ORDER BY �W����) AS ROWID,A.*,�ثe�Ƶ{���� = CAST('' AS varchar(10)),CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�
INTO #��X���
FROM #TEMP1 A
ORDER BY ���x,�W����
 

--Partition By �ϸ�,�s�{�W��

SELECT ROW_NUMBER() OVER(Partition By ���x ORDER BY ROWID) AS �էO,���x,ROWID,���,�ϸ�,�s�{�W��,�ثe�Ƶ{����
,CONVERT(datetime, '') �ثeA1�Ƶ{���ǫإߤ�
INTO #��X���1
FROM #��X���
--WHERE �s�d = '20C03105-000#161'
--WHERE �s�{���A�X = 'N'
ORDER BY �W����

----EXEC dbo.����ORDE3�Ѿl�s�{ ''
----2024/09/21 Techup ADD
CREATE CLUSTERED INDEX #��X���1_Index1 ON #��X���1 (�էO,���x,ROWID,���,�ϸ�,�s�{�W��,�ثe�Ƶ{����)


------EXEC dbo.����ORDE3�Ѿl�s�{ '' ---2025/07/24 Techup
--SELECT 'CCC',* FROM #��X���
--WHERE �s�d = '25K03169AF-031'

--SELECT 'DDD',* FROM #��X���1
--WHERE �ϸ� = '0043-06288' AND �s�{�W�� = 'Lv'


DECLARE  @�էO INT
DECLARE  @�ϸ�  VARCHAR(30)
DECLARE  @���  VARCHAR(30)
DECLARE  @�s�{�W�� VARCHAR(10)
DECLARE  @�U���ϸ�  VARCHAR(30)
DECLARE  @�U���s�{�W�� VARCHAR(10)
DECLARE  @�U�����  VARCHAR(10)
DECLARE  @�ܧ� VARCHAR(10)
DECLARE  @���x  VARCHAR(30)

DECLARE  @NCNT INT -- LOOP �p��

SET @NCNT = 1

SELECT distinct ���x INTO #�B�z���x FROM #��X���1

WHILE (SELECT COUNT(*) FROM #�B�z���x) > 0
BEGIN
SET @���x = (SELECT TOP 1 ���x FROM #�B�z���x ORDER BY ���x)

SELECT �էO,���x,�ϸ�,���,�s�{�W�� INTO #TEMP FROM #��X���1 WHERE ���x = @���x



SELECT ROW_NUMBER() OVER (ORDER BY A.�էO) AS �s�էO,A.*
INTO #TEMP_�U���x�ϸ�����̤p�էO
FROM #TEMP A,(SELECT MIN(�էO) �էO, ���x,�ϸ�,���,�s�{�W��
FROM #TEMP GROUP BY ���x,�ϸ�,���,�s�{�W��) B
WHERE A.�էO = B.�էO AND A.���x = B.���x AND  A.�ϸ� = B.�ϸ� AND A.��� = B.��� AND A.�s�{�W�� = B.�s�{�W��
ORDER BY A.�էO

--SELECT * FROM #TEMP_�U���x�ϸ�����̤p�էO

UPDATE #��X���1
SET �ثe�Ƶ{���� = 'A'+CONVERT(varchar, B.�s�էO)
FROM #��X���1 A,#TEMP_�U���x�ϸ�����̤p�էO B
WHERE A.���x = B.���x AND  A.�ϸ� = B.�ϸ� AND A.��� = B.��� AND A.�s�{�W�� = B.�s�{�W��

UPDATE #��X���1 SET
�ثeA1�Ƶ{���ǫإߤ� = GETDATE()
FROM #��X���1 A,#TEMP_�U���x�ϸ�����̤p�էO B
WHERE A.���x = B.���x AND  A.�ϸ� = B.�ϸ� AND A.��� = B.��� AND A.�s�{�W�� = B.�s�{�W��
AND �ثe�Ƶ{���� = 'A1'

--WHERE �ϸ� = @�ϸ� AND �s�{�W�� = @�s�{�W�� AND ���x = @���x AND �ثe�Ƶ{���� = 'A1' AND �էO = @�էO AND ��� = @���

--WHILE (SELECT COUNT(*) FROM #TEMP) > 0
--BEGIN
-- SET @�էO =  (SELECT TOP 1 �էO FROM #TEMP WHERE ���x = @���x ORDER BY �էO)
-- SET @�ϸ� = (SELECT TOP 1 �ϸ� FROM #TEMP WHERE �էO=@�էO AND ���x = @���x ORDER BY �էO)
-- SET @��� = (SELECT TOP 1 ��� FROM #TEMP WHERE �էO=@�էO AND ���x = @���x AND �ϸ� = @�ϸ� ORDER BY �էO,���)
-- SET @�s�{�W�� = (SELECT TOP 1 �s�{�W�� FROM #TEMP WHERE �էO=@�էO AND �ϸ� = @�ϸ� AND ���x = @���x AND ��� = @��� ORDER BY �էO)    

-- --SELECT @�էO �էO,@�ϸ� �ϸ�,@��� ���,@�s�{�W�� �s�{�W��

-- UPDATE #��X���1 SET
-- �ثe�Ƶ{���� = 'A'+CONVERT(varchar, @NCNT) WHERE �ϸ� = @�ϸ� AND �s�{�W�� = @�s�{�W�� AND ���x = @���x AND �էO = @�էO AND ��� = @���

-- UPDATE #��X���1 SET
-- �ثeA1�Ƶ{���ǫإߤ� = GETDATE() WHERE �ϸ� = @�ϸ� AND �s�{�W�� = @�s�{�W�� AND ���x = @���x AND �ثe�Ƶ{���� = 'A1' AND �էO = @�էO AND ��� = @���

-- --SELECT @�էO,@�ϸ�,@�s�{�W��,@���,* FROM #��X���1

-- DELETE FROM #TEMP WHERE �ϸ� = @�ϸ� AND �s�{�W�� = @�s�{�W�� AND �էO = @�էO AND ���x = @���x AND ��� = @���

-- SET @�U���ϸ� = (SELECT TOP 1 �ϸ� FROM #TEMP WHERE �էO=@�էO+1 AND ���x = @���x AND ��� = @��� ORDER BY �էO)
-- SET @�U���s�{�W�� = (SELECT TOP 1 �s�{�W�� FROM #TEMP WHERE �էO=@�էO+1 AND �ϸ� = @�ϸ�  AND ���x = @���x AND ��� = @��� ORDER BY �էO)
--       SET @�U����� = (SELECT TOP 1 ��� FROM #TEMP WHERE �էO=@�էO+1 AND �ϸ� = @�ϸ�  AND ���x = @���x AND ��� = @��� ORDER BY �էO)

-- IF @�U���ϸ� <> @�ϸ� OR @�U���s�{�W�� <> @�s�{�W�� OR @�U����� <> @���
--   SET @NCNT = @NCNT +1
--END
----���s�k�s
--SET @NCNT = 1
DELETE #�B�z���x WHERE ���x = @���x
DROP TABLE #TEMP
DROP TABLE #TEMP_�U���x�ϸ�����̤p�էO
END






UPDATE #��X���
SET �ثe�Ƶ{���� = B.�ثe�Ƶ{����,�ثeA1�Ƶ{���ǫإߤ� = B.�ثeA1�Ƶ{���ǫإߤ�
FROM #��X��� A,#��X���1 B
WHERE A.ROWID = B.ROWID
AND A.���x = B.���x

--SELECT 'CCCCC',* FROM #��X���

---�p��ثeA1�Ƶ{���ǫإߤ� Techup 2020/06/08




--SELECT A.*,B.�ثe�Ƶ{����
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثe�Ƶ{���� = B.�ثe�Ƶ{����,�ثeA1�Ƶ{���ǫإߤ� = B.�ثeA1�Ƶ{���ǫإߤ�,Applier = B.���x
FROM ORDDE4_�Ѿl�s�{����_����_D A,#��X��� B
WHERE A.INPART = B.�s�d AND A.ORDSQ2 = B.�s�{�Ǹ� AND A.ORDFO = B.�s�{ AND A.ORDSQ3 = 0
--AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2

--�P�_�@
--�p�G���s���J�����ǩM�O�d�쥻�����Ǥ��P �h�έ��s���J�����D 2020/06/08 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثeA1�Ƶ{���ǫإߤ� = A.�ثeA1�Ƶ{���ǫإߤ�
FROM ORDDE4_�Ѿl�s�{����_����_D A,#�O��ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDFO = B.ORDFO AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2
AND A.�ثe�Ƶ{���� <> B.�ثe�Ƶ{����
AND A.ORDSQ3 = 0

--�p�G���s���J�����ǩM�O�d�쥻�����ǬۦP �h�έ쥻���ɶ����D 2020/06/08 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثeA1�Ƶ{���ǫإߤ� = B.�ثeA1�Ƶ{���ǫإߤ�
FROM ORDDE4_�Ѿl�s�{����_����_D A,#�O��ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDFO = B.ORDFO
AND A.�ثe�Ƶ{���� = B.�ثe�Ƶ{���� --AND A.�ثe�Ƶ{���� = 'A1'
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2
AND A.ORDSQ3 = 0










    ----2023/03/25  �v --------------------------------------------------------------------------------------------------------------------------------
-- UPDATE ORDDE4_�Ѿl�s�{����_����_D
-- SET �ثeA1�Ƶ{���ǫإߤ� = NULL
-- WHERE �ثeA1�Ƶ{���ǫإߤ� = '1900-01-01 00:00:00.000'

----���إ߼Ȧs
--SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,ORDSQ2),* INTO #����zDLYTIME_C FROM ORDDE4_�Ѿl�s�{����_����_D WHERE 1 = 0
--SELECT ID = CAST(0 AS INT)  , TIME1 =CAST('' AS datetime),TIME2 = CAST('' AS datetime),MM = CAST(0 AS INT)
--INTO #����zDLYTIME_NEW_C FROM #����zDLYTIME_C WHERE 1 = 0

-- --UPDATE ORDDE4_�Ѿl�s�{����_����_D
-- --SET A1DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(�ثeA1�Ƶ{���ǫإߤ�,GETDATE(),@DLYTIME�C��u�@�p��)/60.00
-- --WHERE ISNULL(�ثeA1�Ƶ{���ǫإߤ�,'') <> ''
-- -----�p��ثeA1�Ƶ{���ǫإߤ� Techup 2020/06/08

--INSERT INTO  #����zDLYTIME_C
--SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,ORDSQ2),* FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ISNULL(�ثeA1�Ƶ{���ǫإߤ�,'') <> ''
--INSERT INTO #����zDLYTIME_NEW_C
--SELECT ID,TIME1 = �ثeA1�Ƶ{���ǫإߤ�,TIME2 = GETDATE(),MM = CAST(0 AS INT) FROM #����zDLYTIME_C --ORDER BY ORDFNO,ORDSQ2

--EXEC [dbo].[�ɶ������t_�̤W�Z�ɶ�] @DLYTIME�C��u�@�p��,'#����zDLYTIME_NEW_C'
   
--UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = B.MM/60.00
--FROM #����zDLYTIME_C A,#����zDLYTIME_NEW_C B,ORDDE4_�Ѿl�s�{����_����_D C
--WHERE A.ID = B.ID AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2

--DROP TABLE #����zDLYTIME_NEW_C
--DROP TABLE #����zDLYTIME_C

----2023/03/25  �v --------------------------------------------------------------------------------------------------------------------------------




--
--�s�W��Ƶ{����  2019/04/18
--���ƪ����O�DA1�� 2020/06/05 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET PRDNAME = '��'+PRDNAME
WHERE ISNULL(�ثe�Ƶ{����,'') <> ''
AND �ثe�Ƶ{���� <> 'A1'
AND INPART LIKE @INPART


--���X�U���x�̤p�DA1�����~�A�U�Ƶ{���Ǫ��Ĥ@�ӻs�d�s�{ Techup 2020/06/17
SELECT
ROW_NUMBER() Over (Partition By A.Applier,�ثe�Ƶ{���� Order By A.Applier,CAST (REPLACE(�ثe�Ƶ{����,'A','') AS INT ),StartTime) As Sort,
A.Applier,�ثe�Ƶ{����,DLYTIME,B.INDWG,A.INPART,A.ORDFO, PRDNAME,A.ORDSQ2,ORDFM1
INTO #��s�DA1���s�d���O
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,(SELECT Applier,MIN(StartTime)StartTime,INPART,ORDFO,ORDSQ2,INDWG FROM #�����ɶ� GROUP BY Applier,INPART,ORDFO,ORDSQ2,INDWG) C
WHERE ISNULL(�ثe�Ƶ{����,'') <> '' AND ISNULL(A.Applier,'') <> ''
AND A.INPART = B.INPART
AND A.Applier = C.Applier AND A.INPART = C.INPART AND A.ORDFO = C.ORDFO AND A.ORDSQ2 = C.ORDSQ2
AND CAST (REPLACE(�ثe�Ƶ{����,'A','') AS INT ) > 1
AND A.INPART LIKE @INPART
ORDER BY A.Applier,CAST (REPLACE(�ثe�Ƶ{����,'A','') AS INT ),C.StartTime

--UPDATE ORDDE4_�Ѿl�s�{����_����_D SET PRDNAME = REPLACE(B.PRDNAME,'��','')
----SELECT *
--FROM #��s�DA1���s�d���O A,ORDDE4_�Ѿl�s�{����_����_D B
--WHERE Sort > 1
--AND A.INPART = B.INPART AND A.ORDFO = B.ORDFO AND A.ORDSQ2 = B.ORDSQ2
----ORDER BY A.Applier,CAST (REPLACE(A.�ثe�Ƶ{����,'A','') AS INT ),Sort
----���X�U���x�̤p�DA1�����~�A�U�Ƶ{���Ǫ��Ĥ@�ӻs�d�s�{ Techup 2020/06/17
---- �PA1 ���s�d���@�_��ܡ�  Techup 2020/06/20


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �U���驵��Ƶ� = '��'
WHERE --INPART = '20F01057-0-000' AND
ORDFCO = 'N' AND ISNULL(�ثe�Ƶ{����,'') <> ''
AND ORDDY2 <= GETDATE()
AND INPART LIKE @INPART


--EXEC dbo.����ORDE3�Ѿl�s�{ ''

--�s�P#TEMP3�]�@�_�ץ����ƪ����O�DA1�� 2020/06/05 Techup
UPDATE #TEMP3
SET PRDNAME = B.PRDNAME
FROM #TEMP3 A,ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDTP = B.ORDTP
AND ISNULL(B.�ثe�Ƶ{����,'') <> ''
AND B.�ثe�Ƶ{���� <> 'A1'


--��s�{(�D�]�p)�w�g���u�N��w�g�o�ƻs�@ 2021/04/15 Techup
UPDATE #�ƶO����
SET ORDFCO = 'Y'
FROM #�ƶO���� A,(SELECT distinct ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDFNO FROM ORDDE4  A,#SOPNAME B WHERE ORDSQ2 > 0 AND A.ORDFO = B.PRDOPNO AND B.PRDTYPE = '2' AND A.ORDFCO = 'Y' ) B
WHERE A.INPART = B.ORDFNO AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 --AND A.ORDSQ2 = B.ORDSQ2
AND A.ORDSQ2 = 0 --A.ORDFO LIKE '%��%'



---- 2021/04/20
UPDATE #�ƶO����  
SET ORDFCO = 'Y'
FROM #�ƶO���� A,(SELECT distinct PTPNO FROM PRODTM A,#SOPNAME B WHERE PTPSQ >0 AND A.PRTFO = B.PRDOPNO AND B.SOPKIND NOT IN ('�]�p','�䥦','�䥦1')) B
WHERE A.INPART = B.PTPNO



INSERT INTO #TEMP3
SELECT * FROM #�ƶO����







 --�R���w�g�����u���s�d
 --DELETE #TP4
  INSERT INTO #�n�R�����s�d
  SELECT A.INPART  FROM  
  (SELECT INPART,COUNT(*) SQ FROM #TEMP3 GROUP BY INPART ) A LEFT OUTER JOIN
  (SELECT INPART,COUNT(*) SQ FROM #TEMP3 WHERE ORDFCO IN ('Y','C') GROUP BY INPART ) B
  ON A.INPART = B.INPART AND A.SQ = B.SQ
  WHERE ISNULL(B.INPART,'') <> ''

  DELETE #�n�R�����s�d
    FROM #�n�R�����s�d A,(SELECT DISTINCT INPART FROM #RST) B
   WHERE A.INPART =  B.INPART

  --SELECT * FROM #TEMP3
  --ORDER BY INPART


   --EXEC dbo.����ORDE3�Ѿl�s�{_T '19L09854L-000#2R1'

 

           DELETE #TEMP3
  WHERE INPART IN (SELECT INPART FROM #�n�R�����s�d)


--�[�J���
  ALTER TABLE #TEMP3 ADD ROWID INT


  --���s��z�s�{�Ǹ�
  SELECT INPART,ORDSQ2,ROWID = ROW_NUMBER() OVER (PARTITION BY INPART ORDER BY INPART,ORDSQ2) INTO #��z�s�{�� FROM #TEMP3  

  UPDATE #TEMP3 SET ROWID = A.ROWID
  FROM #��z�s�{�� A,#TEMP3 B
  WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2

  --���X�̤p�U�s�d�̤p���s�{N
  SELECT INPART,MIN(ROWID) ROWID INTO #TP4_1 FROM #TEMP3 WHERE ORDFCO = 'N' GROUP BY INPART
 


  --SELECT 'AAA',* FROM #TEMP3  WHERE INPART LIKE '19L09854L-000#2R1%'


--  --2021/11/30 �Ȯ����� Techup--------------------------------------------------------
---- GM�n�D 72�p�ɤ��e�������  2021/04/21 Techup
--DELETE #TEMP3 WHERE PRTFM < DATEADD(HH,-72,  convert(varchar, GETDATE(), 111)) AND ORDFCO <> 'N'
--AND INPART NOT IN (SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`��)
----�Ȯ����� ����ܫe�T�� �A���72�p�ɳ��u���᪺��� 2021/04/21 Techup
--  --�u�d�̤p���s�{N���e��Y

----���`�椧�e�s�{ �u�d�e�T�� 2021/05/31 Techup
--DELETE #TEMP3 FROM #TP4_1 A,#TEMP3 B WHERE A.INPART = B.INPART AND A.ROWID-3 > B.ROWID
--AND B.INPART IN (SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`��)
----�w���u�B�~�s��   2021/04/21 Techup
--DELETE #TEMP3 WHERE (ORDFCO <> 'N' AND ORDDTP IN ('2','4')) OR (PRDNAME LIKE 'QC%' AND ORDFCO = 'C')
----2021/11/30 �Ȯ����� Techup--------------------------------------------------------





---SELECT 'DDDD',* FROM #TEMP3  WHERE INPART LIKE '19L09854L-000#2R1%'

--�R�����
      --alter table #TEMP2 drop column ROWID
 
  --EXEC dbo.����ORDE3�Ѿl�s�{ '20H00158-0-101B-E2-1'
-- SELECT 'AAA',* FROM #TEMP3

--SELECT 'BBB',MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND SOPKIND NOT IN ('�]�p')
-- AND PRDNAME NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE DESCR LIKE '%�O%' AND PRDNAME <> 'AT') --�O�����N����
-- AND ORDFCO = 'N' AND A.INPART =  '20H00158-0-101B-E2-1' GROUP BY INPART




SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,ORDSQ2,INPART,ORDFO ,A.ORDQTY,B.ORDQY2,B.ORDDTP,B.ORDFM1
,B.ORDUPR,B.ORDAMT,B.DLYTIME,B.ORDDY4,B.ORDFCO
INTO #TOT2
FROM ORDE3 A,ORDDE4  B
WHERE A.ORDTP = B.ORDTP
AND A.ORDNO = B.ORDNO
AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1
AND INPART IN (SELECT INPART FROM #TEMP3)
AND ORDQY2 > 0 --2020/11/09 �L�Ͳ��Ƥ��ݭn�a�JTechup
AND ORDFCO <> 'C' --2024/08/21 C���������
ORDER BY INPART,ORDSQ2




--SELECT * FROM #�s�d���� WHERE INPART = '19D03574AF-013'
--SELECT * FROM #TOT2 WHERE ORDQY2 <=0
--order by INPART,ORDSQ2


--EXEC dbo.����ORDE3�Ѿl�s�{ '22K01115AF-0-000'


SELECT A.*,
--2019/05/14 GM �n�D�ݨ��󪺤u�ɻs�{
���u�� = ISNULL((CASE WHEN ORDDTP = '1' AND SOPKIND NOT IN ('�]�p') AND DESCR NOT LIKE '%�ը�%' AND ORDFM1 > 0 AND ORDQY2 > 0 THEN
(SELECT [dbo].[GetDecimal](ORDFM1/ORDQY2,100)) ELSE ORDFM1 END ),0)
,B.PRDNAME ,B.SOPKIND
INTO #TOT3
FROM #TOT2 A,#SOPNAME B
WHERE A.ORDFO = B.PRDOPNO
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%' OR SOPKIND = '�|��')
AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') ---�W�U�Ƭ[ ���p�����O
AND PRDTYPE <> '4' ---�O���������p�� 2025/05/27 ���� Techup
ORDER BY INPART,ORDSQ2


--SELECT * INTO #TOT3_�� FROM #TOT3 WHERE ORDAMT > 0
--UPDATE #TOT3_�� SET ORDSQ2 = 0 , SOPKIND = '��',ORDFO = '��', PRDNAME = '��' WHERE ORDAMT > 0
--INSERT INTO #TOT3
--SELECT * FROM #TOT3_��
--UPDATE #TOT3 SET ORDAMT = 0 WHERE ORDSQ2 > 0 AND ORDAMT > 0

--CASE WHEN ORDAMT > 0 AND ORDQY2 > 0  THEN '��'+'('+cast(CONVERT(int, ORDAMT/ORDQY2) AS NVARCHAR(30) )+')'  +'��' ELSE '' END

--���ƪ� �n�D �R�m �u�{�v�w��u�ɦh�ִN�Φh�֭p�� 2025/07/03 Vivian
--�̫�Mĳ ��ιw��u�� > 24hr �h��24hr 2024/08/14 Techup
--���ƪ� �n�D �R�m �u�{�v�w��u�ɦh�ִN�Φh�֭p�� 2024/07/25 Techup
--���ƪ� �n�D �R�m < 1 �ѴN�ιw��u�ɭp�� �Ϥ��Τ@�� 2024/07/23 Techup
--GM �n�D�R�m�s�{ �ܵ�1�� �G��1������ܧY�i 2021/06/10 Techup
--UPDATE #TOT3
--SET ORDFM1 = (CASE WHEN ORDFM1 < 1440 THEN ORDFM1 ELSE '1440' END) ---
--WHERE RTRIM(LTRIM(REPLACE(REPLACE(PRDNAME,'��',''),'��',''))) IN ('IL','F')

--���ƪ� �n�D �R�m �u�{�v�w��u�ɦh�ִN�Φh�֭p�� 2025/07/03 Vivian
--�̫�Mĳ ��ιw��u�� > 24hr �h��24hr 2024/08/14 Techup
--���ƪ� �n�D �R�m �u�{�v�w��u�ɦh�ִN�Φh�֭p�� 2024/07/25 Techup
--���ƪ� �n�D �R�m < 1 �ѴN�ιw��u�ɭp�� �Ϥ��Τ@�� 2024/07/23 Techup
--GM �n�D�R�m�s�{ �ܵ�1�� �G��1������ܧY�i 2021/06/10 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_����_D
--SET ORDFM1 = (CASE WHEN ORDFM1 < 1440 THEN ORDFM1 ELSE '1440' END)--,�~�]�w�p�Ѽ� = '1'
--WHERE RTRIM(LTRIM(REPLACE(REPLACE(PRDNAME,'��',''),'��',''))) IN ('IL','F')
--AND INPART LIKE @INPART


SELECT distinct ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,ORDSNO,NEW_ORDSNO = CAST('' AS varchar(10))
INTO #�s�d����
FROM #TEMP3
ORDER BY INPART


--EXEC dbo.����ORDE3�Ѿl�s�{ ''

--SELECT * FROM #TOT3
--WHERE INPART LIKE '21D01040AF-0-00%'

--�ۦP���@�_C�� �N�R�� 2021/04/15 Techup
DELETE #TOT3
FROM #TOT3 A,(
SELECT INPART,COUNT(*) SQ FROM #TOT3 WHERE REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('DF','DF2') AND ORDFCO = 'C' GROUP BY INPART) B
WHERE A.INPART = B.INPART AND B.SQ > 1 AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('DF','DF2')
--�ۦP���@�_C�� �N�R�� 2021/04/15 Techup
DELETE #TOT3
FROM #TOT3 A,(
SELECT INPART,COUNT(*) SQ FROM #TOT3 WHERE REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('DC','DC2') AND ORDFCO = 'C' GROUP BY INPART) B
WHERE A.INPART = B.INPART AND B.SQ > 1 AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('DC','DC2')
--�ۦP���@�_C�� �N�R�� 2021/04/15 Techup
DELETE #TOT3
FROM #TOT3 A,(
SELECT INPART,COUNT(*) SQ FROM #TOT3 WHERE REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('DG','DG2') AND ORDFCO = 'C' GROUP BY INPART) B
WHERE A.INPART = B.INPART AND B.SQ > 1 AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('DG','DG2')

--SELECT INPART,PRDNAME,ORDFCO FROM #TOT3 WHERE (PRDNAME = 'DC' AND ORDFCO = 'C') OR (PRDNAME = 'DC2' AND ORDFCO = 'C')

--SELECT * FROM #TOT3
--WHERE INPART LIKE '21D01040AF-0-00%'



----�ۦP���@�_C�� �N�R�� 2021/04/15 Techup
--DELETE #TOT3 WHERE PRDNAME = 'DC' AND PRDNAME = 'DC2' AND ORDFCO = 'C'
----�ۦP���@�_C�� �N�R�� 2021/04/15 Techup
--DELETE #TOT3 WHERE PRDNAME = 'DG' AND PRDNAME = 'DG2' AND ORDFCO = 'C'


IF @INPART = '%'
BEGIN
--SELECT * FROM #�s�d����
SELECT distinct INPART INTO #AutoPc�����ɶ� FROM #�����ɶ�
WHERE INPART IN (SELECT INPART FROM #�s�d����)
AND (Assigner LIKE '%AutoPc%' OR ISNULL(�۰ʱƵ{�s��,'') <> '')
AND �H�ξ��x = 1
END


--SELECT  ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART FROM #�s�d����
--GROUP BY ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART
--HAVING COUNT(*) > 1





--SELECT * FROM �Ѿl�s�{����

--SELECT * FROM ORDDE4_�Ѿl�s�{����_D
--WHERE INPART = '21G01008ML-1-001-008-001#6'
--ORDER BY INPART,ORDSQ2

--���ƪ� �n�D �R�m �u�{�v�w��u�ɦh�ִN�Φh�֭p�� 2025/07/03 Vivian
--GM �n�D�R�m�s�{ �ܵ�1�� �G��1������ܧY�i 2021/06/10 Techup
--UPDATE #TEMP3
--SET ORDFM1 = (CASE WHEN ORDFM1 < 1440 THEN ORDFM1 ELSE '1440' END) ---'1440'--,�~�]�w�p�Ѽ� = '1'
--WHERE RTRIM(LTRIM(REPLACE(REPLACE(PRDNAME,'��',''),'��',''))) IN ('IL','F')

-- EXEC dbo.����ORDE3�Ѿl�s�{ '23G05086SL-000'

    UPDATE #TEMP3
SET ORDFCO = 'Y'
FROM #TEMP3 A,PURIND B,PURINM C,PURDEL D,PURMAS E,PURTD F,PURTM G
WHERE --A.INPART = '22M00102-0-K10' AND
A.INPART = B.INPART AND B.PUINO = C.PUINO AND C.SCTRL <> 'X'
AND D.PURNO = E.PURNO AND E.SCTRL = 'Y'
AND B.PURNO = D.PURNO AND B.PURSQ = D.PURSQ
AND D.PTDNO = F.PURNO AND D.PTDSQ = F.PURSQ
AND F.PURNO = G.PURNO AND F.SCTRL = 'Y'
AND A.ORDSQ2 BETWEEN F.PUPA1 AND F.PUPA2  
AND A.ORDSQ2 <> 0 ----2023/09/19 �����Ƥ����� �ƬO�n�o�Ƥ~���� Techup

----2023/12/06 �v �N�S�������i�Ӫ��~�]�h�^�h
--UPDATE #TEMP3
--SET ORDFCO = 'N'
--FROM #TEMP3 A,PURIND B,PURINM C,PURDEL D,PURMAS E,PURTD F,PURTM G, ORDDE4  H
--WHERE --A.INPART = '22M00102-0-K10' AND
--A.INPART = B.INPART AND B.PUINO = C.PUINO AND C.SCTRL <> 'X'
--AND D.PURNO = E.PURNO AND E.SCTRL = 'Y'
--AND B.PURNO = D.PURNO AND B.PURSQ = D.PURSQ
--AND D.PTDNO = F.PURNO AND D.PTDSQ = F.PURSQ
--AND F.PURNO = G.PURNO AND F.SCTRL = 'Y'
--AND A.ORDSQ2 BETWEEN F.PUPA1 AND F.PUPA2  
--AND A.ORDSQ2 <> 0 ----2023/09/19 �����Ƥ����� �ƬO�n�o�Ƥ~���� Techup
--AND A.INPART = H.ORDFNO AND A.ORDSQ2 = H.ORDSQ2 AND H.ORDQY2 > (H.ORDQY4 + H.ORDQY5) ---- �p�G�ƶq�M�����Ƥ��P �N��i�f���ƶq�٨S���������

--SELECT * FROM PURTM
--WHERE PURNO >='2309'
-- PURTP


 --   -- EXEC dbo.����ORDE3�Ѿl�s�{ '23M01079-0-000'
--SELECT 'AAA',* FROM #TEMP3
--WHERE INPART = '23M01079-0-000'


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET ORDFCO = 'Y'
FROM ORDDE4_�Ѿl�s�{����_����_D A,PURIND B,PURINM C,PURDEL D,PURMAS E,PURTD F,PURTM G
WHERE --A.INPART = '22M00102-0-K10' AND
A.INPART = B.INPART AND B.PUINO = C.PUINO AND C.SCTRL <> 'X'
AND D.PURNO = E.PURNO AND E.SCTRL = 'Y'
AND B.PURNO = D.PURNO AND B.PURSQ = D.PURSQ
AND D.PTDNO = F.PURNO AND D.PTDSQ = F.PURSQ
AND F.PURNO = G.PURNO AND F.SCTRL = 'Y'
AND A.ORDSQ2 BETWEEN F.PUPA1 AND F.PUPA2
AND A.INPART LIKE @INPART
AND A.ORDSQ2 <> 0 ----2023/09/19 �����Ƥ����� �ƬO�n�o�Ƥ~���� Techup

----�S��B�z�o�@�i�s�d 2022/12/23
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET ORDFCO = 'N' WHERE INPART = '22G06084SL-001-001' AND ORDSQ2 <= 1 AND INPART LIKE @INPART
UPDATE #TEMP3 SET ORDFCO = 'N' WHERE INPART = '22G06084SL-001-001' AND ORDSQ2 <= 1



UPDATE #TEMP3
SET PRDNAME = CHAR(10)+PRDNAME --�s�{�_��
FROM #TEMP3 A,
(SELECT MIN(ORDSQ2) �s�{��,ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART FROM #TEMP3 A
WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND SOPKIND NOT IN ('�]�p')
AND PRDNAME NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE DESCR LIKE '%�O%' AND PRDNAME <> 'AT') --�O�����N����
AND ORDFCO = 'N' GROUP BY INPART,ORDTP,ORDNO,ORDSQ,ORDSQ1) B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�s�{��
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1  --2024/09/21 Techup ADD
--ORDER BY A.INPART,ORDSQ2

   

--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE INPART = '21Q01012-0-001-001'
--AND ORDFCO <> 'N' AND ISNULL(PRTFM,'') <> ''

--�s�W�t���Ѽ���� ��K���Ѽ� Techup 2021/04/21
ALTER TABLE #TEMP3 ADD �t���Ѽ� VARCHAR(10);

UPDATE #TEMP3 SET �t���Ѽ� = '['+RIGHT(LEFT(convert(varchar,convert(varchar, PRTFM, 111), 120),13),5)+']�� '
WHERE ORDFCO = 'Y'


--UPDATE #TEMP3 SET �t���Ѽ� = '��'+CONVERT(varchar(5),DATEDIFF (DAY,convert(varchar, PRTFM, 111) , convert(varchar, GETDATE(), 111)) ) +' '
--WHERE ORDFCO = 'Y'
--UPDATE #TEMP3 SET �t���Ѽ� = '' WHERE �t���Ѽ� = '��0'
--UPDATE #TEMP3 SET �t���Ѽ� = '��' WHERE �t���Ѽ� = '��1'
--UPDATE #TEMP3 SET �t���Ѽ� = '����' WHERE �t���Ѽ� = '��2'
--UPDATE #TEMP3 SET �t���Ѽ� = '������' WHERE �t���Ѽ� = '��3'


--��X�Q�Ѱ϶������u�u�� 2021/05/06 Techup
SELECT B.*
INTO #�n�B�̪�PRODTM
FROM #�s�d���� A, PRODTM B
WHERE A.INPART = PTPNO
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 ---2024/09/21 Techup ADD
AND CRDATE >=  convert(varchar, DATEADD(DD,-1,GETDATE()), 111) + ' 00:00'
AND CRDATE <=  convert(varchar, GETDATE(), 111) + ' 00:00'

--SELECT * FROM #�n�B�̪�PRODTM
--WHERE PTPNO = '21G04115ML-000'

--�ѩ�u�� �e��00:00 ����00:00����� �ҥH�|������
UPDATE #�n�B�̪�PRODTM
SET PRTFM = convert(varchar, GETDATE(), 111) + ' 00:00'
WHERE --PTPNO = '20K03547AF-000' AND
PRTFM >= convert(varchar, GETDATE(), 111) + ' 00:00'

--�ѩ�|���������u���N�|�S����o�u�� �h������ΰ_�W���p���@��o�u��
UPDATE #�n�B�̪�PRODTM
SET PRTIME = DATEDIFF(mi,CRDATE,PRTFM)
--WHERE PRTIME = 0
--��X�Q�Ѱ϶������u�u�� 2021/05/06 Techup



---DLYTIME ���SFC3138NET_�������n ����ƨӥ� 2021/06/07 Techup
    UPDATE #TEMP3
SET DLYTIME = (CASE WHEN ��ڰ��n < 0 THEN 0 ELSE ��ڰ��n END)
FROM #TEMP3 A,SFC3138NET_�������n B
WHERE A.INPART = B.�s�d AND ORDSQ2 = B.��



SELECT distinct A.ROWID,ORDDTP,A.INPART,A.ORDFO,A.ORDSQ2,A.ORDSQ3,�ήɶ���ORDSQ2,A.PRDNAME,A.CARDNO,ORDAMT,ORDFM1,A.ORDFCO ,A.ORDUPR,A.DLYTIME,ISNULL(C.�}�橵��ɶ�,A.DLYTIME) ����ɶ�
    INTO #��X�e�����`�檺_�Ѿl�s�{����
FROM #TEMP3 A JOIN ORDDE4_�Ѿl�s�{����_���`�� B ON A.INPART = B.OLDPART AND A.ORDSQ2 <= B.ORDSQ2
    LEFT OUTER JOIN
    (
--SELECT dbo.�ɶ��t_�̤W�Z�ɶ�(CFMDATE,PCDATE,8)/60.00 ����ɶ�,CARDNO,ORDSQ2,PRDNAME,INPART,OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� GROUP BY CARDNO,ORDSQ2,PRDNAME,CFMDATE,PCDATE,INPART,OLDPART
SELECT �}�橵��ɶ�,CARDNO,ORDSQ2,PRDNAME,INPART,OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� GROUP BY �}�橵��ɶ�,CARDNO,ORDSQ2,PRDNAME,CFMDATE,PCDATE,INPART,OLDPART
) C
    ON A.CARDNO = C.CARDNO AND A.ORDSQ2 = C.ORDSQ2 AND A.INPART = C.OLDPART AND A.ORDSQ3 <> 0
WHERE B.OLDPART IN (SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`��)





--SELECT 'AAA',* FROM #TEMP3  WHERE INPART LIKE '19L09854L-000#2R1%'
    --SELECT 'BBB',* FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = '19L09854L-000#2R1#2R1'
     --SELECT 'RST',* FROM #TEMP3  WHERE INPART LIKE '19L09854L-000#2R1%'
    ---SELECT 'BBB',* FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = '19L09854L-000#2R1#2R1'
  --EXEC dbo.����ORDE3�Ѿl�s�{_T '19L09854L-000#2R1#2R1'
  --EXEC dbo.����ORDE3�Ѿl�s�{ ''


----ORDDE4_�Ѿl�s�{����_���`��
--SELECT * FROM #��X�e�����`�檺_�Ѿl�s�{���� WHERE INPART LIKE '19L09854L-000#2R1%'
--ORDER BY INPART,ORDSQ2,ORDSQ3

----ORDDE4_�Ѿl�s�{����_���`��
--SELECT * FROM #�s�d����
--ORDER BY INPART


--SELECT INPART,
-- �}��e�s�d�s�{���� = (
-- SELECT
-- CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'  
-- WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' AND ORDSQ3 = '0' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')/'+CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),����ɶ�)))
-- WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' AND ORDSQ3 > '0' THEN PRDNAME+'/'+cast(CARDNO AS NVARCHAR(10) )+ '(' + CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),����ɶ�))) + ')'
-- ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,0),ORDUPR))) + ')'
-- END
-- + '��'
-- FROM #��X�e�����`�檺_�Ѿl�s�{���� A
-- WHERE A.INPART IN (SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = #�s�d����.INPART
-- )  
-- AND PRDNAME NOT LIKE '%Z%'
-- ORDER BY ROWID
-- FOR XML PATH('')

-- )
-- ----- 2019/02/27
-- FROM #�s�d����
-- ORDER BY INPART


--SELECT distinct OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = #�s�d����.INPART

--SELECT A.INPART,�}��e�s�d = B.OLDPART
----= (
----SELECT distinct OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = #�s�d����.INPART)
--INTO #TEST
--FROM #�s�d���� A,(SELECT distinct OLDPART,INPART FROM ORDDE4_�Ѿl�s�{����_���`�� ) B
--WHERE A.INPART = B.INPART AND B.INPART = '17Y1354-1-001-001#9'
--ORDER BY A.INPART

--SELECT distinct OLDPART FROM ORDDE4_�Ѿl�s�{����_���`��  WHERE INPART = '17Y1354-1-001-001#9'

--SELECT * FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE OLDPART = '19Y03554-000#28R1'

--EXEC dbo.����ORDE3�Ѿl�s�{_T ''


-- SELECT �}��e�s�d, COUNT(*) AS count
--FROM #TEST
--GROUP BY �}��e�s�d
--HAVING (COUNT(*) > 1)

--���ƪ� �n�D �R�m �u�{�v�w��u�ɦh�ִN�Φh�֭p�� 2025/07/03 Vivian
--GM �n�D�R�m�s�{ �ܵ�1�� �G��1������ܧY�i 2021/06/10 Techup
--UPDATE #TEMP3
--SET ORDFM1 = (CASE WHEN ORDFM1 < 1440 THEN ORDFM1 ELSE '1440' END)--'1440'
--WHERE RTRIM(LTRIM(REPLACE(REPLACE(PRDNAME,'��',''),'��',''))) IN ('IL','F')



--SELECT * FROM #TEMP3
--EXEC dbo.����ORDE3�Ѿl�s�{ '21L09043SL-000#23'


SELECT A.IDNO,CASE WHEN A.STATUS ='1' THEN '������X'
WHEN A.STATUS ='2' THEN '���t��X'
WHEN A.STATUS ='3' THEN '���t����'
WHEN A.STATUS ='4' THEN '���Ǳ���'
WHEN A.STATUS ='%' THEN '�X�J�t' END AS STATUS ,�ɶ� = (CASE WHEN A.TTIME IS NULL THEN '' ELSE CONVERT(VARCHAR(20),CONVERT(DATETIME,A.TTIME),120) END)
INTO #�u��B�e����
FROM SFCSWP1 A,(SELECT IDNO,MAX(TTIME) TTIME FROM  SFCSWP1 GROUP BY IDNO) B
WHERE A.TTIME = B.TTIME AND A.IDNO = B.IDNO
AND A.IDNO IN (SELECT INPART FROM #�s�d����)
ORDER BY A.IDNO

--SELECT A.�s�y�s��,A.�ت��s�{�s��,AA = '���t����',A.���f�ɶ�
--INTO #SFC2468NET_��B���
--FROM SFC2468NET_��B��� A ,
--(SELECT  �s�y�s��, �ت��s�{�s��,CRDATE = MAX(CRDATE) FROM SFC2468NET_��B��� WHERE SCTRL = 'Y' GROUP BY �s�y�s��, �ت��s�{�s��) B ,
--PERSON C
--WHERE A.�s�y�s�� = B.�s�y�s��
--AND A.�ت��s�{�s�� = B.�ت��s�{�s��
--AND A.CRDATE = B.CRDATE
--AND A.���f�� = C.PNAME
--AND C.PENNO IN('3505','3200','935','2357','865','2668','1908')


--EXEC dbo.����ORDE3�Ѿl�s�{ '21H03083-000#1'
   
--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE INPART = '21H03083-000#1'

--2021/11/30 Techup �P�B��s �W����z�� ORDDE4_�Ѿl�s�{����_����_D
UPDATE #TEMP3
SET DLYTIME = B.DLYTIME,Applier = B.Applier
FROM #TEMP3 A,ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = B.ORDSQ3

SELECT B.CUSTCA,B.PS �o�]�_,B.PE �o�]��,PURDY �w�p�^�t��,A.* ,
�禬�^�t = CASE WHEN ISNULL(C.INPART,'') <> '' THEN '��' ELSE '' END
INTO #TEMP3_�~�s
FROM #TEMP3 A
LEFT OUTER JOIN (
SELECT C.CUSTCA ,A.PURNO,A.PURSQ,A.INPART,A.INDWG,A.PS,A.PE,A.PURDY FROM PURDEL A,PURMAS B,PCUSTOM C
WHERE A.PURNO = B.PURNO AND B.SCTRL <> 'X' AND B.PURMA = C.CUSTNO AND CUSTTP = (CASE WHEN B.PURAA = 0 THEN '1' ELSE B.PURAA END)
) B ON A.INPART = B.INPART AND A.ORDSQ2 BETWEEN B.PS AND B.PE
LEFT OUTER JOIN
(SELECT A.INPART,A.INDWG,A.PURNO,A.PURSQ FROM PURIND A,PURINM B WHERE A.PUINO = B.PUINO AND B.SCTRL = 'Y' ) C
ON B.PURNO = C.PURNO AND B.PURSQ = C.PURSQ AND B.INPART = C.INPART
WHERE ISNULL(B.INPART,'') <> '' ---�S�Ū�ܦ��o�]
--A.INPART = '22M00114-0-N14'
ORDER BY ORDSQ2


   
--
-- SELECT distinct �w���f��, �X���, NG��,B.CUSTCA,B.PS �o�]�_,B.PE �o�]��,PURDY �w�p�^�t��,A.* ,
-- �禬�^�t = CASE WHEN ISNULL(C.INPART,'') <> '' AND ORDQY2 = �X���  THEN '��' ELSE '' END --�s�d�� = �X��� �~�|���O�� ���u 2022/12/29 Techup
-- INTO #TEMP3_�~�s
-- FROM  #TEMP3 A
-- LEFT OUTER JOIN (
-- SELECT C.CUSTCA ,A.PURNO,A.INPART,A.INDWG,A.PS,A.PE,MAX(A.PURDY) PURDY,SUM(PAQY1) �w���f��,SUM(PAQY2) �X���,SUM(NGQTY) NG��
-- FROM PURDEL A,PURMAS B,PCUSTOM C
--WHERE A.PURNO = B.PURNO AND B.SCTRL <> 'X' AND B.PURMA = C.CUSTNO AND CUSTTP = (CASE WHEN B.PURAA = 0 THEN '1' ELSE B.PURAA END)
--GROUP BY C.CUSTCA ,A.PURNO,A.INPART,A.INDWG,A.PS,A.PE
-- ) B ON A.INPART = B.INPART AND A.ORDSQ2 BETWEEN B.PS AND B.PE
-- LEFT OUTER JOIN
-- (SELECT A.INPART,A.INDWG,A.PURNO,A.PURSQ FROM PURIND A,PURINM B WHERE A.PUINO = B.PUINO AND B.SCTRL = 'Y' ) C
-- ON B.PURNO = C.PURNO  AND B.INPART = C.INPART
-- WHERE ISNULL(B.INPART,'') <> '' ---�S�Ū�ܦ��o�]
-- --AND A.ORDFNO = '22G06084SL-001-001'
-- --AND A.ORDQY2 = �X���
-- ORDER BY ORDSQ2




--DELETE #TEMP3_�~�s
----SELECT *
--FROM #TEMP3_�~�s A,(SELECT MAX(ORDSQ2) ORDSQ2,INPART FROM #TEMP3_�~�s GROUP BY INPART) B
--WHERE A.INPART = B.INPART AND A.ORDSQ2 < B.ORDSQ2

--�Υ~�]�t�� ��@�s�{�N�� ����N���ΦA��s�@��
UPDATE #TEMP3_�~�s SET PRDNAME = CUSTCA + PRDNAME+ �禬�^�t  --2023/12/06




--DELETE #TEMP3
--FROM #TEMP3 A,(SELECT MAX(ORDSQ2) ORDSQ2,INPART FROM #TEMP3_�~�s GROUP BY INPART) B
--WHERE A.INPART = B.INPART AND A.ORDSQ2 < B.ORDSQ2

--EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT * FROM #TEMP3_�~�s
--ORDER BY INPART,ORDSQ2
--WHERE INPART = '22L09257ML088-000#4'

--UPDATE #TEMP3 SET ORDDY2 = NULL WHERE ORDSQ2 = 0 AND PRDNAME LIKE '%��%' AND ORDFCO = 'N'
UPDATE #TEMP3 SET ORDDY2 = NULL WHERE PRDNAME LIKE '%AT%' AND ORDFCO = 'N'

UPDATE #TEMP3
SET PRDNAME = B.PRDNAME,ORDDTP = '2',ORDDY2 = CONVERT(DATETIME , �w�p�^�t��)
FROM #TEMP3 A,#TEMP3_�~�s B
WHERE A.INPART = B.INPART
--AND A.ORDSQ2 BETWEEN B.�o�]�_ AND B.�o�]��---- 2025/06/17 �|���O���s�{
AND A.ORDSQ2 = B.ORDSQ2
AND A.SOPKIND = '�~�s'
--- ����OS7
--------�J���٨S�~�]���]�n��ܩ��� 2024/08/01 Techup
--------�J����OS���~�]�n��ܩ��� 2024/08/07 Techup
UPDATE #TEMP3
SET PRDNAME = '����'+ REPLACE(PRDNAME,'��','')
FROM #TEMP3 A,ORDE1 B,
(SELECT ORDTP,ORDNO,ORDSQ,ORDSQ1,COUNT(*) SQ FROM #TEMP3 WHERE PRDNAME LIKE '%OS%' AND ORDFCO = 'N' AND ORDSQ3 = 0
AND ORDFO NOT IN ('72B','784') ---OSC����
GROUP BY ORDTP,ORDNO,ORDSQ,ORDSQ1
) C
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1 --AND A.ORDSQ2 = C.ORDSQ2
AND B.ORDCU = 'ASMLTNF' AND ORDFCO = 'N' AND PRDNAME LIKE '%OS%' AND PRDNAME NOT IN ('OSC','OSW')
--AND (PRDNAME NOT LIKE '%����%' OR PRDNAME NOT LIKE '%�ʹF��%')
AND PRDNAME NOT LIKE '%����%'
AND PRDNAME NOT LIKE '%�ʹF��%'

AND C.SQ > 1
---ORDER BY ORDSQ2,ORDSQ3



---- 2023/10/19 �v �R���~�s������
--DELETE #TEMP3 WHERE ORDFCO = 'Y' AND ORDDTP = '2'

--EXEC dbo.����ORDE3�Ѿl�s�{ '22Q04117-000R3'
--SELECT * FROM #TEMP3
----���L�٭n�A�ɤ@�� CHAR(10) 2023/02/08 Techup
UPDATE #TEMP3
SET PRDNAME = CHAR(10)+PRDNAME --�s�{�_��
FROM #TEMP3 A,
(SELECT MIN(ORDSQ2) �s�{��,ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART FROM #TEMP3 A
WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND SOPKIND NOT IN ('�]�p')
AND PRDNAME NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE DESCR LIKE '%�O%' AND PRDNAME <> 'AT') --�O�����N����
AND ORDFCO = 'N' GROUP BY INPART,ORDTP,ORDNO,ORDSQ,ORDSQ1) B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�s�{��
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 --2024/09/21 Techup ADD

-------�g���q�P�N�AAS�ݫ����H����WD�@�~�A�G�A�нШ�U�NAS�s�{�S�O���O-----2024/09/09 Techup-----
SELECT A.*
INTO #WDAS�@�_�s�@���ϸ�
FROM #TEMP3 A,ORDE3 B
WHERE RTRIM(LTRIM(REPLACE(REPLACE(REPLACE(PRDNAME,'��',''),'��',''),CHAR(10),''))) IN ('WD','AS')
AND ORDSQ3 = 0 AND A.INPART = B.INPART
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 --2024/09/21 Techup ADD
AND B.INDWG IN ('4022.680.04854','4022.680.04635','4022.683.72984S') AND ORDSQ2 IN ('1','2')
ORDER BY ORDSQ2

    UPDATE #WDAS�@�_�s�@���ϸ�
SET PRDNAME = CASE
--WHEN PRDNAME LIKE '%WD%' THEN PRDNAME+'��'
WHEN PRDNAME LIKE '%AS%' THEN '��'+PRDNAME
ELSE PRDNAME
END

UPDATE #TEMP3
SET PRDNAME = B.PRDNAME
FROM #TEMP3 A,#WDAS�@�_�s�@���ϸ� B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = 0
-------�g���q�P�N�AAS�ݫ����H����WD�@�~�A�G�A�нШ�U�NAS�s�{�S�O���O-----2024/09/09 Techup-----





--�B�z�~�]�Ѽ� ����N�Φ^�t��-���� �Ѿl�Ѽ�
    --�S���� �N�Ѧҥ~�]�����ӹw�]LT �Ѽ�
    --�p�G�H�W���S�� �N���]�w1��


--<<<<<<<<<<-�B�z�~�s-->>>>>>>>>>>>>---2022/10/17--------------------------------------------
----�~�s --�B�z�~�]�Ѽ� ����N�Φ^�t��-���� �Ѿl�Ѽ�
----�Ȯ����� 2022/12/15 Techup
--UPDATE #TEMP3 SET �~�]�w�p�Ѽ� = DATEDIFF ( DD , GETDATE() , ISNULL(ORDDY5,ORDDY2))  
--WHERE SOPKIND = '�~�s' AND ISNULL(ISNULL(ORDDY5,ORDDY2),'') <> '' AND ORDFCO = 'N' --AND PRDNAME NOT LIKE '%AT%'

--�S���� �N�Ѧҥ~�]�����ӹw�]LT �Ѽ�
--UPDATE #TEMP3
--SET �~�]�w�p�Ѽ� = B.�^�t�Ѽ�
--FROM #TEMP3 A, �~�]�����ӹw�]LT B
--WHERE RTRIM(LTRIM(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''))) = B.�s�{ AND ISNULL(�~�]�w�p�Ѽ�,'') = ''
--<<<<<<<<<<-�B�z�~�s-->>>>>>>>>>>>>---2022/10/17--------------------------------------------

UPDATE #TEMP3
SET �~�]�w�p�Ѽ� = B.�^�t�Ѽ�
FROM #TEMP3 A, �~�]�����ӹw�]LT B,#SOPNAME C
WHERE
--RTRIM(LTRIM(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''))) = ISNULL(B.�t��,'')
--RTRIM(LTRIM(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),CHAR(10),''))) = ISNULL(B.�t��,'')
 RTRIM(LTRIM(REPLACE(REPLACE(REPLACE(REPLACE(A.PRDNAME,C.PRDNAME,''),'��',''),'��',''),CHAR(10),''))) = ISNULL(B.�t��,'') --2024/01/05 �v ���s�{�٦b�̭��ҥH�]REPLACE��
AND ISNULL(�~�]�w�p�Ѽ�,'') = ''
AND (( C.PRDOPNO = A.ORDFO ) OR C.PRDOPNO = (SELECT PRDOPGP FROM #SOPNAME WHERE PRDOPNO = A.ORDFO ))
AND C.PRDNAME = B.�s�{

----UPDATE #TEMP3 SET �~�]�w�p�Ѽ� = 5 WHERE ORDFO = '5H' AND RTRIM(LTRIM(REPLACE(REPLACE(REPLACE(PRDNAME,'��',''),'��',''),CHAR(10),''))) = '�O��'


--SELECT B.�^�t�Ѽ�,A.*
 --   FROM #TEMP3 A, �~�]�����ӹw�]LT B,SOPNAME C
--WHERE RTRIM(LTRIM(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''))) = ISNULL(B.�t��,'') AND ISNULL(�~�]�w�p�Ѽ�,'') = ''
--AND (( C.PRDOPNO = A.ORDFO ) OR C.PRDOPNO = (SELECT PRDOPGP FROM SOPNAME WHERE PRDOPNO = A.ORDFO ))

--EXEC dbo.����ORDE3�Ѿl�s�{ '22Q03308-000R1#1R1R1'
--EXEC dbo.����ORDE3�Ѿl�s�{ '22Q03313-000R2#1R1R1'
--EXEC dbo.����ORDE3�Ѿl�s�{ '22Q03313-000R2#1R1R1'
--EXEC dbo.����ORDE3�Ѿl�s�{ ''


------�ӹϸ� ���7�� 2022/12/15
--UPDATE #TEMP3
--SET �~�]�w�p�Ѽ� = 7
--FROM #TEMP3 A,ORDE3 B
--WHERE A.INPART = B.INPART AND B.INDWG IN ('4022.680.04863-S20C') AND PRDNAME = 'OS'

--�~�]�Ѽ� ��SOPNAME�зǸ�� 2022/12/15 Techup
    UPDATE #TEMP3
SET �~�]�w�p�Ѽ� = B.SBASDTIME
FROM #TEMP3 A,#SOPNAME B
--WHERE RTRIM(LTRIM(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''))) = B.PRDNAME
WHERE RTRIM(LTRIM(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),CHAR(10),''))) = B.PRDNAME
AND ISNULL(�~�]�w�p�Ѽ�,'') = ''
AND A.ORDDTP = '2'

--SELECT 'AAAA',* FROM #TEMP3




--<<<<<<<<<<-�B�z��-->>>>>>>>>>>>>---2022/10/17--------------------------------------------
--��LT
UPDATE #TEMP3 SET �~�]�w�p�Ѽ� = ISNULL(C.INDAY,0)
FROM #TEMP3 A,ORDE3 B,INVMAST C
WHERE A.INPART = B.INPART AND B.INDWG = C.INDWG AND C.INTYP = '5' AND A.ORDFCO = 'N' AND ORDSQ2 = 0
AND ISNULL(ORDDY2,'') = ''



UPDATE #TEMP3 SET �~�]�w�p�Ѽ� = DATEDIFF ( DD , GETDATE() , ISNULL(ORDDY2,''))  
WHERE ORDFCO = 'N' AND ORDSQ2 = 0 AND ISNULL(ORDDY2,'') <> ''
--<<<<<<<<<<-�B�z��-->>>>>>>>>>>>>---2022/10/17--------------------------------------------

UPDATE #TEMP3
SET �~�]�w�p�Ѽ� = �~�]�w�p�Ѽ�+'D'
WHERE ISNULL(�~�]�w�p�Ѽ�,'') <> ''


--EXEC dbo.����ORDE3�Ѿl�s�{ '22Q03209-000#3R3'
--SELECT 'AAA',* FROM #TEMP3
--ORDER BY ORDSQ2

SELECT B.ORDCU,C.INDWG,A.*
INTO #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z
FROM #TEMP3 A,ORDE1 B,ORDE3 C
WHERE ORDFCO = 'N' AND ORDDTP = '2'  
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO
AND B.ORDCU = 'ASMLTNF'
AND ORDSQ2 > 0
AND ORDFO IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP = '25')
AND (
--PRDNAME LIKE '%OS7%' OR PRDNAME LIKE '%OS14%'
PRDNAME LIKE '%OS%'  ---��ΩҦ�OS���n 2023/07/03 Techup
    OR PRDNAME LIKE '%�ʹF��%'
OR PRDNAME LIKE '%����%') AND SOPKIND = '�~�s'
AND A.INPART = C.INPART
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1 --2024/09/21 Techup ADD
ORDER BY INPART,ORDSQ2
--AND PRDNAME LIKE '%OS%'
--SELECT * FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z --WHERE INPART = '22Q01150-0-000R6'

UPDATE #TEMP3
SET �~�]�w�p�Ѽ� = CAST(B.SBASDTIME AS VARCHAR(5)) + 'D'
FROM #TEMP3 A,SOPNAME B
WHERE  A.ORDFO = B.PRDOPNO AND ORDDTP = '2'

----�J��S�O�ϸ� �s�{�O FSBO �� �d�� �@�ߧ令3�� 2023/05/09 Techup
----�J��S�O�ϸ� �s�{�O FSBO �� �d�� �@�ߧ令7�� 2023/07/03 Techup
UPDATE #TEMP3
SET �~�]�w�p�Ѽ� = '7D'
FROM #TEMP3 A,ORDE3 B
WHERE A.INPART = B.INPART AND B.INDWG = '4022.454.50552' AND (PRDNAME LIKE '%�d��%' OR PRDNAME LIKE '%FSBO%')

--EXEC dbo.����ORDE3�Ѿl�s�{ ''
--2023/07/19 �s�W�w���ʹF���� 4022.683.73492 OS�ҥ~�B�z Techup
UPDATE #TEMP3
SET �~�]�w�p�Ѽ� = '28D'
FROM #TEMP3 A,#�w��ASMLTNF�ϸ��~�]���ҥ~�B�z B
WHERE A.INPART = B.INPART AND (A.PRDNAME LIKE '%OS28%' OR A.PRDNAME LIKE '%�ʹF��%') AND A.SOPKIND = '�~�s'
AND B.INDWG = '4022.683.73492'


----2023/02/06 �s�W�w����˪�OS�ҥ~�B�z Techup
--UPDATE #TEMP3
--SET �~�]�w�p�Ѽ� =
--CAST(15/B.SQ AS VARCHAR(5)) +'D'
----'14D'
----CAST(15/B.SQ AS VARCHAR(5)) +'D' --�`��30�� --��ӧ�14�� --���ƪ��S�אּ42�� 2023/07/03 Techup --���ƪ��S�אּ30�� 2023/07/06 Techup --���ƪ��S�אּ15�� 2023/07/18 Techup
--FROM #TEMP3 A,(
--SELECT COUNT(*) SQ,INPART FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z WHERE INDWG NOT IN ('4022.683.73492') --����O�ʹF�� 2023/07/19 Techup
--GROUP BY INPART) B,
--(SELECT ORDSQ2,INPART FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z) C
--WHERE A.INPART = B.INPART AND A.ORDSQ2 > 0
--AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND ORDDTP = '2'  

------
----SELECT 'AAAA',B.INDWG,A.* FROM #TEMP3 A,ORDE3 B
----WHERE A.INPART = B.INPART
----AND (
------PRDNAME LIKE '%OS7%' OR PRDNAME LIKE '%OS14%'
----PRDNAME LIKE '%OS%'  ---��ΩҦ�OS���n 2023/07/03 Techup
 ----   OR PRDNAME LIKE '%�ʹF��%'
----OR PRDNAME LIKE '%����%') AND SOPKIND = '�~�s'
----AND A.INPART IN ('23Q03141-000','23Q03140-000')

 --   -----�w��RGA ���ϸ� �n��30�ѭp��
--UPDATE #TEMP3
--SET �~�]�w�p�Ѽ� = --'14D'
--CAST(30/B.SQ AS VARCHAR(5)) +'D' --�`��30�� --��ӧ�14�� --���ƪ��S�אּ42�� 2023/07/03 Techup --���ƪ��S�אּ30�� 2023/07/06 Techup --���ƪ��S�אּ15�� 2023/07/18 Techup
--FROM #TEMP3 A,(
--SELECT COUNT(*) SQ,INPART FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z
--GROUP BY INPART) B,
--(SELECT ORDSQ2,INPART FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z) C,ORDE3 D
--WHERE A.INPART = B.INPART AND A.ORDSQ2 > 0
--AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND ORDDTP = '2'  
--AND A.INPART = D.INPART
--AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO AND A.ORDSQ = D.ORDSQ AND A.ORDSQ1 = D.ORDSQ1 --2024/09/21 Techup ADD
--AND D.INDWG IN ('4022.680.03453','4022.680.03512','4022.680.04634','4022.680.04853','4022.680.04863','4022.680.42102','4022.683.72984','4022.683.73033')



--------EXEC dbo.����ORDE3�Ѿl�s�{ ''
--------SELECT A.*
----------�w��~�ȴ��Ѫ��ϸ� �אּ14�� 2023/06/20 Techup
----UPDATE #TEMP3
----SET �~�]�w�p�Ѽ� = '14D'
----FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z A,ORDE3 B,#TEMP3 C
----WHERE INDWG IN ('4022.680.03453','4022.680.03512','4022.680.04634','4022.680.04853','4022.680.04863','4022.680.42102','4022.683.72984','4022.683.73033')
----AND A.INPART = B.INPART
----AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND A.ORDSQ3 = C.ORDSQ3


UPDATE #TEMP3
SET �~�]�w�p�Ѽ� = '7D'
WHERE INPART = '22Q04208-000R1' AND PRDNAME = 'OS14'






--SELECT �~�]�w�p�Ѽ�,* FROM #TEMP3 A,(
--SELECT COUNT(*) SQ,INPART FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z
--GROUP BY INPART) B,
--( SELECT ORDSQ2,INPART FROM #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z
--) C
--WHERE A.INPART = B.INPART AND A.ORDSQ2 > 0
--AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND ORDDTP = '2'  
--ORDER BY ORDSNO

--�P�B��s ORDDE4_�Ѿl�s�{����_����_D
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �~�]�w�p�Ѽ� = A.�~�]�w�p�Ѽ�
FROM #TEMP3 A,ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = B.ORDSQ3


    --------------------����γ]�p��ƪ���z 2023/02/17 Techup------------------------------------------
SELECT A.*
INTO #�e�m�s�{���A
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_D C,
(SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDSQ3 = 0 AND ORDSQ2 > 0 GROUP BY INPART ) D,
ORDE2 E
WHERE A.INPART = B.INPART --AND B.INFIN IN ('N','P')
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
AND A.ORDTP = E.ORDTP AND A.ORDNO = E.ORDNO AND A.ORDSQ = E.ORDSQ
AND A.INPART = C.INPART
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1 --2024/09/21 Techup ADD
AND A.INPART = D.INPART AND A.ORDSQ2 = D.ORDSQ2 AND A.ORDSQ3 = 0 AND A.ORDSQ2 > 0
AND A.INPART LIKE @INPART
AND A.INPART NOT LIKE '%-CAM' AND A.INPART NOT LIKE '%-M' --�J�� -CAM -M �����ͱ���M�]�p
AND SUBSTRING(A.INPART,CHARINDEX('-F',A.INPART)+1,LEN(A.INPART)-CHARINDEX('-F',A.INPART)) NOT LIKE 'F%' --�J�� -F �����ͱ���M�]�p




--SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDSQ3 = 0
--AND INPART = '23G03512SL-001-001#11R1' AND ORDSQ2 > 0
----GROUP BY INPART

----����� �ݭq��إߨ�T�{��
UPDATE #�e�m�s�{���A
SET ORDDY1 = CASE WHEN B.LINE IN ('L','T','N') THEN ISNULL(B.CRDATE,GETDATE()) ELSE ISNULL(B.CFMDATE,GETDATE()) END,
   --ISNULL(ISNULL(C.AMDDATE, C.CRDATE), B.CFMDATE) END, --��ΦC�L�� 2023/03/27 Techup
--(CASE WHEN ISNULL(C.AMDDATE, C.CRDATE) > ISNULL(B.CFMDATE,GETDATE()) THEN ISNULL(C.AMDDATE, C.CRDATE) ELSE  ISNULL(B.CFMDATE,GETDATE()) END) END,
   ORDDY2 = CASE WHEN B.LINE IN ('L','T','N') THEN ISNULL(B.CRDATE,GETDATE()) ELSE ISNULL(B.CFMDATE,GETDATE()) END,
--ISNULL(ISNULL(C.AMDDATE, C.CRDATE), B.CFMDATE) END, --��ΦC�L�� 2023/03/27 Techup
---(CASE WHEN ISNULL(C.AMDDATE, C.CRDATE) > ISNULL(B.CFMDATE,GETDATE()) THEN ISNULL(C.AMDDATE, C.CRDATE) ELSE  ISNULL(B.CFMDATE,GETDATE()) END) END,
ORDDY4 = CASE WHEN B.LINE IN ('L','T','N') THEN ISNULL(B.CRDATE,GETDATE()) ELSE ISNULL(B.CFMDATE,GETDATE()) END,
--ISNULL(ISNULL(C.AMDDATE, C.CRDATE), B.CFMDATE) END, --��ΦC�L�� 2023/03/27 Techup
--(CASE WHEN ISNULL(C.AMDDATE, C.CRDATE) > ISNULL(B.CFMDATE,GETDATE()) THEN ISNULL(C.AMDDATE, C.CRDATE) ELSE  ISNULL(B.CFMDATE,GETDATE()) END) END,
ORDDY5 = CASE WHEN B.LINE IN ('L','T','N') THEN ISNULL(B.CRDATE,GETDATE()) ELSE ISNULL(B.CFMDATE,GETDATE()) END,
--ISNULL(ISNULL(C.AMDDATE, C.CRDATE), B.CFMDATE) END, --��ΦC�L�� 2023/03/27 Techup
--(CASE WHEN ISNULL(C.AMDDATE, C.CRDATE) > ISNULL(B.CFMDATE,GETDATE()) THEN ISNULL(C.AMDDATE, C.CRDATE) ELSE  ISNULL(B.CFMDATE,GETDATE()) END) END,
  PRDATE1 = CASE WHEN B.LINE IN ('L','T','N') THEN ISNULL(B.CRDATE,GETDATE()) ELSE ISNULL(B.CFMDATE,GETDATE()) END,
    --ISNULL(ISNULL(C.AMDDATE, C.CRDATE), B.CFMDATE) END,
--(CASE WHEN ISNULL(C.AMDDATE, C.CRDATE) > ISNULL(B.CFMDATE,GETDATE()) THEN ISNULL(C.AMDDATE, C.CRDATE) ELSE  ISNULL(B.CFMDATE,GETDATE()) END) END,
PRTFM = CASE WHEN B.LINE IN ('L','T','N') THEN ISNULL(B.CRDATE,GETDATE()) ELSE ISNULL(B.CFMDATE,GETDATE()) END,
--(CASE WHEN ISNULL(C.AMDDATE, C.CRDATE) > ISNULL(B.CFMDATE,GETDATE()) THEN ISNULL(C.AMDDATE, C.CRDATE) ELSE  ISNULL(B.CFMDATE,GETDATE()) END) END,
--ISNULL(ISNULL(C.AMDDATE, C.CRDATE), B.CFMDATE) END,
ORDFO = 'OD',PRDNAME = '����',ORDFCO = 'Y',ORDDTP = '1',ORDSQ2 = -1000,SOPKIND='����',WKNO='',DEPTNO='',Applier ='',ORDFM1 = 0,ORDAMT = 0
FROM #�e�m�s�{���A A JOIN ORDE2 B ON A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
--LEFT OUTER JOIN ORDMENO C ON C.ORDTP = B.ORDTP AND C.ORDNO = B.ORDNO AND C.ORDSQ = B.ORDSQ AND C.ORDSQ1 = 0 AND B.INPART = C.INPART

----�}�߲��`�檺�N���ݭn������P�]�p 2023/06/13 Techup
DELETE #�e�m�s�{���A
WHERE INPART IN (SELECT INPART FROM ORDDE4_�Ѿl�s�{����_���`��)
AND ORDSQ2 IN ('-1000','-500')

-----�t�~�z�LSFC2003PNET�}�ߪ��s�d �N���n���� ����M�]�p���d 2024/06/17 Techup
DELETE #�e�m�s�{���A
WHERE INPART IN (SELECT INPART FROM SFC_TMPDE3 WHERE UID IS NULL AND SCRL <> 'X')
AND ORDSQ2 IN ('-1000','-500')


----����檺�N���ݭn������P�]�p 2023/09/03 Techup
DELETE #�e�m�s�{���A
WHERE INPART IN (SELECT INPART FROM ORDM31 WHERE INPART <> INPART1) ---��X�s���s�d���~��
AND ORDSQ2 IN ('-1000','-500')

------EXEC dbo.����ORDE3�Ѿl�s�{ ''

-- SELECT 'EEE',* FROM #�e�m�s�{���A A LEFT OUTER JOIN ORDDE4_�Ѿl�s�{����_����_D B
-- ON A.INPART = B.INPART AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO  AND A.ORDSQ = B.ORDSQ
-- AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2  
--WHERE A.INPART = '23ZA05664' AND B.ORDSQ3 = 0

----���g�J����
INSERT INTO ORDDE4_�Ѿl�s�{����_����_D
SELECT * FROM #�e�m�s�{���A



--�إ߼Ȧs
SELECT * INTO #ORD3STATUS FROM ORD3STATUS
WHERE INPART IN (SELECT distinct INPART FROM ORDDE4_�Ѿl�s�{����_����_D WHERE INPART LIKE @INPART)

DELETE #�e�m�s�{���A
WHERE INPART NOT IN (SELECT distinct INPART FROM #ORD3STATUS)

----�]�p�� �ݭq��T�{���q��T�{��+7�� --�|�����u��
UPDATE #�e�m�s�{���A
SET ORDDY1 = ISNULL(PRDATE1,GETDATE()),ORDDY2=ISNULL(PRDATE1,GETDATE()),ORDDY5= DATEADD(dd,+7, ISNULL(PRDATE1,GETDATE())),
PRDATE1=DATEADD(dd,+7, ISNULL(PRDATE1,GETDATE())),PRTFM = GETDATE(),
ORDFO = 'ENG',PRDNAME = '�]�p',ORDFCO = 'N',ORDDTP = '1',ORDSQ2 = -500,SOPKIND='�]�p',WKNO='',DEPTNO='',Applier ='',ORDFM1 = 4200,ORDAMT = 0
FROM #�e�m�s�{���A A,
(
SELECT INPART,MIN(ISNULL(MOT,'')) MOT,MIN(ISNULL(INDWGYN,'')) INDWGYN,MIN(ISNULL(CAM,'')) CAM,MIN(ISNULL(FT,'')) FT,MIN(ISNULL(KNV,'')) KNV
FROM #ORD3STATUS A,#SOPNAME B WHERE ISNULL(A.PRDNAME,'') <> '' AND A.PRDNAME = B.PRDNAME AND B.PRDOPGP NOT IN ('15','830','833')
AND (ISNULL(MOT,'') = '' OR ISNULL(INDWGYN,'') = '' OR ISNULL(CAM,'') = '' OR ISNULL(FT,'') = ''  OR ISNULL(KNV,'') = '')
GROUP BY INPART
) B
WHERE A.INPART = B.INPART




UPDATE #�e�m�s�{���A
SET ORDDY1 = ISNULL(PRDATE1,GETDATE()),ORDDY2=ISNULL(PRDATE1,GETDATE()),ORDDY5= DATEADD(dd,+7, PRDATE1),
PRDATE1=DATEADD(dd,+7, PRDATE1),PRTFM =ADDATE,
ORDFO = 'ENG',PRDNAME = '�]�p',ORDFCO = 'Y',ORDDTP = '1',ORDSQ2 = -500,SOPKIND='�]�p',WKNO='',DEPTNO='',Applier ='',ORDFM1 = 4200,ORDAMT = 0
FROM #�e�m�s�{���A A,
(SELECT INPART,MAX(ADDATE) ADDATE FROM (
SELECT INPART,MAX(ISNULL(MOTDATE,'')) ADDATE FROM ORD3STATUS  A,#SOPNAME B WHERE ISNULL(A.PRDNAME,'') <> '' AND ISNULL(MOT,'') <> ''
        AND B.PRDOPGP NOT IN ('15','830','833') AND A.PRDNAME = B.PRDNAME GROUP BY INPART
UNION
SELECT INPART,MAX(ISNULL(INDWGYNDATE,'')) ADDATE FROM ORD3STATUS A,#SOPNAME B WHERE ISNULL(A.PRDNAME,'') <> '' AND ISNULL(INDWGYN,'') <> ''
        AND B.PRDOPGP NOT IN ('15','830','833') AND A.PRDNAME = B.PRDNAME GROUP BY INPART
UNION
SELECT INPART,MAX(ISNULL(CAMDATE,'')) ADDATE FROM ORD3STATUS A,#SOPNAME B WHERE ISNULL(A.PRDNAME,'') <> '' AND ISNULL(CAM,'') <> ''
        AND B.PRDOPGP NOT IN ('15','830','833') AND A.PRDNAME = B.PRDNAME GROUP BY INPART
UNION
SELECT INPART,MAX(ISNULL(FTDATE,'')) ADDATE FROM ORD3STATUS A,#SOPNAME B WHERE ISNULL(A.PRDNAME,'') <> '' AND ISNULL(FT,'') <> ''
        AND B.PRDOPGP NOT IN ('15','830','833') AND A.PRDNAME = B.PRDNAME GROUP BY INPART
UNION
SELECT INPART,MAX(ISNULL(KNVDATE,'')) ADDATE FROM ORD3STATUS A,#SOPNAME B WHERE ISNULL(A.PRDNAME,'') <> '' AND ISNULL(KNV,'') <> ''
        AND B.PRDOPGP NOT IN ('15','830','833') AND A.PRDNAME = B.PRDNAME GROUP BY INPART
) A
GROUP BY INPART) B
WHERE ORDSQ2 = -1000 AND A.INPART = B.INPART

--SELECT '�]�p',* FROM #�e�m�s�{���A
--SELECT '�]�p',* FROM ORDDE4_�Ѿl�s�{����_����_D WHERE INPART = @INPART
    --EXEC dbo.����ORDE3�Ѿl�s�{ '23B01001ML-0-017-001-001'
--EXEC dbo.����ORDE3�Ѿl�s�{ ''

--�٦��N�R��
DELETE #�e�m�s�{���A WHERE ORDSQ2 = -1000

--SELECT B.�Ƶ�_PC�s�d���A,A.*
--FROM #�e�m�s�{���A A,ORDE3 B
--WHERE A.INPART = B.INPART
--AND A.INPART = '23B01001ML-0-017-001-001'
--AND (A.INPART NOT LIKE '%-0-%' AND ISNULL(B.�Ƶ�_PC�s�d���A,'') NOT LIKE '%FAI%')

--�p�G���O����]���OFAI�h�R�� �]�p�s�{ 2023/03/02 Techup
DELETE #�e�m�s�{���A
FROM #�e�m�s�{���A A,ORDE3 B
WHERE A.INPART = B.INPART
AND A.INPART NOT LIKE '%-0-%' AND ORDSQ2 = -500

DELETE #�e�m�s�{���A
FROM #�e�m�s�{���A A,ORDE3 B
WHERE A.INPART = B.INPART
AND ISNULL(B.�Ƶ�_PC�s�d���A,'') NOT LIKE '%FAI%'
AND ORDSQ2 = -500

--EXEC dbo.����ORDE3�Ѿl�s�{ '22Q04199-000'
-- SELECT * FROM #�e�m�s�{���A

--�}�߲��`��B�P�w���s���s�d�h��β��`��PC�T�{�� �~�}�l��
UPDATE #�e�m�s�{���A
SET ORDDY5 = B.PCDATE
FROM #�e�m�s�{���A A,ORDDE4_�Ѿl�s�{����_���`�� B
WHERE A.INPART = B.INPART
AND REWORK NOT IN ('�Ȩѫ~���}(�i��)','QC�ߧY�B�z','�i��')
-- EXEC dbo.����ORDE3�Ѿl�s�{ '23X01004MT-0-000R1'



INSERT INTO ORDDE4_�Ѿl�s�{����_����_D
SELECT * FROM #�e�m�s�{���A
--22Q04076ML-002



---��z�L---OOOOOOXXXXXXXX-----------------------------------------------------------------
select ORDDY1,PRTFM,ORDTP,ORDNO,ORDSQ ,DLYTIME
INTO #�q�汵�����
from ORDDE4_�Ѿl�s�{����_����_D
WHERE ORDSQ2 IN ('-1000')
GROUP BY ORDDY1,PRTFM,ORDTP,ORDNO,ORDSQ,DLYTIME
ORDER BY ORDTP,ORDNO,ORDSQ,ORDDY1,PRTFM

---��z�L---OOOOOOXXXXXXXX-----------------------------------------------------------------
UPDATE #�q�汵�����
SET DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(ORDDY1,PRTFM,4)/60.00 --�q�汵�� ��Τ@��4�p�ɳB�z 2023/03/28 Techup
FROM #�q�汵�����

UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = B.DLYTIME --dbo.�ɶ��t_�̤W�Z�ɶ�(ORDDY1,PRTFM,4)/60.00 --�q�汵�� ��Τ@��4�p�ɳB�z 2023/03/28 Techup
FROM ORDDE4_�Ѿl�s�{����_����_D A,#�q�汵����� B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ

UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(ORDDY5,PRTFM,@DLYTIME�C��u�@�p��)/60.00
WHERE INPART LIKE @INPART AND ORDSQ2 IN ('-500')



-------------------------�B�z�ƪ����ʪ��p---2023/06/17 Techup-------------------------------------------------------------------

SELECT A.*
INTO #IV1
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B
WHERE ORDFO ='��'
AND ORDSQ3 = 0 AND ORDSQ2 = 0
AND A.INPART = B.INPART AND ISNULL(B.�ȨѮ�,'') <> 'Y'
AND A.INPART LIKE @INPART

SELECT DISTINCT INPART INTO #INPART FROM #IV1


-- EXEC dbo.����ORDE3�Ѿl�s�{ '23K03191AF-018-001'
-- EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT * FROM #INPART
--WHERE INPART = '23K03191AF-018-001'



--SELECT A.PUPRP,A.PURNO,���=MIN(B.CFDAY)
-- FROM PURTD A,PURTM B
-- WHERE A.PURNO  = B.PURNO
-- AND B.SCTRL= 'Y'
-- AND A.PUPRP = '22L09270ML-011-008'
-- AND B.PURTP ='0'
-- GROUP BY A.PUPRP,A.PURNO


SELECT P.INPART,SQ2=-131,���,���O='����',�渹= CASE WHEN ISNULL(Q.���,'') = '' THEN '' ELSE 'Y' END
 INTO #IV2
 FROM #INPART P LEFT OUTER JOIN (SELECT C.INPART,���=MIN(B.CFDAY)
FROM PURTD A,PURTM B,#INPART C
WHERE A.PURNO  = B.PURNO
AND B.SCTRL= 'Y'
AND A.PUPRP = C.INPART
AND B.PURTP ='0' GROUP BY C.INPART) Q ON P.INPART = Q.INPART
UNION ALL
SELECT P.INPART,SQ2=-121,���,���O='�o�]',�渹= CASE WHEN ISNULL(Q.���,'') = '' THEN '' ELSE 'Y' END
 FROM #INPART P LEFT OUTER JOIN (SELECT C.INPART,���= MIN(B.AMDDAY)
FROM PURDEL A,PURMAS B,#INPART C
WHERE A.PURNO  = B.PURNO
AND B.SCTRL= 'Y'
AND A.INPART = C.INPART
AND B.PURAA= '0' GROUP BY C.INPART) Q ON P.INPART = Q.INPART
UNION ALL
SELECT P.INPART,SQ2=-111,���,���O='�禬',�渹= CASE WHEN ISNULL(Q.���,'') = '' THEN '' ELSE 'Y' END
 FROM #INPART P LEFT OUTER JOIN (SELECT  C.INPART,���=MIN(B.CRUDAY)
FROM PURIND A,PURINM B,#INPART C
WHERE A.PUINO  = B.PUINO
 AND B.SCTRL <> 'X'
 AND A.INPART = C.INPART
 AND B.PURAA= '0' GROUP BY C.INPART) Q ON P.INPART = Q.INPART
UNION ALL
SELECT P.INPART,SQ2=-101,���,���O='�J�w',�渹= CASE WHEN ISNULL(Q.���,'') = '' THEN '' ELSE 'Y' END
 FROM #INPART P LEFT OUTER JOIN (SELECT  C.INPART,���=MIN(B.AMDDAY)
FROM PURIND A,PURINM B,#INPART C
WHERE A.PUINO  = B.PUINO
 AND B.SCTRL = 'Y'
 AND A.INPART = C.INPART
 AND B.PURAA= '0' GROUP BY C.INPART) Q ON P.INPART = Q.INPART



SELECT A.*,B.SQ2,B.���,B.���O,B.�渹
INTO #IV3
FROM #IV1 A,#IV2 B
WHERE A.INPART = B.INPART





------�p�G�S�����ʴN����ܥX ���ʬy�{ 2023/09/06 Techup
DELETE #IV3
FROM #IV3 A,(SELECT distinct INPART FROM #IV3 WHERE ���O = '����' AND ISNULL(���,'') = '' ) B
WHERE A.INPART = B.INPART
--INPART = '23G01095ML-0-000#1R1'


---- 2023/06/28 �v
UPDATE #IV3 SET ORDSQ2=SQ2,ORDFO=LEFT(���O,1),PRDNAME=���O,SOPKIND=���O,ORDDY5=���,MP5CODE=�渹,PRTFM=���,
ORDFCO=CASE WHEN ISNULL(���,'')='' THEN 'N' ELSE 'Y' END,
 WKNO=NULL,DEPTNO=NULL,Applier=NULL,
 --�U���驵��Ƶ�=NULL,
 �U���驵��Ƶ�=(CASE WHEN ORDFCO = 'Y' AND ISNULL(ORDDY5,'') = '' AND ISNULL(���,'') = ''
 THEN '�w�s��' ELSE NULL END),
 CARDNO=NULL,�~�]�w�p�Ѽ�=NULL,ORDFM1 = 0

-----2023/09/05 �Ʀp�G�w�g�o�ƥB �S�����ʬ����h�����]�w���w�s��
UPDATE #IV3
SET �U���驵��Ƶ� = '�w�s��'
FROM #IV3 A,(SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDFO = '��' AND ORDFCO = 'Y') B
WHERE A.INPART = B.INPART AND A.���O IN ('����','�o�]','�禬','�J�w') AND ISNULL(A.���,'') = ''

-- EXEC dbo.����ORDE3�Ѿl�s�{ '23M01079-0-000'


ALTER TABLE #IV3 DROP COLUMN SQ2
ALTER TABLE #IV3 DROP COLUMN ���
ALTER TABLE #IV3 DROP COLUMN ���O
ALTER TABLE #IV3 DROP COLUMN �渹



INSERT INTO ORDDE4_�Ѿl�s�{����_����_D
SELECT * FROM #IV3  
-------------------------�B�z�ƪ����ʪ��p---2023/06/17 Techup-------------------------------------------------------------------




--EXEC dbo.����ORDE3�Ѿl�s�{ ''
-----�w��� ��]�p���_�I �i��α� 2023/06/05 Techup ------------------------------------------
SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,ORDSQ2),*
INTO #����zDLYTIME_E FROM ORDDE4_�Ѿl�s�{����_����_D WHERE 1 = 0

SELECT ID = CAST(0 AS INT)  , TIME1 =CAST('' AS datetime),TIME2 = CAST('' AS datetime),MM = CAST(0 AS INT)
INTO #����zDLYTIME_NEW_E FROM #����zDLYTIME_E WHERE 1 = 0

SELECT *
INTO #����zDLYTIME_E_�Ƥ��e���d
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE INPART LIKE @INPART AND
ORDSQ3 = 0 AND ORDSQ2 < 1


SELECT A.*,ISACTIVE
INTO #����zDLYTIME_E_�Ȯ�
FROM ORDDE4_�Ѿl�s�{����_����_D A,#SOPNAME B
WHERE A.ORDFO = B.PRDOPNO AND INPART LIKE @INPART
AND ORDSQ3 = 0



----���p�⩵�����d ���R��
DELETE #����zDLYTIME_E_�Ȯ� WHERE (ISACTIVE = '1') OR ORDFCO = 'C'  -----����|�窺���� 2024/06/17 Techup
----DELETE #����zDLYTIME_E_�Ȯ� WHERE (ISACTIVE = '1' AND SOPKIND <> '�|��') OR ORDFCO = 'C'
----DELETE #����zDLYTIME_E_�Ȯ� WHERE ISACTIVE = '1' OR ORDFCO = 'C'

ALTER TABLE #����zDLYTIME_E_�Ȯ� DROP COLUMN ISACTIVE    



----�ɦ^�Ƥ��e�����d
INSERT INTO #����zDLYTIME_E_�Ȯ�
SELECT * FROM #����zDLYTIME_E_�Ƥ��e���d

INSERT INTO #����zDLYTIME_E
SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,ORDSQ2),* FROM #����zDLYTIME_E_�Ȯ�

--------�����٨S�}�l���u �h��η�U�ɶ��p�� 2023/09/03 Techup
UPDATE #����zDLYTIME_E
SET PRTFM = GETDATE()
FROM #����zDLYTIME_E A,
(SELECT INPART,MAX(ORDSQ2) �s�{�� FROM #����zDLYTIME_E A WHERE ORDFCO = 'Y' AND ORDSQ3 = 0 AND ORDSQ2 > 0  GROUP BY INPART ) B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�s�{��+1 AND ORDFCO = 'N'
--AND ISNULL(A.PRTFM,'') = ''
AND A.INPART LIKE @INPART
--ORDER BY ORDSQ2




INSERT INTO #����zDLYTIME_NEW_E
SELECT A.ID,TIME1 = B.PRTFM,TIME2 = A.PRTFM,MM = CAST(0 AS INT)
FROM #����zDLYTIME_E A, (SELECT * FROM #����zDLYTIME_E) B
WHERE A.INPART = B.INPART AND A.ID-1 = B.ID

------EXEC dbo.����ORDE3�Ѿl�s�{ '23G01112ML-0-000'
------EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT C.*,B.*,A.*
------��s�t�� �J�첧�`��� �W��������ɶ���� ���`�沣�ͪ� PCDATE 2023/09/14 Techup
UPDATE #����zDLYTIME_NEW_E
SET TIME1 = C.PCDATE
FROM #����zDLYTIME_E A,#����zDLYTIME_NEW_E B,ORDDE4_�Ѿl�s�{����_���`�� C
WHERE A.ID = B.ID AND A.INPART = C.OLDPART AND A.ORDSQ2 = C.ORDSQ2
--WHERE INPART = @INPART
--ORDER BY ORDSQ2

--SELECT * FROM #����zDLYTIME_E
--WHERE INPART = @INPART
--ORDER BY ORDSQ2





EXEC [dbo].[�ɶ������t_�̤W�Z�ɶ�] @DLYTIME�C��u�@�p�� ,'#����zDLYTIME_NEW_E'
   
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = (B.MM-
CASE WHEN A.ORDDTP = 2 AND A.ORDSQ2 > 0 THEN REPLACE(A.�~�]�w�p�Ѽ�,'D','')*10*60
WHEN A.ORDSQ2 > 0 THEN  
A.ORDFM1 ELSE 0
END --2023/09/03 �����w��ɶ� Techup
)/60.00 --B.MM/60.00
FROM #����zDLYTIME_E A,#����zDLYTIME_NEW_E B,ORDDE4_�Ѿl�s�{����_����_D C,
(SELECT INPART,MAX(ORDSQ2) �s�{�� FROM #����zDLYTIME_E A WHERE ORDFCO = 'Y' AND ORDSQ3 = 0 AND ORDSQ2 > 0  GROUP BY INPART ) D
WHERE A.ID = B.ID AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND C.ORDSQ3 = 0
AND A.INPART = D.INPART AND A.ORDSQ2 <= D.�s�{��+1


DROP TABLE #����zDLYTIME_NEW_E
DROP TABLE #����zDLYTIME_E




-------���s�p��ƨ�{�����ɶ� 2023/09/11 Techup-----------------------
--EXEC dbo.����ORDE3�Ѿl�s�{ '23Q01075-0-003-003'
--EXEC dbo.����ORDE3�Ѿl�s�{ ''
SELECT A.INPART �s�d,A.ORDSQ2 �e��ORDSQ2,A.ORDFO �e���s�{,A.PRTFM �e�����u,B.ORDSQ2 �U��ORDSQ2,
B.ORDFO �U���s�{,B.PRTFM �U�����u,B.DLYTIME
INTO #PRODTM_S
FROM (SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDFO = '��' AND ORDSQ3 = 0) A,(
SELECT A.* FROM ORDDE4_�Ѿl�s�{����_����_D A,(
SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D A,#SOPNAME B WHERE ORDSQ2 > 0
AND A.SOPKIND NOT IN ('�]�p') AND A.PRDNAME NOT LIKE '%IQC%' AND ORDFCO <> 'C' AND A.ORDFO = B.PRDOPNO  AND ORDSQ3 = 0
AND B.ISACTIVE = 0 GROUP BY INPART) B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND ORDSQ3 = 0
) B WHERE A.INPART = B.INPART
--AND ISNULL(A.PRTFM,'') <> '' AND ISNULL(B.PRTFM,'') <> ''
AND A.INPART LIKE @INPART
--AND A.INPART = '23F01112-0-000-F1-01'

----�e���p�G�٨S�o�� �����N���Ӧ�DLYTIME 2023/09/14 Techup
UPDATE #PRODTM_S SET DLYTIME = 0
WHERE ISNULL(�e�����u,'') = '' AND ISNULL(�U�����u,'') = ''

----2023/12/20 ���s��s�d�ܦ�������������� �Q�p��X�j�q���n �S���u�ϦӨS�����n�����D �ҥH�Ȥ��[�J�P�_ �v
--UPDATE #PRODTM_S SET DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(�e�����u,�U�����u,10)/60.00
--WHERE ISNULL(�e�����u,'') <> '' AND ISNULL(�U�����u,'') <> ''

UPDATE #PRODTM_S SET DLYTIME = 0 WHERE DLYTIME < 0

SELECT �e�����u,�U�����u,DLYTIME
INTO #PRODTM_S_�X��
FROM #PRODTM_S
GROUP BY �e�����u,�U�����u,DLYTIME

UPDATE #PRODTM_S_�X��
SET DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(�e�����u,�U�����u,@DLYTIME�C��u�@�p��)/60.00 --B.DLYTIME 2024/01/12 Techup �Ƶo��{�����ɶ�

UPDATE #PRODTM_S
SET DLYTIME = B.DLYTIME
FROM #PRODTM_S A, #PRODTM_S_�X�� B
WHERE ISNULL(A.�e�����u,'') = ISNULL(B.�e�����u,'') AND ISNULL(A.�U�����u,'') = ISNULL(B.�U�����u,'')


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = B.DLYTIME --2024/01/12 Techup �Ƶo��{�����ɶ�
FROM ORDDE4_�Ѿl�s�{����_����_D A,#PRODTM_S B
WHERE A.INPART = B.�s�d AND A.ORDSQ2 = B.�U��ORDSQ2
-------���s�p��ƨ�{�����ɶ� 2023/09/11 Techup-----------------------




UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = 0 WHERE DLYTIME < 0 AND INPART LIKE @INPART
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME_O = 0 WHERE DLYTIME_O < 0 AND INPART LIKE @INPART

----�J��}�߲��`�檺�s�s�dDLYTIME �N�q���`��PC�T�{���� �p�� 2023/06/06 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(PCDATE,PRTFM,10)/60.00
--SELECT *
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_���`�� B,
(SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDSQ2 >= 0 AND ORDSQ3 = 0 AND ORDFCO NOT IN ('C','D') GROUP BY INPART) C
WHERE A.INPART LIKE @INPART AND
A.ORDSQ2 = C.ORDSQ2 AND A.INPART = C.INPART AND ORDSQ3 = 0 AND DLYTIME > 0 AND A.INPART = B.INPART
-----�w��� ��]�p���_�I �i��α� 2023/06/05 Techup ------------------------------------------




--------------------����γ]�p��ƪ���z 2023/02/17 Techup------------------------------------------

--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D WHERE INPART = '22G05906ML-001-002#7'
-- EXEC dbo.����ORDE3�Ѿl�s�{ '22G05906ML-001-002#7'


----- 2023/07/17 �v �w�qDRW26640 ���D5XMY ���x��
DECLARE @���x��_26640  INT
SET @���x��_26640 = CASE WHEN (SELECT COUNT(*) FROM MACPRD1 WHERE MAHNO IN('5XMY01','5XMY02','5XMY03') AND UTILRATE <> 0 ) > 0
THEN (SELECT COUNT(*) FROM MACPRD1 WHERE MAHNO IN('5XMY01','5XMY02','5XMY03') AND UTILRATE <> 0 )
ELSE 1 END

-- EXEC dbo.����ORDE3�Ѿl�s�{ '24G03037SL-000#1'

--�[�J�~�s�u�� �@�ѥ�20�p�� Techup 2022/12/28
SELECT A.INPART,ORDFM1 = CONVERT(DECIMAL(10,2),CONVERT(FLOAT,
SUM(
CASE
WHEN B.INDWG = 'DRW26640' AND A.ORDFO IN ('I01','I02','I03','I04','I05','I06','I07','I08','I09','I10') THEN A.ORDFM1 / @���x��_26640 ---- 2023/07/17 �R��n�D
    --WHEN ORDFO LIKE 'CQ%' THEN 10*5*60
WHEN A.ORDDTP = 1 THEN CASE WHEN A.ORDFM1 < C.ORDMT3 THEN 0 ELSE A.ORDFM1 - ISNULL(C.ORDMT3,0) END ----��w�����u�B�j��w��u�� �h��u�ɧ�0 �ϥ��h�n�����w���u 2024/03/29 Techup
WHEN A.ORDDTP = 2 THEN REPLACE(�~�]�w�p�Ѽ�,'D','')*10*60  -----�~�] ���ƪ��n�D�A�ק�^10hr �p�� 2025/05/12 Techup
---WHEN A.ORDDTP = 2 THEN REPLACE(�~�]�w�p�Ѽ�,'D','')*24*60 ------�~�]���24hr�p�� 2025/04/10 Techup
END
) /60))
INTO #�Ѿl�u�ɮɼ�
FROM ORDDE4_�Ѿl�s�{����_����_D A , (SELECT INPART,INDWG FROM ORDE3) B,ORDDE4 C
WHERE A.ORDFCO = 'N' AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT LIKE '%��%' AND ORDSQ3 = 0
AND A.ORDFO NOT IN ('N01','N02','N03','N04','24E','24I','24H','24J','24K','24L')
AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%')  
AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/28 Techup
----�ư��|��u�� 2024/05/27 Techup �᭱�b�N���Y��(�W�e�̤p)���@���s�d�ɤW�|��u��
---AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE SOPKIND = '�|��')  
AND A.ORDSQ2 >= -10  ---- 2023/02/18 �v �Ѿl�u�ɱư�-50 -100
AND A.INPART = B.INPART
AND A.INPART = C.ORDFNO AND A.ORDSQ2 = C.ORDSQ2
AND A.INPART LIKE @INPART
--AND ISNULL(Applier,'') <> '�~�s' ---- 2022/07/25 �v �Ѿl�u�ɱư��~�s���s�{
GROUP BY A.INPART


/******************************************************************************************
             �N�Ƥ�IQC�s�{��ܳ]�p����,���[���e  �}�l  �`���_ 2023/10/20
******************************************************************************************/
-- �u�����ƶO���s�d��J�Ȧs��,���ݳQ�B�z
SELECT ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,ORDSQ2,ORDSQ3,ORDFO,SOPKIND,ORDSQ2_OLD=ORDSQ2
  INTO #���ƪ��s�d�s�{ FROM #TEMP3 WHERE INPART IN (SELECT INPART FROM #TEMP3 WHERE ORDFO='��' AND ORDSQ3=0 AND ORDSQ2 >= 0)
ORDER BY INPART,ORDSQ2

    -- ��X�U�s�d���Ĥ@�ӳ]�p�s�{
SELECT INPART,ORDSQ2=MIN(ORDSQ2) INTO #�̤p���[�Ǹ� FROM #���ƪ��s�d�s�{
WHERE SOPKIND<>'�]�p'
--SOPKIND='���[' ---2024/07/03 �n�令���O�]�p Techup
AND ORDSQ3=0  AND ORDSQ2 >= 0 GROUP BY INPART

    -- �վ�Ȧs�ɪ��ƦC����(�Ƥ�IQC�s�{��ܳ]�p����,���[���e)
UPDATE #���ƪ��s�d�s�{ SET ORDSQ2=A.ORDSQ2+500 FROM #���ƪ��s�d�s�{ A JOIN #�̤p���[�Ǹ� B ON A.INPART=B.INPART WHERE A.ORDSQ2 >= B.ORDSQ2 AND SOPKIND<>'�]�p'
UPDATE #���ƪ��s�d�s�{ SET ORDSQ2=A.ORDSQ2+200 FROM #���ƪ��s�d�s�{ A JOIN #�̤p���[�Ǹ� B ON A.INPART=B.INPART WHERE A.ORDSQ2 < B.ORDSQ2 AND ORDFO IN ('15N','��')
SELECT *,ROW_NUMBER() OVER (PARTITION BY INPART ORDER BY ORDSQ2) AS ORDSQ2_NEW INTO #�ƦC�n����� FROM #���ƪ��s�d�s�{ ORDER BY ORDSQ2

-- �^�g�^�h����ɮ׸��
UPDATE #TEMP3 SET ROWID=B.ORDSQ2_NEW FROM #TEMP3 A JOIN #�ƦC�n����� B ON A.INPART=B.INPART AND A.ORDSQ2=B.ORDSQ2_OLD WHERE A.ORDSQ3=0

--SELECT 'AAAAAAAAA',* FROM #�ƦC�n����� ORDER BY  ORDSQ2
--SELECT 'BBBBBBBBB',* FROM #TEMP3 ORDER BY ROWID

DROP TABLE #�̤p���[�Ǹ�
DROP TABLE #���ƪ��s�d�s�{

--SELECT TOP 100 * FROM ORDDE4_�Ѿl�s�{���� WHERE INPART='23F01222A-0-000-F1-05'
/******************************************************************************************
             �N�Ƥ�IQC�s�{��ܳ]�p����,���[���e  ����
******************************************************************************************/

---- 2023/01/10 �v �W�[CQ���P�_
UPDATE #TEMP3 SET PRDNAME = REPLACE(PRDNAME,'CQ','�|��CQ') WHERE PRDNAME LIKE '%CQ%'
UPDATE #TOT3 SET PRDNAME = REPLACE(PRDNAME,'CQ','�|��CQ') WHERE PRDNAME LIKE '%CQ%'







--�[�J���
    --ALTER TABLE #TEMP3 ADD ROWID_FOR�Ѿl�s�{����4 INT


select ROW_NUMBER() OVER (PARTITION BY INPART ORDER BY ORDSQ2) AS ROWID_FOR�Ѿl�s�{����4
,*
INTO #ROWID_FOR�Ѿl�s�{����4
from #TEMP3
WHERE --PRDNAME NOT LIKE '%��%' AND
PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%') -- 2024/05/29 �v �ư��O�Ϊ��s�{
ORDER BY ORDSQ2


--SELECT *
--INTO ROWID_FOR�Ѿl�s�{����4
--FROM #ROWID_FOR�Ѿl�s�{����4
----WHERE INPART = '24G01070-0-002'
----ORDER BY ORDSQ2


-- EXEC dbo.����ORDE3�Ѿl�s�{ '24G01070-0-002'
-- EXEC dbo.����ORDE3�Ѿl�s�{ ''
-----���������O�k���ո˻s�{ 2024/08/29 Techup
-----��X�e�T�����s�d�s�{�� 2024/08/28 Techup
SELECT A.*
INTO #ROWID_FOR�Ѿl�s�{����4_����e�X��
FROM #ROWID_FOR�Ѿl�s�{����4 A LEFT OUTER JOIN
(
SELECT * FROM #ROWID_FOR�Ѿl�s�{����4 WHERE PRDNAME LIKE '%'+CHAR(10)+'%'
AND PRDNAME NOT LIKE '%AS%' AND PRDNAME NOT LIKE '%ASF%' AND PRDNAME NOT LIKE '%ASCG%'
AND PRDNAME NOT LIKE '%WD%' AND PRDNAME NOT LIKE '%LSWD%'
) B
ON A.INPART = B.INPART AND
----���ƪ��� �S���n���n�ݫe�T�� 2024/12/10 Techup
----���ƪ��� �S���n��^ 2024/10/14 Techup
----���ƪ��� �����ĴX�� ��즨�e�T�� 2024/10/11 Techup
A.ROWID_FOR�Ѿl�s�{����4 = B.ROWID_FOR�Ѿl�s�{����4
--A.ROWID_FOR�Ѿl�s�{����4 = B.ROWID_FOR�Ѿl�s�{����4-3
--A.ORDDY4 >= DATEADD(DD,-3, GETDATE()) ----���ƪ��� �����ĴX�� ��즨�e�T�� 2024/10/11 Techup
WHERE ISNULL(B.INPART,'') <> '' ----
ORDER BY A.INPART,A.ORDSQ2

-----�ɤJ�S�i�h���s�{
INSERT INTO #ROWID_FOR�Ѿl�s�{����4_����e�X��
SELECT A.* FROM #ROWID_FOR�Ѿl�s�{����4 A,(SELECT * FROM #ROWID_FOR�Ѿl�s�{����4 WHERE PRDNAME LIKE '%'+CHAR(10)+'%') B
WHERE A.INPART NOT IN (SELECT INPART FROM #ROWID_FOR�Ѿl�s�{����4_����e�X��)
AND A.INPART = B.INPART AND A.ROWID_FOR�Ѿl�s�{����4 = B.ROWID_FOR�Ѿl�s�{����4
--AND A.INPART = '24G04255SL-000'
ORDER BY A.ORDSQ2

-----�ɤJ�S�i�h���s�{
INSERT INTO #ROWID_FOR�Ѿl�s�{����4_����e�X��
SELECT A.* FROM #ROWID_FOR�Ѿl�s�{����4 A,
(SELECT INPART,MIN(ROWID_FOR�Ѿl�s�{����4) ROWID_FOR�Ѿl�s�{����4 FROM #ROWID_FOR�Ѿl�s�{����4
GROUP BY INPART
) B
WHERE A.INPART NOT IN (SELECT INPART FROM #ROWID_FOR�Ѿl�s�{����4_����e�X��)
AND A.INPART = B.INPART AND A.ROWID_FOR�Ѿl�s�{����4 = B.ROWID_FOR�Ѿl�s�{����4
--AND A.INPART = '24G04255SL-000'
ORDER BY A.ORDSQ2

--SELECT * FROM #TEMP3 A,#ROWID_FOR�Ѿl�s�{����4 B
--WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2
--AND A.ORDSQ3 = 0

-- EXEC dbo.����ORDE3�Ѿl�s�{ '23Q03364-000R8'
--SELECT 'AAAAAAABBBBBB',* FROM #TEMP3
--ORDER BY ORDSQ2

--SELECT * FROM #ROWID_FOR�Ѿl�s�{����4_����e�X��
--ORDER BY ORDSQ2


----�B�z���h��CHAR(10) 2024/11/06 Techup
UPDATE #TEMP3
SET PRDNAME = CHAR(10)+REPLACE(PRDNAME,CHAR(10),'')
WHERE PRDNAME LIKE '%' + char(10) + '%'







IF (@INPART = '%')
BEGIN
--�P�_table�O�_�s�b
if exists (select name from sysobjects where name = 'ORDDE4_�Ѿl�s�{����_D')
DELETE ORDDE4_�Ѿl�s�{����_D
--DROP TABLE ORDDE4_�Ѿl�s�{����_D

--SELECT datepart(dd, '2021-03-05 10:50:00.000')



INSERT INTO ORDDE4_�Ѿl�s�{����_D
SELECT *,�Ѿl�s�{���� =
(
SELECT

CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) +(CASE WHEN ISNULL(Applier,'') = '' THEN '' ELSE '*' END)+ ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'
-----2025/12/15 �����U��Ϊk Techup
--CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'    ------AAAAAAAAAAAAAAAAAAA
----CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
----cast(PRDNAME AS NVARCHAR ) +
--WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'
--+ (CASE WHEN ISNULL(ORDDY2,'') <> '' THEN '�i'+
--Right('0' + CONVERT(VARCHAR(100),datepart(dd, ORDDY2)),2)
--+' '+
--(CASE WHEN datepart(HH, ORDDY2) > 12 THEN 'P'+ CONVERT(VARCHAR(100),datepart(HH, ORDDY2)-12) ELSE 'A' + CONVERT(VARCHAR(100),datepart(HH, ORDDY2)) END)
--+'�j' ELSE '' END ) --�[�J�������� 2020/06/12 Techup


--ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')'
--END
--+ '��'
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART  AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
ORDER BY ROWID
FOR XML PATH('')

),�Ѿl�s�{����2 =
(
SELECT
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '��,' ELSE '' END +
cast(PRDNAME AS NVARCHAR(30) ) + ','
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART AND ORDSQ3 = 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
ORDER BY ROWID
FOR XML PATH('')
),�Ѿl�s�{����3 =
(
SELECT
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '��;' ELSE '' END +
cast(PRDNAME AS NVARCHAR(30) ) + ';' + cast(ORDSQ2 AS NVARCHAR(30) ) +','
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART AND ORDSQ3 = 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
ORDER BY ROWID
FOR XML PATH('')
),�Ѿl�� =
(
SELECT AA = COUNT(*) FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART

),���TOTAL =
(
SELECT
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
CASE WHEN ORDAMT > 0 AND ORDQY2 > 0  THEN '��'+'('+cast(CONVERT(int, ORDAMT/ORDQY2) AS NVARCHAR(30) )+')'  +'��' ELSE '' END +   --2020/09/18
CASE WHEN ORDFO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'  
--cast(PRDNAME AS NVARCHAR ) +
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,3),���u��/60))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL,CONVERT(DECIMAL(12,0),ORDUPR)/ORDQY2)) + ')' END
+ '��'
FROM #TOT3
WHERE #TOT3.INPART = #�s�d����.INPART AND #TOT3.PRDNAME NOT LIKE 'Z%' AND #TOT3.PRDNAME <> 'lo' AND #TOT3.SOPKIND NOT IN ('�]�p')
AND ORDQY2 > 0  
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
--WHERE  #TOT3.PRDNAME NOT LIKE 'Z%' AND #TOT3.PRDNAME <> 'lo'
ORDER BY INPART,ORDSQ2
FOR XML PATH('')

)
,TOTAL =
(
SELECT
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
CASE WHEN ORDAMT > 0 AND ORDQY2 > 0 THEN '��'+'('+cast(CONVERT(int, ORDAMT/ORDQY2) AS NVARCHAR(30) )+')'  +'��' ELSE '' END +  --2020/09/18
CASE WHEN ORDFO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'  
--cast(PRDNAME AS NVARCHAR ) +
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL,CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'
FROM #TOT3
WHERE #TOT3.INPART = #�s�d����.INPART AND #TOT3.PRDNAME NOT LIKE 'Z%' AND #TOT3.PRDNAME <> 'lo'
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%' OR SOPKIND = '�|��')
AND ORDQY2 > 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
--WHERE   #TOT3.PRDNAME NOT LIKE 'Z%' AND #TOT3.PRDNAME <> 'lo'
--ORDER BY INPART,ORDSQ2
FOR XML PATH('')

)
,�e�m�]�p�u�� = 0
,�e�m�]�p�w�p������ = ''
,TOTAL�u�� =
(
SELECT AA =  
CONVERT(DECIMAL(10,2),CONVERT(FLOAT,
SUM(CASE
WHEN ORDDTP = 1 THEN ORDFM1
WHEN ORDDTP = 2 THEN REPLACE(�~�]�w�p�Ѽ�,'D','')*10*60 --�~�]�]�n�ΤѼƺ�u�� 2023/04/08 Techup
END
) /60))
FROM #TEMP3 A,#SOPNAME B
WHERE A.INPART = #�s�d����.INPART AND A.PRDNAME NOT LIKE '%��%' AND ORDSQ3 = 0
AND ORDFO NOT IN ('N01','N02','N03','N04','24E','24I','24H','24J','24K','24L')
AND B.PRDNAME NOT IN ('lo','uld','LD','ULD','am')
AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%' OR SOPKIND = '�|��')
AND ORDFCO <> 'C' ---2025/05/27 Techup C�����s�{����ܡ@
AND A.ORDFO = B.PRDOPNO --AND ISACTIVE = 0 ----���� 2024/07/02 Techup
       AND ORDSQ2 >= -10  
)
,TOTAL�u�ɹw�p������ = ''
,CUS�u�� = 0
,�e��Ѿl�u�� = 0
,�Ѿl�u�� =
ISNULL((
    SELECT TOP 1 ORDFM1 FROM #�Ѿl�u�ɮɼ� WHERE INPART = #�s�d����.INPART
--SELECT AA = ISNULL(SUM(ORDFM1)/60.00,0) FROM #TEMP3
--WHERE #TEMP3.INPART = #�s�d����.INPART AND PRDNAME NOT LIKE '%��%' AND PRDNAME NOT LIKE '%��%'
),0)
,���Ĥu�� = 0
,�i�Τu�� = 0
,�S�O�i�Τu�� = 0
,AutoPc = 'N',
�Ѿl�s�{����4 =
(
SELECT CASE WHEN A.ORDFO LIKE '%��%' AND A.ORDFCO = 'N' THEN A.PRDNAME+'('+cast(CONVERT(bigint, A.ORDAMT) AS NVARCHAR(30) )+')'  
WHEN A.ORDDTP = 1 AND A.ORDFO NOT LIKE '%��%' THEN cast(A.PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),A.ORDFM1/60))) +(CASE WHEN ISNULL(A.Applier,'') = '' THEN '' ELSE '*' END)+ ')'
ELSE cast(A.PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),A.ORDUPR))) + ')' END
+ '��'
FROM (
SELECT distinct A.*
FROM #TEMP3 A , #ROWID_FOR�Ѿl�s�{����4_����e�X�� B
WHERE A.INPART = #�s�d����.INPART  
--AND PRDNAME NOT LIKE '%��%'
AND A.PRDNAME NOT LIKE 'Z%' AND A.PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND A.ORDSQ3 = 0
AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%') -- 2024/05/29 �v �ư��O�Ϊ��s�{
AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
--AND EXISTS (SELECT * FROM #ROWID_FOR�Ѿl�s�{����4_����e�X�� WHERE #TEMP3.INPART= #ROWID_FOR�Ѿl�s�{����4_����e�X��.INPART
--AND #ROWID_FOR�Ѿl�s�{����4_����e�X��.ORDSQ2 <= #TEMP3.ORDSQ2)
AND A.INPART = B.INPART AND A.ORDSQ2 >= B.ORDSQ2
) A
ORDER BY A.ROWID
FOR XML PATH('')





--SELECT
--CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')' + '��'
----CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
----cast(PRDNAME AS NVARCHAR ) +
--WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + (CASE WHEN ISNULL(Applier,'') = '' THEN '' ELSE '*' END)+')' + '��'
--WHEN ORDFCO = 'Y' AND ORDDTP = '2' THEN ''
--ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' + '��' END
--FROM #TEMP3
--WHERE #TEMP3.INPART = #�s�d����.INPART AND PRDNAME NOT LIKE '%��%' AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
--AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%') -- 2024/05/29 �v �ư��O�Ϊ��s�{
--ORDER BY ROWID
--FOR XML PATH('')
)
,�����s�{���ӧtDLYTIME =
(

--SELECT 121/5

SELECT CASE
--�~�]
WHEN #TEMP3.ORDDTP = 2 THEN cast(REPLACE(REPLACE(REPLACE(#TEMP3.PRDNAME,'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) )
                           +'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(18,0),CONVERT(DECIMAL(18,0),ORDUPR))) + ')'
WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN
--(CASE WHEN #TEMP3.ORDSQ2 = (SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld') AND ORDFCO = 'N' AND A.INPART = #TEMP3.INPART GROUP BY INPART)
--THEN '��' ELSE '' END ) +

cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'

+ (CASE WHEN ISNULL(ORDDY2,'') <> '' THEN '�i'+
Right('0' + CONVERT(VARCHAR(100),datepart(dd, ORDDY2)),2)
+' '+
(CASE WHEN datepart(HH, ORDDY2) > 12 THEN 'P'+ CONVERT(VARCHAR(100),datepart(HH, ORDDY2)-12) ELSE 'A' + CONVERT(VARCHAR(100),datepart(HH, ORDDY2)) END)
+'�j' ELSE '' END ) --�[�J�������� 2020/06/12 Techup

--EXEC dbo.����ORDE3�Ѿl�s�{ '20C03105-000'

--+ (CASE WHEN DLYTIME > 0 THEN '�i'+convert(varchar,FLOOR(DLYTIME))+'�j' ELSE '' END)

--+
--(CASE
--     WHEN FLOOR(DLYTIME/5) = 1 AND ORDFCO = 'N' THEN '��'
-- WHEN FLOOR(DLYTIME/5) = 2 AND ORDFCO = 'N' THEN '����'
-- WHEN FLOOR(DLYTIME/5) = 3 AND ORDFCO = 'N' THEN '������'
-- WHEN FLOOR(DLYTIME/5) = 4 AND ORDFCO = 'N' THEN '��������'
-- WHEN FLOOR(DLYTIME/5) = 5 AND ORDFCO = 'N' THEN '����������'
-- WHEN FLOOR(DLYTIME/5) = 6 AND ORDFCO = 'N' THEN '������������'
-- WHEN FLOOR(DLYTIME/5) = 7 AND ORDFCO = 'N' THEN '��������������'
-- WHEN FLOOR(DLYTIME/5) = 8 AND ORDFCO = 'N' THEN '����������������'
-- WHEN FLOOR(DLYTIME/5) = 9 AND ORDFCO = 'N' THEN '������������������'
-- WHEN FLOOR(DLYTIME/5) >= 10 AND ORDFCO = 'N' THEN '��������������������'
-- ELSE ''END)



ELSE

--(CASE WHEN #TEMP3.ORDSQ2 = (SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld') AND ORDFCO = 'N' AND A.INPART = #TEMP3.INPART GROUP BY INPART)
--THEN '��' ELSE '' END ) +

cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART
AND PRDNAME NOT LIKE 'Z%' --AND PRDNAME NOT LIKE '%��%'
AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
ORDER BY ROWID
FOR XML PATH('')

)
,�����s�{���ӧtDLYTIME_���t�]�p = --CAST('' AS VARCHAR(50))
(
--SELECT CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
--WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN
--cast(PRDNAME AS NVARCHAR(30) ) + ISNULL(�t���Ѽ�,'')  + '(' + CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),ORDFM1/60))) +
----CASE WHEN DLYTIME > 0 THEN '/'+CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),DLYTIME))) ELSE '' END ---- �v 2021/06/16 ����
--   ')'
--+ (CASE WHEN ISNULL(ORDDY2,'') <> '' THEN '�i'+
--Right('0' + CONVERT(VARCHAR(100),datepart(dd, ORDDY2)),2)
--+' '+
--(CASE WHEN datepart(HH, ORDDY2) > 12 THEN 'P'+ CONVERT(VARCHAR(100),datepart(HH, ORDDY2)-12) ELSE 'A' + CONVERT(VARCHAR(100),datepart(HH, ORDDY2)) END)
--+'�j' ELSE '' END ) --�[�J�������� 2020/06/12 Techup
--ELSE
--cast(PRDNAME AS NVARCHAR(30) ) + ISNULL(�t���Ѽ�,'')
--+'(' + CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
--+ '��'
--FROM #TEMP3
--WHERE #TEMP3.INPART = #�s�d����.INPART
--AND PRDNAME NOT LIKE 'Z%' --AND PRDNAME NOT LIKE '%��%' -- 2021/04/13
--AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE SOPKIND = '�]�p') AND ORDFO NOT LIKE '%��%'
--AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
--ORDER BY ROWID
--FOR XML PATH('')

SELECT CASE
--�~�]
WHEN A.ORDDTP = 2 THEN cast(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) ) +
  CASE WHEN A.INPART LIKE '%Z%' THEN '(' +
                                       CONVERT(VARCHAR(100),CONVERT(DECIMAL(18,0),CONVERT(DECIMAL(18,0),A.ORDUPR))) +
  ')'
  ELSE
  (CASE WHEN ISNULL(�~�]�w�p�Ѽ�,'') = '' THEN ''
  ELSE
  '(' +  
  �~�]�w�p�Ѽ�  
  +
  ')' END) END
WHEN A.ORDFO LIKE '%��%' AND A.ORDFCO = 'N' THEN REPLACE(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),'��',''),CHAR(10),'������')+'('+cast(CONVERT(bigint, A.ORDAMT) AS NVARCHAR(30) ) + ')'  
           WHEN A.ORDDTP = 1 AND A.ORDFO NOT LIKE '%��%' THEN cast(REPLACE(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) )
    --+ ISNULL(�t���Ѽ�,'')
    +
CASE WHEN A.ORDFO LIKE 'CQ%' THEN '(5D)'
WHEN A.ORDFO LIKE '280' THEN '(1D)'
WHEN ISNULL(A.�t���Ѽ�,'') = '' THEN '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),A.ORDFM1/60))) + ')'
ELSE '' END +
--�P�_�|�����u�B DLYTIME = 0 �N��ܪ� -- 2021/11/30 TECHUP
CASE WHEN A.DLYTIME = 0 AND ISNULL(A.�t���Ѽ�,'') = '' THEN '' ELSE

--2022/07/14 CLOSE
''
--'�i'+ CASE WHEN A.DLYTIME > 0 THEN
-- CASE WHEN CONVERT(DECIMAL(12,2),A.DLYTIME) - CONVERT(DECIMAL(12,2),A.ORDFM1/60) < 0 THEN '0' ELSE CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),A.DLYTIME) - CONVERT(DECIMAL(12,2),A.ORDFM1/60))) END
--  ELSE '0' END +'�j'
END --�[�JDLYTIME


ELSE cast(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) )
--+ ISNULL(�t���Ѽ�,'')
+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),A.ORDUPR))) + ')' END +
(CASE WHEN ISNULL(B.CARDNO,'') <> ''
AND ISNULL(C.REWORK,'') NOT IN ('QC�ߧY�B�z','�Ȩѫ~���}(�i��)','�i��','���o����')
THEN ('����i'+CONVERT(VARCHAR(10),C.�}�橵��ɶ�) + '�j') ELSE '' END) +
'��'
FROM #TEMP3 A LEFT OUTER JOIN (SELECT * FROM #��X�e�����`�檺_�Ѿl�s�{���� WHERE ORDSQ3 = 1) B
ON A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
 LEFT OUTER JOIN ORDDE4_�Ѿl�s�{����_���`�� C ON B.CARDNO = C.CARDNO
WHERE A.INPART = #�s�d����.INPART
AND A.PRDNAME NOT LIKE 'Z%'
AND REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),'�~','') NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE SOPKIND = '�]�p')
--AND A.ORDFO NOT LIKE '%��%'
AND A.PRDNAME NOT LIKE 'Z%' AND A.PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND A.ORDSQ3 = 0

ORDER BY A.ROWID
FOR XML PATH('')

)
,�̫���[�� = cast(0 AS int )
,�Ѿl���[�s�{ =
(
SELECT
CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'  
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART AND PRDNAME NOT LIKE '%��%' AND PRDNAME NOT LIKE '%Z%' AND #TEMP3.SOPKIND = '���[' AND ORDSQ3 = 0
ORDER BY ROWID
FOR XML PATH('')

), �Ѿl�Ƶ{����=CAST('' AS VARCHAR(MAX))
----- 2019/02/27 ADD
,   �s�y��=(SELECT MAX(ORDQTY) FROM #TOT3 WHERE #TOT3.INPART = #�s�d����.INPART )
,   ���ƶO=(SELECT SUM(ORDAMT) FROM #TOT3 WHERE #TOT3.INPART = #�s�d����.INPART )
, QC���O=(SELECT CASE WHEN SUM(#TOT3.ORDAMT) >=10000 OR MAX(#TOT3.ORDQTY) >= 5 THEN '���' ELSE '' END FROM #TOT3 WHERE #TOT3.INPART = #�s�d����.INPART )
,�b���s�{�� = (
SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND SOPKIND NOT IN ('�]�p') --AND ORDFO NOT IN ('27') -- ����as �v 2026/01/26
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O%' ) --�O�����N����
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('EPM','E3Q') ) --EPM �M E3Q �N����A�s 2024/10/17 ���ƪ��n�D�ҥ~�B�z
AND ORDFCO = 'N' AND A.INPART = #�s�d����.INPART AND ORDSQ3 = 0 GROUP BY INPART)
,�b���s�{�ǫe����DLYTIME = 0
,U_INPART=CAST('' AS VARCHAR(40)),AKT�j�����~����=CAST('' AS VARCHAR(40))
,�Q����u�u�� = (SELECT SUM(PRTIME)/60.0 PRTIME FROM #�n�B�̪�PRODTM WHERE CRDATE >=  convert(varchar, DATEADD(DD,-1,GETDATE()), 111) + ' 00:00' AND PRTFM <= convert(varchar, GETDATE(), 111) + ' 00:00'
                       AND PTPNO = #�s�d����.INPART )
,�}��s�d = (
SELECT distinct  TOP 1 OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = #�s�d����.INPART
)
,�}��e�s�d�s�{����
= (
SELECT
CASE WHEN ORDFO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'  
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' AND ORDSQ3 = '0' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'+
                                                               CASE WHEN ����ɶ� > 0 THEN '/'+CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),����ɶ�))) ELSE '' END
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' AND ORDSQ3 > '0' THEN PRDNAME+'/'+cast(CARDNO AS NVARCHAR(10) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),����ɶ�))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')'
END
+ '��'
FROM #��X�e�����`�檺_�Ѿl�s�{���� A,(SELECT distinct INPART,OLDPART,ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = #�s�d����.INPART) B
WHERE A.INPART = B.OLDPART AND A.ORDSQ2 <= B.ORDSQ2
AND PRDNAME NOT LIKE '%Z%'
ORDER BY ROWID
FOR XML PATH('')
--),�u���m = ISNULL((SELECT AA FROM #SFC2468NET_��B��� WHERE �s�y�s�� = #�s�d����.INPART),(SELECT STATUS FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART))
--,��m�ɶ� = ISNULL((SELECT ���f�ɶ� FROM #SFC2468NET_��B��� WHERE �s�y�s�� = #�s�d����.INPART),(SELECT ISNULL(�ɶ�,'') FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART))
),�u���m = ISNULL((SELECT STATUS FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART),'')
,��m�ɶ� = ISNULL((SELECT ISNULL(�ɶ�,'') FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART),'')
,'',0,'',0,'','',0,0 ----�s�W��� �o��n�O�o�� 2025/11/13
----- 2019/02/27

FROM #�s�d����
ORDER BY INPART
-- EXEC dbo.����ORDE3�Ѿl�s�{ '20Y03103-000'
--EXEC dbo.����ORDE3�Ѿl�s�{ ''

--SELECT * FROM ORDDE4_�Ѿl�s�{����_D
--WHERE �b���s�{�� > 1
----WHERE INPART = '19D04485AF-000#18'

--SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld') AND SOPKIND NOT IN ('�]�p')
-- AND PRDNAME NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE DESCR LIKE '%�O%' AND PRDNAME <> 'AT') --�O�����N����
-- AND ORDFCO = 'N' AND A.INPART = '20Y03103-000' GROUP BY INPART


END
ELSE
BEGIN

if exists (select name from sysobjects where name = 'ORDDE4_�Ѿl�s�{����_D')
DELETE ORDDE4_�Ѿl�s�{����_D WHERE INPART LIKE @INPART




--CONVERT(int, ORDAMT) AS int

INSERT INTO ORDDE4_�Ѿl�s�{����_D
SELECT *,�Ѿl�s�{���� =
(
SELECT

CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) +(CASE WHEN ISNULL(Applier,'') = '' THEN '' ELSE '*' END)+ ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'

-------�����U��Ϊk 2025/12/15 Techup
--CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'    ------AAAAAAAAAAAAAAAAAAA
----CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
----cast(PRDNAME AS NVARCHAR ) +
--WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'
--+ (CASE WHEN ISNULL(ORDDY2,'') <> '' THEN '�i'+
--Right('0' + CONVERT(VARCHAR(100),datepart(dd, ORDDY2)),2)
--+' '+
--(CASE WHEN datepart(HH, ORDDY2) > 12 THEN 'P'+ CONVERT(VARCHAR(100),datepart(HH, ORDDY2)-12) ELSE 'A' + CONVERT(VARCHAR(100),datepart(HH, ORDDY2)) END)
--+'�j' ELSE '' END ) --�[�J�������� 2020/06/12 Techup


--ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')'
--END
--+ '��'
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART  AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
ORDER BY ROWID
FOR XML PATH('')
),�Ѿl�s�{����2 =
(
SELECT
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '��,' ELSE '' END +
cast(PRDNAME AS NVARCHAR(30) ) + ','

FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART AND ORDSQ3 = 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
ORDER BY ROWID
FOR XML PATH('')
),�Ѿl�s�{����3 =
(
SELECT
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '��;' ELSE '' END +
cast(PRDNAME AS NVARCHAR(30) ) + ';' + cast(ORDSQ2 AS NVARCHAR(30) ) +','
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART AND ORDSQ3 = 0
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
ORDER BY ROWID
FOR XML PATH('')
),�Ѿl�� =
(
SELECT AA = COUNT(*) FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART AND ORDSQ3 = 0
)
,���TOTAL =
(
SELECT
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
CASE WHEN ORDAMT > 0 AND ORDQY2 > 0 THEN '��'+'('+cast(CONVERT(int, ORDAMT/ORDQY2) AS NVARCHAR(30) )+')'  +'��' ELSE '' END +   --2020/09/18
CASE WHEN ORDFO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
--cast(PRDNAME AS NVARCHAR ) +
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),���u��/60))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL,CONVERT(DECIMAL(12,0),ORDUPR)/ORDQY2)) + ')' END
+ '��'
FROM #TOT3
WHERE #TOT3.INPART = #�s�d����.INPART AND #TOT3.PRDNAME NOT LIKE 'Z%' AND #TOT3.PRDNAME <> 'lo' AND #TOT3.SOPKIND NOT IN ('�]�p')
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
--ORDER BY INPART,ORDSQ2
FOR XML PATH('')

)

,TOTAL =
(
SELECT
CASE WHEN ORDAMT > 0 AND ORDQY2 > 0 THEN '��'+'('+cast(CONVERT(int, ORDAMT/ORDQY2) AS NVARCHAR(30) )+')'  +'��' ELSE '' END +   --2020/09/18
CASE WHEN ORDFO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
--CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
--cast(PRDNAME AS NVARCHAR ) +
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL,CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'
FROM #TOT3
WHERE #TOT3.INPART = #�s�d����.INPART AND #TOT3.PRDNAME NOT LIKE 'Z%' AND #TOT3.PRDNAME <> 'lo'
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%' OR SOPKIND = '�|��')
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
--ORDER BY INPART,ORDSQ2
FOR XML PATH('')

),�e�m�]�p�u�� = 0
,�e�m�]�p�w�p������ = ''
,TOTAL�u�� =
(
SELECT AA =  
CONVERT(DECIMAL(10,2),CONVERT(FLOAT,
SUM(CASE
WHEN ORDDTP = 1 THEN ORDFM1
WHEN ORDDTP = 2 THEN REPLACE(�~�]�w�p�Ѽ�,'D','')*10*60 --�~�]�]�n�ΤѼƺ�u�� 2023/04/08 Techup
END
) /60))
FROM #TEMP3 A,#SOPNAME B
WHERE A.INPART = #�s�d����.INPART AND A.PRDNAME NOT LIKE '%��%' AND ORDSQ3 = 0
AND ORDFO NOT IN ('N01','N02','N03','N04','24E','24I','24H','24J','24K','24L')
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%' OR SOPKIND = '�|��')
AND B.PRDNAME NOT IN ('lo','uld','LD','ULD','am')
AND A.ORDFO = B.PRDOPNO --AND ISACTIVE = 0 ----���� 2024/07/02 Techup
AND ORDFCO <> 'C' ---2025/05/27 Techup C�����s�{����ܡ@
AND ORDSQ2 >= -10  
)
,TOTAL�u�ɹw�p������ = ''
,CUS�u�� = 0
,�e��Ѿl�u�� = 0
,�Ѿl�u�� =
ISNULL((
SELECT TOP 1 ORDFM1 FROM #�Ѿl�u�ɮɼ� WHERE INPART = #�s�d����.INPART
--SELECT AA = ISNULL(SUM(ORDFM1)/60.00,0) FROM #TEMP3
--WHERE #TEMP3.INPART = #�s�d����.INPART AND PRDNAME NOT LIKE '%��%' AND PRDNAME NOT LIKE '%��%'
),0)
,���Ĥu�� = 0
,�i�Τu�� = 0
,�S�O�i�Τu�� = 0
,AutoPc = 'N',
�Ѿl�s�{����4 =
(
SELECT CASE WHEN A.ORDFO LIKE '%��%' AND A.ORDFCO = 'N' THEN A.PRDNAME+'('+cast(CONVERT(bigint, A.ORDAMT) AS NVARCHAR(30) )+')'  
WHEN A.ORDDTP = 1 AND A.ORDFO NOT LIKE '%��%' THEN cast(A.PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),A.ORDFM1/60))) +(CASE WHEN ISNULL(A.Applier,'') = '' THEN '' ELSE '*' END)+ ')'
ELSE cast(A.PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),A.ORDUPR))) + ')' END
+ '��'
FROM (
SELECT distinct A.*
FROM #TEMP3 A , #ROWID_FOR�Ѿl�s�{����4_����e�X�� B
WHERE A.INPART = #�s�d����.INPART  
--AND PRDNAME NOT LIKE '%��%'
AND A.PRDNAME NOT LIKE 'Z%' AND A.PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND A.ORDSQ3 = 0
AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%') -- 2024/05/29 �v �ư��O�Ϊ��s�{
--AND EXISTS (SELECT * FROM #ROWID_FOR�Ѿl�s�{����4_����e�X�� WHERE #TEMP3.INPART= #ROWID_FOR�Ѿl�s�{����4_����e�X��.INPART
--AND #ROWID_FOR�Ѿl�s�{����4_����e�X��.ORDSQ2 <= #TEMP3.ORDSQ2)
AND A.INPART = B.INPART AND A.ORDSQ2 >= B.ORDSQ2
AND A.ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP  = 'BEA') -----����ܶ������s�{ 2025/10/21 Techup
) A
ORDER BY A.ROWID
FOR XML PATH('')


--SELECT
--CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')' + '��'
----CASE WHEN ORDAMT > 0 AND PRDNAME NOT LIKE '%��%' THEN '�ơ�' ELSE '' END +
----cast(PRDNAME AS NVARCHAR ) +
--WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + (CASE WHEN ISNULL(Applier,'') = '' THEN '' ELSE '*' END)+')' + '��'
--WHEN ORDFCO = 'Y' AND ORDDTP = '2' THEN ''
--ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' + '��' END
--FROM #TEMP3
--WHERE #TEMP3.INPART = #�s�d����.INPART AND PRDNAME NOT LIKE '%��%' AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
--AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O��%') -- 2024/05/29 �v �ư��O�Ϊ��s�{
--ORDER BY ROWID
--FOR XML PATH('')

)
,�����s�{���ӧtDLYTIME =
(
SELECT CASE
   --�~�]
WHEN #TEMP3.ORDDTP = 2 THEN cast(REPLACE(REPLACE(REPLACE(#TEMP3.PRDNAME,'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) )
                          +'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(18,0),CONVERT(DECIMAL(18,0),ORDUPR))) + ')'
   WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  

WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN

----SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld') AND ORDFCO = 'N' AND A.INPART = #�s�d����.INPART GROUP BY INPART
--(CASE WHEN #TEMP3.ORDSQ2 = (SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld') AND ORDFCO = 'N' AND A.INPART = #TEMP3.INPART GROUP BY INPART)
--THEN '��' ELSE '' END ) +

cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'


+ (CASE WHEN ISNULL(ORDDY2,'') <> '' THEN '�i'+
Right('0' + CONVERT(VARCHAR(100),datepart(dd, ORDDY2)),2)
+' '+
(CASE WHEN datepart(HH, ORDDY2) > 12 THEN 'P'+ CONVERT(VARCHAR(100),datepart(HH, ORDDY2)-12) ELSE 'A' + CONVERT(VARCHAR(100),datepart(HH, ORDDY2)) END)
+'�j' ELSE '' END ) --�[�J�������� 2020/06/12 Techup

--+ (CASE WHEN DLYTIME > 0 THEN '�i'+convert(varchar,FLOOR(DLYTIME))+'�j' ELSE '' END)

--+
--(CASE
--     WHEN FLOOR(DLYTIME/5) = 1 AND ORDFCO = 'N' THEN '��'
-- WHEN FLOOR(DLYTIME/5) = 2 AND ORDFCO = 'N' THEN '����'
-- WHEN FLOOR(DLYTIME/5) = 3 AND ORDFCO = 'N' THEN '������'
-- WHEN FLOOR(DLYTIME/5) = 4 AND ORDFCO = 'N' THEN '��������'
-- WHEN FLOOR(DLYTIME/5) = 5 AND ORDFCO = 'N' THEN '����������'
-- WHEN FLOOR(DLYTIME/5) = 6 AND ORDFCO = 'N' THEN '������������'
-- WHEN FLOOR(DLYTIME/5) = 7 AND ORDFCO = 'N' THEN '��������������'
-- WHEN FLOOR(DLYTIME/5) = 8 AND ORDFCO = 'N' THEN '����������������'
-- WHEN FLOOR(DLYTIME/5) = 9 AND ORDFCO = 'N' THEN '������������������'
-- WHEN FLOOR(DLYTIME/5) >= 10 AND ORDFCO = 'N' THEN '��������������������'
-- ELSE ''END)



ELSE

--(CASE WHEN #TEMP3.ORDSQ2 = (SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld') AND ORDFCO = 'N' AND A.INPART = #TEMP3.INPART GROUP BY INPART)
--THEN '��' ELSE '' END ) +

cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART
AND PRDNAME NOT LIKE 'Z%' --AND PRDNAME NOT LIKE '%��%'
AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
ORDER BY ROWID
FOR XML PATH('')

)
,�����s�{���ӧtDLYTIME_���t�]�p = --CAST('' AS VARCHAR(50))
(
--SELECT CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
-- WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN
-- cast(PRDNAME AS NVARCHAR(30) ) + ISNULL(�t���Ѽ�,'')  + '(' + CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),ORDFM1/60))) +
-- --CASE WHEN DLYTIME > 0 THEN '/'+CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),DLYTIME))) ELSE '' END ---- �v 2021/06/16 ����
-- ')'
-- + (CASE WHEN ISNULL(ORDDY2,'') <> '' THEN '�i'+
-- Right('0' + CONVERT(VARCHAR(100),datepart(dd, ORDDY2)),2)
-- +' '+
-- (CASE WHEN datepart(HH, ORDDY2) > 12 THEN 'P'+ CONVERT(VARCHAR(100),datepart(HH, ORDDY2)-12) ELSE 'A' + CONVERT(VARCHAR(100),datepart(HH, ORDDY2)) END)
-- +'�j' ELSE '' END ) --�[�J�������� 2020/06/12 Techup
-- ELSE
-- cast(PRDNAME AS NVARCHAR(30) )+ISNULL(�t���Ѽ�,'')
-- --+ (CASE WHEN PRDNAME LIKE '%��%' THEN '��' ELSE '' END) -- 2021/04/13
-- +'(' + CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
-- + '��'
-- FROM #TEMP3
-- WHERE #TEMP3.INPART = #�s�d����.INPART
-- AND PRDNAME NOT LIKE 'Z%' --AND PRDNAME NOT LIKE '%��%' -- 2021/04/13
-- AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE SOPKIND = '�]�p') AND ORDFO NOT LIKE '%��%'
-- AND PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND ORDSQ3 = 0
-- ORDER BY ROWID
-- FOR XML PATH('')

SELECT CASE
WHEN A.ORDDTP = 2 THEN cast(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) ) +
                     CASE WHEN A.INPART LIKE '%Z%' THEN '(' +
                                       CONVERT(VARCHAR(100),CONVERT(DECIMAL(18,0),CONVERT(DECIMAL(18,0),A.ORDUPR))) +
  ')'
  ELSE
  (CASE WHEN ISNULL(�~�]�w�p�Ѽ�,'') = '' THEN ''
  ELSE
  '(' +  
  �~�]�w�p�Ѽ�  
  +
  ')' END) END
WHEN A.ORDFO LIKE '%��%' AND A.ORDFCO = 'N' THEN REPLACE(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),'��',''),CHAR(10),'������')+'('+cast(CONVERT(bigint, A.ORDAMT) AS NVARCHAR(30) ) + ')'  
           WHEN A.ORDDTP = 1 AND A.ORDFO NOT LIKE '%��%' THEN cast(REPLACE(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) )
    --+ ISNULL(�t���Ѽ�,'')
    +
CASE WHEN A.ORDFO LIKE 'CQ%' THEN '(5D)'
WHEN A.ORDFO LIKE '280' THEN '(1D)'
WHEN ISNULL(A.�t���Ѽ�,'') = '' THEN '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),A.ORDFM1/60))) + ')'
ELSE '' END +
--�P�_�|�����u�B DLYTIME = 0 �N��ܪ� -- 2021/11/30 TECHUP
CASE WHEN A.DLYTIME = 0 AND ISNULL(A.�t���Ѽ�,'') = '' THEN '' ELSE

--2022/07/14 CLOSE
''
--'�i'+ CASE WHEN A.DLYTIME > 0 THEN
-- CASE WHEN CONVERT(DECIMAL(12,2),A.DLYTIME) - CONVERT(DECIMAL(12,2),A.ORDFM1/60) < 0 THEN '0' ELSE CONVERT(VARCHAR(100),CONVERT(FLOAT,CONVERT(DECIMAL(12,2),A.DLYTIME) - CONVERT(DECIMAL(12,2),A.ORDFM1/60))) END
--  ELSE '0' END +'�j'
END --�[�JDLYTIME



ELSE cast(REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),CHAR(10),'������') AS NVARCHAR(30) )
--+ ISNULL(�t���Ѽ�,'')
+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),A.ORDUPR))) + ')' END +
(CASE WHEN ISNULL(B.CARDNO,'') <> ''
AND ISNULL(C.REWORK,'') NOT IN ('QC�ߧY�B�z','�Ȩѫ~���}(�i��)','�i��','���o����')
THEN ('����i'+ CONVERT(VARCHAR(10),C.�}�橵��ɶ�) + '�j') ELSE '' END) +
'��'
FROM #TEMP3 A LEFT OUTER JOIN (SELECT * FROM #��X�e�����`�檺_�Ѿl�s�{���� WHERE ORDSQ3 = 1) B ON A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
 LEFT OUTER JOIN ORDDE4_�Ѿl�s�{����_���`�� C ON B.CARDNO = C.CARDNO
WHERE A.INPART = #�s�d����.INPART
AND A.PRDNAME NOT LIKE 'Z%'
AND REPLACE(REPLACE(REPLACE(A.PRDNAME,'��',''),'��',''),'�~','') NOT IN (SELECT PRDNAME FROM #SOPNAME WHERE SOPKIND = '�]�p')
--AND A.ORDFO NOT LIKE '%��%'
AND A.PRDNAME NOT LIKE 'Z%' AND A.PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND A.ORDSQ3 = 0
ORDER BY A.ROWID
FOR XML PATH('')

)
,�̫���[�� = cast(0 AS bigint ),
�Ѿl���[�s�{ =
(
SELECT
CASE WHEN ORDFO LIKE '%��%' AND ORDFCO = 'N' THEN PRDNAME+'('+cast(CONVERT(bigint, ORDAMT) AS NVARCHAR(30) )+')'  
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')' END
+ '��'
FROM #TEMP3
WHERE #TEMP3.INPART = #�s�d����.INPART AND PRDNAME NOT LIKE '%��%' AND PRDNAME NOT LIKE '%Z%' AND #TEMP3.SOPKIND = '���[' AND ORDSQ3 = 0
ORDER BY ROWID
FOR XML PATH('')

), �Ѿl�Ƶ{����=CAST('' AS VARCHAR(MAX))
----- 2019/02/27 ADD
,   �s�y��=(SELECT MAX(ORDQTY) FROM #TOT3 WHERE #TOT3.INPART = #�s�d����.INPART )
,   ���ƶO=(SELECT SUM(ORDAMT) FROM #TOT3 WHERE #TOT3.INPART = #�s�d����.INPART )
, QC���O=(SELECT CASE WHEN SUM(#TOT3.ORDAMT) >=10000 OR MAX(#TOT3.ORDQTY) >= 5 THEN '���' ELSE '' END FROM #TOT3 WHERE #TOT3.INPART = #�s�d����.INPART )
,�b���s�{�� = (
SELECT MIN(ORDSQ2) �s�{�� FROM #TEMP3 A WHERE PRDNAME NOT LIKE 'Z%' AND PRDNAME NOT IN ('lo','uld','LD','ULD','am') AND SOPKIND NOT IN ('�]�p') --AND ORDFO NOT IN ('27') -- ����as �v 2026/01/26
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE DESCR LIKE '%�O%' ) --�O�����N����
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('EPM','E3Q') ) --EPM �M E3Q �N����A�s 2024/10/17 ���ƪ��n�D�ҥ~�B�z
AND ORDFCO = 'N' AND A.INPART = #�s�d����.INPART AND ORDSQ3 = 0 GROUP BY INPART)
,�b���s�{�ǫe����DLYTIME = 0
,U_INPART=CAST('' AS VARCHAR(40)),AKT�j�����~����=CAST('' AS VARCHAR(40))
,�Q����u�u�� = (SELECT SUM(PRTIME)/60.0 PRTIME FROM #�n�B�̪�PRODTM WHERE CRDATE >=  convert(varchar, DATEADD(DD,-1,GETDATE()), 111) + ' 00:00' AND PRTFM <= convert(varchar, GETDATE(), 111) + ' 00:00'
                       AND PTPNO = #�s�d����.INPART)
,�}��s�d = (SELECT distinct TOP 1 OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = #�s�d����.INPART
)
,�}��e�s�d�s�{���� = (
SELECT
CASE WHEN ORDFO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(int, ORDAMT) AS NVARCHAR(30) )+')'  
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' AND ORDSQ3 = '0' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),ORDFM1/60))) + ')'+
                                                               CASE WHEN ����ɶ� > 0 THEN '/'+CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),����ɶ�))) ELSE '' END
WHEN ORDDTP = 1 AND ORDFO NOT LIKE '%��%' AND ORDSQ3 > '0' THEN PRDNAME+'/'+cast(CARDNO AS NVARCHAR(10) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2),����ɶ�))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,0),CONVERT(DECIMAL(12,0),ORDUPR))) + ')'
END
+ '��'
FROM #��X�e�����`�檺_�Ѿl�s�{���� A,(SELECT distinct INPART,OLDPART,ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE INPART = #�s�d����.INPART) B
WHERE A.INPART = B.OLDPART AND A.ORDSQ2 <= B.ORDSQ2
AND PRDNAME NOT LIKE '%Z%'
ORDER BY ROWID
FOR XML PATH('')
--),�u���m = ISNULL((SELECT AA FROM #SFC2468NET_��B��� WHERE �s�y�s�� = #�s�d����.INPART),(SELECT STATUS FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART))
--,��m�ɶ� = ISNULL((SELECT ���f�ɶ� FROM #SFC2468NET_��B��� WHERE �s�y�s�� = #�s�d����.INPART),(SELECT ISNULL(�ɶ�,'') FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART))
),�u���m = ISNULL((SELECT STATUS FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART),'')
,��m�ɶ� = ISNULL((SELECT ISNULL(�ɶ�,'') FROM #�u��B�e���� WHERE IDNO = #�s�d����.INPART),'')
,'',0,'',0,'','',0,0 ----�s�W��� �o��n�O�o�� 2025/11/13
----- 2019/02/27
FROM #�s�d����
ORDER BY INPART
END

----03:23
    --EXEC dbo.����ORDE3�Ѿl�s�{ ''


    UPDATE ORDDE4_�Ѿl�s�{����_D
SET NEW_ORDSNO = convert(varchar, DATEADD(DD,�Ѿl�u��/8.00*-1,ORDSNO), 111)

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�s�{���� = substring(�Ѿl�s�{����,0,LEN(�Ѿl�s�{����))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�s�{����3 = substring(�Ѿl�s�{����3,0,LEN(�Ѿl�s�{����3))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �����s�{���ӧtDLYTIME = substring(�����s�{���ӧtDLYTIME,0,LEN(�����s�{���ӧtDLYTIME))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �����s�{���ӧtDLYTIME_���t�]�p = substring(�����s�{���ӧtDLYTIME_���t�]�p,0,LEN(�����s�{���ӧtDLYTIME_���t�]�p))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �}��e�s�d�s�{���� = substring(�}��e�s�d�s�{����,0,LEN(�}��e�s�d�s�{����))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�s�{����4 = substring(�Ѿl�s�{����4,0,LEN(�Ѿl�s�{����4))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl���[�s�{ = substring(�Ѿl���[�s�{,0,LEN(�Ѿl���[�s�{))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET TOTAL = substring(TOTAL,0,LEN(TOTAL))

UPDATE ORDDE4_�Ѿl�s�{����_D
SET ���TOTAL = substring(���TOTAL,0,LEN(���TOTAL))

--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D

--SELECT distinct INPART FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDSQ2 = -500
--SELECT distinct INPART FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDSQ2 <> -500
--SELECT distinct INPART FROM ORDDE4_�Ѿl�s�{����_����_D --WHERE ORDSQ2 <> -500
--50893
--1323

----------------->>>>>>>>>>-------2024/04/24 Techup ���ƪ� �n�D ����CUS�u��-----------<<<<<<<<<--------------------------------------------------
----------------->>>>>>>>>>-------2023/04/09 Techup ����U���q���ӧ�����-----------<<<<<<<<<--------------------------------------------------
------�S���]�p �N6/4�� 2023/04/24 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET �e�m�]�p�u�� = 0,CUS�u�� = TOTAL�u��/10*4 --�t��������60% CUS������40%
--FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT distinct INPART FROM ORDDE4_�Ѿl�s�{����_����_D) B
--WHERE A.INPART = B.INPART  AND A.INPART LIKE @INPART

----�n���]�p���~�ݭn�W�[ �H�W��ӰϬq�u�� 2023/04/08 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET �e�m�]�p�u�� = TOTAL�u��/5*2,CUS�u�� = TOTAL�u��/5*3 --�]�p������20% CUS������30%
--FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT distinct INPART FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDSQ2 = -500) B
--WHERE A.INPART = B.INPART  AND A.INPART LIKE @INPART
----

----�J�줣����
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = (B.�Ѿl�u��*0.5) ---- 2023/04/28 Techup ���ƪ��n�D�J�줣�����h�ϥγѾl�u��+30HR
--FROM ORDDE4_�Ѿl�s�{����_D A,(
--SELECT A.INPART,ORDSNO,�Ѿl�u�� FROM ORDDE4_�Ѿl�s�{����_D A,(
--SELECT MAX(ORDDY5) ORDDY5,INPART FROM ORDDE4_�Ѿl�s�{����_����_D  WHERE CONVERT(DECIMAL(18,4), ORDFM1) > 0 AND ISNULL(ORDDY5,'') <> '' GROUP BY INPART
--) B WHERE A.INPART = B.INPART AND
--(
--(A.ORDSNO <= convert(varchar, DATEADD(DD,+0, B.ORDDY5), 111) AND A.ORDSNO > convert(varchar, DATEADD(DD,+0,  GETDATE()), 111))
--OR �i�Τu�� < 30 ) --�i��<30�]�ǤJ �˨R 2023/05/02 Techup
--)B
--WHERE A.INPART = B.INPART AND A.INPART LIKE @INPART


----------------------------------------------------------------------------------------------------------------------------------------------------------------------
---- �N���N�W��3��UPDATE  2023/06/29 �v

--SELECT B.DIFLVL,B.INDWG INTO #CUSTREQ3  FROM CUSTREQ2 A , CUSTREQ3 B ,
--(SELECT B.INDWG , CFMDATE = MAX(CFMDATE) FROM CUSTREQ2 A , CUSTREQ3 B WHERE A.PNSQ = B.PNSQ AND A.PNSQ1 = B.PNOSQ AND A.SCRL = 'N' GROUP BY B.INDWG) C
--WHERE A.PNSQ = B.PNSQ AND A.PNSQ1 = B.PNOSQ
--AND A.SCRL = 'N'
--AND B.INDWG = C.INDWG
--AND A.CFMDATE = C.CFMDATE
--AND B.DIFLVL = 'A'


------ �q�� �@��
--------2024/02/15 ���ƪ��n�D �Ѿl�u��/2 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = �Ѿl�u��/2 --�t��������33% CUS������66%
--FROM ORDDE4_�Ѿl�s�{����_D
--WHERE INPART LIKE @INPART
--AND �Ѿl�u�� < 200



------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
----2025/02/13 �v ����n�D������CUS------------------------------------------------------------------------------------------------------------------

------ ����
---------2025/05/07 ���ƪ��n�D�u�� ASMPT JENO 7�� CUS�u�� �v
---------2024/09/20 ���ƪ��n�D�u�� 1.����(�]�p150 �{��100) > 2.HMI 300 ASML CS JENO �Ѿl�u��*1.5 CUS�u�� �v
---------2024/09/10 ���ƪ��n�D�u�� ASML �~���_ ���q ������~�[100 CUS�u�� �v
---------2024/05/24 ���ƪ��n�D�u�� ASML �~���_ ���q ������~�[300 CUS�u�� Techup
---------2023/10/19 ���ƪ��n�D�u�n�~���_�M����~�[100 CUS�u�� Techup
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
--DATEDIFF(DD,B.CRDATE,A.ORDSNO)/7*5*10/2 --(����--> ���)*0.5 ��@CUS
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND D.ORDCU IN (SELECT CUSTNO FROM CUSTOME WHERE CUSTGP IN ('JENO','ASMPT') AND SCRL <> 'Y')
AND A.INPART LIKE @INPART

--2025/11/03 ���ƪ��n�DLLBM
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND A.�Ѿl�s�{���� LIKE '%LLBM%'
AND A.INPART LIKE @INPART


----2025/06/16 ���ƪ��n�D 4022.690.20361 V���� �[�J300CUS
----2025/06/03 ���ƪ��n�D 4022.690.20361 V���� �[�J140CUS
----2025/10/09 �ư�
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = 300
--FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
--WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
--AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
--AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
--AND C.INDWG = '4022.690.20371'
--AND C.INPART LIKE @INPART

----4022.690.20361  V���� �D�� 2025/11/21
----4022.690.20371  V���� ���� 2025/11/21
----2025/11/07
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND C.INDWG IN ('4022.690.20371','4022.690.20361','4022.690.20361-SERV','4022.481.19421')
AND C.INPART LIKE @INPART

---- 2025/11/11 ����n�D�[�W����| �v
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
--DATEDIFF(DD,B.CRDATE,A.ORDSNO)/7*5*10/2 --(����--> ���)*0.5 ��@CUS
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND D.ORDCU IN (SELECT CUSTNO FROM CUSTOME WHERE CUSTGP IN ('CS-0','CISTL','CISTL-CH','CISTL-MC','CISTL-ME','CISTL-MR','CISTL-MS') AND SCRL <> 'Y')
AND A.INPART LIKE @INPART

---- 2025/11/11 ����n�D�[�WFAI �v
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND (A.INPART LIKE '%-0-%' OR ISNULL(C.�Ƶ�_PC�s�d���A,'') LIKE '%FAI%')
AND A.INPART LIKE @INPART


----2025/09/26 ���ƪ��n�D 4022.680.04635  �u�l +200
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND C.INDWG IN ('4022.680.04635','4022.680.39075','4022.680.39082','4022.680.39172','4022.629.00969','4022.439.97644',
'4022.489.21953','4022.489.21954','4022.489.21914','4022.489.72753','4022.489.72902')
AND C.INPART LIKE @INPART
----2025/09/26 ���ƪ��n�D 4022.680.04854  �J�| +200
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND C.INDWG IN ('4022.680.04854','4022.680.38893','4022.680.38862','4022.680.38872','4022.680.38882','4022.489.72753',
'4022.489.72902')
AND C.INPART LIKE @INPART
----2025/09/26 ���ƪ��n�D 4022.683.72985  �o�g�� +200
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND C.INDWG IN('4022.683.72985','4022.683.99614','4022.683.99621','4022.489.21486','4022.489.72753')
AND C.INPART LIKE @INPART
----2025/09/26 ���ƪ��n�D 4022.683.73033  �Z�J���y +200
UPDATE ORDDE4_�Ѿl�s�{����_D
SET CUS�u�� = 100
FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
AND C.INDWG IN ('4022.683.73033')
AND C.INPART LIKE @INPART



---------2025/02/13 ���ƪ��n�D ����|�[150 CUS�u�� �v
---------2024/05/24 ���ƪ��n�D�u�� ����|�[300 CUS�u�� Techup
----------- 2023/10/20 �v ����M����|��+300
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = �Ѿl�u�� * 1.5 --�Ѿl�u��*3+100 ---���� �Ѿl�u��*3+100hr
----DATEDIFF(DD,B.CRDATE,A.ORDSNO)/7*5*10/2 --(����--> ���)*0.5 ��@CUS
--FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
--WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
--AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
--AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
--AND D.ORDCU IN ('HMI','HMI-US','HMIBV')
----AND (A.INPART LIKE '%-0-%' OR ISNULL(C.�Ƶ�_PC�s�d���A,'') LIKE '%FAI%')
--AND A.INPART LIKE @INPART


---------2024/09/20 ���ƪ��n�D�u�� 1.����(�]�p150 �{��100) 2.HMI 300 ASML CS JENO �Ѿl�u��*1.5 CUS�u�� �v
------ ���]�p
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = 0 --250
--FROM ORDDE4_�Ѿl�s�{����_D A, ORDE3 C
--WHERE A.INPART IN (SELECT DISTINCT INPART FROM ORDDE4_�Ѿl�s�{����_����_D WHERE SOPKIND = '�]�p' AND ORDFCO = 'N')
--AND A.INPART LIKE @INPART
--AND (A.INPART LIKE '%-0-%' OR ISNULL(C.�Ƶ�_PC�s�d���A,'') LIKE '%FAI%')
--AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1

------ �L�]�p
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = 0 --100
--FROM ORDDE4_�Ѿl�s�{����_D A, ORDE3 C
--WHERE A.INPART NOT IN (SELECT DISTINCT INPART FROM ORDDE4_�Ѿl�s�{����_����_D WHERE SOPKIND = '�]�p' AND ORDFCO = 'N')
--AND A.INPART LIKE @INPART
--AND (A.INPART LIKE '%-0-%' OR ISNULL(C.�Ƶ�_PC�s�d���A,'') LIKE '%FAI%')
--AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
------

----20225/02/13 �v ����n�D������CUS------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


--------- 2023/11/21 �v �q������CUS
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = 0 WHERE INPART LIKE '%-E%'


--------- 2023/11/27 ���g�z�n�D�@�U�s�dcus�A�[100 �v -- 2024/03/11 -----------------------------------------------------------------------------
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = CUS�u��+100
--WHERE INPART IN ('23C03150-000#1','23C03106-000#1','23C03106-000#2','23C03106-000#38R1','23C03106-000#41',
--'23C03106-000#44R1','23C03106-000#44R1','23C03106-000#45R1','23C03106-000#47','23C03106-000#5','22C03018-000#21R1',
--'22C03019-000#17R1#1R1','23C03106-000','23C03106-000#1','23C03106-000#10')

--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = CUS�u��+440
--WHERE INPART IN ('23F01154-1-000#1','23F01150-0-000','23F01171-0-000','23F01187-0-000','23F01161-0-000','23F01157-0-000')


--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = CUS�u��+300
--WHERE INPART IN ('24G01010-0-000','24G01010-0-000#1','24G01010-0-001','24G01010-0-001-001','24G01010-0-001-002',
--'24G01010-0-001-002#1','24G01010-0-001-003','24G01010-0-001-003#1','24G01010-0-001-004','24G01010-0-001-004#1',
--'24G01010-0-001-005','24G01010-0-001-005#1','24G01010-0-001-006','24G01010-0-001-006#1','24G01010-0-001-007',
--'24G01010-0-001-007#1','24G01010-0-001-008','24G01010-0-001-008#1','24G01010-0-001-009','24G01010-0-001-009#1R1',
--'24G01010-0-001-010','24G01010-0-001-010#1','24G01010-0-001-011','24G01010-0-001-011#1R1','24G01010-0-002',
--'24G01010-0-002-001','24G01010-0-002-001#1','24G01010-0-002-002','24G01010-1-000','24G01010-1-001-001',
--'24G01010-1-001-002','24G01010-1-001-003','24G01010-1-001-004','24G01010-1-001-005','24G01010-1-001-006',
--'24G01010-1-001-007','24G01010-1-001-008','24G01010-1-001-009','24G01010-1-001-010','24G01010-1-001-011',
--'24G01010-1-002','24G01010-1-002-001','24G01010-1-002-002','24G01022ML-0-002','24G01022ML-0-004','24G01022ML-0-009',
--'24G01022ML-0-010','24G01023ML-0-007-001-003-001','24G01023ML-0-007-001-005','24G01023ML-0-007-001-005',
--'24G01023ML-0-007-001-008','24G01023ML-0-007-001-009','24G01023ML-0-007-001-010','24G01023ML-0-007-001-002',
--'24G03232ML-002','24G03232ML-004','24G03232ML-009','24G03232ML-010','24G03233ML-002','24G03233ML-004',
--'24G03233ML-009','24G03233ML-010','24Q01004-0-000','24Q01004-0-001','24Q03222-000','24Q03279-000','23Q01013-1-000',
--'23Q03241-000','23Q03117-000','23Q03118-000','23Q03241-000#1','23Q03241-000#2','23Q01013-1-001R1','23Q03241-001',
--'23Q03117-001','23Q03118-001','23Q03241-001','23Q03241-001#1','24Q03268-000','23Q03297-000','23Q01024-0-000',
--'23Q03918-000','24Q03013-000','23Q03177-000','23Q03177-000#1','23Q03297-001R1','24Q03268-001','23Q03918-001',
--'23Q01024-0-001R2','24Q03013-001','23Q03177-001#1R2','23Q03177-001R2','24Q03277-000','24Q03280-000','24Q03281-000',
--'24Q01005-0-000','23Q03945-000','23F03191-000','23F01152-0-000','23Q01054-0-000','23Q03712-000#9','23Q03712-000#23',
--'23Y03135-000#5','23L09220-000','23Q03956-000#9','23F03190-000','23F03184-000','23F01184-0-000',
--'23Y03137-000#4','23Y03122-000#9','23Y03113-000#9','23F03205-000#4','24Y03002-000','23Y03114-000#3',
--'23Q03957-000#1','23Y03163-000#3','23F03195-000#4','23F01156-0-000','23F03217-000')

--UPDATE ORDDE4_�Ѿl�s�{����_D -- ���w ���g�z
--SET CUS�u�� = 150
--WHERE INPART IN ('24Q03036-000','24Q03037-000','24Q03047-000','24Q03048-000','24Q03049-000','24Q03050-000','24Q03051-000',
--'24Q03052-000','24Q03053-000','24Q03653-000','24Q03654-000','24Q03655-000','24Q03656-000','24Q03657-000','24Q03658-000','24Q03659-000',
--'24Q03660-000','24Q03661-000')

--UPDATE ORDDE4_�Ѿl�s�{����_D -- ���w ���g�z
--SET CUS�u�� = 200
--WHERE INPART IN ('24Q03418-000','24Q03419-000','24Q03420-000','24Q03421-000','24Q03422-000','24Q03111-000','24Q03112-000','24Q03113-000',
--'24Q03114-000','24Q03115-000','24Q03116-000','24Q03117-000','24Q03118-000','24Q03119-000','24Q03120-000','24Q03121-000','24Q03122-000',
--'24Q03123-000','24Q03124-000','24Q03125-000','24Q03423-000','24Q03424-000','24Q03425-000','24Q03426-000','24Q03427-000','24Q03428-000',
--'24Q03429-000','24Q03430-000','24Q03431-000','24Q03432-000','24Q03433-000','24Q03434-000','24Q03435-000','24Q03436-000','24Q03437-000',
--'24Q03438-000','24Q03439-000','24Q03440-000','24Q03441-000','24Q03442-000','24Q03521-000','24Q03522-000','24Q03523-000','24Q03524-000',
--'24Q03525-000','24Q03526-000','24Q03527-000','24Q03528-000','24Q03529-000','24Q03530-000','24Q03531-000','24Q03532-000','24Q03533-000',
--'24Q03534-000','24Q03535-000','24Q03536-000','24Q03537-000','24Q03538-000','24Q03539-000','24Q03540-000','24Q03541-000','24Q03542-000',
--'24Q03543-000','24Q03544-000','24Q03545-000','24Q03561-000','24Q03562-000','24Q03563-000','24Q03564-000','24Q03565-000','24Q03566-000',
--'24Q03567-000','24Q03568-000','24Q03569-000','24Q03570-000','24Q03571-000','24Q03572-000','24Q03573-000','24Q03574-000','24Q03575-000',
--'24Q03576-000','24Q03577-000','24Q03578-000','24Q03579-000','24Q03580-000','24Q03139-000#1','24Q03139-000#2','24Q03139-001#2','24Q03139-000#3',
--'24Q03139-000#4','24Q03139-000#5','24Q03139-000#6','24Q03140-000','24Q03140-003','24Q03141-000','24Q03141-002','24Q03141-003','24Q03142-000',
--'24Q03142-002','24Q03142-003','24Q03143-000','24Q03143-002','24Q03143-003','24Q03205-000','24Q03205-002','24Q03205-003','24Q03283-000',
--'24Q03283-002','24Q03283-003','24Q03036-001','24Q03037-001','24Q03047-001','24Q03048-001','24Q03049-001','24Q03050-001','24Q03051-001',
--'24Q03052-001','24Q03053-001','24Q03653-001','24Q03654-001','24Q03655-001','24Q03656-001','24Q03657-001','24Q03658-001','24Q03659-001',
--'24Q03660-001','24Q03661-001','24Q03058-000','24Q03059-000','24Q03060-000','24Q03061-000','24Q03337-000','24Q03338-000','24Q03339-000',
--'24Q03480-000','24Q03481-000','24Q03482-000','24Q03483-000','24Q03484-000','24Q03485-000','24Q03486-000','24Q03487-000','24Q03488-000',
--'24Q03489-000','24Q03490-000','24Q03491-000','24Q03492-000')

--UPDATE ORDDE4_�Ѿl�s�{����_D -- ���w ���g�z
--SET CUS�u�� = 250
--WHERE INPART IN ('24Q03036-002','24Q03036-003','24Q03037-002','24Q03037-003','24Q03047-002','24Q03047-003','24Q03048-002','24Q03048-003',
--'24Q03049-002','24Q03049-003','24Q03050-002','24Q03050-003','24Q03051-002','24Q03051-003','24Q03052-002','24Q03052-003','24Q03053-002',
--'24Q03053-003','24Q03053-003#1','24Q03653-002','24Q03653-003','24Q03654-002','24Q03654-003','24Q03655-002','24Q03655-003','24Q03656-002',
--'24Q03656-003','24Q03657-002','24Q03657-003','24Q03658-002','24Q03658-003','24Q03659-002','24Q03659-003','24Q03660-002','24Q03660-003',
--'24Q03661-002','24Q03661-003')



--UPDATE ORDDE4_�Ѿl�s�{����_D -- ���w ���g�z
--SET CUS�u�� = 300
--WHERE INPART IN ('24L09153-000#1R1','24Q03072-000','24Q03073-000','24Q03074-000','24Q03075-000','24Q03083-000','24Q03084-000',
--'24Q03085-000','24Q03086-000','24Q03087-000','24Q03088-000','24Q03089-000','24Q03090-000','24Q03167-000','24Q03168-000','24Q03169-000',
--'24Q03170-000','24Q03318-000','24Q03319-000','24Q03320-000','24Q03321-000','24Q03418-001','24Q03418-002','24Q03418-002-002',
--'24Q03419-001','24Q03419-002','24Q03419-002-002','24Q03420-001','24Q03420-002','24Q03420-002-002','24Q03421-001','24Q03421-002',
--'24Q03421-002-002','24Q03422-001','24Q03422-002','24Q03422-002-002','24Q03111-001','24Q03111-002','24Q03111-002-002','24Q03112-001',
--'24Q03112-002','24Q03112-002-002','24Q03113-001','24Q03113-002','24Q03113-002-002','24Q03114-001','24Q03114-002','24Q03114-002-002',
--'24Q03115-001','24Q03115-002','24Q03115-002-002','24Q03116-001','24Q03116-002','24Q03116-002-002','24Q03117-001','24Q03117-002',
--'24Q03117-002-002','24Q03118-001','24Q03118-002','24Q03118-002-002','24Q03119-001','24Q03119-002','24Q03119-002-002','24Q03120-001',
--'24Q03120-002','24Q03120-002-002','24Q03121-001','24Q03121-002','24Q03121-002-002','24Q03122-001','24Q03122-002','24Q03122-002-002',
--'24Q03123-001','24Q03123-002','24Q03123-002-002','24Q03124-001','24Q03124-002','24Q03124-002-002','24Q03125-001','24Q03125-002',
--'24Q03125-002-002','24Q03423-001','24Q03423-002-002','24Q03424-001','24Q03424-002-002','24Q03425-001','24Q03425-002-002','24Q03426-001',
--'24Q03426-002-002','24Q03427-001','24Q03427-002-002','24Q03428-001','24Q03428-002-002','24Q03429-001','24Q03429-002-002','24Q03430-001',
--'24Q03430-002-002','24Q03431-001','24Q03431-002','24Q03431-002-002','24Q03432-001','24Q03432-002-002','24Q03423-002','24Q03424-002',
--'24Q03425-002','24Q03426-002','24Q03427-002','24Q03428-002','24Q03429-002','24Q03430-002','24Q03432-002','24Q03433-001','24Q03433-002-002',
--'24Q03434-001','24Q03434-002','24Q03434-002-002','24Q03435-001','24Q03435-002-002','24Q03436-001','24Q03436-002','24Q03436-002-002',
--'24Q03437-001','24Q03437-002-002','24Q03433-002','24Q03435-002','24Q03437-002','24Q03438-001','24Q03438-002-002','24Q03439-001',
--'24Q03439-002-002','24Q03440-001','24Q03440-002-002','24Q03441-001','24Q03441-002-002','24Q03442-001','24Q03442-002-002','24Q03438-002',
--'24Q03439-002','24Q03440-002','24Q03441-002','24Q03442-002','24Q03521-001','24Q03521-002','24Q03521-002-002','24Q03522-001','24Q03522-002',
--'24Q03522-002-002','24Q03523-001','24Q03523-002','24Q03523-002-002','24Q03524-001','24Q03524-002','24Q03524-002-002','24Q03525-001','24Q03525-002',
--'24Q03525-002-002','24Q03526-001','24Q03526-002','24Q03526-002-002','24Q03527-001','24Q03527-002','24Q03527-002-002','24Q03528-001','24Q03528-002',
--'24Q03528-002-002','24Q03529-001','24Q03529-002','24Q03529-002-002','24Q03530-001','24Q03530-002','24Q03530-002-002','24Q03531-001','24Q03531-002',
--'24Q03531-002-002','24Q03532-001','24Q03532-002','24Q03532-002-002','24Q03533-001','24Q03533-002','24Q03533-002-002','24Q03534-001','24Q03534-002',
--'24Q03534-002-002','24Q03535-001','24Q03535-002','24Q03535-002-002','24Q03536-001','24Q03536-002','24Q03536-002-002','24Q03537-001','24Q03537-002',
--'24Q03537-002-002','24Q03538-001','24Q03538-002','24Q03538-002-002','24Q03539-001','24Q03539-002','24Q03539-002-002','24Q03540-001','24Q03540-002',
--'24Q03540-002-002','24Q03541-001','24Q03541-002','24Q03541-002-002','24Q03542-001','24Q03542-002','24Q03542-002-002','24Q03543-001','24Q03543-002',
--'24Q03543-002-002','24Q03544-001','24Q03544-002','24Q03544-002-002','24Q03545-001','24Q03545-002','24Q03545-002-002','24Q03561-001','24Q03561-002',
--'24Q03561-002-002','24Q03562-001','24Q03562-002','24Q03562-002-002','24Q03563-001','24Q03563-002','24Q03563-002-002','24Q03564-001','24Q03564-002',
--'24Q03564-002-002','24Q03565-001','24Q03565-002','24Q03565-002-002','24Q03566-001','24Q03566-002','24Q03566-002-002','24Q03567-001','24Q03567-002',
--'24Q03567-002-002','24Q03568-001','24Q03568-002','24Q03568-002-002','24Q03569-001','24Q03569-002','24Q03569-002-002','24Q03570-001','24Q03570-002',
--'24Q03570-002-002','24Q03571-001','24Q03571-002','24Q03571-002-002','24Q03572-001','24Q03572-002','24Q03572-002-002','24Q03573-001','24Q03573-002',
--'24Q03573-002-002','24Q03574-001','24Q03574-002','24Q03574-002-002','24Q03575-001','24Q03575-002','24Q03575-002-002','24Q03576-001','24Q03576-002',
--'24Q03576-002-002','24Q03577-001','24Q03577-002','24Q03577-002-002','24Q03578-001','24Q03578-002','24Q03578-002-002','24Q03579-001','24Q03579-002',
--'24Q03579-002-002','24Q03580-001','24Q03580-002','24Q03580-002-002','24Q03139-001#1','24Q03139-001#3','24Q03139-001#4','24Q03139-001#5','24Q03139-001#6',
--'24Q03140-002','24Q03140-001','24Q03141-001','24Q03142-001','24Q03143-001','24Q03205-001','24Q03283-001')


----------------------------------------------------------------------------------------------------------------------------------------------------


--------- 2024/02/21 ���ƪ��n�D4022.690.20371�o�ϸ�CUS�u�ɳ��n�a300
--UPDATE ORDDE4_�Ѿl�s�{����_D SET CUS�u�� = �Ѿl�u��+300
--FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
--WHERE A.INPART = B.INPART AND B.INDWG = '4022.690.20371'
--AND B.INFIN = 'N'

--------- 2024/03/06 �����p�����`�g�z�n�D�B�~�W�[�H�U�ϸ���CUS(�W���W�h�H��A�[300) �v
--UPDATE ORDDE4_�Ѿl�s�{����_D SET CUS�u�� = CUS�u�� + 300
--FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
--WHERE A.INPART = B.INPART AND B.INDWG IN ('CH1917-0036','CH3126-0227')
--AND B.INFIN = 'N'


---- ---- ����B�x����
----UPDATE ORDDE4_�Ѿl�s�{����_D
----SET CUS�u�� = �Ѿl�u��*3+100 ---�x�� �Ѿl�u��*3+100hr
------DATEDIFF(DD,B.CRDATE,A.ORDSNO)/7*5*10/2 --(����--> ���)*0.5 ��@CUS
----FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B ,ORDE3 C ,STANDOPH D
----WHERE A.ORDTP = B.ORDTP
----AND A.ORDNO = B.ORDNO
----AND A.ORDSQ = B.ORDSQ
----AND A.INPART = C.INPART
----AND C.INDWG = D.PRDDWNO
----AND D.DIFLVL = 'A'
----AND A.INPART LIKE @INPART

-------- ����B�x����
----UPDATE ORDDE4_�Ѿl�s�{����_D
----SET CUS�u�� = �Ѿl�u��*4+200 ---����B�x�� �Ѿl�u��*4+200hr
------DATEDIFF(DD,B.CRDATE,A.ORDSNO)/7*5*10/2 --(����--> ���)*0.5 ��@CUS
----FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B ,ORDE3 C ,STANDOPH D
----WHERE A.ORDTP = B.ORDTP
----AND A.ORDNO = B.ORDNO
----AND A.ORDSQ = B.ORDSQ
----AND A.INPART = C.INPART
----AND C.INDWG = D.PRDDWNO
----AND D.DIFLVL = 'A' AND A.INPART LIKE '%-0-%'
----AND A.INPART LIKE @INPART


------------------------------------------------------------------------------------------------------------------------------------------------------------------------
----------------->>>>>>>>>>-------2024/04/24 Techup ���ƪ� �n�D ����CUS�u��-----------<<<<<<<<<--------------------------------------------------

--�]�p�̫����ӧ�����
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �e�m�]�p�w�p������ = [dbo].[�ư����骺��ڤu�@��_�f��](B.ORDDY1,10,�e�m�]�p�u��*60)
FROM ORDDE4_�Ѿl�s�{����_D A,(
SELECT INPART,MIN(ORDDY1) ORDDY1 FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE ORDSQ2 > 0 AND ORDFO NOT LIKE '%CUS%'
AND ORDFO NOT IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP IN ('24','118')) --�]�p������
GROUP BY INPART) B
WHERE A.INPART = B.INPART AND �e�m�]�p�u�� <> 0
AND A.INPART LIKE @INPART
--AND A.INPART = '23K01032AF-0-015-002'

--�̫���
UPDATE ORDDE4_�Ѿl�s�{����_D
SET TOTAL�u�ɹw�p������ = [dbo].[�ư����骺��ڤu�@��_�f��](B.ORDDY1,10,CUS�u��*60)
--convert(varchar, B.ORDDY1, 111)
FROM ORDDE4_�Ѿl�s�{����_D A,(
SELECT INPART,MAX(ORDDY1) ORDDY1 FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE ORDSQ2 > 0 AND ORDFO NOT LIKE '%CUS%' GROUP BY INPART) B
WHERE A.INPART = B.INPART AND CUS�u�� <> 0
AND A.INPART LIKE @INPART
--------------->>>>>>>>>>-------2023/04/09 Techup ����U���q���ӧ�����-----------<<<<<<<<<--------------------------------------------------

----03:38
    --EXEC dbo.����ORDE3�Ѿl�s�{ ''

--�w��Dautopc���[�����A���s�{ �N�����k��A1 2020/06/23 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_����_D
--SET �ثe�Ƶ{���� = 'A1'
--FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B
 --   WHERE Applier IN ('���[','�X�u') AND A.INPART = B.INPART AND B.�b���s�{�� = A.ORDSQ2

    -----SELECT INPART,ORDSQ2,ORDFO,PRDNAME,SetUpTime FROM �����ɶ� WHERE Remark <> '16' GROUP BY INPART,ORDSQ2,ORDFO,PRDNAME,SetUpTime

SELECT C.SetUpTime,GETDATE() DATE,A1DLYTIME
INTO #A1DLYTIME_�X��
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,
(SELECT INPART,ORDSQ2,ORDFO,PRDNAME,SetUpTime FROM #�����ɶ� WHERE Remark <> '16' GROUP BY INPART,ORDSQ2,ORDFO,PRDNAME,SetUpTime) C
    WHERE A.Applier IN ('���[','�X�u') AND A.INPART = B.INPART AND B.�b���s�{�� = A.ORDSQ2
AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND A.ORDFO = C.ORDFO
GROUP BY C.SetUpTime,A1DLYTIME

UPDATE #A1DLYTIME_�X��
SET A1DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(SetUpTime,DATE,@DLYTIME�C��u�@�p��)/60.00

    --�w��Dautopc���[�����A���s�{ �N�����k��A1 2020/06/23 Techup 2021/02/05 Techup �ק�
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثe�Ƶ{���� = 'A1',�ثeA1�Ƶ{���ǫإߤ�=D.SetUpTime,A1DLYTIME = D.A1DLYTIME
--A1DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(C.SetUpTime,GETDATE(),@DLYTIME�C��u�@�p��)/60.00
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,
(SELECT INPART,ORDSQ2,ORDFO,PRDNAME,SetUpTime FROM #�����ɶ� WHERE Remark <> '16' GROUP BY INPART,ORDSQ2,ORDFO,PRDNAME,SetUpTime) C,
#A1DLYTIME_�X�� D
    WHERE A.Applier IN ('���[','�X�u') AND A.INPART = B.INPART AND B.�b���s�{�� = A.ORDSQ2
AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND A.ORDFO = C.ORDFO AND C.SetUpTime = D.SetUpTime
AND A.INPART LIKE @INPART

-- ----EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE INPART = '20C03105-000#161'
----ORDER BY ROWID


--�ȨѮƯʮ� �BINFIP = 0 �b���s�{�ǴN��0
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET �b���s�{�� = 0
--FROM ORDDE4_�Ѿl�s�{����_D A , ORDE3 B
--WHERE B.��ڮƪp LIKE '%�ȨѮ�%' AND A.INPART = B.INPART
--AND INFIP = '0' AND �b���s�{�� <> 0


----2024/05/30 �v �ק�P�_
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET �b���s�{�� = 0
--FROM ORDDE4_�Ѿl�s�{����_D A , ORDE3 B
--WHERE (B.��ڮƪp like '%�ʮ�%' OR B.��ڮƪp LIKE 'QA�|���T�{�w�s�D��%'
--OR B.��ڮƪp LIKE '%���w�s%' OR B.��ڮƪp LIKE '�w�s����%' --OR B.��ڮƪp LIKE '%�ȨѮ�%'  2024/05/23 �v �ư��ȨѮ� �קK�������m���������a���X����
--OR B.��ڮƪp LIKE '%�w�禬���T�{%' --2023/09/13 �G�v�W�[
--OR B.��ڮƪp LIKE '%�����e%' --2024/05/30 Techup
--)
--AND B.��ڮƪp NOT LIKE '%���ʳ�%' AND B.��ڮƪp NOT LIKE '%�o�]��%' AND B.��ڮƪp NOT LIKE '%��]��%'
--AND A.INPART = B.INPART  
--AND �b���s�{�� <> 0
--AND A.INPART LIKE @INPART

--SELECT 'EEEEEEEE',* FROM #PRODTM_S
--WHERE �s�d IN ('24G04011SL-1-001','24G04011SL-1-003')

--SELECT 'DDDDDDDDD',�b���s�{��,* FROM ORDDE4_�Ѿl�s�{����
 --   WHERE INPART IN ('24G04011SL-1-001','24G04011SL-1-003')

----2024/05/30 �v �ק�P�_
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�� = 0
FROM ORDDE4_�Ѿl�s�{����_D A , ORDE3 B , ORDDE4_�Ѿl�s�{����_����_D C
WHERE (B.��ڮƪp like '%�ʮ�%' OR B.��ڮƪp LIKE 'QA�|���T�{�w�s�D��%'
OR B.��ڮƪp LIKE '%���w�s%' OR B.��ڮƪp LIKE '�w�s����%' --OR B.��ڮƪp LIKE '%�ȨѮ�%'  2024/05/23 �v �ư��ȨѮ� �קK�������m���������a���X����
OR B.��ڮƪp LIKE '%�w�禬���T�{%' --2023/09/13 �G�v�W�[
OR B.��ڮƪp LIKE '%�w�o�]%' --2024/08/19 Techup
OR B.��ڮƪp LIKE '%�����e%' --2024/05/30 Techup
)
AND B.��ڮƪp NOT LIKE '%���ʳ�%' AND B.��ڮƪp NOT LIKE '%�o�]��%' AND B.��ڮƪp NOT LIKE '%�禬��%'
AND A.INPART = B.INPART  
AND �b���s�{�� <> 0
AND A.INPART LIKE @INPART
AND A.INPART = C.INPART
AND A.�b���s�{�� = C.ORDSQ2
AND C.ORDFO <> '27'--- AT���n�ư�
---AND A.INPART NOT IN (SELECT �s�d FROM #PRODTM_S )  -----�u�n�|���o�� �N������{���h 2024/08/19 Techup

--SELECT 'CCCCCCCCC',�b���s�{��,* FROM ORDDE4_�Ѿl�s�{����
 --   WHERE INPART IN ('24G04011SL-1-001','24G04011SL-1-003')

--2022/11/10 �w�o�ƥ����e �h���ӱ��b�U�@�� �n��e�@�� Techup
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�� = 0
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
WHERE A.INPART = B.INPART
AND B.��ڮƪp like '%�����e%'
AND �b���s�{�� > 0
AND A.INPART LIKE @INPART
AND A.INPART NOT IN (SELECT �s�d FROM #PRODTM_S WHERE �e���s�{ <> '��' ) ----�{���p�����u �~���^�� 2024/04/12 Techup

-----�p�G�w�g�o�ƴN��b���s�{�ǩ�m��̦����s�{�ǤW 2024/05/03 Techup
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�� = C.ORDSQ2
FROM ORDDE4_�Ѿl�s�{����_D A, #�w�o�ƻs�d B,
(SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE �ήɶ���ORDSQ2 > 0 AND ORDSQ3 = 0 AND ORDFCO = 'N' GROUP BY INPART) C
WHERE A.INPART = B.ORDPN AND �b���s�{�� = 0
AND A.INPART = C.INPART
AND A.INPART NOT IN (SELECT INPART FROM ORDE3 WHERE ��ڮƪp LIKE '%�����e%' AND INFIN = 'N')
AND A.INPART LIKE @INPART





--DROP TABLE �n�禬���s�d

-----�p�G���禬���T�{�B�{�b���b���p���禬�����N�w�禬�������D 2024/08/01 �v
SELECT C.PUPA2 ,A.INPART
INTO #�n�禬���s�d
FROM PURIND A ,PURDEL B ,PURTD C,PURINM E
WHERE A.PURNO = B.PURNO AND A.PUISQ = B.PURSQ
AND B.PURNO = C.PA1NO AND B.PURSQ = C.PURSQ
AND A.PUINO = E.PUINO
AND E.SCTRL = 'N'
AND C.PUPA2 > 0

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�� = B.PUPA2 + 1
FROM ORDDE4_�Ѿl�s�{����_D A, #�n�禬���s�d B
WHERE A.INPART = B.INPART
AND A.�b���s�{�� = 0

---------------------------------------------------------------------------



--�~�]���� �^�g����x���
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET Applier = '�~�s'
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4 B
WHERE --A.INPART = '20K01122AF-0-000'
A.INPART = B.ORDFNO AND A.ORDSQ2 = B.ORDSQ2
AND ISNULL(A.Applier,'') = '' AND ISNULL(B.ORDDP,'') <> ''
AND A.INPART LIKE @INPART




-- �ɳ]�p������ --2022/03/17
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET Applier = '�~�s'
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4 B
WHERE --A.INPART = '20K01122AF-0-000'
A.INPART = B.ORDFNO AND A.ORDFO = B.ORDFO
AND A.ORDFO IN(SELECT PRDOPNO FROM #SOPNAME WHERE SOPKIND = '�]�p')
AND ISNULL(A.Applier,'') = '' AND ISNULL(B.ORDDP,'') <> ''
AND A.INPART LIKE @INPART
-----------�p��ثe����--------------------------------------------------------------------------------------------------------------------------------------

--DROP TABLE #���W�����~
--DROP TABLE #���W�����~
--DROP TABLE #���X�f�q��
--DROP TABLE #�έp���
--DROP TABLE #�Ȥ�M�׽s���`�q���
--DROP TABLE #�U�s�d�Ѿl���[��
--DROP TABLE #�έp���_NEW
--DROP TABLE #�έp���_NEW_01



    SELECT distinct E.ORDCU �Ȥ�s��,ISNULL(A.O2INPART,D.INPART) �X�f�q��,A.INPART,A.INDWG �ϸ�
INTO #���X�f�q��
FROM ORDE3 A,ORDDE4  B,#SOPNAME C,ORDE2 D,ORDE1 E
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO AND A.ORDSQ = D.ORDSQ
AND A.ORDTP = E.ORDTP AND A.ORDNO = E.ORDNO
AND B.ORDFO = C.PRDOPNO --AND C.PRDOPGP IN ('476','28','F00')
AND A.INFIN IN ('N','P')
AND PRDNAME NOT LIKE 'Z%'
AND B.ORDQY2-B.ORDQY5 > 0
AND E.ORDCU IN (SELECT CUSTNO FROM CUSTOME WHERE CUSTGP LIKE 'AF')
AND C.PRDNAME NOT IN ('lo','uld','LD','ULD','am','PK','QF','SC','CP','OS','�O�I�O') AND C.PRDNAME NOT LIKE 'Z%'
AND C.SOPKIND = '���['
AND (C.PRDNAME LIKE 'LL%' OR C.PRDNAME LIKE 'LBM%')

--SELECT * FROM #���X�f�q�� A,ORDE3 B,ORDE2 C
--WHERE �ϸ� = '0042-44600' AND A.INPART = B.INPART AND A.�ϸ� = B.INDWG
--AND B.ORDTP = C.ORDTP AND B.ORDNO = C.ORDNO AND A.�X�f�q�� = '�L�����q��'

--SELECT * FROM #���X�f�q��
--WHERE INPART = '22K03067AF-000'

UPDATE #���X�f�q�� SET �X�f�q�� = C.INPART
--SELECT A.*,B.INPART,C.INPART
FROM #���X�f�q�� A,ORDE3 B,ORDE2 C
WHERE A.INPART = B.INPART AND A.�ϸ� = B.INDWG
AND B.ORDTP = C.ORDTP AND B.ORDNO = C.ORDNO AND B.ORDSQ = C.ORDSQ AND A.�X�f�q�� = '�L�����q��'
--AND A.INPART = '22K03067AF-000'

    --SELECT * FROM #���X�f�q��
--WHERE INPART = '22K03067AF-000'

--1.����X�����ǭn�X�f�q��
--2.�A��X�n�X�f�s�d�����ǭq��X�f
--3.

--SELECT distinct �X�f�q��,INPART,�ϸ� FROM #���X�f�q��
--WHERE  �ϸ� = '0042-44600'
-- ������-TMC
SELECT distinct REPLACE(B.PROJECTNO,'-TMC','') �M�׽s��,SUBSTRING(B.ORDCPN,0,CHARINDEX('-',B.ORDCPN)) �Ȥ�PO,A.�ϸ�,�X�f�q��,B.ORDSDY �q����,B.ORDQTY �q��ƶq,B.ORDFQY �X�f�ƶq,C.INPART �s�d,
C.ORDSNO �s�d���,C.�J�w����,C.ORDQTY �s�d��
INTO #�έp���
FROM (SELECT distinct �X�f�q��,INPART,�ϸ� FROM #���X�f�q��) A ,ORDE2 B,ORDE3 C
WHERE A.�X�f�q�� = B.INPART AND A.INPART = C.INPART AND C.INFIN IN ('N','P') --AND B.ORDFCO = 'N'

--SELECT * FROM #�έp���
--WHERE �M�׽s�� LIKE 'F10302%' AND �ϸ� = '0022-75564'

--�ϸ� = '0022-75564' AND
--�M�׽s�� = 'F10302'


--SELECT * FROM #�έp���
--SELECT distinct �M�׽s��,�ϸ�,�X�f�q��,�q��ƶq,�X�f�ƶq FROM #�έp���
--WHERE �M�׽s�� LIKE 'F10323%' AND �ϸ� = '0042-44600'

--2022/05/06 �Ȥ�PO���ǤJ�M�����Ƥ� Techup
--SELECT �M�׽s��,�Ȥ�PO,�q��ϸ�,SUM(�q��ƶq) �`�q���,SUM(�X�f�ƶq) �`�X�f��
--INTO #�Ȥ�M�׽s���`�q���
--FROM
--(SELECT distinct �M�׽s��,�Ȥ�PO,�q��ϸ�,�q��ƶq,�X�f�ƶq FROM #�έp���) A
--GROUP BY �M�׽s��,�Ȥ�PO,�q��ϸ�

SELECT �M�׽s��,�ϸ�,SUM(�q��ƶq) �`�q���,SUM(�X�f�ƶq) �`�X�f��
INTO #�Ȥ�M�׽s���`�q���
FROM
(SELECT distinct �M�׽s��,�ϸ�,�X�f�q��,�q��ƶq,�X�f�ƶq FROM #�έp���) A
GROUP BY �M�׽s��,�ϸ�

SELECT �s�d,�Ѿl���[��= (SELECT COUNT(*) FROM ORDDE4  A,#SOPNAME B WHERE A.ORDFO = B.PRDOPNO --AND B.SOPKIND = '���['
AND A.ORDFNO = �s�d)
INTO #�U�s�d�Ѿl���[��
FROM #�έp���



--SELECT A.*,B.�`�q���,B.�`�X�f��,C.�Ѿl���[��,ROW_NUMBER() OVER(PARTITION BY A.�M�׽s��,A.�Ȥ�PO,A.�q��ϸ�  ORDER BY CAST(�J�w���� AS int))
SELECT A.*,B.�`�q���,B.�`�X�f��,C.�Ѿl���[��,ROW_NUMBER() OVER(PARTITION BY A.�M�׽s��,A.�ϸ�  ORDER BY CAST(�J�w���� AS int))
+CAST(B.�`�X�f�� AS int)  
'�ĴX�i�s�d'
INTO #�έp���_NEW
FROM #�έp��� A,#�Ȥ�M�׽s���`�q��� B,#�U�s�d�Ѿl���[�� C
--WHERE A.�M�׽s�� = B.�M�׽s�� AND A.�Ȥ�PO = B.�Ȥ�PO AND A.�s�d = C.�s�d AND A.�q��ϸ� = B.�q��ϸ�
WHERE A.�M�׽s�� = B.�M�׽s�� AND A.�s�d = C.�s�d AND A.�ϸ� = B.�ϸ�
---AND A.�Ȥ�PO = '4500427599'
ORDER BY �M�׽s��,�Ȥ�PO,�q����,�X�f�q��







SELECT *,�������= CAST(CAST(�`�q��� AS int) AS varchar(10))+'-'+CAST(CAST( (CASE WHEN �s�d�� > 1 THEN �s�d�� ELSE �ĴX�i�s�d END) AS INT )AS varchar(10))
INTO #�έp���_NEW_01
FROM #�έp���_NEW
--ORDER BY �M�׽s��,�Ȥ�PO--,���,����
ORDER BY �M�׽s��--,���,����

UPDATE ORDE3
SET �ӱM�ײ�N�i�s�d = B.�ĴX�i�s�d,�M������ = B.�������
FROM ORDE3 A,#�έp���_NEW_01 B
WHERE A.INPART = B.�s�d
--SELECT * FROM  #�έp���_NEW_01

----03:39
    --EXEC dbo.����ORDE3�Ѿl�s�{ ''


--SELECT * FROM #�έp���_NEW_01
----WHERE �Ȥ�PO = '4500429270'
----WHERE �M�׽s�� = '25C324-LID'
--WHERE �M�׽s�� = '25C328-LID'
----WHERE �M�׽s�� LIKE 'NSO25S01201E%'

----WHERE �X�f�q�� = 'D2005171AF'
----WHERE �s�d = '20D05173AF-000'
--ORDER BY �M�׽s��,�Ȥ�PO,�q��ϸ�,CAST(�J�w���� AS int),�q����

----�쥻���g��ORDDE4_�Ѿl�s�{����_��ӧ���ORDE3�� 2020/12/08 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET AKT�j�����~���� = '('+B.�������+')'
--FROM ORDDE4_�Ѿl�s�{����_D A,#�έp���_NEW_01 B
--WHERE A.INPART = B.�s�d���X
-----------�p��ثe����--------------------------------------------------------------------------------------------------------------------------------------




IF @INPART  = '%'
BEGIN

UPDATE ORDDE4_�Ѿl�s�{����_D
SET AutoPc = 'Y'
WHERE INPART IN (SELECT INPART FROM #AutoPc�����ɶ�)

--EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT * FROM #TEMP3
--ORDER BY ORDSQ2

 
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �̫���[�� = B.ORDSQ
FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT MAX(ORDSQ2) ORDSQ,INPART FROM #TEMP3 WHERE SOPKIND = '���[' GROUP BY INPART) B
WHERE A.INPART = B.INPART

---- 2018/01/22 ADD
UPDATE ORDDE4_�Ѿl�s�{����_D SET �Ѿl�Ƶ{���� = B.�Ѿl�Ƶ{����
 FROM ORDDE4_�Ѿl�s�{����_D A,ORDDE4_�Ƶ{���� B
WHERE A.ORDTP = B.ORDTP
  AND A.ORDNO = B.ORDNO
  AND A.ORDSQ = B.ORDSQ
  AND A.ORDSQ1 = B.ORDSQ1


SELECT ���Ĥu�� = (dbo.�ɶ��t_NEW(GETDATE(),A.ORDSNO+ ' 07:50',10)/60.00),
        * INTO #�U�s�d���Ĥu�� FROM (select distinct ORDSNO FROM ORDDE4_�Ѿl�s�{����_D) A

UPDATE ORDDE4_�Ѿl�s�{����_D
SET ���Ĥu�� = B.���Ĥu��
FROM ORDDE4_�Ѿl�s�{����_D A,#�U�s�d���Ĥu�� B
WHERE A.ORDSNO = B.ORDSNO
   -----��ΤW�z�覡 �[�ֳt�� 2024/01/13 Techup
----UPDATE ORDDE4_�Ѿl�s�{����_D SET ���Ĥu�� = (dbo.�ɶ��t_NEW(GETDATE(),ORDSNO+ ' 07:50',10)/60.00)-CUS�u�� --���Ĥu�� �n���� �w�dCUS�u��30%���u�� 2023/04/08 Techup

--------------------------�B�z�|�窺�s�d ---�᭱�b�N���Y��(�W�e�̤p)���@���s�d�ɤW�쥻���|��u��-2024/05/27 Techup--------------------------------------------------------------------------------------------

-----����Ѿl�u�ɦP��|�� �������`�|��u��
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�u�ɦP��|�� = �Ѿl�u��-C.�`�|��u��
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,(SELECT INPART,SUM(ORDFM1)/60.0 �`�|��u��  FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE SOPKIND = '�|��' AND ORDFCO = 'N' AND ORDSQ3 = 0 GROUP BY INPART) C
WHERE A.INPART = B.INPART
--AND B.INDWG = '33A4-H11-01'
AND INFIN = 'N' AND �Ѿl�s�{����4 LIKE '%�|��%'
AND A.INPART = C.INPART

-------��X���Y�����i�ιϸ�
SELECT
INDWG,B.ORDSNO,MIN(�i�Τu��) �i�Τu��
INTO #���Y���ݷ|��ϸ�
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
WHERE A.INPART = B.INPART
---AND B.INDWG = '33A4-H11-01'
AND INFIN = 'N' AND �Ѿl�s�{����4 LIKE '%�|��%'
GROUP BY INDWG,B.ORDSNO
--ORDER BY A.ORDSNO

-------��X���Y�����i�ιϸ��̤p�s�d
SELECT C.INDWG,C.ORDSNO,C.�i�Τu��,MIN(A.INPART) INPART
INTO #���Y���ݷ|��s�d_����
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B ,#���Y���ݷ|��ϸ� C
WHERE A.INPART = B.INPART AND B.INDWG = C.INDWG AND B.ORDSNO = C.ORDSNO AND A.�i�Τu�� = C.�i�Τu��
GROUP BY C.INDWG,C.ORDSNO,C.�i�Τu��


----����Y�����i�ιϸ��̤p�s�d�[�^�@�����`�|��u��
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�u�ɦP��|�� = ISNULL(�Ѿl�u�ɦP��|��,0) + ISNULL(�`�|��u��,0)
FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT INPART,SUM(ORDFM1)/60 �`�|��u��  FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE SOPKIND = '�|��' AND ORDFCO = 'N' AND ORDSQ3 = 0 GROUP BY INPART) B
WHERE A.INPART = B.INPART
AND A.INPART IN (SELECT INPART FROM #���Y���ݷ|��s�d_����)
--------------------------�B�z�|�窺�s�d ---�᭱�b�N���Y��(�W�e�̤p)���@���s�d�ɤW�쥻���|��u��-2024/05/27 Techup--------------------------------------------------------------------------------------------


       -----------------�Ȯ������q���Ѿl�u�ɭp�� 2025/04/07-------------------------------
-----------------�B�z�q���Ѿl�u�� 2025/01/06----------------------------
-------��X�q�����ݭn�s�d 2025/01/06 Techup
--SELECT A.INPART,U_INPART=CASE WHEN ( (LEN(A.INPART) - LEN(REPLACE(A.INPART,'-',''))) / LEN('-') =  1) OR
-- ((LEN(A.INPART) - LEN(REPLACE(A.INPART,'-',''))) / LEN('-') = 2 AND
-- (
--   A.INPART LIKE '%-0-%' OR A.INPART LIKE '%-1-%' OR A.INPART LIKE '%-2-%' OR A.INPART LIKE '%-3-%' OR
--   A.INPART LIKE '%-4-%' OR A.INPART LIKE '%-5-%' OR A.INPART LIKE '%-6-%' OR A.INPART LIKE '%-7-%' OR
--   A.INPART LIKE '%-8-%' OR A.INPART LIKE '%-9-%' OR A.INPART LIKE '%-10-%' OR A.INPART LIKE '%-11-%' OR
--   A.INPART LIKE '%-12-%' OR A.INPART LIKE '%-13-%' OR A.INPART LIKE '%-14-%' OR A.INPART LIKE '%-15-%' OR
--   A.INPART LIKE '%-16-%' OR A.INPART LIKE '%-17-%' OR A.INPART LIKE '%-18-%' OR A.INPART LIKE '%-19-%' OR
--   A.INPART LIKE '%-20-%' OR A.INPART LIKE '%-21-%' OR A.INPART LIKE '%-22-%' OR A.INPART LIKE '%-23-%'
-- )
-- )  
-- THEN LEFT(A.INPART,LEN(A.INPART)-CHARINDEX('-', REVERSE(A.INPART)))+'-000'
-- ELSE LEFT(A.INPART,LEN(A.INPART)-CHARINDEX('-', REVERSE(A.INPART))) END,�Ѿl�u�� AS �q�����Ѿl�u��
--   INTO #�q���ݭn�s�d
--   FROM ORDDE4_�Ѿl�s�{����_D C,ORDE3 A,ORDE2 B
-- WHERE A.ORDTP = B.ORDTP
-- AND A.ORDNO = B.ORDNO
-- AND A.ORDSQ = B.ORDSQ
-- AND A.ORDTP = C.ORDTP
-- AND A.ORDNO = C.ORDNO
-- AND A.ORDSQ = C.ORDSQ
-- AND A.ORDSQ1 = C.ORDSQ1
-- AND A.INDWG <> B.INDWG
-- AND A.INPART LIKE '%-E%' AND A.INFIN = 'N' AND C.�Ѿl�u�� > 0
       
-------��X�q���ݭn�s�d���̤p�q���s�{ 2025/01/06 Techup
--SELECT A.INPART,A.�q�����Ѿl�u��,A.U_INPART �q���ݭn�s�d,B.�̤p�q���s�{ �q���ݭn�s�d�̤p�q���s�{
--INTO #�q���ݭn�s�d�̤p�q���s�{
--FROM #�q���ݭn�s�d A,(SELECT MIN(A.ORDSQ2) �̤p�q���s�{,A.INPART
--FROM ORDDE4_�Ѿl�s�{����_����_D A , (SELECT INPART,INDWG FROM ORDE3) B,ORDDE4 C,SOPNAME D
--WHERE A.ORDFCO = 'N' AND A.PRDNAME NOT LIKE 'Z%' AND A.PRDNAME NOT LIKE '%��%' AND ORDSQ3 = 0
--AND A.ORDFO NOT IN ('N01','N02','N03','N04','24E','24I','24H','24J','24K','24L')
--AND A.ORDFO = D.PRDOPNO AND D.DESCR NOT LIKE '%�O��%'
--AND A.ORDSQ2 >= -10  ---- 2023/02/18 �v �Ѿl�u�ɱư�-50 -100
--AND A.INPART = B.INPART
--AND A.INPART = C.ORDFNO AND A.ORDSQ2 = C.ORDSQ2
--AND D.PRDOPGP IN ('14','60')
--GROUP BY A.INPART) B
--WHERE A.U_INPART = B.INPART

--------��X�q�����ݭn�s�d�s�{���᪺�Ѿl�u�� 2025/01/06 Techup
--SELECT E.�q���ݭn�s�d,E.�q���ݭn�s�d�̤p�q���s�{,��q���s�{���ᤧ�Ѿl�s�{ = CONVERT(DECIMAL(10,2),CONVERT(FLOAT,
--SUM(
--CASE
-- WHEN B.INDWG = 'DRW26640' AND A.ORDFO IN ('I01','I02','I03','I04','I05','I06','I07','I08','I09','I10') THEN A.ORDFM1 / 3 ---- 2023/07/17 �R��n�D
-- --WHEN ORDFO LIKE 'CQ%' THEN 10*5*60
-- WHEN A.ORDDTP = 1 THEN CASE WHEN A.ORDFM1 < C.ORDMT3 THEN 0 ELSE A.ORDFM1 - ISNULL(C.ORDMT3,0) END ----��w�����u�B�j��w��u�� �h��u�ɧ�0 �ϥ��h�n�����w���u 2024/03/29 Techup
-- WHEN A.ORDDTP = 2 THEN REPLACE(�~�]�w�p�Ѽ�,'D','')*10*60
-- END
--) /60))
--INTO #�q�����ݭn�s�d�s�{���᪺�Ѿl�u��
--FROM ORDDE4_�Ѿl�s�{����_����_D A ,(SELECT INPART,INDWG FROM ORDE3) B,ORDDE4 C,SOPNAME D,
--(SELECT distinct �q���ݭn�s�d,�q���ݭn�s�d�̤p�q���s�{ FROM #�q���ݭn�s�d�̤p�q���s�{) E
--WHERE A.ORDFCO = 'N' AND A.PRDNAME NOT LIKE 'Z%' AND A.PRDNAME NOT LIKE '%��%' AND ORDSQ3 = 0
--AND A.ORDFO NOT IN ('N01','N02','N03','N04','24E','24I','24H','24J','24K','24L')
--AND A.ORDFO = D.PRDOPNO AND D.DESCR NOT LIKE '%�O��%'
--AND A.INPART = B.INPART
--AND A.INPART = C.ORDFNO AND A.ORDSQ2 = C.ORDSQ2
--AND A.ORDSQ2 >= -10  ---- 2023/02/18 �v �Ѿl�u�ɱư�-50 -100
--AND A.INPART = E.�q���ݭn�s�d AND A.ORDSQ2 >= E.�q���ݭn�s�d�̤p�q���s�{
----AND A.INPART  = '24H03130-000' AND A.ORDSQ2 >= '29'
--GROUP BY E.�q���ݭn�s�d,E.�q���ݭn�s�d�̤p�q���s�{

--------��s�ݭn�s�d�s�{���᪺�Ѿl�u�ɥ[��q���s�d�W 2025/01/06 Techup
-----SELECT C.INPART,C.�Ѿl�u��,A.*,B.��q���s�{���ᤧ�Ѿl�s�{
--UPDATE ORDDE4_�Ѿl�s�{����_D SET �Ѿl�u�� = �Ѿl�u��+��q���s�{���ᤧ�Ѿl�s�{
--FROM #�q���ݭn�s�d�̤p�q���s�{ A,#�q�����ݭn�s�d�s�{���᪺�Ѿl�u�� B,ORDDE4_�Ѿl�s�{����_D C
--WHERE A.�q���ݭn�s�d = B.�q���ݭn�s�d
--AND A.INPART = C.INPART
-----------------�B�z�q���Ѿl�u�� 2025/01/06----------------------------
-----------------�Ȯ������q���Ѿl�u�ɭp�� 2025/04/07-------------------------------




---- 20200608 �p��i�Τu��
UPDATE ORDDE4_�Ѿl�s�{����_D SET �i�Τu�� = ���Ĥu�� - �Ѿl�u�� - CUS�u��  -----�i�Τu�ɶW�e�u�� �]�n����CUS�u�� 2025/05/14 Techup
----+ ISNULL(�W���̤j�k���s�d�Ѿl�u��,0)  -----2025/01/13 ���i�n�D�W�����Ѿl�u�ɤ]�n�ǤJ�U�����Ѿl�u�� ���ƪ����P�N Techup

------
--SELECT *,�`���� = CASE WHEN CHARINDEX(N'��', TOTAL) > 0 THEN (LEN(TOTAL) - LEN(REPLACE(TOTAL, N'��', '')))+1 ELSE 0 END
--UPDATE ORDDE4_�Ѿl�s�{����_D



UPDATE ORDDE4_�Ѿl�s�{����_D SET �S�O�i�Τu�� =
���Ĥu��
-
(TOTAL�u�� +
((CASE WHEN CHARINDEX(N'��', TOTAL) > 0 THEN (LEN(TOTAL) - LEN(REPLACE(TOTAL, N'��', '')))+1 ELSE 0 END)*5)) --�@��5hr�p�� 2025/05/27 Techup
-
CUS�u��  -----�i�Τu�ɶW�e�u�� �]�n����CUS�u�� 2025/05/14 Techup

---- �NPCDATE < ���骺 ���᪺�ɶ��[�W�h��@�Y����
SELECT
���Ĥu�� = (dbo.�ɶ��t_NEW(GETDATE(),A.ORDSNO+ ' 07:50',10)/60.00),
* INTO #�U�s�d���Ĥu��_����
FROM (
select distinct ORDSNO from ORDDE4_�Ѿl�s�{����_D
WHERE �i�Τu�� < 0
AND CONVERT(VARCHAR(10),ORDSNO,111) < CONVERT(VARCHAR(10),GETDATE(),111)
AND CONVERT(VARCHAR(10),ORDSNO,111) >= '2019/01/01'
) A

UPDATE ORDDE4_�Ѿl�s�{����_D
SET ���Ĥu�� = B.���Ĥu��
FROM ORDDE4_�Ѿl�s�{����_D A,#�U�s�d���Ĥu��_���� B
WHERE A.ORDSNO = B.ORDSNO
AND �i�Τu�� < 0
-------��ΤW�z�覡 �[�ֳt�� 2024/01/13 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_D SET �i�Τu�� = �i�Τu�� +  (-(dbo.�ɶ��t_NEW(ORDSNO+ ' 07:50',GETDATE(),10)/60.00 ))
--WHERE �i�Τu�� < 0
--AND CONVERT(VARCHAR(10),ORDSNO,111) < CONVERT(VARCHAR(10),GETDATE(),111)
--AND CONVERT(VARCHAR(10),ORDSNO,111) >= '2019/01/01'


--SELECT  U_INPART=CASE WHEN ( (LEN(A.INPART) - LEN(REPLACE(A.INPART,'-',''))) / LEN('-') =  1) OR
  --                              ((LEN(A.INPART) - LEN(REPLACE(A.INPART,'-',''))) / LEN('-') = 2 AND ( A.INPART LIKE '%-0-%' OR A.INPART LIKE '%-1%'))  
  --                           THEN LEFT(A.INPART,LEN(A.INPART)-CHARINDEX('-', REVERSE(A.INPART)))+'-000'
  --                           ELSE LEFT(A.INPART,LEN(A.INPART)-CHARINDEX('-', REVERSE(A.INPART))) END,*
       UPDATE ORDDE4_�Ѿl�s�{����_D
  SET U_INPART=CASE WHEN ( (LEN(A.INPART) - LEN(REPLACE(A.INPART,'-',''))) / LEN('-') =  1) OR
                                ((LEN(A.INPART) - LEN(REPLACE(A.INPART,'-',''))) / LEN('-') = 2 AND
(

      A.INPART LIKE '%-0-%' OR A.INPART LIKE '%-1-%' OR A.INPART LIKE '%-2-%' OR A.INPART LIKE '%-3-%' OR
  A.INPART LIKE '%-4-%' OR A.INPART LIKE '%-5-%' OR A.INPART LIKE '%-6-%' OR A.INPART LIKE '%-7-%' OR
  A.INPART LIKE '%-8-%' OR A.INPART LIKE '%-9-%' OR A.INPART LIKE '%-10-%' OR A.INPART LIKE '%-11-%' OR
  A.INPART LIKE '%-12-%' OR A.INPART LIKE '%-13-%' OR A.INPART LIKE '%-14-%' OR A.INPART LIKE '%-15-%' OR
  A.INPART LIKE '%-16-%' OR A.INPART LIKE '%-17-%' OR A.INPART LIKE '%-18-%' OR A.INPART LIKE '%-19-%' OR
  A.INPART LIKE '%-20-%' OR A.INPART LIKE '%-21-%' OR A.INPART LIKE '%-22-%' OR A.INPART LIKE '%-23-%'

)

)  
                             THEN LEFT(A.INPART,LEN(A.INPART)-CHARINDEX('-', REVERSE(A.INPART)))+'-000'
                             ELSE LEFT(A.INPART,LEN(A.INPART)-CHARINDEX('-', REVERSE(A.INPART))) END
  FROM ORDDE4_�Ѿl�s�{����_D C,ORDE3 A,ORDE2 B
     WHERE A.ORDTP = B.ORDTP
        AND A.ORDNO = B.ORDNO
        AND A.ORDSQ = B.ORDSQ
        AND A.ORDTP = C.ORDTP
        AND A.ORDNO = C.ORDNO
        AND A.ORDSQ = C.ORDSQ
        AND A.ORDSQ1 = C.ORDSQ1
AND A.INDWG <> B.INDWG
        --AND A.INPART LIKE '19K01121AF%'
        AND A.INPART NOT LIKE '%-E%' AND A.INPART NOT LIKE '%-F%'  AND A.INPART NOT LIKE '%-C%'
        --AND A.INFIN <> 'C'
     --ORDER BY U_INPART,C.INPART

-----2025/11/04 ��s���v�� �W���ݨD�s�d
UPDATE ORDDE4_�Ѿl�s�{����_D SET U_INPART = B.�ݨD�s�d
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
WHERE A.INPART LIKE '%-F%' AND A.INPART = B.INPART AND ISNULL(U_INPART,'') = ''


---- EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT 'AAAAAAAAAAAAAAAAA',����k���s�d���A,U_INPART,* FROM ORDDE4_�Ѿl�s�{����_D
--WHERE INPART = '23G04777SL-6-001#1'


----2024/07/23 ���s�����W�����s�d �v---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--SELECT �O�_�����s�d = B.INPART, �s�s�d = MIN(C.INPART),A.U_INPART ,A.INPART
--INTO #�ץ�U_INPART�w����
--FROM (SELECT A.*,U_INDWG = B.INDWG FROM ORDDE4_�Ѿl�s�{����_D A , ORDE3 B WHERE A.U_INPART = B.INPART) A
--      LEFT OUTER JOIN (SELECT * FROM ORDE3 WHERE INFIN = 'N') B ON A.U_INPART = B.INPART
--  LEFT OUTER JOIN (SELECT * FROM ORDE3 WHERE INFIN = 'N') C ON A.ORDTP = C.ORDTP  AND A.ORDNO = C.ORDNO  AND A.ORDSQ = C.ORDSQ AND A.U_INDWG = C.INDWG AND A.ORDSNO = C.ORDSNO
--GROUP BY B.INPART,A.U_INPART ,A.INPART

----2024/08/02 ��g�W�� ���g�s�d���ݭn�J�w �G�ݭn�P�_��s�{�O�_���������� Techup
SELECT �O�_�����s�d = B.INPART, �s�s�d = MIN(C.INPART),A.U_INPART ,A.INPART
INTO #�ץ�U_INPART�w����
FROM (SELECT A.*,U_INDWG = B.INDWG FROM ORDDE4_�Ѿl�s�{����_D A , ORDE3 B WHERE A.U_INPART = B.INPART) A
LEFT OUTER JOIN (SELECT A.INPART,COUNT(*) SQ FROM ORDE3 A,ORDDE4 B WHERE INFIN = 'N'
AND A.INPART = B.ORDFNO AND ORDFCO = 'N' AND ORDDTP IN ('1','2')
GROUP BY A.INPART) B ON A.U_INPART = B.INPART
LEFT OUTER JOIN (SELECT A.INPART,A.ORDTP,A.ORDNO,A.ORDSQ,A.INDWG,A.ORDSNO,COUNT(*) SQ FROM ORDE3 A,ORDDE4 B WHERE INFIN = 'N'
AND A.INPART = B.ORDFNO AND ORDFCO = 'N' AND ORDDTP IN ('1','2')
GROUP BY A.INPART,A.ORDTP,A.ORDNO,A.ORDSQ,A.INDWG,A.ORDSNO) C ON A.ORDTP = C.ORDTP  AND A.ORDNO = C.ORDNO  AND A.ORDSQ = C.ORDSQ AND A.U_INDWG = C.INDWG AND A.ORDSNO = C.ORDSNO
GROUP BY B.INPART,A.U_INPART ,A.INPART

UPDATE ORDDE4_�Ѿl�s�{����_D SET U_INPART = ISNULL(�O�_�����s�d,�s�s�d) FROM ORDDE4_�Ѿl�s�{����_D A ,#�ץ�U_INPART�w���� B WHERE A.INPART = B.INPART

---- EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT 'BBBBBBBBBBBBBBBB',����k���s�d���A,U_INPART,* FROM ORDDE4_�Ѿl�s�{����_D
--WHERE INPART = '23G04777SL-6-001#1'


-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

--EXEC dbo.����ORDE3�Ѿl�s�{ ''
---�g�J-----ORDE3 --�@�ѥu�O���@��
--���W7�I���I�~���@��
DECLARE @����ɶ� varchar(10)

--SET @����ɶ� = (SELECT SUBSTRING(CONVERT(VARCHAR(20), GETDATE(), 120), 12, 2))

--SELECT DATEPART(HH,GETDATE())

 
--2019/11/26
IF (DATEPART(HH,GETDATE()) = 23 )
BEGIN
DELETE dbo.ORDDE4_�Q��Ѿl�s�{����

INSERT INTO dbo.ORDDE4_�Q��Ѿl�s�{���� SELECT * FROM ORDDE4_�Ѿl�s�{����_D
END




--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D

--IF ( @����ɶ� = '07')
--���@�I�~��z�Q�Ѫ���� 2019/12/09
IF (DATEPART(HH,GETDATE()) = 7)
BEGIN


--SELECT DATEPART(HH,GETDATE())

--���d GM�٨S�T�{���e�S���ҿ׳Ѿl�u�� 2021/04/14 Techup
UPDATE ORDDE4_�Q��Ѿl�s�{����
SET �Ѿl�u�� = 0
FROM ORDDE4_�Q��Ѿl�s�{���� A,ORDE2 B
   WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND B.NOTOPEN = 'N' AND B.INPART LIKE '%*%'


UPDATE ORDDE4_�Ѿl�s�{����_D
SET �e��Ѿl�u�� = B.�Ѿl�u��
--FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,�Ѿl�u�� FROM TEST.dbo.ORDDE4_�Ѿl�s�{����_D) B --#�e��Ѿl�u�� �󴫤覡 ��δ��հϸ��
FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT ORDTP,ORDNO,ORDSQ,ORDSQ1,INPART,�Ѿl�u�� FROM dbo.ORDDE4_�Q��Ѿl�s�{����) B --#�e��Ѿl�u�� �󴫤覡 ��δ��հϸ��
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.INPART = B.INPART


SELECT INPART,ORDTP,ORDNO,ORDSQ,ORDSQ1, CONVERT(varchar, CAST(SUM(ORDFM1)/60 AS decimal(9,1))) �Ѿl�u��
INTO #�Ѿl�u��
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE ORDFCO = 'N' AND PRDNAME NOT LIKE 'Z%'
AND INPART LIKE @INPART
GROUP BY INPART,ORDTP,ORDNO,ORDSQ,ORDSQ1
ORDER BY INPART--,ORDSQ2




--���d GM�٨S�T�{���e�S���ҿ׳Ѿl�u�� 2021/04/14 Techup
UPDATE #�Ѿl�u��
SET �Ѿl�u�� = 0
FROM #�Ѿl�u�� A,ORDE2 B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND B.NOTOPEN = 'N' AND B.INPART LIKE '%*%'



--�W�L���������R���Ĥ@��
UPDATE ORDE3  
SET �e���ѳѾl�u�ɬ��� = SUBSTRING(�e���ѳѾl�u�ɬ���,CHARINDEX(',',�e���ѳѾl�u�ɬ���)+1,(LEN(�e���ѳѾl�u�ɬ���)-(CHARINDEX(',',�e���ѳѾl�u�ɬ���))))
WHERE (len(�e���ѳѾl�u�ɬ���)-len(replace(�e���ѳѾl�u�ɬ���, ',', ''))) >= 10

UPDATE ORDE3
SET �e���ѳѾl�u�ɬ��� = (CASE WHEN ISNULL(�e���ѳѾl�u�ɬ���,'') = '' THEN B.�Ѿl�u��+',' ELSE ISNULL(�e���ѳѾl�u�ɬ���,'')+B.�Ѿl�u��+',' END)
FROM ORDE3 A, #�Ѿl�u�� B
WHERE A.INPART = B.INPART


--�إ߫e���ѳ��u���� 2019/12/30
SELECT A.INPART,�e���ѳѾl�u�ɬ���,�e���ѳ��u����
INTO #ORDE3
FROM ORDE3 A,(SELECT distinct INPART FROM #�Ѿl�u��) B
WHERE INFIN IN ('N','P') AND ORDQTY > 0
AND ISNULL(�e���ѳѾl�u�ɬ���,'') <> ''
AND LINE <> 'Z' AND ORDQTY <> ORDSQY
AND (SELECT COUNT(*) FROM ORDDE4  WHERE ORDFO = '27' AND A.INPART = ORDDE4.ORDFNO ) = 0
AND A.INPART = B.INPART

--DECLARE @INPART  VARCHAR(40)
DECLARE @�e���ѳѾl�u�ɬ���  VARCHAR(500)

SELECT INPART = CAST('' AS VARCHAR(40)) ,*
INTO #TEMP�Ȧs
FROM �ഫ�r��ܸ�ƪ�_�t�Ǹ�('',',')
WHERE 1 = 0

WHILE (SELECT COUNT(*) FROM #ORDE3) > 0
BEGIN
  SELECT @INPART=INPART,@�e���ѳѾl�u�ɬ���=�e���ѳѾl�u�ɬ���  FROM #ORDE3

  INSERT INTO #TEMP�Ȧs
  SELECT @INPART INPART ,* FROM �ഫ�r��ܸ�ƪ�_�t�Ǹ�(@�e���ѳѾl�u�ɬ���,',')

  DELETE #ORDE3 WHERE INPART =@INPART
END

delete #TEMP�Ȧs WHERE item = ''

update #TEMP�Ȧs
set �Ǹ� = (�Ǹ�-(SELECT COUNT(*)+1 FROM #TEMP�Ȧs A WHERE #TEMP�Ȧs.INPART = A.INPART ))

SELECT *,���= convert(varchar, DATEADD (DD , �Ǹ� , getdate()), 111)
INTO #TEMP2�Ȧs
FROM #TEMP�Ȧs

SELECT A.PTPNO,PRTFO,B.PRDNAME,CRDATE,PRTFM,��ڳ��u= DATEDIFF(ss,CRDATE,PRTFM)/60
INTO #PRODTM
FROM PRODTM A,(SELECT * FROM #SOPNAME WHERE ISACTIVE <> 1 ) B
WHERE PTPNO IN (SELECT distinct INPART FROM #TEMP2�Ȧs)
AND PRTFO = B.PRDOPNO

SELECT �إߤ� = convert(varchar(10),CRDATE, 111),PTPNO,PRTFO,PRDNAME,CRDATE ���u�}�l��,PRTFM ���u�����
INTO #PRODTM_N
FROM #PRODTM  

SELECT A.*,MIN(���u�}�l��) ���u�}�l��,MAX(���u�����) ���u�����
INTO #TEMP3�Ȧs
FROM #TEMP2�Ȧs A LEFT OUTER JOIN #PRODTM_N B ON A.INPART = B.PTPNO AND A.��� = B.�إߤ�
GROUP BY A.INPART,A.item,A.�Ǹ�,A.���
ORDER BY A.INPART,�Ǹ�

SELECT DISTINCT �إߤ�,PTPNO,
( SELECT PO_A.PRDNAME+';'
FROM (SELECT �إߤ�,PTPNO,PRTFO,PRDNAME FROM #PRODTM_N GROUP BY �إߤ�,PTPNO,PRTFO,PRDNAME ) PO_A
WHERE PO_A.PTPNO = PO_B.PTPNO AND PO_A.�إߤ� = PO_B.�إߤ�
FOR XML PATH('')
) AS �s�{����
INTO #��z�s�{
FROM #PRODTM_N PO_B
ORDER BY PO_B.PTPNO,PO_B.�إߤ�

SELECT INPART,���,CONVERT(varchar, DatePart(hour, ���u�}�l��))+'~'+CONVERT(varchar,
CASE WHEN DatePart(hour, ���u�����) = 0 THEN 24 ELSE DatePart(hour, ���u�����) END
) ���u�ɶ�
INTO #TEMP4�Ȧs
FROM #TEMP3�Ȧs A LEFT OUTER JOIN #��z�s�{ B ON A.INPART = B.PTPNO AND A.��� = B.�إߤ�
ORDER BY INPART,���
       
UPDATE ORDE3
SET �e���ѳ��u���� = (SELECT ISNULL(���u�ɶ�,'')+',' FROM #TEMP4�Ȧs WHERE A.INPART = #TEMP4�Ȧs.INPART  FOR XML PATH(''))
FROM ORDE3 A,(SELECT distinct INPART FROM  #TEMP4�Ȧs) B
WHERE A.INPART = B.INPART




--�إ߫e���ѳ��u���� 2019/12/30
END
END

----04:26
    --EXEC dbo.����ORDE3�Ѿl�s�{ ''


------2022/12/13 Techup ��s�b���s�{�ǫe����DLYTIME
SELECT
ROW_NUMBER() OVER(Partition By A.INPART ORDER BY ORDSQ2) AS �s�{�Ǹ�ROWID,
A.*
INTO #�U�s�d�s�{��
FROM #TEMP3 A ,#SOPNAME B
WHERE ORDSQ3 = 0 AND A.ORDFO = B.PRDOPNO AND A.PRDNAME NOT IN ('lo','uld','ULD','LD')
AND B.ISACTIVE = 0

    SELECT B.INPART,SUM(B.DLYTIME) �e����DLYTIME
INTO #�U�s�d�e����DLYTIME�X�p
FROM (SELECT INPART,MIN(�s�{�Ǹ�ROWID) �s�{�Ǹ�ROWID FROM #�U�s�d�s�{�� WHERE ORDFCO = 'N' GROUP BY INPART) A,#�U�s�d�s�{�� B
WHERE A.INPART = B.INPART AND A.�s�{�Ǹ�ROWID-2 <= B.�s�{�Ǹ�ROWID AND A.�s�{�Ǹ�ROWID <> B.�s�{�Ǹ�ROWID
GROUP BY B.INPART
------2022/12/13 Techup ��s�b���s�{�ǫe����DLYTIME

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�ǫe����DLYTIME = A.�e����DLYTIME
FROM #�U�s�d�e����DLYTIME�X�p A,ORDDE4_�Ѿl�s�{����_D B
WHERE A.INPART = B.INPART
AND A.INPART LIKE @INPART
------2022/12/13 Techup ��s�b���s�{�ǫe����DLYTIME


  --�u�d�̤p���s�{N���e��Y(3��) --GM�n�ݥ��� �G������ 2019/10/31
  --�u�d�̤p���s�{N���e��Y(5��) --GM�n�ݥ��� �G������ 2022/09/23
  --�u�d�̤p���s�{N���e��Y(5��) --GM�n�ݥ��� �G������ 2022/11/01
--DELETE #TP4
-- FROM #TP4_1 A,#TP4 B
-- WHERE A.ORDFNO = B.ORDFNO AND A.ROWID-5 > B.ROWID  



--SELECT * FROM ORDDE4_�Ѿl�s�{����_D A,(
--SELECT SUM(C.DLYTIME) DLYTIME,C.INPART FROM ORDDE4_�Ѿl�s�{����_����_D C,
--(SELECT TOP 2 A.* FROM ORDDE4_�Ѿl�s�{����_����_D A,SOPNAME B
--WHERE INPART =  AND A.ORDFO = B.PRDOPNO AND B.ISACTIVE = 0
--AND ORDSQ3 = 0
--ORDER BY INPART,ORDSQ2 DESC) B
--WHERE C.ORDTP = B.ORDTP AND C.ORDNO = B.ORDNO AND C.ORDSQ = B.ORDSQ AND C.ORDSQ1 = B.ORDSQ1 AND C.ORDSQ2 = B.ORDSQ2
--AND C.ORDSQ3 = 0 GROUP BY C.INPART) B
--WHERE A.INPART = B.INPART

--,�b���s�{�ǫe����DLYTIME = 0



---���ƶO ���ƻs�{ �B���u��ܤw�g�o�� �B�b�s�{�Ǭ�0 ��ܭ�o�� Techup 2021/03/03 �s�W
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �����s�{���ӧtDLYTIME = '�w�o�ơ�'+�����s�{���ӧtDLYTIME,
   �����s�{���ӧtDLYTIME_���t�]�p='�w�o�ơ�'+�����s�{���ӧtDLYTIME_���t�]�p,
�Ѿl�s�{���� = '�w�o�ơ�'+�Ѿl�s�{����,
�Ѿl�s�{����2 = '�w�o�ơ�'+�Ѿl�s�{����,
�Ѿl�s�{����3 = '�w�o�ơ�'+�Ѿl�s�{����,
�Ѿl�s�{����4 = '�w�o�ơ�'+�Ѿl�s�{����


    --SELECT *
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B
    WHERE A.INPART = B.INPART AND A.PRDNAME LIKE '%��%' AND ���ƶO > 0 AND ORDFCO = 'Y' AND �b���s�{�� = 0
AND A.INPART LIKE @INPART

-------�ȮɥΤ��� �G�������B�z----------2022/02/10-Techup----------------------------------------------------------------------------------------
-------�����C�i�s�d�U�ɬq�Ѿl�u��-----2021/03/09-Techup------------------------------------------------------------------------------------------

----���N���s�b���g�J�s�d�U�ɬq�Ѿl�s�{
--INSERT INTO �s�d�U�ɬq�Ѿl�s�{
--SELECT convert(varchar(10), getdate(), 111) ���,INPART �s�d,0,0,0,0,0,0,0,0,0,0,0,0,0  FROM ORDDE4_�Ѿl�s�{����_D A
--LEFT OUTER JOIN �s�d�U�ɬq�Ѿl�s�{ B ON A.INPART = B.�s�d AND convert(varchar, getdate(), 111) = B.���
--WHERE ISNULL(B.�s�d,'') = ''

--DECLARE  @�g�J�ɶ�  VARCHAR(30)

--SET @�g�J�ɶ� = '['+ convert(varchar(10), DATEPART(HH,getdate())) +']'

--DECLARE @SQL VARCHAR(MAX)  
--SET @SQL = 'UPDATE �s�d�U�ɬq�Ѿl�s�{
--SET '+ @�g�J�ɶ� +' = B.�Ѿl�u��
--FROM �s�d�U�ɬq�Ѿl�s�{ A,ORDDE4_�Ѿl�s�{����_D B
--WHERE A.�s�d = B.INPART AND convert(varchar, getdate(), 111) = A.���'

--EXEC(@SQL)

-------�����C�i�s�d�U�ɬq�Ѿl�u��-----2021/03/09-Techup------------------------------------------------------------------------------------------
-------�ȮɥΤ��� �G�������B�z----------2022/02/10-Techup----------------------------------------------------------------------------------------

---------  2021/03/10 �ե߲k���s�{�wDELAY�B�����u���s�d, �ݱN�U�@���s�d�n���(ORDSEQ)�]��0
-- �ե߲k���s�{�wDELAY���s�d
--SELECT DISTINCT A.INPART,A.ORDSNO,��s�d=CASE WHEN A.INPART LIKE '%R%' AND A.INPART NOT LIKE '%#%'  THEN LEFT(A.INPART,CHARINDEX('R',A.INPART)-1)
-- WHEN A.INPART LIKE '%#%' THEN LEFT(A.INPART,CHARINDEX('#',A.INPART)-1)
-- ELSE A.INPART END
-- INTO #TQ1
-- FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,ORDE2 C
--WHERE ORDFO IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP IN ('23','33'))
-- AND ORDFCO = 'N'
-- AND A.INPART = B.INPART
-- AND B.INFIN = 'N'
-- AND CONVERT(DATETIME,A.ORDSNO) < GETDATE()
-- AND B.LINE <> 'Z'
-- AND B.ORDTP = C.ORDTP AND B.ORDNO= C.ORDNO AND B.ORDSQ = C.ORDSQ AND C.SCTRL ='Y'

SELECT DISTINCT A.INPART,A.ORDSNO,��s�d=CASE WHEN A.INPART LIKE '%R%' AND A.INPART NOT LIKE '%#%'  THEN LEFT(A.INPART,CHARINDEX('R',A.INPART)-1)
WHEN A.INPART LIKE '%#%' THEN LEFT(A.INPART,CHARINDEX('#',A.INPART)-1)
ELSE A.INPART END
INTO #TQ1
FROM ORDDE4_�Ѿl�s�{����_����_D A,(SELECT * FROM SFC3138NET_�[�Z�a���P�_ WHERE �إ߮ɶ� = (SELECT MAX(�إ߮ɶ�) FROM SFC3138NET_�[�Z�a���P�_)) B
WHERE ORDFO IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP IN ('23','33'))
AND ORDFCO = 'N'
AND A.INPART = B.�s�d
AND A.INPART LIKE @INPART


-- �̭�s�d��PCDATE��X�U�@���s�O�u��
SELECT DISTINCT A.INPART
INTO #TQ2
FROM ORDDE4_�Ѿl�s�{����_D A,#TQ1 B  
WHERE A.U_INPART = B.��s�d
AND A.�Ѿl�u�� > 0
AND A.ORDSNO = B.ORDSNO



-- �M��MIS�e�@����s��ORDSEQ_INFIN
UPDATE ORDE3 SET ORDSEQ_INFIN = NULL WHERE ISNULL(ORDSEQ_INFIN,'') <> ''

-- �N�ե߲k���s�{���U�@���s�d�n��ǳ]�w��Y
--SELECT A.INPART,A.ORDSNO,A.ORDSEQ,A.ORDSEQ_DATE,A.ORDSEQ_USER
UPDATE ORDE3 SET ORDSEQ_INFIN = 'Y'
FROM ORDE3 A,#TQ2 B
WHERE A.INPART = B.INPART
----- 2021/03/10 End

----2021/11/03 �����e24�p�ɪ�A1�s�d�M���x �v--------------------------------------------

DELETE A1_24 WHERE CRDATE < CONVERT(DATETIME,DATEADD(DD,-1,GETDATE()))

INSERT INTO A1_24
SELECT * FROM (
SELECT DISTINCT A.INPART, B.MAHNO, CRDATE= GETDATE() FROM ORDDE4_�Ѿl�s�{����_����_D A , MACPRD B
WHERE A.Applier = B.MAHNO
AND B.MAHNO NOT IN (SELECT PNAME FROM PERSON)
AND B.MAHNO NOT IN ('�X�u')
AND DEPT <> '' AND �ثe�Ƶ{���� = 'A1'
) A WHERE (INPART + MAHNO) NOT IN (SELECT (INPART + MAHNO) FROM A1_24)

----------------------------------------------------------------------------------------

UPDATE ORDDE4_�Ѿl�s�{����_D SET ���TOTAL =  '��'+'('+cast(CONVERT(int, B.ORDAMT/B.ORDQY2) AS NVARCHAR(30) )+')'  +'��' +���TOTAL
FROM ORDDE4_�Ѿl�s�{����_D A , ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND B.ORDSQ2 = '0' AND B.ORDFO = '��' AND A.���TOTAL NOT LIKE '%��%'
AND A.INPART LIKE @INPART

----- 2025/06/17
-- --���з�TOTAL 2023/03/21 Techup
-- SELECT distinct B.INDWG ,C.ORDFO
-- INTO #TEMP_��z�зǻs�{
-- FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_����_D C
-- WHERE A.INPART = B.INPART
-- AND A.INPART = C.INPART
-- AND A.�b���s�{�� = C.ORDSQ2
-- AND A.INPART LIKE @INPART
-- --AND A.�b���s�{�� IS NOT NULL

-- INSERT INTO #TEMP_��z�зǻs�{
-- SELECT distinct B.INDWG ,C.ORDFO FROM
-- (
-- SELECT B.INPART , ORDSQ2 =MIN(ORDSQ2)
-- --INTO #TEMP_��z�зǻs�{
-- FROM ORDDE4_�Ѿl�s�{����_D A,ORDDE4_�Ѿl�s�{����_����_D B
-- WHERE A.INPART = B.INPART
-- AND A.�b���s�{�� IS NULL
-- GROUP BY B.INPART
-- ) A,ORDE3 B,ORDDE4_�Ѿl�s�{����_����_D C
-- WHERE A.INPART = B.INPART
-- AND A.INPART = C.INPART
-- AND A.ORDSQ2 = C.ORDSQ2
-- AND A.INPART LIKE @INPART

-- ---- ���O�Ĥ@����
-- SELECT B.PRDDWNO,B.PRDSQNO,B.PRDOPNO,B.PRDOPTP,B.PRDMAMT,B.PRDVTIM,B.PRDPRIC, C.PRDNAME,D.SQTY,SOPKIND ,A.ORDFO
-- INTO #TEMP1_��z�зǻs�{
-- FROM #TEMP_��z�зǻs�{ A,STANDOP B,#SOPNAME C,STANDOPH D , STANDOP E
-- WHERE A.INDWG = B.PRDDWNO
-- AND B.PRDDWNO = E.PRDDWNO
-- AND B.PRDSQNO >= E.PRDSQNO
-- AND A.ORDFO = E.PRDOPNO
-- AND B.PRDOPNO = C.PRDOPNO AND B.PRDDWNO = D.PRDDWNO
-- AND C.ISACTIVE = 0 AND SOPKIND NOT IN ('�O��','�]�p','�䥦1','�䥦')
-- AND PRDNAME NOT IN ('lo','uld','ULD','LD')
-- AND A.ORDFO <> '��'
-- ORDER BY INDWG,B.PRDSQNO

-- ---- ��
-- INSERT INTO #TEMP1_��z�зǻs�{
-- SELECT B.PRDDWNO,B.PRDSQNO,B.PRDOPNO,B.PRDOPTP,B.PRDMAMT,B.PRDVTIM,B.PRDPRIC, C.PRDNAME,D.SQTY,SOPKIND ,A.ORDFO
-- --INTO #TEMP1_��z�зǻs�{
-- FROM #TEMP_��z�зǻs�{ A,STANDOP B,#SOPNAME C,STANDOPH D
-- WHERE A.INDWG = B.PRDDWNO
-- AND B.PRDOPNO = C.PRDOPNO AND B.PRDDWNO = D.PRDDWNO
-- AND C.ISACTIVE = 0 AND SOPKIND NOT IN ('�O��','�]�p','�䥦1','�䥦')
-- AND PRDNAME NOT IN ('lo','uld','ULD','LD')
-- AND A.ORDFO = '��'
-- ORDER BY INDWG,B.PRDSQNO

-- SELECT M.PRDDWNO ,ORDFO ,left(M.productIDs,len(M.productIDs)-1) as productIDsFinal
-- INTO #TEMP2_��z�зǻs�{
-- from
--(SELECT  distinct PRDDWNO,ORDFO,(
-- SELECT DISTINCT
-- CASE WHEN PRDMAMT > 0 THEN '��'+'('+cast(CONVERT(int, PRDMAMT/SQTY) AS NVARCHAR(30) )+')'  +'��' ELSE '' END +   --2020/09/18
-- CASE WHEN PRDOPNO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(bigint, PRDMAMT) AS NVARCHAR(30) )+')'  
-- WHEN PRDOPTP = 1 AND PRDOPNO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2), PRDVTIM/60))) + ')'
-- ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL,CONVERT(DECIMAL(12,0),PRDPRIC)/SQTY)) + ')' END
-- + '��' FROM #TEMP1_��z�зǻs�{ WHERE PRDDWNO = ord.PRDDWNO AND ORDFO = ord.ORDFO FOR XML PATH('')) as productIDs from #TEMP1_��z�зǻs�{ ord ) M
-- ORDER by M.PRDDWNO

--    UPDATE ORDDE4_�Ѿl�s�{����_D
-- SET ���з�TOTAL = C.productIDsFinal
-- FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,#TEMP2_��z�зǻs�{ C , ORDDE4_�Ѿl�s�{����_����_D D
-- WHERE A.INPART = B.INPART
-- AND A.INPART = D.INPART
-- AND A.�b���s�{�� = D.ORDSQ2
-- AND B.INDWG = C.PRDDWNO
-- AND C.ORDFO = D.ORDFO
-- AND A.INPART LIKE @INPART

-- UPDATE ORDDE4_�Ѿl�s�{����_D
-- SET ���з�TOTAL = C.productIDsFinal
-- FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,#TEMP2_��z�зǻs�{ C
-- WHERE A.INPART = B.INPART
-- AND A.�b���s�{�� IS NULL
-- AND B.INDWG = C.PRDDWNO
-- AND A.INPART LIKE @INPART

-----2025/06/17 ADD
SELECT distinct B.INDWG  
 INTO #TEMP_��z�зǻs�{
     FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_����_D C
WHERE A.INPART = B.INPART
  AND A.INPART = C.INPART
  AND B.LINE <> 'Z'

SELECT B.PRDDWNO,B.PRDSQNO,B.PRDOPNO,B.PRDOPTP,B.PRDMAMT,B.PRDVTIM,B.PRDPRIC, C.PRDNAME,D.SQTY,SOPKIND
 INTO #TEMP1_��z�зǻs�{
 FROM #TEMP_��z�зǻs�{ A,STANDOP B,#SOPNAME C,STANDOPH D
WHERE A.INDWG = B.PRDDWNO
  AND B.PRDOPNO = C.PRDOPNO AND B.PRDDWNO = D.PRDDWNO
  AND C.ISACTIVE = 0 AND SOPKIND NOT IN ('�O��','�]�p','�䥦1','�䥦')
  AND PRDNAME NOT IN ('lo','uld','ULD','LD')
 
SELECT M.PRDDWNO ,left(M.productIDs,len(M.productIDs)-1) as productIDsFinal
INTO #TEMP2_��z�зǻs�{
from
(SELECT  distinct PRDDWNO,(
SELECT DISTINCT
CASE WHEN PRDMAMT > 0 THEN '��'+'('+cast(CONVERT(int, PRDMAMT/SQTY) AS NVARCHAR(30) )+')'  +'��' ELSE '' END +   --2020/09/18
CASE WHEN PRDOPNO LIKE '%��%' THEN PRDNAME+'('+cast(CONVERT(bigint, PRDMAMT) AS NVARCHAR(30) )+')'  
WHEN PRDOPTP = 1 AND PRDOPNO NOT LIKE '%��%' THEN cast(PRDNAME AS NVARCHAR(30) )+ '(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL(12,2),CONVERT(DECIMAL(12,2), PRDVTIM/60))) + ')'
ELSE cast(PRDNAME AS NVARCHAR(30) )+'(' + CONVERT(VARCHAR(100),CONVERT(DECIMAL,CONVERT(DECIMAL(12,0),PRDPRIC)/SQTY)) + ')' END
+ '��' FROM #TEMP1_��z�зǻs�{ WHERE PRDDWNO = ord.PRDDWNO  FOR XML PATH('')) as productIDs from #TEMP1_��z�зǻs�{ ord ) M
ORDER by M.PRDDWNO


UPDATE ORDDE4_�Ѿl�s�{����_D
  SET ���з�TOTAL = C.productIDsFinal
 FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,#TEMP2_��z�зǻs�{ C
WHERE A.INPART = B.INPART
  AND B.INDWG = C.PRDDWNO


-----2025/06/17 END

--���з�TOTAL 2023/03/21 Techup

----04:37
    --EXEC dbo.����ORDE3�Ѿl�s�{ ''


--��s���`�氱�n�ɶ� 2023/04/18 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = B.�}�橵��ɶ�
    FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_���`�� B
    WHERE ORDSQ3 = 1 AND A.INPART = B.OLDPART AND A.ORDSQ2 = B.ORDSQ2 AND A.CARDNO = B.CARDNO
AND A.INPART LIKE @INPART




----�S��B�z�o�@�i�s�d 2023/03/23
UPDATE ORDDE4_�Ѿl�s�{����_D SET �b���s�{�� = 2
FROM ORDDE4_�Ѿl�s�{����_D WHERE INPART IN ('22D01168AF-1-002-001','22D01168AF-1-002-002','22D01168AF-1-002-003','22D01168AF-1-002-005','22D01168AF-1-002-006','22D01168AF-1-002-007',
'22D01168AF-1-002-009','22D01168AF-1-002-011','22D01168AF-1-002-012','22D01168AF-1-002-014','22D01168AF-1-002-016','22D01168AF-1-002-008','22D01168AF-1-002-010',
'22D01168AF-1-002-013','22D01168AF-1-002-015','22D01168AF-1-002-017','22D01168AF-1-002-018','22D01168AF-1-002-020','22D01168AF-1-002-004',
'22D01168AF-1-002-019','22D01168AF-1-002-021','22D01168AF-1-002-022','22D01168AF-1-002-023','22D01168AF-1-002-024','22D01168AF-1-002-025','22D01168AF-1-002-026','22D01168AF-1-002-027')
AND �b���s�{�� = 3

UPDATE ORDDE4_�Ѿl�s�{����_D SET �b���s�{�� = 0
FROM ORDDE4_�Ѿl�s�{����_D WHERE INPART IN('22D01168AF-1-002')
AND �b���s�{�� = 1


-- EXEC dbo.����ORDE3�Ѿl�s�{ '23G04777SL-6-001#1'

-------------------------------�B�z�W���ե߲k�����Ѿl�u��---2023/06/12--------------------------------------------------------------------------------

SELECT D.ORDCU,A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,E.INPART INPART2,A.INPART,A.INDWG,A.ORDQTY,A.ORDSNO,�U�q��s�d���h,A.SQ�Ƨ�,B.ORDFO,C.PRDNAME
,MAX(Convert(varchar(10),B.ORDDY1,111)) ORDDY1 ,�Ƶ�_PC�s�d���A
INTO #TEMP1_�ե� FROM ORDE3 A,ORDDE4  B,#SOPNAME C,ORDE1 D,ORDE2 E
WHERE A.INPART = B.ORDFNO AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
AND A.INFIN = 'N' AND A.ORDTP <> '4' AND B.ORDFCO <> 'C' AND C.ISACTIVE = 0 AND PRDNAME <> 'lo' AND B.ORDFO = C.PRDOPNO
AND B.ORDFCO = 'N' AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO AND A.ORDTP = E.ORDTP AND A.ORDNO = E.ORDNO AND A.ORDSQ = E.ORDSQ
GROUP BY D.ORDCU,A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,E.INPART,A.INPART,A.INDWG,B.ORDFO,PRDNAME,A.ORDQTY,A.ORDSNO,�U�q��s�d���h ,SQ�Ƨ�,�Ƶ�_PC�s�d���A
UNION -----�U��]�t�|�����ͪ����`��s�d�]�ݭn�P�ɲ��� ���󥼨� 2024/07/12 Techup
SELECT DISTINCT D.ORDCU,A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,E.INPART INPART2,A.INPART,A.INDWG,A.ORDQTY,A.ORDSNO,�U�q��s�d���h,A.SQ�Ƨ�,'' ORDFO,'' PRDNAME
,MAX(Convert(varchar(10),B.ORDDY1,111)) ORDDY1 ,�Ƶ�_PC�s�d���A
FROM ORDE3 A,ORDDE4  B,SOPNAME C,ORDE1 D,ORDE2 E
WHERE A.INPART = B.ORDFNO AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
AND A.ORDTP <> '4' AND B.ORDFCO <> 'C' AND C.ISACTIVE = 0 AND PRDNAME <> 'lo' AND B.ORDFO = C.PRDOPNO
AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO AND A.ORDTP = E.ORDTP AND A.ORDNO = E.ORDNO AND A.ORDSQ = E.ORDSQ
AND A.INPART IN (SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE ISNULL(PCCODE,'') <> 'Y'
---AND INPART <> OLDPART
AND SUBSTRING(CARDNO,2,1) <> 'G')
GROUP BY D.ORDCU,A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,E.INPART,A.INPART,A.INDWG,A.ORDQTY,A.ORDSNO,�U�q��s�d���h ,SQ�Ƨ�,�Ƶ�_PC�s�d���A
ORDER BY INPART--,B.ORDSQ2



--�R������AT���s�d
DELETE #TEMP1_�ե�
FROM #TEMP1_�ե� A,(SELECT distinct ORDFNO FROM ORDDE4  WHERE ORDFO = '27') B
WHERE A.INPART = B.ORDFNO

SELECT  ORDCU,A.ORDTP,A.ORDNO,A.ORDSQ,ORDSQ1,INPART2,ORDSDY,A.INPART,A.INDWG,A.ORDQTY,ORDSNO,�U�q��s�d���h,SQ�Ƨ�,MAX(Convert(varchar(10),ISNULL(ORDDY1,''),111)) ORDDY1
,�Ƶ�_PC�s�d���A
INTO #TEMP2_�ե�
FROM #TEMP1_�ե� A,ORDE2 B
WHERE A.INPART2 = B.INPART
GROUP BY  ORDCU,A.ORDTP,A.ORDNO,A.ORDSQ,ORDSQ1,INPART2,ORDSDY,A.INPART,A.INDWG,A.ORDQTY,ORDSNO,�U�q��s�d���h,SQ�Ƨ�,�Ƶ�_PC�s�d���A
ORDER BY ORDSNO--,B.ORDSQ2

----���a���s�d �~�ݭn�]
SELECT A.* INTO #TEMP3_�ե� FROM #TEMP2_�ե� A , SFC3138NET_�Ƶ����e B WHERE A.INPART = B.SFC3138NET_KEY AND SFC3138NET_KEY2 = '�Ǹ�'
UNION
SELECT * FROM #TEMP2_�ե� WHERE ISNULL(�Ƶ�_PC�s�d���A,'') LIKE '%FAI%' OR INPART LIKE '%-0-%'
UNION
SELECT * FROM #TEMP2_�ե� --�@�t���]�������n�Զi��
WHERE ORDCU IN (SELECT CUSTNO FROM CUSTOME WHERE CUSTGP IN ('NXP', 'NSPO', 'LS', 'OSE', 'CP', 'PTI', 'AUO', 'ASE', 'ASECL', 'INNOLUX', 'EOND', 'FATC'
, 'GEM', 'HSR', 'CMS', 'U-CAN', 'HIWIN', 'AMKOR', 'EZSA', 'ABOM','LASO','TICP','OES','ASEN','KNL','CISTL','CSIT','CSU','CS'))

--DROP TABLE #TEMP4_�ե�

SELECT A.*,ISNULL(B.�Ѿl�u��,0) �k���s�d�Ѿl�u��,ISNULL(C.�Ѿl�u��,0) �Ѿl�u��,ISNULL(C.�i�Τu��,0) �i�Τu��
INTO #TEMP4_�ե� FROM #TEMP3_�ե� A LEFT OUTER JOIN
(
SELECT �Ѿl�u��=

--SUM(
--ORDFM1
----(CASE WHEN REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('IL','F') THEN '1440' ELSE ORDFM1 END)
--)/60.00,

ISNULL(A.�Ѿl�u��,0),
A.INPART FROM ORDDE4_�Ѿl�s�{���� A
WHERE A.INPART IN (
SELECT distinct B.INPART
FROM ORDDE4_�Ѿl�s�{����_���� A,ORDDE4_�Ѿl�s�{���� B
WHERE A.INPART = B.INPART AND ORDFO IN
(SELECT PRDOPNO FROM #SOPNAME
WHERE PRDOPGP IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('as','AS','ASF','ASCG','WD','LSWD')))
AND ORDFCO = 'N'
GROUP BY B.INPART
UNION
SELECT distinct B.INPART -----�S��ϸ���Ӳե߲k���覡�B�z 2024/04/11 Techup
FROM ORDDE4_�Ѿl�s�{����_���� A,ORDDE4_�Ѿl�s�{���� B,ORDE3 C
WHERE A.INPART = B.INPART AND
(C.INDWG IN ('4022.635.36271','MTA91026','CHD1917A0092','4022.680.17663','4022.680.17653')
OR C.INDWG LIKE '4022.683.7349%')
AND ORDFCO = 'N'
AND B.INPART = C.INPART
GROUP BY B.INPART
UNION
SELECT distinct B.INPART -----�D���Ĥ@��PK �� QF �� CP �N��Ӳե߲k���覡�B�z 2025/03/17 Techup
FROM ORDDE4_�Ѿl�s�{����_���� A,ORDDE4_�Ѿl�s�{���� B,ORDE3 C,ORDE2 D
WHERE A.INPART = B.INPART
AND ORDFCO = 'N'
AND B.INPART = C.INPART
AND C.O2INPART = D.INPART
AND A.ORDFO IN ('47','30','69') AND A.ORDSQ2 = 1
GROUP BY B.INPART

)
--AND ORDFCO = 'N' AND �ήɶ���ORDSQ2 > 0 AND ORDSQ3 = 0
--AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') NOT IN ('IL','F')  -----2024/03/05 �S���}�F
--AND SOPKIND NOT IN ('�|��') --2024/02/23 �W�������R�m �M�|�� ���g�z���� �u�n�|��Y�i
--AND A.INPART = B.INPART
--GROUP BY A.INPART
)
B ON A.INPART = B.INPART LEFT OUTER JOIN ORDDE4_�Ѿl�s�{���� C ON A.INPART = C.INPART
--WHERE A.INPART2 = 'G2303781ML'
ORDER BY ORDSDY,INPART2,�U�q��s�d���h ,SQ�Ƨ�,ORDSNO

--EXEC  dbo.[����ORDE3�Ѿl�s�{] ''

--SELECT A.*
--INTO ##TEMP4_�ե�
--FROM #TEMP4_�ե� A,ORDDE4_�Ѿl�s�{����_���� B
--WHERE A.INPART LIKE '23Q03855SL-000%'
--AND A.INPART = B.INPART AND ORDFCO = 'N' AND �ήɶ���ORDSQ2 > 0 AND ORDSQ3 = 0
--AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') IN ('IL','F')
----DROP TABLE #TEMP5_�ե�

------��X�P���h�P�ϸ����`�Ѿl�u�� �~���|�]�����Ӳ��ͻ~�P �u���@�i�s�d���̤j�k���Ѿl�u�� 2024/02/16 Techup
------���ɭnsum�_�� ���ɦ��n�u��̤j���k���s�d�Ѿl�u�� ����Χ�̤j�� 2024/03/05 Techup
SELECT INPART2,�U�q��s�d���h,ORDSNO,MAX(ISNULL(�k���s�d�Ѿl�u��,0)) �̤j�k���s�d�Ѿl�u�� ,INDWG
INTO #TEMP4_�ե�_�U���h�ϸ��k���s�d�Ѿl�`�u��
FROM #TEMP4_�ե�
GROUP BY INPART2,ORDSNO,�U�q��s�d���h,INDWG

--SELECT * FROM #TEMP4_�ե�_�U���h�ϸ��k���s�d�Ѿl�`�u��
--WHERE INPART2 = 'Q2303855SL'


SELECT A.INPART2,A.�U�q��s�d���h,A.ORDSNO, �̤j�k���s�d�Ѿl�u�� --,�W���k���s�d=CAST('' AS VARCHAR(40))
INTO #TEMP5_�ե� FROM #TEMP4_�ե� A,
(SELECT INPART2,�U�q��s�d���h,ORDSNO,MAX(ISNULL(�̤j�k���s�d�Ѿl�u��,0)) �̤j�k���s�d�Ѿl�u��
FROM #TEMP4_�ե�_�U���h�ϸ��k���s�d�Ѿl�`�u�� GROUP BY INPART2,ORDSNO,�U�q��s�d���h)
B WHERE A.INPART2 = B.INPART2 AND A.�U�q��s�d���h = B.�U�q��s�d���h AND ISNULL(�̤j�k���s�d�Ѿl�u��,0) = ISNULL(�̤j�k���s�d�Ѿl�u��,0)
AND A.ORDSNO = B.ORDSNO
GROUP BY A.INPART2,A.�U�q��s�d���h,A.ORDSNO,�̤j�k���s�d�Ѿl�u�� ORDER BY A.INPART2,A.�U�q��s�d���h



select INPART2,�U�q��s�d���h,ORDSNO,�̤j�k���s�d�Ѿl�u��,sum(�̤j�k���s�d�Ѿl�u��) over(partition by INPART2,ORDSNO ORDER BY INPART2,�U�q��s�d���h) �W���̤j�k���s�d�Ѿl�u��
INTO #TEMP6_�ե�
from #TEMP5_�ե�


--EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT * FROM #TEMP6_�ե�

--���^�����r�H���ոˤu��
UPDATE #TEMP6_�ե� SET �W���̤j�k���s�d�Ѿl�u�� = ISNULL(�W���̤j�k���s�d�Ѿl�u��,0) - ISNULL(�̤j�k���s�d�Ѿl�u��,0)

--SELECT * FROM #TEMP6_�ե�

--SELECT A.INPART INPART2,B.INPART,B.ORDSNO,B.�U�q��s�d���h,B.SQ�Ƨ� FROM ORDE2 A,ORDE3 B
--WHERE  A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
--AND B.INPART LIKE '23G05868SL-005#%'

--SELECT A.INPART,A.ORDSNO,�Ѿl�u��,���Ĥu��,�i�Τu��,C.�W���̤j�k���s�d�Ѿl�u��,B.�U�q��s�d���h,SQ�Ƨ�
UPDATE ORDDE4_�Ѿl�s�{����_D SET �W���̤j�k���s�d�Ѿl�u�� = C.�W���̤j�k���s�d�Ѿl�u��
FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT A.INPART INPART2,B.INPART,B.ORDSNO,B.�U�q��s�d���h,B.SQ�Ƨ� FROM ORDE2 A,ORDE3 B
WHERE  A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ) B,#TEMP6_�ե� C
WHERE A.INPART = B.INPART AND A.ORDSNO = B.ORDSNO
AND B.INPART2 = C.INPART2 AND B.ORDSNO = C.ORDSNO AND B.�U�q��s�d���h = C.�U�q��s�d���h
AND C.�W���̤j�k���s�d�Ѿl�u�� > 0 AND �Ѿl�u�� > 0
--AND B.INPART2 = 'G2303781ML'
--AND A.INPART = '23G03781ML-001-004#1'
--ORDER BY B.INPART2,B.�U�q��s�d���h

--�i�Τu�� = ���Ĥu�� - �Ѿl�u��

--SELECT INPART,ORDSNO,�Ѿl�u��,�i�Τu��,�W���̤j�k���s�d�Ѿl�u��,�U���P����Ѿl�`�u�� FROM ORDDE4_�Ѿl�s�{����_D
 --   WHERE INPART = '24Q03625-000'


----�����W���֭p�����Ѿl�u��
UPDATE ORDDE4_�Ѿl�s�{����_D SET �i�Τu�� = �i�Τu�� - �W���̤j�k���s�d�Ѿl�u��
--WHERE INPART LIKE '23G03781ML%'

-------------------------------�B�z�W���ե߲k�����Ѿl�u��---2023/06/12--------------------------------------------------------------------------------

----05:03
    --EXEC dbo.����ORDE3�Ѿl�s�{ ''


-------------------------------�B�z�U���|���������ո˻s�d---2024/04/16 Techup-------------------------------------------------------------------------
------�U���|���������ո˻s�d
SELECT distinct A.INPART2,A.INPART �ո˻s�d,A.INDWG �ո˹ϸ�,A.�U�q��s�d���h,A.ORDSNO--,B.INPART �U���|�����u�s�d
INTO #�U���|���������ո˻s�d
FROM (
SELECT INPART2,A.INPART,INDWG,A.ORDSNO,�U�q��s�d���h,B.U_INPART
FROM #TEMP4_�ե� A LEFT OUTER JOIN ORDDE4_�Ѿl�s�{����_D B ON A.INPART = B.INPART
WHERE (�k���s�d�Ѿl�u�� > 0 AND ISNULL(A.�Ѿl�u��,0) > 0 ) OR
A.INPART IN (
(SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE ISNULL(PCCODE,'') <> 'Y'
AND SUBSTRING(CARDNO,2,1) <> 'G')) ------�Ϊ̩|�����ͷs�s�d���]�ݭn�X�{ 2024/07/31 Techup
GROUP BY INPART2,A.INPART,INDWG,A.ORDSNO,�U�q��s�d���h ,U_INPART) A ,
(
SELECT A.*,B.U_INPART FROM #TEMP4_�ե� A,ORDDE4_�Ѿl�s�{����_D B WHERE A.INPART = B.INPART AND  ISNULL(A.�Ѿl�u��,0) > 0
UNION ---2024/07/12 ���`��|�����ͪ��]�ݭn��� Techup
SELECT A.*,B.U_INPART FROM #TEMP4_�ե� A,ORDDE4_�Ѿl�s�{����_D B WHERE A.INPART = B.INPART
AND A.INPART IN (SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE ISNULL(PCCODE,'') <> 'Y'
---AND INPART <> OLDPART
AND SUBSTRING(CARDNO,2,1) <> 'G')
) B
WHERE A.INPART2 = B.INPART2 AND A.ORDSNO = B.ORDSNO AND A.�U�q��s�d���h+1 = B.�U�q��s�d���h AND A.INPART = B.U_INPART
ORDER BY A.INPART2,A.�U�q��s�d���h DESC,A.ORDSNO DESC

----���󥼨�
SELECT A.*,B.�b���s�{��,C.ORDFO,C.PRDNAME
    INTO #�U���|������������ո˻s�d
FROM #�U���|���������ո˻s�d A,ORDDE4_�Ѿl�s�{����_D B,ORDDE4_�Ѿl�s�{����_����_D C
WHERE A.�ո˻s�d = B.INPART AND B.INPART = C.INPART AND B.�b���s�{�� = C.ORDSQ2 AND C.ORDSQ3 = 0
AND C.ORDFO IN
(SELECT PRDOPNO FROM #SOPNAME
WHERE PRDOPGP IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('as','AS','ASF','ASCG','WD','LSWD')))
--AND INPART2 = 'B2401005ML-0'
ORDER BY INPART2,�U�q��s�d���h DESC,ORDSNO DESC,�ո˻s�d

----EXEC dbo.����ORDE3�Ѿl�s�{ ''
--select * from #�U���|���������ո˻s�d where �ո˻s�d LIKE '23G04777SL-6%'
--select * from #�U���|������������ո˻s�d where �ո˻s�d LIKE '23G04777SL-6%'
--select * from #TEMP4_�ե� where INPART LIKE '23G04777SL-6%'

UPDATE ORDDE4_�Ѿl�s�{����_D
SET ����k���s�d���A = '���󥼨�'
FROM ORDDE4_�Ѿl�s�{����_D A,#�U���|������������ո˻s�d B
WHERE A.INPART = B.�ո˻s�d

-----�J�첧�`���٨S�}�X�� �]�n��ܤ��󥼨� 2024/07/31 Techup-----------------------
--SELECT A.U_INPART,A.INPART,A.����k���s�d���A
SELECT distinct A.U_INPART
INTO #���`��|���}�߻s�d
FROM ORDDE4_�Ѿl�s�{����_D A,(
SELECT OLDPART FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE ISNULL(PCCODE,'') <> 'Y'
AND SUBSTRING(CARDNO,2,1) <> 'G') B
WHERE A.INPART = B.OLDPART AND ISNULL(A.U_INPART,'') <> ''

UPDATE ORDDE4_�Ѿl�s�{����_D
SET ����k���s�d���A = '���󥼨�'
FROM ORDDE4_�Ѿl�s�{����_D A,ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.�b���s�{�� = B.ORDSQ2 AND B.ORDSQ3 = 0
AND B.ORDFO IN (
SELECT PRDOPNO FROM #SOPNAME WHERE PRDOPGP IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('as','AS','ASF','ASCG','WD','LSWD')))
AND A.INPART IN (SELECT U_INPART FROM #���`��|���}�߻s�d) AND ISNULL(����k���s�d���A,'') = ''
-----�J�첧�`���٨S�}�X�� �]�n��ܤ��󥼨� 2024/07/31 Techup-----------------------

--select * from #�U���|���������ո˻s�d where �ո˻s�d LIKE '23G04777SL-6%'
--select * from #�U���|������������ո˻s�d where �ո˻s�d LIKE '23G04777SL-6%'
--select * from #TEMP4_�ե� where INPART LIKE '23G04777SL-6%'



------�B�z��檺���D 2024/04/17 Techup-----
----���󥼨쪺�ո˻s�d
SELECT B.INDWG,A.*
INTO #���󥼨쪺�ո˻s�d
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
WHERE ISNULL(����k���s�d���A,'') <> '' ---AND A.INPART LIKE '22L09274ML-008%'
AND A.INPART = B.INPART

----���󥼨쪺�b���ո˻s�d_���
SELECT B.INDWG,A.*
INTO #���󥼨쪺�ո˻s�d_���
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_����_D C
WHERE ---A.INPART LIKE '22L09274ML-008#%' AND
A.INPART = B.INPART
AND B.INPART = C.INPART
AND A.�b���s�{�� = C.ORDSQ2 AND C.ORDSQ3 = 0
AND C.ORDFO IN (SELECT PRDOPNO FROM #SOPNAME
WHERE PRDOPGP IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('as','AS','ASF','ASCG','WD','LSWD')))
AND ISNULL(����k���s�d���A,'') = ''

UPDATE #���󥼨쪺�ո˻s�d_���
SET ����k���s�d���A = A.����k���s�d���A
FROM #���󥼨쪺�ո˻s�d A,#���󥼨쪺�ո˻s�d_��� B
WHERE A.INDWG = B.INDWG AND A.ORDSNO = B.ORDSNO
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ ----�n�P�M�q��U���~�P�B���A 2024/07/30 Techup

UPDATE ORDDE4_�Ѿl�s�{����_D
SET ����k���s�d���A = B.����k���s�d���A
FROM ORDDE4_�Ѿl�s�{����_D A,#���󥼨쪺�ո˻s�d_��� B
WHERE A.INPART = B.INPART
------�B�z��檺���D 2024/04/17 Techup-----

-------------------------------�B�z�U���|���������ո˻s�d---2024/04/16 Techup-------------------------------------------------------------------------

----05:10
    --EXEC dbo.����ORDE3�Ѿl�s�{ ''


----------�Ȯ�����---------------------------------------------------------------------------------
--------------------�̤j������γ]�p �M �̤p��ORDSQ2 ����t���Ѽ�----2023/04/26 Techup-----------------------------------------------
--SELECT A.*
--INTO #ORDDE4_����]�p�̤j��
--FROM ORDDE4_�Ѿl�s�{����_����_D A,
--(SELECT INPART,MAX(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE ORDSQ2 IN ('-1000','-500') GROUP BY INPART) B
--WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2

--SELECT A.*
--INTO #ORDDE4_����]�p����̤p��
--FROM ORDDE4_�Ѿl�s�{����_���� A,
--(SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_���� A LEFT OUTER JOIN SOPNAME B ON A.ORDFO = B.PRDOPNO
--WHERE ORDSQ2 NOT IN ('-1000','-500') AND ORDSQ2 >= 0 AND (ISACTIVE = 0 OR ORDSQ2 = 0) GROUP BY INPART) B
--WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2

--SELECT A.PRTFM �e�@���̫���u,B.INPART,B.ORDFCO,B.ORDSQ2,B.ORDFO,B.PRDNAME,B.DLYTIME, ISNULL(B.PRTFM,GETDATE()) PRTFM

--INTO #ORDDE4_�����Ĥ@������
--FROM #ORDDE4_����]�p�̤j�� A,#ORDDE4_����]�p����̤p�� B
--WHERE A.INPART = B.INPART --AND A.INPART = '23C03042MT-000'

----���إ߼Ȧs
--SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART),* INTO #����zDLYTIME_CD FROM #ORDDE4_�����Ĥ@������ WHERE 1 = 0
--SELECT ID = CAST(0 AS INT)  , TIME1 =CAST('' AS datetime),TIME2 = CAST('' AS datetime),MM = CAST(0 AS INT)
--INTO #����zDLYTIME_NEW_CD  FROM #����zDLYTIME_CD WHERE 1 = 0


--INSERT INTO  #����zDLYTIME_CD
--SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART),* FROM #ORDDE4_�����Ĥ@������
--INSERT INTO #����zDLYTIME_NEW_CD
--SELECT ID,TIME1 = �e�@���̫���u,TIME2 = PRTFM,MM = CAST(0 AS INT) FROM #����zDLYTIME_CD

--EXEC [dbo].[�ɶ������t_�̤W�Z�ɶ�] 18,'#����zDLYTIME_NEW_CD'

--UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = (B.MM)/60.00 ---���ƪ��n�D ����]�p���᪺�Ĥ@���t���p�� 2023/04/26 Techup
----SELECT  (B.MM)/60.00,C.*
--FROM #����zDLYTIME_CD A,#����zDLYTIME_NEW_CD B,ORDDE4_�Ѿl�s�{����_����_D C
--WHERE A.ID = B.ID AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2

--DROP TABLE #����zDLYTIME_CD
--DROP TABLE #����zDLYTIME_NEW_CD
--------------------�̤j������γ]�p �M �̤p��ORDSQ2 ����t���Ѽ�----2023/04/26 Techup-----------------------------------------------

------�J��b�����Z���ե߻s�{ DLYTIME ������ 0  2023/06/16 Techup
--UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = 0
----SELECT *
--FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B
--WHERE --A.INPART = '22L09322ML-001#14' AND
--A.INPART = B.INPART --AND B.�b���s�{�� = A.ORDSQ2
--AND ORDFO IN (SELECT PRDOPNO FROM SOPNAME WHERE PRDOPGP IN (SELECT PRDOPNO FROM SOPNAME WHERE PRDNAME IN ('AS','ASF','ASCG','WD')))
--AND DLYTIME > 0
--AND A.INPART LIKE @INPART

----�o�]�n�����Ѫ��@�~�ɶ� 2023/06/20 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = ISNULL(DLYTIME,0) - (5*@DLYTIME�C��u�@�p��)
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,INVMAST C
WHERE --A.INPART = '22Q03509-000' AND
ORDFO = '�o'
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1 AND B.INDWG = C.INDWG AND C.INTYP = '5'
AND A.ORDSQ3 = 0
AND ISNULL(DLYTIME,0) > 0
AND A.INPART LIKE @INPART

------EXEC dbo.����ORDE3�Ѿl�s�{ '23M01112-0-000-F9'
------EXEC dbo.����ORDE3�Ѿl�s�{ ''
-------�禬���p�� ����禬�إߨ��禬�T�{���ɶ� �~��O�禬��DLYTIME
    SELECT  A.INPART,�إߤ��=MIN(B.CRUDAY),�T�{���=MAX(B.AMDDAY),DLYTIME = CONVERT(decimal(9,2), 0)
INTO #PURIND
FROM PURIND A,PURINM B,ORDDE4_�Ѿl�s�{����_D C
WHERE A.PUINO  = B.PUINO AND B.SCTRL <> 'X'
AND B.PURAA= '0'
AND A.INPART = C.INPART
GROUP BY A.INPART

SELECT �إߤ��,�T�{���,DLYTIME = CONVERT(decimal(9,2), 0)
INTO #PURIND_�X��
FROM #PURIND
GROUP BY �إߤ��,�T�{���

UPDATE #PURIND_�X��
SET DLYTIME = dbo.�ɶ��t_�̤W�Z�ɶ�(�إߤ��,�T�{���,@DLYTIME�C��u�@�p��)/60.00

UPDATE #PURIND
SET DLYTIME = B.DLYTIME
FROM #PURIND A,#PURIND_�X�� B
WHERE A.�إߤ�� = B.�إߤ�� AND A.�T�{��� = B.�T�{���


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = B.DLYTIME--dbo.�ɶ��t_�̤W�Z�ɶ�(�إߤ��,�T�{���,@DLYTIME�C��u�@�p��)/60.00    
FROM ORDDE4_�Ѿl�s�{����_����_D A,#PURIND B
WHERE ORDFO = '��' AND A.INPART = B.INPART

----�w���禬�������n������ lettime 2023/06/18 Techup
--SELECT ISNULL(C.INDAY,0),A.*
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = ISNULL(DLYTIME,0) - (ISNULL(C.INDAY,0)*24)
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,INVMAST C
WHERE --A.INPART = '22Q03509-000' AND
ORDFO = '��'
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDSQ1 = B.ORDSQ1 AND B.INDWG = C.INDWG AND C.INTYP = '5'
AND A.ORDSQ3 = 0
AND ISNULL(DLYTIME,0) > 0
AND A.INPART LIKE @INPART

--------EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT 'AAAA',* FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE INPART = '23Q01075-0-003-007'
--ORDER BY ORDSQ2

-----����ɶ� <0 �N���k�s
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = 0
WHERE ISNULL(DLYTIME,0) < 0
AND INPART LIKE @INPART


------ORDDY1 2023/06/16 Techup ORDDY1 �p�G���O�Ū� �h��Υ~�]�w�p�Ѽ�
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET ORDDY1 = DATEADD(DD, REPLACE(�~�]�w�p�Ѽ�,'D','')*-1,ORDDY1)
WHERE ORDFCO = 'N' AND ORDSQ2 = 0 AND ISNULL(ORDDY1,'') <> '' AND REPLACE(�~�]�w�p�Ѽ�,'D','') > 0
AND INPART LIKE @INPART

--SELECT *
------ORDDY5 2023/06/17 Techup ORDDY5 �p�G�O�Ū� �h��γ��u��� �Ӹ�
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET ORDDY5 = PRTFM
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE --INPART IN ('22Q03456-000','23Q03192-000#4') AND
ORDFO = '��' AND ISNULL(ORDDY5,'') = '' AND ISNULL(PRTFM,'') <> '' AND ORDFCO = 'Y'
AND INPART LIKE @INPART

------ORDDY5 �ƪ����� �q����+�~�]�w�p�Ѽ�
------�����ʫh���ʤ� ���o�]�a�o�]�� ���禬�a�禬�� ���J�w�a�J�w��
------�p�G�γƫ~�R �N�ι�ڮƪp��渹
------�ƪ��o�� �N����O���u
--SELECT * FROM #TEMP3
--ORDER BY ORDSQ2

------2023/08/08 �v �p����F 2023/09/21 �y�{���U
--UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME2 = B.DLYTIME2
--FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_���� B
--WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
--AND B.DLYTIME2 IS NOT NULL
--AND A.INPART LIKE @INPART

--UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME2 = GETDATE()
--FROM ORDDE4_�Ѿl�s�{����_����_D A , ORDDE4_�Ѿl�s�{���� B
--WHERE A.INPART = B.INPART
--AND A.ORDSQ2 = B.�b���s�{��
--AND B.�i�Τu�� < 0
--AND A.DLYTIME2 IS NULL
--AND A.INPART LIKE @INPART


-------�S��B�z-------------2023/09/26--Techup--
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = (dbo.�ɶ��t_�̤W�Z�ɶ�('2023-09-25 15:43:01.333' ,
CASE WHEN  ORDFCO = 'N' THEN GETDATE() ELSE PRTFM END
,10)-ORDFM1)/60.00
WHERE INPART = '22Q01147-0-000R3' AND ORDSQ2 = 4 AND ORDFCO = 'N'

UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = (dbo.�ɶ��t_�̤W�Z�ɶ�('2023-09-25 15:43:01.333' ,
CASE WHEN  ORDFCO = 'N' THEN GETDATE() ELSE PRTFM END,10)-ORDFM1)/60.00
WHERE INPART = '22Q01147-0-000R3' AND ORDSQ2 = 4


----�J��o�]�N�Nleadtime���u�����W��*60 2024/01/19 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET ORDFM1 = ISNULL(B.INDAY,0)*60
FROM ORDE3 A,INVMAST B,ORDDE4_�Ѿl�s�{����_����_D C
WHERE ISNULL(dbo.GetInvpart(A.INDWG,A.DWGREV),A.INDWG) = B.INDWG AND B.INTYP = '5'
AND A.INPART = C.INPART AND ORDSQ2 = '-121'


----�B�z�}�߲��`����O�S�����u(ORDDY4)���s�{���-�N�β��`��}��ɶ���@���������u�ɶ�--2024/05/09 Techup-----------------------------------
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,A.ORDSQ2,A.INPART,A.ORDFO,A.PRDNAME,MAX(B.CFMDATE) CFMDATE
INTO #ORDDE4_�Ѿl�s�{����_�̤j���`��}��ɶ�
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_���`�� B
WHERE --A.INPART = '24Q03139-001#2R1' AND
A.CARDNO = B.CARDNO AND ORDSQ3 > 0
GROUP BY A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,A.ORDSQ2,A.INPART,A.ORDFO,A.PRDNAME
ORDER BY A.ORDSQ2

-----�}���`��ӯ��S�����u �h���ζ}��ɶ���@���������u�ɶ� --2024/05/09 Techup-----------------------
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET ORDDY4 = B.CFMDATE,PRTFM=B.CFMDATE
--SELECT B.*,A.*
FROM ORDDE4_�Ѿl�s�{����_����_D A,#ORDDE4_�Ѿl�s�{����_�̤j���`��}��ɶ� B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1 AND A.ORDSQ2 = B.ORDSQ2
AND A.INPART = B.INPART AND ORDSQ3 = 0 AND (ISNULL(A.ORDDY4,'')  = ''OR ISNULL(A.PRTFM,'')  = '')
AND A.ORDFO = B.ORDFO AND A.ORDSQ2 > 0 AND A.ORDFCO = 'Y'
----�B�z�}�߲��`����O�S�����u(ORDDY4)���s�{���-�N�β��`��}��ɶ���@���������u�ɶ�--2024/05/09 Techup-----------------------------------

--UPDATE ORDDE4_�Ѿl�s�{����_D
 --   SET �b���s�{�� = 1    
--WHERE (INPART like '24X01008MT-0%' and �b���s�{�� IS NULL) OR  INPART = '24X01008MT-0-011'

--SELECT 'AAAAAAA',�b���s�{��,* FROM ORDDE4_�Ѿl�s�{����_D
--WHERE INPART IN ('24G04011SL-1-001','24G04011SL-1-003')

--------�b���s�{�� IS NULL �h��γ̤j��Y���᪺N 2024/05/31 Techup
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�� = B.ORDSQ2
FROM ORDDE4_�Ѿl�s�{����_D A,
(
SELECT A.INPART,MIN(A.ORDSQ2) ORDSQ2
FROM ORDDE4_�Ѿl�s�{����_����_D A LEFT OUTER JOIN
(SELECT INPART,MAX(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDFCO = 'Y' AND ORDSQ2 > 0 AND ORDSQ3 = 0 GROUP BY INPART) B
ON A.INPART = B.INPART
JOIN SOPNAME C ON A.ORDFO = C.PRDOPNO AND (C.ISACTIVE <> '1' OR C.SOPKIND = '�|��') --2025/05/08 �|��]�ǤJ Techup
WHERE A.ORDSQ2 > 0 AND A.ORDSQ3 = 0 AND A.ORDFCO = 'N'
AND A.ORDSQ2 > ISNULL(B.ORDSQ2,0)
GROUP BY A.INPART
) B,ORDE3 C
WHERE �b���s�{�� IS NULL AND �Ѿl�s�{����4 NOT LIKE '%AT%'
AND A.INPART = B.INPART AND A.INPART = C.INPART AND C.INFIN = 'N'
AND C.LINE <> 'Z'




--SELECT 'BBBBBBB',�b���s�{��,* FROM ORDDE4_�Ѿl�s�{����_D
--WHERE INPART IN ('24G04011SL-1-001','24G04011SL-1-003')

------<<<2024/08/09 �v �̾ڵo�Ƴ�M�h�Ƴ�M�w���S���b�{��>>>-----------------------------------------------------------------------------------------------------------------
-- ���X��h�Ƽƶq
SELECT ORDPN,QTY=(CASE WHEN A.INVTTP = '301' THEN INVQY1 ELSE INVQY1 *-1 END)
 INTO #INV
 FROM INVTAD A,INVTAM B
WHERE A.INVTTP = B.INVTTP
  AND A.INVTNO = B.INVTNO
AND B.SCTRL='Y'
AND A.INVTTP IN ('301','302')
AND A.ORDPN IN (SELECT DISTINCT INPART FROM ORDDE4_�Ѿl�s�{����_D)
UNION ALL
 SELECT ORDPN,QTY=CASE WHEN A.INVTTP = '303' THEN INVQY1 ELSE INVQY1 *-1 END
 FROM INVTAD A,INVTAM B
WHERE A.INVTTP = B.INVTTP
  AND A.INVTNO = B.INVTNO
AND B.SCTRL='Y'
AND A.INVTTP IN ('303','304')
AND A.ORDPN IN (SELECT DISTINCT INPART FROM ORDDE4_�Ѿl�s�{����_D)
 
-- ����Ƥ��u��
SELECT ORDPN,SUM(QTY) QTY
 INTO #INV1
 FROM #INV
GROUP BY ORDPN
-- HAVING SUM(QTY) > 0
UNION ALL
SELECT A.INVREM,SUM(A.INVQY1)
 FROM  TMPTAD1 A,TMPTAM1 B
WHERE B.SCTRL = 'Y'
  AND A.INVTTP = B.INVTTP
  AND A.INVTNO = B.INVTNO
AND A.INVREM IN (SELECT DISTINCT INPART FROM ORDDE4_�Ѿl�s�{����_D)  -- 2015/07/08 ADD
GROUP BY A.INVREM


UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�� = 0
FROM ORDDE4_�Ѿl�s�{����_D A,(SELECT * FROM #INV1 WHERE QTY <= 0) B
WHERE A.INPART = B.ORDPN

------<<<2024/08/09 �v �̾ڵo�Ƴ�M�h�Ƴ�M�w���S���b�{��>>>-----------------------------------------------------------------------------------------------------------------


------�J�첧�`��}�ߧP�_ ���(���u) �i�� QC�ߧY�B�z �B�b���s�{���٬O0���h�^�k�� �쥻���s�� 2024/07/01 Techup
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �b���s�{�� = C.ORDSQ2
FROM ORDDE4_�Ѿl�s�{����_D A,ORDDE4_�Ѿl�s�{����_���`�� B,
(SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D A,SOPNAME B WHERE ORDSQ3 = 0 AND ORDFCO = 'N'
AND A.ORDFO = B.PRDOPNO AND (B.ISACTIVE = 0 OR B.SOPKIND ='�|��') --2025/05/08 �|��]�ǤJ Techup
GROUP BY INPART) C
WHERE A.INPART = B.INPART AND �b���s�{�� = 0
AND A.INPART = C.INPART
AND REWORK IN ('���(���u)','�i��','QC�ߧY�B�z')



----<<<<-��s�k���ե߻s�{����O�_������ ��ܦA�r��̫e�� 2024/08/22 Techup------------------------------------------------------------------------------------------------------------------------

SELECT E.INPART , �Ѿl�s�{����4,�����s�{���ӧtDLYTIME_���t�]�p, �D���� =����k���s�d���A  , �Ƶ� = ISNULL(B.�Ƶ�,''),
    �Ƶ����� = ISNULL(D.CARDNO,'')+' || '+(CASE WHEN ISNULL(C.�Ƶ�,'') LIKE '%-%' THEN SUBSTRING(ISNULL(C.�Ƶ�,''),0,CHARINDEX('-',ISNULL(C.�Ƶ�,''))) ELSE ISNULL(C.�Ƶ�,'') END )
,�b���s�{�� ,ISNULL(E.�Ƶ�_PC�s�d���A,'') �Ƶ�_PC�s�d���A
INTO #ORDDE4_�Ѿl�s�{����
FROM
    ORDE3 E
JOIN
(SELECT INPART ,�����s�{���ӧtDLYTIME_���t�]�p,�Ѿl�s�{����4 =  (
CASE
WHEN ����k���s�d���A LIKE '���󥼨�' AND
(
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����AS%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASF%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASCG%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����WD%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����as%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����LSWD%')
)
THEN '�ơ�'
WHEN ����k���s�d���A = '' AND
(
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����AS%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASF%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASCG%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����WD%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����as%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����LSWD%')
)
THEN '��ok��'
ELSE ''
END) +
REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(�Ѿl�s�{����4,''),CHAR(10),''),'��',''),'',''),'��','')
,����k���s�d���A,�b���s�{��
FROM ORDDE4_�Ѿl�s�{����_D
WHERE (
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����AS%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASF%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASCG%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����WD%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����as%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����LSWD%')
)
) A ON A.INPART = E.INPART
LEFT OUTER JOIN
    (SELECT SFC3138NET_KEY,SEQ,�Ƶ� =MAX( LEFT(�Ƶ�,19)) FROM  SFC3138NET_�Ƶ����e WHERE SFC3138NET_KEY2 = '�Ƶ�_�{��' AND STATUS0 = 'NEW' GROUP BY SFC3138NET_KEY,SEQ) B
    ON A.INPART = B.SFC3138NET_KEY AND A.�b���s�{�� = B.SEQ LEFT OUTER JOIN
    (SELECT * FROM SFC3138NET_�Ƶ����e WHERE SFC3138NET_KEY2 = '�Ƶ�_PC' AND STATUS0 = 'NEW') C
    ON A.INPART = C.SFC3138NET_KEY AND A.�b���s�{�� = C.SEQ
    LEFT OUTER JOIN (SELECT OLDPART,MAX(CARDNO) CARDNO FROM ORDDE4_�Ѿl�s�{����_���`�� WHERE PCCODE <> 'Y' AND INPART <> OLDPART AND SUBSTRING(CARDNO,2,1) <> 'G' GROUP BY OLDPART) D
    ON A.INPART = D.OLDPART
WHERE (
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����AS%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASF%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����ASCG%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����as%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����WD%') OR
(�����s�{���ӧtDLYTIME_���t�]�p LIKE '%����LSWD%')
)
---- 2024/07/18 �v �S���Ƶ��~��ܯʮ�
UPDATE #ORDDE4_�Ѿl�s�{���� SET �Ѿl�s�{����4 = REPLACE(�Ѿl�s�{����4,'�ơ�','') WHERE �D���� LIKE '%���󥼨�%' AND �Ѿl�s�{����4 LIKE '%�ơ�%' AND �Ƶ� <> ''
UPDATE #ORDDE4_�Ѿl�s�{���� SET �Ѿl�s�{����4 = REPLACE(�Ѿl�s�{����4,'��ok��','') WHERE �D���� = '' AND �Ѿl�s�{����4 LIKE '%��ok��%' AND �Ƶ� <> ''
---- 2024/08/06 �Ƶ������۩���󥼨� �B�Ѿl�s�{���ӥB��ok�� �@�߫h��^�ơ� �M�L�Z�s�Q�׹L�᪺���G  Techup
UPDATE #ORDDE4_�Ѿl�s�{���� SET �Ѿl�s�{����4 = REPLACE(�Ѿl�s�{����4,'��ok��','�ơ�') WHERE ISNULL(�Ƶ�����,'') LIKE '%���󥼨�%' AND �Ѿl�s�{����4 LIKE '%��ok��%'

--SELECT A.INPART,A.�Ѿl�s�{����4,B.�Ѿl�s�{����4
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�s�{����4 = B.�Ѿl�s�{����4
FROM ORDDE4_�Ѿl�s�{����_D A,#ORDDE4_�Ѿl�s�{���� B
WHERE A.INPART = B.INPART AND A.�Ѿl�s�{����4 <> B.�Ѿl�s�{����4


----<<<<-��s�k���ե߻s�{����O�_������ ��ܦA�r��̫e�� 2024/08/22 Techup------------------------------------------------------------------------------------------------------------------------

----2022/03/23 �ε��G�@���ʾ�z(�Ѿl�s�{����)�M(�Ѿl�s�{����_����) �v----------------------------------------------------------------



--DROP TABLE ORDDE4_�Ѿl�s�{����
--SELECT * INTO ORDDE4_�Ѿl�s�{���� FROM ORDDE4_�Ѿl�s�{����_D WHERE 1 = 0

---------------------���X�Ѿl�s�{���Ƨ� 2023/10/27--Techup---------------------------------------------------------------------------------------------------------------------
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,A.ORDSQ2,A.ORDSQ3,�ήɶ���ORDSQ2
=ROW_NUMBER() OVER(PARTITION BY A.INPART ORDER BY A.INPART,A.ORDSQ2 )
,A.INPART,A.ORDFO
INTO #ORDDE4_�Ѿl�s�{����_����_�ήɶ���
FROM ORDDE4_�Ѿl�s�{����_����_D A JOIN
(SELECT INPART,ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D A LEFT OUTER JOIN #SOPNAME B
ON A.ORDFO = B.PRDOPNO WHERE ORDFCO = 'N' AND ORDSQ3 = 0
AND --B.ISACTIVE NOT IN ('1')
(B.ISACTIVE NOT IN ('1') OR B.SOPKIND = '�|��') AND B.SOPKIND <> '�O��' ----�|��]�n�ǤJ ��� 2025/05/08 Techup
) B
ON A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2  
WHERE A.ORDSQ2 > -1  AND ORDSQ3 = 0 ----2023/10/31 �ήɶ��ǥu��ORDSQ2>-1�����
--WHERE A.INPART = '23Q03073-000#1'
ORDER BY INPART,ORDSQ2



UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ήɶ���ORDSQ2 = B.�ήɶ���ORDSQ2
FROM ORDDE4_�Ѿl�s�{����_����_D A, #ORDDE4_�Ѿl�s�{����_����_�ήɶ��� B
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ2 = B.ORDSQ2
AND A.ORDSQ3 = 0
---------------------���X�Ѿl�s�{���Ƨ� 2023/10/27--Techup---------------------------------------------------------------------------------------------------------------------




---------------------�B�z�Ĥ@���OQF�B�ϸ��P�q��ϸ��ۦP�̪�ܳo�O�D�󪺲���--Techup 2023/11/23--------------------------------------------------------------------------------------------------
SELECT A.*,C.INFIN
INTO #�S�O�B�z�Ĥ@���OQF�����
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE2 B,ORDE3 C
WHERE --A.INPART = '23K01109AF-0-000' AND
SOPKIND <> '�]�p' AND ORDSQ2 > 0 AND ORDSQ3 = 0
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.INPART = C.INPART AND B.INDWG = C.INDWG
ORDER BY INPART,ORDSQ2,�ήɶ���ORDSQ2

UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = 0 FROM ORDDE4_�Ѿl�s�{����_����_D C,#�S�O�B�z�Ĥ@���OQF����� A,
(SELECT INPART,MIN(ORDSQ2) ORDSQ2 FROM #�S�O�B�z�Ĥ@���OQF����� GROUP BY INPART) B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND REPLACE(REPLACE(A.PRDNAME,'��',''),'��','') = 'QF'
AND A.INPART = C.INPART AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
AND A.ORDSQ2 = C.ORDSQ2 AND A.ORDSQ3 = C.ORDSQ3
AND C.DLYTIME > 0 AND A.INFIN = 'N'
---------------------�B�z�Ĥ@���OQF�B�ϸ��P�q��ϸ��ۦP�̪�ܳo�O�D�󪺲���--Techup 2023/11/23--------------------------------------------------------------------------------------------------


--SELECT 'AAAA',* FROM ORDDE4_�Ѿl�s�{����_����_D
 --   WHERE INPART = '22Y03344-000#5R1'
-- EXEC dbo.����ORDE3�Ѿl�s�{ '22Y03344-000#5R1'

--------------------�B�zDLYTIME_O����� �p��W����{�b���ɶ�---Tehup 2024/02/22 ------------------------------------------------------------
SELECT *
INTO #ORDDE4_�Ѿl�s�{����_����_D_�����
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE ORDSQ3 = 0 AND ORDFCO <> 'C' -----2024/06/06 ����C���� Techup

----�R�������ݭn��Z�s�{
DELETE #ORDDE4_�Ѿl�s�{����_����_D_�����
FROM #ORDDE4_�Ѿl�s�{����_����_D_����� A,#SOPNAME B
WHERE A.ORDFO = B.PRDOPNO AND
--(B.ISACTIVE = 1 OR B.SOPKIND = '�]�p')  ---�����]�p����W�� �G�R�� 2024/06/04 Techup
(B.ISACTIVE = 1 OR B.SOPKIND = '�]�p') AND B.SOPKIND <> '�|��' ----�����]�p����W�� �G�R�� 2024/06/04 Techup --�[�W�|�窺���� 2024/06/17 Techup


--EXEC dbo.����ORDE3�Ѿl�s�{ '24H00108-0-101A'
--SELECT * FROM #ORDDE4_�Ѿl�s�{����_����_D_�����
--WHERE INPART = '24H00108-0-101A'

----�إߤW�����u���
SELECT A.*,B.�b���s�{��,
LAG(PRTFM) OVER (PARTITION BY A.INPART ORDER BY A.INPART,ORDSQ2) AS �W�����u���
INTO #ORDDE4_�Ѿl�s�{����_����_D_�����_NEW
FROM #ORDDE4_�Ѿl�s�{����_����_D_����� A,ORDDE4_�Ѿl�s�{����_D B
WHERE A.INPART = B.INPART
ORDER BY A.INPART,ORDSQ2


--select * from #ORDDE4_�Ѿl�s�{����_����_D_�����_NEW WHERE INPART = '24Q03277-000R1'

SELECT * INTO #�B�zDLYTIME_O FROM #ORDDE4_�Ѿl�s�{����_����_D_�����_NEW
WHERE ISNULL(�W�����u���,'') <> '' AND SOPKIND <> '�~�s' ---2024/08/14 �~�s���p��DLYTIME Techup
ORDER BY INPART,ORDSQ2

----------------------------------------------------------------------------------------------------------
SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,ORDSQ2),�s�d�إߤ� = CAST('' AS datetime),*
INTO #����zDLYTIME_G FROM #�B�zDLYTIME_O WHERE 1 = 0
SELECT ID = CAST(0 AS INT)  , TIME1 =CAST('' AS datetime),TIME2 = CAST('' AS datetime),MM = CAST(0 AS INT)
INTO #����zDLYTIME_NEW_G FROM #����zDLYTIME_G WHERE 1 = 0


INSERT INTO #����zDLYTIME_G
SELECT ID = ROW_NUMBER() OVER (ORDER BY INPART,ORDSQ2),�W�����u���,*
FROM #�B�zDLYTIME_O

INSERT INTO #����zDLYTIME_NEW_G
SELECT ID,TIME1 = �W�����u���,TIME2 = CASE WHEN ISNULL(PRTFM,'') <> '' AND ORDFCO <> 'N' THEN PRTFM ELSE  GETDATE() END
,MM = CAST(0 AS INT) FROM #����zDLYTIME_G

--EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT * FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE INPART = '24HM03059-000'

--SELECT * FROM #����zDLYTIME_G WHERE INPART = '24HM03059-000'
--SELECT * FROM #����zDLYTIME_NEW_G WHERE ID IN (SELECT ID FROM #����zDLYTIME_G WHERE INPART = '24HM03059-000')
--SELECT 'AAAA',* FROM ORDDE4_�Ѿl�s�{����_����_D WHERE INPART = '24Q03277-000R1'
---ORDER BY ORDSQ2


EXEC [dbo].[�ɶ������t_�̤W�Z�ɶ�] @DLYTIME�C��u�@�p��,'#����zDLYTIME_NEW_G'

UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = (

B.MM-
CASE WHEN A.ORDDTP = 2 AND A.ORDSQ2 > 0 THEN REPLACE(A.�~�]�w�p�Ѽ�,'D','')*10*60
    WHEN A.ORDSQ2 > 0 THEN A.ORDFM1
ELSE 0 END --2023/09/03 �����w��ɶ� Techup
)/60.00 ,
DLYTIME_O = B.MM/60.00  ---�O�d��l���n 2024/05/03 Techup
FROM #����zDLYTIME_G A,#����zDLYTIME_NEW_G B,ORDDE4_�Ѿl�s�{����_����_D C
WHERE A.ID = B.ID AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND C.ORDSQ3 = 0

DROP TABLE #����zDLYTIME_NEW_G
DROP TABLE #����zDLYTIME_G

UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME_O = 0
WHERE DLYTIME_O < 0

--------------------�B�zDLYTIME_O����� �p��W����{�b���ɶ�---Tehup 2024/02/22 ------------------------------------------------------------

--SELECT 'BBBB',* FROM ORDDE4_�Ѿl�s�{����_����_D
 --   WHERE INPART = '22Y03344-000#5R1'

----�J��|��w�g���u�� �N�N�|���n������ 2024/06/17 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = 0
WHERE SOPKIND = '�|��' AND ORDFCO = 'Y'

---------------------�B�z�e������ �N�n���ʨ�A2����---2024/04/17 Techup-------------------------------------------------------------
---- 2024/09/27 �v ���N���Ƶ{������ʧ@���� �קK�u�����Ƶ{�M�������Ƶ{���@��

--SELECT Applier,�ثe�Ƶ{����,A.ORDSNO,A.INPART,B.INDWG,ORDSQ2,ORDFO,PRDNAME
--INTO #�ثe�bA1�����
--FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B
--WHERE �ثe�Ƶ{���� = 'A1' AND ORDFCO = 'N' AND ORDSQ3 = 0 AND �ήɶ���ORDSQ2 > 0
--AND A.INPART = B.INPART
--ORDER BY Applier


--SELECT A.*,ISNULL(B.INPART,'') ����s�d
--INTO #��z�����
--FROM #�ثe�bA1����� A
--LEFT OUTER JOIN ORDDE4_�Ѿl�s�{���� B
--ON A.INPART = B.INPART AND A.ORDSQ2 = B.�b���s�{��
--ORDER BY Applier


-------����������
--SELECT Applier,�ثe�Ƶ{����, COUNT(*) �U���x��������
--INTO #�U���x���`����
--FROM #��z�����
----WHERE Applier = 'CL08-08'
--GROUP BY Applier,�ثe�Ƶ{����
--ORDER BY Applier

-------���쯸������
--SELECT Applier,�ثe�Ƶ{����, COUNT(*) �U���x���쯸������
--INTO #�U���x���쯸������
--FROM #��z�����
--WHERE ����s�d = '' --AND Applier = 'CL08-08'
--GROUP BY Applier,�ثe�Ƶ{����
--ORDER BY Applier

------�p�G�������ƩM���쯸������ �ۦP��� ���������쯸 �N�������Ჾ��A2����
--SELECT A.*
--INTO #�ݱ��������x
--FROM #�U���x���`���� A,#�U���x���쯸������ B
--WHERE A.Applier = B.Applier AND �U���x�������� = �U���x���쯸������

----SELECT 'A'+ CAST(CAST (REPLACE(�ثe�Ƶ{����,'A','') AS INT )+1 AS varchar(10)),�ثe�Ƶ{����,*
--UPDATE ORDDE4_�Ѿl�s�{����_����_D
--SET �ثe�Ƶ{���� = 'A'+ CAST(CAST (REPLACE(�ثe�Ƶ{����,'A','') AS INT )+1 AS varchar(10))
--WHERE Applier IN (SELECT Applier FROM #�ݱ��������x)
--AND ISNULL(�ثe�Ƶ{����,'') <> ''
-----ORDER BY Applier,CAST (REPLACE(�ثe�Ƶ{����,'A','') AS INT )

--UPDATE ORDDE4_�Ѿl�s�{����_����_D
--SET �ثeA1�Ƶ{���ǫإߤ� = NULL
--where ISNULL(�ثeA1�Ƶ{���ǫإߤ�,'') <> '' and �ثe�Ƶ{���� <> 'A1'

---- 2024/09/27 �v ���N���Ƶ{������ʧ@���� �קK�u�����Ƶ{�M�������Ƶ{���@��
---------------------�B�z�e������ �N�n���ʨ�A2����---2024/04/17 Techup-------------------------------------------------------------





---------------------�Ӿ��x�`�W�e�u��< 0���̤j�ثe�Ƶ{���� 2024/04/17 Techup-------------------------------------------------------------------------------------------------------------------------------
SELECT �� = ROW_NUMBER() OVER(ORDER BY A.�ثe�Ƶ{����,A.ORDSNO,A.INPART) ,A.Applier,A.�ثe�Ƶ{����,
A.ORDSNO,B.INDWG,A.INPART,A.ORDSQ2,REPLACE(REPLACE(A.PRDNAME,'��',''),'��','') PRDNAME,A.ORDQY2,A.ORDFM1 ,
RIGHT('0' +CONVERT(VARCHAR(2),DATEPART(MONTH, StartTime)),2)+RIGHT('0' +CONVERT(VARCHAR(2),DATEPART(DAY, StartTime)),2)+ ' ' + CONVERT(VARCHAR(2),DATEPART(HOUR, StartTime)) StartTime,
RIGHT('0' +CONVERT(VARCHAR(2),DATEPART(MONTH, EndTime)),2)+RIGHT('0' +CONVERT(VARCHAR(2),DATEPART(DAY, EndTime)),2)+ ' ' + CONVERT(VARCHAR(2),DATEPART(HOUR, EndTime)) EndTime
INTO #�������G
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B ,
(SELECT Applier,MIN(StartTime) StartTime,MAX(EndTime) EndTime,INPART,ORDFO,ORDSQ2,INDWG,PRDNAME
FROM #�����ɶ� WHERE �H�ξ��x = 1 AND Remark <> '16'
GROUP BY Applier,INPART,ORDFO,ORDSQ2,INDWG,PRDNAME ) C
WHERE ISNULL(A.�ثe�Ƶ{����,'') <> ''
AND A.Applier IN (
SELECT A.MAHNO FROM MACPRD A,MACPRD1 B
WHERE A.MAHNO = B.MAHNO AND ISNULL(A.DEPT,'') <> '')
AND ORDSQ3 = 0 AND A.INPART = B.INPART
AND ISNULL(REPLACE(A.�ثe�Ƶ{����,'A',''),0) < 50
AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND A.ORDFO = C.ORDFO AND A.Applier = C.Applier
--AND A.Applier = '5XM07'
--AND A.INPART = '23Q03864SL-001#27'
ORDER BY A.Applier,ISNULL(REPLACE(A.�ثe�Ƶ{����,'A',''),0),C.StartTime

SELECT �� = MIN(��), Applier,�ثe�Ƶ{����,A.ORDSNO,INDWG,PRDNAME ,ORDQY2 = SUM(ORDQY2) ,StartTime = MIN(StartTime),EndTime = MAX(EndTime),
ORDFM1 = CONVERT(DECIMAL(10,2),SUM(ORDFM1)/60.0) ,�W�e�u�� = MIN(B.�i�Τu��),
�`�Ѿl�u�� = CONVERT(DECIMAL(8,2),SUM(ISNULL(B.�Ѿl�u��,0))),�`���Ĥu�� = CONVERT(DECIMAL(8,2),MAX(ISNULL(B.���Ĥu��,0))),
�`�W�e�u�� = CAST(0 AS INT),�Ÿ� = CAST(RANK() OVER(PARTITION BY Applier ORDER BY INDWG) AS VARCHAR(2)),
����k���s�d���A = MIN(����k���s�d���A)
INTO #�������G1
FROM #�������G A ,ORDDE4_�Ѿl�s�{����_D B
WHERE A.INPART = B.INPART
GROUP BY Applier,�ثe�Ƶ{����,INDWG,PRDNAME,A.ORDSNO


UPDATE #�������G1 SET �`�W�e�u�� = CONVERT(INT,�`���Ĥu�� - �`�Ѿl�u��)


---DROP TABLE #�������G_NEW

SELECT ROW_NUMBER() OVER (PARTITION BY Applier ORDER BY CONVERT(INT, REPLACE(�ثe�Ƶ{����,'A',''))) as SQ,*
INTO #�������G2
FROM #�������G1 WHERE �`�W�e�u�� < 0
AND CONVERT(INT, REPLACE(�ثe�Ƶ{����,'A','')) >=1

-----�����A4�᭱����
DELETE #�������G2
WHERE SQ > 5

--DROP TABLE #�������G_NEW

SELECT DISTINCT Applier,
(
SELECT CONVERT(VARCHAR(40), A.�`�W�e�u��)+','+CHAR(10) FROM #�������G2 A  WHERE A.Applier = B.Applier
FOR XML PATH('')
) AS �W�e�u�ɤp��s
INTO #�������G_NEW
FROM #�������G2 B

--SELECT * FROM #�������G_NEW

--SELECT *
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �`�W�e�u�ɳ̤j�ثe�Ƶ{���� = B.�W�e�u�ɤp��s
FROM #�������G1 A,
#�������G_NEW B,ORDDE4_�Ѿl�s�{����_����_D C
WHERE A.Applier = B.Applier AND A.Applier = C.Applier AND C.ORDSQ3 = 0 AND C.�ήɶ���ORDSQ2 > 0
--ORDER BY A.Applier,CONVERT(INT, REPLACE(A.�ثe�Ƶ{����,'A',''))
---------------------�Ӿ��x�`�W�e�u��< 0���̤j�ثe�Ƶ{���� 2024/04/17 Techup-------------------------------------------------------------------------------------------------------------------------------


-------------------------��z�W���s�{ 2024/04/22 Techup-------------------------------------------------------

-----������ܥX�W���s�{ 2024/09/03 Techup
    --SELECT *
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �W���s�{ = B.�W���s�{,�W���s�{�� = B.�W���s�{��
FROM ORDDE4_�Ѿl�s�{����_����_D A,(
SELECT ISNULL(LAG(REPLACE(REPLACE(PRDNAME,'��',''),'��','')
) OVER (PARTITION BY INPART ORDER BY ORDSQ2),'') AS �W���s�{
,ISNULL(LAG(REPLACE(REPLACE(ORDSQ2,'��',''),'��','')
) OVER (PARTITION BY INPART ORDER BY ORDSQ2),'') AS �W���s�{��
,INPART,ORDFO,PRDNAME,ORDSQ2,�ήɶ���ORDSQ2 ,ORDSQ3
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE --�ήɶ���ORDSQ2 > 0 AND
ORDSQ3 = 0
AND SOPKIND <> '�]�p' ----�]�p���� 2024/04/29 Techup
AND ORDDTP <> '4' ----�O��������W���s�{ 2024/06/28 Techup
AND PRDNAME NOT IN ('lo','uld','LD','ULD','am')
AND PRDNAME NOT LIKE 'Z%'
AND ORDFCO <> 'C' --2024/12/16 Techup ���Ϊ������
) B,ORDE3 C
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = 0
AND A.INPART = C.INPART AND C.INFIN = 'N'
---AND A.INPART = '24K01046AF-0-002-002-003'
AND A.ORDSQ2 > 0


---,NULL �U���ثe�Ƶ{����

UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �U�����[�s�{ = B.�U�����[�s�{,�U�����[�u�� = B.�U�����[�u�� ,�U���ثe�Ƶ{���� = B.�U���ثe�Ƶ{����
FROM ORDDE4_�Ѿl�s�{����_����_D A,(
SELECT ISNULL(LEAD(REPLACE(REPLACE(PRDNAME,'��',''),'��','')
) OVER (PARTITION BY INPART ORDER BY ORDSQ2),'') AS �U�����[�s�{,
ISNULL(LEAD(ORDFM1) OVER (PARTITION BY INPART ORDER BY ORDSQ2),0) AS �U�����[�u��,
ISNULL(LEAD(�ثe�Ƶ{����) OVER (PARTITION BY INPART ORDER BY ORDSQ2),0) AS �U���ثe�Ƶ{����
,INPART,ORDFO,PRDNAME,ORDSQ2,�ήɶ���ORDSQ2 ,ORDSQ3
FROM ORDDE4_�Ѿl�s�{����_����_D
WHERE --�ήɶ���ORDSQ2 > 0 AND
ORDSQ3 = 0
AND SOPKIND = '���[' ----�]�p���� 2024/04/29 Techup
AND ORDDTP <> '4' ----�O��������W���s�{ 2024/06/28 Techup
AND REPLACE(REPLACE(PRDNAME,'��',''),'��','') NOT IN ('lo','uld','LD','ULD','am','f')
AND PRDNAME NOT LIKE 'Z%'
AND ORDFCO = 'N'
) B,ORDE3 C
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = 0
AND A.INPART = C.INPART AND C.INFIN = 'N' AND A.ORDFCO = 'N'
AND A.SOPKIND = '���['
---AND A.INPART = '24K01046AF-0-002-002-003'
AND A.ORDSQ2 > 0
--AND A.ORDQY2 > 1

UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET ���[�j�a�p = '�j'
WHERE ISNULL(ORDFM1,0) > ISNULL(�U�����[�u��,0)
AND ISNULL(�U�����[�s�{,'') <> '' AND ISNULL(�U�����[�u��,0) > 0
AND ORDQY2 > 1

--,���[�j�a�p = CAST('' AS varchar(10))



--UPDATE ORDDE4_�Ѿl�s�{����_����_D
--SET �U���u�� = B.�U���u��
--FROM ORDDE4_�Ѿl�s�{����_����_D A,(
--SELECT ISNULL(LEAD(ORDFM1) OVER (PARTITION BY INPART ORDER BY ORDSQ2),0) AS �U���u��,INPART,ORDFO,PRDNAME,ORDSQ2,�ήɶ���ORDSQ2 ,ORDSQ3
--FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE --�ήɶ���ORDSQ2 > 0 AND
--ORDSQ3 = 0
--AND SOPKIND <> '�]�p' ----�]�p���� 2024/04/29 Techup
--AND ORDDTP <> '4' ----�O��������W���s�{ 2024/06/28 Techup
--AND PRDNAME NOT IN ('lo','uld','LD','ULD','am')
--AND PRDNAME NOT LIKE 'Z%'
--) B,ORDE3 C
--WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = 0
--AND A.INPART = C.INPART AND C.INFIN = 'N'
-----AND A.INPART = '24K01046AF-0-002-002-003'
--AND A.ORDSQ2 > 0


--FROM ORDDE4_�Ѿl�s�{����_����_D A,(
--SELECT ISNULL(LAG(REPLACE(REPLACE(PRDNAME,'��',''),'��','')
--) OVER (PARTITION BY INPART ORDER BY �ήɶ���ORDSQ2),'') AS �W���s�{,INPART,ORDFO,PRDNAME,ORDSQ2,�ήɶ���ORDSQ2 ,ORDSQ3
--FROM ORDDE4_�Ѿl�s�{����_����_D
--WHERE �ήɶ���ORDSQ2 > 0 AND ORDSQ3 = 0
--AND SOPKIND <> '�]�p' ----�]�p���� 2024/04/29 Techup
--AND ORDDTP <> '4' ----�O��������W���s�{ 2024/06/28 Techup
--) B
--WHERE A.INPART = B.INPART AND A.�ήɶ���ORDSQ2 = B.�ήɶ���ORDSQ2 AND A.ORDSQ3 = 0

    -------------------------��z�W���s�{ 2024/04/22 Techup-------------------------------------------------------


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET PRDNAME = 'IQC��',ORDFO = '15N'
WHERE ORDSQ2 = 1 AND PRDNAME LIKE 'QC%' AND PRDNAME LIKE '%��%'


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET PRDNAME = 'IQC',ORDFO = '15N'
WHERE ORDSQ2 = 1 AND PRDNAME LIKE 'QC%' AND PRDNAME NOT LIKE '%��%'

----�Ĥ@���OPK�L���� DLYTIME = 0 2024/05/14 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = 0
WHERE ORDSQ2 = 1 AND PRDNAME like '%PK%'  AND ORDSQ3 = 0
AND DLYTIME > 0

----�B�z�o�ƫ᪺ORDDY5 ���� ������γ��u Techup 2024/05/28
UPDATE  ORDDE4_�Ѿl�s�{����_����_D
    SET ORDDY5 = PRTFM
WHERE ORDFO = '��' AND ORDFCO = 'Y' AND ORDDY5 > PRTFM


-------�J��ȨѮƪ��Ĥ@�����n��DLYTIME 2024/06/06 Techup
--SELECT B.�ȨѮ�,A.INPART,C.ORDSQ2,D.DLYTIME
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET DLYTIME = 0
FROM ORDDE4_�Ѿl�s�{���� A,ORDE3 B,(
SELECT A.INPART,MIN(ORDSQ2) ORDSQ2 FROM ORDDE4_�Ѿl�s�{����_����_D A,SOPNAME B
WHERE ORDSQ3 = 0 AND ORDSQ2 > 0 AND A.SOPKIND <> '�]�p' AND ORDFO = B.PRDOPNO
AND B.ISACTIVE = '0'
GROUP BY A.INPART
) C,ORDDE4_�Ѿl�s�{����_����_D D
WHERE --A.INPART LIKE '24D03513AF-003%' AND
A.INPART = B.INPART AND  B.�ȨѮ� = 'Y'
AND A.INPART = C.INPART
AND C.INPART = D.INPART AND C.ORDSQ2 = D.ORDSQ2 AND D.ORDSQ3 = 0 AND D.DLYTIME > 0


-----�S��ϸ� �b��g ���ݭn������@�_�� ���p�B�z 2024/07/02 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET DLYTIME = 0
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,ORDDE4_�Ѿl�s�{����_D C
WHERE B.INDWG = '4022.635.36271' AND A.INPART = B.INPART
AND B.INFIN = 'N' AND A.INPART = C.INPART AND A.ORDSQ2 = C.�b���s�{��
AND ORDSQ3 = 0 AND REPLACE(REPLACE(REPLACE(PRDNAME,'��',''),'��',''),'��','') = 'g'

------�u���k�s 2024/04/03 Techup
UPDATE ORDDE4_�Ѿl�s�{����_D SET TOTAL�u�� = 0 WHERE TOTAL�u�� IS NULL

-----���� ���n���OS�s�{�X�� 2024/08/07 Techup
UPDATE ORDDE4_�Ѿl�s�{����_D SET �Ѿl�s�{����4 = REPLACE(REPLACE(REPLACE(�Ѿl�s�{����4,'OSW','O.SW'),'OS',''),'O.SW','OSW')
        WHERE �Ѿl�s�{����4 LIKE '%����%' AND �Ѿl�s�{����4 LIKE '%OS%'

UPDATE ORDDE4_�Ѿl�s�{����_D SET �Ѿl�s�{����4 = REPLACE(REPLACE(REPLACE(�Ѿl�s�{����4,'OSW','O.SW'),'OS',''),'O.SW','OSW')
        WHERE �Ѿl�s�{����4 LIKE '%�ʹF���~%' AND �Ѿl�s�{����4 LIKE '%OS%'

UPDATE ORDDE4_�Ѿl�s�{����_D SET �Ѿl�s�{����4 = REPLACE(�Ѿl�s�{����4,'���˩���','����')
        WHERE �Ѿl�s�{����4 LIKE '%���˩���%'


-----��X�̷s�������׵��� 2024/08/29 Techup
SELECT INDWG,DIFLVL,MAX(B.GMDATE) GMDATE
INTO #CUSTREQ3
FROM CUSTREQ3 A,CUSTREQ B
WHERE A.PNO = B.PNO AND A.PNSQ = B.PNSQ AND B.SCRL = 'Y' AND GMYN = 'Y'
AND ISNULL(INDWG,'') <> ''
AND ISNULL(INDWG,'') IN (SELECT distinct INDWG FROM ORDDE4_�Ѿl�s�{����_D)
GROUP BY INDWG,DIFLVL

SELECT A.*
INTO #CUSTREQ3_NEW
FROM #CUSTREQ3 A,(SELECT INDWG,MAX(GMDATE) GMDATE FROM #CUSTREQ3 GROUP BY INDWG) B
WHERE A.INDWG = B.INDWG AND A.GMDATE = B.GMDATE
ORDER BY A.INDWG

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �����׵��� = ISNULL(C.DIFLVL,'')
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,#CUSTREQ3_NEW C
WHERE A.INPART = B.INPART AND B.INFIN IN ('P','N') AND B.INDWG = C.INDWG
-----��X�̷s�������׵��� 2024/08/29 Techup


---- 2025/11/11 �v ����n�D���� �אּFAI�P�_
-----�s�W�����ת�CUS�P�_ 2025/06/26 �v   2025/10/13 �v �[�J�u���b��o���P�_
--UPDATE ORDDE4_�Ѿl�s�{����_D
--SET CUS�u�� = (CASE WHEN �����׵��� = 'A' THEN 300 WHEN �����׵��� = 'B' THEN 100 WHEN �����׵��� = 'C' THEN 50 ELSE 0 END)
--FROM ORDDE4_�Ѿl�s�{����_D A ,ORDE2 B,ORDE3 C,ORDE1 D
--WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
--AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ AND A.ORDSQ1 = C.ORDSQ1
--AND A.ORDTP = D.ORDTP AND A.ORDNO = D.ORDNO
--AND A.INPART LIKE @INPART
--AND ISNULL(CUS�u��,0) = 0
--AND A.INPART IN (SELECT DISTINCT A.INPART FROM ORDDE4_�Ѿl�s�{����_D A , ORDDE4_�Ѿl�s�{����_����_D B
-- WHERE A.INPART = B.INPART AND B.ORDFO IN (SELECT PRDOPNO FROM SOPNAME WHERE SOPKIND LIKE '�]�p'))
-----�s�W�����ת�CUS�P�_ 2025/06/26 �v   2025/10/13 �v �[�J�u���b��o���P�_

-----����X�Ҧ��D��������X�f�q�� ���P���������]���Ƶ��X�n��� 2024/09/04 Techup
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.ORDSQ1,A.INPART,B.INDWG,B.ORDSNO,ISNULL(B.O2INPART,C.INPART) �X�f�q��
INTO #�D�����X�f�q��
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,ORDE2 C
WHERE A.INPART = B.INPART AND B.INDWG = C.INDWG AND B.ORDTP = C.ORDTP AND B.ORDNO = C.ORDNO AND B.ORDSQ = C.ORDSQ
AND ISNULL(B.O2INPART,C.INPART) <> '�L�����q��'
AND B.INFIN IN ('N','P')

------��X�������P�ϸ��P������D�s�d���n���
SELECT A.*,B.�n��� --,B.ORDTP,B.ORDNO,B.ORDSQ
INTO #�D�����X�f�q��_NEW
FROM #�D�����X�f�q�� A,ORDE2 B
WHERE --�X�f�q�� IN ('Q2201135-4','Q2201135-7','Q2201135-9') AND
A.�X�f�q�� = B.INPART --AND ISNULL(B.�n���,'') <> ''

UPDATE #�D�����X�f�q��_NEW
SET �n��� = B.�̤p�n���
FROM #�D�����X�f�q��_NEW A,(SELECT INDWG,ORDSNO,�X�f�q��,MIN(�n���) �̤p�n��� FROM #�D�����X�f�q��_NEW GROUP BY INDWG,ORDSNO,�X�f�q��) B
WHERE A.INDWG = B.INDWG AND A.ORDSNO = B.ORDSNO AND A.�X�f�q�� = B.�X�f�q��
---AND ISNULL(�n���,'') <> ''

--SELECT B.*
UPDATE ORDDE4_�Ѿl�s�{����_D SET �n��� = A.�n���
FROM #�D�����X�f�q��_NEW A,ORDE3 B,ORDDE4_�Ѿl�s�{����_D C
WHERE ISNULL(A.�n���,'') <> ''
--AND �X�f�q�� IN ('Q2201135-4','Q2201135-7','Q2201135-9')
AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
AND A.ORDSNO = B.ORDSNO AND INFIN IN ('N','P')
AND B.INPART = C.INPART
-----����X�Ҧ��D��������X�f�q�� ���P���������]���Ƶ��X�n��� 2024/09/04 Techup

--------�J����� ��ܫe���]�OWD(5)-AS(0.2) �N�ݭn�N�����אּ������ 2024/09/09 Techup
--SELECT INPART,�Ѿl�s�{����4,AAAA= REPLACE(�Ѿl�s�{����4,'����','������')
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�s�{����4 = REPLACE(�Ѿl�s�{����4,'����','������')
WHERE �Ѿl�s�{����4 LIKE '%����%' AND �Ѿl�s�{����4 LIKE '%WD%'


---------------�B�z�եߤU���Ѿl�s�{�u��--2024/12/20 Techup-------------------------------------------------------------------
SELECT
B.ORDTP,B.ORDNO,B.ORDSQ,A.INPART,A.ORDSNO,�Ѿl�u��,���Ĥu��,�i�Τu��,U_INPART,�W���̤j�k���s�d�Ѿl�u��,����k���s�d���A,B.�U�q��s�d���h,
ISNULL(�U���P����Ѿl�`�u��,0) �U���P����Ѿl�`�u��
INTO #��z�U���Ѿl�u��
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
WHERE --A.INPART LIKE '24G01175ML-0%' AND
B.INFIN IN ('N','P')
AND A.INPART = B.INPART AND �Ѿl�u�� > 0
--AND A.INPART <> '24G01175ML-0-001-001R1'
ORDER BY �U�q��s�d���h


SELECT *
INTO #��z�U���Ѿl�u��_���
FROM #��z�U���Ѿl�u��
WHERE (ISNULL(U_INPART ,'') <> '' AND �W���̤j�k���s�d�Ѿl�u�� > 0 ) OR  �U�q��s�d���h = 0
ORDER BY �U�q��s�d���h DESC

DECLARE @counter INT = 1; -- ��l�ƭp�ƾ�

----�]10���h
WHILE @counter <= 10
BEGIN
UPDATE #��z�U���Ѿl�u��_���
SET �U���P����Ѿl�`�u�� = B.�`�Ѿl�u��
FROM #��z�U���Ѿl�u��_��� A,(SELECT ORDTP,ORDNO,ORDSQ,U_INPART,SUM(ISNULL(�Ѿl�u��,0)+ISNULL(�U���P����Ѿl�`�u��,0)) �`�Ѿl�u��,ORDSNO
FROM #��z�U���Ѿl�u��_��� GROUP BY ORDTP,ORDNO,ORDSQ,U_INPART,ORDSNO) B
WHERE A.INPART = B.U_INPART AND A.ORDSNO = B.ORDSNO AND A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ
   
SET @counter = @counter + 1; -- �p�ƾ��[1
END;

--SELECT A.*,B.�U���P����Ѿl�`�u��
UPDATE ORDDE4_�Ѿl�s�{����_D SET �U���P����Ѿl�`�u�� = B.�U���P����Ѿl�`�u��
FROM ORDDE4_�Ѿl�s�{����_D A,#��z�U���Ѿl�u��_��� B
WHERE B.�U���P����Ѿl�`�u�� > 0 AND A.INPART = B.INPART

-----�P�B��s�P�M�q��U���P���h�ϸ� 2025/01/21
SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.INPART,�Ѿl�u��,�W���̤j�k���s�d�Ѿl�u��,�U���P����Ѿl�`�u��,A.ORDSNO ,B.INDWG
INTO #ORDDE4_�Ѿl�s�{����_�U���P����Ѿl�`�u��
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
WHERE �U���P����Ѿl�`�u�� > 0 AND A.INPART = B.INPART AND A.ORDSNO = B.ORDSNO AND B.INFIN = 'N'
--AND A.INPART LIKE '24G04353SL%'

--SELECT A.ORDTP,A.ORDNO,A.ORDSQ,A.INPART,A.�Ѿl�u��,A.�W���̤j�k���s�d�Ѿl�u��,A.�U���P����Ѿl�`�u��,A.ORDSNO ,B.INDWG
--,C.�U���P����Ѿl�`�u��
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �U���P����Ѿl�`�u�� = C.�U���P����Ѿl�`�u��
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B,#ORDDE4_�Ѿl�s�{����_�U���P����Ѿl�`�u�� C
WHERE A.ORDTP = B.ORDTP AND A.ORDNO = B.ORDNO AND A.ORDSQ = B.ORDSQ AND A.ORDSQ1 = B.ORDSQ1
AND A.INPART = B.INPART
AND A.ORDTP = C.ORDTP AND A.ORDNO = C.ORDNO AND A.ORDSQ = C.ORDSQ
AND A.ORDSNO = C.ORDSNO AND B.INDWG = C.INDWG
AND A.�U���P����Ѿl�`�u�� = 0 AND B.INFIN = 'N'


---------------�B�z�եߤU���Ѿl�s�{�u��--2024/12/20 Techup-------------------------------------------------------------------
 


    ------------------�w��]�p�����]�w�Ƶ{���� 2025/01/14 Techup-----------------------
SELECT A.INPART,A.ORDSQ2,A.ORDFO,A.PRDNAME,�ثe�Ƶ{����,�i�Τu��,C.Applier
,ROW_NUMBER() OVER (PARTITION BY C.Applier  ORDER BY  C.Applier,�i�Τu��) as ROW_ID
INTO #�]�pTEMP1
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B,�����ɶ� C,ORDDE4_�Ѿl�s�{����_D D
WHERE SOPKIND = '�]�p' AND ORDFCO = 'N' AND ORDSQ3 = 0 AND A.ORDSQ2 > 0
AND A.INPART = B.INPART AND B.INFIN = 'N'
AND A.INPART = C.INPART AND A.ORDSQ2 = C.ORDSQ2 AND A.ORDFO = C.ORDFO
AND C.�H�ξ��x = '0'
AND A.INPART = D.INPART
ORDER BY C.Applier,�i�Τu��

--SELECT *,AA = 'A'+CONVERT(varchar, ROW_ID)
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET �ثe�Ƶ{���� = 'A'+CONVERT(varchar, ROW_ID) ,Applier = A.Applier
FROM #�]�pTEMP1 A,ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND B.ORDSQ3 = 0 AND A.ORDSQ2 > 0
AND A.ORDFO = B.ORDFO
------------------�w��]�p�����]�w�Ƶ{���� 2025/01/14 Techup-----------------------


------�R�������ɶ� ��η���B�z
DELETE �����ɶ�
FROM �����ɶ� A,ORDDE4 B
WHERE A.INPART = B.ORDFNO AND A.ORDSQ2 = B.ORDSQ2
AND B.ORDFCO = 'N'
AND (A.PRDNAME LIKE '%3Q%' OR A.PRDNAME LIKE '%LQ%' OR A.PRDNAME LIKE '%HM%' OR A.PRDNAME LIKE '%PM%')
AND A.PRDNAME NOT LIKE 'EPM%'

------�M�ŭ쥻�����
        UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET Applier = '',�ثe�Ƶ{���� = ''
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,ORDE3 C
WHERE A.INPART = B.INPART --AND A.ORDSQ2 = B.�b���s�{��
AND A.ORDSQ3 = 0 AND (A.PRDNAME LIKE '%3Q%' OR A.PRDNAME LIKE '%LQ%'
OR A.PRDNAME LIKE '%HM%' OR A.PRDNAME LIKE '%PM%')  
AND A.PRDNAME NOT LIKE 'EPM%'
AND A.ORDFCO = 'N'
AND A.INPART = C.INPART AND C.INFIN = 'N'
AND C.LINE <> 'Z'


-----------------------���s�]�wCMM���Ƶ{���� 2025/03/21 Techup----------------------------------------------

-----------�ª��ƪk ������ 2025/06/11 Techup-----------------------------

--SELECT  
----ROW_NUMBER() OVER (ORDER BY B.ORDSNO,B.�i�Τu��) as ROW_ID
-----ROW_NUMBER() OVER (ORDER BY B.�i�Τu��) as ROW_ID,
--C.INDWG,A.INPART, ORDFO,
--REPLACE(REPLACE(A.PRDNAME,'��',''),'��','') PRDNAME,A.ORDSQ2
--,B.ORDSNO,B.�i�Τu��,A.Applier,A.�ثe�Ƶ{����
--INTO #TEMP1_CMM�u�@_OLD
--FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,ORDE3 C
--WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�b���s�{��
--AND A.ORDSQ3 = 0 AND A.PRDNAME LIKE '%3Q%' AND A.ORDFCO = 'N'
--AND A.INPART = C.INPART AND C.INFIN = 'N'
--AND C.LINE <> 'Z'
----AND A.INPART = '24G04857ML-001#6R1'
--ORDER BY B.�i�Τu��

--SELECT '���R' �էO,�Ǹ� = CONVERT(INT,0),* INTO #TEMP1_CMM�u�@_OLD_��z FROM #TEMP1_CMM�u�@_OLD WHERE 1 = 0

--INSERT INTO #TEMP1_CMM�u�@_OLD_��z
--SELECT '���R' �էO,ROW_NUMBER() OVER (ORDER BY ORDSNO,�i�Τu��) as �Ǹ�,* FROM #TEMP1_CMM�u�@_OLD
--WHERE ORDSNO <= convert(varchar, getdate(), 111)
--ORDER BY ORDSNO,�i�Τu��

--INSERT INTO #TEMP1_CMM�u�@_OLD_��z
--SELECT '�f�R' �էO,ROW_NUMBER() OVER (ORDER BY �i�Τu�� ) as �Ǹ�,* FROM #TEMP1_CMM�u�@_OLD
--WHERE ORDSNO > convert(varchar, getdate(), 111)
--ORDER BY �i�Τu��

--SELECT �էO,INDWG,PRDNAME,MIN(�Ǹ�) �̤p�Ǹ�
--INTO #TEMP1_CMM�u�@_�o���~�̤p�Ǹ�
--FROM #TEMP1_CMM�u�@_OLD_��z
--GROUP BY INDWG,PRDNAME,�էO
--ORDER BY �էO

--------��z�f�R�Ǹ�
--UPDATE #TEMP1_CMM�u�@_�o���~�̤p�Ǹ�
--SET �̤p�Ǹ� = �̤p�Ǹ�+10000
--WHERE �էO = '�f�R'

----DROP TABLE #TEMP1_CMM�u�@_�o���~�̤p�i�Τu��_�Ǹ�

--SELECT ROW_NUMBER() OVER (ORDER BY �̤p�Ǹ�) as ROW_ID,*
--INTO #TEMP1_CMM�u�@_�o���~�̤p�i�Τu��_�Ǹ�
--FROM #TEMP1_CMM�u�@_�o���~�̤p�Ǹ�

----DROP TABLE #TEMP1_CMM�u�@

--SELECT ROW_ID,A.*
--INTO #TEMP1_CMM�u�@
--FROM #TEMP1_CMM�u�@_OLD_��z A,#TEMP1_CMM�u�@_�o���~�̤p�i�Τu��_�Ǹ� B
--WHERE A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME AND A.�էO = B.�էO
--ORDER BY �էO DESC,ROW_ID,�i�Τu��,A.INDWG

--SELECT ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ROW_ID,
--ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ��¦ROW_ID,A.MAHNO
--INTO #MACPRD_CMM���x
--FROM MACPRD1 A,MACPRD B
--WHERE SOPNO = '895' AND A.MAHNO = B.MAHNO AND A.UTILRATE > 0 AND ISNULL(B.DEPT,'') <> ''

--DECLARE @Counter_new INT
--SET @Counter_new = (SELECT COUNT(*) FROM #TEMP1_CMM�u�@)/(SELECT COUNT(*) FROM #MACPRD_CMM���x)+1

--DECLARE @�Ƶ{����  INT = 1;

---- ��p�ƾ��p��ε��� 10 �ɡA����j�餺���{���X
--WHILE (@Counter_new > 0)
--BEGIN
-- UPDATE #TEMP1_CMM�u�@
-- SET Applier = B.MAHNO,�ثe�Ƶ{���� = 'A'+CONVERT(varchar, @�Ƶ{����)
-- FROM #TEMP1_CMM�u�@ A,#MACPRD_CMM���x B
-- WHERE A.ROW_ID = B.ROW_ID

-- UPDATE #MACPRD_CMM���x
-- SET ROW_ID = ROW_ID+8

-- SET @�Ƶ{���� =  @�Ƶ{���� +1
-- -- �p�ƾ��[ 1
-- SET @Counter_new = @Counter_new - 1;
--END;
-----------�ª��ƪk ������ 2025/06/11 Techup-----------------------------

------------�s���ƪk 2025/06/11 Techup------------------------------------
SELECT  
--ROW_NUMBER() OVER (ORDER BY B.ORDSNO,B.�i�Τu��) as ROW_ID
---ROW_NUMBER() OVER (ORDER BY B.�i�Τu��) as ROW_ID,
C.INDWG,A.INPART, ORDFO,
REPLACE(REPLACE(A.PRDNAME,'��',''),'��','') PRDNAME,A.ORDSQ2
,B.ORDSNO,B.�i�Τu��,A.Applier,A.�ثe�Ƶ{����
INTO #TEMP1_CMM�u�@_OLD
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,ORDE3 C
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�b���s�{��
AND A.ORDSQ3 = 0 AND A.PRDNAME LIKE '%3Q%' AND A.ORDFCO = 'N'
AND A.INPART = C.INPART AND C.INFIN = 'N'
AND C.ORDSNO >= DATEADD(YEAR,-2,GETDATE()) ----����n�O���ѥH�e��~���~��i�Ӷ] 2025/06/19 Techup
AND C.LINE <> 'Z'
--AND A.INPART = '24G04857ML-001#6R1'
ORDER BY B.�i�Τu��

--UPDATE #TEMP1_CMM�u�@_OLD SET Applier = '',�ثe�Ƶ{���� = ''



SELECT '���R' �էO,�Ǹ� = CONVERT(INT,0),* INTO #TEMP1_CMM�u�@_OLD_��z FROM #TEMP1_CMM�u�@_OLD WHERE 1 = 0

INSERT INTO #TEMP1_CMM�u�@_OLD_��z
SELECT '���R' �էO,ROW_NUMBER() OVER (ORDER BY ORDSNO,�i�Τu��) as �Ǹ�,* FROM #TEMP1_CMM�u�@_OLD
WHERE ORDSNO <= convert(varchar, getdate(), 111)
ORDER BY ORDSNO,�i�Τu��

INSERT INTO #TEMP1_CMM�u�@_OLD_��z
SELECT '�f�R' �էO,ROW_NUMBER() OVER (ORDER BY �i�Τu�� ) as �Ǹ�,* FROM #TEMP1_CMM�u�@_OLD
WHERE ORDSNO > convert(varchar, getdate(), 111)
ORDER BY �i�Τu��

SELECT �էO,INDWG,PRDNAME,ORDSNO,MIN(�Ǹ�) �̤p�Ǹ�
INTO #TEMP1_CMM�u�@_�o���~�̤p�Ǹ�
FROM #TEMP1_CMM�u�@_OLD_��z
GROUP BY INDWG,PRDNAME,�էO,ORDSNO
ORDER BY �էO



------��z�f�R�Ǹ�
UPDATE #TEMP1_CMM�u�@_�o���~�̤p�Ǹ�
SET �̤p�Ǹ� = �̤p�Ǹ�+10000
WHERE �էO = '�f�R'

---DROP TABLE #TEMP1_CMM�u�@_�o���~�ߤ@�̤p�Ǹ�


SELECT �էO,INDWG,PRDNAME,MIN(�̤p�Ǹ�) �ߤ@�̤p�Ǹ�
INTO #TEMP1_CMM�u�@_�o���~�ߤ@�̤p�Ǹ�
FROM #TEMP1_CMM�u�@_�o���~�̤p�Ǹ�
GROUP BY �էO,INDWG,PRDNAME

SELECT ROW_NUMBER() OVER (ORDER BY �ߤ@�̤p�Ǹ�,�̤p�Ǹ�) as ROW_ID,A.*
INTO #TEMP1_CMM�u�@_�o���~�̤p�i�Τu��_�Ǹ�
FROM #TEMP1_CMM�u�@_�o���~�̤p�Ǹ� A,#TEMP1_CMM�u�@_�o���~�ߤ@�̤p�Ǹ� B
WHERE A.�էO = B.�էO AND A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME


--SELECT ROW_NUMBER() OVER (ORDER BY �̤p�Ǹ�) as ROW_ID,*
--INTO #TEMP1_CMM�u�@_�o���~�̤p�i�Τu��_�Ǹ�
--FROM #TEMP1_CMM�u�@_�o���~�̤p�Ǹ�

--SELECT '�]��o�q',* FROM #TEMP1_CMM�u�@_�o���~�̤p�i�Τu��_�Ǹ�
--WHERE INDWG = '77-110-0124310-00'

--SELECT '�]��o�q',* FROM #TEMP1_CMM�u�@_OLD_��z
--WHERE INDWG = '77-110-0124310-00'

SELECT ROW_ID,A.*
INTO #TEMP1_CMM�u�@
FROM #TEMP1_CMM�u�@_OLD_��z A,#TEMP1_CMM�u�@_�o���~�̤p�i�Τu��_�Ǹ� B
WHERE A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME AND A.�էO = B.�էO AND A.ORDSNO = B.ORDSNO
---AND A.INDWG = '77-110-0124310-00'
ORDER BY �էO DESC,ROW_ID,�i�Τu��,A.INDWG


        SELECT ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ROW_ID,
ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ��¦ROW_ID,A.MAHNO
INTO #MACPRD_CMM���x
FROM MACPRD1 A,MACPRD B
WHERE SOPNO IN ('895','84G') AND A.MAHNO = B.MAHNO AND A.UTILRATE > 0 AND ISNULL(B.DEPT,'') <> ''

SELECT B.*
INTO #�i�Ʃw�����
FROM #MACPRD_CMM���x A,WORKFIXM B
WHERE A.MAHNO = B.MAHNO

--SELECT A.*,B.MAHNO,B.MAHNO_GP
--2. ��L CMM02 / CMM04 / CMM08 / CMM09 / CMM10/ CMM11 �t�Τ��n�۰ʱƩw�b�o�Ǿ��x�A�o�ӧڤ�ʳ]�w  ---�̷өw���Ƶ{ ok
-----���w���������w�����x 2025/06/11 Techup
UPDATE #TEMP1_CMM�u�@ SET Applier = B.MAHNO
FROM #TEMP1_CMM�u�@ A , #�i�Ʃw����� B
WHERE A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME

        --3. �Ҩ���(H�u��M�u)�A3Q �۰ʱƩw�� CMM10  ---�̷өw���Ƶ{ �άO�Ҩ�u�O  ok
UPDATE #TEMP1_CMM�u�@ SET Applier = 'CMM10'
FROM #TEMP1_CMM�u�@ A,ORDE3 B
WHERE ISNULL(Applier,'') = '' AND A.INPART = B.INPART
AND B.LINE IN ('H','M')

----B3Q �T�w��CMM04 2025/06/26 Techup
UPDATE #TEMP1_CMM�u�@
SET Applier = 'CMM04'
WHERE PRDNAME LIKE '%B3Q%'

        --1. �YCMM ���w�����ϸ��s�{�A �O�_�i�H �]�W�h�A�����۰ʱƩw �~�G_CMM (CMM05/CMM06)�A�ìۦP�ϸ� �P�@���x  ---ok
        SELECT
DENSE_RANK() OVER( ORDER BY ROW_ID) AS ���~�Ǹ�ROWID,
*
INTO #TEMP1_CMM�u�@_�Ѿl���u�@
FROM #TEMP1_CMM�u�@ WHERE ISNULL(Applier,'') = ''
ORDER BY �էO DESC,ROW_ID,�i�Τu��,INDWG

---9 8 7 �o�T�x�O��i�� �ƨ�L�u�Q 2025/08/04 Techup
--UPDATE #TEMP1_CMM�u�@_�Ѿl���u�@ SET Applier = 'CMM09' WHERE ���~�Ǹ�ROWID % 5 = 0 and Applier= ''
--UPDATE #TEMP1_CMM�u�@_�Ѿl���u�@ SET Applier = 'CMM08' WHERE ���~�Ǹ�ROWID % 4 = 0 and Applier= ''
UPDATE #TEMP1_CMM�u�@_�Ѿl���u�@ SET Applier = 'CMM07' WHERE ���~�Ǹ�ROWID % 3 = 0 and Applier= ''
UPDATE #TEMP1_CMM�u�@_�Ѿl���u�@ SET Applier = 'CMM06' WHERE ���~�Ǹ�ROWID % 2 = 0 and Applier= ''
UPDATE #TEMP1_CMM�u�@_�Ѿl���u�@ SET Applier = 'CMM05' WHERE Applier = ''


UPDATE #TEMP1_CMM�u�@
SET Applier = B.Applier
FROM #TEMP1_CMM�u�@ A,#TEMP1_CMM�u�@_�Ѿl���u�@ B
WHERE A.ROW_ID = B.ROW_ID AND A.�էO = B.�էO AND A.�Ǹ� = B.�Ǹ�

--SELECT DENSE_RANK() OVER(partition by Applier ORDER BY Applier,ROW_ID) ���x����,*
--FROM #TEMP1_CMM�u�@
--WHERE INDWG = '77-110-0124310-00'
--ORDER BY �էO DESC,ROW_ID,�i�Τu��,INDWG


SELECT DENSE_RANK() OVER(partition by Applier ORDER BY Applier,ROW_ID) ���x����,*
   INTO #TEMP1_CMM�u�@_��z�U���x����
FROM #TEMP1_CMM�u�@
ORDER BY �էO DESC,ROW_ID,�i�Τu��,INDWG

UPDATE #TEMP1_CMM�u�@
SET �ثe�Ƶ{���� = 'A'+CONVERT(varchar, ���x����)
FROM #TEMP1_CMM�u�@ A,#TEMP1_CMM�u�@_��z�U���x���� B
WHERE A.�Ǹ� = B.�Ǹ� AND A.�էO = B.�էO
------------�s���ƪk 2025/06/11 Techup------------------------------------

--SELECT * FROM #TEMP1_CMM�u�@_��z�U���x����
--WHERE INDWG = '77-110-0124310-00'

--SELECT * FROM #TEMP1_CMM�u�@
--WHERE INDWG = '77-110-0124310-00'
---- EXEC dbo.����ORDE3�Ѿl�s�{ ''


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثe�Ƶ{���� = B.�ثe�Ƶ{����,Applier = B.Applier
FROM ORDDE4_�Ѿl�s�{����_����_D A,#TEMP1_CMM�u�@ B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
AND A.ORDSQ3 = 0

------�ܦ�A1���ɶ� 2025/06/10 Techup  -----����sA1�Ƶ{���ǫإߤ�
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET �ثeA1�Ƶ{���ǫإߤ� = GETDATE()
WHERE �ثe�Ƶ{���� = 'A1' AND PRDNAME LIKE '%3Q%'

----��쥻�����M�M�ܰʫ᳣�OA1 �h�O�d�쥻����� 2025/06/10 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثeA1�Ƶ{���ǫإߤ� = B.�ثeA1�Ƶ{���ǫإߤ�
FROM ORDDE4_�Ѿl�s�{����_����_D A,#�O��ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = 0
AND A.PRDNAME LIKE '%3Q%' AND A.�ثe�Ƶ{���� <> ''
AND A.�ثe�Ƶ{���� = B.�ثe�Ƶ{����
AND A.�ثe�Ƶ{���� = 'A1'
-----------------------���s�]�wCMM���Ƶ{���� 2025/03/21 Techup------------------------------------------------------------


-----------------------���s�]�wLQ���Ƶ{���� 2025/04/02 Techup------------------------------------------------------------
SELECT  
--ROW_NUMBER() OVER (ORDER BY B.ORDSNO,B.�i�Τu��) as ROW_ID
---ROW_NUMBER() OVER (ORDER BY B.�i�Τu��) as ROW_ID,
C.INDWG,A.INPART, ORDFO,
REPLACE(REPLACE(A.PRDNAME,'��',''),'��','') PRDNAME,A.ORDSQ2
,B.ORDSNO,B.�i�Τu��,A.Applier,A.�ثe�Ƶ{����
INTO #TEMP1_LQ�u�@_OLD
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,ORDE3 C
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�b���s�{��
AND A.ORDSQ3 = 0 AND A.PRDNAME LIKE '%LQ%' AND A.ORDFCO = 'N'
AND A.INPART = C.INPART AND C.INFIN = 'N'
AND C.LINE <> 'Z'
AND C.ORDSNO >= DATEADD(YEAR,-2,GETDATE()) ----����n�O���ѥH�e��~���~��i�Ӷ] 2025/06/19 Techup
--AND A.INPART = '24G04857ML-001#6R1'
ORDER BY B.�i�Τu��

SELECT '���R' �էO,�Ǹ� = CONVERT(INT,0),* INTO #TEMP1_LQ�u�@_OLD_��z FROM #TEMP1_LQ�u�@_OLD WHERE 1 = 0

INSERT INTO #TEMP1_LQ�u�@_OLD_��z
SELECT '���R' �էO,ROW_NUMBER() OVER (ORDER BY ORDSNO,�i�Τu��) as �Ǹ�,* FROM #TEMP1_LQ�u�@_OLD
WHERE ORDSNO <= convert(varchar, getdate(), 111)
ORDER BY ORDSNO,�i�Τu��

INSERT INTO #TEMP1_LQ�u�@_OLD_��z
SELECT '�f�R' �էO,ROW_NUMBER() OVER (ORDER BY �i�Τu�� ) as �Ǹ�,* FROM #TEMP1_LQ�u�@_OLD
WHERE ORDSNO > convert(varchar, getdate(), 111)
ORDER BY �i�Τu��

SELECT �էO,INDWG,PRDNAME,MIN(�Ǹ�) �̤p�Ǹ�
INTO #TEMP1_LQ�u�@_�o���~�̤p�Ǹ�
FROM #TEMP1_LQ�u�@_OLD_��z
GROUP BY INDWG,PRDNAME,�էO
ORDER BY �էO

------��z�f�R�Ǹ�
UPDATE #TEMP1_LQ�u�@_�o���~�̤p�Ǹ�
SET �̤p�Ǹ� = �̤p�Ǹ�+10000
WHERE �էO = '�f�R'

--DROP TABLE #TEMP1_LQ�u�@_�o���~�̤p�i�Τu��_�Ǹ�

SELECT ROW_NUMBER() OVER (ORDER BY �̤p�Ǹ�) as ROW_ID,*
INTO #TEMP1_LQ�u�@_�o���~�̤p�i�Τu��_�Ǹ�
FROM #TEMP1_LQ�u�@_�o���~�̤p�Ǹ�

--DROP TABLE #TEMP1_LQ�u�@

SELECT ROW_ID,A.*
INTO #TEMP1_LQ�u�@
FROM #TEMP1_LQ�u�@_OLD_��z A,#TEMP1_LQ�u�@_�o���~�̤p�i�Τu��_�Ǹ� B
WHERE A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME AND A.�էO = B.�էO
ORDER BY �էO DESC,ROW_ID,�i�Τu��,A.INDWG

SELECT ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ROW_ID,
ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ��¦ROW_ID,A.MAHNO
INTO #MACPRD_LQ���x
FROM MACPRD1 A,MACPRD B
WHERE SOPNO = '896' AND A.MAHNO = B.MAHNO AND A.UTILRATE > 0 AND ISNULL(B.DEPT,'') <> ''
AND A.MAHNO NOT IN ('AQ01','LQ04')

DECLARE @Counter_new INT
DECLARE @�Ƶ{����  INT = 1;
----�k�s
SET @Counter_new = 0
SET @�Ƶ{���� = 1

SET @Counter_new = (SELECT COUNT(*) FROM #TEMP1_LQ�u�@)/(SELECT COUNT(*) FROM #MACPRD_LQ���x)+1


-- ��p�ƾ��p��ε��� 10 �ɡA����j�餺���{���X
WHILE (@Counter_new > 0)
BEGIN
UPDATE #TEMP1_LQ�u�@
SET Applier = B.MAHNO,�ثe�Ƶ{���� = 'A'+CONVERT(varchar, @�Ƶ{����)
FROM #TEMP1_LQ�u�@ A,#MACPRD_LQ���x B
WHERE A.ROW_ID = B.ROW_ID

UPDATE #MACPRD_LQ���x
SET ROW_ID = ROW_ID+1

SET @�Ƶ{���� =  @�Ƶ{���� +1
-- �p�ƾ��[ 1
SET @Counter_new = @Counter_new - 1;
END;

UPDATE ORDDE4_�Ѿl�s�{����_����_D SET �ثe�Ƶ{���� = B.�ثe�Ƶ{����,Applier = B.Applier
--SELECT B.*,A.*
FROM ORDDE4_�Ѿl�s�{����_����_D A,#TEMP1_LQ�u�@ B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
AND A.ORDSQ3 = 0
-----------------------���s�]�wLQ���Ƶ{���� 2025/04/02 Techup------------------------------------------------------------


---------------------------���s�]�wHM���Ƶ{���� 2025/06/16 Techup------------------------------------------------------------
SELECT  
C.INDWG,A.INPART, ORDFO,
REPLACE(REPLACE(A.PRDNAME,'��',''),'��','') PRDNAME,A.ORDSQ2
,B.ORDSNO,B.�i�Τu��,CONVERT(VARCHAR(20),'') AS Applier,A.�ثe�Ƶ{���� ,���~���� = CONVERT(VARCHAR(10),'')
INTO #TEMP1_HM�u�@_OLD
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,ORDE3 C
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�b���s�{��
AND A.ORDSQ3 = 0 AND A.PRDNAME LIKE '%HM%' AND A.ORDFCO = 'N'
AND A.INPART = C.INPART AND C.INFIN = 'N'
AND C.LINE <> 'Z'
AND C.ORDSNO >= DATEADD(YEAR,-2,GETDATE()) ----����n�O���ѥH�e��~���~��i�Ӷ] 2025/06/19 Techup
---AND A.INPART = '20L03005'
ORDER BY B.�i�Τu��

SELECT '���R' �էO,�Ǹ� = CONVERT(INT,0),* INTO #TEMP1_HM�u�@_OLD_��z FROM #TEMP1_HM�u�@_OLD WHERE 1 = 0

INSERT INTO #TEMP1_HM�u�@_OLD_��z
SELECT '���R' �էO,ROW_NUMBER() OVER (ORDER BY ORDSNO,�i�Τu��) as �Ǹ�,* FROM #TEMP1_HM�u�@_OLD
WHERE ORDSNO <= convert(varchar, getdate(), 111)
ORDER BY ORDSNO,�i�Τu��

INSERT INTO #TEMP1_HM�u�@_OLD_��z
SELECT '�f�R' �էO,ROW_NUMBER() OVER (ORDER BY �i�Τu�� ) as �Ǹ�,* FROM #TEMP1_HM�u�@_OLD
WHERE ORDSNO > convert(varchar, getdate(), 111)
ORDER BY �i�Τu��

SELECT �էO,INDWG,PRDNAME,ORDSNO,MIN(�Ǹ�) �̤p�Ǹ�
INTO #TEMP1_HM�u�@_�o���~�̤p�Ǹ�
FROM #TEMP1_HM�u�@_OLD_��z
GROUP BY INDWG,PRDNAME,�էO,ORDSNO
ORDER BY �էO



------��z�f�R�Ǹ�
UPDATE #TEMP1_HM�u�@_�o���~�̤p�Ǹ�
SET �̤p�Ǹ� = �̤p�Ǹ�+10000
WHERE �էO = '�f�R'


SELECT �էO,INDWG,PRDNAME,MIN(�̤p�Ǹ�) �ߤ@�̤p�Ǹ�
INTO #TEMP1_HM�u�@_�o���~�ߤ@�̤p�Ǹ�
FROM #TEMP1_HM�u�@_�o���~�̤p�Ǹ�
GROUP BY �էO,INDWG,PRDNAME

SELECT ROW_NUMBER() OVER (ORDER BY �ߤ@�̤p�Ǹ�,�̤p�Ǹ�) as ROW_ID,A.*
INTO #TEMP1_HM�u�@_�o���~�̤p�i�Τu��_�Ǹ�
FROM #TEMP1_HM�u�@_�o���~�̤p�Ǹ� A,#TEMP1_HM�u�@_�o���~�ߤ@�̤p�Ǹ� B
WHERE A.�էO = B.�էO AND A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME

SELECT ROW_ID,A.*
INTO #TEMP1_HM�u�@
FROM #TEMP1_HM�u�@_OLD_��z A,#TEMP1_HM�u�@_�o���~�̤p�i�Τu��_�Ǹ� B
WHERE A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME AND A.�էO = B.�էO AND A.ORDSNO = B.ORDSNO
---AND A.INDWG = '77-110-0124310-00'
ORDER BY �էO DESC,ROW_ID,�i�Τu��,A.INDWG



----��~
UPDATE #TEMP1_HM�u�@ SET ���~���� = '��~'
FROM #TEMP1_HM�u�@ A,ORDE3 B,ORDE2 C,ORDE1 D
WHERE A.INPART = B.INPART AND B.ORDTP = C.ORDTP AND B.ORDNO = C.ORDNO AND B.ORDSQ = C.ORDSQ
AND C.ORDTP = D.ORDTP AND C.ORDNO = D.ORDNO AND
(D.ORDCU IN ('ASMLTNF','HMIBV','HMI','HMI-US') OR C.PRODTP = 'OLED')

UPDATE #TEMP1_HM�u�@ SET ���~���� = '��~'
FROM #TEMP1_HM�u�@ A,QLINE B
WHERE A.INDWG = B.INDWG AND ISNULL(���~����,'') = ''

----���
UPDATE #TEMP1_HM�u�@
SET ���~���� = '���'
FROM #TEMP1_HM�u�@ A,ORDE3 B,ORDE1 C,CUSTOME D
WHERE A.INPART = B.INPART AND B.ORDTP = C.ORDTP AND B.ORDNO = C.ORDNO  
AND C.ORDCU = D.CUSTNO AND TRADE = '���'

----HM
UPDATE #TEMP1_HM�u�@ SET ���~���� = 'HM'
WHERE ISNULL(���~����,'') = ''

        SELECT
DENSE_RANK() OVER (ORDER BY ��ׯŧO DESC) as �էOROW_ID,
ROW_NUMBER() OVER (partition by ��ׯŧO ORDER BY ��ׯŧO DESC,A.MAHNO) as ROW_ID,
ROW_NUMBER() OVER (ORDER BY ��ׯŧO DESC,A.MAHNO) as ��¦ROW_ID,B.MAHNO, ��ׯŧO
INTO #MACPRD_HM���x
FROM MACPRD1 A,MACPRD B,PERSON C
WHERE SOPNO = '833' AND A.MAHNO = B.MAHNO AND A.UTILRATE > 0 --AND ISNULL(B.DEPT,'') <> ''
AND �H�� = '�H'
AND A.MAHNO = C.PNAME AND C.SCRL = 'N'  -----��¾���� 2025/08/12 Techup
ORDER BY ��ׯŧO DESC,B.MANAM


---------------------�B�z��~ --------------------------------------------------------------
SELECT DENSE_RANK() OVER (ORDER BY ROW_ID,ROW_ID) as ���~�Ǹ�ROWID,*
INTO #TEMP1_HM�u�@_��~
FROM #TEMP1_HM�u�@ WHERE ���~���� = '��~'

UPDATE #TEMP1_HM�u�@_��~ SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_��~ A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = '��~'
AND B.ROW_ID = '4' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_��~ SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_��~ A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = '��~'
AND B.ROW_ID = '3' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_��~ SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_��~ A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = '��~'
AND B.ROW_ID = '2' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_��~ SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_��~ A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = '��~'
AND B.ROW_ID = '1' AND ISNULL(Applier,'') = ''
----��s�B�z��~
UPDATE #TEMP1_HM�u�@ SET Applier = B.Applier
FROM #TEMP1_HM�u�@ A,#TEMP1_HM�u�@_��~ B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
---------------------�B�z��~ --------------------------------------------------------------


---------------------�B�z��� --------------------------------------------------------------
        SELECT DENSE_RANK() OVER (ORDER BY ROW_ID,ROW_ID) as ���~�Ǹ�ROWID,*
INTO #TEMP1_HM�u�@_���
FROM #TEMP1_HM�u�@ WHERE ���~���� = '���'

UPDATE #TEMP1_HM�u�@_��� SET Applier = B.MAHNO
FROM #TEMP1_HM�u�@_��� A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = '���'
AND B.ROW_ID = '2' AND ISNULL(Applier,'') = ''

UPDATE #TEMP1_HM�u�@_��� SET Applier = B.MAHNO
FROM #TEMP1_HM�u�@_��� A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = '���'
AND B.ROW_ID = '1' AND ISNULL(Applier,'') = ''

UPDATE #TEMP1_HM�u�@ SET Applier = B.Applier
FROM #TEMP1_HM�u�@ A,#TEMP1_HM�u�@_��� B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
---------------------�B�z��� --------------------------------------------------------------

---------------------�B�zHM --------------------------------------------------------------
SELECT DENSE_RANK() OVER (ORDER BY ROW_ID,ROW_ID) as ���~�Ǹ�ROWID,*
INTO #TEMP1_HM�u�@_HM
FROM #TEMP1_HM�u�@ WHERE ���~���� = 'HM'

UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '8' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '7' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '6' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '5' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '4' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '3' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '2' AND ISNULL(Applier,'') = ''
UPDATE #TEMP1_HM�u�@_HM SET Applier = B.MAHNO FROM #TEMP1_HM�u�@_HM A,#MACPRD_HM���x B
WHERE ���~�Ǹ�ROWID % B.ROW_ID = 0 AND ��ׯŧO = ���~���� AND ��ׯŧO = 'HM'
AND B.ROW_ID = '1' AND ISNULL(Applier,'') = ''

UPDATE #TEMP1_HM�u�@ SET Applier = B.Applier
FROM #TEMP1_HM�u�@ A,#TEMP1_HM�u�@_HM B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
---------------------�B�zHM --------------------------------------------------------------



        -----------------------���s�]�wPM���Ƶ{���� 2025/08/15 Techup------------------------------------------------------------
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثe�Ƶ{���� = '',Applier = ''
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,ORDE3 C
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�b���s�{��
AND A.ORDSQ3 = 0 AND A.PRDNAME LIKE '%PM%' AND A.ORDFCO = 'N'
AND A.PRDNAME NOT LIKE 'EPM%'
AND A.INPART = C.INPART AND C.INFIN = 'N'
AND C.LINE <> 'Z'


SELECT  
C.INDWG,A.INPART, ORDFO,
REPLACE(REPLACE(A.PRDNAME,'��',''),'��','') PRDNAME,A.ORDSQ2
,B.ORDSNO,B.�i�Τu��,A.Applier,A.�ثe�Ƶ{����
INTO #TEMP1_PM�u�@_OLD
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDDE4_�Ѿl�s�{����_D B,ORDE3 C
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.�b���s�{��
AND A.ORDSQ3 = 0 AND A.PRDNAME LIKE '%PM%' AND A.ORDFCO = 'N'
AND A.PRDNAME NOT LIKE 'EPM%'
AND A.INPART = C.INPART AND C.INFIN = 'N'
AND C.LINE <> 'Z'
AND C.ORDSNO >= DATEADD(YEAR,-2,GETDATE()) ----����n�O���ѥH�e��~���~��i�Ӷ] 2025/06/19 Techup
--AND A.INPART = '24G04857ML-001#6R1'
ORDER BY B.�i�Τu��

SELECT '���R' �էO,�Ǹ� = CONVERT(INT,0),* INTO #TEMP1_PM�u�@_OLD_��z FROM #TEMP1_PM�u�@_OLD WHERE 1 = 0

INSERT INTO #TEMP1_PM�u�@_OLD_��z
SELECT '���R' �էO,ROW_NUMBER() OVER (ORDER BY ORDSNO,�i�Τu��) as �Ǹ�,* FROM #TEMP1_PM�u�@_OLD
WHERE ORDSNO <= convert(varchar, getdate(), 111)
ORDER BY ORDSNO,�i�Τu��

INSERT INTO #TEMP1_PM�u�@_OLD_��z
SELECT '�f�R' �էO,ROW_NUMBER() OVER (ORDER BY �i�Τu�� ) as �Ǹ�,* FROM #TEMP1_PM�u�@_OLD
WHERE ORDSNO > convert(varchar, getdate(), 111)
ORDER BY �i�Τu��

SELECT �էO,INDWG,PRDNAME,MIN(�Ǹ�) �̤p�Ǹ�
INTO #TEMP1_PM�u�@_�o���~�̤p�Ǹ�
FROM #TEMP1_PM�u�@_OLD_��z
GROUP BY INDWG,PRDNAME,�էO
ORDER BY �էO

------��z�f�R�Ǹ�
UPDATE #TEMP1_PM�u�@_�o���~�̤p�Ǹ�
SET �̤p�Ǹ� = �̤p�Ǹ�+10000
WHERE �էO = '�f�R'

--DROP TABLE #TEMP1_PM�u�@_�o���~�̤p�i�Τu��_�Ǹ�

SELECT ROW_NUMBER() OVER (ORDER BY �̤p�Ǹ�) as ROW_ID,*
INTO #TEMP1_PM�u�@_�o���~�̤p�i�Τu��_�Ǹ�
FROM #TEMP1_PM�u�@_�o���~�̤p�Ǹ�

--DROP TABLE #TEMP1_PM�u�@

SELECT ROW_ID,A.*
INTO #TEMP1_PM�u�@
FROM #TEMP1_PM�u�@_OLD_��z A,#TEMP1_PM�u�@_�o���~�̤p�i�Τu��_�Ǹ� B
WHERE A.INDWG = B.INDWG AND A.PRDNAME = B.PRDNAME AND A.�էO = B.�էO
ORDER BY �էO DESC,ROW_ID,�i�Τu��,A.INDWG



SELECT ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ROW_ID,
ROW_NUMBER() OVER (ORDER BY A.MAHNO) as ��¦ROW_ID
,CONVERT(INT,0) AS �ثe����
,A.MAHNO
INTO #MACPRD_PM���x
FROM MACPRD1 A,MACPRD B
WHERE SOPNO = '830' AND A.MAHNO = B.MAHNO AND A.UTILRATE > 0 AND ISNULL(B.DEPT,'') <> ''

DECLARE @Counter_new_PM INT
DECLARE @�`����  INT ;
DECLARE @���x��  INT ;
DECLARE @�Ƶ{����_PM  INT = 1;
----�k�s
SET @Counter_new_PM = 0
SET @�Ƶ{����_PM = 1
SET @���x�� = (SELECT COUNT(*) FROM #MACPRD_PM���x)
SET @Counter_new_PM = (SELECT COUNT(*) FROM #TEMP1_PM�u�@)/(SELECT COUNT(*) FROM #MACPRD_PM���x)+1

--SET @Counter_new = 5
-- ��p�ƾ��p��ε��� 10 �ɡA����j�餺���{���X
WHILE (@Counter_new_PM > 0)
BEGIN
UPDATE #TEMP1_PM�u�@
SET Applier = B.MAHNO,�ثe�Ƶ{���� = 'A'+CONVERT(varchar, @�Ƶ{����_PM)
FROM #TEMP1_PM�u�@ A,#MACPRD_PM���x B
WHERE A.ROW_ID = B.ROW_ID


UPDATE #MACPRD_PM���x
SET ROW_ID = ROW_ID+@���x��

SET @�Ƶ{����_PM =  @�Ƶ{����_PM +1
SET @�`���� = @�`���� -1
-- �p�ƾ��[ 1
SET @Counter_new_PM = @Counter_new_PM - 1;
END;

UPDATE ORDDE4_�Ѿl�s�{����_����_D SET �ثe�Ƶ{���� = B.�ثe�Ƶ{����,Applier = B.Applier
FROM ORDDE4_�Ѿl�s�{����_����_D A,#TEMP1_PM�u�@ B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
AND A.ORDSQ3 = 0
-----------------------���s�]�wPM���Ƶ{���� 2025/08/15 Techup------------------------------------------------------------




SELECT DENSE_RANK() OVER(partition by Applier ORDER BY Applier,ROW_ID) ���x����,*
   INTO #TEMP1_HM�u�@_��z�U���x����
FROM #TEMP1_HM�u�@
ORDER BY �էO DESC,ROW_ID,�i�Τu��,INDWG

UPDATE #TEMP1_HM�u�@
SET �ثe�Ƶ{���� = 'A'+CONVERT(varchar, ���x����)
FROM #TEMP1_HM�u�@ A,#TEMP1_HM�u�@_��z�U���x���� B
WHERE A.�Ǹ� = B.�Ǹ� AND A.�էO = B.�էO
--------------�s���ƪk 2025/06/11 Techup------------------------------------


UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثe�Ƶ{���� = B.�ثe�Ƶ{����,Applier = B.Applier
FROM ORDDE4_�Ѿl�s�{����_����_D A,#TEMP1_HM�u�@ B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2
AND A.ORDSQ3 = 0

------�ܦ�A1���ɶ� 2025/06/10 Techup  -----����sA1�Ƶ{���ǫإߤ�
UPDATE ORDDE4_�Ѿl�s�{����_����_D SET �ثeA1�Ƶ{���ǫإߤ� = GETDATE()
WHERE �ثe�Ƶ{���� = 'A1' AND PRDNAME LIKE '%HM%'

----��쥻�����M�M�ܰʫ᳣�OA1 �h�O�d�쥻����� 2025/06/10 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET �ثeA1�Ƶ{���ǫإߤ� = B.�ثeA1�Ƶ{���ǫإߤ�
FROM ORDDE4_�Ѿl�s�{����_����_D A,#�O��ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2 AND A.ORDSQ3 = 0
AND A.PRDNAME LIKE '%HM%' AND A.�ثe�Ƶ{���� <> ''
AND A.�ثe�Ƶ{���� = B.�ثe�Ƶ{����
AND A.�ثe�Ƶ{���� = 'A1'
---------------------------���s�]�wHM���Ƶ{���� 2025/06/16 Techup------------------------------------------------------------




-- -- EXEC dbo.����ORDE3�Ѿl�s�{ ''
--SELECT '24Q05218-000#9',�b���s�{��,INPART FROM ORDDE4_�Ѿl�s�{����_D
--WHERE INPART = '24Q05218-000#9'


BEGIN TRY
       BEGIN TRANSACTION;

  ------�s�dC�� �ƶO�s�{�]�N�nC��  2024/10/22 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����_D
SET ORDFCO = 'C'
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B
WHERE PRDNAME LIKE '%��%' AND ORDFCO = 'N'
AND A.INPART = B.INPART AND B.INFIN = 'C'

------���ƪ��n�D��CUS�u�ɴN�b�̫�@�����JCUS�s�{ 2025/05/29---------------------------
UPDATE ORDDE4_�Ѿl�s�{����_D
SET �Ѿl�s�{����4 = �Ѿl�s�{����4+ '��CUS('+  CONVERT(varchar(10),[dbo].[�p�Ʀ�ƶi�쬰���](CUS�u��))+')'
FROM ORDDE4_�Ѿl�s�{����_D A,ORDE3 B
WHERE CUS�u�� > 0 AND A.INPART = B.INPART AND B.INFIN IN ('N','C')
AND A.INPART NOT IN (SELECT INPART FROM ORDDE4_�Ѿl�s�{����_����_D WHERE ORDFO = '27' GROUP BY INPART)
------���ƪ��n�D��CUS�u�ɴN�b�̫�@�����JCUS�s�{ 2025/05/29---------------------------



------�J��~�G�s��s�{ �����N�^��e��---2025/10/21 Techup---------------------------------------------------
SELECT PRDOPNO, PRDNAME
INTO #SOPNAME_�s��~�G�s�{
FROM SOPNAME AS A
WHERE EXISTS (
SELECT 1
FROM SOPNAME AS B
WHERE A.PRDOPGP = B.PRDOPGP
 AND B.PRDNAME IN ('3Q','GI','HM','CD','SQ','PM'))
ORDER BY PRDOPNO

SELECT A.INPART,MAX(ORDSQ2) �̤j�~�G�w���uORDSQ2
INTO #ORDDE4_�Ѿl�s�{����_����_�̤j�~�G�w���uORDSQ2
FROM ORDDE4_�Ѿl�s�{����_����_D A,ORDE3 B
WHERE ORDFO IN (SELECT PRDOPNO FROM #SOPNAME_�s��~�G�s�{)
AND ORDSQ3 = 0 AND A.INPART = B.INPART AND B.INFIN = 'N' AND ORDFCO = 'Y'
GROUP BY A.INPART

SELECT A.INPART �s�d,�̤j�~�G�w���uORDSQ2, B.PRDNAME �᭱���u�s�{
INTO #�B�z�~�G�s��TEMP1
FROM #ORDDE4_�Ѿl�s�{����_����_�̤j�~�G�w���uORDSQ2 A,ORDDE4_�Ѿl�s�{����_����_D B
WHERE A.INPART = B.INPART AND A.�̤j�~�G�w���uORDSQ2 = B.ORDSQ2 AND B.ORDSQ3 = 0

--SELECT A.*,B.ORDSQ2,ORDFCO,B.PRDNAME ,C.�b���s�{��
UPDATE ORDDE4_�Ѿl�s�{����_D SET �b���s�{�� = ORDSQ2
FROM #�B�z�~�G�s��TEMP1 A,ORDDE4_�Ѿl�s�{����_����_D B,ORDDE4_�Ѿl�s�{����_D C
WHERE A.�s�d = B.INPART AND A.�̤j�~�G�w���uORDSQ2 -1 = B.ORDSQ2 AND B.ORDSQ3 = 0
AND B.ORDFCO IN ('D','N') AND B.ORDFO IN (SELECT PRDOPNO FROM #SOPNAME_�s��~�G�s�{)
AND B.INPART = C.INPART AND ISNULL(C.�b���s�{��,'') <> ''
---ORDER BY A.�s�d,ORDSQ2
------�J��~�G�s��s�{---2025/10/21 Techup---------------------------------------------------

------�B�z�q���� 2025/11/13 Techup---------------------------------------------------------------
SELECT �Ȥ� = D.ORDCU,�q�� = C.INPART,�ϸ� = C.INDWG,���X�f�� = C.ORDSDY,�q���=C.ORDQTY,
�s�d = A.INPART ,�s�d�� = B.ORDQTY,PCDATE = A.ORDSNO, ��� = C.ORDPR2
INTO #�U�s�d�q����
FROM ORDDE4_�Ѿl�s�{����_D A, ORDE3 B , ORDE2 C , ORDE1 D  
WHERE A.INPART = B.INPART
AND B.ORDTP = C.ORDTP AND B.ORDNO = C.ORDNO AND B.ORDSQ = C.ORDSQ --AND B.ORDSQ1 = C.ORDSQ1
--AND B.O2INPART = C.INPART
AND C.ORDTP = D.ORDTP
AND C.ORDNO = D.ORDNO AND B.INFIN <> 'C'
ORDER BY D.ORDCU,C.ORDSDY,�q��,�s�d

UPDATE ORDDE4_�Ѿl�s�{����_D
SET �q���� = B.���
from ORDDE4_�Ѿl�s�{����_D A,#�U�s�d�q���� B
WHERE A.INPART = B.�s�d
------�B�z�q���� 2025/11/13 Techup---------------------------------------------------------------




-----202/02/26 Techup
-----�M������------
DROP INDEX [INPART] ON [dbo].[ORDDE4_�Ѿl�s�{����_����]
ALTER TABLE [dbo].[ORDDE4_�Ѿl�s�{����_����] DROP CONSTRAINT [PK_ORDDE4_�Ѿl�s�{����_����] WITH ( ONLINE = OFF )

DROP INDEX [INPART] ON [dbo].[ORDDE4_�Ѿl�s�{����]
ALTER TABLE [dbo].[ORDDE4_�Ѿl�s�{����] DROP CONSTRAINT [PK_ORDDE4_�Ѿl�s�{����] WITH ( ONLINE = OFF )
-----�M������------


-------2024/09/24 ��X�쥿��table Techup
IF((SELECT COUNT(*) FROM ORDDE4_�Ѿl�s�{����_D) > 0)
BEGIN
DELETE ORDDE4_�Ѿl�s�{����
INSERT INTO ORDDE4_�Ѿl�s�{���� SELECT * FROM ORDDE4_�Ѿl�s�{����_D
END

-------2024/09/24 ��X�쥿��table Techup
IF((SELECT COUNT(*) FROM ORDDE4_�Ѿl�s�{����_����_D) > 0)
BEGIN
DELETE  ORDDE4_�Ѿl�s�{����_����
INSERT INTO ORDDE4_�Ѿl�s�{����_���� SELECT GETDATE() �إߤ�,* FROM ORDDE4_�Ѿl�s�{����_����_D
END

----���s�إ߯���-------- 2025/02/26 Techup
SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [INPART] ON [dbo].[ORDDE4_�Ѿl�s�{����]
(
[INPART] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON


ALTER TABLE [dbo].[ORDDE4_�Ѿl�s�{����] ADD  CONSTRAINT [PK_ORDDE4_�Ѿl�s�{����] PRIMARY KEY CLUSTERED
(
[ORDTP] ASC,
[ORDNO] ASC,
[ORDSQ] ASC,
[ORDSQ1] ASC,
[INPART] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]


SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [INPART] ON [dbo].[ORDDE4_�Ѿl�s�{����_����]
(
[INPART] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]

SET ANSI_PADDING ON


/****** Object:  Index [PK_ORDDE4_�Ѿl�s�{����_����_D]    Script Date: 2025/02/26 �W�� 08:51:59 ******/
ALTER TABLE [dbo].[ORDDE4_�Ѿl�s�{����_����] ADD  CONSTRAINT [PK_ORDDE4_�Ѿl�s�{����_����] PRIMARY KEY CLUSTERED
(
[ORDTP] ASC,
[ORDNO] ASC,
[ORDSQ] ASC,
[ORDSQ1] ASC,
[ORDSQ2] ASC,
[ORDSQ3] ASC,
[INPART] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]

----���s�إ߯���-------- 2025/02/26 Techup



        COMMIT TRANSACTION;
SELECT MSG= '�@�~����!!'
    END TRY

    BEGIN CATCH
DECLARE @ErrorMessage NVARCHAR(4000);  
DECLARE @ErrorSeverity INT;  
DECLARE @ErrorState INT;  
 
SET @ErrorMessage = ERROR_MESSAGE();  
SET @ErrorSeverity = ERROR_SEVERITY();  
SET @ErrorState = ERROR_STATE();  
 
-- Use RAISERROR inside the CATCH block to return error  
-- information about the original error that caused  
-- execution to jump to the CATCH block.  
RAISERROR (@ErrorMessage, -- Message text.  
  @ErrorSeverity, -- Severity.  
  @ErrorState -- State.  
  );  

        ROLLBACK TRANSACTION;

SELECT  '�@�~����!!'
    END CATCH;










--DROP TABLE ORDDE4_�Ѿl�s�{����_����
--SELECT * INTO ORDDE4_�Ѿl�s�{����_���� FROM ORDDE4_�Ѿl�s�{����_����_D WHERE 1 = 0

-------------------------------------------------------------------------------------------------------------------------------------

-------------------------------------------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------------------------------------------
---- 2023/09/21 �v �p����F
UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME2 = B.DLYTIME2
FROM ORDDE4_�Ѿl�s�{����_���� A,#ORDDE4_�Ѿl�s�{����_����_DLYTIME2 B
WHERE A.INPART = B.INPART AND A.ORDSQ2 = B.ORDSQ2  AND A.ORDSQ3 = B.ORDSQ3
AND B.DLYTIME2 IS NOT NULL
--AND A.INPART LIKE @INPART

---- �����i�h���ɶ��I
UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME2 = GETDATE()
FROM ORDDE4_�Ѿl�s�{����_���� A , ORDDE4_�Ѿl�s�{���� B
WHERE A.INPART = B.INPART
AND A.ORDSQ2 = B.�b���s�{��
AND B.�i�Τu�� < 0
AND A.DLYTIME2 IS NULL
--AND A.INPART LIKE @INPART

---- �����i�h���ɶ��I�H�� ������ܧ�λs�{�ܧ�ɭP�S�ܦw���� �n��^�h 2023/09/22
UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME2 = NULL
FROM ORDDE4_�Ѿl�s�{����_���� A , ORDDE4_�Ѿl�s�{���� B
WHERE A.INPART = B.INPART
AND A.ORDSQ2 = B.�b���s�{��
AND B.�i�Τu�� > 0
AND A.DLYTIME2 IS NOT NULL
--AND A.INPART LIKE @INPART

---- 2023/09/21 �v �p��եߩβk�� ���n�����n
UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME = 0
WHERE ORDFO IN (SELECT PRDOPNO FROM #SOPNAME
WHERE PRDOPGP IN (SELECT PRDOPNO FROM #SOPNAME WHERE PRDNAME IN ('as','AS','ASF','ASCG','WD','LSWD')))
AND DLYTIME > 0
--AND INPART LIKE @INPART




-------------------------------------------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------------------------------------------
----�S��B�z
UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 2 WHERE INPART IN ('23Q03286-000') AND �b���s�{�� = 0
UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 1 WHERE INPART IN ('23Q03943-000','23Q03943-000#1','23Q03943-000#10','23Q03943-000#11','23Q03943-000#2',
'23Q03943-000#3','23Q03943-000#4','23Q03943-000#5','23Q03943-000#6','23Q03943-000#7','23Q03943-000#8','23Q03943-000#9','23Q03942-000','23Q03942-000#1','23Q03942-000#2') AND �b���s�{�� = 0

UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 4 WHERE INPART = '23F01186-1-000' AND �b���s�{�� = 1
UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 4 WHERE INPART = '23F03186-000' AND �b���s�{�� = 1
UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 4 WHERE INPART = '23F03223-000#1' AND �b���s�{�� = 1
UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 1 WHERE INPART = '24N03030-002R1' AND �b���s�{�� = 0
UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 1 WHERE INPART = '24Q03437-001R1' AND �b���s�{�� = 0
UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 1 WHERE INPART IN('25F01183-0-000','25F01183-0-000#1') AND �b���s�{�� = 2


UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME = 0 WHERE INPART IN ('24G01069-0-001#2R3') AND PRDNAME = 'g7'
UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME = 0 WHERE INPART IN ('24G03918SL-001') AND PRDNAME = '3Q'
UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME = 0 WHERE INPART IN ('24G03918SL-002') AND PRDNAME = 'HM'

UPDATE ORDDE4_�Ѿl�s�{����_���� SET DLYTIME = 0 WHERE INPART IN ('24H01013-0-000R1-E1-1-02') AND ORDFO IN('50','G50')

UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 5 WHERE INPART = '25G01178-0-000' AND �b���s�{�� = 7

--------------��U�B�z�Ѿl�s�{����� 2024/01/08 Techup----------------------------------


SELECT * INTO #ORDDE4_�Ѿl�s�{����_���� FROM ORDDE4_�Ѿl�s�{����_���� WHERE ORDFCO = 'N'

DELETE #ORDDE4_�Ѿl�s�{����_����
FROM #ORDDE4_�Ѿl�s�{����_���� A,(SELECT distinct INPART FROM #ORDDE4_�Ѿl�s�{����_���� WHERE ORDFO = '27') B
WHERE A.INPART = B.INPART

delete #ORDDE4_�Ѿl�s�{����_���� where �ήɶ���ORDSQ2 = 0 AND ORDSQ2 <> 0

SELECT A.INPART,B.ORDSNO,B.�b���s�{��,ORDSQ2,ORDFO,ORDDTP,ORDFM1,A.PRDNAME,ORDDY1,ORDDY2,ORDDY5,A.SOPKIND,PRTFM,DEPTNO,Applier,D.PRDOPGP
INTO #�U�s�d�Ѿl�s�{
FROM #ORDDE4_�Ѿl�s�{����_���� A,ORDDE4_�Ѿl�s�{���� B,ORDE3 C,(SELECT * FROM #SOPNAME) D
WHERE A.INPART = B.INPART AND B.INPART = C.INPART AND C.LINE <>'Z' AND A.ORDFO = D.PRDOPNO
ORDER BY A.INPART,ORDSQ2

SELECT A.*,B.PRDNAME AS PRDNAME_GP
INTO #�U�s�d�Ѿl�s�{_NEW
FROM #�U�s�d�Ѿl�s�{ A,(SELECT * FROM #SOPNAME) B
WHERE A.PRDOPGP = B.PRDOPNO
ORDER BY A.INPART,ORDSQ2

UPDATE #�U�s�d�Ѿl�s�{_NEW SET PRDOPGP = '895',PRDNAME_GP = '3Q' WHERE REPLACE(REPLACE(PRDNAME,'��',''),'��','') LIKE '3Q%'
UPDATE #�U�s�d�Ѿl�s�{_NEW SET PRDOPGP = '896',PRDNAME_GP = 'LQ' WHERE REPLACE(REPLACE(PRDNAME,'��',''),'��','') LIKE 'LQ%'
UPDATE #�U�s�d�Ѿl�s�{_NEW SET PRDOPGP = '830',PRDNAME_GP = 'PM' WHERE REPLACE(REPLACE(PRDNAME,'��',''),'��','') LIKE 'PM%'

SELECT '���x' ����,C.PRDNAME_GP,COUNT(*)�U�s�{���x��
INTO #�U�s�{���x��
FROM MACPRD1 A,MACPRD B,(SELECT distinct PRDOPGP,PRDNAME_GP FROM #�U�s�d�Ѿl�s�{_NEW) C
WHERE A.SOPNO = C.PRDOPGP
AND A.MAHNO = B.MAHNO AND B.DEPT <> '' AND ISNULL(ASTNO,'') <> ''
GROUP BY PRDNAME_GP

SELECT A.*,B.�U�s�{���x��,'���x' ����
INTO #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
FROM #�U�s�d�Ѿl�s�{_NEW A LEFT OUTER JOIN #�U�s�{���x�� B ON A.PRDNAME_GP = B.PRDNAME_GP


UPDATE #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
SET �U�s�{���x�� = B.�U�s�{���x��,���� = '�H��'
FROM #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
A,(SELECT �H�O�t�m���x�ϰ�,COUNT(*) �U�s�{���x�� FROM �H�����t_TEST GROUP BY �H�O�t�m���x�ϰ�) B
WHERE A.PRDNAME_GP = �H�O�t�m���x�ϰ� AND ISNULL(A.�U�s�{���x��,'') = ''

UPDATE #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
SET �U�s�{���x�� = B.�U�s�{���x��,���� = '�H��'
FROM #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
A,(SELECT �H�O�t�m�D�n�s�{,COUNT(*) �U�s�{���x�� FROM �H�����t_TEST GROUP BY �H�O�t�m�D�n�s�{) B
WHERE A.PRDNAME_GP = �H�O�t�m�D�n�s�{ AND ISNULL(A.�U�s�{���x��,'') = ''


--(SELECT �H�O�t�m�D�n�s�{,COUNT(*) �U�s�{���x�� FROM �H�����t_TEST WHERE �H�O�t�m�D�n�s�{ = 'QC' GROUP BY �H�O�t�m�D�n�s�{)

UPDATE #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
SET �U�s�{���x�� = B.�U�s�{���x��,���� = '�H��'
FROM #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x�� A,
(SELECT �H�O�t�m�D�n�s�{,COUNT(*) �U�s�{���x�� FROM �H�����t_TEST WHERE �H�O�t�m�D�n�s�{ = 'QC' GROUP BY �H�O�t�m�D�n�s�{) B
WHERE PRDNAME_GP = 'HM'


--SELECT *
----INTO ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
--FROM #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
----WHERE PRDNAME_GP = 'HM'
--ORDER BY INPART,ORDSQ2


IF((SELECT COUNT(*) FROM #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��) > 0)
BEGIN
DROP TABLE ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
SELECT *
INTO ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
FROM #ORDDE4_�Ѿl�s�{����_�U�s�d�H�����x��
ORDER BY INPART,ORDSQ2
END
--------------��U�B�z�Ѿl�s�{����� 2024/01/08 Techup----------------------------------

----------------�B�z���g���e�����---2024/01/19 Techup---------------------------------------------------
SELECT C.ORDNO,C.ORDSQ,C.INPART �q��u��,C.ORDQTY �q���,B.ORDSNO ���,B.INPART �s�d,D.�i�Τu��,ISNULL(E.PURDY,'') �w�p�^�t�� ,A.ORDSQ2,A.ORDFO
INTO #TEMP1_ORDDE4
FROM ORDDE4 A JOIN ORDE3 B ON A.ORDFNO = B.INPART
JOIN ORDE2 C ON B.ORDTP = C.ORDTP AND B.ORDNO = C.ORDNO AND B.ORDSQ= C.ORDSQ
JOIN ORDDE4_�Ѿl�s�{���� D ON B.INPART = D.INPART
LEFT OUTER JOIN (SELECT A.PURDY,INPART,PS,PE FROM PURDEL A,PURMAS B WHERE A.PURAA = 2 AND A.PURNO = B.PURNO AND B.SCTRL <> 'X') E
ON E.INPART = B.INPART
WHERE B.INFIN = 'N' AND ORDFCO = 'N' AND ORDFO = '25O'
AND B.LINE NOT IN ('L')
ORDER BY B.ORDSNO,C.INPART,ORDSQ2

SELECT ORDNO,ORDSQ,�q��u��,�q���,���,�w�p�^�t��
INTO #TEMP2_A FROM #TEMP1_ORDDE4
GROUP BY ORDNO,ORDSQ,�q��u��,�q���,���,�w�p�^�t��

SELECT A.ORDNO,A.ORDSQ,A.�q��u��,A.�q���,A.���,�̤j�w�p�^�t�� ,�^�t���A = CAST('' AS varchar(10))
INTO #TEMP3_A
FROM #TEMP2_A A,(SELECT �q��u��,���,MAX(�w�p�^�t��) �̤j�w�p�^�t�� FROM #TEMP2_A GROUP BY �q��u��,���) B
WHERE A.��� = B.��� AND A.�q��u�� = B.�q��u��
GROUP BY ORDNO,ORDSQ,A.�q��u��,A.�q���,A.���,�̤j�w�p�^�t��
ORDER BY A.���,ORDNO,ORDSQ

UPDATE #TEMP3_A SET �^�t���A = '�w�p' WHERE ISNULL(�̤j�w�p�^�t��,'') <> ''
UPDATE #TEMP3_A SET �^�t���A = '����' WHERE ISNULL(�̤j�w�p�^�t��,'') = ''

UPDATE #TEMP3_A
SET �̤j�w�p�^�t�� = B.�̤j�w�p�^�t��
FROM #TEMP3 A,(SELECT MAX(�̤j�w�p�^�t��) �̤j�w�p�^�t�� FROM #TEMP3_A) B WHERE �^�t���A = '����'

SELECT *,SUM(�q���) OVER (ORDER BY ���,ORDNO,ORDSQ) AS �֥[��
INTO #TEMP4_A FROM #TEMP3_A WHERE �^�t���A = '����' ORDER BY ���,ORDNO,ORDSQ

UPDATE #TEMP4_A SET �̤j�w�p�^�t�� = CONVERT(varchar(100), DATEADD(DD,�֥[��*7,CAST(�̤j�w�p�^�t�� AS DATETIME)), 111)



SELECT
ORDNO,ORDSQ,�q��u��,�q���,���,�̤j�w�p�^�t�� ,�^�t���A
INTO #���g�~�]�^�t
FROM #TEMP3_A WHERE �^�t���A = '�w�p'    
UNION
SELECT ORDNO,ORDSQ,�q��u��,�q���,���,�̤j�w�p�^�t�� ,�^�t���A FROM #TEMP4_A
ORDER BY ���,ORDNO,ORDSQ

DELETE ���g�~�]�^�t

INSERT INTO ���g�~�]�^�t
SELECT SQ = ROW_NUMBER() OVER (ORDER BY �̤j�w�p�^�t��,ORDNO,ORDSQ),*
FROM #���g�~�]�^�t
--

-------�C��u���@��������ѳ̤j��� 2024/05/17 Techup-----------
IF (DATEPART(HH,GETDATE()) = 8 )
BEGIN
SELECT D.ORDCU,A.INPART,B.INDWG,B.ORDQTY,ISNULL(H.NGQTY,0) NGQTY,B.ORDSNO,MAX(ORDDY5) ORDDY5
,dbo.�ɶ��t_�̤W�Z�ɶ�(MAX(ORDDY5) ,B.ORDSNO,10)/60.00 �t���p��
INTO #TEMP50
FROM ORDDE4_�Ѿl�s�{����_���� A
JOIN ORDE3 B ON A.INPART = B.INPART
JOIN ORDDE4_�Ѿl�s�{���� C ON A.INPART = C.INPART
JOIN ORDE1 D ON D.ORDTP = B.ORDTP AND D.ORDNO = B.ORDNO
LEFT OUTER JOIN PARTNG H ON A.INPART = H.INPART
WHERE B.INFIN = 'N' AND ORDSQ2 > 0 AND ORDSQ3 = 0 AND A.ORDFCO = 'N'
AND ISNULL(ORDDY5,'') <> '' AND B.ORDQTY - ISNULL(H.NGQTY,0) > 0
AND C.�i�Τu�� <= 0 AND B.LINE NOT IN ('Z','T')
AND D.ORDCU NOT BETWEEN 'Z01' AND 'Z50'
AND D.ORDCU NOT IN ('GON','RD1','RD2','RD3','RD6','RD7','RD9','RDA','TC','M','MF3','PE','QC')
GROUP BY D.ORDCU,A.INPART,B.INDWG,B.ORDSNO,B.ORDQTY,ISNULL(H.NGQTY,0) ,C.�i�Τu��

INSERT INTO �C��̤j����X�f���
SELECT GETDATE() '��ƫإߤ�',*
FROM #TEMP50
ORDER BY �t���p��
END
-------�C��u���@��������ѳ̤j��� 2024/05/17 Techup-----------

-------�C��u���@��������ѥi�Τu�� 2025/07/22 �v-----------
IF (DATEPART(HH,GETDATE()) = 16)
BEGIN

INSERT INTO �i�Τu��_���v
SELECT DISTINCT INPART ,�i�Τu�� ,GETDATE()
FROM ORDDE4_�Ѿl�s�{����

DELETE FROM �i�Τu��_���v WHERE ��ƫإߤ� <  DATEADD(DAY,-7,CAST(GETDATE() AS DATE))

END
-------�C��u���@��������ѥi�Τu�� 2025/07/22 �v-----------

--------�S��B�z ----2024/07/30 Techup
UPDATE ORDDE4_�Ѿl�s�{����_����
SET ORDFCO = 'N',PRDNAME = '��'
WHERE INPART IN ('23L09146ML023-000#6-1R1')
AND PRDNAME LIKE '%�ơ�%'
AND ORDFCO = 'Y'

UPDATE ORDDE4_�Ѿl�s�{����
SET �b���s�{�� = 0
WHERE INPART IN ('23L09146ML023-000#6-1R1')

UPDATE ORDDE4_�Ѿl�s�{����
SET �b���s�{�� = 0
WHERE INPART IN ('24F01087-1-000') AND �b���s�{�� = 4

---�S��B�z 2025/05/06
UPDATE ORDDE4_�Ѿl�s�{����
    SET �b���s�{�� = 0
    WHERE INPART IN ('24Q04697-003','24Q04871-003','24Q04872-003','24Q04873-003',
                '24Q04874-003','24Q04875-003','24Q04876-003','24Q05015-003','24Q05016-003')


UPDATE ORDDE4_�Ѿl�s�{����
SET �b���s�{�� = 7
WHERE INPART IN ('23F03249-000','23F03249-000#1') AND �b���s�{�� = 8

-------2024/11/13 Techup
UPDATE ORDDE4_�Ѿl�s�{����
SET �n��� = 1
WHERE INPART = '23Q01024-0-000-P1'

UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 9  
WHERE INPART = '25D03817AF-001#1R1' AND �b���s�{�� = 0


UPDATE ORDDE4_�Ѿl�s�{���� SET �b���s�{�� = 1 WHERE INPART IN ('25Q03342-000','25Q03343-000','25Q03344-000') AND �b���s�{�� = 0


----------------�B�z���g���e�����---2024/01/19 Techup---------------------------------------------------

----- 2024/01/30 ���ͦb�s�s�d�b����m (SFC2468NET_��B���)
EXEC dbo.SFC_�b�s�s�d�b����m '',''

-- EXEC dbo.����ORDE3�Ѿl�s�{ ''
--------�H�U����----------------------------------


IF OBJECT_ID(N'tempdb..#�n�R�����s�d') IS NOT NULL DROP TABLE #�n�R�����s�d
IF OBJECT_ID(N'tempdb..#SOPNAME') IS NOT NULL DROP TABLE #SOPNAME
IF OBJECT_ID(N'tempdb..#�O��ORDDE4_�Ѿl�s�{����_����_D') IS NOT NULL DROP TABLE #�O��ORDDE4_�Ѿl�s�{����_����_D
IF OBJECT_ID(N'tempdb..#TEMP2') IS NOT NULL DROP TABLE #TEMP2
IF OBJECT_ID(N'tempdb..#���ݭp�⪺�s�d') IS NOT NULL DROP TABLE #���ݭp�⪺�s�d
IF OBJECT_ID(N'tempdb..#TEMP3') IS NOT NULL DROP TABLE #TEMP3
IF OBJECT_ID(N'tempdb..#NZ_SOPNAME') IS NOT NULL DROP TABLE #NZ_SOPNAME
IF OBJECT_ID(N'tempdb..#Z_SOPNAME') IS NOT NULL DROP TABLE #Z_SOPNAME
IF OBJECT_ID(N'tempdb..#QA0') IS NOT NULL DROP TABLE #QA0
IF OBJECT_ID(N'tempdb..#QA1') IS NOT NULL DROP TABLE #QA1
IF OBJECT_ID(N'tempdb..#QA2') IS NOT NULL DROP TABLE #QA2
IF OBJECT_ID(N'tempdb..#����zDLYTIME_NEW_A') IS NOT NULL DROP TABLE #����zDLYTIME_NEW_A
IF OBJECT_ID(N'tempdb..#����zDLYTIME_A') IS NOT NULL DROP TABLE #����zDLYTIME_A
IF OBJECT_ID(N'tempdb..#RST') IS NOT NULL DROP TABLE #RST
IF OBJECT_ID(N'tempdb..#�ƶO����') IS NOT NULL DROP TABLE #�ƶO����
IF OBJECT_ID(N'tempdb..#��o�s�{') IS NOT NULL DROP TABLE #��o�s�{
IF OBJECT_ID(N'tempdb..#CAM�s�{') IS NOT NULL DROP TABLE #CAM�s�{
IF OBJECT_ID(N'tempdb..#TEMP_��oCAM') IS NOT NULL DROP TABLE #TEMP_��oCAM
IF OBJECT_ID(N'tempdb..#CAM�s�{') IS NOT NULL DROP TABLE #CAM�s�{
IF OBJECT_ID(N'tempdb..#�U�s�d�̤j��o�s�{') IS NOT NULL DROP TABLE #�U�s�d�̤j��o�s�{
IF OBJECT_ID(N'tempdb..#�U�s�d�̤jCAM�s�{') IS NOT NULL DROP TABLE #�U�s�d�̤jCAM�s�{
IF OBJECT_ID(N'tempdb..#��o�U�s�d�̤j�����骺�̤j�s�{��') IS NOT NULL DROP TABLE #��o�U�s�d�̤j�����骺�̤j�s�{��
IF OBJECT_ID(N'tempdb..#CAM�U�s�d�̤j�����骺�̤j�s�{��') IS NOT NULL DROP TABLE #CAM�U�s�d�̤j�����骺�̤j�s�{��
IF OBJECT_ID(N'tempdb..#TEMP_ALL') IS NOT NULL DROP TABLE #TEMP_ALL
IF OBJECT_ID(N'tempdb..#TEMP_��') IS NOT NULL DROP TABLE #TEMP_��
IF OBJECT_ID(N'tempdb..#����zDLYTIME_B') IS NOT NULL DROP TABLE #����zDLYTIME_B
IF OBJECT_ID(N'tempdb..#����zDLYTIME_NEW_B') IS NOT NULL DROP TABLE #����zDLYTIME_NEW_B
IF OBJECT_ID(N'tempdb..#ORDDE4_�Ѿl�s�{����_����_D_�Ĥ@���s�{') IS NOT NULL DROP TABLE #ORDDE4_�Ѿl�s�{����_����_D_�Ĥ@���s�{
IF OBJECT_ID(N'tempdb..#���ƶO�D�ƻs�{����') IS NOT NULL DROP TABLE #���ƶO�D�ƻs�{����
IF OBJECT_ID(N'tempdb..#INPART�Ĥ@���s�{����') IS NOT NULL DROP TABLE #INPART�Ĥ@���s�{����
IF OBJECT_ID(N'tempdb..#ORDDE4_�Ѿl�s�{����_����_D') IS NOT NULL DROP TABLE #ORDDE4_�Ѿl�s�{����_����_D
IF OBJECT_ID(N'tempdb..#TEMP1') IS NOT NULL DROP TABLE #TEMP1
IF OBJECT_ID(N'tempdb..#��X���') IS NOT NULL DROP TABLE #��X���
IF OBJECT_ID(N'tempdb..#��X���1') IS NOT NULL DROP TABLE #��X���1
IF OBJECT_ID(N'tempdb..#�B�z���x') IS NOT NULL DROP TABLE #�B�z���x
IF OBJECT_ID(N'tempdb..#����zDLYTIME_C') IS NOT NULL DROP TABLE #����zDLYTIME_C
IF OBJECT_ID(N'tempdb..#����zDLYTIME_NEW_C') IS NOT NULL DROP TABLE #����zDLYTIME_NEW_C
IF OBJECT_ID(N'tempdb..#��s�DA1���s�d���O') IS NOT NULL DROP TABLE #��s�DA1���s�d���O
IF OBJECT_ID(N'tempdb..#TOT2') IS NOT NULL DROP TABLE #TOT2
IF OBJECT_ID(N'tempdb..#TOT3') IS NOT NULL DROP TABLE #TOT3
IF OBJECT_ID(N'tempdb..#��X�e�����`�檺_�Ѿl�s�{����') IS NOT NULL DROP TABLE #��X�e�����`�檺_�Ѿl�s�{����
IF OBJECT_ID(N'tempdb..#�u��B�e����') IS NOT NULL DROP TABLE #�u��B�e����
IF OBJECT_ID(N'tempdb..#TEMP3_�~�s') IS NOT NULL DROP TABLE #TEMP3_�~�s
IF OBJECT_ID(N'tempdb..#�w��ASMLTNF�ϸ��~�]���ҥ~�B�z') IS NOT NULL DROP TABLE #�w��ASMLTNF�ϸ��~�]���ҥ~�B�z
IF OBJECT_ID(N'tempdb..#�Ѿl�u�ɮɼ�') IS NOT NULL DROP TABLE #�Ѿl�u�ɮɼ�
IF OBJECT_ID(N'tempdb..#���X�f�q��') IS NOT NULL DROP TABLE #���X�f�q��
IF OBJECT_ID(N'tempdb..#�έp���') IS NOT NULL DROP TABLE #�έp���
IF OBJECT_ID(N'tempdb..#�Ȥ�M�׽s���`�q���') IS NOT NULL DROP TABLE #�Ȥ�M�׽s���`�q���
IF OBJECT_ID(N'tempdb..#�U�s�d�Ѿl���[��') IS NOT NULL DROP TABLE #�U�s�d�Ѿl���[��
IF OBJECT_ID(N'tempdb..#�έp���_NEW') IS NOT NULL DROP TABLE #�έp���_NEW
IF OBJECT_ID(N'tempdb..#�έp���_NEW_01') IS NOT NULL DROP TABLE #�έp���_NEW_01
IF OBJECT_ID(N'tempdb..#�Ѿl�u��') IS NOT NULL DROP TABLE #�Ѿl�u��
IF OBJECT_ID(N'tempdb..#ORDE3') IS NOT NULL DROP TABLE #ORDE3
IF OBJECT_ID(N'tempdb..#TEMP�Ȧs') IS NOT NULL DROP TABLE #TEMP�Ȧs
IF OBJECT_ID(N'tempdb..#TEMP2�Ȧs') IS NOT NULL DROP TABLE #TEMP2�Ȧs
IF OBJECT_ID(N'tempdb..#TEMP3�Ȧs') IS NOT NULL DROP TABLE #TEMP3�Ȧs
IF OBJECT_ID(N'tempdb..#TEMP4�Ȧs') IS NOT NULL DROP TABLE #TEMP4�Ȧs
IF OBJECT_ID(N'tempdb..#PRODTM') IS NOT NULL DROP TABLE #PRODTM
IF OBJECT_ID(N'tempdb..#PRODTM_N') IS NOT NULL DROP TABLE #PRODTM_N
IF OBJECT_ID(N'tempdb..#��z�s�{') IS NOT NULL DROP TABLE #��z�s�{
IF OBJECT_ID(N'tempdb..#�U�s�d�s�{��') IS NOT NULL DROP TABLE #�U�s�d�s�{��
IF OBJECT_ID(N'tempdb..#�U�s�d�e����DLYTIME�X�p') IS NOT NULL DROP TABLE #�U�s�d�e����DLYTIME�X�p
IF OBJECT_ID(N'tempdb..#TQ1') IS NOT NULL DROP TABLE #TQ1
IF OBJECT_ID(N'tempdb..#TQ2') IS NOT NULL DROP TABLE #TQ2
IF OBJECT_ID(N'tempdb..#�e�m�s�{���A') IS NOT NULL DROP TABLE #�e�m�s�{���A

IF OBJECT_ID(N'tempdb..#ORDDE4_����]�p�̤j��') IS NOT NULL DROP TABLE #ORDDE4_����]�p�̤j��
IF OBJECT_ID(N'tempdb..#ORDDE4_����]�p����̤p��') IS NOT NULL DROP TABLE #ORDDE4_����]�p����̤p��
IF OBJECT_ID(N'tempdb..#ORDDE4_�����Ĥ@������') IS NOT NULL DROP TABLE #ORDDE4_�����Ĥ@������
IF OBJECT_ID(N'tempdb..#����zDLYTIME_CD') IS NOT NULL DROP TABLE #����zDLYTIME_CD
    IF OBJECT_ID(N'tempdb..#����zDLYTIME_NEW_CD') IS NOT NULL DROP TABLE #����zDLYTIME_NEW_CD
IF OBJECT_ID(N'tempdb..#����zDLYTIME_E') IS NOT NULL DROP TABLE #����zDLYTIME_E
    IF OBJECT_ID(N'tempdb..#����zDLYTIME_NEW_E') IS NOT NULL DROP TABLE #����zDLYTIME_NEW_E

IF OBJECT_ID(N'tempdb..#TEMP1_�ե�') IS NOT NULL DROP TABLE #TEMP1_�ե�
IF OBJECT_ID(N'tempdb..#TEMP2_�ե�') IS NOT NULL DROP TABLE #TEMP2_�ե�
IF OBJECT_ID(N'tempdb..#TEMP3_�ե�') IS NOT NULL DROP TABLE #TEMP3_�ե�
IF OBJECT_ID(N'tempdb..#TEMP4_�ե�') IS NOT NULL DROP TABLE #TEMP4_�ե�
IF OBJECT_ID(N'tempdb..#TEMP5_�ե�') IS NOT NULL DROP TABLE #TEMP5_�ե�
IF OBJECT_ID(N'tempdb..#TEMP6_�ե�') IS NOT NULL DROP TABLE #TEMP6_�ե�

IF OBJECT_ID(N'tempdb..#IV1') IS NOT NULL DROP TABLE #IV1
IF OBJECT_ID(N'tempdb..#IV2') IS NOT NULL DROP TABLE #IV2
IF OBJECT_ID(N'tempdb..#IV3') IS NOT NULL DROP TABLE #IV3
IF OBJECT_ID(N'tempdb..#INPART') IS NOT NULL DROP TABLE #INPART

IF OBJECT_ID(N'tempdb..#TEMP1_CMM�u�@') IS NOT NULL DROP TABLE #TEMP1_CMM�u�@
IF OBJECT_ID(N'tempdb..#MACPRD_CMM���x') IS NOT NULL DROP TABLE #MACPRD_CMM���x

