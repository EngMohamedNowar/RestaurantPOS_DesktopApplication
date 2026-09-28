using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PizzaPOS.Services;

namespace PizzaPOS.Views
{
    public partial class CashDialog : Window
    {
        // كل الـ logic في CashPad — هنا UI بس
        readonly CashPad _pad;

        public double PaidAmount => _pad.SettledAmount;

        public CashDialog(double total)
        {
            InitializeComponent();
            _pad = new CashPad(total);
            TotalTxt.Text = $"{total:F2} ج";
            UpdateDisplay();
        }

        // ── Button Click ────────────────────────────
        void Num_Click(object s, RoutedEventArgs e)
        {
            var tag = ((System.Windows.Controls.Button)s).Tag?.ToString() ?? "";
            HandleInput(tag);
        }

        // ── Keyboard ────────────────────────────────
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
                Key.OemPeriod or Key.Decimal => ".",
                Key.Back => "B",
                Key.Delete => "C",
                Key.Escape => "ESC",
                Key.Enter or Key.Return => "ENTER",
                _ => null
            };
            if (tag != null) HandleInput(tag);
        }

        // ── Handle Input ────────────────────────────
        void HandleInput(string tag)
        {
            switch (tag)
            {
                case "ENTER":
                    if (ConfirmBtn.IsEnabled)
                        Confirm_Click(this, new RoutedEventArgs());
                    return;

                case "ESC":
                    DialogResult = false; Close(); return;

                default:
                    _pad.Press(tag);
                    break;
            }

            UpdateDisplay();
        }

        // ── Update Display ───────────────────────────
        void UpdateDisplay()
        {
            PaidTxt.Text = _pad.HasValue ? $"{_pad.Paid:F2} ج" : "—";

            if (!_pad.HasValue)
            {
                ChangeTxt.Text = "—";
                ChangeTxt.Foreground = Brush("#06d6a0");
            }
            else if (_pad.Change >= 0)
            {
                ChangeTxt.Text = $"{_pad.Change:F2} ج";
                ChangeTxt.Foreground = Brush("#06d6a0");
            }
            else
            {
                ChangeTxt.Text = $"ناقص {Math.Abs(_pad.Change):F2} ج";
                ChangeTxt.Foreground = Brush("#E63946");
            }

            ConfirmBtn.IsEnabled = _pad.IsEnough;
        }

        static SolidColorBrush Brush(string hex) =>
            new SolidColorBrush((System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(hex));

        void Confirm_Click(object s, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        void Cancel_Click(object s, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
