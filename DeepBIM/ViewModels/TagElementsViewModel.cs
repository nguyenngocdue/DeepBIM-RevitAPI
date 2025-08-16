using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing; // Pipe
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DeepBIM.ViewModels
{
    public enum TagOption
    {
        OnlyElevation = 1,
        OnlyDiameter = 2,
        ElevMinusDia = 3,
        ElevDiaByLength = 4,    // Pipe Length = ngưỡng lọc (mm)
        StartEndElevation = 5,
        StartEndElevationMinusDia = 6
    }

    public class TagFamilyType
    {
        public ElementId SymbolId { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public class TagElementsViewModel : INotifyPropertyChanged
    {
        // ===== Inline ExternalEvent handler (không cần file riêng) =====
        private class ActionHandler : IExternalEventHandler
        {
            public Action<UIApplication> Action;
            public void Execute(UIApplication app)
            {
                var a = Action; Action = null;
                a?.Invoke(app);
            }
            public string GetName() => "DeepBIM - TagElements ExternalEvent";
        }

        private class PipeOnlySelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element e)
                => e?.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves;
            public bool AllowReference(Reference r, XYZ p) => false;
        }

        // ===== Fields =====
        private readonly Document _doc;
        private readonly UIDocument _uidoc;
        private readonly ICollection<ElementId> _selectedIds;

        private readonly ActionHandler _handler;
        private readonly ExternalEvent _extEvent;

        private const double DefaultNudgeUpMm = 150;
        private const double DefaultPairGapMm = 200;
        private const double DefaultAvoidRadiusMm = 200;

        // Form đóng từ VM
        public event Action RequestClose;

        public TagElementsViewModel(Document doc, UIDocument uidoc, ICollection<ElementId> selectedIds)
        {
            _doc = doc;
            _uidoc = uidoc;
            _selectedIds = selectedIds ?? new List<ElementId>();

            // ExternalEvent nội bộ
            _handler = new ActionHandler();
            _extEvent = ExternalEvent.Create(_handler);

            TagFamilies = new ObservableCollection<TagFamilyType>();
            LoadPipeTagTypes();

            SelectedElevationTagFamily = TagFamilies.FirstOrDefault();
            SelectedDiameterTagFamily = TagFamilies.FirstOrDefault();
            SelectedOption = TagOption.OnlyElevation;

            // Pipe Length mặc định (mm) — có thể để trống để không lọc
            Length1 = "2000"; Length2 = ""; Length3 = ""; Length4 = ""; Length5 = ""; Length6 = "";

            NudgeUpMm = DefaultNudgeUpMm;
            PairGapMm = DefaultPairGapMm;
            AvoidRadiusMm = DefaultAvoidRadiusMm;

            // Quan trọng: gọi qua ExternalEvent và ĐÓNG FORM ngay
            CreateCommand = new RelayCommand(_ => RaiseCreateAndClose(), _ => CanCreate());
        }

        // ===== Bindings =====
        public ObservableCollection<TagFamilyType> TagFamilies { get; }
        private TagFamilyType _selElev, _selDia;
        public TagFamilyType SelectedElevationTagFamily
        { get => _selElev; set { _selElev = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        public TagFamilyType SelectedDiameterTagFamily
        { get => _selDia; set { _selDia = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private TagOption _selectedOption;
        public TagOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value; OnPropertyChanged();
                OnPropertyChanged(nameof(IsLen1Enabled));
                OnPropertyChanged(nameof(IsLen2Enabled));
                OnPropertyChanged(nameof(IsLen3Enabled));
                OnPropertyChanged(nameof(IsLen4Enabled));
                OnPropertyChanged(nameof(IsLen5Enabled));
                OnPropertyChanged(nameof(IsLen6Enabled));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Pipe Length (mm) – chỉ ô ứng với option hiện tại mới enable
        private string _l1, _l2, _l3, _l4, _l5, _l6;
        public string Length1 { get => _l1; set { _l1 = value; OnPropertyChanged(); } }
        public string Length2 { get => _l2; set { _l2 = value; OnPropertyChanged(); } }
        public string Length3 { get => _l3; set { _l3 = value; OnPropertyChanged(); } }
        public string Length4 { get => _l4; set { _l4 = value; OnPropertyChanged(); } }
        public string Length5 { get => _l5; set { _l5 = value; OnPropertyChanged(); } }
        public string Length6 { get => _l6; set { _l6 = value; OnPropertyChanged(); } }

        public bool IsLen1Enabled => SelectedOption == TagOption.OnlyElevation;
        public bool IsLen2Enabled => SelectedOption == TagOption.OnlyDiameter;
        public bool IsLen3Enabled => SelectedOption == TagOption.ElevMinusDia;
        public bool IsLen4Enabled => SelectedOption == TagOption.ElevDiaByLength;
        public bool IsLen5Enabled => SelectedOption == TagOption.StartEndElevation;
        public bool IsLen6Enabled => SelectedOption == TagOption.StartEndElevationMinusDia;

        // tham số bố trí (mm)
        public double NudgeUpMm { get; set; }
        public double PairGapMm { get; set; }
        public double AvoidRadiusMm { get; set; }

        public ICommand CreateCommand { get; }

        // ===== Command impl =====
        private void RaiseCreateAndClose()
        {
            // Thực thi CreateTags trên thread Revit
            _handler.Action = (UIApplication app) =>
            {
                try { CreateTags(); }
                catch (Exception ex) { TaskDialog.Show("Pipe Tags Tool", ex.Message); }
            };
            _extEvent.Raise();      // Giao việc cho Revit
            RequestClose?.Invoke(); // ĐÓNG FORM NGAY
        }

        // ===== Load Tag types =====
        private void LoadPipeTagTypes()
        {
            var syms = new List<FamilySymbol>();

            syms.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeTags)
                .Cast<FamilySymbol>());

            // mở thêm nếu cần
            syms.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeFittingTags)
                .Cast<FamilySymbol>());
            syms.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeAccessoryTags)
                .Cast<FamilySymbol>());

            var items = syms.Select(fs => new TagFamilyType
            {
                SymbolId = fs.Id,
                Name = $"{fs.Family?.Name ?? "Family"} : {fs.Name}"
            })
            .GroupBy(x => x.Name).Select(g => g.First())
            .OrderBy(x => x.Name);

            TagFamilies.Clear();
            foreach (var it in items) TagFamilies.Add(it);

            if (TagFamilies.Count == 0)
                TagFamilies.Add(new TagFamilyType { SymbolId = ElementId.InvalidElementId, Name = "(No pipe tag families found)" });
        }

        private bool CanCreate()
        {
            if (SelectedElevationTagFamily == null || SelectedDiameterTagFamily == null) return false;
            if (SelectedElevationTagFamily.SymbolId == ElementId.InvalidElementId) return false;
            if (SelectedDiameterTagFamily.SymbolId == ElementId.InvalidElementId) return false;
            return TryGetActiveMinLengthFeet(out _); // ô đang active phải hợp lệ (hoặc trống)
        }

        // ===== Main run (được gọi trong ExternalEvent) =====
        private void CreateTags()
        {
            var view = _doc.ActiveView;
            if (view == null) { TaskDialog.Show("Pipe Tags Tool", "No active view."); return; }

            // 1) Ống từ selection; nếu trống → yêu cầu quét chọn
            var pipes = GetPipesFromSelection();
            if (pipes.Count == 0)
            {
                pipes = AskUserPickPipes();
                if (pipes.Count == 0) { TaskDialog.Show("Pipe Tags Tool", "No pipes selected."); return; }
                _uidoc.Selection.SetElementIds(pipes.Select(p => p.Id).ToHashSet());
            }

            // 2) Lọc theo Pipe Length ngưỡng (mm)
            TryGetActiveMinLengthFeet(out double minLenFt);
            if (minLenFt > 0) pipes = pipes.Where(p => GetCurveLength(p) >= minLenFt).ToList();

            if (pipes.Count == 0)
            {
                TaskDialog.Show("Pipe Tags Tool", "No pipes meet the minimum length.");
                return;
            }

            // 3) Chuẩn bị type & tham số
            var elevType = _doc.GetElement(SelectedElevationTagFamily.SymbolId) as ElementType;
            var diaType = _doc.GetElement(SelectedDiameterTagFamily.SymbolId) as ElementType;
            ActivateIfNeeded(elevType);
            ActivateIfNeeded(diaType);

            double nudgeUp = UnitUtils.ConvertToInternalUnits(NudgeUpMm, UnitTypeId.Millimeters);
            double pairGap = UnitUtils.ConvertToInternalUnits(PairGapMm, UnitTypeId.Millimeters);
            double avoidR = UnitUtils.ConvertToInternalUnits(AvoidRadiusMm, UnitTypeId.Millimeters);

            var up = view.UpDirection;
            var right = view.RightDirection;

            int created = 0, skipped = 0;

            using (var tx = new Transaction(_doc, "Place Pipe Tags"))
            {
                tx.Start();

                foreach (var e in pipes)
                {
                    var curve = (e.Location as LocationCurve)?.Curve;
                    if (curve == null) { skipped++; continue; }

                    switch (SelectedOption)
                    {
                        case TagOption.OnlyElevation:
                            {
                                var head = curve.Evaluate(0.5, true) + up * nudgeUp;
                                var tag = PlaceTagIfClear(e, view, head, elevType, avoidR);
                                if (tag != null) { RotateTagToPipe(tag, curve, view); created++; } else skipped++;
                                break;
                            }
                        case TagOption.OnlyDiameter:
                            {
                                var head = curve.Evaluate(0.5, true) + up * nudgeUp;
                                var tag = PlaceTagIfClear(e, view, head, diaType, avoidR);
                                if (tag != null) { RotateTagToPipe(tag, curve, view); created++; } else skipped++;
                                break;
                            }
                        case TagOption.ElevMinusDia:
                            {
                                var baseHead = curve.Evaluate(0.5, true) + up * nudgeUp;
                                var h1 = baseHead - right * (pairGap * 0.5);
                                var h2 = baseHead + right * (pairGap * 0.5);

                                var t1 = PlaceTagIfClear(e, view, h1, elevType, avoidR);
                                if (t1 != null) { RotateTagToPipe(t1, curve, view); created++; } else skipped++;

                                var t2 = PlaceTagIfClear(e, view, h2, diaType, avoidR);
                                if (t2 != null) { RotateTagToPipe(t2, curve, view); created++; } else skipped++;
                                break;
                            }
                        case TagOption.ElevDiaByLength:
                            {
                                var baseHead = curve.Evaluate(0.5, true) + up * nudgeUp;
                                var h1 = baseHead - right * (pairGap * 0.5);
                                var h2 = baseHead + right * (pairGap * 0.5);

                                var t1 = PlaceTagIfClear(e, view, h1, elevType, avoidR);
                                if (t1 != null) { RotateTagToPipe(t1, curve, view); created++; } else skipped++;

                                var t2 = PlaceTagIfClear(e, view, h2, diaType, avoidR);
                                if (t2 != null) { RotateTagToPipe(t2, curve, view); created++; } else skipped++;
                                break;
                            }
                        case TagOption.StartEndElevation:
                            {
                                var p0 = curve.GetEndPoint(0) + up * nudgeUp;
                                var p1 = curve.GetEndPoint(1) + up * nudgeUp;

                                var t0 = PlaceTagIfClear(e, view, p0, elevType, avoidR);
                                if (t0 != null) { RotateTagToPipe(t0, curve, view); created++; } else skipped++;

                                var t1 = PlaceTagIfClear(e, view, p1, elevType, avoidR);
                                if (t1 != null) { RotateTagToPipe(t1, curve, view); created++; } else skipped++;
                                break;
                            }
                        case TagOption.StartEndElevationMinusDia:
                            {
                                var p0 = curve.GetEndPoint(0) + up * nudgeUp;
                                var p1 = curve.GetEndPoint(1) + up * nudgeUp;

                                var t0 = PlaceTagIfClear(e, view, p0 - right * (pairGap * 0.5), elevType, avoidR);
                                if (t0 != null) { RotateTagToPipe(t0, curve, view); created++; } else skipped++;

                                var t1 = PlaceTagIfClear(e, view, p0 + right * (pairGap * 0.5), diaType, avoidR);
                                if (t1 != null) { RotateTagToPipe(t1, curve, view); created++; } else skipped++;

                                var t2 = PlaceTagIfClear(e, view, p1 - right * (pairGap * 0.5), elevType, avoidR);
                                if (t2 != null) { RotateTagToPipe(t2, curve, view); created++; } else skipped++;

                                var t3 = PlaceTagIfClear(e, view, p1 + right * (pairGap * 0.5), diaType, avoidR);
                                if (t3 != null) { RotateTagToPipe(t3, curve, view); created++; } else skipped++;
                                break;
                            }
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("Pipe Tags Tool", $"Created: {created}\nSkipped: {skipped}");
        }

        // ===== Helpers =====
        private IList<Element> GetPipesFromSelection()
        {
            var ids = _uidoc.Selection.GetElementIds();
            if (ids == null || ids.Count == 0) return new List<Element>();
            return ids.Select(id => _doc.GetElement(id))
                      .Where(e => e?.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves)
                      .ToList();
        }

        private IList<Element> AskUserPickPipes()
        {
            try
            {
                var refs = _uidoc.Selection.PickObjects(
                    ObjectType.Element,
                    new PipeOnlySelectionFilter(),
                    "Select pipes to tag (drag a window or click multiple). Press Finish/Esc when done.");
                return refs.Select(r => _doc.GetElement(r.ElementId)).ToList();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new List<Element>(); // Esc
            }
        }

        private static double GetCurveLength(Element e)
            => (e.Location as LocationCurve)?.Curve?.Length ?? 0.0;

        private static void ActivateIfNeeded(ElementType t)
        { if (t is FamilySymbol fs && !fs.IsActive) fs.Activate(); }

        // parse mm -> feet (trống => 0)
        private static bool TryParseMmToFeetOptional(string text, out double feet)
        {
            feet = 0;
            if (string.IsNullOrWhiteSpace(text)) return true;
            var s = text.Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var mm) || mm < 0)
                return false;
            feet = UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
            return true;
        }

        private bool TryGetActiveMinLengthFeet(out double feet)
        {
            string s = SelectedOption switch
            {
                TagOption.OnlyElevation => Length1,
                TagOption.OnlyDiameter => Length2,
                TagOption.ElevMinusDia => Length3,
                TagOption.ElevDiaByLength => Length4,
                TagOption.StartEndElevation => Length5,
                TagOption.StartEndElevationMinusDia => Length6,
                _ => null
            };
            return TryParseMmToFeetOptional(s, out feet);
        }

        // ==== XOAY TAG THEO PHƯƠNG ỐNG ====
        private static XYZ ProjectOnPlane(XYZ v, XYZ n) => v - n.Multiply(v.DotProduct(n));
        private static XYZ TangentAtMid(Curve c)
        {
            if (c is Line ln) return (ln.GetEndPoint(1) - ln.GetEndPoint(0)).Normalize();
            var der = c.ComputeDerivatives(0.5, true);
            return der.BasisX.Normalize();
        }

        private void RotateTagToPipe(IndependentTag tag, Curve curve, View view)
        {
            try
            {
                var n = view.ViewDirection;                                   // pháp tuyến mặt phẳng view
                var a = ProjectOnPlane(view.RightDirection, n).Normalize();    // trục chuẩn (text ngang)
                var d = ProjectOnPlane(TangentAtMid(curve), n);
                if (d == null || d.GetLength() < 1e-9) return;
                d = d.Normalize();

                // góc có dấu từ 'a' -> 'd' quanh 'n'
                double angle = Math.Atan2(n.DotProduct(a.CrossProduct(d)), a.DotProduct(d));

                // quay quanh pháp tuyến view đi qua head
                var axis = Line.CreateUnbound(tag.TagHeadPosition, n);
                ElementTransformUtils.RotateElement(_doc, tag.Id, axis, angle);
            }
            catch { /* bỏ qua nếu version không hỗ trợ xoay tag */ }
        }

        // né chồng tag trong bán kính avoid
        private IndependentTag FindNearbyTag(View view, XYZ head, double radiusFeet)
        {
            var r = radiusFeet;
            var bbMin = new XYZ(head.X - r, head.Y - r, head.Z - r);
            var bbMax = new XYZ(head.X + r, head.Y + r, head.Z + r);
            var outline = new Outline(bbMin, bbMax);
            var bbFilter = new BoundingBoxIntersectsFilter(outline);

            return new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(IndependentTag))
                .WherePasses(bbFilter)
                .Cast<IndependentTag>()
                .FirstOrDefault();
        }

        private IndependentTag PlaceTagIfClear(Element target, View view, XYZ head, ElementType tagType, double avoidRadiusFeet)
        {
            if (FindNearbyTag(view, head, avoidRadiusFeet) != null) return null;
            return PlaceTag(target, view, head, tagType);
        }

        private IndependentTag PlaceTag(Element target, View view, XYZ head, ElementType tagType)
        {
            try
            {
                var tag = IndependentTag.Create(
                    _doc,
                    view.Id,
                    new Reference(target),
                    false, // leader
                    TagMode.TM_ADDBY_CATEGORY,
                    TagOrientation.Horizontal,
                    head);

                if (tag == null) return null;

                // đổi type bằng ELEM_TYPE_PARAM (tương thích nhiều version)
                if (tagType != null && tag.GetTypeId() != tagType.Id)
                {
                    var typeParam = tag.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM);
                    if (typeParam != null && !typeParam.IsReadOnly)
                        typeParam.Set(tagType.Id);
                }
                return tag;
            }
            catch { return null; }
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    // ===== Extension nhỏ =====
    static class XyzExt
    {
        public static bool IsZeroLength(this XYZ v, double eps = 1e-9)
            => v == null || v.GetLength() < eps;
    }
}
