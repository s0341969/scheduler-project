# Changelog

## 2026-03-14 主要更新
- 新增 `OrgChartSystem` ASP.NET Core 8 專案。
- 導入 SQLite + EF Core，建立本地端資料儲存。
- 實作組織節點 API：查詢、新增、更新、刪除、上下移、變更上層。
- 實作顯示模式設定 API（person/code）。
- 實作 JSON 匯入與匯出 API。
- 建立前端單頁介面（繁體中文）：
  - 樹狀組織圖視覺化
  - 右側節點欄位編輯
  - 新增根/子節點、刪除、上移/下移
  - 匯入/匯出 JSON
  - 顯示模式切換
- 完成 `dotnet build` 與本地 API 冒煙測試（CRUD/排序/設定/匯出流程）。
