# Changelog

## 2026-06-03
- 強化 `start_system.ps1`：將啟動等待時間延長至 180 秒，並同時檢查 API 健康狀態與 worker 容器是否 running
- 強化 `start_system.ps1`：啟動失敗時自動輸出 `api` / `worker` 的近期 logs，便於診斷 Docker 啟動問題
- 修正 `README.md` 的專案實際路徑，並同步更新一鍵啟動腳本的真實行為說明
- 新增 `.gitignore`，避免 `.env`、`__pycache__` 與虛擬環境檔案誤入版控

## 2026-06-02
- 補上 JWT 認證、密碼雜湊與 Bearer Token 解析
- 補上 `Admin` / `Analyst` / `Auditor` 的 RBAC 檢查
- 將 API 從 `src/api/main.py` 拆回 `src/api/endpoints/*`
- 啟動時自動建立資料表與預設管理員帳號
- 補上 finding 狀態機驗證與正確的 audit log 舊新值紀錄
- 修正掃描結果處理：severity 正規化、弱點去重、finding upsert、保存原始輸出
- 補上 `healthz` 健康檢查、`.env.example`、部署設定修正與基礎測試
- 清理既有 Python 檔案的 UTF-16/null-byte 編碼問題，統一為 UTF-8
- 新增可雙擊執行的 `start_system.bat`
- 強化 `start_system.ps1`：加入 `.env` / Docker / 健康檢查超時驗證
- 修正 `start_system.ps1`：Docker 檢查失敗即中止，並固定 `docker compose -p vulnshield-iso`
