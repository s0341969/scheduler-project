using System;
using System.Threading;
using System.Threading.Tasks;
using BotExchangeRateWinForms.Models;

namespace BotExchangeRateWinForms.Services
{
    public sealed class ExchangeRateJobRunner
    {
        private readonly BotExchangeRateScraper _scraper;
        private readonly ExchangeRateSqlRepository _repository;
        private readonly SemaphoreSlim _runLock;

        public ExchangeRateJobRunner(BotExchangeRateScraper scraper, ExchangeRateSqlRepository repository)
        {
            _scraper = scraper;
            _repository = repository;
            _runLock = new SemaphoreSlim(1, 1);
        }

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
                var connectionString = _repository.ResolveConnectionString(settings);
                var scrapeResult = await _scraper.ScrapeAsync(settings).ConfigureAwait(false);
                var dbResult = await _repository.SaveAsync(connectionString, scrapeResult).ConfigureAwait(false);

                return new JobExecutionResult
                {
                    IsSuccess = true,
                    TotalRows = scrapeResult.Records.Count,
                    InsertedRows = dbResult.Item1,
                    DuplicateRows = dbResult.Item2,
                    SourceUpdatedAt = scrapeResult.SourceUpdatedAt,
                    Message = string.Format(
                        "抓取成功，共 {0} 筆，新增 {1} 筆，重複 {2} 筆。",
                        scrapeResult.Records.Count,
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
