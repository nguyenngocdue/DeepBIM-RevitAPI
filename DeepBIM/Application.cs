using Autodesk.Revit.UI;
using DeepBIM.Components.Panels;
using DeepBIM.RibbonConfigs;
using Nice3point.Revit.Toolkit.External;
using System;
using System.Linq;
using System.Reflection;

namespace DeepBIM
{
    /// <summary>Revit add-in entry</summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            try
            {
                CreateRibbonFromJson();
                AlignPanel.CreateAlignPanel(this.Application);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("DeepBIM Ribbon", $"Failed to create ribbon: {ex.Message}");
            }
        }

        private void CreateRibbonFromJson()
        {
            // Đổi tên file/đường dẫn nếu bạn muốn
            var cfg = RibbonHelpers.LoadConfig("Resources/Jsons/DeepBIM.ribbon.json");

            foreach (var panelCfg in cfg.Panels ?? Enumerable.Empty<RibbonPanelConfig>())
            {
                // Nice3point Toolkit: Application.CreatePanel(panelName, tabName)
                var panel = Application.CreatePanel(panelCfg.Name, cfg.Tab);

                foreach (var item in panelCfg.Items ?? Enumerable.Empty<RibbonItemConfig>())
                {
                    var t = (item.Type ?? "").ToLowerInvariant();
                    if (t == "push") AddPush(panel, item);
                    else if (t == "pulldown") AddPulldown(panel, item);
                }
            }
        }

        private void AddPush(RibbonPanel panel, RibbonItemConfig item)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            var pbd = new PushButtonData(
                name: RibbonHelpers.SanitizeName(item.Label),
                text: item.Label,
                assemblyName: assemblyPath,
                className: item.Command
            )
            {
                ToolTip = item.Tooltip,
                LongDescription = item.LongDescription
            };

            var tipImg = RibbonHelpers.LoadImage(item.TooltipImage);
            if (tipImg != null) pbd.ToolTipImage = tipImg;

            var push = panel.AddItem(pbd) as PushButton;
            if (push != null)
                RibbonHelpers.SafeSetImages(push, item.SmallImage, item.LargeImage);
        }


        private void AddPulldown(RibbonPanel panel, RibbonItemConfig item)
        {
            var pdData = new PulldownButtonData(
                RibbonHelpers.SanitizeName(item.Label),
                item.Label
            );

            var pd = panel.AddItem(pdData) as PulldownButton;
            if (pd == null) return;

            RibbonHelpers.SafeSetImages(pd, item.SmallImage, item.LargeImage);

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            foreach (var child in item.Children?.Where(c => (c.Type ?? "").ToLowerInvariant() == "push")
                     ?? Enumerable.Empty<RibbonItemConfig>())
            {
                var childData = new PushButtonData(
                    RibbonHelpers.SanitizeName(child.Label),
                    child.Label,
                    assemblyPath,
                    child.Command
                )
                {
                    ToolTip = child.Tooltip,
                    LongDescription = child.LongDescription
                };

                var tipImg = RibbonHelpers.LoadImage(child.TooltipImage);
                if (tipImg != null) childData.ToolTipImage = tipImg;

                var childBtn = pd.AddPushButton(childData) as PushButton;
                if (childBtn != null)
                {
                    RibbonHelpers.SafeSetImages(childBtn, child.SmallImage, child.LargeImage);
                }
            }
        }

    }

}
