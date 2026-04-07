using SqlMaintenanceAgent.App;
using SqlMaintenanceAgent.App.Configuration;
using SqlMaintenanceAgent.App.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var options = AgentOptions.Load(args);
using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(options.Llm.TimeoutSeconds)
};

var llmClient = new LlmClient(httpClient, options.Llm);
var promptPolicy = new PromptPolicy();
var sqlGuard = new SqlGuard(options.Security);
var sqlExecutor = new SqlExecutor(options.Database, options.Security);
var auditLogger = new AuditLogger(options.Audit);

var cliHost = new CliHost(options, llmClient, promptPolicy, sqlGuard, sqlExecutor, auditLogger);
await cliHost.RunAsync();
