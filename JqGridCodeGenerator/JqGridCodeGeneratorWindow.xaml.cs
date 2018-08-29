using JqGridCodeGenerator.View.Pages;
using System.Windows;

namespace JqGridCodeGenerator
{
    /// <summary>
    /// Interaction logic for JqGridCodeGeneratorWindow.xaml
    /// </summary>
    public partial class JqGridCodeGeneratorWindow : Window
    {
        public static JqGridCodeGeneratorWindow Instance { get; set; }

        public JqGridCodeGeneratorWindow()
        {
            Instance = this;
            InitializeComponent();
            PageFrame.NavigationService.Navigate(new ChooseDataBasePage());
        }
    }
}
