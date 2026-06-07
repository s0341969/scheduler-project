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
- `Nmap` 掃描與 `Port / Service` 結果解析
- `Nuclei` / `Nessus` 匯入
- `Greenbone / OpenVAS` API 同步
- 弱點清單與改善追蹤
- `Excel / PDF` 報表匯出（含封面頁、摘要統計、風險等級分佈、弱點明細表）
- 稽核紀錄保存
- 系統檢查與 `Nmap` 一鍵安裝

## 目前保留的主要內容

- [VulnScan.Web](G:\codex_pg\vulnshield-iso\VulnScan.Web)
- [VulnScan.Web.slnx](G:\codex_pg\vulnshield-iso\VulnScan.Web.slnx)
- [start_vulnscan_web.bat](G:\codex_pg\vulnshield-iso\start_vulnscan_web.bat)
- [VulnScan_Web_操作手冊.md](G:\codex_pg\vulnshield-iso\VulnScan_Web_操作手冊.md)
- [VulnScan_Web_SPEC.md](G:\codex_pg\vulnshield-iso\VulnScan_Web_SPEC.md)

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

## 後續優先項目

目前下一輪優先項目：

1. `UsersController` 與使用者管理頁
2. `EF Core Migration`，取代 `EnsureCreated()`
3. `Greenbone` 測試連線與更完整的同步治理
4. 更完整的整合測試與匯出治理
