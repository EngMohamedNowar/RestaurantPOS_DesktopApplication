// Views/CloseShiftDialog.cs
// ← مش partial — مش محتاج XAML
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.Services;

namespace PizzaPOS.Views
{
    public class CloseShiftDialog : Window
    {
        readonly Shift _shift;
        readonly AppDbContext _db = new();
        readonly ShiftService _svc = new();

        TextBox _closeCashBox = null!;
        TextBlock _diffLabel = null!;
        TextBlock _diffTxt = null!;
        Border _diffBorder = null!;

        double _expectedCash;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public CloseShiftDialog(Shift shift)
        {
            _shift = shift;
            Title = "⏹ إغلاق الشفت";
            Width = 500;
            SizeToContent = SizeToContent.Height;
            Background = B("#0a0a14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;
            BuildUI();
        }

        void BuildUI()
        {
            // جلب بيانات الشفت
            var (sales, orders, avg) = _db.GetShiftSummary(_shift.Id);
            var (cashSales, cardSales) = _db.GetShiftPayMethods(_shift.Id);
            _expectedCash = _shift.OpeningCash + cashSales;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // stats
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // cash input
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // diff
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // buttons

            // ══ Header ══
            var header = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#E63946"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = B("#E63946"),
                CornerRadius = new CornerRadius(10),
                Width = 42,
                Height = 42,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "⏹",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إغلاق الشفت",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"الكاشير: {SessionService.CurrentUser?.FullName ?? "—"}  •  فتح: {_shift.OpenedAt}",
                FontSize = 11,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Stats ══
            var statsGrid = new Grid { Margin = new Thickness(16, 14, 16, 0) };
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            void AddCard(Border card, int row, int col)
            {
                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                statsGrid.Children.Add(card);
            }

            AddCard(StatCard("📦 عدد الأوردرات", $"{orders} أوردر", "#a78bfa"), 0, 0);
            AddCard(StatCard("📊 متوسط الأوردر", $"{avg:F2} ج", "#a78bfa"), 0, 2);
            AddCard(StatCard("💵 مبيعات كاش", $"{cashSales:F2} ج", "#ffd166"), 1, 0);
            AddCard(StatCard("💳 مبيعات فيزا", $"{cardSales:F2} ج", "#ffd166"), 1, 2);
            AddCard(StatCard("💰 كاش افتتاحي", $"{_shift.OpeningCash:F2} ج", "#b0c4de"), 2, 0);
            AddCard(StatCard("💵 كاش متوقع", $"{_expectedCash:F2} ج", "#06d6a0"), 2, 2);

            // Total — Full Width
            var totalCard = new Border
            {
                Background = B("#0d1f14"),
                CornerRadius = new CornerRadius(10),
                BorderBrush = B("#1e3a2e"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 8, 0, 0)
            };
            totalCard.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.3
            };
            var totalInner = new Grid();
            totalInner.ColumnDefinitions.Add(new ColumnDefinition());
            totalInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var totalLbl = new TextBlock
            {
                Text = "💰 إجمالي المبيعات",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = B("#5aaa80"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var totalVal = new TextBlock
            {
                Text = $"{sales:F2} ج",
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = B("#06d6a0")
            };
            totalVal.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            Grid.SetColumn(totalLbl, 0);
            Grid.SetColumn(totalVal, 1);
            totalInner.Children.Add(totalLbl);
            totalInner.Children.Add(totalVal);
            totalCard.Child = totalInner;
            Grid.SetRow(totalCard, 3);
            Grid.SetColumnSpan(totalCard, 3);
            statsGrid.Children.Add(totalCard);

            Grid.SetRow(statsGrid, 1);
            root.Children.Add(statsGrid);

            // ══ Cash Input ══
            var cashSection = new Border
            {
                Background = B("#0d1220"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 14, 0, 0)
            };
            var cashSp = new StackPanel();
            cashSp.Children.Add(new TextBlock
            {
                Text = "💵 أدخل الكاش الفعلي في الدرج",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = B("#eef0f2"),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition());
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _closeCashBox = new TextBox
            {
                Background = B("#0f1526"),
                Foreground = B("#ffffff"),
                BorderBrush = B("#2a3f6a"),
                BorderThickness = new Thickness(1, 1, 0, 1),
                Padding = new Thickness(12, 10, 12, 10),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                CaretBrush = B("#06d6a0"),
                Text = _expectedCash.ToString("F2")
            };
            _closeCashBox.TextChanged += (_, _) => UpdateDiff();

            // زرار إعادة التعيين
            var resetBtn = new Border
            {
                Background = B("#1e2d4a"),
                CornerRadius = new CornerRadius(0, 8, 8, 0),
                Padding = new Thickness(12, 0, 12, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "إعادة تعيين للمتوقع"
            };
            resetBtn.Child = new TextBlock
            {
                Text = "↺",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = B("#06d6a0"),
                VerticalAlignment = VerticalAlignment.Center
            };
            resetBtn.MouseLeftButtonDown += (_, _) =>
            {
                _closeCashBox.Text = _expectedCash.ToString("F2");
                _closeCashBox.Focus();
            };

            Grid.SetColumn(_closeCashBox, 0);
            Grid.SetColumn(resetBtn, 1);
            inputGrid.Children.Add(_closeCashBox);
            inputGrid.Children.Add(resetBtn);
            cashSp.Children.Add(inputGrid);
            cashSp.Children.Add(new TextBlock
            {
                Text = $"⟵ الكاش المتوقع: {_expectedCash:F2} ج  (افتتاحي + كاش)",
                FontSize = 11,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 6, 0, 0)
            });
            cashSection.Child = cashSp;
            Grid.SetRow(cashSection, 2);
            root.Children.Add(cashSection);

            // ══ Diff ══
            _diffBorder = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(16, 12, 16, 0)
            };
            var diffInner = new Grid();
            diffInner.ColumnDefinitions.Add(new ColumnDefinition());
            diffInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _diffLabel = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            _diffTxt = new TextBlock
            {
                FontSize = 20,
                FontWeight = FontWeights.Black,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(_diffLabel, 0);
            Grid.SetColumn(_diffTxt, 1);
            diffInner.Children.Add(_diffLabel);
            diffInner.Children.Add(_diffTxt);
            _diffBorder.Child = diffInner;
            Grid.SetRow(_diffBorder, 3);
            root.Children.Add(_diffBorder);

            UpdateDiff();

            // ══ Buttons ══
            var btnBar = new Border
            {
                Background = B("#0d1220"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 12, 16, 16),
                Margin = new Thickness(0, 12, 0, 0)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = MakeBtn("إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            var closeBtn = MakeBtn("⏹ إغلاق الشفت", "#E63946",
                System.Windows.Media.Brushes.White, DoClose);
            closeBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#E63946"),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.5
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(closeBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(closeBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 4);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) =>
            {
                _closeCashBox.Focus();
                _closeCashBox.SelectAll();
            };
        }

        // ── Update Diff ──────────────────────────────
        void UpdateDiff()
        {
            double actual = double.TryParse(_closeCashBox.Text, out var v) ? v : 0;
            double diff = actual - _expectedCash;

            if (Math.Abs(diff) < 0.01)
            {
                _diffBorder.Background = B("#0d1f14");
                _diffBorder.BorderBrush = B("#1e3a2e");
                _diffLabel.Text = "✅ الكاش مطابق تماماً";
                _diffLabel.Foreground = B("#06d6a0");
                _diffTxt.Text = "0.00 ج";
                _diffTxt.Foreground = B("#06d6a0");
            }
            else if (diff > 0)
            {
                _diffBorder.Background = B("#0d1f0a");
                _diffBorder.BorderBrush = B("#1e4a1e");
                _diffLabel.Text = "⬆️ فائض في الكاش";
                _diffLabel.Foreground = B("#06d6a0");
                _diffTxt.Text = $"+{diff:F2} ج";
                _diffTxt.Foreground = B("#06d6a0");
            }
            else
            {
                _diffBorder.Background = B("#1a0a0a");
                _diffBorder.BorderBrush = B("#3a1e1e");
                _diffLabel.Text = "⬇️ عجز في الكاش";
                _diffLabel.Foreground = B("#E63946");
                _diffTxt.Text = $"{diff:F2} ج";
                _diffTxt.Foreground = B("#E63946");
            }
        }

        // ── Do Close ─────────────────────────────────
        void DoClose()
        {
            double closing = double.TryParse(_closeCashBox.Text, out var v) ? v : 0;

            if (closing < 0)
            {
                MessageBox.Show("الكاش الفعلي لا يمكن أن يكون سالباً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var (sales, orders, avg) = _db.GetShiftSummary(_shift.Id);
            var (cashSales, cardSales) = _db.GetShiftPayMethods(_shift.Id);
            double diff = closing - _expectedCash;
            string diffStr = Math.Abs(diff) < 0.01 ? "✅ مطابق"
                                       : diff > 0 ? $"⬆️ فائض +{diff:F2} ج"
                                                  : $"⬇️ عجز {diff:F2} ج";

            _svc.CloseShift(_shift.Id, closing);
            SessionService.CurrentShift = null;

            string msg =
                $"══════════════════════════════\n" +
                $"       تقرير إغلاق الشفت\n" +
                $"══════════════════════════════\n\n" +
                $"👤 الكاشير:        {SessionService.CurrentUser?.FullName ?? "—"}\n" +
                $"🕐 وقت الفتح:     {_shift.OpenedAt}\n" +
                $"🕑 وقت الإغلاق:   {DateTime.Now:HH:mm}\n\n" +
                $"──────────────────────────────\n" +
                $"📦 عدد الأوردرات:    {orders}\n" +
                $"💰 إجمالي المبيعات:  {sales:F2} ج\n" +
                $"💵 مبيعات كاش:       {cashSales:F2} ج\n" +
                $"💳 مبيعات فيزا:      {cardSales:F2} ج\n" +
                $"📊 متوسط الأوردر:    {avg:F2} ج\n\n" +
                $"──────────────────────────────\n" +
                $"💵 كاش افتتاحي:     {_shift.OpeningCash:F2} ج\n" +
                $"💵 كاش متوقع:       {_expectedCash:F2} ج\n" +
                $"💵 كاش فعلي:        {closing:F2} ج\n" +
                $"⚖️  الفرق:          {diffStr}\n";

            MessageBox.Show(msg, "✅ تم إغلاق الشفت",
                MessageBoxButton.OK,
                diff < -0.01 ? MessageBoxImage.Warning : MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        // ── Stat Card ────────────────────────────────
        Border StatCard(string label, string value, string color)
        {
            var b = new Border
            {
                Background = B("#0d1525"),
                CornerRadius = new CornerRadius(10),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = B("#4a6080"),
                FontWeight = FontWeights.Bold
            });
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 15,
                FontWeight = FontWeights.Black,
                Foreground = B(color),
                Margin = new Thickness(0, 3, 0, 0)
            });
            b.Child = sp;
            return b;
        }

        // ── Button ───────────────────────────────────
        Button MakeBtn(string text, string bg,
                       System.Windows.Media.Brush fg, Action click)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            f.SetBinding(Border.PaddingProperty,
                new Binding("Padding")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderBrushProperty,
                new Binding("BorderBrush")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderThicknessProperty,
                new Binding("BorderThickness")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp);
            var tpl = new ControlTemplate(typeof(Button)) { VisualTree = f };

            var btn = new Button
            {
                Content = text,
                Background = B(bg),
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 13, 0, 13),
                FontWeight = FontWeights.Black,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }
    }
}