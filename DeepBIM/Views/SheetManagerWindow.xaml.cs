using Autodesk.Revit.UI;
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
    /// Interaction logic for SheetManagerWindow.xaml
    /// </summary>
    public partial class SheetManagerWindow : Window
    {
        private readonly UIDocument _uiDoc;
        private readonly SheetManagerViewModel _viewModel;
        public SheetManagerWindow(UIApplication app)
        {
            InitializeComponent();
            _uiDoc = app.ActiveUIDocument;
            _viewModel = new SheetManagerViewModel(_uiDoc.Document);
            DataContext = _viewModel;
        }
   

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter &&
            //    DataContext is DeepBIM.ViewModels.SheetManagerViewModel vm &&
            //    vm.SearchCommand.CanExecute(null))
            //{
            //    vm.SearchCommand.Execute(null);
            //}
        }
    }
}
