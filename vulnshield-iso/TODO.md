# TODO

- 導入 Alembic migration，取代啟動時 `create_all`
- 新增 integration tests，覆蓋登入、建立資產、觸發掃描與狀態流轉
- 將 `/reports/iso27001` 移入獨立 router 與 service
- 為 `authenticated_windows` 補上 WinRM / SMB / 本機安全設定盤點與弱點規則
- 為 `authenticated_linux` 補上 SSH 套件盤點、服務設定稽核與本機弱點檢查
- 為 credential 補上停用、刪除、使用審計、輪替與細粒度權限控管
- 補齊設備模板與 scan_profile 的權限、排程與審計規則，避免高強度掃描被誤用
- 為 Docker build 補上 image build 驗證流程，避免掃描器安裝路徑失效後才在手動啟動時發現
- 為 `docker compose up` 補上啟動整合測試，驗證 API、worker、db、redis 的實際依賴與健康狀態
- 為 Celery queue 補上任務註冊與 queue health 的 smoke test，避免掃描任務停在 `Pending`
- 將 Nmap 服務探測升級為結構化掃描輸出，而不只依賴文字解析
- 為 Nmap / Nuclei 執行失敗增加更完整的 observability 與 retry 策略
- 為 `start_system.ps1` 補上可選的 `--no-browser` / `-NoBrowser` 模式，避免在無桌面環境自動開啟 Swagger
- 為設備管理頁補上編輯設備、設備狀態變更、複製設備與排程掃描功能
- 為設備管理頁補上 per-device remediation 任務與負責人追蹤
- 為 dashboard 的 `報告` 分頁補上趨勢圖、掃描類型分布與可匯出報表
- 視需要補一鍵登入、建立測試資產、觸發掃描的自動化腳本
- 視需要補停止系統與清除容器的 `stop_system.ps1` / `stop_system.bat`
