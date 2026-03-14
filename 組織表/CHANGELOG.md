# Changelog
## 2026-03-14 拖放調整組織功能
- 新增節點重排 API：POST /api/orgchart/nodes/{id}/reposition，可一次調整上層與排序索引。
- 後端 OrgChartService 新增重排邏輯：
  - 支援同層任意位置重排。
  - 支援跨層移動與插入指定索引。
  - 維持各層 SortOrder 連續重編，並更新 UpdatedAtUtc。
- 前端樹狀圖新增拖拉操作：
  - 拖到節點上方：插入同層前方。
  - 拖到節點中央：成為子節點。
  - 拖到節點下方：插入同層後方。
  - 拖曳時顯示放置提示樣式，並保留原有編輯與上下移按鈕功能。
- 編輯面板操作提示文字更新，加入拖拉規則說明。

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

## 2026-03-14 連線修正
- 修正前端 `Failed to fetch` 情境：
  - 新增 API 位址自動偵測與錯誤提示。
  - 支援用 `?apiBase=...` 手動指定 API 端點。
  - 調整 `fetch` header 行為，避免不必要預檢。
  - 增強非 JSON 錯誤回應處理。
- 後端新增 CORS 設定，允許跨來源呼叫 API。
- 完成驗證：`dotnet build`、API 冒煙測試、CORS 預檢（`Access-Control-Allow-Origin: *`）。

