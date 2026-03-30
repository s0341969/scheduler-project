using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Rag.Api.Options;

namespace Rag.Api.Services;

public sealed class LmStudioClient
{
    private readonly HttpClient _httpClient;
    private readonly LmStudioOptions _options;

    public LmStudioClient(HttpClient httpClient, IOptions<LmStudioOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken ct)
    {
        var payload = new EmbeddingRequest(_options.EmbeddingModel, input);
        using var response = await _httpClient.PostAsJsonAsync("embeddings", payload, ct);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("LM Studio embeddings 回應為空");

        var vector = data.Data.FirstOrDefault()?.Embedding;
        if (vector is null || vector.Count == 0)
        {
            throw new InvalidOperationException("LM Studio embeddings 缺少向量內容");
        }

        return vector.ToArray();
    }

    public async Task<string> CreateAnswerAsync(string prompt, CancellationToken ct)
    {
        var payload = new ChatRequest(
            _options.ChatModel,
            [new ChatMessage("system", "你是嚴謹的企業知識庫助理。"), new ChatMessage("user", prompt)],
            _options.Temperature,
            _options.MaxTokens);

        using var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, ct);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("LM Studio chat 回應為空");

        var content = data.Choices.FirstOrDefault()?.Message.Content;
        return string.IsNullOrWhiteSpace(content) ? "模型未回傳內容。" : content.Trim();
    }

    private sealed record EmbeddingRequest(string Model, string Input);

    private sealed record EmbeddingResponse(List<EmbeddingItem> Data);

    private sealed record EmbeddingItem([property: JsonPropertyName("embedding")] List<float> Embedding);

    private sealed record ChatRequest(string Model, List<ChatMessage> Messages, double Temperature, int MaxTokens);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatResponse(List<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
