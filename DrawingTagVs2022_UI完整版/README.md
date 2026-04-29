# DrawingTagVs2022 UI 完整版

## 專案用途

這是一個以 ASP.NET Core 8 建立的圖面檢驗標註工具，提供以下能力：

- 以瀏覽器載入 PDF、JPG、PNG 圖面。
- 依圖號、版次、規格階段呼叫 Stored Procedure 取得檢驗規格資料。
- 在圖面上建立、拖曳、刪除泡泡標註。
- 將標註資料、圖面內容、縮放狀態存入 MSSQL 做版本控管。
- 匯出完整圖面或單一量測設備圖層的 PDF。

## 目前架構

- Solution: `DrawingTagVs2022.sln`
- Web 專案: `DrawingTagWeb`
- 後端: ASP.NET Core 8 Web API
- 前端: 單一 `wwwroot/index.html`
- 資料庫: MSSQL
- 主要套件: `Microsoft.Data.SqlClient`

## Stored Procedure 規格

前端按下「從資料庫載入規格」後，會呼叫：

- `GET /api/drawing-spec?indwg={圖號}&dwgrev={版次}&option={規格}`

後端會執行 `StoredProcedures:GetDrawingSpec` 設定值，預設為：

- `dbo.KNV10256_SIP量測表`

### 第一個結果集

第一個結果集代表規格資料。系統會從欄位中辨識：

- 項次欄位：`ItemNo`、`項次`、`序號` 等同義名稱
- 量測設備欄位：`EQUIP`、`InspectionMethod`、`檢驗方式`、`METHOD` 等同義名稱

右側 Grid 目前只顯示：

- 泡泡
- 量測設備

但原始結果列仍保留在前端記憶體中，供套色與後續擴充使用。

### 第二個結果集

第二個結果集代表 PDF 路徑清單，供前端下拉選取。系統可辨識以下兩種回傳方式：

1. 直接回傳完整路徑欄位，例如：
   - `PdfPath`
   - `FilePath`
   - `FullPath`
   - `圖檔完整路徑`
   - `PDF路徑`
2. 分開回傳資料夾與檔名欄位，例如：
   - 路徑欄位：`DirectoryPath`、`FolderPath`、`圖檔路徑`
   - 檔名欄位：`PdfFileName`、`FileName`、`圖檔檔名`

若第二個結果集使用你目前的格式：

- `圖檔路徑`
- `檔名`

系統會自動組成 `圖檔路徑 + 檔名` 的完整 PDF 路徑。

前端載入規格後，會把第二個結果集顯示在「資料庫 PDF 路徑」下拉選單，使用者可選擇其中一筆並按下「載入資料庫 PDF」。
下拉選單只顯示檔名，不顯示完整路徑。

## PDF 載入方式

瀏覽器無法直接安全讀取資料庫回傳的本機或網路磁碟路徑，因此本專案新增後端代理端點：

- `GET /api/drawing-spec/pdf-file?path={完整PDF路徑}`

這個端點會由後端主機讀取 PDF 並串流回瀏覽器，再由前端擷取首頁轉成圖面背景。

注意事項：

- 後端主機必須有權限存取該 PDF 路徑。
- 目前僅允許 `.pdf` 副檔名。
- 若是網路分享路徑，執行網站的 Windows 帳號必須具備讀取權限。

## 資料表

初始化腳本位於：

- `DrawingTagWeb/Sql/01_CreateTables.sql`

主要資料表：

- `dbo.DrawingProject`
- `dbo.DrawingTag`

用途如下：

- `DrawingProject`：保存圖號、版本、圖面 Base64、規格快照、縮放資訊
- `DrawingTag`：保存每個泡泡的項次、量測設備、X/Y 座標

## 執行方式

1. 先在 MSSQL 執行 `DrawingTagWeb/Sql/01_CreateTables.sql`
2. 修改 `DrawingTagWeb/appsettings.json` 的 `ConnectionStrings:DefaultConnection`
3. 確認 `wwwroot/lib` 已放入離線 JS 檔
4. 執行：

```powershell
dotnet build
dotnet run --project .\DrawingTagWeb\DrawingTagWeb.csproj
```

預設開發網址：

- `http://localhost:5088`

## 離線前端相依檔

請放入 `DrawingTagWeb/wwwroot/lib`：

- `xlsx.full.min.js`
- `html2pdf.bundle.min.js`
- `pdf.min.js`
- `pdf.worker.min.js`

## 目前重要行為

- 功能列順序目前為：`1. 圖面建檔/規格查詢 → 2. 載入 PDF/圖面 → 3. 定義座標 → 4. 備用功能 → 5. 製程圖層獨立匯出 → 6. 專案全域操作`
- `載入 PDF / 圖面` 區塊目前為獨立第二步驟，位於規格查詢與座標定義之間，並使用較窄欄寬避免佔用過多空間。
- 第 2 步中的本機檔案挑選區預設收合，資料庫 PDF 路徑選取維持直接可見。
- 左鍵按住拖曳：平移圖面
- 左鍵按住後放開且未拖曳：新增泡泡
- 泡泡左鍵拖曳：移動泡泡
- 泡泡右鍵：刪除泡泡
- 載入規格後會依 `EQUIP` 自動套色
- `EQUIP` 色碼規則：
  - `CMM`：綠色
  - `PM`：黃色
  - `GI`：藍色
  - `HM`：紅色
  - 其他：灰色

## 已知限制

- 前端目前仍集中在單一 `index.html`，維護成本偏高。
- `DrawingProject.ImageBase64` 直接保存圖面內容，資料量大時會增加 DB 壓力。
- `appsettings.json` 目前仍使用明文連線字串，不適合長期保留在版控。
