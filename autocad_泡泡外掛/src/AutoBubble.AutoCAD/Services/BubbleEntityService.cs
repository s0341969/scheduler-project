using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AutoBubble.Core.Configuration;

namespace AutoBubble.AutoCAD.Services
{
    internal sealed class BubbleEntityService
    {
        public ObjectId CreateBubbleCircle(
            Database database,
            Transaction transaction,
            BlockTableRecord currentSpace,
            AutoBubbleSettings settings,
            Point3d center,
            double radius)
        {
            var layerId = EnsureLayer(database, transaction, settings);

            var circle = new Circle(center, Vector3d.ZAxis, radius)
            {
                LayerId = layerId,
                Color = Color.FromColorIndex(ColorMethod.ByAci, settings.BubbleColorIndex)
            };

            var id = currentSpace.AppendEntity(circle);
            transaction.AddNewlyCreatedDBObject(circle, true);
            return id;
        }

        private static ObjectId EnsureLayer(Database database, Transaction transaction, AutoBubbleSettings settings)
        {
            var layerTable = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (layerTable.Has(settings.BubbleLayerName))
            {
                return layerTable[settings.BubbleLayerName];
            }

            layerTable.UpgradeOpen();
            var record = new LayerTableRecord
            {
                Name = settings.BubbleLayerName,
                Color = Color.FromColorIndex(ColorMethod.ByAci, settings.BubbleColorIndex)
            };

            var layerId = layerTable.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
            return layerId;
        }
    }
}
