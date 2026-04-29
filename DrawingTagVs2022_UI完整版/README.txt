DrawingTagVs2022 - 圖面檢驗標註工具 Phase 1 最新版
======================================================

這是一個可用 Visual Studio 2022 直接開啟的 ASP.NET Core 專案。

本版重點：
1. 保留目前 UX 強化版 HTML 前端。
2. 左鍵按住拖拉平移，左鍵按下放開且未拖曳才新增定位。
3. 功能列可隱藏，讓 PDF 定位工作區更大。
4. API 已對接你的 Stored Procedure：dbo.KNV10256_SIP量測表。
5. 載入規格時會帶入三個參數：圖號、版次、項目。
6. 項目支援：首件 / 製程中 / 終檢。
7. 右側 GridView 顯示 Stored Procedure 回傳的所有欄位。
8. API 將圖面與標註點存入 MSSQL，並自動產生版本。
9. API 可載入指定圖號最新版本。

開啟方式：
1. 解壓縮 ZIP。
2. 用 Visual Studio 2022 開啟 DrawingTagVs2022.sln。
3. 修改 DrawingTagWeb\appsettings.json 的 DefaultConnection。
4. 執行 DrawingTagWeb 專案。
5. 瀏覽器會開啟 http://localhost:5088。

第一次使用：
1. 在 SSMS 執行 DrawingTagWeb\Sql\01_CreateTables.sql。
   用途：建立 DrawingProject / DrawingTag，用來存圖面版本與標註座標。
2. 如果你正式資料庫已經有 dbo.KNV10256_SIP量測表，不需要執行 02_SampleStoredProcedure.sql。
   02_SampleStoredProcedure.sql 只放說明與測試範例。
3. 將 4 個離線 JS 檔放到 DrawingTagWeb\wwwroot\lib。

需要的離線 JS：
- xlsx.full.min.js
- html2pdf.bundle.min.js
- pdf.min.js
- pdf.worker.min.js

Stored Procedure 對接：
預設 API 會呼叫：dbo.KNV10256_SIP量測表

SP 參數對應：
- 前端圖號 -> API indwg  -> SP @INDWG   VARCHAR(30)
- 前端版次 -> API dwgrev -> SP @DWGREV  VARCHAR(10)
- 前端項目 -> API option -> SP @OPTION  VARCHAR(10)

前端 API 路徑：
GET /api/drawing-spec?indwg=M1234&dwgrev=A&option=首件

SP 回傳欄位至少需包含：
- ItemNo 或 項次
- InspectionMethod 或 檢驗方式

其他欄位會直接顯示在右側 GridView。

注意：
1. 如果 @OPTION 中文出現亂碼，建議把 SP 的 @OPTION 改成 NVARCHAR(10)。
2. 如果 SP 回傳欄位名稱不同，請在 SP SELECT 裡加別名，例如：
   SELECT 項次 AS ItemNo, 檢驗方式 AS InspectionMethod, ...
3. 如果公司內網完全不能上網，第一次建置時 NuGet 套件 Microsoft.Data.SqlClient 需要先在有網路環境還原，或使用公司內部 NuGet 來源。

2026-04-29 更新：已改為正式串接 dbo.KNV10256_SIP量測表，參數為 @INDWG / @DWGREV / @OPTION。

2026-04-29 更新：EQUIP 套色規則
--------------------------------
本版已將 Stored Procedure 回傳的 EQUIP 欄位納入套色來源：
- EQUIP = CMM → 綠色
- EQUIP = PM  → 黃色
- EQUIP = GI  → 藍色
- EQUIP = HM  → 紅色

右側 GridView 仍會完整顯示 dbo.KNV10256_SIP量測表 回傳的所有欄位。
前端套色抓取順序：EQUIP / InspectionMethod / 檢驗方式 / METHOD。


本版修正：泡泡內只顯示序號；點擊圖面泡泡時，右側 GridView 會自動選取並捲動到對應的泡泡規格資料列。
