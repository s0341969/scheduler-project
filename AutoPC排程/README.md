# AutoPc 自動排程（C# + MSSQL）

## 目標

此專案提供第一版「可落地執行」的自動排程流程，採用：

- C# 負責排程運算
- MSSQL Stored Procedure 負責資料載入與寫回

## 目前資料表（依需求）

1. `WORKFIXM`
2. `機台基本資料`
3. `可排程工作`（已合併原 `ORD3STATUS`）
4. `指派時間`
5. `ORDDE4_剩餘製程明細`
6. `SOPNAME`（本版先保留，尚未納入演算法）

## 第一版排程行為

1. 由 `usp_AutoPc_LoadSchedulingContext` 載入 4 組資料：
   - 機台能力（MachineId / DailyHours / ProcessCode）
   - 定品定機路由（WORKFIXM）
   - 可排程工作（RequiredHours / DueDate / PriorityAvailableHours）
   - 既有指派（避免重疊）
2. C# 使用貪婪排程：
   - 先按「可用工時（越小越優先）」、再按「交期」、再按「工時」排序
   - 每筆工作先找候選機台，再選擇最早完工的機台
   - 支援跨天切段（同一工作可能拆成多筆指派）
3. 寫入 `指派時間`：
   - 透過 TVP + `usp_AutoPc_SaveAssignments` 批次寫入

## SQL 腳本

請依序執行：

1. [`sql/001_CreateType_AutoPcAssignmentTvp.sql`](sql/001_CreateType_AutoPcAssignmentTvp.sql)
2. [`sql/002_CreateOrAlter_usp_AutoPc_LoadSchedulingContext.sql`](sql/002_CreateOrAlter_usp_AutoPc_LoadSchedulingContext.sql)
3. [`sql/003_CreateOrAlter_usp_AutoPc_SaveAssignments.sql`](sql/003_CreateOrAlter_usp_AutoPc_SaveAssignments.sql)

## 執行方式

1. 設定連線字串環境變數：`AUTO_PC_CONN`
2. Dry run（不寫入 DB）：

```powershell
dotnet run --project .\AutoPcScheduler\AutoPcScheduler.csproj -- --dry-run
```

3. 正式排程寫入：

```powershell
dotnet run --project .\AutoPcScheduler\AutoPcScheduler.csproj -- --plan-date=2026-03-15 --horizon-days=7 --assigner=AutoPc
```

## 重要假設與限制

- 由於 `TABLE資料表.xlsx` 有部分中文欄位編碼破損，SP 針對中文欄位採「欄位名稱規則偵測」方式（例如包含「機台」「工時」「製程」）。
- 若資料庫實際欄位名稱與偵測規則不一致，請調整 SQL 腳本中的欄位偵測條件。
- `SOPNAME` 尚未納入第一版排序/路由邏輯，已列入 TODO。

## 專案結構

- `AutoPcScheduler/`：C# 排程主程式
- `sql/`：MSSQL Type / Stored Procedure
- `README.md`：重要決策與目前行為
- `CHANGELOG.md`：修改紀錄
- `TODO.md`：待辦事項
