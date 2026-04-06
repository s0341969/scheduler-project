using System;
using System.Collections.Generic;
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
                    Message = "\u4e0a\u4e00\u8f2a\u6293\u53d6\u5c1a\u672a\u5b8c\u6210\uff0c\u5df2\u7565\u904e\u672c\u6b21\u57f7\u884c\u3002"
                };
            }

            try
            {
                var scrapeResult = await _scraper.ScrapeAsync(settings).ConfigureAwait(false);
                var records = new List<ExchangeRateRecord>(scrapeResult.Records);

                if (!settings.WriteToDatabase)
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
                            "\u6293\u53d6\u6210\u529f\uff0c\u5171 {0} \u7b46\uff0c\u76ee\u524d\u70ba\u300c\u50c5\u6293\u53d6\u4e0d\u5beb\u5165\u8cc7\u6599\u5eab\u300d\u6a21\u5f0f\u3002",
                            records.Count)
                    };
                }

                var connectionString = _repository.ResolveConnectionString(settings);
                var dbResult = await _repository.SaveAsync(connectionString, scrapeResult).ConfigureAwait(false);

                return new JobExecutionResult
                {
                    IsSuccess = true,
                    TotalRows = records.Count,
                    InsertedRows = dbResult.Item1,
                    DuplicateRows = dbResult.Item2,
                    SourceUpdatedAt = scrapeResult.SourceUpdatedAt,
                    Records = records,
                    Message = string.Format(
                        "\u6293\u53d6\u6210\u529f\uff0c\u5171 {0} \u7b46\uff0c\u5df2\u540c\u6b65 CHRNAME \u8207 CHRNAME-HISTORY {1} \u7b46\uff0c\u7565\u904e {2} \u7b46\u3002",
                        records.Count,
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
