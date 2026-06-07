using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public sealed class SystemCheckService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ApplicationDbContext dbContext,
    INmapService nmapService,
    INucleiService nucleiService,
    IGreenboneSettingsService greenboneSettingsService) : ISystemCheckService
{
    public async Task<SystemCheckIndexViewModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var providerSetting = configuration["Database:Provider"] ?? "SqlServer";
        var activeProvider = ResolveProviderLabel(dbContext.Database.ProviderName, providerSetting);
        var canConnect = await SafeCanConnectAsync(cancellationToken);
        var activeConnectionString = dbContext.Database.GetConnectionString() ?? configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var greenboneOptions = await greenboneSettingsService.GetEffectiveOptionsAsync(cancellationToken);
        var nmapStatus = nmapService.GetInstallationStatus();
        var nucleiInstalled = nucleiService.IsInstalled();
        var nucleiPath = nucleiService.GetInstallPath();

        var sqliteStatus = BuildSqliteStatus(activeProvider, providerSetting, activeConnectionString, canConnect);
        var msSqlStatus = BuildMsSqlStatus(activeProvider, providerSetting, activeConnectionString, canConnect);

        return new SystemCheckIndexViewModel
        {
            Nmap = new NmapCheckViewModel
            {
                IsInstalled = nmapStatus.IsInstalled,
                CanStartInstall = OperatingSystem.IsWindows() && !nmapStatus.IsInstalled,
                StatusText = nmapStatus.IsInstalled ? "已安裝" : "未安裝",
                ResolvedPath = string.IsNullOrWhiteSpace(nmapStatus.ResolvedPath) ? "未找到" : nmapStatus.ResolvedPath,
                Source = string.IsNullOrWhiteSpace(nmapStatus.Source) ? "未判定" : nmapStatus.Source,
                Message = nmapStatus.Message,
            },
            Nuclei = new NmapCheckViewModel
            {
                IsInstalled = nucleiInstalled,
                CanStartInstall = false,
                StatusText = nucleiInstalled ? "已安裝" : "未安裝",
                ResolvedPath = string.IsNullOrWhiteSpace(nucleiPath) ? "未找到" : nucleiPath,
                Source = nucleiInstalled ? "PATH" : "未安裝",
                Message = nucleiInstalled ? "可用" : "nuclei.exe 未安裝於系統 PATH 中，或 VulnScan:NucleiPath 未設定正確路徑",
            },
            Greenbone = BuildGreenboneStatus(greenboneOptions),
            ActiveDatabase = BuildActiveDatabaseStatus(activeProvider, canConnect, sqliteStatus, msSqlStatus),
            Sqlite = sqliteStatus,
            MsSql = msSqlStatus,
        };
    }

    private async Task<bool?> SafeCanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    private DatabaseCheckViewModel BuildActiveDatabaseStatus(
        string activeProvider,
        bool? canConnect,
        DatabaseCheckViewModel sqliteStatus,
        DatabaseCheckViewModel msSqlStatus)
    {
        var activeStatus = string.Equals(activeProvider, "SQLite", StringComparison.OrdinalIgnoreCase)
            ? sqliteStatus
            : msSqlStatus;

        return new DatabaseCheckViewModel
        {
            Label = "目前資料庫提供者",
            Provider = activeProvider,
            StatusText = canConnect == true ? "連線正常" : "連線異常",
            IsConfigured = true,
            IsActiveProvider = true,
            CanConnect = canConnect,
            Target = activeStatus.Target,
            Detail = activeStatus.Detail,
        };
    }

    private DatabaseCheckViewModel BuildSqliteStatus(
        string activeProvider,
        string providerSetting,
        string activeConnectionString,
        bool? canConnect)
    {
        var isConfigured = string.Equals(providerSetting, "Sqlite", StringComparison.OrdinalIgnoreCase)
            || activeConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

        if (!isConfigured)
        {
            return new DatabaseCheckViewModel
            {
                Label = "SQLite",
                Provider = "SQLite",
                StatusText = "未設定",
                IsConfigured = false,
                IsActiveProvider = false,
                CanConnect = null,
                Target = "未提供 SQLite 連線字串",
                Detail = "目前設定未使用 SQLite。",
            };
        }

        SqliteConnectionStringBuilder connectionBuilder;
        var dataSource = string.Empty;
        try
        {
            connectionBuilder = new SqliteConnectionStringBuilder(activeConnectionString);
            dataSource = connectionBuilder.DataSource;
        }
        catch
        {
            return new DatabaseCheckViewModel
            {
                Label = "SQLite",
                Provider = "SQLite",
                StatusText = "設定格式錯誤",
                IsConfigured = true,
                IsActiveProvider = string.Equals(activeProvider, "SQLite", StringComparison.OrdinalIgnoreCase),
                CanConnect = string.Equals(activeProvider, "SQLite", StringComparison.OrdinalIgnoreCase) ? canConnect : null,
                Target = "無法解析 SQLite 連線字串",
                Detail = "請檢查 ConnectionStrings:DefaultConnection 是否為有效的 SQLite 連線字串。",
            };
        }
        var resolvedPath = string.IsNullOrWhiteSpace(dataSource)
            ? "未指定檔案路徑"
            : Path.IsPathRooted(dataSource)
                ? dataSource
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, dataSource));
        var fileExists = !string.IsNullOrWhiteSpace(dataSource) && File.Exists(resolvedPath);
        var isActive = string.Equals(activeProvider, "SQLite", StringComparison.OrdinalIgnoreCase);

        return new DatabaseCheckViewModel
        {
            Label = "SQLite",
            Provider = "SQLite",
            StatusText = isActive
                ? canConnect == true ? "啟用中 / 連線正常" : "啟用中 / 連線異常"
                : "已設定但未啟用",
            IsConfigured = true,
            IsActiveProvider = isActive,
            CanConnect = isActive ? canConnect : null,
            Target = resolvedPath,
            Detail = fileExists
                ? "SQLite 檔案存在。"
                : "SQLite 路徑已解析，但資料檔目前不存在或尚未建立。",
        };
    }

    private DatabaseCheckViewModel BuildMsSqlStatus(
        string activeProvider,
        string providerSetting,
        string activeConnectionString,
        bool? canConnect)
    {
        var isConfigured = string.Equals(providerSetting, "SqlServer", StringComparison.OrdinalIgnoreCase);
        if (!isConfigured)
        {
            return new DatabaseCheckViewModel
            {
                Label = "MSSQL",
                Provider = "SQL Server",
                StatusText = "未啟用",
                IsConfigured = false,
                IsActiveProvider = false,
                CanConnect = null,
                Target = "目前未使用 SQL Server",
                Detail = "系統未以 SQL Server 作為目前資料庫提供者，也未主動探測外部 SQL Server。",
            };
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(activeConnectionString);
        }
        catch
        {
            return new DatabaseCheckViewModel
            {
                Label = "MSSQL",
                Provider = "SQL Server",
                StatusText = "設定格式錯誤",
                IsConfigured = true,
                IsActiveProvider = string.Equals(activeProvider, "SQL Server", StringComparison.OrdinalIgnoreCase),
                CanConnect = string.Equals(activeProvider, "SQL Server", StringComparison.OrdinalIgnoreCase) ? canConnect : null,
                Target = "無法解析 SQL Server 連線字串",
                Detail = "請檢查 ConnectionStrings:DefaultConnection 是否為有效的 SQL Server 連線字串。",
            };
        }
        var target = string.IsNullOrWhiteSpace(builder.DataSource)
            ? "未指定伺服器"
            : $"{builder.DataSource} / {builder.InitialCatalog}";
        var isActive = string.Equals(activeProvider, "SQL Server", StringComparison.OrdinalIgnoreCase);

        return new DatabaseCheckViewModel
        {
            Label = "MSSQL",
            Provider = "SQL Server",
            StatusText = isActive
                ? canConnect == true ? "啟用中 / 連線正常" : "啟用中 / 連線異常"
                : "已設定但未啟用",
            IsConfigured = true,
            IsActiveProvider = isActive,
            CanConnect = isActive ? canConnect : null,
            Target = target,
            Detail = isActive
                ? "這是目前應用程式正在使用的 SQL Server 連線。"
                : "設定上保留 SQL Server 模式，但目前未切換為 SQL Server，也未主動探測外部 SQL Server。",
        };
    }

    private static GreenboneCheckViewModel BuildGreenboneStatus(Models.GreenboneOptions options)
    {
        var hasEndpoint = !string.IsNullOrWhiteSpace(options.Host) && options.Port > 0;
        var hasAccount = !string.IsNullOrWhiteSpace(options.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(options.Password);
        var isConfigured = hasEndpoint && hasAccount && hasPassword;

        return new GreenboneCheckViewModel
        {
            IsConfigured = isConfigured,
            IsEnabled = options.Enabled,
            Endpoint = hasEndpoint ? $"{options.Host}:{options.Port}" : "未設定",
            Account = hasAccount ? options.Username : "未設定",
            StatusText = isConfigured
                ? options.Enabled ? "已設定 / 啟用中" : "已設定 / 目前停用"
                : "未完成設定",
            Message = isConfigured
                ? "Greenbone 連線參數已具備，可由自動匯入或手動同步使用。"
                : "至少需完成 Host、Port、Username 與 Password 才算完成 Greenbone 設定。",
        };
    }

    private static string ResolveProviderLabel(string? providerName, string providerSetting)
    {
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return "SQLite";
            }

            if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return "SQL Server";
            }
        }

        return string.Equals(providerSetting, "Sqlite", StringComparison.OrdinalIgnoreCase)
            ? "SQLite"
            : "SQL Server";
    }
}
