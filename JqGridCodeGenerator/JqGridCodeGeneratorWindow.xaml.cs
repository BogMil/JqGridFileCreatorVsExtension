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

            CenterWindowOnScreen();
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }
}
