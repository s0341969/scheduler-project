# RAG for C# WinForms (PostgreSQL + pgvector + LM Studio)

本專案提供可直接落地的後端 API，讓 WinForms 系統可查詢 NAS 上 PDF 建立的 RAG 資料庫。

## 架構

- NAS：存放原始 PDF 檔案
- Rag.Api：負責 PDF 匯入、切塊、embedding、向量檢索、回答生成
- PostgreSQL + pgvector：儲存 chunk 與向量
- LM Studio：提供 embedding 與 chat 模型（目前預設 `http://10.1.1.123:1234/v1`）

## 必要條件

- .NET 9 SDK
- Docker / Docker Compose（Linux 主機建議）
- LM Studio 已啟動 API Server
- Embedding 模型：`text-embedding-nomic-embed-text-v1.5`
- Chat 模型：任一可用指令模型（預設 `qwen2.5-7b-instruct`）
- Linux 安裝 `pdftotext`：

```bash
sudo apt update
sudo apt install -y poppler-utils
```

## 設定

請調整 `Rag.Api/appsettings.json`：

- `Rag:ConnectionString`：PostgreSQL 連線字串
- `LmStudio:BaseUrl`：預設 `http://10.1.1.123:1234/v1`
- `LmStudio:EmbeddingModel`：`text-embedding-nomic-embed-text-v1.5`
- `LmStudio:ChatModel`：你的 LM Studio 已載入模型名稱

## 啟動步驟

1. 啟動 PostgreSQL

```bash
docker compose up -d
```

2. 還原與建置

```bash
dotnet restore
dotnet build RagServer.sln
```

3. 啟動 API

```bash
dotnet run --project Rag.Api
```

## API

### 1) 初始化資料表

`POST /api/rag/init`

### 2) 匯入 NAS PDF

`POST /api/rag/ingest`

Request body:

```json
{
  "directoryPath": "\\\\10.1.1.10\\share\\pdf",
  "recursive": true
}
```

### 3) 查詢

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

## WinForms 串接建議

- WinForms 只呼叫 `/api/rag/query`
- UI 顯示答案與來源列表（檔案+頁碼）
- 建立「重建索引」按鈕呼叫 `/api/rag/ingest`

## 目前行為與限制

- 採用增量索引（依檔案 SHA-256 判斷是否需重建）
- chunk 策略為字元長度切分（可在 `Rag:ChunkSize/ChunkOverlap` 調整）
- 目前 PDF 抽取依賴 `pdftotext`（掃描 PDF 需另接 OCR）
