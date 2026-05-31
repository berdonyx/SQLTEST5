using System.Windows;
using ManufacturingApp.Pages;
using ManufacturingApp.Models;

namespace ManufacturingApp
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            MainFrame.Navigate(new LoginPage());
        }

        public void ShowNavBar(string userName, string role)
        {
            TbCurrentUser.Text = $"👤 {userName}  |  {role}";
            NavPanel.Visibility = Visibility.Visible;
        }

        public void HideNavBar()
        {
            NavPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            AppSession.CurrentUser = null;
            HideNavBar();
            MainFrame.Navigate(new LoginPage());
        }
    }
}
