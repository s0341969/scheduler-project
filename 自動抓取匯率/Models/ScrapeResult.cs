using System;
using System.Collections.Generic;

namespace BotExchangeRateWinForms.Models
{
    public sealed class ScrapeResult
    {
        public ScrapeResult()
        {
            Records = new List<ExchangeRateRecord>();
        }

        public DateTime SourceRateDate { get; set; }

        public DateTime SourceUpdatedAt { get; set; }

        public IList<ExchangeRateRecord> Records { get; private set; }
    }
}
