// Views/ProductsWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PizzaPOS.Views
{
    public class ProductsWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<Product> _products = new();
        DataGrid _dg = null!;
        TextBox _searchBox = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public ProductsWindow()
        {
            Title = "إدارة المنتجات";
            Width = 940; Height = 660;
            MinWidth = 800;
            Background = B("#070b14");
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
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ══ Header ══
            var header = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border
            {
                Background = B("#FF6B35"),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 14, 0)
            };
            iconBorder.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
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
                Text = "إدارة المنتجات",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "إضافة وتعديل وحذف منتجات القائمة",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });

            var countBadge = new Border
            {
                Background = B("#1a0e08"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            var countTxt = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Black,
                Foreground = B("#FF6B35")
            };
            _products.CollectionChanged += (_, _) =>
                countTxt.Text = $"📦  {_products.Count} منتج";
            countTxt.Text = "📦  0 منتج";
            countBadge.Child = countTxt;

            Grid.SetColumn(iconBorder, 0); hGrid.Children.Add(iconBorder);
            Grid.SetColumn(titleStack, 1); hGrid.Children.Add(titleStack);
            Grid.SetColumn(countBadge, 2); hGrid.Children.Add(countBadge);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Search Bar ══
            var searchBar = new Border
            {
                Background = B("#090e1a"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 10, 18, 10)
            };
            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition());
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchWrap = new Border
            {
                Background = B("#0f1a2e"),
                BorderBrush = B("#1e2d4a"),
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
                Foreground = B("#eef0f2"),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 9, 0, 9),
                FontSize = 13,
                CaretBrush = B("#FF6B35"),
                Width = 380,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _searchBox.TextChanged += (_, _) => DoSearch();
            searchRow.Children.Add(_searchBox);
            searchWrap.Child = searchRow;
            Grid.SetColumn(searchWrap, 0); searchGrid.Children.Add(searchWrap);

            var refreshBtn = MakeActionButton("🔄  تحديث", "#1e3a5f", B("#7ab8f5"));
            refreshBtn.Margin = new Thickness(10, 0, 0, 0);
            refreshBtn.Click += (_, _) => { _searchBox.Text = ""; LoadProducts(); };
            Grid.SetColumn(refreshBtn, 1); searchGrid.Children.Add(refreshBtn);

            searchBar.Child = searchGrid;
            Grid.SetRow(searchBar, 1);
            root.Children.Add(searchBar);

            // ══ DataGrid ══
            _dg = BuildGrid();
            _dg.ItemsSource = _products;
            _dg.MouseDoubleClick += (_, _) => { if (_dg.SelectedItem is Product) EditProduct(); };

            var gridWrapper = new Border
            {
                Margin = new Thickness(18, 14, 18, 0),
                CornerRadius = new CornerRadius(12),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            var scroll = new ScrollViewer
            {
                Content = _dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = B("#070b14")
            };
            gridWrapper.Child = scroll;
            Grid.SetRow(gridWrapper, 2);
            root.Children.Add(gridWrapper);

            // ══ Action Bar ══
            var actionBar = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 14, 18, 18)
            };
            var actionGrid = new Grid();
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addBtn = MakeActionButton("➕  إضافة منتج", "#FF6B35", B("#fff8f5"));
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => AddProduct();

            var editBtn = MakeActionButton("✏️  تعديل", "#1e3a5f", B("#7ab8f5"));
            editBtn.Click += (_, _) => EditProduct();

            var sizesBtn = MakeActionButton("📏  أحجام وإضافات", "#130f20", B("#a78bfa"));
            sizesBtn.BorderBrush = B("#a78bfa");
            sizesBtn.BorderThickness = new Thickness(1);
            sizesBtn.Click += (_, _) =>
            {
                if (_dg.SelectedItem is not Product p)
                { Notify("اختر منتج أولاً"); return; }
                new ProductSizesWindow(p) { Owner = this }.ShowDialog();
            };

            var delBtn = MakeActionButton("🗑  حذف", "#1a080a", B("#E63946"));
            delBtn.BorderBrush = B("#E63946");
            delBtn.BorderThickness = new Thickness(1);
            delBtn.Click += (_, _) => DeleteProduct();

            Grid.SetColumn(addBtn, 0); actionGrid.Children.Add(addBtn);
            Grid.SetColumn(editBtn, 1); actionGrid.Children.Add(editBtn);
            Grid.SetColumn(sizesBtn, 2); actionGrid.Children.Add(sizesBtn);
            Grid.SetColumn(delBtn, 4); actionGrid.Children.Add(delBtn);

            actionBar.Child = actionGrid;
            Grid.SetRow(actionBar, 3);
            root.Children.Add(actionBar);

            Content = root;
            LoadProducts();
        }

        DataGrid BuildGrid()
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = Brushes.Transparent,
                RowBackground = B("#0b1020"),
                AlternatingRowBackground = B("#080d1a"),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = B("#111c35"),
                BorderThickness = new Thickness(0),
                CanUserAddRows = false,
                RowHeight = 48,
                ColumnHeaderHeight = 46,
                FontSize = 13,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            var hs = new Style(typeof(DataGridColumnHeader));
            hs.Setters.Add(new Setter(Control.BackgroundProperty, B("#0c1530")));
            hs.Setters.Add(new Setter(Control.ForegroundProperty, B("#ffd166")));
            hs.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            hs.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
            hs.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 0, 16, 0)));
            hs.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 2)));
            hs.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, B("#FF6B35")));
            dg.ColumnHeaderStyle = hs;

            var rs = new Style(typeof(DataGridRow));
            rs.Setters.Add(new Setter(DataGridRow.ForegroundProperty, B("#c8d8f0")));
            var hov = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B("#111d38")));
            rs.Triggers.Add(hov);
            var sel = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B("#1a2d50")));
            rs.Triggers.Add(sel);
            dg.RowStyle = rs;

            var cs = new Style(typeof(DataGridCell));
            cs.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cs.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(16, 0, 16, 0)));
            cs.Setters.Add(new Setter(DataGridCell.ForegroundProperty, B("#c8d8f0")));
            cs.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));
            var csel = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            csel.Setters.Add(new Setter(DataGridCell.BackgroundProperty, B("#1a2d50")));
            csel.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent));
            cs.Triggers.Add(csel);
            dg.CellStyle = cs;

            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "  الأيقونة",
                Binding = new Binding("Icon"),
                Width = 75,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.FontSizeProperty, 22.0),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "اسم المنتج",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B("#eef0f2")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الفئة",
                Binding = new Binding("CategoryName"),
                Width = 130,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B("#7ab8f5")),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "💵 السعر",
                Binding = new Binding("Price") { StringFormat = "{0:F2} ج" },
                Width = 110,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B("#06d6a0")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "📊 التكلفة",
                Binding = new Binding("Cost") { StringFormat = "{0:F2} ج" },
                Width = 110,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B("#fb923c")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            return dg;
        }

        void LoadProducts()
        {
            _products.Clear();
            foreach (var p in _db.GetProducts()) _products.Add(p);
        }

        void DoSearch()
        {
            var txt = _searchBox.Text.Trim();
            _products.Clear();
            foreach (var p in _db.GetProducts(null,
                string.IsNullOrEmpty(txt) ? null : txt))
                _products.Add(p);
        }

        void AddProduct()
        {
            var dlg = new ProductEditDialog(null, _db) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveProduct(dlg.Result!);
            LoadProducts();
        }

        void EditProduct()
        {
            if (_dg.SelectedItem is not Product sel)
            { Notify("اختر منتج أولاً"); return; }
            var dlg = new ProductEditDialog(sel, _db) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            dlg.Result!.Id = sel.Id;
            _db.SaveProduct(dlg.Result!);
            LoadProducts();
        }

        void DeleteProduct()
        {
            if (_dg.SelectedItem is not Product sel)
            { Notify("اختر منتج أولاً"); return; }
            if (MessageBox.Show($"هل تريد حذف \"{sel.Name}\"؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            _db.DeleteProduct(sel.Id);
            LoadProducts();
        }

        void Notify(string msg) =>
            MessageBox.Show(msg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);

        Button MakeActionButton(string text, string bgHex, Brush fg)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            f.SetBinding(Border.PaddingProperty,
                new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderBrushProperty,
                new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderThicknessProperty,
                new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp);
            return new Button
            {
                Content = text,
                Background = B(bgHex),
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(18, 10, 18, 10),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0),
                Template = new ControlTemplate(typeof(Button)) { VisualTree = f }
            };
        }
    }

    // ══════════════════════════════════════════════════
    //  ProductEditDialog
    // ══════════════════════════════════════════════════
    public class ProductEditDialog : Window
    {
        public Product? Result { get; private set; }

        readonly AppDbContext _db;
        readonly Product? _editing;

        TextBox _tbName = null!;
        TextBox _tbPrice = null!;
        TextBox _tbCost = null!;
        TextBox _tbIcon = null!;
        ListBox _catList = null!;
        TextBlock _previewIcon = null!;
        TextBlock _previewName = null!;
        TextBlock _previewPrice = null!;
        TextBlock _previewCat = null!;
        TextBlock _iconPreviewTxt = null!;   // المعاينة الصغيرة بجانب الـ textbox
        WrapPanel _pickerWrap = null!;
        (string Em, string Bg, string Br)[] _emojiGroups = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        static string CatIcon(string? icon) =>
            string.IsNullOrWhiteSpace(icon) ? "📋" : icon;

        public ProductEditDialog(Product? editing, AppDbContext db)
        {
            _editing = editing;
            _db = db;
            Title = editing == null ? "إضافة منتج جديد" : "تعديل منتج";
            Width = 600; Height = 680;
            Background = B("#070b14");
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

            bool isAdd = _editing == null;
            var accentHex = isAdd ? "#06d6a0" : "#ffd166";

            // ══ Header ══
            var header = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = B(accentHex),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            hIcon.Child = new TextBlock
            {
                Text = isAdd ? "➕" : "✏️",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hSp.Children.Add(hIcon);
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = isAdd ? "إضافة منتج جديد" : "تعديل بيانات المنتج",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = isAdd ? "أدخل بيانات المنتج الجديد" : $"تعديل: {_editing!.Name}",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Content ══
            var content = new Grid { Margin = new Thickness(0) };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            content.ColumnDefinitions.Add(new ColumnDefinition());

            // ── Left Panel ──
            var leftPanel = new StackPanel { Background = B("#090e1a") };

            var previewCard = new Border
            {
                Background = B("#0f1a2e"),
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 20, 16, 20),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var previewStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            var iconCircle = new Border
            {
                Background = B("#1a2d50"),
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(50),
                Width = 80,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _previewIcon = new TextBlock
            {
                Text = _editing?.Icon ?? "🍕",
                FontSize = 38,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconCircle.Child = _previewIcon;

            _previewName = new TextBlock
            {
                Text = _editing?.Name ?? "اسم المنتج",
                FontSize = 13,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _previewPrice = new TextBlock
            {
                Text = $"{_editing?.Price ?? 0:F2} ج",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#06d6a0"),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            _previewCat = new TextBlock
            {
                Text = _editing != null ? _editing.CategoryName ?? "الفئة" : "الفئة",
                FontSize = 10,
                Foreground = B("#7ab8f5"),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            previewStack.Children.Add(iconCircle);
            previewStack.Children.Add(_previewName);
            previewStack.Children.Add(_previewPrice);
            previewStack.Children.Add(_previewCat);
            previewCard.Child = previewStack;
            leftPanel.Children.Add(previewCard);

            leftPanel.Children.Add(new TextBlock
            {
                Text = "اختر الفئة *",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                Margin = new Thickness(14, 14, 14, 6)
            });

            _catList = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = B("#eef0f2"),
                FontSize = 13,
                Margin = new Thickness(10, 0, 10, 10)
            };

            var itemStyle = new Style(typeof(ListBoxItem));
            itemStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, B("#0f1a2e")));
            itemStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, B("#eef0f2")));
            itemStyle.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(12, 9, 12, 9)));
            itemStyle.Setters.Add(new Setter(ListBoxItem.MarginProperty, new Thickness(0, 2, 0, 2)));
            itemStyle.Setters.Add(new Setter(ListBoxItem.FontSizeProperty, 13.0));
            var itemHov = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            itemHov.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, B("#1a2d50")));
            itemStyle.Triggers.Add(itemHov);
            var itemSel = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            itemSel.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, B("#FF6B35")));
            itemSel.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.White));
            itemStyle.Triggers.Add(itemSel);

            var itemFactory = new FrameworkElementFactory(typeof(Border));
            itemFactory.SetBinding(Border.BackgroundProperty,
                new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            itemFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            itemFactory.SetBinding(Border.PaddingProperty,
                new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var itemCP = new FrameworkElementFactory(typeof(ContentPresenter));
            itemCP.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            itemFactory.AppendChild(itemCP);
            itemStyle.Setters.Add(new Setter(ListBoxItem.TemplateProperty,
                new ControlTemplate(typeof(ListBoxItem)) { VisualTree = itemFactory }));

            _catList.ItemContainerStyle = itemStyle;
            _catList.Background = Brushes.Transparent;

            var cats = _db.GetCategories();
            foreach (var cat in cats)
            {
                var item = new ListBoxItem
                {
                    Content = $"{CatIcon(cat.Icon)}  {cat.Name}",
                    Tag = cat
                };
                _catList.Items.Add(item);
                if (_editing != null && cat.Id == _editing.CategoryId)
                    _catList.SelectedItem = item;
            }
            if (_catList.SelectedItem == null && _catList.Items.Count > 0)
                _catList.SelectedIndex = 0;

            // ✅ الفيكس الرئيسي: لما تختار فئة كل العناصر تتحدث بأيقونتها
            _catList.SelectionChanged += (_, _) =>
            {
                if (_catList.SelectedItem is not ListBoxItem li || li.Tag is not Category c) return;

                var newIcon = CatIcon(c.Icon);

                // 1. اسم الفئة في المعاينة
                _previewCat.Text = $"{newIcon} {c.Name}";

                // 2. الأيقونة الكبيرة في الدائرة
                _previewIcon.Text = newIcon;

                // 3. الـ textbox والمعاينة الصغيرة بجانبه
                _tbIcon.Text = newIcon;
                if (_iconPreviewTxt != null)
                    _iconPreviewTxt.Text = newIcon;

                // 4. تحديث الـ emoji picker — تظليل الخلية المناسبة
                if (_pickerWrap != null && _emojiGroups != null)
                {
                    int idx = 0;
                    foreach (Border b in _pickerWrap.Children)
                    {
                        var grp = _emojiGroups[idx++];
                        if (grp.Em == newIcon)
                        {
                            b.Background = B(grp.Br);
                            b.BorderThickness = new Thickness(2);
                            if (b.Effect is DropShadowEffect fx) fx.BlurRadius = 14;
                        }
                        else
                        {
                            b.Background = B(grp.Bg);
                            b.BorderThickness = new Thickness(1);
                            if (b.Effect is DropShadowEffect fx) fx.BlurRadius = 0;
                        }
                    }
                }
            };

            var catScroll = new ScrollViewer
            {
                Content = _catList,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 200
            };
            leftPanel.Children.Add(catScroll);

            Grid.SetColumn(leftPanel, 0);
            content.Children.Add(leftPanel);

            // ── Right: Fields ──
            var rightScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var fields = new StackPanel { Margin = new Thickness(18, 16, 18, 8) };

            fields.Children.Add(FieldLabel("اسم المنتج *"));
            _tbName = MakeTB(_editing?.Name ?? "");
            _tbName.Margin = new Thickness(0, 4, 0, 16);
            _tbName.TextChanged += (_, _) =>
                _previewName.Text = string.IsNullOrEmpty(_tbName.Text) ? "اسم المنتج" : _tbName.Text;
            fields.Children.Add(_tbName);

            fields.Children.Add(FieldLabel("💵 سعر البيع (ج) *"));
            _tbPrice = MakeTB(_editing?.Price.ToString("F2") ?? "0.00");
            _tbPrice.Margin = new Thickness(0, 4, 0, 16);
            _tbPrice.TextChanged += (_, _) =>
            {
                if (double.TryParse(_tbPrice.Text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double v))
                    _previewPrice.Text = $"{v:F2} ج";
            };
            fields.Children.Add(_tbPrice);

            fields.Children.Add(FieldLabel("📊 التكلفة (ج)"));
            _tbCost = MakeTB(_editing?.Cost.ToString("F2") ?? "0.00");
            _tbCost.Margin = new Thickness(0, 4, 0, 16);
            fields.Children.Add(_tbCost);

            fields.Children.Add(FieldLabel("الأيقونة *"));
            var iconInputRow = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            iconInputRow.ColumnDefinitions.Add(new ColumnDefinition());
            iconInputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            iconInputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

            _tbIcon = MakeTB(_editing?.Icon ?? "🍕");
            _tbIcon.FontSize = 18;
            _tbIcon.TextAlignment = TextAlignment.Center;
            _tbIcon.TextChanged += (_, _) =>
            {
                var v = _tbIcon.Text.Trim();
                if (!string.IsNullOrEmpty(v)) _previewIcon.Text = v;
            };

            var iconPreviewBox = new Border
            {
                Background = B("#FF6B35"),
                CornerRadius = new CornerRadius(10),
                Width = 56,
                Height = 48
            };
            iconPreviewBox.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.35
            };
            _iconPreviewTxt = new TextBlock
            {
                Text = _editing?.Icon ?? "🍕",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _tbIcon.TextChanged += (_, _) =>
            {
                var v = _tbIcon.Text.Trim();
                if (!string.IsNullOrEmpty(v)) _iconPreviewTxt.Text = v;
            };
            iconPreviewBox.Child = _iconPreviewTxt;

            Grid.SetColumn(_tbIcon, 0); iconInputRow.Children.Add(_tbIcon);
            Grid.SetColumn(iconPreviewBox, 2); iconInputRow.Children.Add(iconPreviewBox);
            fields.Children.Add(iconInputRow);

            fields.Children.Add(new TextBlock
            {
                Text = "اختر سريعاً:",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 6)
            });

            _emojiGroups = new (string Em, string Bg, string Br)[]
            {
                ("🍕","#2d1200","#FF6B35"), ("🍔","#2d1200","#FF6B35"), ("🌮","#2d1200","#FF6B35"),
                ("🌯","#2d1200","#FF6B35"), ("🥪","#2d1200","#FF6B35"), ("🌭","#2d1200","#FF6B35"),
                ("🍟","#2d1200","#FF6B35"), ("🥙","#2d1200","#FF6B35"), ("🧆","#2d1200","#FF6B35"),
                ("🥓","#2d1200","#FF6B35"),
                ("🥩","#2d0808","#E63946"), ("🍗","#2d0808","#E63946"), ("🍖","#2d0808","#E63946"),
                ("🦐","#2d0808","#E63946"), ("🦞","#2d0808","#E63946"), ("🦀","#2d0808","#E63946"),
                ("🦑","#2d0808","#E63946"), ("🍣","#2d0808","#E63946"), ("🍤","#2d0808","#E63946"),
                ("🥚","#2d0808","#E63946"),
                ("🍰","#2d2200","#ffd166"), ("🎂","#2d2200","#ffd166"), ("🧁","#2d2200","#ffd166"),
                ("🍩","#2d2200","#ffd166"), ("🍦","#2d2200","#ffd166"), ("🍨","#2d2200","#ffd166"),
                ("🍧","#2d2200","#ffd166"), ("🍫","#2d2200","#ffd166"), ("🍬","#2d2200","#ffd166"),
                ("🍭","#2d2200","#ffd166"),
                ("☕","#081828","#60a5fa"), ("🧃","#081828","#60a5fa"), ("🥤","#081828","#60a5fa"),
                ("🧋","#081828","#60a5fa"), ("🍵","#081828","#60a5fa"), ("🧉","#081828","#60a5fa"),
                ("🍹","#081828","#60a5fa"), ("🍸","#081828","#60a5fa"), ("🥛","#081828","#60a5fa"),
                ("🧊","#081828","#60a5fa"),
                ("🥗","#082010","#06d6a0"), ("🍝","#082010","#06d6a0"), ("🍜","#082010","#06d6a0"),
                ("🍛","#082010","#06d6a0"), ("🍲","#082010","#06d6a0"), ("🥘","#082010","#06d6a0"),
                ("🍎","#082010","#06d6a0"), ("🍊","#082010","#06d6a0"), ("🍋","#082010","#06d6a0"),
                ("🍇","#082010","#06d6a0"), ("🍓","#082010","#06d6a0"), ("🍑","#082010","#06d6a0"),
                ("🥭","#082010","#06d6a0"), ("🍍","#082010","#06d6a0"), ("🥝","#082010","#06d6a0"),
                ("🍌","#082010","#06d6a0"),
                ("🧇","#180820","#a78bfa"), ("🥞","#180820","#a78bfa"), ("🍱","#180820","#a78bfa"),
                ("🥡","#180820","#a78bfa"),
            };

            var pickerScroll = new ScrollViewer
            {
                Height = 130,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _pickerWrap = new WrapPanel();
            string currentIcon = _editing?.Icon ?? "🍕";

            foreach (var g in _emojiGroups)
            {
                var em = g.Em;
                bool isSelected = em == currentIcon;
                var cell = new Border
                {
                    Width = 44,
                    Height = 44,
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(10),
                    Background = isSelected ? B(g.Br) : B(g.Bg),
                    BorderBrush = B(g.Br),
                    BorderThickness = new Thickness(isSelected ? 2 : 1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Child = new TextBlock
                    {
                        Text = em,
                        FontSize = 20,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                cell.Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString(g.Br),
                    BlurRadius = isSelected ? 10 : 0,
                    ShadowDepth = 0,
                    Opacity = 0.6
                };
                cell.MouseEnter += (_, _) =>
                {
                    if (_tbIcon.Text != em)
                    {
                        cell.Background = B(g.Br);
                        cell.BorderThickness = new Thickness(2);
                        if (cell.Effect is DropShadowEffect fx) fx.BlurRadius = 10;
                    }
                };
                cell.MouseLeave += (_, _) =>
                {
                    if (_tbIcon.Text != em)
                    {
                        cell.Background = B(g.Bg);
                        cell.BorderThickness = new Thickness(1);
                        if (cell.Effect is DropShadowEffect fx) fx.BlurRadius = 0;
                    }
                };
                cell.MouseLeftButtonUp += (_, _) =>
                {
                    _tbIcon.Text = em;
                    _previewIcon.Text = em;
                    _iconPreviewTxt.Text = em;
                    int idx = 0;
                    foreach (Border b in _pickerWrap.Children)
                    {
                        var grp = _emojiGroups[idx++];
                        b.Background = B(grp.Bg);
                        b.BorderBrush = B(grp.Br);
                        b.BorderThickness = new Thickness(1);
                        if (b.Effect is DropShadowEffect fx) fx.BlurRadius = 0;
                    }
                    cell.Background = B(g.Br);
                    cell.BorderThickness = new Thickness(2);
                    if (cell.Effect is DropShadowEffect sfx) sfx.BlurRadius = 14;
                };
                _pickerWrap.Children.Add(cell);
            }
            pickerScroll.Content = _pickerWrap;
            fields.Children.Add(pickerScroll);

            rightScroll.Content = fields;
            Grid.SetColumn(rightScroll, 1);
            content.Children.Add(rightScroll);

            var divider = new Border
            {
                Background = B("#1a2540"),
                Width = 1,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(divider, 1);
            content.Children.Add(divider);

            Grid.SetRow(content, 1);
            root.Children.Add(content);

            // ══ Buttons ══
            var btnBar = new Border
            {
                Background = B("#090e1a"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 14, 20, 18)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = MakeDialogBtn("إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            var saveBtn = MakeDialogBtn(
                isAdd ? "➕  إضافة المنتج" : "💾  حفظ التعديلات",
                accentHex, B("#0a0a14"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.4
            };

            Grid.SetColumn(cancelBtn, 0); btnGrid.Children.Add(cancelBtn);
            Grid.SetColumn(saveBtn, 2); btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbName.Text))
            {
                MessageBox.Show("أدخل اسم المنتج", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (_catList.SelectedItem is not ListBoxItem li || li.Tag is not Category selCat)
            {
                MessageBox.Show("اختر الفئة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var priceStr = _tbPrice.Text.Replace(',', '.');
            if (!double.TryParse(priceStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double price) || price < 0)
            {
                MessageBox.Show("سعر البيع غير صحيح", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var costStr = _tbCost.Text.Replace(',', '.');
            double.TryParse(costStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double cost);

            Result = new Product
            {
                Name = _tbName.Text.Trim(),
                CategoryId = selCat.Id,
                Price = price,
                Cost = cost,
                Icon = string.IsNullOrWhiteSpace(_tbIcon.Text) ? "🍕" : _tbIcon.Text.Trim(),
                IsActive = true
            };
            DialogResult = true;
            Close();
        }

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
            Background = B("#0f1a2e"),
            Foreground = B("#eef0f2"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10),
            FontSize = 13,
            CaretBrush = B("#FF6B35"),
            SelectionBrush = B("#FF6B35")
        };

        Button MakeDialogBtn(string text, string bg, Brush fg, Action click)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            f.SetBinding(Border.PaddingProperty,
                new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderBrushProperty,
                new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderThicknessProperty,
                new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp);
            var btn = new Button
            {
                Content = text,
                Background = B(bg),
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 13, 0, 13),
                FontWeight = FontWeights.Black,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = new ControlTemplate(typeof(Button)) { VisualTree = f }
            };
            btn.Click += (_, _) => click();
            return btn;
        }
    }
}