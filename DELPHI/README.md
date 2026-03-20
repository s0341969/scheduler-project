# DELPHI 專案轉換狀態

## 專案目標
將 `G:\GON\GON\PUR2019` 的 Delphi 採購系統逐步移植為 C# WinForms，先建立可執行骨架，再逐步搬移商業邏輯與資料庫查詢。

## 目前完成
- 建立 `Pur2019Migration.sln`
- 建立 `PUR2019.WinForms` (.NET 9 WinForms)
- 建立主入口與主選單（對應 Delphi 的 `PUR2019.dpr` / `PUR2019A.dpr`）
- 建立採購主畫面、明細畫面、異常檢查畫面、管理畫面、Utility 畫面
- 建立可切換資料來源的服務層：
  - `InMemoryPurchaseOrderService`
  - `OdbcPurchaseOrderService`
- 已對應 Delphi `PUR2019P` 主要流程：
  - 查詢：`PURTM` (`PURTP='0'`) + `PURTD` (`PURNO`)
  - 單頭：新增、儲存、刪除、確認、取消確認、作廢
  - 單身：新增、刪除
- 進階邏輯第一批：
  - `PURDEL` 防護：已有發料關聯時禁止取消確認
  - `ORDMENO.MPCHK` 同步：單身新增/刪除時同步更新
  - `PUPRP`（製令單號）欄位已導入 UI 與服務層
  - 可選啟用 Legacy SP 檢核（確認時）
- 進階邏輯第二批：
  - `PUPA1/PUPA2` 製程區間輸入與驗證（支援 `0-0`、`99-99`、一般區間）
  - `ORDDE4` 參考金額 (`PURM2`) 計算
  - 成本比 (`PURP2`) 自動計算
  - MOQ 讀取與最小採購量檢核
- UI/操作一致性第三批：
  - 主畫面補齊 `F6 確認`（按鈕與快捷鍵）
  - 主畫面補齊「刪除單頭」操作入口
  - 關閉畫面前加入單頭未存檔提示（離開前可選擇先儲存）
  - 單身視窗新增即時計算金額預覽與 MOQ 提示
- 儲存流程使用交易（transaction）保護，避免單頭單身狀態不一致。

## 資料來源設定
預設為 `InMemory`。可切換為 `Database` 使用 ODBC。

設定檔：`PUR2019.WinForms/appsettings.json`
```json
{
  "DataSource": {
    "Mode": "InMemory",
    "OdbcConnectionString": "Driver={SQL Server};Server=127.0.0.1;Database=MISD;Uid=sa;Pwd=CHANGE_ME;TrustServerCertificate=Yes;",
    "EnableLegacyStoredProcedureChecks": false
  }
}
```

也可用環境變數覆蓋：
- `PUR2019_DATA_SOURCE`：`InMemory` 或 `Database`
- `PUR2019_ODBC_CONNECTION_STRING`：ODBC 連線字串
- `PUR2019_ENABLE_LEGACY_SP_CHECKS`：`true/false`

## 建置
```powershell
dotnet build
```

## 轉換策略
- 採 additive 方式進行，不破壞現有 Delphi 專案。
- 先完成 UI 與服務層解耦，再逐步將 `PUR2019P.pas`、`PUR2019AP.pas`、`Utility.pas` 商業邏輯搬移。
- 每次搬移以可編譯、可測試為單位提交。

## 目前限制
- 尚未完整移植 `PUR2019P` 全部欄位事件（如更細欄位連動與 Delphi 報表 SQL）。
- 尚未完整映射 Delphi `.dfm` 上所有控制項與事件邏輯。

## UI 現況
- PUR2019F 已改為接近 Delphi 舊版排版（功能列、頭資料區、明細大表格）。
- 按鍵命名保留 F2/F3/F5/F8/F9/F10/F11/F12 風格。

## 功能等價追蹤
- 新增對照矩陣：[PUR2019P_PARITY_MATRIX.md](G:\\codex_pg\\DELPHI\\PUR2019P_PARITY_MATRIX.md)
- 以 Delphi TPUR2019F procedure 清單逐項對照 Y/P/N 狀態。
- F2~F12 已對齊快捷鍵操作。
