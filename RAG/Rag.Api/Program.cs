using Rag.Api.Data;
using Rag.Api.Models;
using Rag.Api.Options;
using Rag.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
builder.Services.Configure<LmStudioOptions>(builder.Configuration.GetSection(LmStudioOptions.SectionName));

builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<RagRepository>();
builder.Services.AddSingleton<TextChunker>();
builder.Services.AddSingleton<PdfTextExtractor>();
builder.Services.AddHttpClient<LmStudioClient>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.MapPost("/api/rag/init", async (DatabaseInitializer initializer, CancellationToken ct) =>
{
    await initializer.InitializeAsync(ct);
    return Results.Ok(new { message = "資料表與索引初始化完成" });
});

app.MapPost("/api/rag/ingest", async (
    IngestRequest request,
    RagRepository repository,
    PdfTextExtractor pdfTextExtractor,
    TextChunker chunker,
    LmStudioClient lmStudioClient,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.DirectoryPath) || !Directory.Exists(request.DirectoryPath))
    {
        return Results.BadRequest(new { message = "DirectoryPath 無效或不存在" });
    }

    var files = Directory.EnumerateFiles(
            request.DirectoryPath,
            "*.pdf",
            request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
        .ToList();

    if (files.Count == 0)
    {
        return Results.Ok(new IngestResponse(0, 0, 0, "目錄中沒有 PDF"));
    }

    var ingestedDocuments = 0;
    var skippedDocuments = 0;
    var totalChunks = 0;

    foreach (var file in files)
    {
        ct.ThrowIfCancellationRequested();

        var hash = await FileHasher.Sha256Async(file, ct);
        var needsIngest = await repository.ShouldIngestDocumentAsync(file, hash, ct);
        if (!needsIngest)
        {
            skippedDocuments++;
            continue;
        }

        var pageTexts = pdfTextExtractor.ExtractPages(file);
        if (pageTexts.Count == 0)
        {
            skippedDocuments++;
            continue;
        }

        var chunks = chunker.ChunkPages(pageTexts);
        if (chunks.Count == 0)
        {
            skippedDocuments++;
            continue;
        }

        var chunkRows = new List<ChunkRow>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var vector = await lmStudioClient.CreateEmbeddingAsync(chunk.Content, ct);
            chunkRows.Add(new ChunkRow(chunk.PageNumber, chunk.ChunkIndex, chunk.Content, vector));
        }

        await repository.UpsertDocumentWithChunksAsync(file, hash, chunkRows, ct);

        ingestedDocuments++;
        totalChunks += chunkRows.Count;
    }

    return Results.Ok(new IngestResponse(files.Count, ingestedDocuments, skippedDocuments, $"總共建立 {totalChunks} 個 chunks"));
});

app.MapPost("/api/rag/query", async (
    QueryRequest request,
    RagRepository repository,
    LmStudioClient lmStudioClient,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest(new { message = "Question 不可空白" });
    }

    var topK = request.TopK is > 0 and <= 20 ? request.TopK.Value : 5;
    var questionVector = await lmStudioClient.CreateEmbeddingAsync(request.Question, ct);
    var matches = await repository.SearchSimilarChunksAsync(questionVector, topK, ct);

    if (matches.Count == 0)
    {
        return Results.Ok(new QueryResponse("找不到可用資料，請先建立索引或調整問題。", []));
    }

    var contextLines = matches
        .Select((m, i) => $"[{i + 1}] 檔案: {m.SourcePath}, 頁: {m.PageNumber}, 相似度: {m.Similarity:F4}\n{m.Content}")
        .ToList();

    var prompt =
        "你是一個企業知識庫助理。請只依據提供的內容回答，若內容不足請明確說明不知道。" + Environment.NewLine +
        "回答規則：" + Environment.NewLine +
        "1. 使用繁體中文。" + Environment.NewLine +
        "2. 先給簡短結論，再給依據。" + Environment.NewLine +
        "3. 回答結尾列出引用來源（檔案與頁碼）。" + Environment.NewLine +
        Environment.NewLine +
        "問題：" + request.Question + Environment.NewLine +
        Environment.NewLine +
        "可用內容：" + Environment.NewLine +
        string.Join(Environment.NewLine + Environment.NewLine, contextLines);

    var answer = await lmStudioClient.CreateAnswerAsync(prompt, ct);

    var citations = matches
        .Select(m => new Citation(m.SourcePath, m.PageNumber, Math.Round(m.Similarity, 4)))
        .ToList();

    return Results.Ok(new QueryResponse(answer, citations));
});

app.Run();
