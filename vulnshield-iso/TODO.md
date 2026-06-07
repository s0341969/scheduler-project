# TODO

## 下次優先

1. `UsersController` 與使用者管理頁
2. `EF Core Migration`，取代目前的 `EnsureCreated()`
3. `Greenbone` 測試連線、帳密驗證與同步治理強化
4. `VulnScan.Web` 整合測試

## VulnScan.Web 後續項目

- 為 `資產清冊`、`改善追蹤`、`自動匯入` 補上與結果頁一致的導覽卡
- 為 `系統檢查` 頁補上 `Hangfire`、自動匯入目錄與背景服務健康度
- 為 `Nmap` 一鍵安裝流程補上下載進度、校驗碼驗證與安裝後自動重新檢查
- 為 `Greenbone / OpenVAS` 補上測試連線按鈕、僅驗證帳密 / TLS 模式與設定異動審計
- 為版本抽取規則補上更完整解析，降低 `Nuclei / Nessus / Greenbone` 文字輸出的啟發式誤差
- 為 `VulnScan.Web` 補上使用者停用、重設密碼與角色治理
- 為 `Vulnerabilities` 補上更細的搜尋、篩選、編輯與刪除治理
- 為 `ScanRuns / Reports` 補上更完整的趨勢與週期分析
- 強化 PDF 匯出版型：頁尾、公司識別、圖表與更清楚的弱點分群摘要
- 補上 `stop_vulnscan_web.bat`，讓新版系統可一鍵停止

## 原則

- repo 現在只保留 `VulnScan.Web`
- 不再回補或恢復舊的 `Python / FastAPI / Celery / Docker` 版本
