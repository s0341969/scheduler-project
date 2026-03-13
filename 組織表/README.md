# 組織圖管理系統（C# + SQLite）

## 專案目標
建立一套可在本地端運行的組織圖系統，提供類似互動式組織圖網站的核心能力：
- 組織節點樹狀顯示
- 節點新增、編輯、刪除
- 同層上下排序
- 變更上層節點
- JSON 匯入與匯出
- 顯示模式切換（以人員為主 / 以代碼為主）

## 技術選型與重要決策
- 後端：ASP.NET Core 8 Minimal API
- 資料庫：SQLite（`OrgChartSystem/orgchart.db`）
- ORM：Entity Framework Core (Sqlite)
- 前端：原生 HTML/CSS/JavaScript（單頁互動）
- 初始化策略：啟動時 `EnsureCreated`，若資料表無資料則自動建立預設組織節點

## 目前功能行為
- API 路徑前綴：`/api/orgchart`
- `GET /api/orgchart`：取得完整樹狀資料與顯示模式
- `POST /api/orgchart/nodes`：新增節點（可指定 `parentId`）
- `PUT /api/orgchart/nodes/{id}`：更新節點內容與上層
- `DELETE /api/orgchart/nodes/{id}`：刪除節點（含所有子孫節點）
- `POST /api/orgchart/nodes/{id}/move`：同層上移/下移
- `PUT /api/orgchart/settings`：更新顯示模式（`person` / `code`）
- `GET /api/orgchart/export`：匯出 JSON
- `POST /api/orgchart/import`：匯入 JSON（覆蓋現有資料）

## 前端操作
- 可點選節點進入右側表單編輯
- 支援新增根節點、新增子節點、刪除、上下移
- 支援 JSON 檔匯入與下載匯出
- 介面文字預設繁體中文（zh-TW）

## 執行方式
1. 建置
   - `dotnet build OrgChartSystem/OrgChartSystem.csproj`
2. 啟動
   - `dotnet run --project OrgChartSystem/OrgChartSystem.csproj`
3. 開啟瀏覽器
   - `http://localhost:5000`（或終端顯示的實際網址）

## 專案結構
- `OrgChartSystem/Program.cs`：API 與應用程式入口
- `OrgChartSystem/Data/`：DbContext 與初始化
- `OrgChartSystem/Models/`：資料模型
- `OrgChartSystem/Services/OrgChartService.cs`：組織圖業務邏輯
- `OrgChartSystem/Contracts/`：API Request/Response DTO
- `OrgChartSystem/wwwroot/`：前端頁面與互動腳本
