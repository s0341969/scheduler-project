namespace Rag.Api.Options;

public sealed class LmStudioOptions
{
    public const string SectionName = "LmStudio";

    public string BaseUrl { get; set; } = "http://127.0.0.1:1234/v1";
    public string ApiKey { get; set; } = "lm-studio";
    public string EmbeddingModel { get; set; } = "text-embedding-nomic-embed-text-v1.5";
    public string ChatModel { get; set; } = "qwen2.5-7b-instruct";
    public double Temperature { get; set; } = 0.1;
    public int MaxTokens { get; set; } = 512;
}
