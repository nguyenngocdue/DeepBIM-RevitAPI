using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DeepBIM.Models;

namespace DeepBIM.Views.CustomControls
{
    /// <summary>
    /// Interaction logic for ComboBoxAC.xaml
    /// </summary>
    public partial class ComboBoxAC : UserControl
    {
        public ComboBox CustomCbControl;
        public ComboBoxAC()
        {
            InitializeComponent();
            DataContext = this;
        }
        public static readonly DependencyProperty ItemsSourceProperty =
           DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<CommandItem>), typeof(ComboBoxAC), new PropertyMetadata(null));
        public ObservableCollection<CommandItem> ItemsSource
        {
            get { return (ObservableCollection<CommandItem>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty DisplayMemberPathProperty =
           DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(ComboBoxAC), new PropertyMetadata(string.Empty));
        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register("SelectedValuePath", typeof(string), typeof(ComboBoxAC), new PropertyMetadata(string.Empty));
        public string SelectedValuePath
        {
            get { return (string)GetValue(SelectedValuePathProperty); }
            set { SetValue(SelectedValuePathProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
           DependencyProperty.Register("SelectedItem", typeof(CommandItem), typeof(ComboBoxAC), new PropertyMetadata(null));
        public CommandItem SelectedItem
        {
            get { return (CommandItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty FilterModeProperty =
             DependencyProperty.Register("FilterMode", typeof(AutoCompleteFilterMode), typeof(ComboBoxAC), new PropertyMetadata(AutoCompleteFilterMode.None));
        public AutoCompleteFilterMode FilterMode
        {
            get { return (AutoCompleteFilterMode)GetValue(FilterModeProperty); }
            set { SetValue(FilterModeProperty, value); }
        }

        public static readonly DependencyProperty ShowBorderProperty =
            DependencyProperty.Register("ShowBorder", typeof(bool), typeof(ComboBoxAC), new PropertyMetadata(false, OnShowBorderChanged));
        public bool ShowBorder
        {
            get { return (bool)GetValue(ShowBorderProperty); }
            set { SetValue(ShowBorderProperty, value); }
        }

        private static void OnShowBorderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ComboBoxAC;
            control?.UpdateBorder();
        }

        private void UpdateBorder()
        {
            if (ShowBorder)
            {
                border.BorderBrush = System.Windows.Media.Brushes.Black;
                border.BorderThickness = new Thickness(1);
            }
            else
            {
                border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
            }
        }

        public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent("SelectionChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ComboBoxAC));
        public static readonly DependencyProperty SelectionChangedProperty =
            DependencyProperty.Register("SelectionChanged", typeof(RoutedEventHandler), typeof(ComboBoxAC));

        public event RoutedEventHandler SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        private void comboBoxAutoComplete_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RoutedEventArgs newEventArgs = new(SelectionChangedEvent);
            RaiseEvent(newEventArgs);
        }

        private void comboBoxAutoComplete_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(comboBoxAutoComplete.Items);
            if (e == null)
            {
                view.Filter = null;
                return;
            }

            var searchText = string.Concat(comboBoxAutoComplete.Text, e.Text);
            FilterComboboxList(view, searchText);
        }

        private async void comboBoxAutoComplete_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //AddToCart();
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Back)
            {
                if (string.IsNullOrEmpty(comboBoxAutoComplete.Text))
                {
                    comboBoxAutoComplete.SelectedItem = null;
                    comboBoxAutoComplete.IsDropDownOpen = false;

                    return;
                }
                else
                {
                    //ICollectionView view = CollectionViewSource.GetDefaultView(comboBoxAutoComplete.Items);
                    //var searchText = comboBoxAutoComplete.Text.Substring(0, comboBoxAutoComplete.Text.Length - 1);
                    //FilterComboboxList(view, searchText);
                }
            }
            else if (e.Key == Key.Escape)
            {
                comboBoxAutoComplete.IsDropDownOpen = false;
                comboBoxAutoComplete.SelectedItem = null;
                return;
            }

            await Task.Factory.StartNew(() => { Thread.Sleep(300); });
            if (string.IsNullOrEmpty(comboBoxAutoComplete.Text))
                comboBoxAutoComplete_PreviewTextInput(null, null);
        }

        private void comboBoxAutoComplete_KeyUp(object sender, KeyEventArgs e)
        {
            if (string.IsNullOrEmpty(comboBoxAutoComplete.Text))
            {
            }
            if (e.Key == Key.Back)
            {
                if (string.IsNullOrEmpty(comboBoxAutoComplete.Text))
                {
                    comboBoxAutoComplete.SelectedItem = null;
                    comboBoxAutoComplete.IsDropDownOpen = false;
                    return;
                }
            }
        }

        private Storyboard fadeInStoryboard;
        private void comboBoxAutoComplete_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (comboBoxAutoComplete.IsDropDownOpen)
                fadeInStoryboard?.Begin(comboBoxAutoComplete);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            fadeInStoryboard = (Storyboard)FindResource("FadeInAnimation");
            CustomCbControl = comboBoxAutoComplete;
            UpdateBorder();
        }

        private void comboBoxAutoComplete_Loaded(object sender, RoutedEventArgs e)
        {
            CustomCbControl = comboBoxAutoComplete;
            CustomCbControl.Focus();
        }

        private void comboBoxAutoComplete_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ShowBorder)
            {
                border.BorderBrush = System.Windows.Media.Brushes.Yellow;
                border.BorderThickness = new Thickness(1);
            }
        }

        private void comboBoxAutoComplete_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ShowBorder)
            {
                border.BorderBrush = System.Windows.Media.Brushes.Black;
                border.BorderThickness = new Thickness(.5);
            }
        }

        private void FilterComboboxList(ICollectionView view, string searchText)
        {
            if (!string.IsNullOrEmpty(searchText))
                view.Filter = item =>
                {
                    if (item is CommandItem lineItem)
                    {
                        comboBoxAutoComplete.SelectedIndex = -1;
                        comboBoxAutoComplete.IsDropDownOpen = true;

                        if (FilterMode == AutoCompleteFilterMode.Contains || FilterMode == AutoCompleteFilterMode.None)
                            return (!string.IsNullOrEmpty(lineItem.DisplayName) && lineItem.DisplayName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) /*||
                               (!string.IsNullOrEmpty(lineItem.Description) && lineItem.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)*/;

                        if (FilterMode == AutoCompleteFilterMode.Equals)
                            return (!string.IsNullOrEmpty(lineItem.DisplayName) && lineItem.DisplayName.Equals(searchText)) /*||
                               (!string.IsNullOrEmpty(lineItem.Description) && lineItem.Description.Equals(searchText))*/;

                        if (FilterMode == AutoCompleteFilterMode.StartsWith)
                            return (!string.IsNullOrEmpty(lineItem.DisplayName) && lineItem.DisplayName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) /*||
                               (!string.IsNullOrEmpty(lineItem.Description) && lineItem.Description.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))*/;
                    }
                    return false;
                };
            else
            {
                view.Filter = null;
                comboBoxAutoComplete.IsDropDownOpen = false;
            }
        }
    }
}
