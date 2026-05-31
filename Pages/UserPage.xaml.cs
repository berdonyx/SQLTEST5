using System;
using System.Data;
using System.Linq;
using System.Windows.Controls;
using ManufacturingApp.Helpers;
using ManufacturingApp.Models;

namespace ManufacturingApp.Pages
{
    public partial class UserPage : Page
    {
        public UserPage()
        {
            InitializeComponent();
            var user = AppSession.CurrentUser;
            TbWelcome.Text  = $"Добро пожаловать, {user?.Login ?? "Пользователь"}!";
            TbRoleBadge.Text = user?.RoleName ?? "—";
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                var dt = DatabaseHelper.GetOrderSummary();
                DgOrders.ItemsSource = dt.DefaultView;

                TbOrderCount.Text   = dt.Rows.Count.ToString();
                decimal total       = 0;
                foreach (DataRow row in dt.Rows)
                {
                    if (row["TotalOrderCost"] != DBNull.Value)
                        total += Convert.ToDecimal(row["TotalOrderCost"]);
                }
                TbTotalRevenue.Text = total.ToString("N2");
            }
            catch (Exception ex)
            {
                TbOrderCount.Text   = "Ошибка";
                TbTotalRevenue.Text = ex.Message;
            }
        }

        private void BtnRefresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadOrders();
        }
    }
}
