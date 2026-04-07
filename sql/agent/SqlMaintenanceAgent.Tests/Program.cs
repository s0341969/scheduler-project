using System.Net;
using SqlMaintenanceAgent.App.Configuration;
using SqlMaintenanceAgent.App.Services;

var tests = new List<(string Name, Func<Task> Test)>
{
    ("SqlGuard_Allow_Select", Tests.SqlGuardAllowSelect),
    ("SqlGuard_Block_UpdateWithoutWhere", Tests.SqlGuardBlockUpdateWithoutWhere),
    ("SqlGuard_Block_DeleteWithoutWhere", Tests.SqlGuardBlockDeleteWithoutWhere),
    ("SqlGuard_ReadOnlyBlocksWrite", Tests.SqlGuardReadOnlyBlocksWrite),
    ("LlmClient_TransientStatus", Tests.LlmClientTransientStatus),
    ("LlmClient_ExtractJson", Tests.LlmClientExtractJson),
    ("SqlExecutor_ThrowOnCancelledToken", Tests.SqlExecutorThrowOnCancelledToken)
};

var failed = new List<string>();
foreach (var (name, test) in tests)
{
    try
    {
        await test();
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception ex)
    {
        failed.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[FAIL] {name} -> {ex.Message}");
    }
}

if (failed.Count > 0)
{
    Console.WriteLine("測試失敗：");
    foreach (var failure in failed)
    {
        Console.WriteLine($"- {failure}");
    }

    Environment.ExitCode = 1;
    return;
}

Console.WriteLine($"全部通過，共 {tests.Count} 項。");

static class Tests
{
    public static Task SqlGuardAllowSelect()
    {
        var guard = new SqlGuard(new SecurityOptions { ReadOnly = true });
        var result = guard.Analyze("SELECT TOP 10 * FROM dbo.TableA");
        Assert.True(result.IsAllowed, "SELECT 應允許執行。");
        Assert.False(result.IsWrite, "SELECT 不應被視為寫入。");
        return Task.CompletedTask;
    }

    public static Task SqlGuardBlockUpdateWithoutWhere()
    {
        var guard = new SqlGuard(new SecurityOptions { ReadOnly = false });
        var result = guard.Analyze("UPDATE dbo.TableA SET Name='X';");
        Assert.False(result.IsAllowed, "無 WHERE 的 UPDATE 應被阻擋。");
        Assert.True(result.Findings.Any(f => f.Code == "UPDATE_WITHOUT_WHERE"), "應包含 UPDATE_WITHOUT_WHERE。");
        return Task.CompletedTask;
    }

    public static Task SqlGuardBlockDeleteWithoutWhere()
    {
        var guard = new SqlGuard(new SecurityOptions { ReadOnly = false });
        var result = guard.Analyze("DELETE FROM dbo.TableA;");
        Assert.False(result.IsAllowed, "無 WHERE 的 DELETE 應被阻擋。");
        Assert.True(result.Findings.Any(f => f.Code == "DELETE_WITHOUT_WHERE"), "應包含 DELETE_WITHOUT_WHERE。");
        return Task.CompletedTask;
    }

    public static Task SqlGuardReadOnlyBlocksWrite()
    {
        var guard = new SqlGuard(new SecurityOptions { ReadOnly = true });
        var result = guard.Analyze("INSERT INTO dbo.TableA(Id) VALUES(1);");
        Assert.False(result.IsAllowed, "唯讀模式不應允許 INSERT。");
        Assert.True(result.Findings.Any(f => f.Code == "READ_ONLY_MODE"), "應包含 READ_ONLY_MODE。");
        return Task.CompletedTask;
    }

    public static Task LlmClientTransientStatus()
    {
        Assert.True(LlmClient.IsTransientStatusCode(HttpStatusCode.TooManyRequests), "429 應為 transient。");
        Assert.True(LlmClient.IsTransientStatusCode(HttpStatusCode.ServiceUnavailable), "503 應為 transient。");
        Assert.False(LlmClient.IsTransientStatusCode(HttpStatusCode.BadRequest), "400 不應為 transient。");
        return Task.CompletedTask;
    }

    public static Task LlmClientExtractJson()
    {
        var json = LlmClient.ExtractJsonObject("prefix {\"summary\":\"ok\"} suffix");
        Assert.Equal("{\"summary\":\"ok\"}", json);
        return Task.CompletedTask;
    }

    public static async Task SqlExecutorThrowOnCancelledToken()
    {
        var executor = new SqlExecutor(
            new DatabaseOptions { ProviderName = "System.Data.SqlClient", ConnectionString = "Server=localhost;Database=master;Integrated Security=true;Encrypt=false;" },
            new SecurityOptions { ReadOnly = true });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cancelled = false;
        try
        {
            await executor.ExecuteReadAsync("SELECT 1", cts.Token);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        Assert.True(cancelled, "已取消 token 應拋出 OperationCanceledException。");
    }
}

static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    public static void False(bool condition, string message)
    {
        if (condition)
        {
            throw new Exception(message);
        }
    }

    public static void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new Exception($"預期 '{expected}'，實際 '{actual}'。");
        }
    }
}
