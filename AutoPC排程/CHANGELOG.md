# Changelog

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
