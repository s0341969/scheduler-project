using System;
using System.Collections.Generic;

namespace BotExchangeRateWinForms.Models
{
    /// <summary>
    /// 表示一次抓取作業的整體結果。
    /// </summary>
    public sealed class ScrapeResult
    {
        /// <summary>
        /// 初始化抓取結果與明細集合。
        /// </summary>
        public ScrapeResult()
        {
            Records = new List<ExchangeRateRecord>();
        }

        /// <summary>
        /// 牌告資料所屬的掛牌日期。
        /// </summary>
        public DateTime SourceRateDate { get; set; }

        /// <summary>
        /// 來源頁面顯示的最新掛牌時間。
        /// </summary>
        public DateTime SourceUpdatedAt { get; set; }

        /// <summary>
        /// 本次抓到的所有幣別匯率明細。
        /// </summary>
        public IList<ExchangeRateRecord> Records { get; private set; }
    }
}
