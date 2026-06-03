# VulnShield-ISO 🛡️

VulnShield-ISO 是一套以 `FastAPI + Celery + Redis + PostgreSQL + Nmap + Nuclei` 建構的弱點掃描與漏洞管理系統。此版本已補上可實際使用的 JWT 認證、RBAC、啟動時資料表初始化、預設管理員建立，以及掃描結果正規化與去重邏輯。

## 核心能力
- JWT Bearer 認證，登入端點為 `POST /token`
- 角色權限管控：`Admin`、`Analyst`、`Auditor`
- 啟動時自動建立資料表與預設管理員
- 內建設備管理頁：`GET /dashboard`
- Dashboard 已拆成三個工作分頁：`設備`、`掃描`、`報告`
- 設備導向資產模型，支援設備類型、位置、標籤與備註
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
- 啟動失敗時自動列出 `api` 與 `worker` 的近期 logs
- `docker compose` 會固定使用專案名 `vulnshield-iso`
- `db` 與 `redis` 會先經過 healthcheck，`api` / `worker` 會等依賴服務健康後再啟動

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
  - `scan_profile`

或使用設備導向入口：
- `POST /assets/{asset_id}/scan`

### 4. 查詢掃描狀態
- `GET /scans/{task_id}/status`

設備專用：
- `GET /assets/{asset_id}/scans`
  - 會一併回傳 `scan_summary`，用於設備頁展示掃描內容

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
3. 在 `設備` 分頁左側建立設備，填入設備類型、目標位址、標籤與位置
4. 在 `設備` 分頁的清單選取目標設備
5. 在設備詳情頁按 `執行弱點掃描`
6. 切到 `掃描` 分頁查看全域掃描任務歷史、狀態與每次掃描的內容摘要
7. 切到 `報告` 分頁查看整體風險統計、高風險設備與掃描面向彙總

## 目前行為重點
- 若 Nuclei severity 是文字等級（如 `critical`、`medium`），系統會先正規化為數值。
- 相同弱點會以 `template-id | matcher-name | info.name` 組成穩定 key，避免每次掃描都新增重複 `Vulnerability`。
- 相同 `asset + vulnerability` 會更新既有 `Finding`，而不是無限制重複新增。
- 已驗證關閉的 finding 若在後續掃描再次出現，會重新回到 `Open`。
- `Asset` 已明確作為設備 inventory 使用，並支援依設備查看最近掃描、風險摘要與設備專屬 findings。
- Celery worker 啟動時會主動匯入 `src.worker.tasks`，避免掃描任務因未註冊而停在 `Pending`。
- 掃描摘要會將 Nmap 結果整理成服務清單，並把 Nuclei 結果分成「漏洞」與「資訊/風險提示」兩類。
- Dashboard 目前採三分頁工作台：設備頁做 inventory 與單設備掃描，掃描頁做全域任務檢視，報告頁做風險與掃描面向彙總。

## 注意事項
- `DEFAULT_ADMIN_PASSWORD` 僅適合首次啟動，正式環境必須立即更換。
- 目前資料表初始化採用啟動時 `create_all`，尚未導入 Alembic migration。
- 本機若要執行測試或啟動 API，需要先安裝 `requirements.txt` 內相依套件。
- `Dockerfile` 已避免使用遠端 shell install script 安裝 `Nuclei`，改為固定版本 binary，以降低建置失敗與供應鏈風險。
- `API` 啟動時會重試資料庫初始化；即使 PostgreSQL 比 API 慢幾秒起來，也不會立刻因一次拒絕連線就退出。
- 目前設備管理頁使用瀏覽器本地儲存 `access_token`，適合內部 PoC 與單機部署；若要正式上線，建議後續改為更完整的 session / 前端權杖保護策略。
