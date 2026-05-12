# TODO

- 驗證 `hMailServer` 在實際主機上的 `EventHandlers.vbs` 語法檢查與事件載入是否正常。
- 針對 `NotifyMIS` 增加失敗收件者記錄，讓無效信箱可被精確追蹤。
- 盤點規則檔實際內容與編碼，避免 UTF-8 / UTF-16 混用造成維護成本。
- 若未來仍需自動驗證，補一份可在 Windows 主機執行的 `hMailServer` 部署與 smoke test 文件。
