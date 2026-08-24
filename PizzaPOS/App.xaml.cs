using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Text;                 // مهم
using PizzaPOS.Data;
using PizzaPOS.Services;
using PizzaPOS.Views;

namespace PizzaPOS
{
    // يحوّل SolidColorBrush → Color عشان نستخدمه في DropShadowEffect.Color
    // (الـ Freezable مش بيرث DataContext من الشجرة، فبنربط على الـ Brush مباشرة)
    public class BrushToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush) return brush.Color;
            if (value is Color color) return color;
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ScrollBar الرأسي لازم IsDirectionReversed=True عشان اتجاه السكرول ما ينعكسش
    public class OrientationToReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Orientation o && o == Orientation.Vertical;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

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

                BackupService.CreateBackup();

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