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
                string.Format("\u5c1a\u672a\u8a2d\u5b9a MSSQL \u9023\u7dda\u5b57\u4e32\u3002\u8acb\u5728\u756b\u9762\u8f38\u5165\uff0c\u6216\u8a2d\u5b9a\u74b0\u5883\u8b8a\u6578 {0}\u3002", EnvironmentVariableName));
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

        public async Task<Tuple<int, int>> SaveAsync(string connectionString, ScrapeResult result, bool writeChrname, bool writeChrnameHistory)
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
                    var savedRows = 0;
                    var skippedRows = 0;

                    foreach (var record in result.Records)
                    {
                        CurrencyMap currencyMap;
                        if (!TryMapCurrency(record, out currencyMap))
                        {
                            skippedRows++;
                            continue;
                        }

                        decimal? ftil;
                        decimal? ftol;
                        ResolveStoredRates(record, out ftil, out ftol);

                        if (writeChrname)
                        {
                            await UpsertCurrentAsync(connection, transaction, currencyMap, ftil, ftol, record.SourceUpdatedAt).ConfigureAwait(false);
                        }

                        if (writeChrnameHistory)
                        {
                            await InsertHistoryAsync(connection, transaction, currencyMap, ftil, ftol, record.SourceUpdatedAt).ConfigureAwait(false);
                        }

                        savedRows++;
                    }

                    transaction.Commit();
                    return Tuple.Create(savedRows, skippedRows);
                }
            }
        }

        private static async Task UpsertCurrentAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            CurrencyMap currencyMap,
            decimal? ftil,
            decimal? ftol,
            DateTime crdate)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 30;
                command.CommandText =
                    "IF EXISTS (SELECT 1 FROM dbo.CHRNAME WITH (UPDLOCK, HOLDLOCK) WHERE CHRNAM = @CHRNAM) " +
                    "BEGIN " +
                    "UPDATE dbo.CHRNAME " +
                    "SET CHRDS = @CHRDS, FTIL = @FTIL, FTOL = @FTOL, CRDATE = @CRDATE " +
                    "WHERE CHRNAM = @CHRNAM; " +
                    "END " +
                    "ELSE " +
                    "BEGIN " +
                    "INSERT INTO dbo.CHRNAME (CHRNAM, CHRDS, FTIL, FTOL, CRDATE) " +
                    "VALUES (@CHRNAM, @CHRDS, @FTIL, @FTOL, @CRDATE); " +
                    "END";

                command.Parameters.AddWithValue("@CHRNAM", currencyMap.Code);
                command.Parameters.AddWithValue("@CHRDS", currencyMap.Name);
                command.Parameters.AddWithValue("@FTIL", (object)ftil ?? DBNull.Value);
                command.Parameters.AddWithValue("@FTOL", (object)ftol ?? DBNull.Value);
                command.Parameters.AddWithValue("@CRDATE", crdate);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private static async Task InsertHistoryAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            CurrencyMap currencyMap,
            decimal? ftil,
            decimal? ftol,
            DateTime crdate)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 30;
                command.CommandText =
                    "IF NOT EXISTS (" +
                    "SELECT 1 FROM dbo.[CHRNAME-HISTORY] WITH (UPDLOCK, HOLDLOCK) " +
                    "WHERE CHRNAM = @CHRNAM AND CRDATE = @CRDATE) " +
                    "BEGIN " +
                    "INSERT INTO dbo.[CHRNAME-HISTORY] (CHRNAM, FTIL, FTOL, CRDATE) " +
                    "VALUES (@CHRNAM, @FTIL, @FTOL, @CRDATE); " +
                    "END";

                command.Parameters.AddWithValue("@CHRNAM", currencyMap.Code);
                command.Parameters.AddWithValue("@FTIL", (object)ftil ?? DBNull.Value);
                command.Parameters.AddWithValue("@FTOL", (object)ftol ?? DBNull.Value);
                command.Parameters.AddWithValue("@CRDATE", crdate);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private static void ResolveStoredRates(ExchangeRateRecord record, out decimal? ftil, out decimal? ftol)
        {
            var isCny = Contains(record.CurrencyCode, "CNY") || Contains(record.CurrencyName, "\u4eba\u6c11\u5e63");
            if (isCny)
            {
                ftil = record.CashBuy;
                ftol = record.CashSell;
                return;
            }

            ftil = record.SpotBuy ?? 0m;
            ftol = record.SpotSell ?? 0m;
        }

        private static bool TryMapCurrency(ExchangeRateRecord record, out CurrencyMap currencyMap)
        {
            var sourceText = string.Format(
                "{0} {1}",
                record.CurrencyCode ?? string.Empty,
                record.CurrencyName ?? string.Empty);

            if (Contains(sourceText, "USD"))
            {
                currencyMap = new CurrencyMap("US", "\u7f8e\u91d1");
                return true;
            }

            if (Contains(sourceText, "JPY"))
            {
                currencyMap = new CurrencyMap("JP", "\u65e5\u5e63");
                return true;
            }

            if (Contains(sourceText, "THB"))
            {
                currencyMap = new CurrencyMap("TA", "\u6cf0\u5e63");
                return true;
            }

            if (Contains(sourceText, "CNY"))
            {
                currencyMap = new CurrencyMap("RMB", "\u4eba\u6c11\u5e63");
                return true;
            }

            if (Contains(sourceText, "CHF"))
            {
                currencyMap = new CurrencyMap("CHF", "\u6cd5\u90ce");
                return true;
            }

            if (Contains(sourceText, "EUR"))
            {
                currencyMap = new CurrencyMap("EUR", "\u6b50\u5143");
                return true;
            }

            if (Contains(sourceText, "GBP"))
            {
                currencyMap = new CurrencyMap("GBP", "\u82f1\u78c5");
                return true;
            }

            if (Contains(sourceText, "MA") || Contains(sourceText, "MYR"))
            {
                currencyMap = new CurrencyMap("MA", "\u99ac\u5e63");
                return true;
            }

            if (Contains(sourceText, "NT$") || Contains(sourceText, "TWD"))
            {
                currencyMap = new CurrencyMap("NT$", "\u53f0\u5e63");
                return true;
            }

            if (Contains(sourceText, "SGD"))
            {
                currencyMap = new CurrencyMap("SGD", "\u65b0\u5e63");
                return true;
            }

            if (Contains(sourceText, "AUD"))
            {
                currencyMap = new CurrencyMap("AUD", "\u6fb3\u5e63");
                return true;
            }

            currencyMap = null;
            return false;
        }

        private static string GetCreateTableSql()
        {
            return
                "IF OBJECT_ID(N'dbo.CHRNAME', N'U') IS NULL " +
                "BEGIN " +
                "CREATE TABLE dbo.CHRNAME (" +
                "CHRNAM NVARCHAR(10) NOT NULL CONSTRAINT PK_CHRNAME PRIMARY KEY, " +
                "CHRDS NVARCHAR(20) NOT NULL, " +
                "FTIL DECIMAL(18, 6) NULL, " +
                "FTOL DECIMAL(18, 6) NULL, " +
                "CRDATE DATETIME2(3) NULL" +
                "); " +
                "END; " +
                "IF OBJECT_ID(N'dbo.[CHRNAME-HISTORY]', N'U') IS NULL " +
                "BEGIN " +
                "CREATE TABLE dbo.[CHRNAME-HISTORY] (" +
                "CHRNAM NVARCHAR(10) NOT NULL, " +
                "FTIL DECIMAL(18, 6) NULL, " +
                "FTOL DECIMAL(18, 6) NULL, " +
                "CRDATE DATETIME2(3) NOT NULL" +
                "); " +
                "END;";
        }

        private static bool Contains(string input, string token)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            return input.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class CurrencyMap
        {
            public CurrencyMap(string code, string name)
            {
                Code = code;
                Name = name;
            }

            public string Code { get; private set; }

            public string Name { get; private set; }
        }
    }
}
