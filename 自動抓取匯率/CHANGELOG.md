# Changelog

## 2026-04-06

- `CHRNAM` 幣別代號對應改為貼近既有 legacy 規則，重點保留 `MA` 與 `NT$` 的判斷方式。
- 文件中的幣別代號對應說明同步更新。
- 修正 `CHRNAME` 寫入 SQL 將 `CHRDS` 欄位寫死造成的錯誤，改為執行時自動相容 `CHRDS`、`CHRDSC` 或無說明欄位的資料表。
- 已依 `MIS` 資料庫實際 schema 調整初始化 SQL 與歷史檔寫入邏輯，支援 `CHRNAME-HISTORY.CHRDSC` 及 `CNO IDENTITY`。
- 寫入邏輯改為只有匯率變動才寫入：`CHRNAME` 比對目前主檔，`CHRNAME-HISTORY` 比對最新歷史匯率。
- 補齊所有 `.cs` 檔的繁中功能註解，涵蓋類別、主要方法、核心屬性與主畫面 Designer 作用說明。
