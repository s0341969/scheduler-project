# PDF 圓圈定位紀錄系統

這是一個可直接執行的 PDF 圓圈定位紀錄系統，目標是從 PDF 中找出「圓圈內含數字」的標記，並輸出頁碼、數字內容與 PDF 座標。

## 功能

- 支援多頁 PDF 批次處理
- 偵測圓形外框
- 支援範本庫比對，適合固定版型圖面
- 優先使用 PDF 向量文字位置提升準確率
- 若無法從向量文字取得數字，改用 OCR 辨識圈內數字
- OCR 採延後初始化，只有真的需要 OCR 時才載入模型
- 匯出 `JSON` 與 `CSV`
- 輸出每頁標註預覽圖，方便人工複核
- 預覽圖會在已偵測圓圈旁額外畫出彩色提示圈，方便快速確認哪些圈已被抓到

## 輸出欄位

- `page`: 頁碼，從 1 開始
- `number`: 圓圈內辨識出的數字
- `center_x`, `center_y`: PDF 座標中心點，單位為 point
- `radius`: 圓半徑，單位為 point
- `bbox_x0`, `bbox_y0`, `bbox_x1`, `bbox_y1`: 圓外接框座標，單位為 point
- `source`: `vector-text` 或 `ocr`
- `confidence`: 辨識信心值

## 安裝

建議直接使用目前工作區已安裝的 Python：

```powershell
C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe -m pip install -r requirements.txt
```

或直接以專案方式安裝：

```powershell
C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe -m pip install -e .[dev]
```

## 執行

```powershell
C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe -m circle_locator.cli .\your-file.pdf
```

指定輸出目錄：

```powershell
C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe -m circle_locator.cli .\your-file.pdf --output-dir .\output\pdf
```

關閉預覽圖輸出：

```powershell
C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe -m circle_locator.cli .\your-file.pdf --no-preview
```

使用範本庫加速與提升穩定性：

```powershell
C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe -m circle_locator.cli .\your-file.pdf --template-dir .\templates --template-threshold 0.78
```

安裝成命令列工具後也可直接執行：

```powershell
pdf-circle-locator .\your-file.pdf --template-dir .\templates
```

Windows 若想直接雙擊測試，可使用：

```powershell
.\run_pdf_circle_locator.bat
```

若要測自己的 PDF，可把 PDF 拖到 `run_pdf_circle_locator.bat` 上，或用命令列帶入路徑：

```powershell
.\run_pdf_circle_locator.bat .\your-file.pdf
```

`run_pdf_circle_locator.bat` 為了避免 Windows `cmd.exe` 的批次檔編碼問題，訊息文字固定使用 ASCII 英文；偵測輸出與程式本體不受此限制。

## 測試

本專案為 Python 專案，未包含 `.sln` 或 `.csproj`，因此不適用 `dotnet build`。

請使用專案指定 Python runtime 執行：

```powershell
C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe -m pytest
```

為避免目前 Windows 執行環境對 `pytest` 預設快取目錄與暫存 fixture 的權限限制，測試已改為使用專案內自行建立的臨時目錄，並停用 `cacheprovider`。

## 輸出位置

- JSON: `output/pdf/detections.json`
- CSV: `output/pdf/detections.csv`
- 預覽圖: `output/pdf/preview/page-0001.png` 等

## 實作說明

1. 使用 `PyMuPDF` 將 PDF 頁面以指定 DPI render 成影像。
2. 使用 `OpenCV HoughCircles` 偵測圓圈候選。
3. 使用 `PyMuPDF` 直接抽取頁面文字 span，若數字 span 落在圓圈內，優先採用。
4. 若該圓內沒有可用文字 span，使用 `RapidOCR` 對圈內區域做 OCR。
5. 若提供 `templates` 目錄，會先額外使用範本比對產生高可信候選。
   若範本檔名已提供數字提示，系統不會再對該候選重複執行 OCR。
6. 將像素座標回推成 PDF point 座標。
7. 輸出結構化紀錄與標註預覽圖。

## 範本庫格式

將範本圖放在 `templates/` 目錄下，建議每張圖片只包含單一個圓圈與其內數字。

檔名規則建議：

- `1.png`
- `12.png`
- `108_scan.png`

如果檔名內有數字，系統會把它當成 `number_hint` 優先使用。
若檔名沒有數字，系統會退回 OCR 辨識。

範本準備原則：

- 使用和正式文件接近的解析度
- 盡量裁到圓圈外圍，避免太多背景
- 每種大小、字型、掃描品質各放幾張
- 固定版型文件可依版型拆不同模板資料夾

## 已知限制

- 若圓圈嚴重破損、不是接近圓形、或數字被其他圖形遮蔽，可能漏檢。
- 若 PDF 是低解析掃描件，建議提高 `--dpi` 到 `300` 或 `360`。
- 若頁面上有大量非目標圓形，可能需要調整 `--min-radius-pt` 與 `--max-radius-pt`。
- OCR 對極小字、手寫字、特殊字型的穩定性低於向量文字。

## 建議使用方式

實務上請搭配預覽圖複核，流程如下：

1. 先跑一次整份 PDF。
2. 人工查看 `preview` 圖確認漏檢或誤檢。
3. 若有偏差，再根據圖面尺寸調整 `--dpi`、`--min-radius-pt`、`--max-radius-pt` 後重跑。

## 專案結構

- `circle_locator/`: 核心套件與 CLI
- `templates/`: 你的圓圈範本庫
- `tools/`: 產生測試 PDF 與範本的工具
- `run_pdf_circle_locator.bat`: Windows 雙擊測試入口
- `tests/`: 基本自動化測試
- `output/`: 偵測輸出
- `tmp/`: 中間測試資料
