# VulnShield-ISO 🛡️

VulnShield-ISO 是一套以 `FastAPI + Celery + Redis + PostgreSQL + Nmap + Nuclei` 建構的弱點掃描與漏洞管理系統。此版本已補上可實際使用的 JWT 認證、RBAC、啟動時資料表初始化、預設管理員建立，以及掃描結果正規化與去重邏輯。

## 核心能力
- JWT Bearer 認證，登入端點為 `POST /token`
- 角色權限管控：`Admin`、`Analyst`、`Auditor`
- 啟動時自動建立資料表與預設管理員
- Nmap + Nuclei 非同步掃描流程
- 弱點風險分數：`CVSS/Severity × Asset Criticality`
- Finding 狀態機：`Open -> Acknowledged -> Fixed -> Verified`，並保留 `Risk-Accepted`
- 狀態變更會寫入 `audit_logs`
- 掃描原始輸出會保存到 `ScanTask.raw_output_path`

## 部署方式

### 必要環境
- Docker
- Docker Compose

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

### 驗證
- 健康檢查：`GET http://localhost:8000/healthz`
- Swagger：`GET http://localhost:8000/docs`

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
  - `criticality`：1 到 5
  - `owner_id`
  - `env`：`Production` / `Staging` / `Development`

### 3. 觸發掃描
- `POST /scans/trigger`
- 參數：
  - `asset_id`
  - `scan_profile`

### 4. 查詢掃描狀態
- `GET /scans/{task_id}/status`

### 5. 查詢 findings
- `GET /findings`

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

## 目前行為重點
- 若 Nuclei severity 是文字等級（如 `critical`、`medium`），系統會先正規化為數值。
- 相同弱點會以 `template-id | matcher-name | info.name` 組成穩定 key，避免每次掃描都新增重複 `Vulnerability`。
- 相同 `asset + vulnerability` 會更新既有 `Finding`，而不是無限制重複新增。
- 已驗證關閉的 finding 若在後續掃描再次出現，會重新回到 `Open`。

## 注意事項
- `DEFAULT_ADMIN_PASSWORD` 僅適合首次啟動，正式環境必須立即更換。
- 目前資料表初始化採用啟動時 `create_all`，尚未導入 Alembic migration。
- 本機若要執行測試或啟動 API，需要先安裝 `requirements.txt` 內相依套件。
