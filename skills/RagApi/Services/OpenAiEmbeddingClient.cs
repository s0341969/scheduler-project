using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagApi.Options;

namespace RagApi.Services;

public sealed class OpenAiEmbeddingClient(HttpClient httpClient, IOptions<OpenAiOptions> options, ILogger<OpenAiEmbeddingClient> logger)
{
    private readonly OpenAiOptions _options = options.Value;

    public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API 金鑰未設定。請設定 OpenAI:ApiKey。");
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("輸入文字不可為空。", nameof(input));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildEmbeddingsEndpoint());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new EmbeddingRequest
        {
            Input = input,
            Model = _options.EmbeddingModel
        });

        httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds));
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("OpenAI Embeddings API 失敗: {StatusCode}, {Error}", response.StatusCode, error);
            throw new InvalidOperationException("建立 embedding 失敗，請檢查 API 金鑰與模型設定。");
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        var vector = payload?.Data?.FirstOrDefault()?.Embedding;

        if (vector is null || vector.Length == 0)
        {
            throw new InvalidOperationException("Embedding 回應為空。請確認模型與輸入內容。");
        }

        return vector;
    }

    private string BuildEmbeddingsEndpoint()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/embeddings";
    }

    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = [];
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}
