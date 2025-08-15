using System.Collections.Generic;

namespace DeepBIM.RibbonConfigs
{
    public class RibbonConfig
    {
        public string Tab { get; set; }
        public List<RibbonPanelConfig> Panels { get; set; } = new();
    }

    public class RibbonPanelConfig
    {
        public string Name { get; set; }
        public List<RibbonItemConfig> Items { get; set; } = new();
    }

    public class RibbonItemConfig
    {
        // "push" | "pulldown"
        public string Type { get; set; }
        public string Label { get; set; }

        // Dùng cho type="push"
        public string Command { get; set; }

        // Đường dẫn resource tương đối trong DLL, ví dụ:
        // "Resources/Icons/RibbonIcon16.png"
        public string SmallImage { get; set; }
        public string LargeImage { get; set; }

        public string Tooltip { get; set; }
        public string LongDescription { get; set; }
        public string TooltipImage { get; set; }

        // Cho type="pulldown"
        public List<RibbonItemConfig> Children { get; set; } = new();
    }
}
