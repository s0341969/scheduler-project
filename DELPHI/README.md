# DELPHI 專案轉換狀態

## 專案目標
將 `G:\GON\GON\PUR2019` 的 Delphi 採購系統逐步移植為 C# WinForms，先建立可執行骨架，再逐步搬移商業邏輯與資料庫查詢。

## 目前完成
- 建立 `Pur2019Migration.sln`
- 建立 `PUR2019.WinForms` (.NET 9 WinForms)
- 建立主入口與主選單（對應 Delphi 的 `PUR2019.dpr` / `PUR2019A.dpr`）
- 建立採購主畫面、明細畫面、異常檢查畫面、管理畫面、Utility 畫面
- 建立 `IPurchaseOrderService` 與 `InMemoryPurchaseOrderService`，可支援查詢與畫面互動

## 轉換策略
- 採 additive 方式進行，不破壞現有 Delphi 專案。
- 先完成 UI 與服務層解耦，再逐步將 `PUR2019P.pas`、`PUR2019AP.pas`、`Utility.pas` 商業邏輯搬移。
- 每次搬移以可編譯、可測試為單位提交。

## 建置
```powershell
dotnet build
```

## 目前限制
- 尚未接入實際資料庫（目前使用記憶體資料服務）。
- 尚未完整映射 Delphi `.dfm` 上所有控制項與事件邏輯。
