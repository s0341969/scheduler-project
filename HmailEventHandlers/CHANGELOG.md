# CHANGELOG

## 2026-05-13 00:10

- 在 `Mail Security [高風險詐騙] Notice` 新增 `Evidence` 欄位，顯示每個 `RiskReason` 命中的欄位與前後文片段。
- 在 `Mail Security - Suspected Phishing Email Sample` 同步新增 `Evidence` 欄位。
- 新增 `BuildRiskEvidenceText` 等 helper，從 `Subject`、`Body`、`HTMLBody` 回推命中位置，協助追查像 `firebaseapp.com` 這類隱藏於 HTML 原始碼或回覆鏈中的內容。

## 2026-05-12 16:05

- 調整高風險網域維護策略，移除 `GetDefaultHighRiskDomains()` 內所有寫死的特定網域。
- 之後網域型高風險來源統一由 `HighRisk_Domains.txt` 與 `AutoBlock_Domain.txt` 管理。
- 程式內僅保留固定結構型高風險特徵，例如 `#@`、`//#`。

## 2026-05-12 15:00

- 新增 `MISNotifyTo` 多收件者支援，改為可用分號 `;` 分隔多個 MIS 通知信箱。
- 新增 `AddRecipientsFromList`，由 `NotifyMIS` 逐筆加入收件者，避免單一字串解析不穩定。
- 補齊 `README.md`、`CHANGELOG.md`、`TODO.md` 專案文件。
