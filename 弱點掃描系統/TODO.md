# TODO

- 導入 Alembic migration，取代啟動時 `create_all`
- 為 `GET /findings` 加上 Analyst 僅可查看自己資產 finding 的限制
- 新增 integration tests，覆蓋登入、建立資產、觸發掃描與狀態流轉
- 將 `/reports/iso27001` 移入獨立 router 與 service
- 為 Nmap / Nuclei 執行失敗增加更完整的 observability 與 retry 策略
- 補上 `.gitignore` 與本地開發虛擬環境規範
- 視需要補一鍵登入、建立測試資產、觸發掃描的自動化腳本
