# Changelog

## 2026-04-02 雲端留存：單次 Benchmark 結果（TEST）

- 新增 `benchmark_results_2026-04-02.md`，保存本次單次實測結果。
- 測試條件：`10.1.1.76 / TEST / dbo.產生ORDE3剩餘製程`，每個情境執行 1 次（wall-clock）。
- 結果：
  - `@INPART='24X01008MT-0%'`：`356571 ms`
  - `@INPART='23G%'`：`350177 ms`
  - `@INPART='%'`：`345210 ms`

## 2026-04-02 字串預彙總（`#TEMP3_字串彙總`）接入兩分支 INSERT（本體檔）

- 修改 `產生ORDE3剩餘製程.sql`：
  - 在 `INSERT INTO ORDDE4_剩餘製程明細_D` 之前新增 `#TEMP3_字串彙總` 預彙總表。
  - 預先計算並索引：
    - `剩餘製程明細`
    - `剩餘製程明細2`
    - `剩餘製程明細3`
    - `剩餘數`
  - 兩個分支（`@INPART='%'` / `@INPART<>'%'`）的 `INSERT INTO ORDDE4_剩餘製程明細_D` 改為：
    - `FROM #製卡明細 LEFT JOIN #TEMP3_字串彙總`
    - 直接取用 `TMP3S` 欄位，移除原本重複相關子查詢。
- 目標：
  - 降低 `ORDDE4_剩餘製程明細_D` 寫入段落對 `#TEMP3` 的重複掃描。
  - 在不更動既有商規輸出的前提下，先降低 `%` 全量情境熱點成本。

## 2026-04-01 第三輪優化：批次化 PRODTM 時間差 + 索引補強（TEST）

- 直接修改並部署 `dbo.產生ORDE3剩餘製程`：
  - `#PRODTM_S_合併` 改含 `ID`，新增 `IX_PRODTM_S_MERGE_MAIN` / `IX_PRODTM_S_MERGE_KEY`。
  - 新增 `#PRODTM_S_分鐘`，改用 `EXEC [dbo].[時間分鐘差_依上班時間]` 批次計算分鐘差後回寫 `DLYTIME`。
  - 補強索引：
    - `#SOPNAME`: `IX_SOPNAME_GROUP`, `IX_SOPNAME_DESC_KIND`
    - `#TOT3`: `IX_TOT3_INPART_ORDSQ2`
    - `#製卡明細`: `IX_CARD_MAIN`
    - `#TEMP3`: `IX_TEMP3_INPART_ORDSQ2KEY`
- 單次 wall-clock 測試：
  - `@INPART='24X01008MT-0%'`：`104577ms`
  - `@INPART='23G%'`：`111441ms`
  - `@INPART='%'`：`343962ms`
- 與上一輪比較：`%` 由 `442892ms` 降至 `343962ms`（約 -22%）。

## 2026-04-01 直接改寫正式 SP 第二輪熱點修補（TEST）

- 直接修改 `產生ORDE3剩餘製程.sql` 並部署到 `10.1.1.76 / TEST` 的 `[dbo].[產生ORDE3剩餘製程]`。
- 第一輪（保留完整商規）：
  - `#ORDE3` 迴圈改 `CROSS APPLY` 一次展開。
  - 目標表刪除改為：
    - `%` 優先 `TRUNCATE`，失敗 fallback `DELETE`。
    - 非 `%` 用本次 `INPART` 集合刪除（`#TEMP3` / `#製卡明細`）。
  - `#整批整理DLYTIME` 與 `#QA1` 回寫 join 鍵補索引。
- 第二輪：
  - `#TEMP3` 的 `WKNO/DEPTNO` 回寫改為 `#PRODTM_WK` 快照 join。
  - `#PRODTM_S_合併` 補索引，並只對前後關報工皆有值資料執行時間函式。
- 單次 wall-clock 測試：
  - 第一輪後：`24X01008MT-0%=117915ms`、`23G%=110978ms`、`%=438275ms`
  - 第二輪後：`24X01008MT-0%=125225ms`、`23G%=123405ms`、`%=442892ms`
- 結論：目前 `%` 全量仍約 7 分多鐘，主要瓶頸仍在 `ORDDE4_剩餘製程明細_D` 大字串彙總寫入與時間函式段落。

## 2026-03-31 15:17 新增全重寫 v2 並完成初步基準（TEST）

- 新增檔案 `產生ORDE3剩餘製程_v2.sql`，建立 `dbo.產生ORDE3剩餘製程_v2`。
- v2 以單次快照重用為核心：`#Card / #SOP / #ProdAgg / #Dispatch / #FlowDedup / #Summary`。
- v2 已部署至 `10.1.1.76 / TEST`，`modify_date=2026-03-31 15:17:14`。
- 快速 wall-clock 測試（單次）：
  - `@INPART='24X01008MT-0%'`：`104 ms`
  - `@INPART='23G%'`：`1,807 ms`
  - `@INPART='%'`：`57,797 ms`
- 備註：v2 目前定位為 A/B 驗證版，尚需逐項比對舊版商規輸出等價性。

## 2026-03-30 10:58 重複鍵修補 + 實測（TEST）

- 修補 `PK_ORDDE4_剩餘製程明細_直式_D` 重複鍵：
  - 寫回 `ORDDE4_剩餘製程明細_直式_D` 前，改為依 `#TEMP3` 的 `INPART` 清理既有資料。
- 修補 `PK_ORDDE4_剩餘製程明細_D` 重複鍵：
  - `@INPART <> '%'` 分支寫回前，改為依 `#製卡明細` 的 `INPART` 清理既有資料。
- 部署後即時測試（`10.1.1.76 / TEST`）：
  - `@INPART='24X01008MT-0%'`：`WallClockMs=156661`（Status=OK）
  - `@INPART='23G%'`：`WallClockMs=155546`（Status=OK）
- 以上測試不再出現先前 PK 重複鍵錯誤。

## 2026-03-30 10:47 同資料表單次查詢再重用（PRODTM / 指派時間）

- 依使用者回饋「跑速未改善」，再做一次「同表只查一次」優化並部署到 TEST。
- 新增 `PRODTM` 一次快照與重用鏈：
  - `#PRODTM_PTPSQ`
  - `#PRODTM_已報工彙總`
  - `#PRODTM_最後報工`
  - `#PRODTM_有效PTPNO`
- 取代重複子查詢：
  - `ORDDE4` 已報工更新改用 `#PRODTM_已報工彙總`
  - `#TEMP3` 最後報工時間改用 `#PRODTM_最後報工`
  - 兩處「後製程已報工」判斷改用 `#PRODTM_有效PTPNO`
  - `WKNO/DEPTNO` `OUTER APPLY` 改接 `#PRODTM_PTPSQ`
- 新增 `#指派時間_機台首站`，替換一處重複 `#指派時間` Group By 子查詢。
- 已部署，`sys.procedures.modify_date` 更新為 `2026-03-30 10:47:07`。

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



