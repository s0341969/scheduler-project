# CHANGELOG

## 2026-05-12 16:05

- 調整高風險網域維護策略，移除 `GetDefaultHighRiskDomains()` 內所有寫死的特定網域。
- 之後網域型高風險來源統一由 `HighRisk_Domains.txt` 與 `AutoBlock_Domain.txt` 管理。
- 程式內僅保留固定結構型高風險特徵，例如 `#@`、`//#`。

## 2026-05-12 15:00

- 新增 `MISNotifyTo` 多收件者支援，改為可用分號 `;` 分隔多個 MIS 通知信箱。
- 新增 `AddRecipientsFromList`，由 `NotifyMIS` 逐筆加入收件者，避免單一字串解析不穩定。
- 補齊 `README.md`、`CHANGELOG.md`、`TODO.md` 專案文件。
