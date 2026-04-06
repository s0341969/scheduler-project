namespace BotExchangeRateWinForms.Models
{
    /// <summary>
    /// 保存使用者在畫面上可調整的所有設定值。
    /// </summary>
    public sealed class UserSettings
    {
        /// <summary>
        /// 匯率來源頁面的網址。
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Timer 週期間隔的數值部分。
        /// </summary>
        public int PollIntervalValue { get; set; }

        /// <summary>
        /// Timer 週期間隔的單位，支援分鐘或小時。
        /// </summary>
        public string PollIntervalUnit { get; set; }

        /// <summary>
        /// HTTP 抓取逾時秒數。
        /// </summary>
        public int RequestTimeoutSeconds { get; set; }

        /// <summary>
        /// MSSQL 連線字串。
        /// </summary>
        public string SqlConnectionString { get; set; }

        /// <summary>
        /// 舊版單一 DB 寫入開關，保留相容性用。
        /// </summary>
        public bool WriteToDatabase { get; set; }

        /// <summary>
        /// 是否允許寫入 CHRNAME 主檔。
        /// </summary>
        public bool WriteChrname { get; set; }

        /// <summary>
        /// 是否允許寫入 CHRNAME-HISTORY 歷史檔。
        /// </summary>
        public bool WriteChrnameHistory { get; set; }

        /// <summary>
        /// 建立第一次執行時使用的預設設定。
        /// </summary>
        public static UserSettings CreateDefault()
        {
            return new UserSettings
            {
                SourceUrl = "https://rate.bot.com.tw/xrt?Lang=zh-TW",
                PollIntervalValue = 30,
                PollIntervalUnit = "\u5206\u9418",
                RequestTimeoutSeconds = 30,
                SqlConnectionString = string.Empty,
                WriteToDatabase = false,
                WriteChrname = false,
                WriteChrnameHistory = false
            };
        }
    }
}
