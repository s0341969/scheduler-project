using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using BotExchangeRateWinForms.Models;

namespace BotExchangeRateWinForms.Services
{
    /// <summary>
    /// 負責所有 MSSQL 讀寫、建表與幣別對應邏輯。
    /// </summary>
    public sealed class ExchangeRateSqlRepository
    {
        private const string EnvironmentVariableName = "BOT_RATE_DB_CONN";

        /// <summary>
        /// 取得實際可用的 MSSQL 連線字串，優先使用畫面設定，否則讀環境變數。
        /// </summary>
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

        /// <summary>
        /// 初始化專案需要的資料表結構。
        /// </summary>
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

        /// <summary>
        /// 依目前設定將抓取結果寫入主檔與歷史檔，並回傳寫入與略過筆數。
        /// </summary>
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
                    var chrnameSchema = writeChrname
                        ? await LoadDescriptionSchemaAsync(connection, transaction, "dbo.CHRNAME").ConfigureAwait(false)
                        : TableSchema.Empty;
                    var chrnameHistorySchema = writeChrnameHistory
                        ? await LoadDescriptionSchemaAsync(connection, transaction, "dbo.[CHRNAME-HISTORY]").ConfigureAwait(false)
                        : TableSchema.Empty;
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

                        var currentSnapshot = await LoadCurrentSnapshotAsync(connection, transaction, currencyMap.Code).ConfigureAwait(false);
                        var latestHistorySnapshot = await LoadLatestHistorySnapshotAsync(connection, transaction, currencyMap.Code).ConfigureAwait(false);
                        var shouldWriteChrname = writeChrname && NeedsWrite(currentSnapshot, ftil, ftol);
                        var shouldWriteChrnameHistory = writeChrnameHistory && NeedsWrite(latestHistorySnapshot, ftil, ftol);

                        if (shouldWriteChrname)
                        {
                            await UpsertCurrentAsync(
                                connection,
                                transaction,
                                chrnameSchema,
                                currencyMap,
                                ftil,
                                ftol,
                                record.SourceUpdatedAt).ConfigureAwait(false);
                        }

                        if (shouldWriteChrnameHistory)
                        {
                            await InsertHistoryAsync(
                                connection,
                                transaction,
                                chrnameHistorySchema,
                                currencyMap,
                                ftil,
                                ftol,
                                record.SourceUpdatedAt).ConfigureAwait(false);
                        }

                        if (shouldWriteChrname || shouldWriteChrnameHistory)
                        {
                            savedRows++;
                        }
                        else
                        {
                            skippedRows++;
                        }
                    }

                    transaction.Commit();
                    return Tuple.Create(savedRows, skippedRows);
                }
            }
        }

        /// <summary>
        /// 將最新匯率寫入 CHRNAME，若幣別已存在則更新，不存在則新增。
        /// </summary>
        private static async Task UpsertCurrentAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            TableSchema tableSchema,
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
                command.CommandText = BuildChrnameUpsertSql(tableSchema);

                command.Parameters.AddWithValue("@CHRNAM", currencyMap.Code);
                command.Parameters.AddWithValue("@FTIL", (object)ftil ?? DBNull.Value);
                command.Parameters.AddWithValue("@FTOL", (object)ftol ?? DBNull.Value);
                command.Parameters.AddWithValue("@CRDATE", crdate);

                if (tableSchema.HasDescriptionColumn)
                {
                    command.Parameters.AddWithValue("@DESCRIPTION", currencyMap.Name);
                }

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 讀取指定資料表實際使用的幣別說明欄位名稱。
        /// </summary>
        private static async Task<TableSchema> LoadDescriptionSchemaAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 30;
                command.CommandText =
                    "SELECT TOP (1) name " +
                    "FROM sys.columns " +
                    "WHERE object_id = OBJECT_ID(@TABLE_NAME) " +
                    "AND name IN (N'CHRDS', N'CHRDSC') " +
                    "ORDER BY CASE WHEN name = N'CHRDS' THEN 0 ELSE 1 END;";
                command.Parameters.AddWithValue("@TABLE_NAME", tableName);

                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                var descriptionColumnName = result as string;
                if (string.IsNullOrWhiteSpace(descriptionColumnName))
                {
                    return TableSchema.Empty;
                }

                return new TableSchema(descriptionColumnName);
            }
        }

        /// <summary>
        /// 讀取 CHRNAME 目前已存的主檔匯率，用來判斷是否真的有變動。
        /// </summary>
        private static async Task<RateSnapshot> LoadCurrentSnapshotAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            string chrnam)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 30;
                command.CommandText =
                    "SELECT TOP (1) FTIL, FTOL " +
                    "FROM dbo.CHRNAME WITH (UPDLOCK, HOLDLOCK) " +
                    "WHERE CHRNAM = @CHRNAM;";
                command.Parameters.AddWithValue("@CHRNAM", chrnam);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return RateSnapshot.Empty;
                    }

                    return new RateSnapshot(
                        ReadNullableDecimal(reader, 0),
                        ReadNullableDecimal(reader, 1),
                        true);
                }
            }
        }

        /// <summary>
        /// 讀取 CHRNAME-HISTORY 最新一筆歷史匯率，用來判斷是否需要新增歷史。
        /// </summary>
        private static async Task<RateSnapshot> LoadLatestHistorySnapshotAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            string chrnam)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 30;
                command.CommandText =
                    "SELECT TOP (1) FTIL, FTOL " +
                    "FROM dbo.[CHRNAME-HISTORY] WITH (UPDLOCK, HOLDLOCK) " +
                    "WHERE CHRNAM = @CHRNAM " +
                    "ORDER BY CRDATE DESC;";
                command.Parameters.AddWithValue("@CHRNAM", chrnam);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return RateSnapshot.Empty;
                    }

                    return new RateSnapshot(
                        ReadNullableDecimal(reader, 0),
                        ReadNullableDecimal(reader, 1),
                        true);
                }
            }
        }

        /// <summary>
        /// 判斷目前新匯率和既有匯率是否不同，不同才需要寫入。
        /// </summary>
        private static bool NeedsWrite(RateSnapshot snapshot, decimal? newFtil, decimal? newFtol)
        {
            if (snapshot == null || !snapshot.Exists)
            {
                return true;
            }

            return !AreRatesEqual(snapshot.Ftil, newFtil) || !AreRatesEqual(snapshot.Ftol, newFtol);
        }

        /// <summary>
        /// 比較兩個可為 null 的匯率值是否相同。
        /// </summary>
        private static bool AreRatesEqual(decimal? left, decimal? right)
        {
            if (!left.HasValue && !right.HasValue)
            {
                return true;
            }

            if (left.HasValue != right.HasValue)
            {
                return false;
            }

            return left.Value == right.Value;
        }

        /// <summary>
        /// 安全讀取 DataReader 中可為 null 的 decimal 欄位。
        /// </summary>
        private static decimal? ReadNullableDecimal(SqlDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetDecimal(ordinal);
        }

        /// <summary>
        /// 依資料表實際欄位組出 CHRNAME 的 UPSERT SQL。
        /// </summary>
        private static string BuildChrnameUpsertSql(TableSchema tableSchema)
        {
            if (tableSchema != null && tableSchema.HasDescriptionColumn)
            {
                return
                    "IF EXISTS (SELECT 1 FROM dbo.CHRNAME WITH (UPDLOCK, HOLDLOCK) WHERE CHRNAM = @CHRNAM) " +
                    "BEGIN " +
                    "UPDATE dbo.CHRNAME " +
                    "SET " + tableSchema.DescriptionColumnName + " = @DESCRIPTION, FTIL = @FTIL, FTOL = @FTOL, CRDATE = @CRDATE " +
                    "WHERE CHRNAM = @CHRNAM; " +
                    "END " +
                    "ELSE " +
                    "BEGIN " +
                    "INSERT INTO dbo.CHRNAME (CHRNAM, " + tableSchema.DescriptionColumnName + ", FTIL, FTOL, CRDATE) " +
                    "VALUES (@CHRNAM, @DESCRIPTION, @FTIL, @FTOL, @CRDATE); " +
                    "END";
            }

            return
                "IF EXISTS (SELECT 1 FROM dbo.CHRNAME WITH (UPDLOCK, HOLDLOCK) WHERE CHRNAM = @CHRNAM) " +
                "BEGIN " +
                "UPDATE dbo.CHRNAME " +
                "SET FTIL = @FTIL, FTOL = @FTOL, CRDATE = @CRDATE " +
                "WHERE CHRNAM = @CHRNAM; " +
                "END " +
                "ELSE " +
                "BEGIN " +
                "INSERT INTO dbo.CHRNAME (CHRNAM, FTIL, FTOL, CRDATE) " +
                "VALUES (@CHRNAM, @FTIL, @FTOL, @CRDATE); " +
                "END";
        }

        /// <summary>
        /// 將匯率新增到 CHRNAME-HISTORY 歷史檔。
        /// </summary>
        private static async Task InsertHistoryAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            TableSchema tableSchema,
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
                command.CommandText = BuildChrnameHistoryInsertSql(tableSchema);

                command.Parameters.AddWithValue("@CHRNAM", currencyMap.Code);
                command.Parameters.AddWithValue("@FTIL", (object)ftil ?? DBNull.Value);
                command.Parameters.AddWithValue("@FTOL", (object)ftol ?? DBNull.Value);
                command.Parameters.AddWithValue("@CRDATE", crdate);

                if (tableSchema.HasDescriptionColumn)
                {
                    command.Parameters.AddWithValue("@DESCRIPTION", currencyMap.Name);
                }

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 依資料表實際欄位組出 CHRNAME-HISTORY 的 INSERT SQL。
        /// </summary>
        private static string BuildChrnameHistoryInsertSql(TableSchema tableSchema)
        {
            if (tableSchema != null && tableSchema.HasDescriptionColumn)
            {
                return
                    "IF NOT EXISTS (" +
                    "SELECT 1 FROM dbo.[CHRNAME-HISTORY] WITH (UPDLOCK, HOLDLOCK) " +
                    "WHERE CHRNAM = @CHRNAM AND CRDATE = @CRDATE) " +
                    "BEGIN " +
                    "INSERT INTO dbo.[CHRNAME-HISTORY] (CHRNAM, " + tableSchema.DescriptionColumnName + ", FTIL, FTOL, CRDATE) " +
                    "VALUES (@CHRNAM, @DESCRIPTION, @FTIL, @FTOL, @CRDATE); " +
                    "END";
            }

            return
                "IF NOT EXISTS (" +
                "SELECT 1 FROM dbo.[CHRNAME-HISTORY] WITH (UPDLOCK, HOLDLOCK) " +
                "WHERE CHRNAM = @CHRNAM AND CRDATE = @CRDATE) " +
                "BEGIN " +
                "INSERT INTO dbo.[CHRNAME-HISTORY] (CHRNAM, FTIL, FTOL, CRDATE) " +
                "VALUES (@CHRNAM, @FTIL, @FTOL, @CRDATE); " +
                "END";
        }

        /// <summary>
        /// 按既有規則決定寫入 FTIL / FTOL 要使用現金或即期匯率。
        /// </summary>
        private static void ResolveStoredRates(ExchangeRateRecord record, out decimal? ftil, out decimal? ftol)
        {
            var isCny = Contains(record.CurrencyCode, "CNY") || Contains(record.CurrencyName, "人民幣");
            if (isCny)
            {
                ftil = record.CashBuy;
                ftol = record.CashSell;
                return;
            }

            ftil = record.SpotBuy ?? 0m;
            ftol = record.SpotSell ?? 0m;
        }

        /// <summary>
        /// 將來源幣別映射成資料庫使用的 CHRNAM 與中文名稱。
        /// </summary>
        private static bool TryMapCurrency(ExchangeRateRecord record, out CurrencyMap currencyMap)
        {
            var sourceText = string.Format(
                "{0} {1}",
                record.CurrencyCode ?? string.Empty,
                record.CurrencyName ?? string.Empty);

            if (Contains(sourceText, "USD"))
            {
                currencyMap = new CurrencyMap("US", "美金");
                return true;
            }

            if (Contains(sourceText, "JPY"))
            {
                currencyMap = new CurrencyMap("JP", "日幣");
                return true;
            }

            if (Contains(sourceText, "THB"))
            {
                currencyMap = new CurrencyMap("TA", "泰幣");
                return true;
            }

            if (Contains(sourceText, "CNY"))
            {
                currencyMap = new CurrencyMap("RMB", "人民幣");
                return true;
            }

            if (Contains(sourceText, "CHF"))
            {
                currencyMap = new CurrencyMap("CHF", "法郎");
                return true;
            }

            if (Contains(sourceText, "EUR"))
            {
                currencyMap = new CurrencyMap("EUR", "歐元");
                return true;
            }

            if (Contains(sourceText, "GBP"))
            {
                currencyMap = new CurrencyMap("GBP", "英磅");
                return true;
            }

            if (Contains(sourceText, "MA") || Contains(sourceText, "MYR"))
            {
                currencyMap = new CurrencyMap("MA", "馬幣");
                return true;
            }

            if (Contains(sourceText, "NT$") || Contains(sourceText, "TWD"))
            {
                currencyMap = new CurrencyMap("NT$", "台幣");
                return true;
            }

            if (Contains(sourceText, "SGD"))
            {
                currencyMap = new CurrencyMap("SGD", "新幣");
                return true;
            }

            if (Contains(sourceText, "AUD"))
            {
                currencyMap = new CurrencyMap("AUD", "澳幣");
                return true;
            }

            currencyMap = null;
            return false;
        }

        /// <summary>
        /// 產生初始化資料庫所需的建表 SQL。
        /// </summary>
        private static string GetCreateTableSql()
        {
            return
                "IF OBJECT_ID(N'dbo.CHRNAME', N'U') IS NULL " +
                "BEGIN " +
                "CREATE TABLE dbo.CHRNAME (" +
                "CHRNAM VARCHAR(4) NOT NULL CONSTRAINT PK_CHRNAME PRIMARY KEY, " +
                "CHRDSC VARCHAR(10) NULL, " +
                "FTIL DECIMAL(18, 4) NULL, " +
                "FTOL DECIMAL(18, 4) NULL, " +
                "CRDATE DATETIME NULL, " +
                "[固定匯率] NUMERIC(18, 4) NULL, " +
                "[鼎新] VARCHAR(10) NULL" +
                "); " +
                "END; " +
                "IF OBJECT_ID(N'dbo.[CHRNAME-HISTORY]', N'U') IS NULL " +
                "BEGIN " +
                "CREATE TABLE dbo.[CHRNAME-HISTORY] (" +
                "CNO INT IDENTITY(1,1) NOT NULL, " +
                "CHRNAM VARCHAR(4) NOT NULL, " +
                "CHRDSC VARCHAR(10) NULL, " +
                "FTIL DECIMAL(18, 4) NULL, " +
                "FTOL DECIMAL(18, 4) NULL, " +
                "CRDATE DATETIME NULL" +
                "); " +
                "END;";
        }

        /// <summary>
        /// 忽略大小寫判斷字串是否包含指定關鍵字。
        /// </summary>
        private static bool Contains(string input, string token)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            return input.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 保存資料庫要用的幣別代碼與中文名稱。
        /// </summary>
        private sealed class CurrencyMap
        {
            /// <summary>
            /// 建立一組資料庫幣別代碼與顯示名稱。
            /// </summary>
            public CurrencyMap(string code, string name)
            {
                Code = code;
                Name = name;
            }

            /// <summary>
            /// 資料庫使用的 CHRNAM。
            /// </summary>
            public string Code { get; private set; }

            /// <summary>
            /// 寫入說明欄位使用的中文名稱。
            /// </summary>
            public string Name { get; private set; }
        }

        /// <summary>
        /// 保存資料表實際存在的說明欄位資訊。
        /// </summary>
        private sealed class TableSchema
        {
            public static readonly TableSchema Empty = new TableSchema(null);

            /// <summary>
            /// 建立資料表欄位描述資訊。
            /// </summary>
            public TableSchema(string descriptionColumnName)
            {
                DescriptionColumnName = descriptionColumnName;
            }

            /// <summary>
            /// 實際存在的幣別說明欄位名稱。
            /// </summary>
            public string DescriptionColumnName { get; private set; }

            /// <summary>
            /// 是否存在可寫入的幣別說明欄位。
            /// </summary>
            public bool HasDescriptionColumn
            {
                get { return !string.IsNullOrWhiteSpace(DescriptionColumnName); }
            }
        }

        /// <summary>
        /// 保存資料表中目前或最新一筆的 FTIL / FTOL 快照。
        /// </summary>
        private sealed class RateSnapshot
        {
            public static readonly RateSnapshot Empty = new RateSnapshot(null, null, false);

            /// <summary>
            /// 建立一組匯率快照。
            /// </summary>
            public RateSnapshot(decimal? ftil, decimal? ftol, bool exists)
            {
                Ftil = ftil;
                Ftol = ftol;
                Exists = exists;
            }

            /// <summary>
            /// 主檔或歷史檔中的 FTIL。
            /// </summary>
            public decimal? Ftil { get; private set; }

            /// <summary>
            /// 主檔或歷史檔中的 FTOL。
            /// </summary>
            public decimal? Ftol { get; private set; }

            /// <summary>
            /// 代表這個快照是否真的來自資料表既有資料。
            /// </summary>
            public bool Exists { get; private set; }
        }
    }
}
