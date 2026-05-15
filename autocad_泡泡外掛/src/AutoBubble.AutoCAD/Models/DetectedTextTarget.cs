using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AutoBubble.AutoCAD.Models
{
    internal sealed class DetectedTextTarget
    {
        public DetectedTextTarget(ObjectId sourceId, string rawText, int numericValue, Point3d center, double radius)
        {
            SourceId = sourceId;
            RawText = rawText;
            NumericValue = numericValue;
            Center = center;
            Radius = radius;
        }

        public ObjectId SourceId { get; }

        public string RawText { get; }

        public int NumericValue { get; }

        public Point3d Center { get; }

        public double Radius { get; }
    }
}
