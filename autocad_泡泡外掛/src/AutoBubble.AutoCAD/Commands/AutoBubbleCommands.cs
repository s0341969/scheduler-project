using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AutoBubble.AutoCAD.Services;
using AutoBubble.Core.Services;

namespace AutoBubble.AutoCAD.Commands
{
    public sealed class AutoBubbleCommands
    {
        [CommandMethod("AUTOBUBBLE_SCAN")]
        public void Scan()
        {
            ExecuteCommand("AUTOBUBBLE_SCAN", ExecuteScan);
        }

        [CommandMethod("AUTOBUBBLE_APPLY")]
        public void Apply()
        {
            ExecuteCommand("AUTOBUBBLE_APPLY", ExecuteApplyToCurrentSpace);
        }

        [CommandMethod("AUTOBUBBLE_PICK")]
        public void PickAndApply()
        {
            ExecuteCommand("AUTOBUBBLE_PICK", ExecuteApplyToSelection);
        }

        private static void ExecuteCommand(string commandName, System.Action<Document> action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                return;
            }

            try
            {
                action(document);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage($"\n{commandName} 執行失敗：{ex.Message}");
            }
        }

        private static void ExecuteScan(Document document)
        {
            var editor = document.Editor;
            var database = document.Database;
            var settings = new SettingsLoader().Load();
            var markerService = new XDataMarkerService();
            var scanner = new TextTargetScanner(settings, new BubbleTextParser(settings), markerService);

            using (document.LockDocument())
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                markerService.EnsureRegisteredApp(database, transaction);
                var targets = scanner.ScanCurrentSpace(database, transaction);

                editor.WriteMessage($"\nAutoBubble 掃描完成，找到 {targets.Count} 個候選文字。");
                var displayCount = targets.Count < 20 ? targets.Count : 20;
                for (var index = 0; index < displayCount; index++)
                {
                    var target = targets[index];
                    editor.WriteMessage(
                        $"\n[{index + 1}] 值={target.NumericValue}, 文字=\"{target.RawText}\", 中心=({target.Center.X:F3}, {target.Center.Y:F3}), 半徑={target.Radius:F3}");
                }

                if (targets.Count > displayCount)
                {
                    editor.WriteMessage($"\n其餘 {targets.Count - displayCount} 個候選未展開顯示。");
                }

                transaction.Commit();
            }
        }

        private static void ExecuteApplyToCurrentSpace(Document document)
        {
            using (document.LockDocument())
            {
                ApplyTargets(document, GetCurrentSpaceTargets);
            }
        }

        private static void ExecuteApplyToSelection(Document document)
        {
            var editor = document.Editor;
            var selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "\n請選取要加泡泡的文字或含屬性的圖塊："
            };

            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "TEXT"),
                new TypedValue((int)DxfCode.Start, "MTEXT"),
                new TypedValue((int)DxfCode.Start, "ATTRIB"),
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            });

            var selectionResult = editor.GetSelection(selectionOptions, filter);
            if (selectionResult.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n未選取任何可處理文字。");
                return;
            }

            using (document.LockDocument())
            {
                ApplyTargets(document, (scanner, database, transaction) =>
                {
                    var ids = new List<ObjectId>();
                    foreach (SelectedObject selectedObject in selectionResult.Value)
                    {
                        if (selectedObject != null && selectedObject.ObjectId.IsValid)
                        {
                            ids.Add(selectedObject.ObjectId);
                        }
                    }

                    return scanner.ScanSelection(transaction, ids);
                });
            }
        }

        private static void ApplyTargets(
            Document document,
            TargetResolver resolver)
        {
            var editor = document.Editor;
            var database = document.Database;
            var settings = new SettingsLoader().Load();
            var markerService = new XDataMarkerService();
            var scanner = new TextTargetScanner(settings, new BubbleTextParser(settings), markerService);
            var bubbleService = new BubbleEntityService();

            using (var transaction = database.TransactionManager.StartTransaction())
            {
                markerService.EnsureRegisteredApp(database, transaction);
                var currentSpace = (BlockTableRecord)transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite);
                var targets = resolver(scanner, database, transaction);

                var appliedCount = 0;
                foreach (var target in targets)
                {
                    var sourceEntity = (Entity)transaction.GetObject(target.SourceId, OpenMode.ForWrite);
                    if (markerService.HasBubbleMark(sourceEntity))
                    {
                        continue;
                    }

                    var bubbleId = bubbleService.CreateBubbleCircle(
                        database,
                        transaction,
                        currentSpace,
                        settings,
                        target.Center,
                        target.Radius);

                    var bubbleEntity = (Entity)transaction.GetObject(bubbleId, OpenMode.ForRead);
                    markerService.MarkSource(sourceEntity, bubbleEntity.Handle.ToString());
                    appliedCount++;
                }

                transaction.Commit();
                editor.WriteMessage($"\nAutoBubble 完成，新增 {appliedCount} 個泡泡。");
            }
        }

        private static IReadOnlyList<Models.DetectedTextTarget> GetCurrentSpaceTargets(
            TextTargetScanner scanner,
            Database database,
            Transaction transaction)
        {
            return scanner.ScanCurrentSpace(database, transaction);
        }

        private delegate IReadOnlyList<Models.DetectedTextTarget> TargetResolver(
            TextTargetScanner scanner,
            Database database,
            Transaction transaction);
    }
}
