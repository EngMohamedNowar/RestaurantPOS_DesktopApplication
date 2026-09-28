// WarehouseWindow.cs
// Main warehouse screen: ingredient list, search, stats, stock actions.
//
// Was one of twelve types packed into Views/WarehouseWindow.cs. Split into
// one type per file; the body below is unchanged.

using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using PizzaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        readonly AppDbContext _db = new();
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
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 0 addBtn
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 1 editBtn
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 2 adjustBtn
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 3 addStockBtn
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 4 movementsBtn
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 5 manageCatBtn
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition());                            // 6 spacer
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 7 deleteBtn

            var addBtn = UiHelper.MakeActionButton("➕  إضافة مادة", "#06d6a0", UiHelper.B("#0a0a14"));
            addBtn.HorizontalAlignment = HorizontalAlignment.Left;
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => AddIngredient();

            var editBtn = UiHelper.MakeActionButton("✏️  تعديل", "#ffd166", UiHelper.B("#0a0a14"));
            editBtn.HorizontalAlignment = HorizontalAlignment.Left;
            editBtn.Click += (_, _) => EditIngredient();

            var adjustBtn = UiHelper.MakeActionButton("🔄  تسوية المخزون", "#a78bfa", UiHelper.B("#0a0a14"));
            adjustBtn.HorizontalAlignment = HorizontalAlignment.Left;
            adjustBtn.Click += (_, _) => AdjustStock();

            var addStockBtn = UiHelper.MakeActionButton("📥  إضافة مخزون", "#1e3a5f", UiHelper.B("#7ab8f5"));
            addStockBtn.HorizontalAlignment = HorizontalAlignment.Left;
            addStockBtn.Click += (_, _) => AddStock();

            var movementsBtn = UiHelper.MakeActionButton("📊  حركات المخزون", "#130f20", UiHelper.B("#a78bfa"));
            movementsBtn.HorizontalAlignment = HorizontalAlignment.Left;
            movementsBtn.BorderBrush = UiHelper.B("#a78bfa");
            movementsBtn.BorderThickness = new Thickness(1);
            movementsBtn.Click += (_, _) => ShowMovements();

            var manageCatBtn = UiHelper.MakeActionButton("📁  إدارة الفئات", "#1a1800", UiHelper.B("#ffd166"));
            manageCatBtn.HorizontalAlignment = HorizontalAlignment.Left;
            manageCatBtn.BorderBrush = UiHelper.B("#ffd166");
            manageCatBtn.BorderThickness = new Thickness(1);
            manageCatBtn.Click += (_, _) => ManageCategories();

            var deleteBtn = UiHelper.MakeActionButton("🗑  حذف مادة", "#2a1a1a", UiHelper.B("#E63946"));
            deleteBtn.HorizontalAlignment = HorizontalAlignment.Left;
            deleteBtn.BorderBrush = UiHelper.B("#E63946");
            deleteBtn.BorderThickness = new Thickness(1);
            deleteBtn.Click += (_, _) => DeleteIngredient();

            Grid.SetColumn(addBtn, 0); actionGrid.Children.Add(addBtn);
            Grid.SetColumn(editBtn, 1); actionGrid.Children.Add(editBtn);
            Grid.SetColumn(adjustBtn, 2); actionGrid.Children.Add(adjustBtn);
            Grid.SetColumn(addStockBtn, 3); actionGrid.Children.Add(addStockBtn);
            Grid.SetColumn(movementsBtn, 4); actionGrid.Children.Add(movementsBtn);
            Grid.SetColumn(manageCatBtn, 5); actionGrid.Children.Add(manageCatBtn);
            Grid.SetColumn(deleteBtn, 7); actionGrid.Children.Add(deleteBtn);

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
            catch (Exception ex)
            {
                // العرض صفر الوهمي: كأن مفيش فئات خالص، وده مضلّل
                // في تقرير جرد. سجّل واعرض شرطة بدل رقم مخترع.
                AppLogger.Warn($"Category count unavailable in warehouse summary: {ex.Message}");
                _categoriesTxt.Text = "—";
                return;
            }
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

        void DeleteIngredient()
        {
            if (_dg.SelectedItem is not Ingredient sel)
            { Notify("اختر مادة أولاً"); return; }

            int usageCount;
            try { usageCount = _db.GetIngredientUsageCount(sel.Id); }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string msg = usageCount > 0
                ? $"المادة \"{sel.Name}\" مستخدمة في وصفة {usageCount} منتج.\nهل تحذف المادة وتشيلها من كل الوصفات المرتبطة بيها؟"
                : $"هل أنت متأكد من حذف المادة \"{sel.Name}\"؟";

            if (MessageBox.Show(msg, "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                _db.DeleteIngredient(sel.Id);
                LoadItems();
            }
            catch (Exception ex)
            {
                AppLogger.Error("DeleteIngredient failed", ex);
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
}
