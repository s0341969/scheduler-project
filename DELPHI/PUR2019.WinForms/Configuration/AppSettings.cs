using System.Text.Json;

namespace PUR2019.WinForms.Configuration;

public sealed record AppSettings
{
    public DataSourceSettings DataSource { get; init; } = new();

    public static AppSettings Load(string baseDirectory)
    {
        var settingsPath = Path.Combine(baseDirectory, "appsettings.json");
        AppSettings settings;

        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppSettings();
        }
        else
        {
            settings = new AppSettings();
        }

        var modeFromEnv = Environment.GetEnvironmentVariable("PUR2019_DATA_SOURCE");
        if (!string.IsNullOrWhiteSpace(modeFromEnv))
        {
            settings = settings with
            {
                DataSource = settings.DataSource with
                {
                    Mode = modeFromEnv.Trim()
                }
            };
        }

        var connFromEnv = Environment.GetEnvironmentVariable("PUR2019_ODBC_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connFromEnv))
        {
            settings = settings with
            {
                DataSource = settings.DataSource with
                {
                    OdbcConnectionString = connFromEnv.Trim()
                }
            };
        }

        var legacyCheckFromEnv = Environment.GetEnvironmentVariable("PUR2019_ENABLE_LEGACY_SP_CHECKS");
        if (!string.IsNullOrWhiteSpace(legacyCheckFromEnv) && bool.TryParse(legacyCheckFromEnv, out var legacyEnabled))
        {
            settings = settings with
            {
                DataSource = settings.DataSource with
                {
                    EnableLegacyStoredProcedureChecks = legacyEnabled
                }
            };
        }

        return settings;
    }
}

public sealed record DataSourceSettings
{
    public string Mode { get; init; } = "InMemory";

    public string OdbcConnectionString { get; init; } = string.Empty;

    public bool EnableLegacyStoredProcedureChecks { get; init; } = false;
}
