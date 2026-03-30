# Changelog

## 2026-03-30 10:36 直接部署 DB 版優化 SP（TEST）

- 依使用者提供連線字串，直接讀取 `10.1.1.76 / TEST` 內 `dbo.產生ORDE3剩餘製程` 定義。
- 新增 `產生ORDE3剩餘製程_從DB直接優化.sql`（以 DB 版本為母版，產生可直接執行 `ALTER PROCEDURE`）。
- 已執行 `ALTER PROCEDURE` 部署到 TEST，`sys.procedures.modify_date` 更新為 `2026-03-30 10:36:10`。
- 回讀驗證關鍵優化項目均存在（XACT_ABORT、TEMP 索引、重複彙總暫存、OUTER APPLY 決定性更新、DLYTIME_O 歸零）。

## 2026-03-30 09:46 新增效能基準測試腳本（dbo.產生ORDE3剩餘製程）

- 新增 `benchmark_產生ORDE3剩餘製程.sql`。
- 腳本可直接在 `TEST` 執行，輸出：
  - 每次執行明細（wall-clock / CPU / elapsed / logical reads / physical reads / writes）
  - 情境彙總（AVG / P95 / MIN / MAX）
- 使用 `sys.dm_exec_procedure_stats` 前後差值估算單次資源成本，並保留併發污染風險註記。

## 2026-03-30 09:39 產生ORDE3剩餘製程.sql 效能優化（排程重算）

- 直接寫改本體檔 產生ORDE3剩餘製程.sql，新增 2026/03/30 MODIFY 註記。
- 調整 #TEMP3 索引：
  - 將聚集鍵由包含可變欄位（WKNO/Applier/PRTFM）改為穩定鍵（ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDSQ3）。
  - 新增 IX_TEMP3_INPART_STATUS、IX_TEMP3_INPART_OP，強化 INPART/ORDSQ2/狀態 路徑。
- 強化 #指派時間：
  - 新增 IX_TMP_DISPATCH_INPART_OP、IX_TMP_DISPATCH_STATUS。
  - 新增彙總暫存表 #指派時間_SetUpKey、#指派時間_機台區間、#指派時間_最小機台 及其索引。
- 將重複子查詢改為彙總暫存重用：
  - A1DLYTIME 兩段 SetUpTime 子查詢改接 #指派時間_SetUpKey。
  - 機台超前工時段落改接 #指派時間_機台區間。
  - #TEMP3 建立時 MIN(Applier) 子查詢改接 #指派時間_最小機台。
## 2026-03-12 23:54 產生ORDE3剩餘製程.sql 再校正

- 直接寫改本體檔 產生ORDE3剩餘製程.sql，新增 2026/03/12 MODIFY 註記。
- 修正邏輯順序：@INPART 正規化提前到首次使用前，避免 ORDDTP 更新範圍失準。
- ORDDTP 更新改為 ANSI JOIN 並加入 @INPART 範圍收斂條件（透過 ORDE3 鍵值 EXISTS）。
- 降低重複存取：啟用 #SOPNAME、#指派時間、#TEMP2、#QA1、#RST 索引。
- 修正非決定性更新：#TEMP3 的 WKNO/DEPTNO 改為 OUTER APPLY TOP (1)。
- 補強一致性：新增 DLYTIME_O 負值歸零；在站製程序回填條件新增 A.剩餘工時 > 0。
## 2026-03-12 23:10 直接修改 StoredProcedure 本體

- 新增 `產生ORDE3剩餘製程_實際修補SP.sql`。
- 此檔會直接從資料庫讀取 `dbo.產生ORDE3剩餘製程` 定義，套用 5 輪修正後 `ALTER PROCEDURE`。
- 內含片段匹配檢查：若任一關鍵片段找不到會中止，避免錯版誤改。

## 2026-03-12 23:35 產生ORDE3剩餘製程五輪修正

- 新增 `產生ORDE3剩餘製程_5輪修正.sql`。
- 完成 5 輪檢查與修正策略：
  - 第 1 輪：參數與交易安全性（`XACT_ABORT`、`@INPART` 正規化）。
  - 第 2 輪：暫存表索引補強（高頻 Join 鍵）。
  - 第 3 輪：修正非決定性更新（`#TEMP3` 的 `WKNO/DEPTNO` 來源）。
  - 第 4 輪：降低全表更新範圍（增加 `@INPART` 條件與必要防呆）。
  - 第 5 輪：邏輯一致性修正（在站製程序回填條件、負值工時歸零一致化）。
- 因工作目錄無 `.sln/.csproj`，`dotnet build` 無法執行成功（MSB1003）。

## 2026-03-12 23:30 直接修改 產生ORDE3剩餘製程.sql（語意邏輯）

- 直接修改 [產生ORDE3剩餘製程.sql] 本體檔（非外掛修補檔）。
- 修正項目：
  - 加入 SET XACT_ABORT ON。
  - @INPART 參數正規化（NULLIF/LTRIM/RTRIM）。
  - ORDDTP 更新改 ANSI JOIN 並加入 @INPART 範圍收斂。
  - 啟用 #SOPNAME 與 #指派時間 暫存索引建立語句。
  - #TEMP3 的 WKNO/DEPTNO 更新改為 OUTER APPLY TOP (1)，避免非決定性更新。
  - 補上 DLYTIME_O < 0 歸零。

## 2026-03-12 23:45 直接寫改本體檔

- 直接修改 [產生ORDE3剩餘製程.sql]。
- 實際套用：XACT_ABORT、@INPART 正規化、ORDDTP 範圍收斂、#SOPNAME/#指派時間 索引、WKNO/DEPTNO 決定性更新、DLYTIME_O 歸零。



