using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ManufacturingApp.Helpers;
using ManufacturingApp.Models;

namespace ManufacturingApp.Pages
{
    public partial class AdminPage : Page
    {
        private int _editingUserId = -1; // -1 = new user mode

        public AdminPage()
        {
            InitializeComponent();
            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            var dt = DatabaseHelper.GetRoles();
            CbRole.ItemsSource   = dt.DefaultView;
            CbRole.SelectedIndex = 0;
        }

        private void LoadUsers()
        {
            var users = DatabaseHelper.GetAllUsers();
            DgUsers.ItemsSource  = users;
            TbUserCount.Text     = $"Всего пользователей: {users.Count}";
        }

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(DgUsers.SelectedItem is User u)) return;

            _editingUserId       = u.UserID;
            TbFormTitle.Text     = $"✏ Редактирование: {u.Login}";
            TbEditLogin.Text     = u.Login;
            PbEditPassword.Password = "";
            ChkBlocked.IsChecked = u.IsBlocked;

            // Select role in ComboBox
            foreach (DataRowView row in CbRole.ItemsSource)
            {
                if ((int)row["RoleID"] == u.RoleID)
                {
                    CbRole.SelectedItem = row;
                    break;
                }
            }

            HideBanner();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            HideBanner();
            string login    = TbEditLogin.Text.Trim();
            string password = PbEditPassword.Password;

            if (string.IsNullOrEmpty(login))
            {
                ShowBanner("Введите логин.", isError: true);
                return;
            }

            if (CbRole.SelectedItem == null)
            {
                ShowBanner("Выберите роль.", isError: true);
                return;
            }

            int  roleId    = (int)((DataRowView)CbRole.SelectedItem)["RoleID"];
            bool isBlocked = ChkBlocked.IsChecked == true;

            if (_editingUserId < 0)
            {
                // ADD new user
                if (string.IsNullOrEmpty(password))
                {
                    ShowBanner("Введите пароль для нового пользователя.", isError: true);
                    return;
                }

                if (DatabaseHelper.LoginExists(login))
                {
                    ShowBanner($"Пользователь с логином «{login}» уже существует.", isError: true);
                    return;
                }

                string hash = PasswordHelper.Hash(password);
                DatabaseHelper.AddUser(login, hash, roleId);
                ShowBanner("Пользователь успешно добавлен.", isError: false);
            }
            else
            {
                // EDIT existing
                if (DatabaseHelper.LoginExists(login, _editingUserId))
                {
                    ShowBanner($"Логин «{login}» уже занят другим пользователем.", isError: true);
                    return;
                }

                string hash = string.IsNullOrEmpty(password) ? "" : PasswordHelper.Hash(password);
                DatabaseHelper.UpdateUser(_editingUserId, login, hash, roleId, isBlocked);
                ShowBanner("Данные пользователя обновлены.", isError: false);
            }

            LoadUsers();
            ClearForm();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            HideBanner();
        }

        private void ClearForm()
        {
            _editingUserId          = -1;
            TbFormTitle.Text        = "➕ Новый пользователь";
            TbEditLogin.Text        = "";
            PbEditPassword.Password = "";
            ChkBlocked.IsChecked    = false;
            DgUsers.SelectedItem    = null;
            if (CbRole.Items.Count > 0)
                CbRole.SelectedIndex = 0;
        }

        private void BtnShowOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dt = DatabaseHelper.GetOrderSummary();
                DgOrders.ItemsSource = dt.DefaultView;
                DgOrders.Visibility  = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowBanner("Ошибка загрузки заказов: " + ex.Message, isError: true);
            }
        }

        private void ShowBanner(string msg, bool isError)
        {
            TbMsg.Text             = (isError ? "⚠  " : "✔  ") + msg;
            BannerMsg.Background   = isError
                ? new SolidColorBrush(Color.FromRgb(253, 236, 234))
                : new SolidColorBrush(Color.FromRgb(232, 248, 240));
            TbMsg.Foreground       = isError
                ? new SolidColorBrush(Color.FromRgb(192,  57,  43))
                : new SolidColorBrush(Color.FromRgb( 39, 174,  96));
            BannerMsg.Visibility   = Visibility.Visible;
        }

        private void HideBanner()
        {
            BannerMsg.Visibility = Visibility.Collapsed;
        }
    }
}
