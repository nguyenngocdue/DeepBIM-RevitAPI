using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace DeepBIM.Helpers
{
    public class AlignService : IAlignService
    {
        public Result AlignElements(UIDocument uiDoc, Document doc, AlignType alignType, double? minGap = null)
        {
            using var txg = new TransactionGroup(doc, "Align Elements");
            try
            {
                txg.Start();
                // ✅ Lấy minGap từ settings (theo mm)
                double minGap_mm = SettingsManager.GetMinGap();
                double minGap_meters = minGap_mm / 1000.0;
                double minGap_Internal = UnitUtils.ConvertToInternalUnits( minGap_meters, UnitTypeId.Meters);


                // 1. Lấy danh sách phần tử đã chọn
                List<ElementId> ids = uiDoc.Selection.GetElementIds().ToList();

                // Nếu chọn ít hơn 2 phần tử → yêu cầu pick
                if (ids.Count < 2)
                {
                    string prompt = alignType is AlignType.DistributeHorizontally or AlignType.DistributeVertically
                        ? "Select 3 or more objects to distribute (outermost elements will be fixed)."
                        : "Select objects to align (>=2); alignment will be based on the group's extreme edge.";

                    IList<Reference> picked = uiDoc.Selection.PickObjects(
                        ObjectType.Element, prompt);

                    ids = picked.Select(r => r.ElementId).ToList();
                    uiDoc.Selection.SetElementIds(ids);

                    if (ids.Count < 2)
                        throw new OperationCanceledException("At least 2 elements are required.");
                }

                View view = doc.ActiveView;
                List<Element> elements = ids
                    .Select(id => doc.GetElement(id))
                    .Where(e => e != null)
                    .ToList();

                // Lấy BoundingBox theo view
                var boxes = elements
                    .Select(e => (Element: e, Box: e.get_BoundingBox(view)))
                    .Where(x => x.Box != null)
                    .ToList();

                if (boxes.Count < 2)
                {
                    TaskDialog.Show("Error", "At least two elements must have a valid BoundingBox.");
                    txg.RollBack();
                    return Result.Failed;
                }

                // Lấy hướng của view
                XYZ viewRight = (view.RightDirection.Normalize() ?? XYZ.BasisX);
                XYZ viewUp = (view.UpDirection.Normalize() ?? XYZ.BasisY);

                using (Transaction tx = new Transaction(doc, $"Align {alignType}"))
                {
                    tx.Start();

                    switch (alignType)
                    {
                        case AlignType.Left:
                            AlignToEdge(boxes, viewRight, useMin: true);
                            break;
                        case AlignType.Right:
                            AlignToEdge(boxes, viewRight, useMin: false);
                            break;
                        case AlignType.Bottom:
                            AlignToEdge(boxes, viewUp, useMin: true);
                            break;
                        case AlignType.Top:
                            AlignToEdge(boxes, viewUp, useMin: false);
                            break;
                        case AlignType.CenterX:
                            AlignToCenter(boxes, viewRight);
                            break;
                        case AlignType.CenterY:
                            AlignToCenter(boxes, viewUp);
                            break;
                        case AlignType.DistributeHorizontally:
                            if (boxes.Count >= 3)
                                Distribute(boxes, viewRight, minGap_Internal);
                            break;
                        case AlignType.DistributeVertically:
                            if (boxes.Count >= 3)
                                Distribute(boxes, viewUp, minGap_Internal);
                            break;
                        case AlignType.UntangleHorizontally:
                            Untangle(boxes, viewRight, minGap_Internal);
                            break;
                        case AlignType.UntangleVertically:
                            Untangle(boxes, viewUp, minGap_Internal);
                            break;
                    }

                    tx.Commit();
                }

                txg.Assimilate();
                return Result.Succeeded;
            }
            catch (OperationCanceledException)
            {
                if (txg.HasStarted()) txg.RollBack();
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                if (txg.HasStarted()) txg.RollBack();
                TaskDialog.Show("Alignment Error", "An error occurred:\n" + ex.Message);
                return Result.Failed;
            }
        }

        // Căn theo biên: Min (Left/Bottom) hoặc Max (Right/Top)
        private void AlignToEdge(List<(Element Element, BoundingBoxXYZ Box)> boxes, XYZ direction, bool useMin)
        {
            double targetPos = useMin
                ? boxes.Min(b => b.Box.Min.DotProduct(direction))
                : boxes.Max(b => b.Box.Max.DotProduct(direction));

            foreach (var item in boxes)
            {
                double currentPos = useMin
                    ? item.Box.Min.DotProduct(direction)
                    : item.Box.Max.DotProduct(direction);

                XYZ delta = (targetPos - currentPos) * direction;

                if (!IsZero(delta))
                {
                    ElementTransformUtils.MoveElement(item.Element.Document, item.Element.Id, delta);
                }
            }
        }

        // Căn theo tâm nhóm
        private void AlignToCenter(List<(Element Element, BoundingBoxXYZ Box)> boxes, XYZ direction)
        {
            double min = boxes.Min(b => b.Box.Min.DotProduct(direction));
            double max = boxes.Max(b => b.Box.Max.DotProduct(direction));
            double targetPos = (min + max) * 0.5;

            foreach (var item in boxes)
            {
                XYZ center = (item.Box.Min + item.Box.Max) * 0.5;
                double currentPos = center.DotProduct(direction);
                XYZ delta = (targetPos - currentPos) * direction;

                if (!IsZero(delta))
                {
                    ElementTransformUtils.MoveElement(item.Element.Document, item.Element.Id, delta);
                }
            }
        }

        // Phân bố đều: giữ phần tử đầu và cuối (theo tọa độ), các phần tử giữa chia đều khoảng cách
        private void Distribute(List<(Element Element, BoundingBoxXYZ Box)> boxes, XYZ direction, double minGap = 0.0)
        {
            var centers = boxes.Select(b =>
            {
                XYZ c = (b.Box.Min + b.Box.Max) * 0.5;
                double pos = c.DotProduct(direction);
                return (Box: b, Pos: pos);
            })
            .OrderBy(x => x.Pos)
            .ToList();

            double startPos = centers.First().Pos;
            double endPos = centers.Last().Pos;
            int count = centers.Count;
            double totalSpacing = (count - 1) * minGap;
            double availableSpace = endPos - startPos;

            if (availableSpace < totalSpacing)
            {
                // Không đủ chỗ → dàn đều tối đa
                // Hoặc có thể giữ nguyên và thông báo
                return;
            }

            double spacing = (availableSpace - totalSpacing) / (count - 1);

            for (int i = 1; i < count - 1; i++)
            {
                var item = centers[i];
                double targetPos = startPos + i * (spacing + minGap);
                double offset = targetPos - item.Pos;
                XYZ delta = offset * direction;

                if (!IsZero(delta))
                {
                    ElementTransformUtils.MoveElement(item.Box.Element.Document, item.Box.Element.Id, delta);
                }
            }
        }

        private void Untangle(List<(Element Element, BoundingBoxXYZ Box)> boxes, XYZ direction, double minGap = 0.0)
        {
            XYZ dir = direction.Normalize() ?? XYZ.BasisX;

            var items = boxes.Select(b =>
            {
                double min = b.Box.Min.DotProduct(dir);
                double max = b.Box.Max.DotProduct(dir);
                return new { Element = b.Element, Min = min, Max = max, Id = b.Element.Id };
            })
            .OrderBy(x => x.Min)
            .ToList();

            if (items.Count < 2) return;

            double currentEnd = items[0].Max;
            var moveDelta = new Dictionary<ElementId, XYZ>();

            for (int i = 1; i < items.Count; i++)
            {
                var item = items[i];

                if (item.Min < currentEnd + minGap - 1e-9)
                {
                    double offset = currentEnd + minGap - item.Min;
                    XYZ delta = offset * dir;
                    moveDelta[item.Id] = delta;

                    // Cập nhật lại vị trí sau khi di chuyển
                    double newMax = item.Max + offset;
                    currentEnd = Math.Max(currentEnd, newMax);
                }
                else
                {
                    currentEnd = Math.Max(currentEnd, item.Max);
                }
            }

            foreach (var (id, delta) in moveDelta)
            {
                ElementTransformUtils.MoveElement(boxes[0].Element.Document, id, delta);
            }
        }

        private static bool IsZero(XYZ v, double tolerance = 1e-9)
        {
            return Math.Abs(v.X) < tolerance &&
                   Math.Abs(v.Y) < tolerance &&
                   Math.Abs(v.Z) < tolerance;
        }
    }
}