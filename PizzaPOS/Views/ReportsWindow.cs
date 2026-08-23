// Views/ReportsWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using PizzaPOS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PizzaPOS.Views
{
    public class ReportsWindow : Window
    {
        readonly AppDbContext _db = new();
        DatePicker _fromPicker = null!;
        DatePicker _toPicker = null!;
        DataGrid _dgDaily = null!;
        DataGrid _dgProd = null!;
        DataGrid _dgLoss = null!;
        TextBlock _totalLossTxt = null!;
        TextBlock _totalSalesTxt = null!;
        TextBlock _totalProfitTxt = null!;
        TextBlock _totalOrdersTxt = null!;

        public ReportsWindow()
        {
            Title = "📊 التقارير";
            Width = 1060; Height = 700;
            MinWidth = 900;
            Background = UiHelper.B("#070b14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            BuildUI();
        }

        // ══════════════════════════════════════════════════
        //  Calendar Styles — يجب استدعاؤها أولاً في BuildUI
        // ══════════════════════════════════════════════════
        void RegisterCalendarStyles()
        {
            // ── Calendar container ──────────────────────────────────────────
            var calStyle = new Style(typeof(Calendar));
            calStyle.Setters.Add(new Setter(Calendar.BackgroundProperty, UiHelper.B("#0c1221")));
            calStyle.Setters.Add(new Setter(Calendar.ForegroundProperty, UiHelper.B("#eef0f2")));
            calStyle.Setters.Add(new Setter(Calendar.BorderBrushProperty, UiHelper.B("#FF6B35")));
            calStyle.Setters.Add(new Setter(Calendar.BorderThicknessProperty, new Thickness(1)));
            calStyle.Setters.Add(new Setter(Calendar.FontFamilyProperty, new FontFamily("Tahoma")));
            calStyle.Setters.Add(new Setter(Calendar.FontSizeProperty, 12.0));
            calStyle.Setters.Add(new Setter(Calendar.FontWeightProperty, FontWeights.Bold));
            Resources["DarkCalendarStyle"] = calStyle;

            // ── CalendarDayButton — كل يوم في التقويم ──────────────────────
            var dayStyle = new Style(typeof(CalendarDayButton));
            dayStyle.Setters.Add(new Setter(CalendarDayButton.ForegroundProperty, UiHelper.B("#c8d8f0")));
            dayStyle.Setters.Add(new Setter(CalendarDayButton.BackgroundProperty, UiHelper.B("#0c1221")));
            dayStyle.Setters.Add(new Setter(CalendarDayButton.FontSizeProperty, 11.5));
            dayStyle.Setters.Add(new Setter(CalendarDayButton.FontWeightProperty, FontWeights.Bold));
            dayStyle.Setters.Add(new Setter(CalendarDayButton.MinWidthProperty, 32.0));
            dayStyle.Setters.Add(new Setter(CalendarDayButton.MinHeightProperty, 30.0));
            dayStyle.Setters.Add(new Setter(CalendarDayButton.MarginProperty, new Thickness(1)));

            // Hover
            var dayHover = new Trigger { Property = CalendarDayButton.IsMouseOverProperty, Value = true };
            dayHover.Setters.Add(new Setter(CalendarDayButton.BackgroundProperty, UiHelper.B("#1a2d50")));
            dayHover.Setters.Add(new Setter(CalendarDayButton.ForegroundProperty, UiHelper.B("#FF6B35")));
            dayStyle.Triggers.Add(dayHover);

            // Selected — خلفية برتقالية واضحة
            var daySelected = new Trigger { Property = CalendarDayButton.IsSelectedProperty, Value = true };
            daySelected.Setters.Add(new Setter(CalendarDayButton.BackgroundProperty, UiHelper.B("#FF6B35")));
            daySelected.Setters.Add(new Setter(CalendarDayButton.ForegroundProperty, UiHelper.B("#ffffff")));
            dayStyle.Triggers.Add(daySelected);

            // أيام الشهر السابق/التالي — لون خافت
            var dayInactive = new Trigger { Property = CalendarDayButton.IsInactiveProperty, Value = true };
            dayInactive.Setters.Add(new Setter(CalendarDayButton.ForegroundProperty, UiHelper.B("#2e4060")));
            dayStyle.Triggers.Add(dayInactive);

            Resources[typeof(CalendarDayButton)] = dayStyle;

            // ── CalendarButton — أزرار الشهر والسنة ────────────────────────
            var calBtnStyle = new Style(typeof(CalendarButton));
            calBtnStyle.Setters.Add(new Setter(CalendarButton.ForegroundProperty, UiHelper.B("#c8d8f0")));
            calBtnStyle.Setters.Add(new Setter(CalendarButton.BackgroundProperty, UiHelper.B("#0c1221")));
            calBtnStyle.Setters.Add(new Setter(CalendarButton.FontWeightProperty, FontWeights.Bold));
            calBtnStyle.Setters.Add(new Setter(CalendarButton.FontSizeProperty, 12.0));
            calBtnStyle.Setters.Add(new Setter(CalendarButton.MinWidthProperty, 60.0));
            calBtnStyle.Setters.Add(new Setter(CalendarButton.MinHeightProperty, 32.0));
            calBtnStyle.Setters.Add(new Setter(CalendarButton.MarginProperty, new Thickness(1)));

            var cbHover = new Trigger { Property = CalendarButton.IsMouseOverProperty, Value = true };
            cbHover.Setters.Add(new Setter(CalendarButton.BackgroundProperty, UiHelper.B("#1a2d50")));
            cbHover.Setters.Add(new Setter(CalendarButton.ForegroundProperty, UiHelper.B("#FF6B35")));
            calBtnStyle.Triggers.Add(cbHover);

            Resources[typeof(CalendarButton)] = calBtnStyle;
        }

        void BuildUI()
        {
            RegisterCalendarStyles(); // ← أول سطر دائماً

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // summary cards
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // filter
            root.RowDefinitions.Add(new RowDefinition());                             // tabs

            // ══ Header ══════════════════════════════════════════════════════
            var header = new Border
            {
                Padding = new Thickness(20, 14, 20, 14),
                BorderBrush = UiHelper.B("#FF6B35"),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            header.Background = UiHelper.B("#0c1221");

            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var logoSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var logoIcon = new Border
            {
                Background = UiHelper.B("#FF6B35"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            logoIcon.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.6
            };
            logoIcon.Child = new TextBlock
            {
                Text = "📊",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var logoText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            logoText.Children.Add(new TextBlock
            {
                Text = "التقارير والإحصائيات",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#eef0f2")
            });
            logoText.Children.Add(new TextBlock
            {
                Text = "تحليل الأداء والأرباح والخسائر",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            logoSp.Children.Add(logoIcon);
            logoSp.Children.Add(logoText);
            Grid.SetColumn(logoSp, 0);
            hGrid.Children.Add(logoSp);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Summary Cards ════════════════════════════════════════════════
            var cardsBorder = new Border
            {
                Background = UiHelper.B("#090e1a"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 12, 16, 12)
            };
            var cardsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _totalSalesTxt = new TextBlock { FontSize = 20, FontWeight = FontWeights.Black, Foreground = UiHelper.B("#06d6a0") };
            _totalProfitTxt = new TextBlock { FontSize = 20, FontWeight = FontWeights.Black, Foreground = UiHelper.B("#a78bfa") };
            _totalOrdersTxt = new TextBlock { FontSize = 20, FontWeight = FontWeights.Black, Foreground = UiHelper.B("#ffd166") };
            _totalLossTxt = new TextBlock { FontSize = 20, FontWeight = FontWeights.Black, Foreground = UiHelper.B("#E63946") };

            cardsPanel.Children.Add(MakeSummaryCard("💰 إجمالي المبيعات", _totalSalesTxt, "#06d6a0", "#0a1f18"));
            cardsPanel.Children.Add(MakeSummaryCard("📈 إجمالي الربح", _totalProfitTxt, "#a78bfa", "#130f20"));
            cardsPanel.Children.Add(MakeSummaryCard("🧾 عدد الأوردرات", _totalOrdersTxt, "#ffd166", "#1a1508"));
            cardsPanel.Children.Add(MakeSummaryCard("📉 إجمالي الخسائر", _totalLossTxt, "#E63946", "#1a080a"));

            cardsBorder.Child = cardsPanel;
            Grid.SetRow(cardsBorder, 1);
            root.Children.Add(cardsBorder);

            // ══ Filter Bar ═══════════════════════════════════════════════════
            var filterBar = new Border
            {
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 10, 16, 10)
            };
            var filterSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _fromPicker = MakeDatePicker(DateTime.Today.AddDays(-7));
            _toPicker = MakeDatePicker(DateTime.Today);

            var searchBtn = UiHelper.MakeActionButton("🔍  عرض التقرير", "#FF6B35", UiHelper.B("#fff8f5"));
            searchBtn.Click += (_, _) => LoadData();

            var exportBtn = UiHelper.MakeActionButton("📤  تصدير Excel", "#1e3a5f", UiHelper.B("#7ab8f5"));
            exportBtn.Click += (_, _) => ExportAll();

            filterSp.Children.Add(UiHelper.FieldLabel("الفترة من:"));
            filterSp.Children.Add(_fromPicker);
            filterSp.Children.Add(UiHelper.FieldLabel("إلى:"));
            filterSp.Children.Add(_toPicker);
            filterSp.Children.Add(searchBtn);
            filterSp.Children.Add(exportBtn);
            filterBar.Child = filterSp;
            Grid.SetRow(filterBar, 2);
            root.Children.Add(filterBar);

            // ══ Tabs ══════════════════════════════════════════════════════════
            var tabs = new TabControl { Background = UiHelper.B("#070b14"), BorderThickness = new Thickness(0) };

            var tabStyle = new Style(typeof(TabItem));
            tabStyle.Setters.Add(new Setter(TabItem.BackgroundProperty, UiHelper.B("#0c1221")));
            tabStyle.Setters.Add(new Setter(TabItem.ForegroundProperty, UiHelper.B("#4a6080")));
            tabStyle.Setters.Add(new Setter(TabItem.FontWeightProperty, FontWeights.Bold));
            tabStyle.Setters.Add(new Setter(TabItem.FontSizeProperty, 12.0));
            tabStyle.Setters.Add(new Setter(TabItem.PaddingProperty, new Thickness(20, 12, 20, 12)));
            tabStyle.Setters.Add(new Setter(TabItem.BorderThicknessProperty, new Thickness(0)));
            var selT = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            selT.Setters.Add(new Setter(TabItem.BackgroundProperty, UiHelper.B("#070b14")));
            selT.Setters.Add(new Setter(TabItem.ForegroundProperty, UiHelper.B("#FF6B35")));
            tabStyle.Triggers.Add(selT);
            tabs.Resources.Add(typeof(TabItem), tabStyle);

            // ── Tab 1: Daily ─────────────────────────────────────────────────
            _dgDaily = BuildGrid();
            _dgDaily.Columns.Add(UiHelper.Col("📅 التاريخ", "Date", 120));
            _dgDaily.Columns.Add(UiHelper.ColNum("المبيعات ج", "Sales", 100, "#eef0f2"));
            _dgDaily.Columns.Add(UiHelper.ColNum("💵 كاش", "Cash", 88, "#06d6a0"));
            _dgDaily.Columns.Add(UiHelper.ColNum("💳 فيزا", "Card", 88, "#60a5fa"));
            _dgDaily.Columns.Add(UiHelper.ColInt("أوردرات", "Orders", 70, "#ffd166"));
            _dgDaily.Columns.Add(UiHelper.ColNum("خصم", "Discount", 80, "#fb923c"));
            _dgDaily.Columns.Add(UiHelper.ColNum("ضريبة", "Tax", 80, "#94a3b8"));
            _dgDaily.Columns.Add(UiHelper.ColNum("خدمة", "ServiceCharge", 80, "#c084fc"));
            _dgDaily.Columns.Add(UiHelper.ColNum("💹 ربح", "Profit", 95, "#06d6a0"));
            _dgDaily.Columns.Add(UiHelper.ColNum("📉 خسائر", "Loss", 95, "#E63946"));

            // ── Tab 2: Top Products ──────────────────────────────────────────
            _dgProd = BuildGrid();
            _dgProd.Columns.Add(new DataGridTextColumn
            {
                Header = "🍕 المنتج",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold)
                    }
                }
            });
            _dgProd.Columns.Add(UiHelper.Col("الفئة", "Category", 110));
            _dgProd.Columns.Add(UiHelper.ColInt("الكمية", "Qty", 75, "#ffd166"));
            _dgProd.Columns.Add(UiHelper.ColNum("المبيعات", "Sales", 105, "#eef0f2"));
            _dgProd.Columns.Add(UiHelper.ColNum("💹 الربح", "Profit", 105, "#06d6a0"));

            // ── Tab 3: Losses ────────────────────────────────────────────────
            var lossPanel = BuildLossTab();

            tabs.Items.Add(new TabItem { Header = "📅  التقرير اليومي", Content = UiHelper.Scroll(_dgDaily) });
            tabs.Items.Add(new TabItem { Header = "🏆  أكثر المنتجات مبيعاً", Content = UiHelper.Scroll(_dgProd) });
            tabs.Items.Add(new TabItem { Header = "📉  سجل الخسائر", Content = lossPanel });

            Grid.SetRow(tabs, 3);
            root.Children.Add(tabs);
            Content = root;
            LoadData();
        }

        // ══ Summary Card ═════════════════════════════════════════════════════
        Border MakeSummaryCard(string label, TextBlock valueTxt, string accentHex, string bgHex)
        {
            var card = new Border
            {
                Background = UiHelper.B(bgHex),
                BorderBrush = UiHelper.B(accentHex),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 10, 0),
                MinWidth = 200
            };
            card.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.15
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B(accentHex),
                Margin = new Thickness(0, 0, 0, 4)
            });
            valueTxt.Text = "—";
            sp.Children.Add(valueTxt);
            card.Child = sp;
            return card;
        }

        // ══ Loss Tab ══════════════════════════════════════════════════════════
        Grid BuildLossTab()
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition());
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var legendBar = new Border
            {
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 10, 16, 10)
            };
            var legendSp = new StackPanel { Orientation = Orientation.Horizontal };
            legendSp.Children.Add(new TextBlock
            {
                Text = "دليل الألوان:",
                Foreground = UiHelper.B("#4a6080"),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            });
            legendSp.Children.Add(LegendItem("■", "#E63946", "خسائر يدوية"));
            legendSp.Children.Add(LegendItem("■", "#ffd166", "بيع بأقل من التكلفة"));
            legendSp.Children.Add(LegendItem("■", "#a78bfa", "خصومات مُعطاة"));

            var addBtn = UiHelper.MakeActionButton("➕  إضافة خسارة", "#E63946", System.Windows.Media.Brushes.White);
            addBtn.HorizontalAlignment = HorizontalAlignment.Left;
            addBtn.Margin = new Thickness(20, 0, 0, 0);
            addBtn.Click += (_, _) => AddManualLoss();

            var legendGrid = new Grid();
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition());
            legendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            legendGrid.Children.Add(legendSp);
            Grid.SetColumn(addBtn, 2);
            legendGrid.Children.Add(addBtn);
            legendBar.Child = legendGrid;
            Grid.SetRow(legendBar, 0);
            g.Children.Add(legendBar);

            _dgLoss = BuildGrid();
            _dgLoss.Columns.Add(UiHelper.Col("📅 التاريخ", "Date", 110));
            _dgLoss.Columns.Add(UiHelper.Col("النوع", "Type", 140));
            _dgLoss.Columns.Add(new DataGridTextColumn
            {
                Header = "الوصف",
                Binding = new Binding("Description"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            _dgLoss.Columns.Add(UiHelper.ColNum("المبلغ ج", "Amount", 110, "#E63946"));

            _dgLoss.Resources.Add(typeof(DataGridRow), new Style
            {
                Setters =
                {
                    new Setter(DataGridRow.ForegroundProperty, new Binding("Color") { Converter = new HexToBrushConverter() }),
                    new Setter(DataGridRow.FontWeightProperty, FontWeights.Bold)
                }
            });

            var rs = new Style(typeof(DataGridRow));
            rs.Setters.Add(new Setter(DataGridRow.ForegroundProperty, UiHelper.B("#eef0f2")));
            var hover = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(DataGridRow.BackgroundProperty, UiHelper.B("#12192e")));
            rs.Triggers.Add(hover);
            _dgLoss.RowStyle = rs;

            var scrollLoss = UiHelper.Scroll(_dgLoss);
            Grid.SetRow(scrollLoss, 1);
            g.Children.Add(scrollLoss);

            var footer = new Border
            {
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 10, 16, 10)
            };
            footer.Child = new TextBlock
            {
                Text = "⚠️  الخسائر تشمل: المصاريف اليدوية + البيع بأقل من التكلفة + الخصومات المُعطاة",
                Foreground = UiHelper.B("#4a6080"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(footer, 2);
            g.Children.Add(footer);

            return g;
        }

        void AddManualLoss()
        {
            var dlg = new LossEntryDialog(_db) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            LoadData();
        }

        // ══ Load Data ═════════════════════════════════════════════════════════
        void LoadData()
        {
            var from = _fromPicker.SelectedDate ?? DateTime.Today.AddDays(-7);
            var to = _toPicker.SelectedDate ?? DateTime.Today;

            var daily = _db.GetDailyRange(from, to);
            var losses = _db.GetLossesSummary(from, to);

            _dgDaily.ItemsSource = daily;
            _dgProd.ItemsSource = _db.GetTopProducts(from, to);
            _dgLoss.ItemsSource = losses;

            double totalSales = daily.Sum(d => d.Sales);
            double totalProfit = daily.Sum(d => d.Profit);
            int totalOrders = daily.Sum(d => d.Orders);
            double totalLoss = losses.Sum(l => l.Amount);

            _totalSalesTxt.Text = $"{totalSales:F2} ج";
            _totalProfitTxt.Text = $"{totalProfit:F2} ج";
            _totalOrdersTxt.Text = $"{totalOrders}";
            _totalLossTxt.Text = $"{totalLoss:F2} ج";
        }

        // ══ Export All ════════════════════════════════════════════════════════
        void ExportAll()
        {
            var from = _fromPicker.SelectedDate ?? DateTime.Today.AddDays(-7);
            var to = _toPicker.SelectedDate ?? DateTime.Today;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel|*.xlsx",
                FileName = $"PizzaPOS_Report_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var daily = _db.GetDailyRange(from, to);
                var prods = _db.GetTopProducts(from, to);
                var losses = _db.GetLossesSummary(from, to);

                using var wb = new ClosedXML.Excel.XLWorkbook();

                // ── Sheet 1: يومي ──────────────────────────────────────────
                var ws1 = wb.Worksheets.Add("يومي");
                var h1 = new[] { "التاريخ", "المبيعات", "كاش", "فيزا", "أوردرات", "خصم", "ضريبة", "خدمة", "ربح", "خسائر" };
                for (int i = 0; i < h1.Length; i++)
                {
                    var cell = ws1.Cell(1, i + 1);
                    cell.Value = h1[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FF6B35");
                    cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                }
                int row = 2;
                foreach (var d in daily)
                {
                    ws1.Cell(row, 1).Value = d.Date;
                    ws1.Cell(row, 2).Value = d.Sales;
                    ws1.Cell(row, 3).Value = d.Cash;
                    ws1.Cell(row, 4).Value = d.Card;
                    ws1.Cell(row, 5).Value = d.Orders;
                    ws1.Cell(row, 6).Value = d.Discount;
                    ws1.Cell(row, 7).Value = d.Tax;
                    ws1.Cell(row, 8).Value = d.ServiceCharge;
                    var pc = ws1.Cell(row, 9);
                    pc.Value = d.Profit;
                    pc.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#00aa66");
                    pc.Style.Font.Bold = true;
                    var lc = ws1.Cell(row, 10);
                    lc.Value = d.Loss;
                    lc.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#E63946");
                    lc.Style.Font.Bold = true;
                    if (row % 2 == 0)
                        ws1.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f7f9fc");
                    row++;
                }
                ws1.Cell(row, 1).Value = "الإجمالي";
                ws1.Cell(row, 2).Value = daily.Sum(d => d.Sales);
                ws1.Cell(row, 3).Value = daily.Sum(d => d.Cash);
                ws1.Cell(row, 4).Value = daily.Sum(d => d.Card);
                ws1.Cell(row, 5).Value = daily.Sum(d => d.Orders);
                ws1.Cell(row, 6).Value = daily.Sum(d => d.Discount);
                ws1.Cell(row, 7).Value = daily.Sum(d => d.Tax);
                ws1.Cell(row, 8).Value = daily.Sum(d => d.ServiceCharge);
                ws1.Cell(row, 9).Value = daily.Sum(d => d.Profit);
                ws1.Cell(row, 10).Value = daily.Sum(d => d.Loss);
                for (int c = 1; c <= 10; c++)
                {
                    ws1.Cell(row, c).Style.Font.Bold = true;
                    ws1.Cell(row, c).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFE4CC");
                }
                ws1.Columns().AdjustToContents();

                // ── Sheet 2: أكثر مبيعاً ───────────────────────────────────
                var ws2 = wb.Worksheets.Add("أكثر مبيعاً");
                var h2 = new[] { "المنتج", "الفئة", "الكمية", "المبيعات", "الربح" };
                for (int i = 0; i < h2.Length; i++)
                {
                    var cell = ws2.Cell(1, i + 1);
                    cell.Value = h2[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FF6B35");
                    cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                }
                row = 2;
                foreach (var p in prods)
                {
                    ws2.Cell(row, 1).Value = p.Name;
                    ws2.Cell(row, 2).Value = p.Category;
                    ws2.Cell(row, 3).Value = p.Qty;
                    ws2.Cell(row, 4).Value = p.Sales;
                    var pc = ws2.Cell(row, 5);
                    pc.Value = p.Profit;
                    pc.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#00aa66");
                    pc.Style.Font.Bold = true;
                    if (row % 2 == 0)
                        ws2.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f7f9fc");
                    row++;
                }
                ws2.Cell(row, 1).Value = "الإجمالي";
                ws2.Cell(row, 3).Value = prods.Sum(p => p.Qty);
                ws2.Cell(row, 4).Value = prods.Sum(p => p.Sales);
                ws2.Cell(row, 5).Value = prods.Sum(p => p.Profit);
                for (int c = 1; c <= 5; c++)
                {
                    ws2.Cell(row, c).Style.Font.Bold = true;
                    ws2.Cell(row, c).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFE4CC");
                }
                ws2.Columns().AdjustToContents();

                // ── Sheet 3: الخسائر ──────────────────────────────────────
                var ws3 = wb.Worksheets.Add("الخسائر");
                var h3 = new[] { "التاريخ", "النوع", "الوصف", "المبلغ" };
                for (int i = 0; i < h3.Length; i++)
                {
                    var cell = ws3.Cell(1, i + 1);
                    cell.Value = h3[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E63946");
                    cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                }
                row = 2;
                foreach (var l in losses)
                {
                    ws3.Cell(row, 1).Value = l.Date;
                    ws3.Cell(row, 2).Value = l.Type;
                    ws3.Cell(row, 3).Value = l.Description;
                    var ac = ws3.Cell(row, 4);
                    ac.Value = l.Amount;
                    ac.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#E63946");
                    ac.Style.Font.Bold = true;
                    if (row % 2 == 0)
                        ws3.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#fff0f0");
                    row++;
                }
                ws3.Cell(row, 3).Value = "إجمالي الخسائر";
                ws3.Cell(row, 4).Value = losses.Sum(l => l.Amount);
                for (int c = 1; c <= 4; c++)
                {
                    ws3.Cell(row, c).Style.Font.Bold = true;
                    ws3.Cell(row, c).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#ffe0e0");
                }
                ws3.Cell(row, 4).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#E63946");
                ws3.Columns().AdjustToContents();

                wb.SaveAs(dlg.FileName);

                if (MessageBox.Show("✅ تم تصدير التقرير!\nهل تريد فتح الملف؟",
                    "تم", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══ Helpers ═══════════════════════════════════════════════════════════
        DataGrid BuildGrid()
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = Brushes.Transparent,
                RowBackground = UiHelper.B("#0b1020"),
                AlternatingRowBackground = UiHelper.B("#080d1a"),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = UiHelper.B("#111c35"),
                BorderThickness = new Thickness(0),
                CanUserAddRows = false,
                RowHeight = 40,
                ColumnHeaderHeight = 44,
                FontSize = 12,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            var hs = new Style(typeof(DataGridColumnHeader));
            hs.Setters.Add(new Setter(Control.BackgroundProperty, UiHelper.B("#0c1530")));
            hs.Setters.Add(new Setter(Control.ForegroundProperty, UiHelper.B("#ffd166")));
            hs.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            hs.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));
            hs.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 0, 14, 0)));
            hs.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 2)));
            hs.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, UiHelper.B("#FF6B35")));
            dg.ColumnHeaderStyle = hs;

            var rs = new Style(typeof(DataGridRow));
            rs.Setters.Add(new Setter(DataGridRow.ForegroundProperty, UiHelper.B("#c8d8f0")));
            var hov = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(DataGridRow.BackgroundProperty, UiHelper.B("#111d38")));
            rs.Triggers.Add(hov);
            var sel = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(DataGridRow.BackgroundProperty, UiHelper.B("#1a2d50")));
            rs.Triggers.Add(sel);
            dg.RowStyle = rs;

            var cs = new Style(typeof(DataGridCell));
            cs.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cs.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(14, 0, 14, 0)));
            cs.Setters.Add(new Setter(DataGridCell.ForegroundProperty, UiHelper.B("#c8d8f0")));
            cs.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));
            var csel = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            csel.Setters.Add(new Setter(DataGridCell.BackgroundProperty, UiHelper.B("#1a2d50")));
            csel.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent));
            cs.Triggers.Add(csel);
            dg.CellStyle = cs;
            return dg;
        }

        // ══ DatePicker محسّن — تعديل الـ visual tree بعد Loaded ══════════════
        DatePicker MakeDatePicker(DateTime dt)
        {
            var dp = new DatePicker
            {
                SelectedDate = dt,
                Width = 160,
                Height = 36,
                Margin = new Thickness(0, 0, 10, 0),
                Background = UiHelper.B("#0f1a2e"),
                Foreground = UiHelper.B("#eef0f2"),
                BorderBrush = UiHelper.B("#FF6B35"),
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                CalendarStyle = (Style)Resources["DarkCalendarStyle"],
                VerticalContentAlignment = VerticalAlignment.Center
            };

            dp.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.30
            };

            dp.Loaded += (_, _) => StyleDatePickerInternals(dp);
            return dp;
        }

        void StyleDatePickerInternals(DatePicker dp)
        {
            // ── الـ TextBox الداخلي ──────────────────────────────────────────
            var tb = FindVisualChild<DatePickerTextBox>(dp);
            if (tb != null)
            {
                tb.Background = UiHelper.B("#0f1a2e");
                tb.Foreground = UiHelper.B("#eef0f2");
                tb.CaretBrush = UiHelper.B("#FF6B35");
                tb.SelectionBrush = UiHelper.B("#FF6B35");
                tb.BorderThickness = new Thickness(0);
                tb.Padding = new Thickness(8, 0, 4, 0);
                tb.FontSize = 12;
                tb.FontWeight = FontWeights.Bold;
                tb.VerticalContentAlignment = VerticalAlignment.Center;

                // Watermark (النص الرمادي الافتراضي)
                tb.ApplyTemplate();
                var wm = tb.Template?.FindName("PART_Watermark", tb) as ContentControl;
                if (wm != null)
                {
                    wm.Foreground = UiHelper.B("#3a5070");
                    wm.FontSize = 11;
                }

                tb.GotFocus += (_, _) =>
                {
                    dp.BorderBrush = UiHelper.B("#FF6B35");
                    dp.BorderThickness = new Thickness(2);
                    ((DropShadowEffect)dp.Effect).Opacity = 0.6;
                    ((DropShadowEffect)dp.Effect).BlurRadius = 18;
                };
                tb.LostFocus += (_, _) =>
                {
                    dp.BorderBrush = UiHelper.B("#FF6B35");
                    dp.BorderThickness = new Thickness(1);
                    ((DropShadowEffect)dp.Effect).Opacity = 0.30;
                    ((DropShadowEffect)dp.Effect).BlurRadius = 10;
                };
            }

            // ── زرار التقويم ─────────────────────────────────────────────────
            var btn = FindVisualChild<Button>(dp);
            if (btn != null)
            {
                btn.Width = 30;
                btn.Background = UiHelper.B("#FF6B35");
                btn.BorderThickness = new Thickness(0);
                btn.Cursor = System.Windows.Input.Cursors.Hand;
                btn.Content = new TextBlock
                {
                    Text = "📅",
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                btn.MouseEnter += (_, _) => btn.Background = UiHelper.B("#e05a28");
                btn.MouseLeave += (_, _) => btn.Background = UiHelper.B("#FF6B35");

                var f = new FrameworkElementFactory(typeof(Border));
                f.SetBinding(Border.BackgroundProperty,
                    new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                f.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 6, 6, 0));
                var cp = new FrameworkElementFactory(typeof(ContentPresenter));
                cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                f.AppendChild(cp);
                btn.Template = new ControlTemplate(typeof(Button)) { VisualTree = f };
            }

            // ── الـ Border الخارجي ────────────────────────────────────────────
            var outerBorder = FindVisualChild<Border>(dp);
            if (outerBorder != null)
            {
                outerBorder.Background = UiHelper.B("#0f1a2e");
                outerBorder.BorderBrush = UiHelper.B("#FF6B35");
                outerBorder.BorderThickness = new Thickness(1);
                outerBorder.CornerRadius = new CornerRadius(6);
            }
        }

        // ══ Visual Tree Helper ════════════════════════════════════════════════
        T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        StackPanel LegendItem(string dot, string color, string label) =>
            new()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text              = dot,
                        Foreground        = UiHelper.B(color),
                        FontSize          = 14,
                        Margin            = new Thickness(0, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text              = label,
                        Foreground        = UiHelper.B("#7a9ab8"),
                        FontSize          = 11,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LossEntryDialog
    // ══════════════════════════════════════════════════════════════════════════
    public class LossEntryDialog : Window
    {
        readonly AppDbContext _db;
        TextBox _tbDesc = null!;
        TextBox _tbAmount = null!;
        ComboBox _cbType = null!;

        public LossEntryDialog(AppDbContext db)
        {
            _db = db;
            Title = "📉 إضافة خسارة";
            Width = 420;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#070b14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;
            BuildUI();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Header ──────────────────────────────────────────────────────
            var header = new Border
            {
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#E63946"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#E63946"),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            hIcon.Child = new TextBlock
            {
                Text = "📉",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hSp.Children.Add(hIcon);
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "تسجيل خسارة يدوية",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#E63946")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "أدخل تفاصيل الخسارة بدقة",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──────────────────────────────────────────────────────
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            fields.Children.Add(UiHelper.FieldLabel("نوع الخسارة *"));
            _cbType = new ComboBox
            {
                Background = UiHelper.B("#0f1a2e"),
                Foreground = UiHelper.B("#eef0f2"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 9, 10, 9),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 14),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var iStyle = new Style(typeof(ComboBoxItem));
            iStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#0f1a2e")));
            iStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, UiHelper.B("#eef0f2")));
            iStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(12, 8, 12, 8)));
            var hov = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#1a2640")));
            iStyle.Triggers.Add(hov);
            var selT = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selT.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#E63946")));
            selT.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, UiHelper.B("#ffffff")));
            iStyle.Triggers.Add(selT);
            _cbType.ItemContainerStyle = iStyle;
            _cbType.Resources.Add(SystemColors.WindowBrushKey, UiHelper.B("#0f1a2e"));
            _cbType.Resources.Add(SystemColors.HighlightBrushKey, UiHelper.B("#E63946"));
            _cbType.Resources.Add(SystemColors.ControlBrushKey, UiHelper.B("#0f1a2e"));
            foreach (var t in _db.GetLossTypes())
                _cbType.Items.Add(new ComboBoxItem
                {
                    Content = t,
                    Tag = t,
                    Background = UiHelper.B("#0f1a2e"),
                    Foreground = UiHelper.B("#eef0f2")
                });
            _cbType.SelectedIndex = 0;
            fields.Children.Add(_cbType);

            fields.Children.Add(UiHelper.FieldLabel("الوصف *"));
            _tbDesc = UiHelper.MakeTB("", "#E63946");
            _tbDesc.Height = 80;
            _tbDesc.AcceptsReturn = true;
            _tbDesc.TextWrapping = TextWrapping.Wrap;
            _tbDesc.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbDesc);

            fields.Children.Add(UiHelper.FieldLabel("المبلغ (ج) *"));
            _tbAmount = UiHelper.MakeTB("0.00", "#E63946");
            _tbAmount.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbAmount);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Button Bar ──────────────────────────────────────────────────
            var btnBar = new Border
            {
                Background = UiHelper.B("#090e1a"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 14, 20, 18)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancelBtn.BorderBrush = UiHelper.B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            var saveBtn = UiHelper.MakeBtn("💾  تسجيل الخسارة", "#E63946",
                System.Windows.Media.Brushes.White, Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#E63946"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.45
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbDesc.Focus();
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbDesc.Text))
            {
                MessageBox.Show("أدخل وصف الخسارة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // يقبل فاصلة عشرية سواء نقطة أو فاصلة
            var amountStr = _tbAmount.Text.Replace(',', '.');
            if (!double.TryParse(amountStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double amount) || amount <= 0)
            {
                MessageBox.Show("أدخل مبلغ صحيح", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var type = (_cbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "أخرى";
            _db.SaveLoss(new LossEntry
            {
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                Type = type,
                Description = _tbDesc.Text.Trim(),
                Amount = amount,
                CreatedBy = SessionService.CurrentUser?.FullName ?? "—"
            });
            MessageBox.Show("✅ تم تسجيل الخسارة بنجاح", "تم",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  HexToBrushConverter
    // ══════════════════════════════════════════════════════════════════════════
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
                catch { return Brushes.White; }
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}
