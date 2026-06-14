# Changelog

## 2026-06-09

- 修正網站啟動時未將 `runtimes\win-x64\native` 與 `x64/x86` 納入 native DLL 搜尋路徑，導致執行「分析目前圖面」時 `OpenCvSharpExtern.dll` 無法載入，跳出 `TypeInitializationException`
- 在 `DrawingTagWeb/Program.cs` 啟動流程加入 native library search path 補強，避免 OpenCV 初始化失敗
- 修正實際使用中的 `run_web.bat` 啟動器，於組合 `publish_output` 與 Release runtime 後，先將 `runtimes\win-x64\native`、`runtimes\win-x86\native`、`x64`、`x86` 加入 `PATH`，確保目前網站執行版本也能正確載入 OpenCV/Tesseract native DLL
- 由官方 `OpenCvSharp4.runtime.win 4.10.0.20240616` 套件補回缺失的 `OpenCvSharpExtern.dll` 與 `opencv_videoio_ffmpeg4100*.dll`，並寫入 `publish_output\runtimes` 與 `DrawingTagWeb\bin\Release\net8.0\win-x64\runtimes`
- 調亮深色主題下「載入 PDF / 圖面」區塊的標題、`挑選檔案`、資料庫 PDF 路徑與提示文字，避免黑底時辨識度不足
- 取消前端 PDF 固定 10 頁限制，改為頁數不限、以總像素上限與自動降解析度保護載入效能，避免 16 頁以上 PDF 直接被拒絕
- 修正前端載入 `/api/system-info` 後會被後端 `pdfLoading.maxPagesToRender=10` 覆蓋的問題，現在固定忽略後端頁數限制，只保留像素上限與解析度設定
- 收斂網站啟動入口，保留根目錄 `run_web.bat` 為唯一固定入口，移除 `DrawingTagWeb\run_web.bat`，避免混用
- 新增 `DrawingTypeScanner\run_scanner.bat`，可直接以前景模式執行現有 Release 版 `DrawingTypeScanner.exe`
- `DrawingTypeScanner\run_scanner.bat` 執行前會先從 `appsettings.json` 顯示掃描路徑、搜尋條件、遞迴設定、平行度、批次大小、SQL timeout 與遮罩後的資料庫連線摘要
- 系統版次提升為 `V2026.04.29.05`

## 2026-04-29 16:41

- 調整規格查詢 API，支援 Stored Procedure 兩個結果集：
  - 第一個結果集為規格資料
  - 第二個結果集為 PDF 路徑清單
- 新增後端 PDF 串流端點，讓瀏覽器可透過網站載入資料庫回傳的 PDF 路徑
- 前端新增資料庫 PDF 下拉選單與載入按鈕
- 前端在載入規格後會同步顯示可選的資料庫 PDF 數量
- 補齊 `README.md`、`CHANGELOG.md`、`TODO.md`
- 第二個結果集支援使用 `圖檔路徑 + 檔名` 組成完整路徑，且下拉選單只顯示檔名
- 修正第二結果集欄位判斷順序，若同時存在 `圖檔路徑` 與 `檔名`，優先組成完整 PDF 檔案路徑
- 將 `載入 PDF / 圖面` 與 `資料庫 PDF 路徑` 操作區塊移到第二步驟「定義座標」欄位
- 重新編排功能區順序，將 `載入 PDF / 圖面` 拆成獨立第 2 步，後續區塊編號順延為 3~6
- 將第 2 步 `載入 PDF / 圖面` 區塊縮成較窄欄寬，減少橫向佔位
- 將第 2 步中的 `挑選檔案` 改為預設收合，僅保留資料庫 PDF 路徑操作直接顯示
- 縮小圖面泡泡標示尺寸與字體，減少遮擋工程圖內容
- 新增泡泡定位時，右側規格資料會同步自動選到對應項次
- 將 `dotnet run` 預設綁定位址調整為 `http://10.1.1.12:5088`
- 將預設綁定位址改為讀取 `appsettings.json` 的 `Hosting:Url`，未來修改 IP 不必再改 `launchSettings.json`
- 從資料庫載入規格時，若第二結果集有 PDF 資料，會預設自動載入第一筆，不需再手動點擊按鈕
- 新增系統版次顯示 API 與畫面版次標籤，並將目前版次提升為 `V2026.04.29.02`
- 明確建立規則：每一次修正都必須同步遞增系統版次
- 將圖面泡泡標示縮小到接近原本的一半，並同步縮小選取外框；系統版次提升為 `V2026.04.29.03`
- 修正多頁 PDF 載入只顯示第一頁的問題，改為串接全部頁面後載入；系統版次提升為 `V2026.04.29.04`
- 新增根目錄 `.gitignore`，供此資料夾獨立上傳 GitHub repository 時排除 `.vs`、`publish`、`bin`、`obj`
