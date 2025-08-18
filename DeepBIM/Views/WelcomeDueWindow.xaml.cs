using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DeepBIM.Utils;
using DeepBIM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DeepBIM.Views
{
    /// <summary>
    /// Interaction logic for WelcomeDueWindow.xaml
    /// </summary>
    public partial class WelcomeDueWindow : Window
    {
        private readonly WelcomeDueViewModel viewModel;
        public WelcomeDueWindow(UIApplication uiapp, UIDocument uidoc,Document doc)
        {
            SystemLoadManager.LoadMaterialDesign(@"B:\C# Tool Revit\DeepBIM\DeepBIM\bin\Debug R25");
            SystemLoadManager.LoadBehaviors(@"B:\C# Tool Revit\DeepBIM\DeepBIM\bin\Debug R25");
            InitializeComponent();
            viewModel = new WelcomeDueViewModel(uiapp, uidoc, doc);
            this.DataContext = viewModel;
        }
    }
}
