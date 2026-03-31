# TODO

- [ ] 在 PostgreSQL 主機 `10.1.1.12` 驗證 `CREATE EXTENSION vector;` 可成功執行
- [ ] 在 API 主機驗證可連線 LM Studio `10.1.1.123:1234`
- [ ] 在 Windows 設定 `Rag:PdfToTextPath` 或 PATH，並完成 PDF 匯入 smoke test
- [ ] 針對掃描型 PDF 加入 OCR 流程（例如 Tesseract）
- [ ] 增加 ingest 排程（例如每天凌晨增量重建）
- [ ] 新增 API 身分驗證（JWT 或 API Key）
- [ ] 將查詢與匯入加入結構化日誌與追蹤（OpenTelemetry）
- [ ] 撰寫整合測試（包含 LM Studio mock）
