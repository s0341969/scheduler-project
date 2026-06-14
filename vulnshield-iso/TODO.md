# TODO

## 下次優先

1. `UsersController` 與使用者管理頁
2. `EF Core Migration`，取代目前的 `EnsureCreated()`
3. `Greenbone` 測試連線、帳密驗證與同步治理強化
4. 補上 Controllers 層級整合測試、補上 Integration Test
5. 為 `ScanJobs` 頁補上 `Nuclei` 安裝狀態檢查卡（類似 Nmap preflight alert）

## VulnScan.Web 後續項目

- 為 `資產清冊`、`改善追蹤`、`自動匯入` 補上與結果頁一致的導覽卡
- 為 `系統檢查` 頁補上 `Hangfire`、自動匯入目錄與背景服務健康度
- 為 `Nmap` 一鍵安裝流程補上下載進度、校驗碼驗證與安裝後自動重新檢查
- 為 `Greenbone / OpenVAS` 補上測試連線按鈕、僅驗證帳密 / TLS 模式與設定異動審計
- 為版本抽取規則補上更完整解析，降低 `Nuclei / Nessus / Greenbone` 文字輸出的啟發式誤差
- 為 `VulnScan.Web` 補上使用者停用、重設密碼與角色治理
- 為 `Vulnerabilities` 補上更細的搜尋、篩選、編輯與刪除治理
- 為 `ScanRuns / Reports` 補上更完整的趨勢與週期分析
- 補上 `stop_vulnscan_web.bat`，讓新版系統可一鍵停止

## 已完成

- ✅ `ExceptionMiddleware` 全域例外處理中介層
- ✅ `Swagger / OpenAPI` 支援
- ✅ `SignalR` 即時通知（掃描完成 Toast）
- ✅ `Rate Limiting` API 限流
- ✅ `Webhook` 匯出機制（含 HMAC 簽章）
- ✅ `Dockerfile` + `docker-compose.yml` 容器化
- ✅ `VulnScan.Web.Tests` 測試專案（16 項測試）

## 原則

- repo 現在只保留 `VulnScan.Web`
- 不再回補或恢復舊的 `Python / FastAPI / Celery / Docker` 版本
