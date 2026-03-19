# Changelog

## 2026-03-19
- 新增 `Pur2019Migration.sln` 與 `PUR2019.WinForms` 專案。
- 建立 WinForms 轉換骨架：`MainMenuForm`、`PurchaseMainForm`、`PurchaseDetailForm`、`PurchaseCheckForm`、`PurchaseAdminForm`、`UtilityForm`。
- 建立採購模型與服務層：`PurchaseOrderHeader`、`PurchaseOrderLine`、`IPurchaseOrderService`、`InMemoryPurchaseOrderService`。
- 設定程式文化為 `zh-TW`，UI 文字採繁體中文。
- 新增 `.gitignore`，排除 `bin/obj` 與 `.user`，並移除已追蹤的建置產物。
- 新增 `appsettings.json` 與 `AppSettings` 載入器，支援設定檔/環境變數切換資料來源。
- 新增 `OdbcPurchaseOrderService`（對應 Delphi `PURTM/PURTD` 查詢邏輯）。
- 主選單新增資料來源顯示，啟動時若資料來源初始化失敗會自動 fallback 到 InMemory。
