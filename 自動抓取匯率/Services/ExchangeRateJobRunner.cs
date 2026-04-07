using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotExchangeRateWinForms.Models;
using GonGinLibrary;

namespace BotExchangeRateWinForms.Services
{
    /// <summary>
    /// 協調抓取流程與資料庫寫入，提供主畫面單次執行入口。
    /// </summary>
    public sealed class ExchangeRateJobRunner
    {
        private readonly BotExchangeRateScraper _scraper;
        private readonly ExchangeRateSqlRepository _repository;
        private readonly SemaphoreSlim _runLock;

        /// <summary>
        /// 注入抓取器與資料庫存取元件。
        /// </summary>
        public ExchangeRateJobRunner(BotExchangeRateScraper scraper, ExchangeRateSqlRepository repository)
        {
            _scraper = scraper;
            _repository = repository;
            _runLock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// 執行一次完整工作，包含抓取、條件寫入與結果整理。
        /// </summary>
        public async Task<JobExecutionResult> ExecuteAsync(UserSettings settings)
        {
            if (!_runLock.Wait(0))
            {
                return new JobExecutionResult
                {
                    IsSkipped = true,
                    Message = "上一輪抓取尚未完成，已略過本次執行。"
                };
            }

            try
            {
                var scrapeResult = await _scraper.ScrapeAsync(settings).ConfigureAwait(false);
                var records = new List<ExchangeRateRecord>(scrapeResult.Records);

                if (!settings.WriteChrname && !settings.WriteChrnameHistory)
                {
                    return new JobExecutionResult
                    {
                        IsSuccess = true,
                        TotalRows = records.Count,
                        InsertedRows = 0,
                        DuplicateRows = 0,
                        SourceUpdatedAt = scrapeResult.SourceUpdatedAt,
                        Records = records,
                        Message = string.Format(
                            "抓取成功，共 {0} 筆，目前為「僅抓取不寫入資料庫」模式。",
                            records.Count)
                    };
                }

                var connectionString = GonGinVariable.SqlConnectString;


                    //_repository.ResolveConnectionString(settings);
                var dbResult = await _repository.SaveAsync(
                    connectionString,
                    scrapeResult,
                    settings.WriteChrname,
                    settings.WriteChrnameHistory).ConfigureAwait(false);

                var writeTargetText = settings.WriteChrname && settings.WriteChrnameHistory
                    ? "CHRNAME 與 CHRNAME-HISTORY"
                    : settings.WriteChrname
                        ? "CHRNAME"
                        : "CHRNAME-HISTORY";

                return new JobExecutionResult
                {
                    IsSuccess = true,
                    TotalRows = records.Count,
                    InsertedRows = dbResult.Item1,
                    DuplicateRows = dbResult.Item2,
                    SourceUpdatedAt = scrapeResult.SourceUpdatedAt,
                    Records = records,
                    Message = string.Format(
                        "抓取成功，共 {0} 筆，已同步 {1} {2} 筆，略過 {3} 筆。",
                        records.Count,
                        writeTargetText,
                        dbResult.Item1,
                        dbResult.Item2)
                };
            }
            catch (Exception ex)
            {
                return new JobExecutionResult
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
            finally
            {
                _runLock.Release();
            }
        }
    }
}
