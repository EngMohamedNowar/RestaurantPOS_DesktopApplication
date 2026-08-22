// Views/WarehouseWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using PizzaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Data.Sqlite;

namespace PizzaPOS.Views
{
    public class WarehouseWindow : Window
    {
        readonly InventoryService _svc = new();
        readonly ObservableCollection<Ingredient> _items = new();
        DataGrid _dg = null!;
        TextBox _searchBox = null!;
        TextBlock _totalIngredientsTxt = null!;
        TextBlock _lowStockTxt = null!;
        TextBlock _totalValueTxt = null!;
        TextBlock _categoriesTxt = null!;
        bool _lowOnly;

        public WarehouseWindow()
        {
            Title = "📦 إدارة المستودع";
            Width = 1020; Height = 680;
            MinWidth = 860;
            Background = UiHelper.B("#0f1526");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            BuildUI();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ══ Header ══
            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#a78bfa"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border
            {
                Background = UiHelper.B("#a78bfa"),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 14, 0)
            };
            iconBorder.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#a78bfa"),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            iconBorder.Child = new TextBlock
            {
                Text = "📦",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = "إدارة المستودع",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#eef0f2")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "إدارة المواد الخام والمخزون",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });

            var countBadge = new Border
            {
                Background = UiHelper.B("#1a0e2e"),
                BorderBrush = UiHelper.B("#a78bfa"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            var countTxt = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#a78bfa")
            };
            _items.CollectionChanged += (_, _) =>
                countTxt.Text = $"📦  {_items.Count} مادة";
            countTxt.Text = "📦  0 مادة";
            countBadge.Child = countTxt;

            Grid.SetColumn(iconBorder, 0); hGrid.Children.Add(iconBorder);
            Grid.SetColumn(titleStack, 1); hGrid.Children.Add(titleStack);
            Grid.SetColumn(countBadge, 2); hGrid.Children.Add(countBadge);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Stat Cards ══
            var statsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(18, 14, 18, 0)
            };

            _totalIngredientsTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#a78bfa")
            };
            var cardTotal = UiHelper.MakeStatCard("إجمالي المواد", _totalIngredientsTxt, "#a78bfa", "#130f20");

            _lowStockTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#E63946")
            };
            var cardLow = UiHelper.MakeStatCard("مخزون منخفض", _lowStockTxt, "#E63946", "#1a080a");

            _totalValueTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#06d6a0")
            };
            var cardValue = UiHelper.MakeStatCard("قيمة المخزون", _totalValueTxt, "#06d6a0", "#082010");

            _categoriesTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            };
            var cardCats = UiHelper.MakeStatCard("عدد الفئات", _categoriesTxt, "#ffd166", "#1a1800");

            statsRow.Children.Add(cardTotal);
            statsRow.Children.Add(cardLow);
            statsRow.Children.Add(cardValue);
            statsRow.Children.Add(cardCats);
            Grid.SetRow(statsRow, 1);
            root.Children.Add(statsRow);

            // ══ Search Bar ══
            var searchBar = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 10, 18, 10)
            };
            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition());
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchWrap = new Border
            {
                Background = UiHelper.B("#0f1a2e"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 0, 12, 0)
            };
            var searchRow = new StackPanel { Orientation = Orientation.Horizontal };
            searchRow.Children.Add(new TextBlock
            {
                Text = "🔍",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
            _searchBox = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = UiHelper.B("#eef0f2"),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 9, 0, 9),
                FontSize = 13,
                CaretBrush = UiHelper.B("#a78bfa"),
                Width = 380,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _searchBox.TextChanged += (_, _) => DoSearch();
            searchRow.Children.Add(_searchBox);
            searchWrap.Child = searchRow;
            Grid.SetColumn(searchWrap, 0); searchGrid.Children.Add(searchWrap);

            var lowStockToggle = UiHelper.MakeActionButton(
                _lowOnly ? "📋  كل المواد" : "⚠️  منخفض فقط",
                _lowOnly ? "#1a080a" : "#1e3a5f",
                UiHelper.B(_lowOnly ? "#E63946" : "#7ab8f5"));
            lowStockToggle.Margin = new Thickness(10, 0, 0, 0);
            lowStockToggle.Click += (_, _) =>
            {
                _lowOnly = !_lowOnly;
                DoSearch();
                lowStockToggle.Content = _lowOnly ? "📋  كل المواد" : "⚠️  منخفض فقط";
            };

            var refreshBtn = UiHelper.MakeActionButton("🔄  تحديث", "#1e3a5f", UiHelper.B("#7ab8f5"));
            refreshBtn.Margin = new Thickness(10, 0, 0, 0);
            refreshBtn.Click += (_, _) => { _searchBox.Text = ""; LoadItems(); };
            Grid.SetColumn(lowStockToggle, 1); searchGrid.Children.Add(lowStockToggle);
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(refreshBtn, 2); searchGrid.Children.Add(refreshBtn);

            searchBar.Child = searchGrid;
            Grid.SetRow(searchBar, 2);
            root.Children.Add(searchBar);

            // ══ DataGrid ══
            _dg = BuildGrid();
            _dg.ItemsSource = _items;

            var gridWrapper = new Border
            {
                Margin = new Thickness(18, 14, 18, 0),
                CornerRadius = new CornerRadius(12),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            var scroll = new ScrollViewer
            {
                Content = _dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = UiHelper.B("#0f1526")
            };
            gridWrapper.Child = scroll;
            Grid.SetRow(gridWrapper, 3);
            root.Children.Add(gridWrapper);

            // ══ Action Bar ══
            var actionBar = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 14, 18, 18)
            };
            var actionGrid = new Grid();
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var addBtn = UiHelper.MakeActionButton("➕  إضافة مادة", "#06d6a0", UiHelper.B("#0a0a14"));
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => AddIngredient();

            var editBtn = UiHelper.MakeActionButton("✏️  تعديل", "#ffd166", UiHelper.B("#0a0a14"));
            editBtn.Click += (_, _) => EditIngredient();

            var adjustBtn = UiHelper.MakeActionButton("🔄  تسوية المخزون", "#a78bfa", UiHelper.B("#0a0a14"));
            adjustBtn.Click += (_, _) => AdjustStock();

            var addStockBtn = UiHelper.MakeActionButton("📥  إضافة مخزون", "#1e3a5f", UiHelper.B("#7ab8f5"));
            addStockBtn.Click += (_, _) => AddStock();

            var movementsBtn = UiHelper.MakeActionButton("📊  حركات المخزون", "#130f20", UiHelper.B("#a78bfa"));
            movementsBtn.BorderBrush = UiHelper.B("#a78bfa");
            movementsBtn.BorderThickness = new Thickness(1);
            movementsBtn.Click += (_, _) => ShowMovements();

            var recalcBtn = UiHelper.MakeActionButton("💰  تحديث أسعار المنتجات", "#1a1a0a", UiHelper.B("#ffd166"));
            recalcBtn.BorderBrush = UiHelper.B("#ffd166");
            recalcBtn.BorderThickness = new Thickness(1);
            recalcBtn.Click += (_, _) => RecalculatePrices();

            var manageCatBtn = UiHelper.MakeActionButton("📁  إدارة الفئات", "#1a1800", UiHelper.B("#ffd166"));
            manageCatBtn.BorderBrush = UiHelper.B("#ffd166");
            manageCatBtn.BorderThickness = new Thickness(1);
            manageCatBtn.Click += (_, _) => ManageCategories();

            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(addBtn, 0); actionGrid.Children.Add(addBtn);
            Grid.SetColumn(editBtn, 1); actionGrid.Children.Add(editBtn);
            Grid.SetColumn(adjustBtn, 2); actionGrid.Children.Add(adjustBtn);
            Grid.SetColumn(addStockBtn, 3); actionGrid.Children.Add(addStockBtn);
            Grid.SetColumn(movementsBtn, 4); actionGrid.Children.Add(movementsBtn);
            Grid.SetColumn(recalcBtn, 5); actionGrid.Children.Add(recalcBtn);
            Grid.SetColumn(manageCatBtn, 6); actionGrid.Children.Add(manageCatBtn);

            actionBar.Child = actionGrid;
            Grid.SetRow(actionBar, 4);
            root.Children.Add(actionBar);

            Content = root;
            LoadItems();
        }

        DataGrid BuildGrid()
        {
            var dg = UiHelper.BuildGrid(
                rowBg: "#0d1525",
                altBg: "#0a0f1c",
                headerBg: "#0f1526",
                headerFg: "#ffd166",
                accent: "#a78bfa",
                hoverBg: "#12192e",
                selBg: "#1a2640",
                cellFg: "#eef0f2",
                rowHeight: 48,
                headerHeight: 46);

            var nameCol = new DataGridTextColumn { Header = "المادة", Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) };
            nameCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2")),
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(nameCol);

            var catCol = UiHelper.Col("الفئة", "CategoryName", 110);
            catCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#7ab8f5")),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(catCol);

            var stockCol = new DataGridTemplateColumn
            {
                Header = "المخزون",
                Width = 100
            };
            var stockTpl = new DataTemplate();
            var stockFactory = new FrameworkElementFactory(typeof(TextBlock));
            stockFactory.SetBinding(TextBlock.TextProperty, new Binding("Stock") { StringFormat = "{0:F2}" });
            stockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            stockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            stockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            stockFactory.SetBinding(TextBlock.ForegroundProperty, new Binding("StockStatus")
            {
                Converter = new StockColorConverter()
            });
            stockTpl.VisualTree = stockFactory;
            stockCol.CellTemplate = stockTpl;
            dg.Columns.Add(stockCol);

            var minCol = UiHelper.Col("الحد الأدنى", "MinStock", 100);
            minCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#8892a4")),
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(minCol);

            var unitCol = UiHelper.Col("الوحدة", "Unit", 80);
            unitCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#7ab8f5")),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(unitCol);

            var costCol = UiHelper.Col("التكلفة/وحدة", "CostPerUnit", 110);
            costCol.Binding = new Binding("CostPerUnit") { StringFormat = "{0:F2} ج" };
            costCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#06d6a0")),
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(costCol);

            var statusCol = new DataGridTemplateColumn
            {
                Header = "الحالة",
                Width = 100
            };
            var statusTpl = new DataTemplate();
            var statusFactory = new FrameworkElementFactory(typeof(Border));
            statusFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            statusFactory.SetValue(Border.PaddingProperty, new Thickness(8, 2, 8, 2));
            statusFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            var statusTxt = new FrameworkElementFactory(typeof(TextBlock));
            statusTxt.SetBinding(TextBlock.TextProperty, new Binding("StockStatus"));
            statusTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            statusTxt.SetValue(TextBlock.FontSizeProperty, 11.0);
            statusTxt.SetBinding(TextBlock.ForegroundProperty, new Binding("StockStatus")
            {
                Converter = new StockStatusFgConverter()
            });
            statusFactory.AppendChild(statusTxt);
            statusFactory.SetBinding(Border.BackgroundProperty, new Binding("StockStatus")
            {
                Converter = new StockStatusBgConverter()
            });
            statusTpl.VisualTree = statusFactory;
            statusCol.CellTemplate = statusTpl;
            dg.Columns.Add(statusCol);

            return dg;
        }

        void LoadItems()
        {
            _items.Clear();
            foreach (var i in _svc.GetAll()) _items.Add(i);
            UpdateStats();
        }

        void DoSearch()
        {
            var txt = _searchBox.Text.Trim();
            _items.Clear();
            var list = string.IsNullOrEmpty(txt) ? _svc.GetAll() : _svc.GetAll(txt);
            if (_lowOnly)
            {
                var low = _svc.GetLowStock();
                var lowIds = new HashSet<int>();
                foreach (var l in low) lowIds.Add(l.Id);
                foreach (var i in list)
                    if (lowIds.Contains(i.Id))
                        _items.Add(i);
            }
            else
            {
                foreach (var i in list) _items.Add(i);
            }
            UpdateStats();
        }

        void UpdateStats()
        {
            var all = _svc.GetAll();
            var low = _svc.GetLowStock();
            double totalValue = 0;
            foreach (var i in all) totalValue += i.Stock * i.CostPerUnit;

            _totalIngredientsTxt.Text = all.Count.ToString();
            _lowStockTxt.Text = low.Count.ToString();
            _totalValueTxt.Text = $"{totalValue:F2} ج";

            int catCount = 0;
            try
            {
                using var conn = DatabaseHelper.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM IngredientCategories";
                catCount = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { }
            _categoriesTxt.Text = catCount.ToString();
        }

        void AddIngredient()
        {
            var dlg = new IngredientEditDialog(null) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _svc.Save(dlg.Result!);
            LoadItems();
        }

        void EditIngredient()
        {
            if (_dg.SelectedItem is not Ingredient sel)
            { Notify("اختر مادة أولاً"); return; }
            var dlg = new IngredientEditDialog(sel) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            dlg.Result!.Id = sel.Id;
            _svc.Save(dlg.Result!);
            LoadItems();
        }

        void AddStock()
        {
            if (_dg.SelectedItem is not Ingredient sel)
            { Notify("اختر مادة أولاً"); return; }
            var dlg = new StockInputDialog(sel, "إضافة مخزون", "📥") { Owner = this };
            if (dlg.ShowDialog() != true) return;
            int userId = SessionService.CurrentUser?.Id ?? 0;
            _svc.AddStock(sel.Id, dlg.Qty, dlg.Note, userId);
            LoadItems();
        }

        void AdjustStock()
        {
            if (_dg.SelectedItem is not Ingredient sel)
            { Notify("اختر مادة أولاً"); return; }
            var dlg = new StockAdjustDialog(sel) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            int userId = SessionService.CurrentUser?.Id ?? 0;
            _svc.AdjustStock(sel.Id, dlg.NewQty, dlg.Note, userId);
            LoadItems();
        }

        void ShowMovements()
        {
            var dlg = new MovementsDialog() { Owner = this };
            dlg.ShowDialog();
        }

        void RecalculatePrices()
        {
            var settingsDlg = new ProfitMarginDialog { Owner = this };
            if (settingsDlg.ShowDialog() != true) return;

            double margin = settingsDlg.MarginValue;
            var db = new AppDbContext();
            db.RecalculateAllProductCosts(margin);

            int count = db.GetAllActiveProducts().Count(p => p.Cost > 0);
            MessageBox.Show(
                $"تم تحديث أسعار {count} منتج بنجاح!\n\nهامش الربح: {margin:F0}%\nالمنتجات المحدثة: فقط المنتجات اللي ليها وصفة (مكونات محددة)",
                "تحديث الأسعار",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LoadItems();
        }

        void Notify(string msg) =>
            MessageBox.Show(msg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);

        void AddCategory()
        {
            var dlg = new AddCategoryDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                string name = dlg.CategoryName;
                try
                {
                    using var conn = DatabaseHelper.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO IngredientCategories(Name) VALUES(@n)";
                    cmd.Parameters.AddWithValue("@n", name);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show($"تم إضافة الفئة \"{name}\" بنجاح!", "تم",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void ManageCategories()
        {
            var dlg = new ManageIngredientCategoriesDialog { Owner = this };
            dlg.ShowDialog();
        }
    }

    // ══════════════════════════════════════════════════
    //  StockColorConverter
    // ══════════════════════════════════════════════════
    public class StockColorConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            var s = v?.ToString() ?? "";
            return s.Contains("منخفض") ? UiHelper.B("#E63946") : UiHelper.B("#06d6a0");
        }
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    // ══════════════════════════════════════════════════
    //  StockStatusFgConverter  –  text color for badge
    // ══════════════════════════════════════════════════
    public class StockStatusFgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            var s = v?.ToString() ?? "";
            return s.Contains("منخفض") ? UiHelper.B("#ffffff") : UiHelper.B("#0a0a14");
        }
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    // ══════════════════════════════════════════════════
    //  StockStatusBgConverter  –  badge background
    // ══════════════════════════════════════════════════
    public class StockStatusBgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            var s = v?.ToString() ?? "";
            return s.Contains("منخفض") ? UiHelper.B("#E63946") : UiHelper.B("#06d6a0");
        }
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    // ══════════════════════════════════════════════════
    //  ProfitMarginDialog
    // ══════════════════════════════════════════════════
    public class ProfitMarginDialog : Window
    {
        public double MarginValue { get; private set; } = 50;
        TextBox _tbMargin = null!;

        public ProfitMarginDialog()
        {
            Title = "💰 تحديد هامش الربح";
            Width = 380; Height = 280;
            Background = UiHelper.B("#0a0a14");
            Foreground = Brushes.White;
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;

            var db = new AppDbContext();
            string saved = db.GetSetting("ProfitMargin", "50");
            MarginValue = double.TryParse(saved, out var m) ? m : 50;

            var root = new StackPanel { Margin = new Thickness(20) };

            root.Children.Add(new TextBlock
            {
                Text = "📊 تحديد هامش الربح",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#ffd166"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            });

            root.Children.Add(new TextBlock
            {
                Text = "السعر = التكلفة × (1 + هامش الربح / 100)",
                FontSize = 11, Foreground = UiHelper.B("#b0c4de"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var lbl = UiHelper.FieldLabel("هامش الربح (%)");
            lbl.Margin = new Thickness(0, 0, 0, 4);
            root.Children.Add(lbl);

            _tbMargin = UiHelper.MakeTB(MarginValue.ToString("F0"));
            _tbMargin.Width = 120;
            _tbMargin.HorizontalAlignment = HorizontalAlignment.Left;
            root.Children.Add(_tbMargin);

            root.Children.Add(new TextBlock
            {
                Text = "مثال: تكلفة 30ج + هامش 50% = سعر 45ج",
                FontSize = 10, Foreground = UiHelper.B("#7a8ba8"),
                Margin = new Thickness(0, 8, 0, 16)
            });

            var btnBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var saveBtn = UiHelper.MakeBtn("✅ تحديث الأسعار", "#06d6a0", UiHelper.B("#0a0a14"), () =>
            {
                if (double.TryParse(_tbMargin.Text, out var val) && val >= 0 && val <= 500)
                {
                    MarginValue = val;
                    db.SetSetting("ProfitMargin", val.ToString("F0"));
                    DialogResult = true;
                }
                else
                    MessageBox.Show("أدخل رقم صحيح (0 - 500)", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }, paddingV: 10, margin: 6);

            var cancelBtn = UiHelper.MakeBtn("❌ إلغاء", "#1a2640", UiHelper.B("#b0c4de"), () => DialogResult = false,
                paddingV: 10, margin: 6);

            btnBar.Children.Add(saveBtn);
            btnBar.Children.Add(cancelBtn);
            root.Children.Add(btnBar);

            Content = root;
        }
    }

    // ══════════════════════════════════════════════════
    //  AddCategoryDialog
    // ══════════════════════════════════════════════════
    public class AddCategoryDialog : Window
    {
        public string CategoryName { get; private set; } = "";
        TextBox _tbName = null!;

        public AddCategoryDialog()
        {
            Title = "📁 إضافة فئة جديدة";
            Width = 400;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#0f1526");
            Foreground = Brushes.White;
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Header ──
            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#ffd166"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1a1800"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "📁",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إضافة فئة جديدة",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "أدخل اسم الفئة الجديدة للمواد الخام",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Field ──
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };
            fields.Children.Add(UiHelper.FieldLabel("اسم الفئة *"));
            _tbName = UiHelper.MakeTB("", "#ffd166");
            _tbName.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbName);
            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

            var saveBtn = UiHelper.MakeBtn("📁 إضافة الفئة", "#ffd166", UiHelper.B("#0a0a14"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#ffd166"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.4
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }

        void Save()
        {
            string name = _tbName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("أدخل اسم الفئة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CategoryName = name;
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  ManageIngredientCategoriesDialog
    // ══════════════════════════════════════════════════
    public class ManageIngredientCategoriesDialog : Window
    {
        ListBox _list = null!;

        public ManageIngredientCategoriesDialog()
        {
            Title = "تعديل/حذف فئات المواد الخام";
            Width = 420;
            Height = 500;
            Background = UiHelper.B("#0f1526");
            Foreground = Brushes.White;
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
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#ffd166"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1a1800"),
                CornerRadius = new CornerRadius(10),
                Width = 40, Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "edit", FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = UiHelper.B("#ffd166")
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إدارة فئات المواد الخام",
                FontSize = 16, FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "تعديل الاسم أو حذف أي فئة",
                FontSize = 11, Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            _list = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(16, 12, 16, 12)
            };
            var lstStyle = new Style(typeof(ListBoxItem));
            lstStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, UiHelper.B("#0d1525")));
            lstStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, UiHelper.B("#eef0f2")));
            lstStyle.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(14, 10, 14, 10)));
            lstStyle.Setters.Add(new Setter(ListBoxItem.MarginProperty, new Thickness(0, 0, 0, 4)));
            lstStyle.Setters.Add(new Setter(ListBoxItem.BorderThicknessProperty, new Thickness(1)));
            lstStyle.Setters.Add(new Setter(ListBoxItem.BorderBrushProperty, UiHelper.B("#1e2d4a")));
            var lHov = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            lHov.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, UiHelper.B("#12192e")));
            lHov.Setters.Add(new Setter(ListBoxItem.BorderBrushProperty, UiHelper.B("#ffd166")));
            lstStyle.Triggers.Add(lHov);
            _list.ItemContainerStyle = lstStyle;

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            scroll.Content = _list;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnSp = new StackPanel { Orientation = Orientation.Horizontal };
            btnSp.Children.Add(UiHelper.MakeBtn("إضافة فئة جديدة", "#1a1800", UiHelper.B("#ffd166"), AddNew, 10, 12));
            btnSp.Children.Add(new Border { Width = 10 });
            btnSp.Children.Add(UiHelper.MakeBtn("إغلاق", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a")));
            btnBar.Child = btnSp;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => LoadCategories();
        }

        void LoadCategories()
        {
            _list.Items.Clear();
            try
            {
                using var conn = DatabaseHelper.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Name FROM IngredientCategories ORDER BY Name";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int id = r.GetInt32(0);
                    string name = r.GetString(1);

                    var itemSp = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Tag = id
                    };

                    var nameTxt = new TextBlock
                    {
                        Text = name, FontSize = 13, FontWeight = FontWeights.Bold,
                        Foreground = UiHelper.B("#eef0f2"),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 12, 0)
                    };
                    itemSp.Children.Add(nameTxt);

                    var editBtn = UiHelper.MakeBtn("edit", "#1a2640", UiHelper.B("#ffd166"),
                        () => EditCategory(id, name), 6, 6);
                    editBtn.MinWidth = 32; editBtn.MinHeight = 32;
                    itemSp.Children.Add(editBtn);

                    var delBtn = UiHelper.MakeBtn("X", "#2a1a1a", UiHelper.B("#E63946"),
                        () => DeleteCategory(id, name), 6, 6);
                    delBtn.MinWidth = 32; delBtn.MinHeight = 32;
                    itemSp.Children.Add(delBtn);

                    _list.Items.Add(new ListBoxItem { Content = itemSp, Tag = id });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void AddNew()
        {
            var dlg = new AddCategoryDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var conn = DatabaseHelper.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO IngredientCategories(Name) VALUES(@n)";
                    cmd.Parameters.AddWithValue("@n", dlg.CategoryName);
                    cmd.ExecuteNonQuery();
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void EditCategory(int id, string oldName)
        {
            var dlg = new EditCategoryDialog(oldName) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var conn = DatabaseHelper.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE IngredientCategories SET Name=@n WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@n", dlg.CategoryName);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void DeleteCategory(int id, string name)
        {
            int count = 0;
            try
            {
                using var conn = DatabaseHelper.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Ingredients WHERE CategoryId=@id";
                cmd.Parameters.AddWithValue("@id", id);
                count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { }

            string msg = count > 0
                ? $"الفئة \"{name}\" فيها {count} مادة خام.\nهل تحذف الفئة والمواد المرتبطة بيها؟"
                : $"هل أنت متأكد من حذف الفئة \"{name}\"؟";

            if (MessageBox.Show(msg, "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using var conn = DatabaseHelper.Open();
                    using var tx = conn.BeginTransaction();
                    try
                    {
                        var del1 = conn.CreateCommand(); del1.Transaction = tx;
                        del1.CommandText = "DELETE FROM Ingredients WHERE CategoryId=@id";
                        del1.Parameters.AddWithValue("@id", id);
                        del1.ExecuteNonQuery();

                        var del2 = conn.CreateCommand(); del2.Transaction = tx;
                        del2.CommandText = "DELETE FROM IngredientCategories WHERE Id=@id";
                        del2.Parameters.AddWithValue("@id", id);
                        del2.ExecuteNonQuery();

                        tx.Commit();
                        LoadCategories();
                    }
                    catch { tx.Rollback(); throw; }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════
    //  EditCategoryDialog
    // ══════════════════════════════════════════════════
    public class EditCategoryDialog : Window
    {
        public string CategoryName { get; private set; } = "";
        TextBox _tbName = null!;

        public EditCategoryDialog(string currentName)
        {
            Title = "تعديل اسم الفئة";
            Width = 380;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#0f1526");
            Foreground = Brushes.White;
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#ffd166"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1a1800"),
                CornerRadius = new CornerRadius(10),
                Width = 40, Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "edit", FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = UiHelper.B("#ffd166")
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "تعديل الفئة",
                FontSize = 16, FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"العنوان الحالي: {currentName}",
                FontSize = 11, Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };
            fields.Children.Add(UiHelper.FieldLabel("اسم الفئة *"));
            _tbName = UiHelper.MakeTB("", "#ffd166");
            _tbName.Text = currentName;
            _tbName.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbName);
            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

            var saveBtn = UiHelper.MakeBtn("حفظ التعديل", "#ffd166", UiHelper.B("#0a0a14"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#ffd166"),
                BlurRadius = 16, ShadowDepth = 0, Opacity = 0.4
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }

        void Save()
        {
            string name = _tbName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("أدخل اسم الفئة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CategoryName = name;
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  IngredientEditDialog
    // ══════════════════════════════════════════════════
    public class IngredientEditDialog : Window
    {
        public Ingredient? Result { get; private set; }

        readonly Ingredient? _editing;
        TextBox _tbName = null!;
        TextBox _tbUnit = null!;
        TextBox _tbStock = null!;
        TextBox _tbMin = null!;
        TextBox _tbCost = null!;
        ComboBox _cbCategory = null!;

        public IngredientEditDialog(Ingredient? editing)
        {
            _editing = editing;
            Title = editing == null ? "إضافة مادة جديدة" : "تعديل المادة";
            Width = 460;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#0f1526");
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

            bool isAdd = _editing == null;
            var accentHex = isAdd ? "#06d6a0" : "#ffd166";

            // ── Header ──
            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B(accentHex),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B(accentHex),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = isAdd ? "➕" : "✏️",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = isAdd ? "إضافة مادة جديدة" : "تعديل بيانات المادة",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B(accentHex)
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = isAdd ? "أدخل بيانات المادة الجديدة" : $"تعديل: {_editing!.Name}",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            fields.Children.Add(UiHelper.FieldLabel("اسم المادة *"));
            _tbName = UiHelper.MakeTB(_editing?.Name ?? "", "#a78bfa");
            _tbName.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbName);

            fields.Children.Add(UiHelper.FieldLabel("الفئة *"));
            _cbCategory = new ComboBox
            {
                Background = UiHelper.B("#0f1526"),
                Foreground = UiHelper.B("#eef0f2"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 9, 10, 9),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 14),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#0f1526")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, UiHelper.B("#eef0f2")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(12, 9, 12, 9)));
            var hov = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#1a2640")));
            itemStyle.Triggers.Add(hov);
            var selT = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selT.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#a78bfa")));
            selT.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, UiHelper.B("#0a0a14")));
            itemStyle.Triggers.Add(selT);
            _cbCategory.ItemContainerStyle = itemStyle;
            _cbCategory.Resources.Add(SystemColors.WindowBrushKey, UiHelper.B("#0f1526"));
            _cbCategory.Resources.Add(SystemColors.HighlightBrushKey, UiHelper.B("#a78bfa"));
            _cbCategory.Resources.Add(SystemColors.ControlBrushKey, UiHelper.B("#0f1526"));

            LoadCategories();

            fields.Children.Add(_cbCategory);

            fields.Children.Add(UiHelper.FieldLabel("الوحدة (كجم / لتر / قطعة ...) *"));
            _tbUnit = UiHelper.MakeTB(_editing?.Unit ?? "", "#a78bfa");
            _tbUnit.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbUnit);

            var numRow = new Grid();
            numRow.ColumnDefinitions.Add(new ColumnDefinition());
            numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            numRow.ColumnDefinitions.Add(new ColumnDefinition());

            var stockPanel = new StackPanel();
            stockPanel.Children.Add(UiHelper.FieldLabel("المخزون الحالي *"));
            _tbStock = UiHelper.MakeTB(_editing?.Stock.ToString("F2") ?? "0", "#a78bfa");
            _tbStock.Margin = new Thickness(0, 4, 0, 14);
            stockPanel.Children.Add(_tbStock);
            Grid.SetColumn(stockPanel, 0);
            numRow.Children.Add(stockPanel);

            var minPanel = new StackPanel();
            minPanel.Children.Add(UiHelper.FieldLabel("الحد الأدنى *"));
            _tbMin = UiHelper.MakeTB(_editing?.MinStock.ToString("F2") ?? "0", "#a78bfa");
            _tbMin.Margin = new Thickness(0, 4, 0, 14);
            minPanel.Children.Add(_tbMin);
            Grid.SetColumn(minPanel, 2);
            numRow.Children.Add(minPanel);

            fields.Children.Add(numRow);

            fields.Children.Add(UiHelper.FieldLabel("التكلفة للوحدة (ج) *"));
            _tbCost = UiHelper.MakeTB(_editing?.CostPerUnit.ToString("F2") ?? "0", "#a78bfa");
            _tbCost.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbCost);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

            var saveBtn = UiHelper.MakeBtn(
                isAdd ? "➕ إضافة" : "💾 حفظ",
                accentHex, UiHelper.B("#0a0a14"), Save);

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }

        void LoadCategories()
        {
            try
            {
                using var conn = DatabaseHelper.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Name FROM IngredientCategories ORDER BY Name";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int id = r.GetInt32(0);
                    string name = r.GetString(1);
                    var item = new ComboBoxItem { Content = name, Tag = id };
                    _cbCategory.Items.Add(item);
                    if (_editing != null && id == _editing.CategoryId)
                        _cbCategory.SelectedItem = item;
                }
            }
            catch { }
            if (_cbCategory.SelectedItem == null && _cbCategory.Items.Count > 0)
                _cbCategory.SelectedIndex = 0;
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbName.Text))
            {
                MessageBox.Show("أدخل اسم المادة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (_cbCategory.SelectedItem is not ComboBoxItem catItem)
            {
                MessageBox.Show("اختر الفئة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(_tbUnit.Text))
            {
                MessageBox.Show("أدخل الوحدة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!double.TryParse(_tbStock.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double stock) || stock < 0)
            {
                MessageBox.Show("المخزون غير صحيح", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!double.TryParse(_tbMin.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double min) || min < 0)
            {
                MessageBox.Show("الحد الأدنى غير صحيح", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!double.TryParse(_tbCost.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double cost) || cost < 0)
            {
                MessageBox.Show("التكلفة غير صحيحة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            int catId = (int)catItem.Tag!;
            string catName = catItem.Content?.ToString() ?? "";

            Result = new Ingredient
            {
                CategoryId = catId,
                CategoryName = catName,
                Name = _tbName.Text.Trim(),
                Unit = _tbUnit.Text.Trim(),
                Stock = stock,
                MinStock = min,
                CostPerUnit = cost
            };
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  StockInputDialog  –  Add Stock
    // ══════════════════════════════════════════════════
    public class StockInputDialog : Window
    {
        public double Qty { get; private set; }
        public string Note { get; private set; } = "";

        readonly Ingredient _ing;
        TextBox _tbQty = null!;
        TextBox _tbNote = null!;

        public StockInputDialog(Ingredient ing, string title, string icon)
        {
            _ing = ing;
            Title = $"{icon} {title}";
            Width = 400;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#0f1526");
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

            // ── Header ──
            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#1e3a5f"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1e3a5f"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "📥",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إضافة مخزون",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#7ab8f5")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"المادة: {_ing.Name}  •  المخزون الحالي: {_ing.Stock:F2} {_ing.Unit}",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            fields.Children.Add(UiHelper.FieldLabel($"الكمية المضافة ({_ing.Unit}) *"));
            _tbQty = UiHelper.MakeTB("", "#1e3a5f");
            _tbQty.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbQty);

            fields.Children.Add(UiHelper.FieldLabel("ملاحظة"));
            _tbNote = UiHelper.MakeTB("", "#1e3a5f");
            _tbNote.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbNote);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

            var saveBtn = UiHelper.MakeBtn("📥 إضافة المخزون", "#1e3a5f", UiHelper.B("#7ab8f5"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#1e3a5f"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.4
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbQty.Focus();
        }

        void Save()
        {
            if (!double.TryParse(_tbQty.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صحيحة أكبر من صفر", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            Qty = qty;
            Note = _tbNote.Text.Trim();
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  StockAdjustDialog  –  Adjust Stock
    // ══════════════════════════════════════════════════
    public class StockAdjustDialog : Window
    {
        public double NewQty { get; private set; }
        public string Note { get; private set; } = "";

        readonly Ingredient _ing;
        TextBox _tbQty = null!;
        TextBox _tbNote = null!;

        public StockAdjustDialog(Ingredient ing)
        {
            _ing = ing;
            Title = "🔄 تسوية المخزون";
            Width = 400;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#0f1526");
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

            // ── Header ──
            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#a78bfa"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#a78bfa"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "🔄",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "تسوية المخزون",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#a78bfa")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"المادة: {_ing.Name}  •  الحالي: {_ing.Stock:F2} {_ing.Unit}",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            fields.Children.Add(UiHelper.FieldLabel($"الكمية الجديدة ({_ing.Unit}) *"));
            _tbQty = UiHelper.MakeTB(_ing.Stock.ToString("F2"), "#a78bfa");
            _tbQty.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbQty);

            fields.Children.Add(UiHelper.FieldLabel("ملاحظة"));
            _tbNote = UiHelper.MakeTB("", "#a78bfa");
            _tbNote.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbNote);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

            var saveBtn = UiHelper.MakeBtn("💾 حفظ التسوية", "#a78bfa", UiHelper.B("#0a0a14"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#a78bfa"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.4
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbQty.Focus();
        }

        void Save()
        {
            if (!double.TryParse(_tbQty.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double qty) || qty < 0)
            {
                MessageBox.Show("أدخل كمية صحيحة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            NewQty = qty;
            Note = _tbNote.Text.Trim();
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  MovementsDialog
    // ══════════════════════════════════════════════════
    public class MovementsDialog : Window
    {
        public MovementsDialog()
        {
            Title = "📊 حركات المخزون";
            Width = 820; Height = 560;
            MinWidth = 700;
            Background = UiHelper.B("#0f1526");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            BuildUI();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());

            // ── Header ──
            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#a78bfa"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#a78bfa"),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "📊",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "حركات المخزون",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "آخر 7 أيام",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── DataGrid ──
            var dg = UiHelper.BuildGrid(
                rowBg: "#0d1525",
                altBg: "#0a0f1c",
                headerBg: "#0f1526",
                headerFg: "#ffd166",
                accent: "#a78bfa",
                hoverBg: "#12192e",
                selBg: "#1a2640",
                cellFg: "#eef0f2",
                rowHeight: 42,
                headerHeight: 44);

            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "المادة",
                Binding = new Binding("Ingredient"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "النوع",
                Binding = new Binding("TypeDisplay"),
                Width = 100,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#a78bfa")),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الكمية",
                Binding = new Binding("Qty") { StringFormat = "{0:F2}" },
                Width = 90,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2"))
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "ملاحظة",
                Binding = new Binding("Note"),
                Width = 160,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#8892a4")),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "التاريخ",
                Binding = new Binding("CreatedAt"),
                Width = 140,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#4a6080")),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            var svc = new InventoryService();
            var movements = svc.GetMovements(7);
            dg.ItemsSource = movements;

            var scroll = new ScrollViewer
            {
                Content = dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = UiHelper.B("#0f1526")
            };
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            Content = root;
        }
    }
}
