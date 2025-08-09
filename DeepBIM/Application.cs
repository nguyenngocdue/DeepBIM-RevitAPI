using DeepBIM.Commands;
using Nice3point.Revit.Toolkit.External;
using System.Windows;

namespace DeepBIM
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "DeepBIM");

            panel.AddPushButton<StartupCommand>("Execute")
                .SetImage("/DeepBIM;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DeepBIM;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<SheetManagerCommand>("Execute")
              .SetImage("/DeepBIM;component/Resources/Icons/RibbonIcon16.png")
              .SetLargeImage("/DeepBIM;component/Resources/Icons/RibbonIcon32.png");
        }
    }
}