// Views/OrderTrackingWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PizzaPOS.Views
{
    public class OrderTrackingWindow : Window
    {
        readonly AppDbContext _db = new();
        DispatcherTimer _timer = null!;

        StackPanel _colNew = null!;
        StackPanel _colKitchen = null!;
        StackPanel _colReady = null!;
        StackPanel _colDelivery = null!;
        StackPanel _colDone = null!;

        TextBlock _cntNew = null!;
        TextBlock _cntKitchen = null!;
        TextBlock _cntReady = null!;
        TextBlock _cntDelivery = null!;
        TextBlock _cntDone = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        record Stage(string Key, string Ar, string Icon,
                     string Color, string NextKey, string NextLabel);

        readonly List<Stage> _stages = new()
        {
            new("new",       "جديد",          "🆕", "#ffd166", "kitchen",   "أرسل للمطبخ ←"),
            new("kitchen",   "مع المطبخ",      "👨‍🍳", "#a78bfa", "ready",     "جاهز ←"),
            new("ready",     "جاهز",           "✅", "#06d6a0", "delivery",  "خرج للتوصيل ←"),
            new("delivery",  "خرج للتوصيل",   "🛵", "#FF6B35", "completed", "تم التسليم ←"),
            new("completed", "تم",             "🎉", "#4a6080", "",          "")
        };

        string StatusToKey(string? status) => status switch
        {
            "new" => "new",
            "جديد" => "new",
            "kitchen" => "kitchen",
            "مع المطبخ" => "kitchen",
            "ready" => "ready",
            "جاهز" => "ready",
            "delivery" => "delivery",
            "خرج للتوصيل" => "delivery",
            "completed" => "completed",
            "تم" => "completed",
            _ => "new"
        };

        string KeyToStatus(string key) => key switch
        {
            "new" => "new",
            "kitchen" => "مع المطبخ",
            "ready" => "جاهز",
            "delivery" => "خرج للتوصيل",
            "completed" => "completed",
            _ => "new"
        };

        public OrderTrackingWindow()
        {
            Title = "📋 تتبع الأوردرات";
            WindowState = WindowState.Maximized;
            Background = B("#060810");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            BuildUI();
            Load();
            StartAutoRefresh();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#E63946"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 12, 20, 12)
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = B("#E63946"),
                CornerRadius = new CornerRadius(10),
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "📋",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(hIcon);
            titleSp.Children.Add(new TextBlock
            {
                Text = "لوحة تتبع الأوردرات",
                FontSize = 20,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2"),
                VerticalAlignment = VerticalAlignment.Center
            });

            var clockTxt = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = B("#ffd166"),
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var ct = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            ct.Tick += (_, _) => clockTxt.Text = DateTime.Now.ToString("HH:mm:ss");
            ct.Start();
            clockTxt.Text = DateTime.Now.ToString("HH:mm:ss");

            Grid.SetColumn(titleSp, 0); Grid.SetColumn(clockTxt, 1);
            hGrid.Children.Add(titleSp); hGrid.Children.Add(clockTxt);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Board
            var board = new Grid { Margin = new Thickness(12) };
            for (int i = 0; i < 9; i++)
                board.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = i % 2 == 0
                        ? new GridLength(1, GridUnitType.Star)
                        : new GridLength(8)
                });

            (_colNew, _cntNew) = BuildColumn("🆕", "جديد", "#ffd166", 0, board);
            (_colKitchen, _cntKitchen) = BuildColumn("👨‍🍳", "مع المطبخ", "#a78bfa", 2, board);
            (_colReady, _cntReady) = BuildColumn("✅", "جاهز", "#06d6a0", 4, board);
            (_colDelivery, _cntDelivery) = BuildColumn("🛵", "خرج للتوصيل", "#FF6B35", 6, board);
            (_colDone, _cntDone) = BuildColumn("🎉", "تم", "#4a6080", 8, board);

            Grid.SetRow(board, 1);
            root.Children.Add(board);

            // Footer
            var footer = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 8, 16, 8)
            };
            var fSp = new StackPanel { Orientation = Orientation.Horizontal };
            fSp.Children.Add(MakeBtn("🔄 تحديث", "#1e2d4a", B("#b0c4de"), Load));
            fSp.Children.Add(new TextBlock
            {
                Text = "  •  بيتحدث كل 15 ثانية  •  اضغط زرار الحالة لتقدم الأوردر  •  ✏️ للتعديل",
                Foreground = B("#4a6080"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            });
            footer.Child = fSp;
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
        }

        (StackPanel, TextBlock) BuildColumn(string icon, string label,
                                            string color, int col, Grid board)
        {
            var colRoot = new Grid();
            colRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            colRoot.RowDefinitions.Add(new RowDefinition());

            var hdr = new Border
            {
                Background = B("#0d1525"),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                BorderBrush = B(color),
                BorderThickness = new Thickness(0, 3, 0, 0),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 2)
            };
            var hg = new Grid();
            hg.ColumnDefinitions.Add(new ColumnDefinition());
            hg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tSp = new StackPanel { Orientation = Orientation.Horizontal };
            tSp.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 18,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            tSp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 14,
                FontWeight = FontWeights.Black,
                Foreground = B(color),
                VerticalAlignment = VerticalAlignment.Center
            });

            var cntB = new Border
            {
                Background = B(color),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            var cntTxt = new TextBlock
            {
                Text = "0",
                FontSize = 12,
                FontWeight = FontWeights.Black,
                Foreground = B("#0a0a14")
            };
            cntB.Child = cntTxt;

            Grid.SetColumn(tSp, 0); Grid.SetColumn(cntB, 1);
            hg.Children.Add(tSp); hg.Children.Add(cntB);
            hdr.Child = hg;
            Grid.SetRow(hdr, 0);
            colRoot.Children.Add(hdr);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = B("#080c18")
            };
            var cards = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            scroll.Content = cards;
            Grid.SetRow(scroll, 1);
            colRoot.Children.Add(scroll);

            var colBorder = new Border
            {
                Child = colRoot,
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };
            Grid.SetColumn(colBorder, col);
            board.Children.Add(colBorder);

            return (cards, cntTxt);
        }

        void Load()
        {
            try
            {
                var orders = _db.GetActiveOrders();
                var groups = new Dictionary<string, List<Order>>
                {
                    ["new"] = new(),
                    ["kitchen"] = new(),
                    ["ready"] = new(),
                    ["delivery"] = new(),
                    ["completed"] = new()
                };
                foreach (var o in orders)
                    groups[StatusToKey(o.Status)].Add(o);

                FillColumn(_colNew, _cntNew, groups["new"], "#ffd166", "new");
                FillColumn(_colKitchen, _cntKitchen, groups["kitchen"], "#a78bfa", "kitchen");
                FillColumn(_colReady, _cntReady, groups["ready"], "#06d6a0", "ready");
                FillColumn(_colDelivery, _cntDelivery, groups["delivery"], "#FF6B35", "delivery");
                FillColumn(_colDone, _cntDone, groups["completed"], "#4a6080", "completed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ:\n{ex.Message}", "خطأ في التحميل");
            }
        }

        void FillColumn(StackPanel panel, TextBlock count,
                        List<Order> orders, string color, string stageKey)
        {
            panel.Children.Clear();
            count.Text = orders.Count.ToString();
            var stage = _stages.First(s => s.Key == stageKey);
            foreach (var o in orders)
                panel.Children.Add(BuildCard(o, color, stage));

            if (orders.Count == 0)
                panel.Children.Add(new TextBlock
                {
                    Text = "لا يوجد أوردرات",
                    Foreground = B("#2a3f5a"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
        }

        UIElement BuildCard(Order order, string color, Stage stage)
        {
            int orderId = order.Id;
            string orderNum = order.OrderNumber;
            string stageKey = stage.Key;
            string nextKey = stage.NextKey;
            string nextLabel = stage.NextLabel;

            bool isUrgent = DateTime.TryParse(order.CreatedAt, out var dt)
                            && (DateTime.Now - dt).TotalMinutes > 20
                            && stageKey != "completed";

            string timeStr = "";
            if (DateTime.TryParse(order.CreatedAt, out var dt2))
            {
                var e = DateTime.Now - dt2;
                timeStr = e.TotalMinutes < 60
                    ? $"منذ {(int)e.TotalMinutes} دقيقة"
                    : $"منذ {(int)e.TotalHours} ساعة";
            }

            var card = new Border
            {
                Background = isUrgent ? B("#1a0a0a") : B("#0d1525"),
                CornerRadius = new CornerRadius(10),
                BorderBrush = isUrgent ? B("#E63946") : B(color),
                BorderThickness = new Thickness(isUrgent ? 2 : 1),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(4, 0, 4, 8)
            };

            var sp = new StackPanel();

            var topRow = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition());
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var numB = new Border
            {
                Background = B(color),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2)
            };
            numB.Child = new TextBlock
            {
                Text = orderNum,
                Foreground = B("#0a0a14"),
                FontWeight = FontWeights.Black,
                FontSize = 11
            };
            

            var typeB = new Border
            {
                Background = B("#0f1526"),
                CornerRadius = new CornerRadius(6),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 2, 6, 2)
            };
            typeB.Child = new TextBlock
            { Text = order.OrderType, Foreground = B("#b0c4de"), FontSize = 10 };

            Grid.SetColumn(numB, 0); Grid.SetColumn(typeB, 1);
            topRow.Children.Add(numB); topRow.Children.Add(typeB);
            sp.Children.Add(topRow);

            sp.Children.Add(new TextBlock
            {
                Text = $"🕐 {timeStr}",
                FontSize = 10,
                Foreground = isUrgent ? B("#E63946") : B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            sp.Children.Add(new TextBlock
            {
                Text = $"💰 {order.Total:F2} ج",
                FontSize = 13,
                FontWeight = FontWeights.Black,
                Foreground = B(color),
                Margin = new Thickness(0, 0, 0, 4)
            });

            if (!string.IsNullOrEmpty(order.CustomerName))
                sp.Children.Add(new TextBlock
                {
                    Text = $"👤 {order.CustomerName}",
                    FontSize = 11,
                    Foreground = B("#b0c4de"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 2)
                });

            if (!string.IsNullOrEmpty(order.DeliveryAddress))
                sp.Children.Add(new TextBlock
                {
                    Text = $"📍 {order.DeliveryAddress}",
                    FontSize = 10,
                    Foreground = B("#4a6080"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 2)
                });

            if (!string.IsNullOrEmpty(order.DriverName))
                sp.Children.Add(new TextBlock
                {
                    Text = $"🛵 {order.DriverName}",
                    FontSize = 10,
                    Foreground = B("#FF6B35"),
                    Margin = new Thickness(0, 0, 0, 2)
                });

            if (!string.IsNullOrEmpty(order.Notes))
                sp.Children.Add(new Border
                {
                    Background = B("#0f1820"),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 4, 0, 4),
                    Child = new TextBlock
                    {
                        Text = $"📝 {order.Notes}",
                        FontSize = 10,
                        Foreground = B("#ffd166"),
                        TextWrapping = TextWrapping.Wrap
                    }
                });


            var btnSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 6, 0, 0)
            };

            if (!string.IsNullOrEmpty(nextKey))
            {
                string capturedNextKey = nextKey;
                string capturedLabel = nextLabel;
                int capturedId = orderId;

                var advBtn = MakeActionBtn(
                    capturedLabel,
                    capturedNextKey == "completed" ? "#06d6a0" : color,
                    B("#0a0a14"),
                    () =>
                    {
                        string newStatus = KeyToStatus(capturedNextKey);
                        string? delStatus = capturedNextKey == "delivery" ? "قيد التوصيل"
                                          : capturedNextKey == "completed" ? "تم التوصيل"
                                          : null;
                        _db.UpdateOrderStatus(capturedId, newStatus, delStatus);
                        Load();
                    });
                btnSp.Children.Add(advBtn);
            }

            if (stageKey != "completed")
            {
                int capturedId = orderId;
                var editBtn = MakeActionBtn("✏️", "#1e2d4a", B("#ffd166"), () =>
                {
                    var fullOrder = _db.GetOrderById(capturedId);
                    if (fullOrder == null) return;
                    var dlg = new EditOrderDialog(fullOrder, _db) { Owner = this };
                    if (dlg.ShowDialog() == true) Load();
                });
                editBtn.Padding = new Thickness(10, 8, 10, 8);
                editBtn.Margin = new Thickness(6, 0, 0, 0);
                editBtn.ToolTip = "تعديل الأوردر";
                btnSp.Children.Add(editBtn);
            }
            else
            {
                int capturedId = orderId;
                var printBtn = MakeActionBtn("🖨️", "#0d1525", B("#4a6080"), () =>
                {
                    var o = _db.GetOrderById(capturedId);
                    if (o == null) return;
                    new EpsonService().PrintReceipt(o, _db);
                });
                printBtn.Padding = new Thickness(10, 8, 10, 8);
                printBtn.Margin = new Thickness(6, 0, 0, 0);
                printBtn.ToolTip = "إعادة طباعة الفاتورة";
                btnSp.Children.Add(printBtn);
            }

            sp.Children.Add(btnSp);

            if (isUrgent)
                sp.Children.Add(new TextBlock
                {
                    Text = "⚠️ أوردر متأخر!",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = B("#E63946"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                });

            card.Child = sp;

            card.MouseEnter += (_, _) =>
            { if (stageKey != "completed") card.Background = B("#12192e"); };
            card.MouseLeave += (_, _) =>
                card.Background = isUrgent ? B("#1a0a0a") : B("#0d1525");

            return card;
        }

        void StartAutoRefresh()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _timer.Tick += (_, _) => Load();
            _timer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            base.OnClosed(e);
        }

        Button MakeActionBtn(string text, string bg, SolidColorBrush fg, Action click)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new System.Windows.Data.Binding("Background")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            f.SetBinding(Border.PaddingProperty,
                new System.Windows.Data.Binding("Padding")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
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
                Padding = new Thickness(0, 8, 0, 8),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }


        Button MakeBtn(string text, string bg, SolidColorBrush fg, Action click)
        {
            var btn = MakeActionBtn(text, bg, fg, click);
            btn.Padding = new Thickness(16, 9, 16, 9);
            btn.FontSize = 13;
            return btn;
        }
    }

    // ════════════════════════════════════════════════
    //  EditOrderDialog
    // ════════════════════════════════════════════════
    public class EditOrderDialog : Window
    {
        readonly Order _order;
        readonly AppDbContext _db;
        readonly List<Product> _products;
        readonly List<Driver> _drivers;

        ObservableCollection<OrderItem> _items = null!;
        StackPanel _itemsDirectPanel = null!;
        TextBlock _subtotalTxt = null!;
        TextBlock _discTxt = null!;
        TextBlock _taxTxt = null!;
        TextBlock _totalTxt = null!;
        TextBox _tbDiscount = null!;
        ComboBox _cbPayMethod = null!;
        TextBox _tbNotes = null!;
        TextBox _tbCustName = null!;
        TextBox _tbCustPhone = null!;
        TextBox _tbAddress = null!;
        ComboBox _cbDriver = null!;
        TextBox _tbFee = null!;
        StackPanel _deliverySp = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public EditOrderDialog(Order order, AppDbContext db)
        {
            _order = order;
            _db = db;
            _products = db.GetProducts();
            _drivers = db.GetDrivers();
            _items = new ObservableCollection<OrderItem>(order.Items);

            Title = $"✏️ تعديل أوردر {order.OrderNumber}";
            Width = 700; Height = 700;
            Background = B("#0a0a14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.CanResize;
            BuildUI();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var hdr = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#ffd166"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(18, 14, 18, 14)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = B("#ffd166"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "✏️",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = $"تعديل أوردر: {_order.OrderNumber}",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = B("#ffd166")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"{_order.CreatedAt}  •  {_order.OrderType}",
                FontSize = 11,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon); hSp.Children.Add(hInfo);
            hdr.Child = hSp;
            Grid.SetRow(hdr, 0); root.Children.Add(hdr);

            // Content
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(16, 12, 16, 12)
            };
            var content = new StackPanel();

            content.Children.Add(SectionLabel("📦 الأصناف"));
            _itemsDirectPanel = new StackPanel();
            content.Children.Add(_itemsDirectPanel);
            RebuildItemsPanel();

            // إضافة صنف
            var addRow = new Grid { Margin = new Thickness(0, 8, 0, 16) };
            addRow.ColumnDefinitions.Add(new ColumnDefinition());
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cbProduct = new ComboBox
            {
                Background = B("#0f1526"),
                Foreground = B("#eef0f2"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13,
                DisplayMemberPath = "Name",
                ItemsSource = _products
            };
            StyleComboItems(cbProduct);

            var addBtn = MakeBtn("➕ إضافة صنف", "#06d6a0", B("#0a0a14"), () =>
            {
                if (cbProduct.SelectedItem is not Product p) return;
                var ex = _items.FirstOrDefault(i => i.ProductId == p.Id
                                                 && string.IsNullOrEmpty(i.SizeName));
                if (ex != null)
                    ex.Qty++;
                else
                    _items.Add(new OrderItem
                    {
                        ProductId = p.Id,
                        Name = p.Name,
                        Icon = p.Icon,
                        BasePrice = p.Price,
                        Cost = p.Cost > 0 ? p.Cost : p.Price * 0.4, // ✅ لو Cost = 0 يحط تقدير
                        ExtrasKey = $"{p.Id}|",
                        Qty = 1
                    });
                RebuildItemsPanel();
                Recalc();
            });

            Grid.SetColumn(cbProduct, 0); Grid.SetColumn(addBtn, 1);
            addRow.Children.Add(cbProduct); addRow.Children.Add(addBtn);
            content.Children.Add(addRow);

            // طريقة الدفع والخصم
            content.Children.Add(SectionLabel("💳 الدفع والخصم"));
            var payDiscGrid = new Grid { Margin = new Thickness(0, 6, 0, 14) };
            payDiscGrid.ColumnDefinitions.Add(new ColumnDefinition());
            payDiscGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            payDiscGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var payStack = new StackPanel();
            payStack.Children.Add(FieldLabel("طريقة الدفع"));
            _cbPayMethod = new ComboBox
            {
                Background = B("#0f1526"),
                Foreground = B("#eef0f2"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0)
            };
            StyleComboItems(_cbPayMethod);
            foreach (var m in new[] { "كاش", "فيزا/ماستر" })
                _cbPayMethod.Items.Add(new ComboBoxItem
                {
                    Content = m,
                    Tag = m,
                    Background = B("#0f1526"),
                    Foreground = B("#eef0f2")
                });
            _cbPayMethod.SelectedIndex = _order.PayMethod == "كاش" ? 0 : 1;
            payStack.Children.Add(_cbPayMethod);
            Grid.SetColumn(payStack, 0); payDiscGrid.Children.Add(payStack);

            var discStack = new StackPanel();
            discStack.Children.Add(FieldLabel("الخصم (ج)"));
            _tbDiscount = MakeTB(_order.Discount.ToString("F2"));
            _tbDiscount.Margin = new Thickness(0, 4, 0, 0);
            _tbDiscount.TextChanged += (_, _) => Recalc();
            discStack.Children.Add(_tbDiscount);
            Grid.SetColumn(discStack, 2); payDiscGrid.Children.Add(discStack);
            content.Children.Add(payDiscGrid);

            // ملاحظات
            content.Children.Add(SectionLabel("📝 ملاحظات"));
            _tbNotes = MakeTB(_order.Notes ?? "");
            _tbNotes.Margin = new Thickness(0, 6, 0, 14);
            content.Children.Add(_tbNotes);

            // بيانات الديلفري
            _deliverySp = new StackPanel();
            _deliverySp.Visibility = _order.OrderType == "ديلفري"
                ? Visibility.Visible : Visibility.Collapsed;

            _deliverySp.Children.Add(SectionLabel("🛵 بيانات التوصيل"));

            var delGrid1 = new Grid { Margin = new Thickness(0, 6, 0, 10) };
            delGrid1.ColumnDefinitions.Add(new ColumnDefinition());
            delGrid1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            delGrid1.ColumnDefinitions.Add(new ColumnDefinition());

            var custNameSp = new StackPanel();
            custNameSp.Children.Add(FieldLabel("اسم العميل"));
            _tbCustName = MakeTB(_order.CustomerName);
            _tbCustName.Margin = new Thickness(0, 4, 0, 0);
            custNameSp.Children.Add(_tbCustName);

            var custPhoneSp = new StackPanel();
            custPhoneSp.Children.Add(FieldLabel("التليفون"));
            _tbCustPhone = MakeTB(_order.CustomerPhone);
            _tbCustPhone.Margin = new Thickness(0, 4, 0, 0);
            custPhoneSp.Children.Add(_tbCustPhone);

            Grid.SetColumn(custNameSp, 0); Grid.SetColumn(custPhoneSp, 2);
            delGrid1.Children.Add(custNameSp); delGrid1.Children.Add(custPhoneSp);
            _deliverySp.Children.Add(delGrid1);

            _deliverySp.Children.Add(FieldLabel("العنوان"));
            _tbAddress = new TextBox
            {
                Text = _order.DeliveryAddress,
                Background = B("#0f1526"),
                Foreground = B("#ffffff"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13,
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Margin = new Thickness(0, 4, 0, 10)
            };
            _deliverySp.Children.Add(_tbAddress);

            var driverFeeSp = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            driverFeeSp.ColumnDefinitions.Add(new ColumnDefinition());
            driverFeeSp.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            driverFeeSp.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

            var driverSp = new StackPanel();
            driverSp.Children.Add(FieldLabel("السائق"));
            _cbDriver = new ComboBox
            {
                Background = B("#0f1526"),
                Foreground = B("#eef0f2"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0)
            };
            StyleComboItems(_cbDriver);
            var noDriverItem = new ComboBoxItem
            {
                Content = "— بدون سائق —",
                Tag = 0,
                Background = B("#0f1526"),
                Foreground = B("#4a6080")
            };
            _cbDriver.Items.Add(noDriverItem);
            _cbDriver.SelectedItem = noDriverItem;
            foreach (var d in _drivers)
            {
                var item = new ComboBoxItem
                {
                    Content = d.Display,
                    Tag = d.Id,
                    Background = B("#0f1526"),
                    Foreground = B("#eef0f2")
                };
                _cbDriver.Items.Add(item);
                if (d.Id == _order.DriverId) _cbDriver.SelectedItem = item;
            }
            driverSp.Children.Add(_cbDriver);

            var feeSp = new StackPanel();
            feeSp.Children.Add(FieldLabel("رسوم التوصيل (ج)"));
            _tbFee = MakeTB(_order.DeliveryFee.ToString("F2"));
            _tbFee.Margin = new Thickness(0, 4, 0, 0);
            _tbFee.TextChanged += (_, _) => Recalc();
            feeSp.Children.Add(_tbFee);

            Grid.SetColumn(driverSp, 0); Grid.SetColumn(feeSp, 2);
            driverFeeSp.Children.Add(driverSp); driverFeeSp.Children.Add(feeSp);
            _deliverySp.Children.Add(driverFeeSp);
            content.Children.Add(_deliverySp);

            scroll.Content = content;
            Grid.SetRow(scroll, 1); root.Children.Add(scroll);

            // Totals
            var totals = new Border
            {
                Background = B("#0d1525"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(16, 10, 16, 10)
            };
            var totSp = new StackPanel();
            _subtotalTxt = new TextBlock { FontSize = 12, Foreground = B("#b0c4de"), Margin = new Thickness(0, 0, 0, 4) };
            _discTxt = new TextBlock { FontSize = 12, Foreground = B("#ff7a85"), Margin = new Thickness(0, 0, 0, 4) };
            _taxTxt = new TextBlock { FontSize = 12, Foreground = B("#b0c4de"), Margin = new Thickness(0, 0, 0, 6) };
            _totalTxt = new TextBlock { FontSize = 18, FontWeight = FontWeights.Black, Foreground = B("#06d6a0") };
            totSp.Children.Add(_subtotalTxt);
            totSp.Children.Add(_discTxt);
            totSp.Children.Add(_taxTxt);
            totSp.Children.Add(_totalTxt);
            totals.Child = totSp;
            Grid.SetRow(totals, 2); root.Children.Add(totals);

            // Buttons
            var btnBar = new Border
            {
                Background = B("#0d1220"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 12, 16, 14)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

            var cancelBtn = MakeBtn("إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            var printBtn = MakeBtn("🖨️ إعادة طباعة", "#1e2d4a", B("#b0c4de"), () =>
            {
                SaveChanges();
                var updated = _db.GetOrderById(_order.Id);
                if (updated != null) new EpsonService().PrintReceipt(updated, _db);
            });

            var saveBtn = MakeBtn("💾 حفظ التعديلات", "#ffd166", B("#0a0a14"), () =>
            {
                if (SaveChanges()) { DialogResult = true; Close(); }
            });

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(printBtn, 2);
            Grid.SetColumn(saveBtn, 4);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(printBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 3); root.Children.Add(btnBar);

            Content = root;
            Recalc();
        }

        void Recalc()
        {
            double sub = _items.Sum(i => i.Price * i.Qty);
            double disc = double.TryParse(_tbDiscount?.Text, out var d) ? d : 0;
            disc = Math.Min(disc, sub);

            string taxStr = _db.GetSetting("TaxRate", "14");
            double taxPct = double.TryParse(taxStr, out var t) ? t : 14;
            if (taxPct < 1) taxPct *= 100;

            double after = sub - disc;
            double tax = after * (taxPct / 100);
            double fee = double.TryParse(_tbFee?.Text, out var f) ? f : 0;
            double total = after + tax + fee;

            if (_subtotalTxt != null) _subtotalTxt.Text = $"المجموع: {sub:F2} ج";
            if (_discTxt != null) _discTxt.Text = disc > 0 ? $"خصم: -{disc:F2} ج" : "";
            if (_taxTxt != null) _taxTxt.Text = $"ضريبة ({taxPct:0.##}%): {tax:F2} ج";
            if (_totalTxt != null) _totalTxt.Text = $"الإجمالي: {total:F2} ج";
        }

        void RebuildItemsPanel()
        {
            if (_itemsDirectPanel == null) return;
            _itemsDirectPanel.Children.Clear();
            foreach (var item in _items)
                _itemsDirectPanel.Children.Add(BuildItemRow(item));
        }

        UIElement BuildItemRow(OrderItem item)
        {
            var border = new Border
            {
                Background = B("#0d1525"),
                CornerRadius = new CornerRadius(8),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(10, 8, 10, 8),
                Tag = item
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            var nameSp = new StackPanel();
            nameSp.Children.Add(new TextBlock
            {
                Text = item.Name,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = B("#ffffff")
            });
            if (!string.IsNullOrEmpty(item.SizeName))
                nameSp.Children.Add(new TextBlock
                { Text = item.SizeName, FontSize = 10, Foreground = B("#ffd166") });
            Grid.SetColumn(nameSp, 0); g.Children.Add(nameSp);

            var decBtn = MakeQtyBtn("−", () =>
            { if (item.Qty > 1) { item.Qty--; RebuildItemsPanel(); Recalc(); } });
            Grid.SetColumn(decBtn, 1); g.Children.Add(decBtn);

            var qtyTxt = new TextBlock
            {
                Text = item.Qty.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Black,
                Foreground = B("#ffffff"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            item.PropertyChanged += (_, _) => qtyTxt.Text = item.Qty.ToString();
            Grid.SetColumn(qtyTxt, 2); g.Children.Add(qtyTxt);

            var incBtn = MakeQtyBtn("+", () => { item.Qty++; RebuildItemsPanel(); Recalc(); });
            Grid.SetColumn(incBtn, 3); g.Children.Add(incBtn);

            var priceTxt = new TextBlock
            {
                Text = $"{item.Price * item.Qty:F2} ج",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = B("#06d6a0"),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            item.PropertyChanged += (_, _) => priceTxt.Text = $"{item.Price * item.Qty:F2} ج";
            Grid.SetColumn(priceTxt, 4); g.Children.Add(priceTxt);

            OrderItem capturedItem = item;
            var delBtn = new Button
            {
                Content = "✕",
                FontSize = 12,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = B("#4a6080"),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            delBtn.Click += (_, _) =>
            {
                _items.Remove(capturedItem);
                RebuildItemsPanel();
                Recalc();
            };
            Grid.SetColumn(delBtn, 5); g.Children.Add(delBtn);

            border.Child = g;
            return border;
        }

        bool SaveChanges()
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("لازم يكون في صنف واحد على الأقل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            double sub = _items.Sum(i => i.Price * i.Qty);
            double disc = double.TryParse(_tbDiscount.Text, out var d) ? Math.Min(d, sub) : 0;
            string taxStr = _db.GetSetting("TaxRate", "14");
            double taxPct = double.TryParse(taxStr, out var t) ? t : 14;
            if (taxPct < 1) taxPct *= 100;
            double after = sub - disc;
            double tax = after * (taxPct / 100);
            double fee = double.TryParse(_tbFee?.Text, out var f) ? f : 0;

            var pm = (_cbPayMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                     ?? _order.PayMethod;

            int driverId = 0;
            string driverName = "";
            if (_cbDriver?.SelectedItem is ComboBoxItem di && (int)(di.Tag ?? 0) > 0)
            {
                driverId = (int)(di.Tag ?? 0);
                driverName = _drivers.FirstOrDefault(d2 => d2.Id == driverId)?.Name ?? "";
            }

            _order.Items = _items.ToList();
            _order.Subtotal = sub;
            _order.Discount = disc;
            _order.Tax = tax;
            _order.Total = after + tax + fee;
            _order.PaidAmount = after + tax + fee;
            _order.Change = 0;
            _order.PayMethod = pm;
            _order.Notes = _tbNotes.Text.Trim();
            _order.CustomerName = _tbCustName?.Text.Trim() ?? _order.CustomerName;
            _order.CustomerPhone = _tbCustPhone?.Text.Trim() ?? _order.CustomerPhone;
            _order.DeliveryAddress = _tbAddress?.Text.Trim() ?? _order.DeliveryAddress;
            _order.DriverId = driverId;
            _order.DriverName = driverName;
            _order.DeliveryFee = fee;

            _db.UpdateOrder(_order);
            return true;
        }

        TextBlock SectionLabel(string t) => new()
        {
            Text = t,
            FontSize = 13,
            FontWeight = FontWeights.Black,
            Foreground = B("#ffd166"),
            Margin = new Thickness(0, 0, 0, 6)
        };

        TextBlock FieldLabel(string t) => new()
        {
            Text = t,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = B("#4a6080")
        };

        TextBox MakeTB(string val) => new()
        {
            Text = val,
            Background = B("#0f1526"),
            Foreground = B("#ffffff"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 8, 10, 8),
            FontSize = 13,
            CaretBrush = B("#ffd166")
        };

        Button MakeQtyBtn(string text, Action click)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new System.Windows.Data.Binding("Background")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            var cp2 = new FrameworkElementFactory(typeof(ContentPresenter));
            cp2.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp2.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp2);
            var tpl = new ControlTemplate(typeof(Button)) { VisualTree = f };

            var btn = new Button
            {
                Content = text,
                Background = B("#1e2d4a"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Width = 26,
                Height = 26,
                FontSize = 15,
                FontWeight = FontWeights.Black,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }

        Button MakeBtn(string text, string bg, SolidColorBrush fg, Action click)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new System.Windows.Data.Binding("Background")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            f.SetBinding(Border.PaddingProperty,
                new System.Windows.Data.Binding("Padding")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            f.SetBinding(Border.BorderBrushProperty,
                new System.Windows.Data.Binding("BorderBrush")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            f.SetBinding(Border.BorderThicknessProperty,
                new System.Windows.Data.Binding("BorderThickness")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
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
                Padding = new Thickness(14, 10, 14, 10),
                FontWeight = FontWeights.Black,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 6, 0),
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }

        void StyleComboItems(ComboBox cb)
        {
            var s = new Style(typeof(ComboBoxItem));
            s.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#0f1526")));
            s.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#eef0f2")));
            s.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(10, 8, 10, 8)));
            var h = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            h.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#1a2640")));
            s.Triggers.Add(h);
            cb.ItemContainerStyle = s;
            cb.Resources.Add(SystemColors.WindowBrushKey, B("#0f1526"));
        }
    }
}