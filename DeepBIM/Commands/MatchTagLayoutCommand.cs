using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class MatchTagLayoutCommand : IExternalCommand
    {
       
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = data.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View view = doc.ActiveView;
            Selection sel = uiDoc.Selection;

            try
            {
                // 1) Pick base tag
                Reference baseRef = sel.PickObject(ObjectType.Element, new TagSelectionFilter(),
                                   "Pick BASE tag (layout source)");
                var baseTag = doc.GetElement(baseRef) as IndependentTag;
                if (baseTag == null)
                {
                    TaskDialog.Show("Match Tag Layout", "Please pick an IndependentTag as base.");
                    return Result.Cancelled;
                }

                // Lưu offsets trên mặt phẳng view (Right/Up)
                if (!TryGetLayout(view, baseTag, out var baseLayout))
                {
                    TaskDialog.Show("Match Tag Layout", "Base tag has no leader/head layout that can be replicated.");
                    return Result.Cancelled;
                }

                // 2) Pick targets
                IList<Reference> picked = sel.PickObjects(ObjectType.Element, new TagSelectionFilter(),
                                           "Pick other tags to match the layout");
                List<IndependentTag> targets = picked
                    .Select(r => doc.GetElement(r))
                    .OfType<IndependentTag>()
                    .Where(t => t.Id != baseTag.Id)
#if REVIT2024_OR_GREATER
                    .GroupBy(t => t.Id.Value).Select(g => g.First())
#else
                    .GroupBy(t => t.Id.IntegerValue).Select(g => g.First())
#endif
                    .ToList();

                if (targets.Count == 0) return Result.Cancelled;

                using (Transaction t = new Transaction(doc, "Match Tag Layout"))
                {
                    t.Start();
                    foreach (var tag in targets)
                    {
                        ApplyLayout(view, tag, baseLayout);
                    }
                    t.Commit();
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                TaskDialog.Show("Match Tag Layout - Error", ex.ToString());
                return Result.Failed;
            }
        }

        // ------------------------ Helpers ------------------------
        private class TagSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is IndependentTag;
            public bool AllowReference(Reference reference, XYZ position) => true;
        }

        #region Layout data
        private class TagLayout
        {
            public bool HasLeader;
            public XYZ HeadOffsetRU;    // offset từ LeaderEnd -> TagHead (hệ Right/Up)
            public XYZ ElbowOffsetRU;   // offset từ LeaderEnd -> LeaderElbow (nếu có), null nếu không
            public bool HasElbow => ElbowOffsetRU != null;
        }
        #endregion

        /// Lấy bố cục của base: offsets được biểu diễn trong hệ toạ độ (Right,Up) của view
        private static bool TryGetLayout(View view, IndependentTag tag, out TagLayout layout)
        {
            layout = null;

            // --- lấy các điểm cần thiết theo kiểu "compat" ---
            bool hasLeader = GetHasLeaderCompat(tag);
            XYZ end = GetLeaderEndCompat(tag);           // có thể null ở API cũ
            XYZ head = tag.TagHeadPosition;               // luôn có
            XYZ elbow = hasLeader ? GetLeaderElbowCompat(tag) : null;

            // Nếu API không cho truy xuất LeaderEnd, ta coi end = elbow hoặc rơi về TaggedLocalPoint
            if (end == null)
            {
                end = elbow ?? GetTaggedLocalPointCompat(tag);
            }

            if (end == null || head == null) return false;

            // offset WORLD
            XYZ headOffset = head.Subtract(end);
            XYZ elbowOffset = (elbow != null) ? elbow.Subtract(end) : null;

            layout = new TagLayout
            {
                HasLeader = hasLeader,
                HeadOffsetRU = WorldToRU(view, headOffset),
                ElbowOffsetRU = (elbowOffset != null) ? WorldToRU(view, elbowOffset) : null
            };
            return true;
        }

        /// Áp bố cục vào tag đích
        private static void ApplyLayout(View view, IndependentTag tag, TagLayout layout)
        {
            // Bật/tắt leader nếu API hỗ trợ
            SetHasLeaderCompat(tag, layout.HasLeader);

            // Lấy end theo compat; nếu vẫn null thì lấy tạm head làm mốc
            XYZ end = GetLeaderEndCompat(tag) ?? GetTaggedLocalPointCompat(tag) ?? tag.TagHeadPosition;

            // Đặt lại head
            XYZ newHead = end.Add(RUToWorld(view, layout.HeadOffsetRU));
            tag.TagHeadPosition = newHead;

            // Đặt elbow nếu có và API hỗ trợ
            if (layout.HasLeader && layout.ElbowOffsetRU != null)
            {
                XYZ newElbow = end.Add(RUToWorld(view, layout.ElbowOffsetRU));
                SetLeaderElbowCompat(tag, newElbow);   // nếu API không có setter -> sẽ bỏ qua
            }
        }

        private static XYZ WorldToRU(View view, XYZ v)
        {
            XYZ r = view.RightDirection.Normalize();
            XYZ u = view.UpDirection.Normalize();
            return new XYZ(v.DotProduct(r), v.DotProduct(u), 0.0);
        }

        private static XYZ RUToWorld(View view, XYZ ru)
        {
            XYZ r = view.RightDirection.Normalize();
            XYZ u = view.UpDirection.Normalize();
            return r.Multiply(ru.X).Add(u.Multiply(ru.Y));
        }

        private static bool GetHasLeaderCompat(IndependentTag tag)
        {
            // property HasLeader có từ rất sớm, nhưng vẫn dùng reflection cho an toàn
            var p = tag.GetType().GetProperty("HasLeader", BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.CanRead && p.PropertyType == typeof(bool))
                return (bool)p.GetValue(tag);
            // fallback: coi như không có leader
            return false;
        }

        /// cố gắng lấy LeaderEnd theo nhiều cách
        private static XYZ GetLeaderEndCompat(IndependentTag tag)
        {
            // 1) Property LeaderEnd
            var p = tag.GetType().GetProperty("LeaderEnd", BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.CanRead && p.PropertyType == typeof(XYZ))
                return (XYZ)p.GetValue(tag);

            // 2) Getter "get_LeaderEnd"
            var m = tag.GetType().GetMethod("get_LeaderEnd", BindingFlags.Instance | BindingFlags.Public);
            if (m != null && m.ReturnType == typeof(XYZ))
                return (XYZ)m.Invoke(tag, null);

            // 3) API cũ: TaggedLocalPoint (điểm bám lên host) – gần tương đương LeaderEnd
            return GetTaggedLocalPointCompat(tag);
        }

        /// cố gắng lấy LeaderElbow; nếu không có API, trả null
        private static XYZ GetLeaderElbowCompat(IndependentTag tag)
        {
            var p = tag.GetType().GetProperty("LeaderElbow", BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.CanRead && p.PropertyType == typeof(XYZ))
                return (XYZ)p.GetValue(tag);

            var m = tag.GetType().GetMethod("get_LeaderElbow", BindingFlags.Instance | BindingFlags.Public);
            if (m != null && m.ReturnType == typeof(XYZ))
                return (XYZ)m.Invoke(tag, null);

            return null;
        }

        /// đặt LeaderElbow nếu API có; nếu không thì bỏ qua
        private static void SetLeaderElbowCompat(IndependentTag tag, XYZ value)
        {
            var p = tag.GetType().GetProperty("LeaderElbow", BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.CanWrite && p.PropertyType == typeof(XYZ))
            {
                p.SetValue(tag, value);
                return;
            }

            var m = tag.GetType().GetMethod("set_LeaderElbow", BindingFlags.Instance | BindingFlags.Public);
            if (m != null)
                m.Invoke(tag, new object[] { value });
        }

        /// đặt HasLeader nếu API cho phép; nếu không có setter thì bỏ qua
        private static void SetHasLeaderCompat(IndependentTag tag, bool hasLeader)
        {
            var p = tag.GetType().GetProperty("HasLeader", BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.CanWrite && p.PropertyType == typeof(bool))
            {
                p.SetValue(tag, hasLeader);
                return;
            }

            var m = tag.GetType().GetMethod("set_HasLeader", BindingFlags.Instance | BindingFlags.Public);
            if (m != null)
                m.Invoke(tag, new object[] { hasLeader });
        }

        /// API cũ: trả về điểm bám lên host (gần giống LeaderEnd)
        private static XYZ GetTaggedLocalPointCompat(IndependentTag tag)
        {
            var m = tag.GetType().GetMethod("GetTaggedLocalPoint", BindingFlags.Instance | BindingFlags.Public);
            if (m != null && m.ReturnType == typeof(XYZ))
                return (XYZ)m.Invoke(tag, null);

            // Không có gì khả dụng
            return null;
        }
    }
}
