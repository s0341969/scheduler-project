# Changelog

## 2026-04-06

- 將單一「寫入 MSSQL 資料庫」開關拆成兩個獨立選項：
  - `寫入 CHRNAME`
  - `寫入 CHRNAME-HISTORY`
- 寫入流程改為依勾選狀態分別執行主檔與歷史檔。
- 舊版設定若曾啟用單一 DB 寫入開關，載入時會自動對應成兩個新開關都啟用。
- 同步更新 README、CHANGELOG、TODO。
