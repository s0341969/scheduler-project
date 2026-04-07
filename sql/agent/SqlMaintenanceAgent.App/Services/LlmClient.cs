using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SqlMaintenanceAgent.App.Configuration;
using SqlMaintenanceAgent.App.Models;

namespace SqlMaintenanceAgent.App.Services;

public sealed class LlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;

    public LlmClient(HttpClient httpClient, LlmOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<LlmAdvisoryResponse> GetAdvisoryAsync(
        AgentCommandType commandType,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var requestPayload = new
        {
            model = _options.Model,
            temperature = 0.1,
            max_tokens = _options.MaxTokens,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var responseContent = await SendWithRetryAsync(requestPayload, cancellationToken);
        return ParseAdvisoryResponse(responseContent);
    }

    public static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private async Task<string> SendWithRetryAsync(object payload, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(payload);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.RetryCount; attempt++)
        {
            using var content = new StringContent(serialized, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (IsTransientStatusCode(response.StatusCode) && attempt < _options.RetryCount)
                    {
                        await Task.Delay(GetDelay(attempt), cancellationToken);
                        continue;
                    }

                    throw new InvalidOperationException($"LM Studio API 錯誤：{(int)response.StatusCode} {response.ReasonPhrase}, body={body}");
                }

                using var responseJson = JsonDocument.Parse(body);
                var messageNode = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(messageNode))
                {
                    throw new InvalidOperationException("LM Studio API 回傳內容為空。");
                }

                return messageNode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
                if (attempt < _options.RetryCount)
                {
                    await Task.Delay(GetDelay(attempt), cancellationToken);
                    continue;
                }
            }
        }

        throw new InvalidOperationException("LM Studio API 呼叫失敗。", lastException);
    }

    private static TimeSpan GetDelay(int attempt)
    {
        var seconds = Math.Min(8, Math.Pow(2, attempt));
        return TimeSpan.FromSeconds(seconds);
    }

    private static LlmAdvisoryResponse ParseAdvisoryResponse(string llmContent)
    {
        var json = ExtractJsonObject(llmContent);
        var response = JsonSerializer.Deserialize<LlmAdvisoryResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (response is null)
        {
            throw new InvalidOperationException("模型回覆格式無法解析。");
        }

        return response with
        {
            RiskLevel = string.IsNullOrWhiteSpace(response.RiskLevel) ? "MEDIUM" : response.RiskLevel.ToUpperInvariant()
        };
    }

    public static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("模型回覆不含有效 JSON。");
        }

        return text[start..(end + 1)];
    }
}
