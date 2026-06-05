using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Fonts;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var defaultConnectionString = ResolveConnectionString(builder.Configuration, builder.Environment, databaseProvider);
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");

Directory.CreateDirectory(dataProtectionKeysPath);
GlobalFontSettings.FontResolver ??= new PdfFontResolver();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<VulnScanOptions>(builder.Configuration.GetSection(VulnScanOptions.SectionName));
builder.Services.Configure<LocalAuthOptions>(builder.Configuration.GetSection(LocalAuthOptions.SectionName));
builder.Services.Configure<AutoImportOptions>(builder.Configuration.GetSection(AutoImportOptions.SectionName));
builder.Services.Configure<GreenboneOptions>(builder.Configuration.GetSection(GreenboneOptions.SectionName));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("VulnScan.Web");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (IsSqliteProvider(databaseProvider))
    {
        options.UseSqlite(defaultConnectionString);
        return;
    }

    options.UseSqlServer(defaultConnectionString);
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "VulnScan.Auth";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddHangfire(configuration =>
{
    configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    if (IsSqliteProvider(databaseProvider))
    {
        configuration.UseMemoryStorage();
        return;
    }

    configuration.UseSqlServerStorage(defaultConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
    });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(1, Environment.ProcessorCount / 2);
});

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAutoImportService, AutoImportService>();
builder.Services.AddScoped<IGreenboneGmpClient, GreenboneGmpClient>();
builder.Services.AddScoped<IGreenboneImportService, GreenboneImportService>();
builder.Services.AddScoped<IGreenboneSettingsService, GreenboneSettingsService>();
builder.Services.AddScoped<ISystemCheckService, SystemCheckService>();
builder.Services.AddHttpClient<INmapInstallerService, NmapInstallerService>();
builder.Services.AddScoped<IScanAllowedRangeService, ScanAllowedRangeService>();
builder.Services.AddScoped<INmapService, NmapService>();
builder.Services.AddScoped<INmapXmlParserService, NmapXmlParserService>();
builder.Services.AddScoped<IScanJobService, ScanJobService>();
builder.Services.AddScoped<IVulnerabilityService, VulnerabilityService>();
builder.Services.AddScoped<IScanImportService, ScanImportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHostedService<AutoImportBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var localAuthOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocalAuthOptions>>().Value;
    try
    {
        await DbInitializer.InitializeAsync(dbContext, passwordHasher, localAuthOptions);
    }
    catch (SqlException exception)
    {
        WriteDatabaseStartupError(app.Configuration, app.Environment, databaseProvider, exception);
        throw;
    }
    catch (InvalidOperationException exception) when (exception.InnerException is SqlException sqlException)
    {
        WriteDatabaseStartupError(app.Configuration, app.Environment, databaseProvider, sqlException);
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard(builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire");
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static bool IsSqliteProvider(string databaseProvider) =>
    string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase);

static string ResolveConnectionString(IConfiguration configuration, IWebHostEnvironment environment, string databaseProvider)
{
    var configured = configuration.GetConnectionString("DefaultConnection");
    if (!IsSqliteProvider(databaseProvider))
    {
        return configured ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");
    }

    var sqliteConnection = configured ?? "Data Source=App_Data\\vulnscan-dev.db";
    var builder = new SqliteConnectionStringBuilder(sqliteConnection);
    var dataSource = builder.DataSource;

    if (string.IsNullOrWhiteSpace(dataSource))
    {
        builder.DataSource = Path.Combine(environment.ContentRootPath, "App_Data", "vulnscan-dev.db");
    }
    else if (!Path.IsPathRooted(dataSource))
    {
        builder.DataSource = Path.Combine(environment.ContentRootPath, dataSource);
    }

    var finalDataSource = builder.DataSource;
    var directoryPath = Path.GetDirectoryName(finalDataSource);
    if (!string.IsNullOrWhiteSpace(directoryPath))
    {
        Directory.CreateDirectory(directoryPath);
    }

    return builder.ToString();
}

static void WriteDatabaseStartupError(IConfiguration configuration, IWebHostEnvironment environment, string databaseProvider, SqlException exception)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "(missing)";
    var environmentName = environment.EnvironmentName;

    Console.Error.WriteLine("==================================================");
    Console.Error.WriteLine("VulnScan.Web startup failed: database connection error.");
    Console.Error.WriteLine($"Environment : {environmentName}");
    Console.Error.WriteLine($"Provider    : {databaseProvider}");
    Console.Error.WriteLine($"Connection  : {connectionString}");
    Console.Error.WriteLine($"SQL Error   : {exception.Message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Recommended actions:");
    Console.Error.WriteLine("1. Development mode should normally use SQLite and should not require LocalDB.");
    Console.Error.WriteLine("2. If you intentionally switched back to SQL Server, verify the connection string in:");
    Console.Error.WriteLine("   VulnScan.Web/appsettings.json");
    Console.Error.WriteLine("   VulnScan.Web/appsettings.Development.json");
    Console.Error.WriteLine("3. If you need MSSQL for production, keep it in appsettings.json and deploy with a reachable SQL Server instance.");
    Console.Error.WriteLine("4. Then rerun start_vulnscan_web.bat.");
    Console.Error.WriteLine("==================================================");
}
