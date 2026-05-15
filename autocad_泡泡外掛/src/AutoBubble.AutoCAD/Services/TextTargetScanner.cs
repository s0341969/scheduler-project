using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AutoBubble.AutoCAD.Models;
using AutoBubble.Core.Configuration;
using AutoBubble.Core.Services;

namespace AutoBubble.AutoCAD.Services
{
    internal sealed class TextTargetScanner
    {
        private readonly AutoBubbleSettings _settings;
        private readonly BubbleTextParser _parser;
        private readonly XDataMarkerService _markerService;

        public TextTargetScanner(
            AutoBubbleSettings settings,
            BubbleTextParser parser,
            XDataMarkerService markerService)
        {
            _settings = settings;
            _parser = parser;
            _markerService = markerService;
        }

        public IReadOnlyList<DetectedTextTarget> ScanCurrentSpace(Database database, Transaction transaction)
        {
            var targets = new List<DetectedTextTarget>();
            var currentSpace = (BlockTableRecord)transaction.GetObject(database.CurrentSpaceId, OpenMode.ForRead);
            var visited = new HashSet<ObjectId>();

            foreach (ObjectId id in currentSpace)
            {
                AppendTargetsFromEntity(transaction, id, skipMarked: true, visited, targets);
            }

            return targets;
        }

        public IReadOnlyList<DetectedTextTarget> ScanSelection(Transaction transaction, IEnumerable<ObjectId> selectedIds)
        {
            var targets = new List<DetectedTextTarget>();
            var visited = new HashSet<ObjectId>();

            foreach (var id in selectedIds)
            {
                AppendTargetsFromEntity(transaction, id, skipMarked: true, visited, targets);
            }

            return targets;
        }

        private void AppendTargetsFromEntity(
            Transaction transaction,
            ObjectId id,
            bool skipMarked,
            ISet<ObjectId> visited,
            ICollection<DetectedTextTarget> targets)
        {
            if (!id.IsValid || id.IsErased || !visited.Add(id))
            {
                return;
            }

            if (TryBuildTarget(transaction, id, skipMarked, out var target))
            {
                targets.Add(target);
            }

            if (!(transaction.GetObject(id, OpenMode.ForRead, false) is BlockReference blockReference))
            {
                return;
            }

            foreach (ObjectId attributeId in blockReference.AttributeCollection)
            {
                if (!attributeId.IsValid || attributeId.IsErased || !visited.Add(attributeId))
                {
                    continue;
                }

                if (TryBuildTarget(transaction, attributeId, skipMarked, out var attributeTarget))
                {
                    targets.Add(attributeTarget);
                }
            }
        }

        private bool TryBuildTarget(Transaction transaction, ObjectId id, bool skipMarked, out DetectedTextTarget target)
        {
            target = null!;

            if (!id.IsValid || id.IsErased)
            {
                return false;
            }

            if (!(transaction.GetObject(id, OpenMode.ForRead, false) is Entity entity))
            {
                return false;
            }

            if (skipMarked && _markerService.HasBubbleMark(entity))
            {
                return false;
            }

            if (!TryGetText(entity, out var rawText))
            {
                return false;
            }

            if (!_parser.TryParse(rawText, out var numericValue, out var normalizedText))
            {
                return false;
            }

            if (!TryGetEntityExtents(entity, out var extents))
            {
                return false;
            }

            var center = new Point3d(
                (extents.MinPoint.X + extents.MaxPoint.X) / 2.0d,
                (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0d,
                0.0d);

            var width = Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
            var height = Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
            var halfDiagonal = Math.Sqrt((width * width) + (height * height)) / 2.0d;
            var radius = halfDiagonal * (1.0d + _settings.RadiusPaddingFactor);
            radius = Math.Max(_settings.MinimumRadius, Math.Min(_settings.MaximumRadius, radius));

            target = new DetectedTextTarget(id, normalizedText, numericValue, center, radius);
            return true;
        }

        private static bool TryGetText(Entity entity, out string rawText)
        {
            rawText = string.Empty;

            if (entity is DBText dbText)
            {
                rawText = dbText.TextString ?? string.Empty;
                return true;
            }

            if (entity is AttributeReference attributeReference)
            {
                rawText = attributeReference.TextString ?? string.Empty;
                return true;
            }

            if (entity is MText mText)
            {
                rawText = mText.Contents ?? string.Empty;
                return true;
            }

            return false;
        }

        private static bool TryGetEntityExtents(Entity entity, out Extents3d extents)
        {
            try
            {
                extents = entity.GeometricExtents;
                return true;
            }
            catch
            {
                extents = default;
                return false;
            }
        }
    }
}
