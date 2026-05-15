using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AutoBubble.AutoCAD.Constants;

namespace AutoBubble.AutoCAD.Services
{
    internal sealed class XDataMarkerService
    {
        public void EnsureRegisteredApp(Database database, Transaction transaction)
        {
            var regAppTable = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (regAppTable.Has(AutoBubbleConstants.ApplicationName))
            {
                return;
            }

            regAppTable.UpgradeOpen();
            var record = new RegAppTableRecord
            {
                Name = AutoBubbleConstants.ApplicationName
            };

            regAppTable.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }

        public bool HasBubbleMark(Entity entity)
        {
            var xdata = entity.XData;
            if (xdata == null)
            {
                return false;
            }

            var isAutoBubbleApp = false;
            foreach (TypedValue value in xdata)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataRegAppName)
                {
                    isAutoBubbleApp = string.Equals(
                        value.Value as string,
                        AutoBubbleConstants.ApplicationName,
                        StringComparison.Ordinal);
                    continue;
                }

                if (!isAutoBubbleApp)
                {
                    continue;
                }

                if (value.TypeCode == (int)DxfCode.ExtendedDataAsciiString &&
                    value.Value is string text &&
                    text.StartsWith(AutoBubbleConstants.XDataStatusPrefix + "|", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkSource(Entity sourceEntity, string bubbleHandle)
        {
            var values = new List<TypedValue>();
            var currentXData = sourceEntity.XData;
            if (currentXData != null)
            {
                var skipCurrentApp = false;
                foreach (TypedValue value in currentXData)
                {
                    if (value.TypeCode == (int)DxfCode.ExtendedDataRegAppName)
                    {
                        var appName = value.Value as string;
                        skipCurrentApp = string.Equals(appName, AutoBubbleConstants.ApplicationName, StringComparison.Ordinal);
                        if (skipCurrentApp)
                        {
                            continue;
                        }
                    }

                    if (!skipCurrentApp)
                    {
                        values.Add(value);
                    }
                }
            }

            values.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoBubbleConstants.ApplicationName));
            values.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, AutoBubbleConstants.XDataStatusPrefix + "|" + bubbleHandle));
            sourceEntity.XData = new ResultBuffer(values.ToArray());
        }
    }
}
