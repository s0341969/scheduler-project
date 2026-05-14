# TODO

- 驗證 `hMailServer` 在實際主機上的 `EventHandlers.vbs` 語法檢查與事件載入是否正常。
- 針對 `NotifyMIS` 增加失敗收件者記錄，讓無效信箱可被精確追蹤。
- 若未來頻繁部署，可考慮把 `ScriptVersion` 與 `BuildTime` 改為單一常數或部署流程自動注入。
- 若 `Evidence` 仍不足以定位問題，可再擴充為顯示命中來源欄位的多筆結果，而不是只顯示第一筆片段。
- 若需更高精度，可考慮在評分階段直接記錄命中來源欄位與位置，而不是在通知階段回推。
- 盤點 `HighRisk_Domains.txt` 與 `AutoBlock_Domain.txt` 的實際內容，確認移除內建網域後仍符合目前資安策略。
- 盤點規則檔實際內容與編碼，避免 UTF-8 / UTF-16 混用造成維護成本。
- 若未來仍需自動驗證，補一份可在 Windows 主機執行的 `hMailServer` 部署與 smoke test 文件。
