using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotExchangeRateWinForms.Models;

namespace BotExchangeRateWinForms.Services
{
    /// <summary>
    /// 負責下載臺銀匯率頁面並解析成可寫入資料庫的結構。
    /// </summary>
    public sealed class BotExchangeRateScraper
    {
        /// <summary>
        /// 擷取頁面中的最新掛牌時間。
        /// </summary>
        private static readonly Regex UpdatedAtRegex = new Regex(
            "\\u724c\\u50f9\\u6700\\u65b0\\u639b\\u724c\\u6642\\u9593\\s*[:\\uFF1A]\\s*(?<value>\\d{4}/\\d{2}/\\d{2}\\s+\\d{2}:\\d{2})",
            RegexOptions.Compiled);

        /// <summary>
        /// 擷取頁面中的掛牌日期。
        /// </summary>
        private static readonly Regex RateDateRegex = new Regex(
            "(?<value>\\d{4}/\\d{2}/\\d{2})\\s*\\u672c\\u884c\\s*.*?\\u724c\\u544a\\u532f\\u7387",
            RegexOptions.Compiled);

        /// <summary>
        /// 找出表格中的每一列資料。
        /// </summary>
        private static readonly Regex TableRowRegex = new Regex(
            "<tr[^>]*>(?<row>.*?)</tr>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// 找出每列中的儲存格內容。
        /// </summary>
        private static readonly Regex TableCellRegex = new Regex(
            "<td[^>]*>(?<cell>.*?)</td>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// 從「幣別名稱 (代碼)」格式中拆出名稱與代碼。
        /// </summary>
        private static readonly Regex CurrencyRegex = new Regex(
            "^(?<name>.+?)\\s*\\((?<code>[A-Z]{3})\\)$",
            RegexOptions.Compiled);

        /// <summary>
        /// 判斷欄位是否為可解析的數值字串。
        /// </summary>
        private static readonly Regex NumericRegex = new Regex(
            "^(?:-|\\d+(?:\\.\\d+)?)$",
            RegexOptions.Compiled);

        /// <summary>
        /// 依設定抓取來源頁面 HTML，並轉成結構化結果。
        /// </summary>
        public async Task<ScrapeResult> ScrapeAsync(UserSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (string.IsNullOrWhiteSpace(settings.SourceUrl))
            {
                throw new InvalidOperationException("\u5c1a\u672a\u8a2d\u5b9a\u4f86\u6e90\u7db2\u5740\u3002");
            }

            string html;
            using (var client = CreateHttpClient(settings))
            {
                html = await client.GetStringAsync(settings.SourceUrl).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                throw new InvalidOperationException("\u4f86\u6e90\u9801\u9762\u6c92\u6709\u56de\u50b3\u5167\u5bb9\u3002");
            }

            return Parse(html, settings.SourceUrl);
        }

        /// <summary>
        /// 解析下載到的 HTML，組出完整的抓取結果與各幣別明細。
        /// </summary>
        internal ScrapeResult Parse(string html, string sourceUrl)
        {
            var plainText = NormalizeWhitespace(RemoveHtmlTags(html));
            var updatedAt = ParseDateTime(UpdatedAtRegex, plainText, "\u627e\u4e0d\u5230\u300e\u724c\u50f9\u6700\u65b0\u639b\u724c\u6642\u9593\u300f\u3002");
            var rateDate = ParseDate(RateDateRegex, plainText, updatedAt.Date);

            var result = new ScrapeResult
            {
                SourceRateDate = rateDate,
                SourceUpdatedAt = updatedAt
            };

            var rows = TableRowRegex.Matches(html);
            foreach (Match rowMatch in rows)
            {
                var rowHtml = rowMatch.Groups["row"].Value;
                if (rowHtml.IndexOf('(') < 0)
                {
                    continue;
                }

                var cells = ExtractCells(rowHtml);
                if (cells.Count < 5)
                {
                    continue;
                }

                var currencyMatch = CurrencyRegex.Match(cells[0]);
                if (!currencyMatch.Success)
                {
                    continue;
                }

                var numericCells = new List<string>();
                for (var i = 1; i < cells.Count; i++)
                {
                    if (NumericRegex.IsMatch(cells[i]))
                    {
                        numericCells.Add(cells[i]);
                    }
                }

                if (numericCells.Count < 4)
                {
                    continue;
                }

                result.Records.Add(new ExchangeRateRecord
                {
                    CurrencyName = currencyMatch.Groups["name"].Value.Trim(),
                    CurrencyCode = currencyMatch.Groups["code"].Value.Trim(),
                    CashBuy = ParseDecimalOrNull(numericCells[0]),
                    CashSell = ParseDecimalOrNull(numericCells[1]),
                    SpotBuy = ParseDecimalOrNull(numericCells[2]),
                    SpotSell = ParseDecimalOrNull(numericCells[3]),
                    SourceRateDate = rateDate,
                    SourceUpdatedAt = updatedAt,
                    SourceUrl = sourceUrl
                });
            }

            if (result.Records.Count == 0)
            {
                throw new InvalidOperationException("\u89e3\u6790\u5b8c\u6210\uff0c\u4f46\u6c92\u6709\u627e\u5230\u4efb\u4f55\u532f\u7387\u8cc7\u6599\u3002\u53ef\u80fd\u662f\u9801\u9762\u7d50\u69cb\u5df2\u8b8a\u66f4\u3002");
            }

            return result;
        }

        /// <summary>
        /// 建立抓取用 HttpClient，包含逾時、壓縮與 User-Agent 設定。
        /// </summary>
        private static HttpClient CreateHttpClient(UserSettings settings)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, settings.RequestTimeoutSeconds));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) BotExchangeRateWinForms/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            return client;
        }

        /// <summary>
        /// 從純文字中解析臺銀顯示的最新掛牌時間。
        /// </summary>
        private static DateTime ParseDateTime(Regex regex, string input, string errorMessage)
        {
            var match = regex.Match(input);
            if (!match.Success)
            {
                throw new InvalidOperationException(errorMessage);
            }

            DateTime parsedValue;
            if (!DateTime.TryParseExact(
                match.Groups["value"].Value,
                "yyyy/MM/dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedValue))
            {
                throw new InvalidOperationException("\u7121\u6cd5\u89e3\u6790\u724c\u50f9\u66f4\u65b0\u6642\u9593\u3002");
            }

            return parsedValue;
        }

        /// <summary>
        /// 從頁面中解析掛牌日期；若缺少則退回更新時間的日期部分。
        /// </summary>
        private static DateTime ParseDate(Regex regex, string input, DateTime fallbackDate)
        {
            var match = regex.Match(input);
            if (!match.Success)
            {
                return fallbackDate.Date;
            }

            DateTime parsedValue;
            if (DateTime.TryParseExact(
                match.Groups["value"].Value,
                "yyyy/MM/dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedValue))
            {
                return parsedValue.Date;
            }

            return fallbackDate.Date;
        }

        /// <summary>
        /// 從單列表格 HTML 中擷取所有有效儲存格文字。
        /// </summary>
        private static List<string> ExtractCells(string rowHtml)
        {
            var cells = new List<string>();
            var matches = TableCellRegex.Matches(rowHtml);
            foreach (Match match in matches)
            {
                var cellText = NormalizeWhitespace(RemoveHtmlTags(match.Groups["cell"].Value));
                if (!string.IsNullOrWhiteSpace(cellText))
                {
                    cells.Add(cellText);
                }
            }

            return cells;
        }

        /// <summary>
        /// 移除 HTML 標籤、script、style，保留可解析文字。
        /// </summary>
        private static string RemoveHtmlTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var withoutScripts = Regex.Replace(input, "<script.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var withoutStyles = Regex.Replace(withoutScripts, "<style.*?</style>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var withoutTags = Regex.Replace(withoutStyles, "<[^>]+>", " ");
            return WebUtility.HtmlDecode(withoutTags);
        }

        /// <summary>
        /// 將空白字元整理成單一空格，方便 Regex 比對。
        /// </summary>
        private static string NormalizeWhitespace(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return Regex.Replace(input, "\\s+", " ").Trim();
        }

        /// <summary>
        /// 將數值字串轉成 decimal；若為破折號則視為無值。
        /// </summary>
        private static decimal? ParseDecimalOrNull(string value)
        {
            if (value == "-")
            {
                return null;
            }

            decimal parsedValue;
            if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedValue))
            {
                throw new InvalidOperationException(string.Format("\u7121\u6cd5\u89e3\u6790\u532f\u7387\u6578\u503c\uff1a{0}", value));
            }

            return parsedValue;
        }
    }
}
