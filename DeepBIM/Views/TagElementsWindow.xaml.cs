using Autodesk.Revit.UI;
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
    /// Interaction logic for TagElementsWindow.xaml
    /// </summary>
    public partial class TagElementsWindow : Window
    {
        public TagElementsWindow(UIApplication uiapp, Document doc)
        {
            InitializeComponent();
        }
      
    }
}
