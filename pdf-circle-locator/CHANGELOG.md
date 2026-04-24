# Changelog

## 2026-04-25

- 新增 `run_pdf_circle_locator_fast.bat`，提供 Windows 快速模式入口，預設啟用 `--fast-mode --fast-probe-dpi 96 --no-preview`。
- 新增 `--fast-mode` 與 `--fast-probe-dpi`，先用低 DPI 預檢頁面是否存在圓圈候選，沒有候選的頁面直接跳過完整偵測。
- 將 `run_pdf_circle_locator.bat` 的提示訊息改為 ASCII 英文，避免 `cmd.exe` 讀取批次檔時發生中文編碼錯亂而無法執行。

## 2026-04-24

- 新增 `run_pdf_circle_locator.bat`，支援 Windows 雙擊 sample 測試與拖放 PDF 直接執行偵測。
- 預覽圖新增彩色提示圈，會畫在已偵測圓圈旁邊，方便人工快速辨識已抓取目標。
- 延後初始化 `RapidOCR`，避免純向量文字頁面也承受 OCR 啟動成本。
- 修正圓圈去重邏輯，抑制落在較大圓內的誤檢小圓。
- 範本比對改為先做位置去重，且有檔名數字提示時不再重複執行 OCR，改善 CLI 執行時間。
- 新增回歸測試，驗證 sample PDF 只輸出 `1`、`12`、`7` 三筆結果。
- 移除對 `pytest tmp_path` fixture 與預設快取的依賴，改用專案內自建臨時目錄，避免 Windows 權限限制導致測試失敗。
- 補齊專案文件基線檔案 `CHANGELOG.md` 與 `TODO.md`。
