using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using DeepBIM.Models;

namespace DeepBIM.Services
{
    public static class RibbonCommandCatalog
    {
        public static readonly ObservableCollection<CommandItem> Items = new();

        public static void RefreshOnce()
        {
            if (Items.Count > 0) return;

            var adWinAsm = AppDomain.CurrentDomain.GetAssemblies()
                               .FirstOrDefault(a => string.Equals(a.GetName().Name, "AdWindows", StringComparison.OrdinalIgnoreCase));
            if (adWinAsm == null) return;

            var compMgrType = adWinAsm.GetType("Autodesk.Windows.ComponentManager");
            var ribbon = compMgrType?.GetProperty("Ribbon", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (ribbon == null) return;

            var tabs = ribbon.GetType().GetProperty("Tabs")?.GetValue(ribbon) as System.Collections.IEnumerable;
            if (tabs == null) return;

            foreach (var tab in tabs)
            {
                var tabName = tab.GetType().GetProperty("AutomationName")?.GetValue(tab) as string;
                var panels = tab.GetType().GetProperty("Panels")?.GetValue(tab) as System.Collections.IEnumerable;
                if (panels == null) continue;

                foreach (var panel in panels)
                {
                    var source = panel.GetType().GetProperty("Source")?.GetValue(panel);
                    var panelTitle = source?.GetType().GetProperty("Title")?.GetValue(source) as string;
                    var items = source?.GetType().GetProperty("Items")?.GetValue(source) as System.Collections.IEnumerable;
                    Collect(items, tabName, panelTitle);
                }
            }
        }

        private static void Collect(System.Collections.IEnumerable items, string tab, string panel)
        {
            if (items == null) return;

            foreach (var it in items)
            {
                var t = it.GetType();
                var itemsProp = t.GetProperty("Items");
                if (itemsProp != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(itemsProp.PropertyType))
                {
                    Collect((System.Collections.IEnumerable)itemsProp.GetValue(it), tab, panel);
                    continue;
                }

                if (t.FullName == "Autodesk.Windows.RibbonButton")
                {
                    var autoName = t.GetProperty("AutomationName")?.GetValue(it) as string;
                    var id = t.GetProperty("Id")?.GetValue(it) as string;
                    if (string.IsNullOrWhiteSpace(autoName) || string.IsNullOrWhiteSpace(id)) continue;

                    var display = autoName.Replace("\r\n", " ").Replace("\n", " ");
                    var desc = $"{tab} → {panel} → {display}";
                    var cmdId = RevitCommandId.LookupCommandId(id);
                    if (cmdId == null) continue;

                    if (!Items.Any(x => x.RevitCommandId?.Id == cmdId.Id))
                        Items.Add(new CommandItem { DisplayName = display, Description = desc, RevitCommandId = cmdId });
                }
            }
        }
    }

}
