using System;
using System.Linq;
using System.Windows;
using System.Text;                 // مهم
using PizzaPOS.Data;
using PizzaPOS.Services;
using PizzaPOS.Views;

namespace PizzaPOS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            base.OnStartup(e);
            try
            {
                if (!LicenseManager.IsActivated())
                {
                    var licenseWin = new LicenseActivationWindow();
                    if (licenseWin.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                DatabaseHelper.Initialize();

                var login = new LoginWindow();
                if (login.ShowDialog() != true) { Shutdown(); return; }

                var inv = new InventoryService();
                var low = inv.GetLowStock();

                if (low.Count > 0)
                {
                    string items = string.Join("\n", low.Select(i => $"• {i.Name}: {i.Stock} {i.Unit}"));

                    MessageBox.Show(
                        $"⚠ تنبيه: {low.Count} صنف مخزونهم منخفض!\n\n{items}",
                        "تنبيه المخزون",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                var main = new MainWindow();
                MainWindow = main;
                main.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "خطأ في التشغيل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }
    }
}