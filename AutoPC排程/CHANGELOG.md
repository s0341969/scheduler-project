# Changelog

## 2026-03-15 (排程規則與效能)

- 排程引擎改為依 `WORKFIXM` 路由規則選機：
  - 僅使用 `MTYPE='1'` 的路由。
  - 比對順序改為「先 `INDWG`，找不到再 `PRDNAME`」。
  - `MAHNO` 有值時固定排該機台。
  - `MAHNO` 無值時改以 `MAHNO_GP` 對應 `機台基本資料.精度組別`，挑選組內最小負荷機台。
- 每台機台排程起始時間改為參考 `指派時間` 的最後 `EndTime`；若該機台無資料，從 08:00 開始。
- 機台每日工時優先取 `機台基本資料` 的「每日標準工時」欄位（SP 欄位偵測優先序已調整）。
- 新增工時單位自動正規化：若 DB 值為分鐘，會在 Repository 轉為小時後再排程（套用 `每日標準工時` / `WKTIME` / `PriorityAvailableHours`）。
- WinForms 排程進度文字新增「製卡 / 製程 / 圖號」。
- WinForms 開始排程流程調整為先刷新畫面再進入排程，並節流進度更新以降低卡頓。
- `usp_AutoPc_LoadSchedulingContext` 更新：
  - 路由結果集改為輸出 `WORKFIXM` 且限定 `MTYPE='1'`。
  - 既有指派結果集改為回傳各機台最後 `EndTime`（錨點）供起始時間判斷，避免大量歷史資料造成卡頓。
- 實測 `MIS`：`--dry-run` 可完成（`新排程 43517`、`未排入 448`）。

## 2026-03-15 (UI-功能擴充)

- 表單新增排程進度顯示：進度條、已排/未排統計、目前處理工單。
- 表單新增待排工作總數顯示。
- 表單新增排程方法說明（貪婪法）。
- 表單新增查詢條件：機台、圖號、製卡、製程（可在 GridView 篩選）。
- 新排程 GridView 新增製程名稱欄位。
- 製程查詢支援代碼與製程名稱。
- `AutoPcSchedulerEngine` 新增進度回報模型與回呼介面。

## 2026-03-15 (UI)

- `AutoPcScheduler` 新增 WinForms 介面，預設啟動為表單模式。
- 表單新增「開始排程」按鈕，使用者按下後才執行排程。
- 新增 GridView 顯示新排程清單，並增加未排入工作清單。
- `Program.cs` 保留命令列模式，加入 `--cli` 參數切換。
- `AutoPcScheduler.csproj` 調整為 `net8.0-windows` 並啟用 `UseWindowsForms`。

## 2026-03-15

- 新增 `AutoPcScheduler` C# 專案，建立第一版排程引擎（貪婪分派、可跨天切段）。
- 新增 `ISchedulingRepository` + `SqlSchedulingRepository`，以 SP 載入排程資料並批次寫入。
- 新增 SQL 腳本：
  - `001_CreateType_AutoPcAssignmentTvp.sql`
  - `002_CreateOrAlter_usp_AutoPc_LoadSchedulingContext.sql`
  - `003_CreateOrAlter_usp_AutoPc_SaveAssignments.sql`
- 新增 `.gitignore`，排除 Excel 解析過程的暫存檔。
- 建立文件：`README.md`、`CHANGELOG.md`、`TODO.md`。

## 2026-03-15 (修正)

- 修正 `usp_AutoPc_LoadSchedulingContext`：當 `機台基本資料` 不存在時，改為 fallback 使用 `WORKFIXM` 推導機台清單。
- 新增 `@DefaultDailyHours` 參數（預設 `8`），供 fallback 機台能力使用。
- 實測 `MIS` 環境可連線，並確認 `可排程工作` 為 view、`指派時間` 與 `WORKFIXM` 存在。
- 修正 `usp_AutoPc_LoadSchedulingContext` 移除 `TRY_CONVERT`，改為 SQL Server 2008 相容寫法（`CAST` / `ISNUMERIC` / `ISDATE`）。
