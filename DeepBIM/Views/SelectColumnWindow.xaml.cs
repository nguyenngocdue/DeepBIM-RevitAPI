using System.Windows;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DeepBIM.ViewModels;

namespace DeepBIM.Views
{
    public partial class SelectColumnWindow : Window
    {
        private readonly UIDocument _uiDoc;
        private readonly SelectColumnViewModel _viewModel;

        public SelectColumnWindow(UIApplication app)
        {
            InitializeComponent();
            _uiDoc = app.ActiveUIDocument;

            _viewModel = new SelectColumnViewModel();
            DataContext = _viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var elements = _uiDoc.Selection.PickElementsByRectangle(new ColumnSelectionFilter());
                _viewModel.LoadSelectedColumns(elements);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // User cancelled selection
            }
        }
    }
}
