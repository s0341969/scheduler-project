using System.Globalization;
using System.Text.RegularExpressions;
using AutoBubble.Core.Configuration;

namespace AutoBubble.Core.Services
{
    public sealed class BubbleTextParser
    {
        private static readonly Regex PureNumberPattern = new Regex(@"^\d+$", RegexOptions.Compiled);
        private static readonly Regex NumberExtractorPattern = new Regex(@"\d+", RegexOptions.Compiled);
        private readonly AutoBubbleSettings _settings;

        public BubbleTextParser(AutoBubbleSettings settings)
        {
            _settings = settings;
        }

        public bool TryParse(string? rawText, out int numericValue, out string normalizedText)
        {
            numericValue = 0;
            normalizedText = string.Empty;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            normalizedText = Normalize(rawText!);
            if (normalizedText.Length == 0)
            {
                return false;
            }

            if (_settings.AcceptOnlyPureNumber)
            {
                if (!PureNumberPattern.IsMatch(normalizedText))
                {
                    return false;
                }

                if (!int.TryParse(normalizedText, NumberStyles.None, CultureInfo.InvariantCulture, out numericValue))
                {
                    return false;
                }

                return numericValue >= _settings.MinimumValue && numericValue <= _settings.MaximumValue;
            }

            var match = NumberExtractorPattern.Match(normalizedText);
            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out numericValue))
            {
                return false;
            }

            return numericValue >= _settings.MinimumValue && numericValue <= _settings.MaximumValue;
        }

        private static string Normalize(string rawText)
        {
            return rawText
                .Replace("\\P", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }
    }
}
