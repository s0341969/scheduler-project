# Changelog

## 2026-03-30

- 建立 `RagServer.sln` 與 `Rag.Api` 專案
- 新增 PostgreSQL + pgvector Docker 部署檔 `docker-compose.yml`
- 新增資料庫初始化腳本 `infra/postgres/init.sql`
- 實作 RAG API：
  - `POST /api/rag/init` 初始化資料表與索引
  - `POST /api/rag/ingest` 掃描目錄 PDF、切塊、計算 embedding、寫入向量
  - `POST /api/rag/query` 向量檢索並產生回答與引用
- 串接 LM Studio OpenAI 相容 API（embeddings/chat completions）
- 將預設 LM Studio Base URL 設為 `http://10.1.1.123:1234/v1`
- PDF 文字擷取改採 `pdftotext`（poppler-utils）
- 新增 README 與 TODO
