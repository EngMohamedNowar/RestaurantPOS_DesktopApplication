using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PizzaPOS.Services;
using PizzaPOS.ViewModels;

namespace PizzaPOS.Views
{
    public partial class MainWindow : Window
    {
        readonly MainViewModel _vm;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _vm = new MainViewModel();
                DataContext = _vm;
                StartClock();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "MainWindow Error");
                throw;
            }
        }
        void OpenTracking_Click(object s, RoutedEventArgs e)
        {
            new OrderTrackingWindow { Owner = this }.Show();
             //Show() مش ShowDialog() عشان تفضل مفتوحة في الخلفية
        }

        void StartClock()
        {
            // ── ساعة ──
            var clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clock.Tick += (_, _) => ClockTxt.Text = DateTime.Now.ToString("HH:mm:ss");
            clock.Start();
            ClockTxt.Text = DateTime.Now.ToString("HH:mm:ss");

            // ── تحديث الإحصائيات كل 30 ثانية ──
            var stats = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            stats.Tick += (_, _) => _vm.RefreshStats();
            stats.Start();
        }

        public void FocusSearch()
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        }

        void Window_Loaded(object s, RoutedEventArgs e)
        {
            SearchBox.Focus();
            CashierStatusTxt.Text =
                $"الكاشير: {SessionService.CurrentUser?.FullName ?? "—"}";

            bool isAdmin = SessionService.CurrentUser?.IsAdmin == true;
            BtnCategories.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnProducts.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnReports.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnSettings.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnProfit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnLoss.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnUsers.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnDrivers.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        void OpenUsers_Click(object s, RoutedEventArgs e)
        {
            if (SessionService.CurrentUser?.IsAdmin != true) return;
            new UsersWindow { Owner = this }.ShowDialog();
        }

        void OpenDrivers_Click(object s, RoutedEventArgs e)
        {
            if (SessionService.CurrentUser?.IsAdmin != true) return;
            new DriversWindow { Owner = this }.ShowDialog();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.F1: _vm.PayCashCmd.Execute(null); break;
                case Key.F2: _vm.PayCardCmd.Execute(null); break;
                case Key.F3: _vm.HoldCmd.Execute(null); break;
                case Key.F4: _vm.ClearCmd.Execute(null); break;
                case Key.F5: ShowHeldOrders(); break;
                case Key.Escape:
                    SearchBox.Clear(); SearchBox.Focus(); break;
                case Key.OemQuestion:
                case Key.Divide:
                    if (!SearchBox.IsFocused)
                    { e.Handled = true; SearchBox.Focus(); }
                    break;
            }
        }

        void ShowHeldOrders()
        {
            try
            {
                var dlg = new HeldOrdersDialog(_vm)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Topmost = true
                };
                dlg.Loaded += (_, _) =>
                {
                    dlg.Topmost = false;
                    dlg.Activate();
                    dlg.Focus();
                };
                dlg.ShowDialog();
                _vm.RefreshStats(); // ← تحديث بعد إغلاق المعلق
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "خطأ");
            }
        }

        void HeldOrders_Click(object s, RoutedEventArgs e)
            => ShowHeldOrders();

        void OpenCategories_Click(object s, RoutedEventArgs e)
        {
            if (SessionService.CurrentUser?.IsAdmin != true) return;
            new CategoriesWindow { Owner = this }.ShowDialog();
            _vm.LoadCategories();
        }

        void OpenProducts_Click(object s, RoutedEventArgs e)
        {
            if (SessionService.CurrentUser?.IsAdmin != true) return;
            new ProductsWindow { Owner = this }.ShowDialog();
            _vm.LoadProducts();
        }

        void OpenReports_Click(object s, RoutedEventArgs e)
        {
            if (SessionService.CurrentUser?.IsAdmin != true) return;
            new ReportsWindow { Owner = this }.ShowDialog();
            _vm.RefreshStats(); // ← تحديث بعد إغلاق التقارير
        }

        void OpenSettings_Click(object s, RoutedEventArgs e)
        {
            if (SessionService.CurrentUser?.IsAdmin != true) return;
            new SettingsWindow { Owner = this }.ShowDialog();
            _vm.ClearOrder();
            _vm.RefreshStats(); // ← تحديث بعد تغيير الإعدادات
        }

        void OpenCustomers_Click(object s, RoutedEventArgs e)
        {
            new CustomersWindow { Owner = this }.ShowDialog();
        }

        void OpenWarehouse_Click(object s, RoutedEventArgs e)
        {
            new WarehouseWindow { Owner = this }.ShowDialog();
        }

        void CloseShift_Click(object s, RoutedEventArgs e)
            => _vm.CloseShiftCmd.Execute(null);
    }
}