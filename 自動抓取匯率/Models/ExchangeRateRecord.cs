using System;

namespace BotExchangeRateWinForms.Models
{
    /// <summary>
    /// 表示單一幣別的一筆匯率資料。
    /// </summary>
    public sealed class ExchangeRateRecord
    {
        /// <summary>
        /// 三碼幣別代號，例如 USD、JPY。
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// 幣別中文名稱，例如 美金、日圓。
        /// </summary>
        public string CurrencyName { get; set; }

        /// <summary>
        /// 現金匯率本行買入。
        /// </summary>
        public decimal? CashBuy { get; set; }

        /// <summary>
        /// 現金匯率本行賣出。
        /// </summary>
        public decimal? CashSell { get; set; }

        /// <summary>
        /// 即期匯率本行買入。
        /// </summary>
        public decimal? SpotBuy { get; set; }

        /// <summary>
        /// 即期匯率本行賣出。
        /// </summary>
        public decimal? SpotSell { get; set; }

        /// <summary>
        /// 本筆資料所屬的掛牌日期。
        /// </summary>
        public DateTime SourceRateDate { get; set; }

        /// <summary>
        /// 來源頁面顯示的更新時間。
        /// </summary>
        public DateTime SourceUpdatedAt { get; set; }

        /// <summary>
        /// 原始資料來源網址。
        /// </summary>
        public string SourceUrl { get; set; }
    }
}
