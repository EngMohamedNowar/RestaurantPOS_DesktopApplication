// Views/LoginWindow.xaml.cs
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PizzaPOS.Services;

namespace PizzaPOS.Views
{
    public partial class LoginWindow : Window
    {
        readonly UserService _svc = new();
        string _pin = "";

        public LoginWindow()
        {
            InitializeComponent();
            ShopNameText.Text = SessionService.ShopName;
            UsernameBox.Focus();
        }

        void Window_KeyDown(object s, KeyEventArgs e)
        {
            string? tag = e.Key switch
            {
                Key.D0 or Key.NumPad0 => "0",
                Key.D1 or Key.NumPad1 => "1",
                Key.D2 or Key.NumPad2 => "2",
                Key.D3 or Key.NumPad3 => "3",
                Key.D4 or Key.NumPad4 => "4",
                Key.D5 or Key.NumPad5 => "5",
                Key.D6 or Key.NumPad6 => "6",
                Key.D7 or Key.NumPad7 => "7",
                Key.D8 or Key.NumPad8 => "8",
                Key.D9 or Key.NumPad9 => "9",
                Key.Back => "B",
                Key.Delete => "C",
                Key.Enter or Key.Return => "ENTER",
                _ => null
            };

            if (tag == null) return;

            if (tag == "ENTER")
            {
                if (UsernameBox.IsFocused)
                {
                    // لو username فاضي → خليه يكمل
                    if (string.IsNullOrWhiteSpace(UsernameBox.Text))
                    {
                        ErrText.Text = "أدخل اسم المستخدم";
                        e.Handled = true;
                        return;
                    }
                    // انتقل للـ PIN — امسح focus من username
                    Keyboard.ClearFocus();
                    ErrText.Text = "";
                    e.Handled = true;
                    return;
                }
                // مش على username → حاول تدخل
                Login_Click(s, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            // لو focus على username → اتركه يكتب طبيعي
            if (UsernameBox.IsFocused &&
                tag != "B" && tag != "C") return;

            switch (tag)
            {
                case "C": _pin = ""; break;
                case "B":
                    if (_pin.Length > 0) _pin = _pin[..^1];
                    break;
                default:
                    if (_pin.Length < 4) _pin += tag;
                    break;
            }

            UpdatePinDisplay();
            e.Handled = true;

            // لو اكتمل الـ PIN تلقائياً → دخول
            if (_pin.Length == 4) Login_Click(s, new RoutedEventArgs());
        }
        void PinBtn_Click(object s, RoutedEventArgs e)
        {
            string tag = ((Button)s).Tag.ToString()!;
            switch (tag)
            {
                case "C": _pin = ""; break;
                case "B":
                    if (_pin.Length > 0) _pin = _pin[..^1];
                    break;
                default:
                    if (_pin.Length < 4) _pin += tag;
                    break;
            }
            UpdatePinDisplay();
            if (_pin.Length == 4) Login_Click(s, e);
        }

        void UpdatePinDisplay()
        {
            var filled = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FF6B35"));
            var empty = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1e2d4a"));

            PinDots.Visibility = _pin.Length > 0
                ? Visibility.Visible : Visibility.Collapsed;
            PinPlaceholder.Visibility = _pin.Length == 0
                ? Visibility.Visible : Visibility.Collapsed;

            Dot1.Fill = _pin.Length >= 1 ? filled : empty;
            Dot2.Fill = _pin.Length >= 2 ? filled : empty;
            Dot3.Fill = _pin.Length >= 3 ? filled : empty;
            Dot4.Fill = _pin.Length >= 4 ? filled : empty;
        }

        void Login_Click(object s, RoutedEventArgs e)
        {
            ErrText.Text = "";
            string username = UsernameBox.Text.Trim();

            if (string.IsNullOrEmpty(username) || _pin.Length < 4)
            {
                ErrText.Text = "أدخل اسم المستخدم والـ PIN كاملاً";
                return;
            }

            var user = _svc.Login(username, _pin);
            if (user == null)
            {
                ErrText.Text = "❌ بيانات خاطئة";
                _pin = "";
                UpdatePinDisplay();
                return;
            }

            SessionService.CurrentUser = user;

            var shiftSvc = new ShiftService();
            var shift = shiftSvc.GetOpenShift(user.Id);

            if (shift != null)
            {
                SessionService.CurrentShift = shift;
            }
            else
            {
                var dlg = new OpenShiftDialog();
                bool? result = dlg.ShowDialog();
                SessionService.CurrentShift = result == true
                    ? shiftSvc.OpenShift(user.Id, dlg.OpeningCash)
                    : shiftSvc.OpenShift(user.Id, 0);
            }

            DialogResult = true;
        }

        void OpenPortfolio(object s, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://engmohamednowar.github.io/portfolio/",
                UseShellExecute = true
            });
        }
    }
}