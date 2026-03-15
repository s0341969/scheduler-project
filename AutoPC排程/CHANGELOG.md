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
