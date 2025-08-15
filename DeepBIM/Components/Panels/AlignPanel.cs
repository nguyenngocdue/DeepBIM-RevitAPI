using Autodesk.Revit.UI;
using DeepBIM.RibbonConfigs;
using System.Reflection;

namespace DeepBIM.Components.Panels
{
    public static class AlignPanel
    {
        public static void CreateAlignPanel(UIControlledApplication app, string tabName = "DeepBIM")
        {
            // đảm bảo có tab
            try { app.CreateRibbonTab(tabName); } catch { }
            const string ICON_FOLDER = "Resources/Icons";
            const string ICON_ONLY = "\u2022"; // zero-width space -> hiện icon, ẩn text

            // tạo panel trong tab
            RibbonPanel panel = app.CreateRibbonPanel(tabName, "Alignment");

            string asm = Assembly.GetExecutingAssembly().Location;

            // ===== C1: Left | Center | Right =====
            var pbdLeft = new PushButtonData("AlignLeftBtn", "Align Left", asm, "DeepBIM.Commands.AlignLeftCommand");
            pbdLeft.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/align-left-16.png");
            pbdLeft.ToolTip = "Align Left";

            var pbdCenter = new PushButtonData("AlignCenterBtn", "Align Center", asm, "DeepBIM.Commands.AlignCenterXCommand");
            pbdCenter.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/align-center-16.png");
            pbdCenter.ToolTip = "Align Center";

            var pbdRight = new PushButtonData("AlignRightBtn", "Align Right", asm, "DeepBIM.Commands.AlignRightCommand");
            pbdRight.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/align-right-16.png");
            pbdRight.ToolTip = "Align Right";

            panel.AddStackedItems(pbdLeft, pbdCenter, pbdRight);

            //// ===== C2: Top | Middle | Bottom =====
            var pbdTop = new PushButtonData("AlignTopBtn", "Align Top", asm, "DeepBIM.Commands.AlignTopCommand");
            pbdTop.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/align-top-16.png");
            pbdTop.ToolTip = "Align Top";

            var pbdMiddle = new PushButtonData("AlignMiddleBtn", "Align Middle", asm, "DeepBIM.Commands.AlignCenterYCommand");
            pbdMiddle.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/align-middle-16.png");
            pbdMiddle.ToolTip = "Align Middle";

            var pbdBottom = new PushButtonData("AlignBottomBtn", "Align Bottom", asm, "DeepBIM.Commands.AlignBottomCommand");
            pbdBottom.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/align-bottom-16.png");
            pbdBottom.ToolTip = "Align Bottom";
            panel.AddStackedItems(pbdTop, pbdMiddle, pbdBottom);


            //// ===== C3: Distribute Horizontally | Distribute Vertically | Arrange Tags =====
            var pbdDistH = new PushButtonData("DistHorizBtn", "Distribute Horizontal", asm, "DeepBIM.Commands.DistributeHorizontallyCommand");
            pbdDistH.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/distribute-horizontal-16.png");
            pbdDistH.ToolTip = "Distribute Horizontally";

            var pbdDistV = new PushButtonData("DistVertBtn", "Distribute Vertical", asm, "DeepBIM.Commands.DistributeVerticallyCommand");
            pbdDistV.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/distribute-vertical-16.png");
            pbdDistV.ToolTip = "Distribute Vertically";

            ////var pbdArrangeTags = new PushButtonData("ArrangeTagsBtn", "Arrange\nTags", asm, "DeepBIM.Commands.ArrangeTagsCommand");
            ////pbdArrangeTags.Image = RibbonHelpers.LoadImage("Resources/Icons/arrange-tags16.png");
            ////pbdArrangeTags.ToolTip = "Arrange Tags";
            panel.AddStackedItems(pbdDistH, pbdDistV);

            //// ===== C4: Untangle Vertically | Untangle Horizontally =====
            var pbdUntangleV = new PushButtonData("UntangleVertBtn", "Untangle Vertical", asm, "DeepBIM.Commands.UntangleVerticallyCommand");
            pbdUntangleV.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/untangle-vertical-16.png");
            pbdUntangleV.ToolTip = "Untangle Vertically";

            var pbdUntangleH = new PushButtonData("UntangleHorizBtn", "Untangle Horizontal", asm, "DeepBIM.Commands.UntangleHorizontallyCommand");
            pbdUntangleH.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/untangle-hori-24.png");
            pbdUntangleH.ToolTip = "Untangle Horizontally";
            panel.AddStackedItems(pbdUntangleV, pbdUntangleH);

            // ===== C5 =====
            var pbdSetting = new PushButtonData(
                "SettingBtn",
                "Settings",
                asm,
                "DeepBIM.Commands.SettingsCommand"
            );
            pbdSetting.Image = RibbonHelpers.LoadImage($"{ICON_FOLDER}/setting-align-16.png");
            pbdSetting.ToolTip = "Open Settings";

            // Chỉ 1 nút thì dùng AddItem
            panel.AddItem(pbdSetting);
        }
    }
}
