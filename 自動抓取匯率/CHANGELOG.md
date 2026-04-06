# Changelog

## 2026-04-06

- 新增可用 Visual Studio 2015 開啟的 WinForms 專案 `BotExchangeRateWinForms.csproj`。
- 新增 Windows Forms 主畫面，支援：
  - MSSQL 連線字串設定
  - Timer 抓取頻率設定（分鐘 / 小時）
  - 手動執行
  - 啟動 / 停止 Timer
  - 初始化資料庫
- 新增臺灣銀行牌告匯率 HTML 抓取與解析邏輯。
- 新增 MSSQL 寫入邏輯與去重索引設計。
- 新增 `sql/001_Create_BotExchangeRateTables.sql` 初始化腳本。
- 新增本專案的 `README.md`、`CHANGELOG.md`、`TODO.md`。
