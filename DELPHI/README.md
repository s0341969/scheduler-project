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
- 儲存流程使用交易（transaction）保護，避免單頭單身狀態不一致。

## 資料來源設定
預設為 `InMemory`。可切換為 `Database` 使用 ODBC。

設定檔：`PUR2019.WinForms/appsettings.json`
```json
{
  "DataSource": {
    "Mode": "InMemory",
    "OdbcConnectionString": "Driver={SQL Server};Server=127.0.0.1;Database=MISD;Uid=sa;Pwd=CHANGE_ME;TrustServerCertificate=Yes;"
  }
}
```

也可用環境變數覆蓋：
- `PUR2019_DATA_SOURCE`：`InMemory` 或 `Database`
- `PUR2019_ODBC_CONNECTION_STRING`：ODBC 連線字串

## 建置
```powershell
dotnet build
```

## 轉換策略
- 採 additive 方式進行，不破壞現有 Delphi 專案。
- 先完成 UI 與服務層解耦，再逐步將 `PUR2019P.pas`、`PUR2019AP.pas`、`Utility.pas` 商業邏輯搬移。
- 每次搬移以可編譯、可測試為單位提交。

## 目前限制
- 目前尚未完整移植 `PUR2019P` 的進階檢核（如 ORDMENO/PURDEL 特殊規則、SP 檢核、預算檢核）。
- 尚未完整映射 Delphi `.dfm` 上所有控制項與事件邏輯。
