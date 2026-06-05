using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        LocalAuthOptions localAuthOptions,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureCompatibilityAsync(dbContext, cancellationToken);

        if (localAuthOptions.BootstrapUsers.Count > 0)
        {
            foreach (var bootstrapUser in localAuthOptions.BootstrapUsers)
            {
                if (string.IsNullOrWhiteSpace(bootstrapUser.Account) ||
                    string.IsNullOrWhiteSpace(bootstrapUser.UserName) ||
                    string.IsNullOrWhiteSpace(bootstrapUser.RoleName) ||
                    string.IsNullOrWhiteSpace(bootstrapUser.Password))
                {
                    continue;
                }

                var existingUser = await dbContext.Users.FirstOrDefaultAsync(
                    item => item.Account == bootstrapUser.Account,
                    cancellationToken);

                if (existingUser is null)
                {
                    var user = new User
                    {
                        Account = bootstrapUser.Account.Trim(),
                        UserName = bootstrapUser.UserName.Trim(),
                        Email = string.IsNullOrWhiteSpace(bootstrapUser.Email) ? null : bootstrapUser.Email.Trim(),
                        RoleName = bootstrapUser.RoleName.Trim(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        PasswordChangedAt = DateTime.UtcNow,
                    };
                    user.PasswordHash = passwordHasher.HashPassword(user, bootstrapUser.Password);
                    dbContext.Users.Add(user);
                }
                else if (string.IsNullOrWhiteSpace(existingUser.PasswordHash))
                {
                    existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, bootstrapUser.Password);
                    existingUser.PasswordChangedAt = DateTime.UtcNow;
                    existingUser.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        if (!await dbContext.ScanAllowedRanges.AnyAsync(cancellationToken))
        {
            dbContext.ScanAllowedRanges.Add(new ScanAllowedRange
            {
                RangeName = "內網預設",
                Cidr = "10.0.0.0/8",
                Description = "預設允許 10.x.x.x 內網掃描範圍",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCompatibilityAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(dbContext, "Vulnerabilities", "DetectedVersion", cancellationToken))
        {
            return;
        }

        var providerName = dbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Vulnerabilities ADD COLUMN DetectedVersion TEXT NULL;", cancellationToken);
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Vulnerabilities ADD DetectedVersion nvarchar(200) NULL;", cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(ApplicationDbContext dbContext, string tableName, string columnName, CancellationToken cancellationToken)
    {
        var providerName = dbContext.Database.ProviderName ?? string.Empty;
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText = $"PRAGMA table_info([{tableName}]);";
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            command.CommandText = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName;";
            var tableNameParameter = command.CreateParameter();
            tableNameParameter.ParameterName = "@tableName";
            tableNameParameter.Value = tableName;
            command.Parameters.Add(tableNameParameter);

            var columnNameParameter = command.CreateParameter();
            columnNameParameter.ParameterName = "@columnName";
            columnNameParameter.Value = columnName;
            command.Parameters.Add(columnNameParameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
