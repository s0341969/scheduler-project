# VulnShield-ISO / VulnScan.Web 🛡️

> 2026-06-04 起，repo 內已新增一條依 `VulnScan_Web_SPEC.md` 重新開發的平行產品線：`VulnScan.Web`。  
> 舊的 `src/*` Python/FastAPI 系統保留不動，新的商用化弱掃平台開發以 `G:\codex_pg\vulnshield-iso\VulnScan.Web` 為主。

VulnShield-ISO 是一套以 `FastAPI + Celery + Redis + PostgreSQL + Nmap + Nuclei` 建構的弱點掃描與漏洞管理系統。此版本已補上可實際使用的 JWT 認證、RBAC、啟動時資料表初始化、預設管理員建立，以及掃描結果正規化與去重邏輯。

## 新版商用規格實作：`VulnScan.Web`

`VulnScan.Web` 是依 [VulnScan_Web_SPEC.md](G:\codex_pg\vulnshield-iso\VulnScan_Web_SPEC.md) 新建的 ASP.NET Core MVC 版本，定位是新的弱點掃描管理平台，而不是延伸舊 Python Dashboard。

### 目前已完成的 V1 核心
- ASP.NET Core MVC + MSSQL + Hangfire 專案骨架
- 依 spec 建立資料模型：
  - `Users`
  - `Assets`
  - `ScanAllowedRanges`
  - `ScanJobs`
  - `ScanRuns`
  - `AssetPorts`
  - `Vulnerabilities`
  - `VulnerabilityActions`
  - `AuditLogs`
  - `ReportExports`
- 白名單檢查服務：非白名單 IP 會直接拒絕並寫入 `AuditLogs`
- `ScanJobs -> ScanRuns -> Hangfire -> Nmap -> XML Parser -> AssetPorts` 執行流程
- Excel 匯出服務：可匯出高風險弱點報表
- MVC 頁面：
  - `Dashboard`
  - `Assets`
  - `ScanAllowedRanges`
  - `ScanJobs`
  - `ScanRuns`
  - `AssetPorts`
  - `Vulnerabilities`
  - `Reports`
  - `AuditLogs`
- 啟動時會自動 `EnsureCreated()`，並建立預設使用者與內網白名單

### `VulnScan.Web` 啟動方式
1. 先確認 MSSQL 可連線，並依需求調整：
   - [VulnScan.Web/appsettings.json](G:\codex_pg\vulnshield-iso\VulnScan.Web\appsettings.json)
   - [VulnScan.Web/appsettings.Development.json](G:\codex_pg\vulnshield-iso\VulnScan.Web\appsettings.Development.json)
2. 於 repo 根目錄執行：

```powershell
dotnet build
dotnet run --project .\VulnScan.Web\VulnScan.Web.csproj
```

### `VulnScan.Web` 本地登入假設
由於 spec 的 `Users` 資料表沒有密碼欄位，這版採用最小侵入的本地登入策略：
- 帳號與角色由 `Users` 資料表管理
- 密碼由 `LocalAuth:SharedPassword` 控制
- Development 預設密碼在 [VulnScan.Web/appsettings.Development.json](G:\codex_pg\vulnshield-iso\VulnScan.Web\appsettings.Development.json) 為 `Admin123!`
- 啟動時會自動建立：
  - `admin`
  - `secmgr`
  - `scanner`
  - `viewer`

正式環境應改為：
- AD / LDAP / SSO
- 或在後續版本為 `Users` 補齊獨立密碼/身分驗證機制

## 核心能力
- JWT Bearer 認證，登入端點為 `POST /token`
- 角色權限管控：`Admin`、`Analyst`、`Auditor`
- 啟動時自動建立資料表與預設管理員
- 內建設備管理頁：`GET /dashboard`
- Dashboard 已拆成三個工作分頁：`設備`、`掃描`、`報告`
- Dashboard 已重新規劃為商用品控制台版面，採深色側欄、亮色作業主區、決策型 KPI 卡與分頁式工作區
- Dashboard 互動已改為非阻斷式通知模式，設備、排程、掃描與 credential 操作會以 toast 顯示結果，而不是中斷式 `alert()`
- Dashboard 表單已補上 inline validation，登入、設備、credential 與排程欄位錯誤會直接顯示在欄位旁，不再只依賴 submit 後錯誤訊息
- Dashboard 三個主分頁已真正分工：`設備` 只做設備治理，`掃描` 只做設備勾選與批次掃描，`報告` 只看掃描紀錄與風險報表
- 設備導向資產模型，支援設備類型、位置、標籤與備註
- 設備支援生命週期狀態：`運作中`、`維護中`、`已退役`
- 支援 DB-backed 排程掃描，透過 `Celery beat` 每分鐘同步到期任務
- 商用化第二階段骨架已完成：`scan_profile`、設備模板、掃描分層摘要、Credential 管理、Authenticated Scan 策略與 API
- Nmap + Nuclei 非同步掃描流程
- 弱點風險分數：`CVSS/Severity × Asset Criticality`
- Finding 狀態機：`Open -> Acknowledged -> Fixed -> Verified`，並保留 `Risk-Accepted`
- 狀態變更會寫入 `audit_logs`
- 掃描原始輸出會保存到 `ScanTask.raw_output_path`
- 設備頁可查看每次掃描的內容摘要：使用引擎、探測到的服務、漏洞命中、資訊/風險提示

## 部署方式

### 必要環境
- Docker
- Docker Compose

### 內建掃描工具安裝方式
- Docker image 會在建置時安裝 `nmap`
- `Nuclei` 會使用固定版本 release binary 安裝，目前預設為 `3.3.8`
- 若未來 `Nuclei` 版本需要升級，請同步更新 `Dockerfile` 的 `NUCLEI_VERSION`

### 環境變數
先複製 `G:\codex_pg\vulnshield-iso\.env.example` 為 `.env`，至少調整以下值：
- `SECRET_KEY`
- `CREDENTIAL_ENCRYPTION_KEY`：建議正式環境獨立設定，用於加密保存掃描帳密
- `DEFAULT_ADMIN_PASSWORD`
- `POSTGRES_PASSWORD`

### 啟動
```bash
docker compose -p vulnshield-iso up -d --build
```

或直接執行一鍵啟動檔：
- PowerShell：`G:\codex_pg\vulnshield-iso\start_system.ps1`
- 雙擊版：`G:\codex_pg\vulnshield-iso\start_system.bat`

一鍵啟動腳本目前會先檢查：
- `.env` 是否存在
- `docker` 指令是否可用
- Docker Desktop 是否已啟動
- `http://localhost:8000/healthz` 是否在 180 秒內回應成功
- `worker` 容器是否已進入 running 狀態
- `beat` 容器是否已進入 running 狀態
- 啟動失敗時自動列出 `api` 與 `worker` 的近期 logs
- 啟動失敗時也會列出 `beat` 的近期 logs
- `docker compose` 會固定使用專案名 `vulnshield-iso`
- `db` 與 `redis` 會先經過 healthcheck，`api` / `worker` 會等依賴服務健康後再啟動
- `beat` 會和 `worker` 一起啟動，用於排程同步

### 驗證
- 健康檢查：`GET http://localhost:8000/healthz`
- Swagger：`GET http://localhost:8000/docs`
- 設備管理頁：`GET http://localhost:8000/dashboard`

## 預設登入流程
1. 系統啟動時，會依 `.env` 建立預設管理員。
2. 呼叫 `POST /token`，使用 `application/x-www-form-urlencoded` 提交：
   - `username`
   - `password`
3. 取得 `access_token` 後，以 `Authorization: Bearer <token>` 呼叫其他 API。

## 角色權限
| 角色 | 可執行操作 | 限制 |
| :--- | :--- | :--- |
| `Admin` | 建立使用者、管理資產、觸發掃描、更新 finding | 無 |
| `Analyst` | 建立自己的資產、掃描自己的資產、更新 finding | 不可驗證 `Verified`，不可操作他人資產 |
| `Auditor` | 查看 finding、驗證 `Fixed -> Verified` | 不負責建立資產與觸發掃描 |

## API 流程

### 1. 建立使用者
- `POST /users`
- 僅 `Admin`

### 2. 建立資產
- `POST /assets`
- 重要欄位：
  - `name`
  - `target`
  - `device_type`：`Computer` / `Server` / `Firewall` / `Router` / `Switch` / `NAS` / `NetworkDevice` / `Other`
  - `criticality`：1 到 5
  - `owner_id`
  - `env`：`Production` / `Staging` / `Development`
  - `location`
  - `tags`
  - `notes`

### 3. 觸發掃描
- `POST /scans/trigger`
- 參數：
  - `asset_id`
  - `scan_profile`：`quick` / `standard` / `aggressive` / `web_only` / `network_only` / `authenticated_windows` / `authenticated_linux` / `authenticated_snmp`
  - `device_template`：可選，覆蓋設備預設模板
  - `credential_id`：若為認證型掃描，可指定覆蓋設備預設 credential

或使用設備導向入口：
- `POST /assets/{asset_id}/scan`
  - Body 可選：
```json
{
  "scan_profile": "quick",
  "device_template": "generic",
  "credential_id": null
}
```

### 4. 查詢掃描狀態
- `GET /scans/{task_id}/status`

掃描策略與模板目錄：
- `GET /scans/profiles`
- `GET /scans/templates`
- `GET /credentials/kinds`
- `GET /credentials`
- `POST /credentials`
- `GET /credentials/{credential_id}`
- `PATCH /credentials/{credential_id}`
- `GET /credentials/{credential_id}/audit`
- `DELETE /credentials/{credential_id}`
- `GET /schedules`
- `PATCH /schedules/{schedule_id}`
- `DELETE /schedules/{schedule_id}`

設備專用：
- `GET /assets/{asset_id}/scans`
  - 會一併回傳 `scan_summary`，用於設備頁展示掃描內容
- `GET /assets/{asset_id}/schedules`
- `POST /assets/{asset_id}/schedules`

### 5. 查詢 findings
- `GET /findings`

設備專用：
- `GET /assets/{asset_id}/findings`

### 6. 更新 finding 狀態
- `PATCH /findings/{finding_id}/status`
- Body：
```json
{
  "new_status": "Acknowledged"
}
```

### 7. 合規摘要
- `GET /reports/iso27001`

### 8. 目前登入者
- `GET /users/me`

## 設備管理頁使用方式
1. 開啟 `http://localhost:8000/dashboard`
2. 使用 `.env` 內預設帳號密碼登入
3. 在 `設備` 分頁左側建立設備，填入設備類型、目標位址、標籤、預設掃描模式、設備模板與預設 credential
4. 若要使用認證型掃描，可在同一頁下方的 `Credential 庫` 建立 Windows、Linux SSH 或 SNMP 憑證
5. 在 `設備` 分頁的清單選取目標設備
6. 在設備詳情頁可直接按 `編輯設備` 回填左側表單，修改設備的目標、模板、credential 與備註
7. 在設備詳情頁可改選當次掃描模式與 credential，再按 `執行弱點掃描`
8. 在 `Credential 庫` 可直接停用、重新啟用或刪除 credential；若仍被設備綁定或有執行中掃描，系統會阻擋刪除
9. 在設備詳情可直接建立每日 / 每週 / Cron 排程，指定掃描模式、模板與 credential
10. 切到 `掃描` 分頁時，才會出現可勾選的設備清單；可一次勾多台設備，選定掃描模式後批次觸發
11. 切到 `報告` 分頁時，才會顯示所有掃描過的紀錄，以及風險摘要、趨勢與優先處理資訊
12. Dashboard 視覺已改為商用導向：左側固定控制欄負責登入與導航，右側依工作情境拆成設備治理、批次掃描與報告決策三個主要工作面

## 目前行為重點
- `scan_profile` 目前支援：
  - `quick`
  - `standard`
  - `aggressive`
  - `web_only`
  - `network_only`
  - `authenticated_windows`
  - `authenticated_linux`
  - `authenticated_snmp`
- 設備模板目前支援：
  - `generic`
  - `firewall`
  - `switch`
  - `nas`
  - `web_server`
- Credential 類型目前支援：
  - `WindowsPassword`
  - `LinuxSSHPassword`
  - `LinuxSSHKey`
  - `SNMPv2c`
- 排程週期目前支援：
  - `Daily`
  - `Weekly`
  - `Cron`
- 若 Nuclei severity 是文字等級（如 `critical`、`medium`），系統會先正規化為數值。
- 相同弱點會以 `template-id | matcher-name | info.name` 組成穩定 key，避免每次掃描都新增重複 `Vulnerability`。
- 相同 `asset + vulnerability` 會更新既有 `Finding`，而不是無限制重複新增。
- 已驗證關閉的 finding 若在後續掃描再次出現，會重新回到 `Open`。
- `Asset` 已明確作為設備 inventory 使用，並支援依設備查看最近掃描、風險摘要與設備專屬 findings。
- 設備狀態目前支援：
  - `Active`：可正常執行掃描
  - `Maintenance`：仍可掃描，但表示設備處於維護窗口
  - `Retired`：不可再建立新的掃描任務
- Celery worker 啟動時會主動匯入 `src.worker.tasks`，避免掃描任務因未註冊而停在 `Pending`。
- 掃描摘要目前分成：
  - `服務發現`
  - `漏洞發現`
  - `錯誤設定`
  - `憑證風險`
  - `曝露管理介面`
  - `資訊 / 風險提示`
- 掃描摘要會額外記錄 `authentication` 區塊，標示本次是否要求 credential，以及實際使用的 credential 名稱與種類
- Dashboard 目前採三分頁工作台：設備頁做 inventory 與單設備掃描策略、掃描頁做全域任務檢視，報告頁做風險與掃描面向彙總。
- Dashboard 目前採真正分工式三分頁：設備頁顯示設備清單與新增設備，掃描頁顯示可勾選設備清單與批次掃描，報告頁集中顯示所有掃描紀錄與報表。
- Dashboard UI 已改成商用品資訊架構：使用固定側欄、Hero KPI、決策型報告卡、設備治理工作區與亮暗對比明確的操作層級，避免 PoC 風格卡片堆疊。
- 掃描分頁目前會顯示可掃描設備數、目前勾選數、退役設備數，並提供多選設備後批次觸發掃描。
- 掃描任務卡目前會顯示簡化步驟時間軸：`Queue -> Probe -> Analysis -> Report`，協助判斷任務卡在哪一層。
- 設備詳情頁目前會先顯示單設備 KPI：設備狀態、進行中任務、已完成任務、失敗任務與排程數量，再往下看掃描與 finding 明細。
- 報告分頁目前已加入條帶式風險分布與設備狀態分布，並集中顯示所有掃描紀錄，讓畫面不只列數字，也能直接往下翻歷史任務。
- 報告分頁目前已補上近 7 天掃描趨勢，區分完成、失敗與執行中任務，便於看週期波動。
- 排程掃描會先寫入 `scan_schedules`，再由 `Celery beat` 每分鐘檢查 `next_run_at` 是否到期，到期後建立 `scan_tasks` 並交給 worker 執行。
- 排程若未明確指定 credential，會沿用設備預設 credential。
- 報告頁已補上商用導向資訊：設備狀態分布、優先處理清單與營運建議，可直接看出先處理哪台設備與哪類營運問題。
- Credential 的敏感內容會以對稱加密方式存入資料庫，不會經由 API 回傳明文。
- Credential 已支援停用與刪除保護：停用後不可再綁定到設備或發動掃描；若仍被設備綁定或有 `Pending` / `Running` 任務，系統會拒絕刪除。
- Credential 與設備編輯都會寫入 `audit_logs`，credential 另外可透過 `GET /credentials/{id}/audit` 查最近審計紀錄。
- `authenticated_snmp` 已接上 Nmap `snmp-info` 腳本與 SNMP community 注入；`authenticated_windows` 與 `authenticated_linux` 目前先完成 credential 管理、相容性驗證、任務綁定與摘要呈現，後續再補真正的深度稽核引擎。

## 注意事項
- `DEFAULT_ADMIN_PASSWORD` 僅適合首次啟動，正式環境必須立即更換。
- 目前資料表初始化採用啟動時 `create_all`，尚未導入 Alembic migration。
- 本機若要執行測試或啟動 API，需要先安裝 `requirements.txt` 內相依套件。
- `Dockerfile` 已避免使用遠端 shell install script 安裝 `Nuclei`，改為固定版本 binary，以降低建置失敗與供應鏈風險。
- `API` 啟動時會重試資料庫初始化；即使 PostgreSQL 比 API 慢幾秒起來，也不會立刻因一次拒絕連線就退出。
- 目前設備管理頁使用瀏覽器本地儲存 `access_token`，適合內部 PoC 與單機部署；若要正式上線，建議後續改為更完整的 session / 前端權杖保護策略。
- 正式環境不應依賴 `SECRET_KEY` 衍生 credential 加密金鑰，請明確設定 `CREDENTIAL_ENCRYPTION_KEY`。
- `authenticated_windows` 與 `authenticated_linux` 目前尚未接入 WinRM、SMB、SSH 套件盤點或本機設定稽核，因此仍屬第二階段骨架而非完整已驗證掃描。
