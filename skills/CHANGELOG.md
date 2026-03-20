# Changelog

## 2026-03-20

- 新增 `RagSolution.sln` 與 `RagApi` ASP.NET Core Web API 專案。
- 新增 `POST /api/rag/index-folder`：支援掃描資料夾、PDF 解析、切塊、embedding、寫入 pgvector。
- 新增 `POST /api/rag/retrieve`：輸入查詢字串回傳文件路徑、頁碼、片段與相似度分數。
- 新增 PostgreSQL schema 自動初始化（`vector` extension、`documents`、`chunks` 與索引）。
- 新增 OpenAI Embeddings 整合與錯誤處理。
- 新增 `docker-compose.yml`（PostgreSQL + pgvector）與根目錄文件。
