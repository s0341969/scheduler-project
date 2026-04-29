# Changelog

## 2026-04-29 16:41

- 調整規格查詢 API，支援 Stored Procedure 兩個結果集：
  - 第一個結果集為規格資料
  - 第二個結果集為 PDF 路徑清單
- 新增後端 PDF 串流端點，讓瀏覽器可透過網站載入資料庫回傳的 PDF 路徑
- 前端新增資料庫 PDF 下拉選單與載入按鈕
- 前端在載入規格後會同步顯示可選的資料庫 PDF 數量
- 補齊 `README.md`、`CHANGELOG.md`、`TODO.md`
