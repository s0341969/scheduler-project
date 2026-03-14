# 組織圖管理系統（C# + SQLite）

## 專案目標
建立一套可在本地端運行的組織圖系統，提供互動式組織圖管理能力：
- 組織節點樹狀顯示
- 節點新增、編輯、刪除
- 同層排序與拖放調整層級
- JSON 匯入與匯出
- 顯示模式切換（以人員為主 / 以代碼為主）
- 匯入預覽、版本快照、備份還原
- 權限控管（唯讀 / 編輯）與審計紀錄

## 技術選型與重要決策
- 後端：ASP.NET Core 8 Minimal API
- 資料庫：SQLite（`OrgChartSystem/orgchart.db`）
- ORM：Entity Framework Core (Sqlite)
- 前端：原生 HTML/CSS/JavaScript（單頁互動）
- 初始化策略：啟動時 `EnsureCreated`，並補齊快照/審計資料表
- 自動備份：背景服務依設定定期建立 SQLite 檔案備份

## 權限模型
透過 Header 控制 API 存取：
- `X-OrgChart-Role`：`viewer` 或 `editor`
- `X-OrgChart-Key`：對應角色金鑰
- `X-OrgChart-Actor`：操作者名稱（審計紀錄用）

預設金鑰（可在 `appsettings.json` 修改）：
- `Auth:ReadKey = viewer123`
- `Auth:EditKey = editor123`

## 目前功能行為
- API 路徑前綴：`/api/orgchart`
- `GET /api/orgchart`：取得完整樹狀資料與顯示模式
- `GET /api/orgchart/search?q=...&limit=...`：依部門/人員/代碼等欄位搜尋
- `POST /api/orgchart/nodes`：新增節點
- `PUT /api/orgchart/nodes/{id}`：更新節點內容與上層
- `DELETE /api/orgchart/nodes/{id}`：刪除節點（含所有子孫節點）
- `POST /api/orgchart/nodes/{id}/move`：同層上移/下移
- `POST /api/orgchart/nodes/{id}/reposition`：拖放調整節點上層與排序
- `PUT /api/orgchart/settings`：更新顯示模式（`person` / `code`）
- `GET /api/orgchart/export`：匯出 JSON
- `POST /api/orgchart/import/preview`：匯入預覽（節點數、深度、警示）
- `POST /api/orgchart/import`：匯入 JSON（覆蓋現有資料，且先自動快照）
- `GET /api/orgchart/snapshots`：查詢快照
- `POST /api/orgchart/snapshots`：手動建立快照
- `POST /api/orgchart/snapshots/{id}/restore`：回復快照
- `GET /api/orgchart/backups`：查詢備份檔
- `POST /api/orgchart/backups`：手動建立資料庫備份
- `POST /api/orgchart/backups/restore`：還原資料庫備份（還原前會先自動備份）
- `GET /api/orgchart/audits`：查詢審計紀錄

## 前端操作
- 可點選節點進入右側表單編輯
- 支援拖拉節點調整位置：
  - 拖到節點上方：移到同層前方
  - 拖到節點中央：成為該節點子節點
  - 拖到節點下方：移到同層後方
  - 拖到空白區：移到根層末端
- 支援搜尋節點並高亮結果
- 支援匯入預覽確認
- 支援快照建立/回復與備份建立/還原
- 支援在前端輸入角色/金鑰/操作者
- 介面文字預設繁體中文（zh-TW）

## 備份設定
`appsettings.json`：
- `Backup:Directory`：備份目錄（預設 `backups`）
- `Backup:AutoEnabled`：是否啟用自動備份（預設 `true`）
- `Backup:AutoIntervalHours`：備份間隔小時（預設 `24`）

## 測試
- 新增 `OrgChartSystem.Tests`（xUnit）
- 已覆蓋：
  - 同層重排
  - 防止移到自己子孫
  - 搜尋命中
  - 匯入預覽統計

## 執行方式
1. 建置
   - `dotnet build OrgChartSystem/OrgChartSystem.csproj`
2. 啟動
   - `dotnet run --project OrgChartSystem/OrgChartSystem.csproj`
3. 測試
   - `dotnet test OrgChartSystem.Tests/OrgChartSystem.Tests.csproj`
4. 開啟瀏覽器
   - `http://localhost:5000`（或終端顯示的實際網址）

## 專案結構
- `OrgChartSystem/Program.cs`：API 與應用程式入口（含權限檢查）
- `OrgChartSystem/Data/`：DbContext 與初始化
- `OrgChartSystem/Models/`：資料模型（含快照/審計）
- `OrgChartSystem/Services/OrgChartService.cs`：組織圖業務邏輯
- `OrgChartSystem/Contracts/`：API Request/Response DTO
- `OrgChartSystem/wwwroot/`：前端頁面與互動腳本
- `OrgChartSystem.Tests/`：xUnit 測試專案
