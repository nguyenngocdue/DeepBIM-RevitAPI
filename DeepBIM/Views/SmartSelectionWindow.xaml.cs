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
    public partial class SmartSelectionWindow : Window
    {
        // Assume a ViewModel for data binding
        private SmartSelectionViewModel viewModel;
        public Dispatcher Dispatcher { get; }
        public SmartSelectionWindow(Document doc, UIDocument uiDoc)
        {
            InitializeComponent();
            viewModel = new SmartSelectionViewModel(doc, uiDoc);
            DataContext = viewModel;
        }



        private Document _doc;

       
       

        private void LoadCategories()
        {
           
        }

      

   


        private void TreeViewItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
          
        }

        public void UpdateSelectedCount()
        {
          
        }

     

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void SearchSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void ResetAllCategoryVisibility()
        {
           
        }



        private void SavePreset_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
          
        }

        

        private void SelectedViews_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void LevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
        }

        private void AddPropertyFilter_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void RemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
           
        }

     

        private void SelectElementsInRevit(List<Category> categories)
        {
           
        }

    }
  
}