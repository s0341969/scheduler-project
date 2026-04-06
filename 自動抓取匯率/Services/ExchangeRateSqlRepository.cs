using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using BotExchangeRateWinForms.Models;

namespace BotExchangeRateWinForms.Services
{
    public sealed class ExchangeRateSqlRepository
    {
        private const string EnvironmentVariableName = "BOT_RATE_DB_CONN";

        public string ResolveConnectionString(UserSettings settings)
        {
            if (settings != null && !string.IsNullOrWhiteSpace(settings.SqlConnectionString))
            {
                return settings.SqlConnectionString.Trim();
            }

            var environmentConnectionString = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                return environmentConnectionString.Trim();
            }

            throw new InvalidOperationException(
                string.Format("尚未設定 MSSQL 連線字串。請在畫面輸入，或設定環境變數 {0}。", EnvironmentVariableName));
        }

        public async Task InitializeDatabaseAsync(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("connectionString");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = GetCreateTableSql();
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<Tuple<int, int>> SaveAsync(string connectionString, ScrapeResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    var insertedRows = 0;
                    var duplicateRows = 0;

                    foreach (var record in result.Records)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = CommandType.Text;
                            command.CommandTimeout = 30;
                            command.CommandText =
                                "IF NOT EXISTS (" +
                                "SELECT 1 FROM dbo.BotExchangeRateSnapshot WITH (UPDLOCK, HOLDLOCK) " +
                                "WHERE SourceUpdatedAt = @SourceUpdatedAt AND CurrencyCode = @CurrencyCode) " +
                                "BEGIN " +
                                "INSERT INTO dbo.BotExchangeRateSnapshot " +
                                "(SourceRateDate, SourceUpdatedAt, CurrencyCode, CurrencyName, CashBuy, CashSell, SpotBuy, SpotSell, SourceUrl) " +
                                "VALUES " +
                                "(@SourceRateDate, @SourceUpdatedAt, @CurrencyCode, @CurrencyName, @CashBuy, @CashSell, @SpotBuy, @SpotSell, @SourceUrl); " +
                                "SELECT CAST(1 AS INT); " +
                                "END " +
                                "ELSE " +
                                "BEGIN " +
                                "SELECT CAST(0 AS INT); " +
                                "END";

                            command.Parameters.AddWithValue("@SourceRateDate", record.SourceRateDate.Date);
                            command.Parameters.AddWithValue("@SourceUpdatedAt", record.SourceUpdatedAt);
                            command.Parameters.AddWithValue("@CurrencyCode", record.CurrencyCode);
                            command.Parameters.AddWithValue("@CurrencyName", record.CurrencyName);
                            command.Parameters.AddWithValue("@CashBuy", (object)record.CashBuy ?? DBNull.Value);
                            command.Parameters.AddWithValue("@CashSell", (object)record.CashSell ?? DBNull.Value);
                            command.Parameters.AddWithValue("@SpotBuy", (object)record.SpotBuy ?? DBNull.Value);
                            command.Parameters.AddWithValue("@SpotSell", (object)record.SpotSell ?? DBNull.Value);
                            command.Parameters.AddWithValue("@SourceUrl", record.SourceUrl);

                            var inserted = (int)await command.ExecuteScalarAsync().ConfigureAwait(false);
                            if (inserted == 1)
                            {
                                insertedRows++;
                            }
                            else
                            {
                                duplicateRows++;
                            }
                        }
                    }

                    transaction.Commit();
                    return Tuple.Create(insertedRows, duplicateRows);
                }
            }
        }

        private static string GetCreateTableSql()
        {
            return
                "IF OBJECT_ID(N'dbo.BotExchangeRateSnapshot', N'U') IS NULL " +
                "BEGIN " +
                "CREATE TABLE dbo.BotExchangeRateSnapshot (" +
                "SnapshotId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BotExchangeRateSnapshot PRIMARY KEY, " +
                "SourceRateDate DATE NOT NULL, " +
                "SourceUpdatedAt DATETIME2(0) NOT NULL, " +
                "CurrencyCode NVARCHAR(10) NOT NULL, " +
                "CurrencyName NVARCHAR(50) NOT NULL, " +
                "CashBuy DECIMAL(18, 6) NULL, " +
                "CashSell DECIMAL(18, 6) NULL, " +
                "SpotBuy DECIMAL(18, 6) NULL, " +
                "SpotSell DECIMAL(18, 6) NULL, " +
                "SourceUrl NVARCHAR(500) NOT NULL, " +
                "InsertedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_BotExchangeRateSnapshot_InsertedAtUtc DEFAULT SYSUTCDATETIME()" +
                "); " +
                "END; " +
                "IF NOT EXISTS (" +
                "SELECT 1 FROM sys.indexes WHERE name = N'UX_BotExchangeRateSnapshot_SourceUpdatedAt_CurrencyCode' AND object_id = OBJECT_ID(N'dbo.BotExchangeRateSnapshot')) " +
                "BEGIN " +
                "CREATE UNIQUE INDEX UX_BotExchangeRateSnapshot_SourceUpdatedAt_CurrencyCode " +
                "ON dbo.BotExchangeRateSnapshot (SourceUpdatedAt, CurrencyCode); " +
                "END;";
        }
    }
}
