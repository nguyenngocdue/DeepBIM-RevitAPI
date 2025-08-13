using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.ViewModels
{
    public class Ex1SmartSelectionViewModel: INotifyPropertyChanged
    {
        private UIApplication _uiApp;
        private Document _doc;
        private UIDocument _uiDoc;

        public ObservableCollection<CategoryItem>  CategoryView { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public Ex1SmartSelectionViewModel(UIApplication uiapp, UIDocument uidoc, Document doc) 
        {
            _uiApp = uiapp;
            _uiDoc = uidoc;
            _doc = doc;
            CategoryView = new ObservableCollection<CategoryItem>();

            // Fetch categories and bind them
            LoadCategories();


        }

        // Method to load all categories from Revit
        private void LoadCategories()
        {
            // Clear the CategoryView to avoid duplicates
            CategoryView.Clear();

            // Get all categories from the Revit document
            Categories allCategories = _doc.Settings.Categories;
            foreach (Category category in allCategories) {
                //skip invalid categories
                if(category.Name != null)
                {
                    CategoryView.Add(new CategoryItem
                    {
                        Display = category.Name,  // Name of the category
                        IsChecked = false          // Initial checkbox state
                    });
                }
            }


            //Notify the UI that the collection has changed
            OnPropertyChanged(nameof(CategoryView));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
