using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PizzaPOS.Helpers;
using PizzaPOS.Services;

namespace PizzaPOS.Views
{
    public class LicenseActivationWindow : Window
    {
        readonly TextBox _txtKey;
        readonly TextBlock _txtHwid;
        readonly TextBlock _txtStatus;
        readonly TextBlock _txtTimer;
        readonly DispatcherTimer _shutdownTimer;
        int _secondsLeft = 300;

        public LicenseActivationWindow()
        {
            Title = "تفعيل الترخيص";
            Width = 520;
            Height = 460;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;

            var border = new Border
            {
                Background = UiHelper.B("#1E1E2E"),
                CornerRadius = new CornerRadius(12),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(30)
            };

            var stack = new StackPanel { Margin = new Thickness(10) };

            var title = new TextBlock
            {
                Text = "تفعيل الترخيص",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#E63946"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stack.Children.Add(title);

            var subtitle = new TextBlock
            {
                Text = "البرنامج مقفل. أدخل مفتاح التفعيل للاستمرار.",
                FontSize = 13,
                Foreground = UiHelper.B("#B0B0B0"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(subtitle);

            stack.Children.Add(UiHelper.FieldLabel("Hardware ID:"));

            var hwidRow = new DockPanel { Margin = new Thickness(0, 0, 0, 15) };

            var btnCopyHwid = UiHelper.MakeBtn("نسخ", "#444466", Brushes.White, () =>
            {
                Clipboard.SetText(HardwareId.GetShortId());
                _txtStatus.Text = "تم نسخ الـ Hardware ID!";
                _txtStatus.Foreground = UiHelper.B("#4CAF50");
            }, 8, 11, 0, "6");
            btnCopyHwid.Width = 50;
            btnCopyHwid.HorizontalAlignment = HorizontalAlignment.Right;
            btnCopyHwid.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(btnCopyHwid, Dock.Right);

            _txtHwid = new TextBlock
            {
                Text = HardwareId.GetShortId(),
                FontSize = 14,
                Foreground = UiHelper.B("#F5C518"),
                FontFamily = new FontFamily("Consolas"),
                Background = UiHelper.B("#2A2A3C"),
                Padding = new Thickness(10, 8, 10, 8),
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            hwidRow.Children.Add(btnCopyHwid);
            hwidRow.Children.Add(_txtHwid);
            stack.Children.Add(hwidRow);

            stack.Children.Add(UiHelper.FieldLabel("مفتاح التفعيل:"));
            _txtKey = new TextBox
            {
                FontSize = 18,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = UiHelper.B("#2A2A3C"),
                BorderBrush = UiHelper.B("#444466"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                MaxLength = 19,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15),
                CharacterCasing = CharacterCasing.Upper,
                FlowDirection = FlowDirection.LeftToRight
            };
            _txtKey.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) Activate_Click(); };
            _txtKey.TextChanged += (_, __) =>
            {
                string raw = _txtKey.Text.Replace("-", "").Replace(" ", "");
                if (raw.Length > 16) raw = raw.Substring(0, 16);

                string formatted = "";
                for (int i = 0; i < raw.Length && i < 16; i++)
                {
                    if (i > 0 && i % 4 == 0) formatted += "-";
                    formatted += raw[i];
                }

                if (_txtKey.Text != formatted)
                {
                    int pos = _txtKey.SelectionStart;
                    _txtKey.Text = formatted;
                    _txtKey.SelectionStart = Math.Min(pos, formatted.Length);
                }
            };
            stack.Children.Add(_txtKey);

            var btnActivate = UiHelper.MakeBtn("تفعيل", "#E63946", Brushes.White, () => Activate_Click(), 14);
            btnActivate.Width = 200;
            btnActivate.HorizontalAlignment = HorizontalAlignment.Center;
            btnActivate.Margin = new Thickness(0, 0, 0, 10);
            stack.Children.Add(btnActivate);

            var btnExit = UiHelper.MakeBtn("إغلاق", "#555577", Brushes.White, () => Application.Current.Shutdown(), 12);
            btnExit.Width = 120;
            btnExit.HorizontalAlignment = HorizontalAlignment.Center;
            btnExit.Margin = new Thickness(0, 0, 0, 10);
            stack.Children.Add(btnExit);

            _txtStatus = new TextBlock
            {
                FontSize = 12,
                Foreground = UiHelper.B("#B0B0B0"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 5)
            };
            stack.Children.Add(_txtStatus);

            _txtTimer = new TextBlock
            {
                FontSize = 11,
                Foreground = UiHelper.B("#888888"),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };
            stack.Children.Add(_txtTimer);

            border.Child = stack;
            Content = border;

            _shutdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _shutdownTimer.Tick += (_, __) =>
            {
                _secondsLeft--;
                int min = _secondsLeft / 60;
                int sec = _secondsLeft % 60;
                _txtTimer.Text = $"البرنامج سيتوقف بعد {min:D2}:{sec:D2}";

                if (_secondsLeft <= 0)
                {
                    _shutdownTimer.Stop();
                    MessageBox.Show("انتهت المدة المسموحة. البرنامج سيتوقف.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                }
            };
            _shutdownTimer.Start();
        }

        void Activate_Click()
        {
            string key = _txtKey.Text.Trim().Replace("-", "").Replace(" ", "");

            if (key.Length != 16)
            {
                _txtStatus.Text = "Invalid key!";
                _txtStatus.Foreground = UiHelper.B("#E63946");
                return;
            }

            string formatted = $"{key.Substring(0, 4)}-{key.Substring(4, 4)}-{key.Substring(8, 4)}-{key.Substring(12, 4)}";

            if (LicenseManager.Activate(formatted, 30))
            {
                _shutdownTimer.Stop();
                _txtStatus.Text = "Activated successfully!";
                _txtStatus.Foreground = UiHelper.B("#4CAF50");

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (_, __) =>
                {
                    timer.Stop();
                    DialogResult = true;
                    Close();
                };
                timer.Start();
            }
            else
            {
                _txtStatus.Text = "Invalid key or doesn't match this device!";
                _txtStatus.Foreground = UiHelper.B("#E63946");
                _txtKey.SelectAll();
                _txtKey.Focus();
            }
        }
    }
}
