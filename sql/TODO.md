# TODO

- [ ] 在 SQL Server `TEST` 先執行 `產生ORDE3剩餘製程_實際修補SP.sql`，確認 5 輪片段皆匹配成功。
- [ ] 將 `dbo.產生ORDE3剩餘製程` 拆分為多個子程序（資料準備、排程標記、統計輸出）。
- [ ] 把大量 `FROM A,B WHERE` 舊式 Join 逐步改為 ANSI JOIN（降低誤連接風險）。
- [ ] 對正式表建立與檢核必要索引（含維護成本評估）。
- [ ] 導入基準測試：同一批 INPART 比較修正前後執行時間與結果筆數。
- [ ] 補齊專案 `.sln/.csproj` 或指定 build 專案路徑，讓 `dotnet build` 可通過。
