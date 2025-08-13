using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeepBIM.ViewModels
{
    public class CategoryItem: INotifyPropertyChanged
    {
        private bool _isChecked;
        private string _display;

        public ElementId CategoryId { get; set; }

        public string Display
        {
            get => _display;
            set
            {
                if (_display != value)
                {
                    _display = value;
                    OnPropertyChanged(nameof(Display));
                }
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}