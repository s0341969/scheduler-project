namespace BotExchangeRateWinForms.Models
{
    public sealed class UserSettings
    {
        public string SourceUrl { get; set; }

        public int PollIntervalValue { get; set; }

        public string PollIntervalUnit { get; set; }

        public int RequestTimeoutSeconds { get; set; }

        public string SqlConnectionString { get; set; }

        public bool WriteToDatabase { get; set; }

        public static UserSettings CreateDefault()
        {
            return new UserSettings
            {
                SourceUrl = "https://rate.bot.com.tw/xrt?Lang=zh-TW",
                PollIntervalValue = 30,
                PollIntervalUnit = "\u5206\u9418",
                RequestTimeoutSeconds = 30,
                SqlConnectionString = string.Empty,
                WriteToDatabase = false
            };
        }
    }
}
