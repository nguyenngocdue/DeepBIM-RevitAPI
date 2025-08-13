using Autodesk.Revit.UI;
using DeepBIM.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;



namespace DeepBIM.Views
{
    public partial class Ex1SmartSelectionWindow : Window
    {
        // Assume a ViewModel for data binding
        private Ex1SmartSelectionViewModel viewModel;
        public Ex1SmartSelectionWindow(UIApplication uiapp, UIDocument uidoc, Document doc)
        {
            InitializeComponent();
            viewModel = new Ex1SmartSelectionViewModel(uiapp, uidoc, doc);
            DataContext = viewModel;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
  
}