# HmailEventHandlers

## 系統用途

此專案是 `hMailServer` 的 `EventHandlers.vbs` 收信事件腳本，用於公司郵件進站時的釣魚郵件偵測、主旨標記、警告插入、MIS 通知與規則學習。

## 目前主要行為

1. `White_Domain.txt` 命中寄件者時最高優先權直接放行。
2. 本系統送出的 `Mail Security` 通知信以自訂 Header 放行，避免通知循環。
3. 使用者寄到 `spam-report@mail.gongin.com.tw` 的回報信會抽取 URL 網域，累積學習資料並在達門檻後寫入 `HighRisk_Domains.txt`。
4. 內部 IP + 內部寄件者 + 全部內部收件者，可依 `EnableTrustedInternalMailPass` 直接放行。
5. 其餘信件依分數規則判斷風險：
   - 高風險：標記 `[高風險詐騙]`、插入紅色警告、通知 MIS、可自動寫入封鎖名單。
   - 中風險：標記 `[疑似詐騙]`、插入橘色警告。
   - 低風險：只寫 Log。

## MIS 通知 Evidence 顯示

高風險通知信 `Mail Security [高風險詐騙] Notice` 與附件 `Mail Security - Suspected Phishing Email Sample` 會在 `Reason` 下方額外顯示 `Evidence`。

`Evidence` 現在不是在通知階段回推，而是直接在評分階段依照同一份 `MailBodyText` 掃描字串、於命中當下立即擷取片段。

這代表：

- `Reason` 與 `Evidence` 使用同一份命中資料
- 只要 `Reason` 命中，`Evidence` 就必須同步顯示對應的 `NormalizedScanText` 片段
- 不再允許出現「Reason 命中，但 Evidence 全部找不到」的邏輯分叉

`EvidenceMode=CaptureSnippetAtScoringTime` 代表目前通知採用這個設計。

另外通知信與樣本附件都會顯示：

- `ScriptVersion`
- `BuildTime`
- `EvidenceMode`

用途是讓收件人直接判斷目前主機執行的 `EventHandlers.vbs` 是否為最新部署版本。

## 內建與外部規則分工

2026-05-12 起，系統不再內建任何特定高風險網域。

目前分工如下：

- 程式內建保留：
  - 高風險特殊 Pattern：`#@`、`//#`
  - URL 特徵：`http://`、`https://`
  - 中風險關鍵字備援：`GetDefaultMediumRiskKeywords()`
- 外部文字檔管理：
  - `HighRisk_Domains.txt`：人工維護的高風險網域
  - `AutoBlock_Domain.txt`：系統自動累積的封鎖網域

這樣做的目的是避免解除某個網域時，還要同步修改 `EventHandlers.vbs` 與 `HighRisk_Domains.txt`，降低雙重維護風險。

## 重要外部規則檔

- `White_Domain.txt`
- `PassMembers.txt`
- `HighRisk_Domains.txt`
- `MediumRisk_Keywords.txt`
- `PhishingScore_Config.txt`
- `Candidate_Keywords.txt`
- `Suspicious_Domain.txt`
- `AutoBlock_Domain.txt`
- `Notify_History.txt`
- `Reported_Domains.txt`
- `Reported_Domain_Count.txt`
- `Domain_Ignore_List.txt`

以上路徑目前由 `EventHandlers.vbs` 內常數硬編碼在 `D:\Program Files (x86)\hMailServer\Events\`。

## MIS 通知多人設定

`MISNotifyTo` 現在支援一個或多個收件者，使用分號 `;` 分隔。

範例：

```vbscript
Const MISNotifyTo = "techup@mail.gongin.com.tw;mis01@mail.gongin.com.tw;mis02@mail.gongin.com.tw"
```

系統會在 `NotifyMIS` 中逐筆加入收件者，因此：

- 單一收件者維持相容
- 多收件者可直接擴充
- 中間有空白或空項目會被略過

## 專案現況

- 目前資料夾只有 `EventHandlers.vbs` 與修改紀錄文件，不是 `dotnet` 專案。
- 因此若執行 `dotnet build`，預期不會有可建置的 `.sln` 或 `.csproj`。
