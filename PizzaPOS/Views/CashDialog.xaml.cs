using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PizzaPOS.Views
{
    public partial class CashDialog : Window
    {
        readonly double _total;
        string _input = "";

        public double PaidAmount { get; private set; }

        public CashDialog(double total)
        {
            InitializeComponent();
            _total = total;
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

                case "B":
                    if (_input.Length > 0)
                        _input = _input[..^1];
                    break;

                case "C":
                    _input = "";
                    break;

                case "Q50": _input = "50"; break;
                case "Q100": _input = "100"; break;
                case "Q200": _input = "200"; break;
                case "Q500": _input = "500"; break;
                case "EXACT": _input = _total.ToString("F2"); break;

                case ".":
                    if (!_input.Contains('.'))
                        _input += _input == "" ? "0." : ".";
                    break;

                default:
                    // منع أكثر من خانتين عشريتين
                    if (_input.Contains('.'))
                    {
                        int dot = _input.IndexOf('.');
                        if (_input.Length - dot >= 3) break;
                    }
                    // منع صفر في البداية
                    if (_input == "0") _input = tag;
                    else _input += tag;
                    break;
            }

            UpdateDisplay();
        }

        // ── Update Display ───────────────────────────
        void UpdateDisplay()
        {
            bool hasPaid = _input != "" && _input != "0.";
            double paid = double.TryParse(_input, out var v) ? v : 0;
            double change = Math.Round(paid - _total, 2);

            PaidTxt.Text = hasPaid ? $"{paid:F2} ج" : "—";

            if (!hasPaid)
            {
                ChangeTxt.Text = "—";
                ChangeTxt.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString("#06d6a0"));
            }
            else if (change >= 0)
            {
                ChangeTxt.Text = $"{change:F2} ج";
                ChangeTxt.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString("#06d6a0"));
            }
            else
            {
                ChangeTxt.Text = $"ناقص {Math.Abs(change):F2} ج";
                ChangeTxt.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString("#E63946"));
            }

            ConfirmBtn.IsEnabled = hasPaid && Math.Round(paid - _total, 2) >= 0;
        }

        void Confirm_Click(object s, RoutedEventArgs e)
        {
            PaidAmount = double.TryParse(_input, out var v) ? v : _total;
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