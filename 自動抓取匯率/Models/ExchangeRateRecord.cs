using System;

namespace BotExchangeRateWinForms.Models
{
    public sealed class ExchangeRateRecord
    {
        public string CurrencyCode { get; set; }

        public string CurrencyName { get; set; }

        public decimal? CashBuy { get; set; }

        public decimal? CashSell { get; set; }

        public decimal? SpotBuy { get; set; }

        public decimal? SpotSell { get; set; }

        public DateTime SourceRateDate { get; set; }

        public DateTime SourceUpdatedAt { get; set; }

        public string SourceUrl { get; set; }
    }
}
