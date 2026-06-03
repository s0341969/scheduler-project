# TODO

- 導入 Alembic migration，取代啟動時 `create_all`
- 新增 integration tests，覆蓋登入、建立資產、觸發掃描與狀態流轉
- 將 `/reports/iso27001` 移入獨立 router 與 service
- 為 Docker build 補上 image build 驗證流程，避免掃描器安裝路徑失效後才在手動啟動時發現
- 為 `docker compose up` 補上啟動整合測試，驗證 API、worker、db、redis 的實際依賴與健康狀態
- 為 Nmap / Nuclei 執行失敗增加更完整的 observability 與 retry 策略
- 為 `start_system.ps1` 補上可選的 `--no-browser` / `-NoBrowser` 模式，避免在無桌面環境自動開啟 Swagger
- 為設備管理頁補上編輯設備、設備狀態變更與排程掃描功能
- 為設備管理頁補上 per-device remediation 任務與負責人追蹤
- 視需要補一鍵登入、建立測試資產、觸發掃描的自動化腳本
- 視需要補停止系統與清除容器的 `stop_system.ps1` / `stop_system.bat`
