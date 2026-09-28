using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            Width = 620;
            Height = 560;
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
                Text = "البرنامج مقفل. انسخ مفتاح التفعيل من المورّد والصقه هنا.",
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
                _txtStatus?.Text = "تم نسخ الـ Hardware ID!";
                _txtStatus?.Foreground = UiHelper.B("#4CAF50");
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

            stack.Children.Add(UiHelper.FieldLabel("مفتاح التفعيل (الصقه كامل):"));

            // المفتاح بقى ~395 محرف (توقيع RSA-2048)، فالتنسيق التلقائي
            // القديم كان بيقصّه على 16 محرف. بقى Box متعدّد الأسطر
            // بيقبل اللصق وبيعمل wrap — والمستخدم بينسخ بينسخ.
            _txtKey = new TextBox
            {
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                Foreground = Brushes.White,
                Background = UiHelper.B("#2A2A3C"),
                BorderBrush = UiHelper.B("#444466"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                MaxLength = 600,
                MinHeight = 80,
                MaxHeight = 110,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 10),
                FlowDirection = FlowDirection.LeftToRight
            };
            _txtKey.KeyDown += (_, e) =>
            {
                // Enter يفعّل،Shift+Enter لسطر جديد (الـ Box متعدد الأسطر)
                bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (e.Key == System.Windows.Input.Key.Enter && !shift)
                {
                    e.Handled = true;
                    Activate_Click();
                }
            };
            stack.Children.Add(_txtKey);

            var hint = new TextBlock
            {
                Text = "المفتاح طويل ويتبعت بالنسخ/اللصق — مفيش داعي تكتبه بإيدك.",
                FontSize = 11,
                Foreground = UiHelper.B("#888888"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(hint);

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
            // اللصق بيجيب مسافات وسطور جديدة — التطبيع جوه
            // LicenseSignature.TrySplitKey/Ungroup بيتعامل معاها.
            string key = _txtKey.Text.Trim();

            if (key.Length == 0)
            {
                _txtStatus.Text = "الصق مفتاح التفعيل الأول.";
                _txtStatus.Foreground = UiHelper.B("#E63946");
                return;
            }

            // الـ expiry جوه المفتاح الموقّع — البرنامج ما بيقدرش يقرّر
            // المدة. النداء القديم كان Activate(formatted, 30) يعني
            // "30 يوم من هنا" والبرنامج هو اللي كتبها، فكانت قابلة للتزوير.
            if (LicenseManager.Activate(key))
            {
                _shutdownTimer.Stop();
                _txtStatus.Text = "تم التفعيل بنجاح!";
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
                _txtStatus.Text = "مفتاح غير صالح، أو مش مخصوص للجهاز ده.";
                _txtStatus.Foreground = UiHelper.B("#E63946");
                _txtKey.Focus();
            }
        }
    }
}
