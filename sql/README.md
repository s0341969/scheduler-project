# 專案說明

本次針對 `dbo.產生ORDE3剩餘製程` 進行 5 輪檢查與修正，重點是：

1. 效能：減少暫存表掃描、降低不必要的全表更新、避免非決定性更新。
2. 邏輯：修正參數正規化、更新行為一致性、避免因多筆來源造成結果不穩定。
3. 可靠性：加入交易與錯誤處理強化，避免執行中斷造成資料不一致。

## 主要產出

- SQL 修補檔（方案說明版）：`產生ORDE3剩餘製程_5輪修正.sql`
- SQL 修補檔（可直接改 DB SP 本體）：`產生ORDE3剩餘製程_實際修補SP.sql`
- 說明文件同步：`CHANGELOG.md`、`TODO.md`

## 重要決策

- 不直接重寫整支 SP，改用「最小侵入」修補，先保住既有商業邏輯。
- 先做可確定正向收益的修正：參數正規化、索引補強、非決定性 UPDATE 修正。
- 大規模重構（例如整支改成 CTE/模組化）列入後續 TODO。

## 本次直接改檔

- 已直接在 [產生ORDE3剩餘製程.sql] 套用語意與邏輯修正（不透過資料庫抓取）。

- 2026-03-12：已直接在本體檔 [產生ORDE3剩餘製程.sql] 寫改（非僅修補腳本）。

## 2026-03-12 本體檔再校正（語意邏輯 + 重複存取）

- 將 @INPART 正規化提前到首次使用前，避免前段 ORDDTP 更新範圍誤判。
- ORDDTP 更新改為 ANSI JOIN，並以 @INPART 對應 ORDE3 鍵值收斂更新範圍。
- 啟用高頻暫存表索引：#SOPNAME、#指派時間、#TEMP2、#QA1、#RST。
- #TEMP3 之 WKNO/DEPTNO 更新改為 OUTER APPLY TOP (1)，避免多筆來源造成非決定性結果。
- 補強一致性：新增 DLYTIME_O 負值歸零、在站製程序回填增加 A.剩餘工時 > 0 條件。
- 修改段落均加上 2026/03/12 MODIFY 註記，且維持 Unicode 中文內容（無亂碼）。

## 2026-03-30 本體檔效能優化（執行時間 7-8 分鐘議題）

- 調整 `#TEMP3` 索引策略：
  - 原本聚集索引包含 `WKNO/Applier/PRTFM`（可變欄位）會放大更新成本。
  - 改為穩定鍵 `ORDTP,ORDNO,ORDSQ,ORDSQ1,ORDSQ2,ORDSQ3`，並新增 `INPART` 導向的非聚集索引。
- `#指派時間` 新增兩條高頻路徑索引，降低後續 Join 與 Group By 掃描。
- 把重複子查詢改為一次彙總重用：
  - 新增 `#指派時間_SetUpKey`（SetUpTime 關聯用）
  - 新增 `#指派時間_機台區間`（機台時段彙總用）
  - 新增 `#指派時間_最小機台`（MIN(Applier) 關聯用）
- 將 A1 排程與機台超前工時段落改為使用上述彙總暫存表，避免重複計算。

## 效能量測腳本（2026-03-30）

- 新增可直接執行的基準測試腳本：
  - `benchmark_產生ORDE3剩餘製程.sql`
- 量測內容：
  - 每次執行 wall-clock 毫秒
  - `sys.dm_exec_procedure_stats` 前後差值（CPU / elapsed / logical reads / physical reads / writes）
  - 各情境彙總（AVG / P95 / MIN / MAX）
- 預設情境：
  - 單一製卡（可自行替換）
  - 特定前綴批次
  - 全量 `%` 高負載

## 2026-03-30 直接以資料庫版本改寫與部署

- 來源基準改為直接讀取 DB 定義（`10.1.1.76 / TEST / dbo.產生ORDE3剩餘製程`），避免目錄檔與 DB 漂移。
- 新增部署腳本：
  - `產生ORDE3剩餘製程_從DB直接優化.sql`（可直接 `ALTER PROCEDURE`）
- 已直接部署到 TEST，並回讀驗證關鍵優化標記存在：
  - `SET XACT_ABORT ON`
  - `IX_TMP2_MAIN / IX_QA1_OLDPART_ORDSQ2 / IX_RST_MAIN / IX_TEMP3_INPART_STATUS`
  - `#指派時間_SetUpKey / #指派時間_機台區間 / #指派時間_最小機台`
  - `OUTER APPLY` 決定性更新 `WKNO/DEPTNO`

## 2026-03-30 同資料表單次查詢再重用（第二波）

- 針對使用者反饋「同一資料表重複 SELECT 仍偏慢」，新增 `PRODTM` 一次快照策略：
  - `#PRODTM_PTPSQ`：先取 `PTPSQ > 0` 必要欄位並建索引。
  - `#PRODTM_已報工彙總`：供 `ORDDE4` 已報工更新重用。
  - `#PRODTM_最後報工`：供 `#TEMP3` 取最後報工時間重用。
  - `#PRODTM_有效PTPNO`：供「後製程已報工」判斷重用（取代兩處重複子查詢）。
- `#指派時間` 再補一層重用：
  - 新增 `#指派時間_機台首站`，取代原本一處 `MIN(StartTime)` 重複 Group By 子查詢。

## 2026-03-30 穩定性修補與即時實測

- 修補重複鍵寫入問題（避免 benchmark 中途失敗）：
  - `ORDDE4_剩餘製程明細_直式_D`：寫回前改為依 `#TEMP3` 實際 `INPART` 清理舊資料，而非僅 `LIKE @INPART`。
  - `ORDDE4_剩餘製程明細_D`：`@INPART <> '%'` 分支寫回前改為依 `#製卡明細` 實際 `INPART` 清理舊資料。
- TEST 即時基準（單次）：
  - `@INPART='24X01008MT-0%'`：`156,661 ms`
  - `@INPART='23G%'`：`155,546 ms`
- 補充：上述兩組測試皆可完整執行（不再出現先前 PK 重複鍵中斷）。

## 2026-03-31 全重寫版本 v2（A/B 用）

- 新增 `dbo.產生ORDE3剩餘製程_v2`（保留舊版，先做並行驗證）。
- 設計目標：同表單次掃描、固定快照重用、雙目標表一次寫回。
- 主要策略：
  - `#Card / #SOP / #ProdAgg / #Dispatch` 四段快照。
  - `#FlowDedup` 先做 PK 去重，再寫入 `ORDDE4_剩餘製程明細_直式_D`。
  - `#Summary` 聚合後寫入 `ORDDE4_剩餘製程明細_D`。
- TEST 快速基準（單次 wall-clock）：
  - `@INPART='24X01008MT-0%'`：`104 ms`
  - `@INPART='23G%'`：`1,807 ms`
  - `@INPART='%'`：`57,797 ms`（約 57.8 秒）
- 目前狀態：效能已大幅下降；下一步需做欄位/商規逐項對帳確認與舊版等價性。

## 2026-03-31 完整流程版 v2_full（覆蓋舊版商規）

- 新增 `dbo.產生ORDE3剩餘製程_v2_full`：
  - 以完整流程版為基底（非簡化骨架），用於「功能完整性」驗證與替換評估。
- TEST 基準（單次 wall-clock）：
  - `@INPART='24X01008MT-0%'`：`175,710 ms`
  - `@INPART='23G%'`：`169,607 ms`（重試測得 OK）
  - `@INPART='%'`：`491,493 ms`（約 8.2 分鐘）
- 結論：
  - `v2_full` 保留完整流程，但效能接近舊版。
  - `v2`（骨架版）快很多但尚未覆蓋全部商規。

## 2026-04-01 直接改寫正式 SP（完整商規保留）第二輪

- 直接修改並部署 `dbo.產生ORDE3剩餘製程`（非 v2 骨架），重點為不刪商規、只替換熱點寫法：
  - `#ORDE3` 由 `WHILE` 逐筆展開改為 `CROSS APPLY` set-based 一次展開。
  - `ORDDE4_剩餘製程明細_直式_D` / `ORDDE4_剩餘製程明細_D` 清理策略：
    - `@INPART='%'`：`TRUNCATE`（失敗自動 fallback `DELETE`）
    - 非 `%`：改為依本次實際 `INPART` 集合刪除（`#TEMP3` / `#製卡明細`）。
  - `#整批整理DLYTIME_A / #整批整理DLYTIME_NEW_A` 補索引，`#QA1` 更新改明確 `JOIN`。
  - `#PRODTM_S_合併` 補索引，時間函式改只對「前後關報工皆有值」資料計算。
  - `#TEMP3` 的 `WKNO/DEPTNO` 回寫改為先建 `#PRODTM_WK` 快照後一次 `JOIN` 更新（避免逐列 `OUTER APPLY`）。
- TEST 實測（單次 wall-clock）：
  - 第一輪後：`24X01008MT-0% = 117,915 ms`、`23G% = 110,978 ms`、`% = 438,275 ms`
  - 第二輪後：`24X01008MT-0% = 125,225 ms`、`23G% = 123,405 ms`、`% = 442,892 ms`
- 結論：
  - 在「完整商規不刪減」前提下，當前瓶頸仍集中在字串彙總寫入 `ORDDE4_剩餘製程明細_D` 與多段時間函式計算。
  - 下一階段需要針對 `ORDDE4_剩餘製程明細_D` 改成「先分段預彙總、再一次寫入」的結構化重寫，才有機會明顯縮短 `%` 全量時間。

## 2026-04-01 第三輪熱點修補（正式 SP）

- 針對第二輪後的熱點（`#PRODTM_S_合併` 時間差計算）改為批次流程：
  - `#PRODTM_S_合併` 改含 `ID`，並建立 `ID`/鍵值索引。
  - 新增 `#PRODTM_S_分鐘`，用既有 `EXEC [dbo].[時間分鐘差_依上班時間]` 批次計算分鐘差。
  - `DLYTIME` 改由批次結果回寫（取代原本逐列 scalar UDF 計算）。
- 補強其他高頻讀取路徑索引：
  - `#SOPNAME`: `IX_SOPNAME_GROUP`, `IX_SOPNAME_DESC_KIND`
  - `#TOT3`: `IX_TOT3_INPART_ORDSQ2`
  - `#製卡明細`: `IX_CARD_MAIN`
  - `#TEMP3`: `IX_TEMP3_INPART_ORDSQ2KEY`
- TEST 實測（單次 wall-clock）：
  - `@INPART='24X01008MT-0%'`：`104,577 ms`
  - `@INPART='23G%'`：`111,441 ms`
  - `@INPART='%'`：`343,962 ms`
- 效果：
  - `%` 由上一輪約 `442,892 ms` 下降至 `343,962 ms`（約再快 22%）。
  - 目前主要剩餘瓶頸轉為 `INSERT INTO ORDDE4_剩餘製程明細_D`（字串彙總段落）。

## 2026-04-02 針對 `ORDDE4_剩餘製程明細_D` 的字串預彙總（正式 SP）

- 直接修改 `產生ORDE3剩餘製程.sql`，不變更既有輸出欄位與商規條件，僅調整彙總計算位置：
  - 新增 `#TEMP3_字串彙總`：
    - 以 `#製卡明細` 的 `INPART` 為集合，預先計算
      - `剩餘製程明細`
      - `剩餘製程明細2`
      - `剩餘製程明細3`
      - `剩餘數`
    - 建立 `IX_TEMP3_STR_MAIN`（`INPART` 唯一聚集索引）。
  - `IF (@INPART='%' )` 與 `ELSE` 兩個 `INSERT INTO ORDDE4_剩餘製程明細_D` 分支：
    - 改由 `LEFT JOIN #TEMP3_字串彙總` 取用上述四個欄位。
    - 移除 `INSERT` 內對 `#TEMP3` 的重複相關子查詢。
- 設計考量：
  - 維持原本 `FOR XML PATH('')` 字串組裝邏輯與排序（`ORDER BY ROWID`），避免行為差異。
  - 先做「可加回滾的小步驟」：只搬移熱點計算位置，不改其餘欄位邏輯。

## 2026-04-02 雲端留存：單次效能實測

- 已新增測試紀錄檔：
  - `benchmark_results_2026-04-02.md`
- 單次 wall-clock（TEST）：
  - `24X01008MT-0%`: `356,571 ms`
  - `23G%`: `350,177 ms`
  - `%`: `345,210 ms`

## 2026-04-02 緊急修補：`INPART` 模稜兩可 / INSERT 欄位數不符

- 問題：
  - 在 `#TEMP3_字串彙總` 接入後，兩段 `INSERT INTO ORDDE4_剩餘製程明細_D` 仍用 `SELECT *`，且主查詢 `ORDER BY INPART` 未限定來源，導致：
    - `模稜兩可的資料行名稱 'INPART'`
    - `資料行名稱或提供的數值數量與資料表定義不相符`
- 修補：
  - 兩段主查詢改為 `SELECT #製卡明細.*`（避免把 `TMP3S` 欄位一併展開進 `*`）。
  - 兩段排序改為 `ORDER BY #製卡明細.INPART`（避免欄位名稱衝突）。
- 驗證：
  - 已部署 TEST 後以 `@INPART='24X01008MT-0%'` 直接執行，程序可完成，未再出現上述兩個錯誤。

## 2026-04-02 `%` 全量分段計時（TEST）

- 為定位 5~6 分鐘瓶頸，已在 SP 加入僅 `@INPART='%'` 啟用的 `[PERF]` 訊息點：
  - `START`
  - `BeforeSummaryInsert`
  - `AfterSummaryInsert`
  - `BeforeCommit`
- 本次單次實測（wall-clock）：
  - `TOTAL_MS = 334,707 ms`（約 5 分 35 秒）
- 分段結果：
  - `START -> BeforeSummaryInsert`: `122,104 ms`
  - `BeforeSummaryInsert -> AfterSummaryInsert`: `82,178 ms`
  - `AfterSummaryInsert -> BeforeCommit`: `113,499 ms`
  - `BeforeCommit -> 結束`: 約 `16,926 ms`（以 wall-clock 推估）
- 初步結論：
  - 目前主要耗時集中在「前處理 + 後處理」兩段（合計約 235 秒），不只 `ORDDE4_剩餘製程明細_D` 插入段落。

## 2026-04-02 `%` 全量細分里程碑（第二輪，TEST）

- `%` 單次 wall-clock：`349,985 ms`（約 5 分 50 秒）
- 里程碑：
  - `AfterTemp3Build total=16,020 ms`
  - `BeforeVerticalInsert total=36,856 ms`
  - `AfterVerticalInsert1 total=42,357 ms`
  - `AfterVerticalInsert2 total=42,903 ms`
  - `AfterDlytimeCore total=118,042 ms`
  - `BeforeSummaryInsert total=130,608 ms`
  - `AfterSummaryInsert total=204,956 ms`
  - `BeforeCommit total=328,395 ms`
- 主要區段耗時（delta）：
  - `AfterVerticalInsert2 -> AfterDlytimeCore`: `75,139 ms`
  - `BeforeSummaryInsert -> AfterSummaryInsert`: `74,348 ms`
  - `AfterSummaryInsert -> BeforeCommit`: `123,439 ms`（目前最大瓶頸）
