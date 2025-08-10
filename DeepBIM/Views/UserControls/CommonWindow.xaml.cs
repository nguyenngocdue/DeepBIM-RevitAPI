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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeepBIM.Views.UserControls
{
    /// <summary>
    /// Interaction logic for CommonWindow.xaml
    /// </summary>
    [ContentProperty("Content")]
    public partial class CommonWindow : UserControl
    {
        public CommonWindow()
        {
            InitializeComponent();
        }
        // Logo
        public ImageSource Logo
        {
            get { return (ImageSource)GetValue(LogoProperty); }
            set { SetValue(LogoProperty, value); }
        }
        public static readonly DependencyProperty LogoProperty =
            DependencyProperty.Register(nameof(Logo), typeof(ImageSource), typeof(CommonWindow), new PropertyMetadata(null));

        // Title
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(CommonWindow), new PropertyMetadata("Untitled"));

        // FunctionCommand
        public ICommand FunctionCommand
        {
            get { return (ICommand)GetValue(FunctionCommandProperty); }
            set { SetValue(FunctionCommandProperty, value); }
        }
        public static readonly DependencyProperty FunctionCommandProperty =
            DependencyProperty.Register(nameof(FunctionCommand), typeof(ICommand), typeof(CommonWindow), new PropertyMetadata(null));

        // FunctionText
        public string FunctionText
        {
            get { return (string)GetValue(FunctionTextProperty); }
            set { SetValue(FunctionTextProperty, value); }
        }
        public static readonly DependencyProperty FunctionTextProperty =
            DependencyProperty.Register(nameof(FunctionText), typeof(string), typeof(CommonWindow), new PropertyMetadata("Thực hiện"));

        // Content – để chứa nội dung con
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(CommonWindow), new PropertyMetadata(null));
    }
}
