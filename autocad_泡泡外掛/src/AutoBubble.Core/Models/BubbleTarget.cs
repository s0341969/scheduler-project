namespace AutoBubble.Core.Models
{
    public sealed class BubbleTarget
    {
        public BubbleTarget(string textValue, int numericValue, double centerX, double centerY, double radius)
        {
            TextValue = textValue;
            NumericValue = numericValue;
            CenterX = centerX;
            CenterY = centerY;
            Radius = radius;
        }

        public string TextValue { get; }

        public int NumericValue { get; }

        public double CenterX { get; }

        public double CenterY { get; }

        public double Radius { get; }
    }
}
