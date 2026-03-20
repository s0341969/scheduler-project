# RAG API (C# + PostgreSQL pgvector)

本專案提供可直接部署的 RAG 後端，目標是讓既有 C# 系統只需呼叫 API，就能知道「應該看哪些 PDF 與頁碼」。

## 功能

- `POST /api/rag/index-folder`：批次掃描資料夾中的 PDF，切塊後寫入 pgvector。
- `POST /api/rag/retrieve`：輸入查詢字串，回傳最相關的文件、頁碼、片段與分數。
- 啟動時自動建立 `vector` extension 與資料表（若不存在）。
- 透過檔案 SHA-256 避免重複索引未變更檔案。

## 技術選型

- Runtime: .NET 9 (ASP.NET Core Web API)
- Database: PostgreSQL + pgvector
- PDF 解析: UglyToad.PdfPig
- Embedding Provider: OpenAI Embeddings API

## 快速開始

1. 啟動 PostgreSQL + pgvector

```bash
cd G:\codex_pg\skills
docker compose up -d
```

2. 設定 OpenAI API Key

- 開啟 `RagApi/appsettings.json`
- 設定 `OpenAI:ApiKey`

3. 建置與啟動

```bash
dotnet build RagSolution.sln
dotnet run --project RagApi/RagApi.csproj
```

4. 準備 PDF

- 將 PDF 放進 `RagApi/pdfs`（可自行調整 `Rag:PdfRootPath`）

5. 建立索引

```http
POST /api/rag/index-folder
Content-Type: application/json

{
  "folderPath": "G:\\codex_pg\\skills\\RagApi\\pdfs",
  "maxFiles": 100
}
```

6. 執行查詢

```http
POST /api/rag/retrieve
Content-Type: application/json

{
  "query": "公司差旅報銷規則是什麼？",
  "topK": 5,
  "userId": "u123"
}
```

## 設定說明

`RagApi/appsettings.json`

- `Rag:ConnectionString`：PostgreSQL 連線字串
- `Rag:PdfRootPath`：預設 PDF 資料夾
- `Rag:ChunkSize`：每塊字元長度
- `Rag:ChunkOverlap`：塊與塊重疊字元
- `Rag:DefaultTopK`：預設回傳筆數
- `Rag:MaxTopK`：可接受最大回傳筆數
- `OpenAI:ApiKey`：OpenAI API Key
- `OpenAI:EmbeddingModel`：Embedding 模型名稱

## API 回傳格式

`POST /api/rag/retrieve`

```json
{
  "results": [
    {
      "documentId": "G:\\codex_pg\\skills\\RagApi\\pdfs\\travel_policy.pdf",
      "title": "travel_policy.pdf",
      "page": 12,
      "score": 0.913421,
      "snippet": "國內差旅住宿上限為..."
    }
  ]
}
```

## 安全與穩定建議

- 生產環境請改用環境變數注入 `OpenAI:ApiKey`，不要把金鑰寫入檔案。
- 建議在 API Gateway 或反向代理層加上認證與 rate limit。
- 建議為 `retrieve` 加入使用者權限過濾（目前保留 `userId` 欄位可擴充）。
- 大量匯入時建議做背景工作排程，避免前台 API timeout。

## Docker (PostgreSQL + pgvector)

請使用根目錄的 `docker-compose.yml`：

- DB: `localhost:5432`
- Database: `ragdb`
- User: `rag_user`
- Password: `rag_pass`
