# Changelog

## 2026-03-19
- 新增 `Pur2019Migration.sln` 與 `PUR2019.WinForms` 專案。
- 建立 WinForms 轉換骨架：`MainMenuForm`、`PurchaseMainForm`、`PurchaseDetailForm`、`PurchaseCheckForm`、`PurchaseAdminForm`、`UtilityForm`。
- 建立採購模型與服務層：`PurchaseOrderHeader`、`PurchaseOrderLine`、`IPurchaseOrderService`、`InMemoryPurchaseOrderService`。
- 設定程式文化為 `zh-TW`，UI 文字採繁體中文。
- 新增 .gitignore，排除 bin/obj 與 .user，並移除已追蹤的建置產物。
