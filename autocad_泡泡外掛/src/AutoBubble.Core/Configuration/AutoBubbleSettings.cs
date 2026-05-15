using System;

namespace AutoBubble.Core.Configuration
{
    public sealed class AutoBubbleSettings
    {
        public string BubbleLayerName { get; set; } = "AUTOBUBBLE";

        public short BubbleColorIndex { get; set; } = 1;

        public double RadiusPaddingFactor { get; set; } = 0.35d;

        public double MinimumRadius { get; set; } = 1.5d;

        public double MaximumRadius { get; set; } = 100.0d;

        public int MinimumValue { get; set; } = 1;

        public int MaximumValue { get; set; } = 9999;

        public bool AcceptOnlyPureNumber { get; set; } = true;

        public static AutoBubbleSettings Sanitize(AutoBubbleSettings? settings)
        {
            var result = settings ?? new AutoBubbleSettings();

            if (string.IsNullOrWhiteSpace(result.BubbleLayerName))
            {
                result.BubbleLayerName = "AUTOBUBBLE";
            }

            result.RadiusPaddingFactor = Clamp(result.RadiusPaddingFactor, 0.0d, 3.0d);
            result.MinimumRadius = Clamp(result.MinimumRadius, 0.1d, 1000.0d);
            result.MaximumRadius = Clamp(result.MaximumRadius, result.MinimumRadius, 5000.0d);
            result.MinimumValue = Math.Max(0, result.MinimumValue);
            result.MaximumValue = Math.Max(result.MinimumValue, result.MaximumValue);

            return result;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
