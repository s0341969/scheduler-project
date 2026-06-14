# VulnScan.Web

`VulnScan.Web` 是目前這個 repo 唯一保留的正式版本。  
舊的 `Python / FastAPI / Celery / Docker` 弱掃系統已自 repo 移除，不再維護。

若要查看使用方式，請優先參考：
- [VulnScan_Web_操作手冊.md](G:\codex_pg\vulnshield-iso\VulnScan_Web_操作手冊.md)
- [VulnScan_Web_SPEC.md](G:\codex_pg\vulnshield-iso\VulnScan_Web_SPEC.md)

## 專案定位

`VulnScan.Web` 是一套以 `ASP.NET Core MVC` 建構的企業弱點掃描管理平台，核心能力包含：

- 資產清冊管理
- 掃描白名單控管
- 掃描任務與掃描執行紀錄
- `Nmap` 掃描（6 種掃描模式）與 `Port / Service` 結果解析
- `Nuclei` 直接掃描（支援多種範本分類 + Web DAST 分類）與 `Nuclei` / `Nessus` 匯入
- `Greenbone / OpenVAS` API 同步
- 弱點清單與改善追蹤
- `Excel / PDF` 報表匯出（含封面頁、摘要統計、風險等級分佈、弱點明細表）
- 稽核紀錄保存
- 系統檢查與 `Nmap` 一鍵安裝

## 目前保留的主要內容

- [VulnScan.Web](G:\codex_pg\vulnshield-iso\VulnScan.Web)
- [VulnScan.Web.Tests](G:\codex_pg\vulnshield-iso\VulnScan.Web.Tests) - 整合測試
- [VulnScan.Web.slnx](G:\codex_pg\vulnshield-iso\VulnScan.Web.slnx)
- [start_vulnscan_web.bat](G:\codex_pg\vulnshield-iso\start_vulnscan_web.bat)
- [VulnScan_Web_操作手冊.md](G:\codex_pg\vulnshield-iso\VulnScan_Web_操作手冊.md)
- [VulnScan_Web_SPEC.md](G:\codex_pg\vulnshield-iso\VulnScan_Web_SPEC.md)
- [Dockerfile](G:\codex_pg\vulnshield-iso\Dockerfile)
- [docker-compose.yml](G:\codex_pg\vulnshield-iso\docker-compose.yml)

## 啟動方式

### 一鍵啟動

直接執行：

```bat
G:\codex_pg\vulnshield-iso\start_vulnscan_web.bat
```

啟動後登入頁：

- `http://localhost:5186/Auth/Login`

### 手動啟動

```powershell
Set-Location G:\codex_pg\vulnshield-iso
dotnet build
dotnet run --project .\VulnScan.Web\VulnScan.Web.csproj
```

## 開發環境設定

開發模式預設使用 `SQLite`：

- 設定檔：[VulnScan.Web/appsettings.Development.json](G:\codex_pg\vulnshield-iso\VulnScan.Web\appsettings.Development.json)
- DB 檔案：`G:\codex_pg\vulnshield-iso\VulnScan.Web\App_Data\vulnscan-dev.db`

正式環境可切換為 `SQL Server`：

- 設定檔：[VulnScan.Web/appsettings.json](G:\codex_pg\vulnshield-iso\VulnScan.Web\appsettings.json)

## 預設登入帳號

Development 預設 bootstrap 帳號：

- `admin / Admin123!Demo`
- `secmgr / Security123!Demo`
- `scanner / Scanner123!Demo`
- `viewer / Viewer123!Demo`

## Nuclei 相關行為

若要執行 Nuclei 直接掃描，系統需可取得 `nuclei.exe`。系統會依序檢查：

1. `VulnScan:NucleiPath`（appsettings.json）
2. 系統 `PATH`

若未找到，執行 Nuclei 掃描任務時會拋出 `InvalidOperationException` 並記錄失敗的 `ScanRun`。

支援的 Nuclei 範本分類（ScanProfile）：
- `All` - 所有範本
- `cves` - 已知 CVE
- `vulnerabilities` - 一般弱點
- `misconfiguration` - 錯誤設定
- `exposures` - 曝露
- `default-logins` - 預設登入

支援的 Nuclei Web DAST 分類：
- `web-sqli` - SQL Injection
- `web-xss` - Cross-Site Scripting
- `web-lfi` - LFI / RFI
- `web-ssrf` - SSRF
- `web-rce` - Remote Code Execution
- `web-tech` - Web Tech Detection

## Nmap 掃描模式

支援 7 種掃描強度：
- `Quick` - 快速連接埠掃描（-T4 -F）
- `QuickPlus` - 快速 + 版本偵測（-T4 -sV）
- `Standard` - 標準 + 版本 + OS（-T4 -sV -O）
- `Deep` - 深度掃描（-T4 -A -sV --version-intensity 9）
- `Stealth` - 隱匿模式（-T2 -sS -sV）
- `VulnScript` - 安全腳本掃描（-T4 -sV --script vuln）
- `CredentialCheck` - 預設帳密檢測（-T4 -sV --script cred-summary）

## 相依性掃描（第三方套件安全檢查）

支援掃描專案目錄中的第三方套件弱點：

- **.NET 專案**：執行 `dotnet list package --vulnerable --include-transitive`，解析移植性相依性弱點
- **npm 專案**：執行 `npm audit --json`，解析 JavaScript 套件弱點
- **Python 專案**：執行 `pip-audit --json`，解析 Python 套件弱點

相依性掃描結果會寫入 `Vulnerabilities` 資料表（Protocol = "dependency"），可與 Nmap / Nuclei 結果合併檢視。

## 主動 Patch 版本比對

掃描完成後自動檢查檢測到的軟體版本是否落在已知 CVE 範圍內：

- 內建本地漏洞資料庫（`App_Data/vulnerability-db/known-vulnerabilities.json`）
- 支援完整 semver 範圍語法（`<` / `>` / `=` / `>=` / `<=` / `-` 範圍 / `||` OR）
- 支援 `AssetPorts.ServiceProduct/ServiceVersion` 與 `Vulnerabilities.DetectedVersion` 雙來源
- 可在 `appsettings.json` 中設定 `VulnScan:EnablePatchVersionCheck: false` 關閉

## 掃描工具選擇

建立或編輯掃描任務時，可在表單中切換掃描工具（Nmap / Nuclei / 相依性掃描），並根據所選工具自動切換對應的 profile 選項。

## Nmap 相關行為

若要執行內建掃描，Windows 主機仍需可取得 `nmap.exe`。系統會依序檢查：

1. `VulnScan:NmapPath`
2. 系統 `PATH`
3. 常見安裝路徑
   - `C:\Program Files (x86)\Nmap\nmap.exe`
   - `C:\Program Files\Nmap\nmap.exe`
   - `C:\Nmap\nmap.exe`

若未找到：

- `掃描任務` 頁會先顯示前置檢查未通過
- `立即掃描` 按鈕會停用
- `SystemCheck` 頁可直接查看狀態
- Windows 上若角色為 `Admin` 或 `SecurityManager`，可直接按 `直接安裝 Nmap`

`Nmap` 一鍵安裝流程會：

1. 讀取官方下載頁 `https://nmap.org/download.html`
2. 掃描官方下載頁與 `https://nmap.org/dist/` 中所有 `nmap-*-setup.exe`
3. 自動選擇最新版本
4. 下載到 `VulnScan.Web\App_Data\Installers`
5. 啟動官方 Windows installer

## 主要功能頁面

- `掃描結果 / 紀錄`
  - 看每次掃描是否成功、錯誤原因、Host / Open Port / 弱點數
- `服務結果`
  - 看實際掃到的 `Port / Service / Product / Version`
- `弱點結果`
  - 看真正的弱點明細、版本、嚴重度、改善狀態
- `報告 / 匯出`
  - 看摘要 KPI，並輸出 `Excel / PDF`
- `系統檢查`
  - 看 `Nmap`、`Greenbone`、`SQLite / MSSQL` 狀態

## 重要現況

- 本 repo 現在只維護 `VulnScan.Web`
- 舊的 `src/`、`tests/`、`Dockerfile`、`docker-compose.yml`、`start_system.*` 已移除
- 啟動、使用、報告與維運流程都應以 `VulnScan.Web` 為準

## 基礎建設特色

本專案已內建以下生產環境基礎建設：

- **全域例外處理中介層**：開發與正式環境皆適用，API 請求回傳 JSON 錯誤、MVC 請求導向錯誤頁
- **Swagger / OpenAPI**：瀏覽 `GET /openapi/v1.json` 取得 API 規格，支援 API 控制器自動產生文件
- **SignalR 即時通知**：`/hub/notifications` 端點，掃描狀態變更時主動推送到瀏覽器，支援自動重連與 Toast 通知
- **Rate Limiting**：API 端點每分鐘 100 次請求限制（開發環境 200 次），佇列溢位回傳 429
- **Webhook 匯出**：掃描完成時自動發送 HTTP POST 至設定的 Webhook URL，支援 HMAC-SHA256 簽章驗證
- **Docker 容器化**：`Dockerfile` + `docker-compose.yml`，SQL Server + VulnScan.Web 一鍵部署
- **整合測試**：`VulnScan.Web.Tests` 專案，使用 xUnit + Moq + EF Core SQLite InMemory
- **網頁應用弱掃**：支援 Nuclei Web DAST 分類（SQLi / XSS / LFI / SSRF / RCE / Tech Detection）
- **帳號權限檢測**：Nmap CredentialCheck profile，掃描預設帳密風險
- **相依性掃描**：支援 .NET (`dotnet list vulnerable`)、npm (`npm audit`)、Python (`pip-audit`)
- **主動 Patch 版本比對**：掃描後自動比對軟體版本與本地 CVE 資料庫，支援 semver 範圍語法

## 後續優先項目

目前下一輪優先項目：

1. `UsersController` 與使用者管理頁
2. `EF Core Migration`，取代 `EnsureCreated()`
3. `Greenbone` 測試連線與更完整的同步治理
4. 為 `DependencyScanService` / `PatchVersionService` 補上單元測試
