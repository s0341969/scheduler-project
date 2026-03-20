# TODO

- [ ] 依 `userId` 實作文件層級 ACL 權限過濾，避免跨租戶資料被檢索。
- [ ] 加入 OCR 流程，支援掃描型 PDF。
- [ ] 將 `index-folder` 轉為背景工作（Queue/Worker）以支援大量文件。
- [ ] 新增整合測試（需測試 PostgreSQL 容器）。
- [ ] 增加觀測性：查詢延遲、命中率、token 成本、失敗率。
