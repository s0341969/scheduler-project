# SchedulerDragDrop（C# WPF）

## 目前行為

- 啟動程式後不會自動載入資料。
- 先選 `PRDOPGP` 群組（由資料表 `指派時間_TEMP` 的 `PRDOPGP` distinct 值載入）。
- 按下「查詢排程」才會讀取資料並建立排程。
- `PRDOPGP` 使用彈出視窗多選。

## 資料來源（單表）

- 只使用：`MIS.dbo.指派時間_TEMP`
- 不再使用：機台資料表、定品訂機表、工作表、可用狀態清單

## 欄位映射

- `輸出排序`：排序優先順序
- `INPART`：製卡
- `INDWG`：圖號
- `PDATE0`：交期
- `PRDNAME`：製程
- `PRDOPGP`：製程群組（查詢條件）
- `Applier`：機台（可用 `;` 分隔）
- `WKTIME`：工時分鐘（僅用此欄，不乘 QTY）
- `QTY`：數量（顯示用途）
- `新ORDSQ2`：製程排序

## 排程規則

1. 依 `輸出排序` 由小到大優先排程。
2. 同製卡依 `新ORDSQ2` 維持先後順序。
3. `Applier` 單機台：固定該機台。
4. `Applier` 多機台（`;`）：自動選負荷最小（最早可用）機台。

## 系統架構

- UI 層（WPF）
- `MainWindow.xaml` / `MainWindow.xaml.cs`：主畫面、拖拉、多選、右鍵推移、時間軸、進度顯示。
- `ProcessGroupSelectionDialog`：PRDOPGP 多選視窗。
- `ShiftToDateTimeDialog`：推移到指定日期時段視窗。

- 資料存取層
- `DatabaseScheduleLoader.cs`：從 MSSQL `MIS.dbo.指派時間_TEMP` 載入與轉換資料。
- `SchedulerConfigProvider.dll`：連線字串與欄位設定提供者。

- 排程引擎層
- `SchedulerEngine.cs`：自動排程、重建排程、機台負荷選擇、製程序約束。
- 拖拉放置採局部後推：只推移同機台重疊與後續工件。
- 同製卡製程序約束：前段不可晚於後段；移動前段時後段自動後推。

- 模型層
- `Models.cs`：`WorkOrder`、`Machine`、`ScheduleItem`、`TaskCard`、`MachineLane` 等資料模型。

### 執行流程（高層）

1. 啟動程式（UI 先載入）。
2. 選擇 PRDOPGP 群組。
3. 按「查詢排程」後載入資料。
4. 由 `SchedulerEngine` 計算初始排程。
5. UI 以時間軸與機台欄位呈現卡片。
6. 拖拉/推移時以局部更新避免整批重算造成卡頓。

## 效能與互動更新

- 查詢排程改為非同步背景執行，避免查詢時 UI 卡住。
- 上方顯示進度列、百分比、目前製卡/製程/機台，並支援中止查詢。
- 時間軸預設高度為 24px/小時（可調）。
- 滑桿改為節流更新，降低拖動時卡頓。
- 單一製程推移採快速路徑，避免每次整批重算。
- 拖拉至任一時間點時，卡片依放置時間定位；同機台重疊工作會自動後推。
- 拖拉到其他機台時，原機台後續工作維持原時間，不會自動前移。
- 時間軸刷新使用保留範圍模式，避免操作時出現縮放跳動感。

## 啟動穩定性

- 啟動時先顯示主畫面，再載入群組資料。
- 啟動診斷日誌：`%LOCALAPPDATA%\SchedulerDragDrop\startup_diagnostic.log`

## 啟動

```powershell
cd C:\codex_pg\SchedulerDragDrop
dotnet run
```
