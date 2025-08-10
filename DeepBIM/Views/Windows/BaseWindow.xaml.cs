using System.Windows;

namespace DeepBIM.Views.Windows
{
    public partial class BaseWindow : Window
    {
        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register(nameof(WindowTitle), typeof(string),
                typeof(BaseWindow), new PropertyMetadata(string.Empty));

        public string WindowTitle
        {
            get => (string)GetValue(WindowTitleProperty);
            set => SetValue(WindowTitleProperty, value);
        }

        public BaseWindow()
        {
            InitializeComponent();
        }
    }
}
