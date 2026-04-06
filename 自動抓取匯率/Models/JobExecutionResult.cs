using System;
using System.Collections.Generic;

namespace BotExchangeRateWinForms.Models
{
    public sealed class JobExecutionResult
    {
        public JobExecutionResult()
        {
            Records = new List<ExchangeRateRecord>();
        }

        public bool IsSuccess { get; set; }

        public bool IsSkipped { get; set; }

        public int TotalRows { get; set; }

        public int InsertedRows { get; set; }

        public int DuplicateRows { get; set; }

        public string Message { get; set; }

        public DateTime? SourceUpdatedAt { get; set; }

        public IList<ExchangeRateRecord> Records { get; set; }
    }
}
