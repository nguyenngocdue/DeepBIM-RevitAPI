using Autodesk.Revit.UI;
using DeepBIM.Commands;
using Nice3point.Revit.Toolkit.External;
using System.Windows;
using System.Windows.Media.Imaging;

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

            //panel.AddPushButton<SheetManagerCommand>("Execute")
            //  .SetImage("/DeepBIM;component/Resources/Icons/sheet-manage26.png")
            //  .SetLargeImage("/DeepBIM;component/Resources/Icons/sheet-manage32.png");


            // Tạo dữ liệu cho pulldown
            PulldownButtonData pulldownBtnData = new PulldownButtonData(
                "SheetPulldown",
                "Sheet Tools");

            // Tạo pulldown button
            PulldownButton pulldownBtn = panel.AddItem(pulldownBtnData) as PulldownButton;
            // Set icon
            pulldownBtn.Image = new BitmapImage(new Uri("pack://application:,,,/DeepBIM;component/Resources/Icons/sheet-manage26.png"));
            pulldownBtn.LargeImage = new BitmapImage(new Uri("pack://application:,,,/DeepBIM;component/Resources/Icons/sheet-manage32.png"));

            String assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // Creation command
            PushButtonData creationData = new PushButtonData(
                "SheetCreation",
                "Creation",
                assemblyPath,
                "DeepBIM.Commands.SheetCreationCommand"
            )
            {
                ToolTip = "Create new sheets from template or Excel mapping.",
                LongDescription = "Creates new sheets in batch with numbering rules, title block selection, and optional view placement.",
                ToolTipImage = new BitmapImage(new Uri(
                    "pack://application:,,,/DeepBIM;component/Resources/Icons/plus32.png"
                ))
            };

            PushButton btnCreation = pulldownBtn.AddPushButton(creationData) as PushButton;
            btnCreation.Image = new BitmapImage(new Uri(
                "pack://application:,,,/DeepBIM;component/Resources/Icons/plus16.png"
            ));
            btnCreation.LargeImage = new BitmapImage(new Uri(
                "pack://application:,,,/DeepBIM;component/Resources/Icons/plus32.png"
            ));

            // Manage command
            PushButtonData manageData = new PushButtonData(
                "SheetManage",
                "Manage",
                assemblyPath,
                "DeepBIM.Commands.SheetManagerCommand"
            )
            {
                ToolTip = "Bulk rename or renumber existing sheets.",
                LongDescription = "Search/replace, add prefix/suffix, and reorder sheet numbers safely with preview before applying.",
                ToolTipImage = new BitmapImage(new Uri(
                    "pack://application:,,,/DeepBIM;component/Resources/Icons/management32.png"
                ))
            };

            PushButton btnManage = pulldownBtn.AddPushButton(manageData) as PushButton;
            btnManage.Image = new BitmapImage(new Uri(
                "pack://application:,,,/DeepBIM;component/Resources/Icons/management16.png"
            ));
            btnManage.LargeImage = new BitmapImage(new Uri(
                "pack://application:,,,/DeepBIM;component/Resources/Icons/management32.png"
            ));





        }
    }
}