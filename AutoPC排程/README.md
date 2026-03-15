# AutoPc 自動排程（C# + MSSQL）

## 目標

此專案提供可落地執行的自動排程流程，採用：

- C# 負責排程運算
- MSSQL Stored Procedure 負責資料載入與寫回

## 目前資料表（依需求）

1. `WORKFIXM`
2. `機台基本資料`
3. `可排程工作`（已合併原 `ORD3STATUS`）
4. `指派時間`
5. `ORDDE4_剩餘製程明細`
6. `SOPNAME`（本版先保留，尚未納入演算法）

## 目前排程行為

1. 由 `usp_AutoPc_LoadSchedulingContext` 載入 4 組資料：
   - 機台能力（`MachineId` / `MachineGroup` / `DailyHours`）
   - 定品定機路由（`WORKFIXM`，僅 `MTYPE='1'`）
   - 可排程工作（`RequiredHours` / `DueDate` / `PriorityAvailableHours`）
   - 既有指派（`指派時間`）
2. C# 排程演算法：
   - 工作排序：先「可用工時（越小越優先）」、再「交期」、再「工時」
   - 機台選擇規則：
     - 先用 `INDWG` 比對；若找不到，再用 `PRDNAME` 比對（皆限定 `MTYPE='1'`）
     - 若 `MAHNO` 有值：排在指定機台
     - 若 `MAHNO` 無值：改用 `MAHNO_GP` 對應 `機台基本資料.精度組別`，從組內挑目前負荷最小機台
   - 每台機台起始時間：以 `指派時間` 該機台最後 `EndTime` 為基準；若無資料則從 08:00 開始
   - 每台機台每日可排工時：使用 `機台基本資料` 的「每日標準工時」
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
2. 啟動表單（預設模式）：

```powershell
dotnet run --project .\AutoPcScheduler\AutoPcScheduler.csproj
```

3. 在表單中按「開始排程」才會執行。
4. 表單可顯示：
   - 排程進度（進度條 + 已排/未排 + 目前製卡/製程/圖號）
   - 待排工作總數
   - 排程方法說明（WORKFIXM 路由 + 貪婪排程）
   - 新排程清單（GridView，含製程名稱）與未排入清單
5. 表單可查詢欄位：機台、圖號、製卡、製程（代碼/名稱）。
6. 排程採背景執行，按下「開始排程」後 UI 不會卡住。
7. 若要使用命令列模式，請加 `--cli`：

```powershell
dotnet run --project .\AutoPcScheduler\AutoPcScheduler.csproj -- --cli --dry-run
```

```powershell
dotnet run --project .\AutoPcScheduler\AutoPcScheduler.csproj -- --cli --plan-date=2026-03-15 --horizon-days=7 --assigner=AutoPc
```

## 重要假設與限制

- 由於 `TABLE資料表.xlsx` 有部分中文欄位編碼破損，SP 目前採「欄位名稱規則偵測」方式（例如包含「機台」「精度組別」「每日標準工時」）。
- `usp_AutoPc_LoadSchedulingContext` 的既有指派結果集會回傳各機台最後 `EndTime`（錨點），供排程起始時間判斷。
- Repository 會依機台每日工時分佈自動判斷時間單位（分鐘/小時），必要時將 `每日標準工時` 與 `WKTIME` 轉為小時後再排程。
- 若資料庫沒有 `機台基本資料`（或欄位不足），`usp_AutoPc_LoadSchedulingContext` 會 fallback 使用 `WORKFIXM` 產生機台，並以 `@DefaultDailyHours`（預設 8 小時）作為每日工時。
- `SOPNAME` 尚未納入排序/前後站約束邏輯，已列入 TODO。

## 專案結構

- `AutoPcScheduler/`：C# 排程主程式
  - `Program.cs`：預設啟動 WinForms；加 `--cli` 走命令列
  - `UI/SchedulerMainForm.cs`：排程表單、進度、查詢、GridView
  - `Services/AutoPcSchedulerEngine.cs`：排程引擎（機台挑選與進度回報）
- `sql/`：MSSQL Type / Stored Procedure
- `README.md`：重要決策與目前行為
- `CHANGELOG.md`：修改紀錄
- `TODO.md`：待辦事項
