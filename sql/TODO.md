# TODO

- [ ] 在 SQL Server `TEST` 先執行 `產生ORDE3剩餘製程_實際修補SP.sql`，確認 5 輪片段皆匹配成功。
- [ ] 將 `dbo.產生ORDE3剩餘製程` 拆分為多個子程序（資料準備、排程標記、統計輸出）。
- [ ] 把大量 `FROM A,B WHERE` 舊式 Join 逐步改為 ANSI JOIN（降低誤連接風險）。
- [ ] 對正式表建立與檢核必要索引（含維護成本評估）。
- [ ] 導入基準測試：同一批 INPART 比較修正前後執行時間與結果筆數。
- [ ] 補齊專案 `.sln/.csproj` 或指定 build 專案路徑，讓 `dotnet build` 可通過。
- [ ] 以 SSMS 開啟 [產生ORDE3剩餘製程.sql] 做一次中文欄位名稱完整性檢查，確認無編碼破壞。
- [ ] 在 TEST 實測本體檔版本 產生ORDE3剩餘製程.sql 與修補腳本版輸出差異。
- [ ] 在 TEST 針對本次新增索引（#QA1/#TEMP2/#RST）比對執行計畫與實際耗時，確認收益與無回歸。
- [ ] 在 TEST 驗證 ORDDTP 範圍收斂更新（@INPART 非 %）的影響筆數與舊版一致性。
- [ ] 在 TEST 驗證 2026-03-30 新增索引（#TEMP3 / #指派時間 / #指派時間_SetUpKey / #指派時間_機台區間 / #指派時間_最小機台）是否命中預期查詢。
- [ ] 針對常用 INPART 與全量（@INPART='%'）各跑 3 次，紀錄修正前後平均耗時、P95、CPU time、logical reads。
