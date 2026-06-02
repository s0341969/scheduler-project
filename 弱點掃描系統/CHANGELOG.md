# Changelog

## 2026-06-02
- 補上 JWT 認證、密碼雜湊與 Bearer Token 解析
- 補上 `Admin` / `Analyst` / `Auditor` 的 RBAC 檢查
- 將 API 從 `src/api/main.py` 拆回 `src/api/endpoints/*`
- 啟動時自動建立資料表與預設管理員帳號
- 補上 finding 狀態機驗證與正確的 audit log 舊新值紀錄
- 修正掃描結果處理：severity 正規化、弱點去重、finding upsert、保存原始輸出
- 補上 `healthz` 健康檢查、`.env.example`、部署設定修正與基礎測試
- 清理既有 Python 檔案的 UTF-16/null-byte 編碼問題，統一為 UTF-8
