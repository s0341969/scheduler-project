using System.Data;
using System.Data.Common;
using SqlMaintenanceAgent.App.Configuration;
using SqlMaintenanceAgent.App.Models;

namespace SqlMaintenanceAgent.App.Services;

public sealed class SqlExecutor
{
    private readonly DatabaseOptions _databaseOptions;
    private readonly SecurityOptions _securityOptions;

    public SqlExecutor(DatabaseOptions databaseOptions, SecurityOptions securityOptions)
    {
        _databaseOptions = databaseOptions;
        _securityOptions = securityOptions;
    }

    public async Task<QueryExecutionResult> ExecuteReadAsync(string sql, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(_databaseOptions.ConnectionString))
        {
            return new QueryExecutionResult
            {
                Success = false,
                ErrorMessage = "資料庫連線字串未設定。"
            };
        }

        try
        {
            var factory = DbProviderFactories.GetFactory(_databaseOptions.ProviderName);
            using var connection = factory.CreateConnection();
            if (connection is null)
            {
                return new QueryExecutionResult { Success = false, ErrorMessage = "無法建立資料庫連線物件。" };
            }

            connection.ConnectionString = _databaseOptions.ConnectionString;
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = _databaseOptions.CommandTimeoutSeconds;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new QueryExecutionResult { Success = true };

            for (var i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(reader.GetName(i));
            }

            var rowCounter = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                rowCounter++;
                if (rowCounter > _databaseOptions.MaxRows)
                {
                    return result with { Truncated = true };
                }

                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var column in result.Columns)
                {
                    var value = reader[column];
                    row[column] = value is DBNull ? "NULL" : Convert.ToString(value) ?? string.Empty;
                }

                result.Rows.Add(row);
            }

            return result with { AffectedRows = result.Rows.Count };
        }
        catch (Exception ex)
        {
            return new QueryExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<QueryExecutionResult> ExecuteWriteWithTransactionAsync(string sql, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_securityOptions.ReadOnly)
        {
            return new QueryExecutionResult
            {
                Success = false,
                ErrorMessage = "目前為唯讀模式，禁止寫入。"
            };
        }

        if (string.IsNullOrWhiteSpace(_databaseOptions.ConnectionString))
        {
            return new QueryExecutionResult
            {
                Success = false,
                ErrorMessage = "資料庫連線字串未設定。"
            };
        }

        try
        {
            var factory = DbProviderFactories.GetFactory(_databaseOptions.ProviderName);
            using var connection = factory.CreateConnection();
            if (connection is null)
            {
                return new QueryExecutionResult { Success = false, ErrorMessage = "無法建立資料庫連線物件。" };
            }

            connection.ConnectionString = _databaseOptions.ConnectionString;
            await connection.OpenAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = _databaseOptions.CommandTimeoutSeconds;

            try
            {
                var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                transaction.Commit();
                return new QueryExecutionResult
                {
                    Success = true,
                    AffectedRows = affectedRows
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new QueryExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"寫入失敗並已回滾：{ex.Message}"
                };
            }
        }
        catch (Exception ex)
        {
            return new QueryExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
