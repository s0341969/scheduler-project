using System;
using System.Collections.Generic;

namespace BotExchangeRateWinForms.Models
{
    /// <summary>
    /// 表示一次手動或 Timer 執行的最終回傳結果。
    /// </summary>
    public sealed class JobExecutionResult
    {
        /// <summary>
        /// 初始化畫面顯示需要的結果集合。
        /// </summary>
        public JobExecutionResult()
        {
            Records = new List<ExchangeRateRecord>();
        }

        /// <summary>
        /// 是否執行成功。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 是否因前一輪尚未完成而被略過。
        /// </summary>
        public bool IsSkipped { get; set; }

        /// <summary>
        /// 本次抓到的總筆數。
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// 實際寫入資料庫的幣別筆數。
        /// </summary>
        public int InsertedRows { get; set; }

        /// <summary>
        /// 因未對應幣別或匯率未變動而略過的筆數。
        /// </summary>
        public int DuplicateRows { get; set; }

        /// <summary>
        /// 顯示給畫面的執行結果訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 來源頁面顯示的最新掛牌時間。
        /// </summary>
        public DateTime? SourceUpdatedAt { get; set; }

        /// <summary>
        /// 本次抓到的匯率明細，用於畫面 Grid 顯示。
        /// </summary>
        public IList<ExchangeRateRecord> Records { get; set; }
    }
}
