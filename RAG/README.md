# RAG for C# WinForms (PostgreSQL + pgvector + LM Studio)

本專案提供可直接落地的後端 API，讓 WinForms 系統可查詢 NAS 上 PDF 建立的 RAG 資料庫。

## 架構

- NAS：存放原始 PDF 檔案
- Rag.Api：負責 PDF 匯入、切塊、embedding、向量檢索、回答生成
- PostgreSQL + pgvector：儲存 chunk 與向量
- LM Studio：提供 embedding 與 chat 模型

## 目前預設連線

- LM Studio：`http://10.1.1.123:1234/v1`
- PostgreSQL：`Host=10.1.1.12;Port=5432;Database=ragdb;Username=raguser;Password=ragpass`

## Windows 安裝重點

### 1) PostgreSQL (Windows)

可使用 PostgreSQL 官方安裝程式（建議 16 版）。

### 2) pgvector

必須能執行 `CREATE EXTENSION vector;` 才能做向量檢索。

若你的 Windows PostgreSQL 無法直接安裝 pgvector，建議改用 Docker 版 PostgreSQL + pgvector：

```powershell
docker run -d --name rag-postgres -e POSTGRES_DB=ragdb -e POSTGRES_USER=raguser -e POSTGRES_PASSWORD=ragpass -p 5432:5432 pgvector/pgvector:pg16
```

### 3) pdftotext (Windows)

本專案匯入 PDF 使用 `pdftotext.exe`。

- 安裝 Poppler for Windows
- 將 `pdftotext.exe` 加入 PATH
- 或在 `Rag:PdfToTextPath` 設定完整路徑

範例：

```json
"Rag": {
  "PdfToTextPath": "C:\\Program Files\\poppler\\Library\\bin\\pdftotext.exe"
}
```

## Linux 依賴（若部署在 Linux）

```bash
sudo apt update
sudo apt install -y poppler-utils
```

## 設定

請調整 `Rag.Api/appsettings.json`：

- `Rag:ConnectionString`：PostgreSQL 連線字串
- `Rag:PdfToTextPath`：Windows 可指定 `pdftotext.exe` 完整路徑
- `LmStudio:BaseUrl`：LM Studio API 位址
- `LmStudio:EmbeddingModel`：`text-embedding-nomic-embed-text-v1.5`
- `LmStudio:ChatModel`：你的 LM Studio 已載入模型名稱

## 啟動步驟

1. 建立資料表（首次）

`POST /api/rag/init`

2. 匯入 NAS PDF

`POST /api/rag/ingest`

Request body:

```json
{
  "directoryPath": "\\\\10.1.1.10\\share\\pdf",
  "recursive": true
}
```

3. 查詢

`POST /api/rag/query`

Request body:

```json
{
  "question": "請問請款流程的核准條件是什麼？",
  "topK": 5
}
```

Response 會回傳：

- `answer`：模型回答
- `citations`：引用來源（檔案路徑、頁碼、相似度）

## 目前行為與限制

- 採用增量索引（依檔案 SHA-256 判斷是否需重建）
- chunk 策略為字元長度切分（可在 `Rag:ChunkSize/ChunkOverlap` 調整）
- 僅支援可抽取文字的 PDF（掃描 PDF 需另接 OCR）
