using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DeepBIM.Helpers;
using DeepBIM.Utils;

namespace DeepBIM.ViewModels
{
    public class AlignViewModel : INotifyPropertyChanged
    {
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;
        private readonly IAlignService _service;

        public AlignViewModel(UIDocument uiDoc, Document doc, IAlignService service)
        {
            _uiDoc = uiDoc; _doc = doc; _service = service;
            OkCommand = new RelayCommand(_ => OnOk());
            CancelCommand = new RelayCommand(_ => OnCancel());
        }

        private AlignType _selected = AlignType.Left;
        public AlignType SelectedAlignType
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        public RelayCommand OkCommand { get; }
        public RelayCommand CancelCommand { get; }
        public Window? HostWindow { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        private void OnOk() { if (HostWindow != null) { HostWindow.DialogResult = true; HostWindow.Close(); } }
        private void OnCancel() { if (HostWindow != null) { HostWindow.DialogResult = false; HostWindow.Close(); } }

        public Result RunAlign() => _service.AlignElements(_uiDoc, _doc, SelectedAlignType);
    }
}
