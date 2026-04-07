using System.Text.Json;

namespace SqlMaintenanceAgent.App.Configuration;

public sealed record AgentOptions
{
    public bool AllowWrite { get; init; }
    public LlmOptions Llm { get; init; } = new();
    public DatabaseOptions Database { get; init; } = new();
    public SecurityOptions Security { get; init; } = new();
    public AuditOptions Audit { get; init; } = new();

    public static AgentOptions Load(string[] args)
    {
        var options = new AgentOptions();
        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "agent", "SqlMaintenanceAgent.App", "appsettings.json");
        }

        if (File.Exists(appSettingsPath))
        {
            var json = File.ReadAllText(appSettingsPath);
            var loaded = JsonSerializer.Deserialize<AgentOptions>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (loaded is not null)
            {
                options = loaded;
            }
        }

        options = ApplyEnvironment(options);
        options = ApplyArguments(options, args);
        return options;
    }

    private static AgentOptions ApplyEnvironment(AgentOptions options)
    {
        return options with
        {
            Llm = options.Llm with
            {
                BaseUrl = Environment.GetEnvironmentVariable("SQL_AGENT_LLM_BASE_URL") ?? options.Llm.BaseUrl,
                ApiKey = Environment.GetEnvironmentVariable("SQL_AGENT_LLM_API_KEY") ?? options.Llm.ApiKey,
                Model = Environment.GetEnvironmentVariable("SQL_AGENT_LLM_MODEL") ?? options.Llm.Model,
                TimeoutSeconds = ParseInt(Environment.GetEnvironmentVariable("SQL_AGENT_LLM_TIMEOUT_SECONDS"), options.Llm.TimeoutSeconds),
                MaxTokens = ParseInt(Environment.GetEnvironmentVariable("SQL_AGENT_LLM_MAX_TOKENS"), options.Llm.MaxTokens),
                RetryCount = ParseInt(Environment.GetEnvironmentVariable("SQL_AGENT_LLM_RETRY_COUNT"), options.Llm.RetryCount)
            },
            Database = options.Database with
            {
                ProviderName = Environment.GetEnvironmentVariable("SQL_AGENT_DB_PROVIDER") ?? options.Database.ProviderName,
                ConnectionString = Environment.GetEnvironmentVariable("SQL_AGENT_DB_CONNECTION_STRING") ?? options.Database.ConnectionString,
                CommandTimeoutSeconds = ParseInt(Environment.GetEnvironmentVariable("SQL_AGENT_DB_COMMAND_TIMEOUT_SECONDS"), options.Database.CommandTimeoutSeconds),
                MaxRows = ParseInt(Environment.GetEnvironmentVariable("SQL_AGENT_DB_MAX_ROWS"), options.Database.MaxRows)
            },
            Security = options.Security with
            {
                ReadOnly = ParseBool(Environment.GetEnvironmentVariable("SQL_AGENT_READ_ONLY"), options.Security.ReadOnly)
            },
            Audit = options.Audit with
            {
                LogDirectory = Environment.GetEnvironmentVariable("SQL_AGENT_AUDIT_DIR") ?? options.Audit.LogDirectory
            }
        };
    }

    private static AgentOptions ApplyArguments(AgentOptions options, string[] args)
    {
        var allowWrite = args.Any(arg => string.Equals(arg, "--allow-write", StringComparison.OrdinalIgnoreCase));
        return options with { AllowWrite = options.AllowWrite || allowWrite };
    }

    private static int ParseInt(string? input, int defaultValue)
    {
        return int.TryParse(input, out var value) && value > 0 ? value : defaultValue;
    }

    private static bool ParseBool(string? input, bool defaultValue)
    {
        return bool.TryParse(input, out var parsed) ? parsed : defaultValue;
    }
}

public sealed record LlmOptions
{
    public string BaseUrl { get; init; } = "http://127.0.0.1:1234/v1";
    public string ApiKey { get; init; } = "lm-studio";
    public string Model { get; init; } = "local-model";
    public int TimeoutSeconds { get; init; } = 120;
    public int MaxTokens { get; init; } = 1200;
    public int RetryCount { get; init; } = 3;
}

public sealed record DatabaseOptions
{
    public string ProviderName { get; init; } = "System.Data.SqlClient";
    public string ConnectionString { get; init; } = "Server=10.1.1.76;Database=TEST;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;";
    public int CommandTimeoutSeconds { get; init; } = 120;
    public int MaxRows { get; init; } = 200;
}

public sealed record SecurityOptions
{
    public bool ReadOnly { get; init; } = true;
}

public sealed record AuditOptions
{
    public string LogDirectory { get; init; } = "agent/logs";
}
