# Changelog

## 2026-04-06

- 抓取模型恢復同時保留現金匯率與即期匯率四個欄位，供 DB 寫入規則使用。
- 畫面 `DataGridView` 仍只顯示現金本行買入與現金本行賣出。
- MSSQL 寫入目標調整為同時處理：
  - `dbo.CHRNAME`
  - `dbo.[CHRNAME-HISTORY]`
- 寫入 `FTIL / FTOL` 時套用既有規則：
  - `CNY` 用現金買入 / 現金賣出
  - 其他幣別用即期買入 / 即期賣出
- `CHRNAM` 對應規則改為依既有邏輯轉換，例如 `USD -> US`、`JPY -> JP`、`THB -> TA`、`CNY -> RMB`、`MYR -> MA`。
- 初始化 SQL 腳本同步改為建立 `CHRNAME` 與 `CHRNAME-HISTORY`。
- 同步更新 README、CHANGELOG、TODO。
