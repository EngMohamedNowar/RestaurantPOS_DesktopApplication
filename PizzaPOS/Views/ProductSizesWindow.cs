// Views/ProductSizesWindow.cs
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
    public class ProductSizesWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly Product _product;

        readonly ObservableCollection<ProductSize> _sizes = new();
        readonly ObservableCollection<ProductExtra> _extras = new();

        DataGrid _dgSizes = null!;
        DataGrid _dgExtras = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public ProductSizesWindow(Product product)
        {
            _product = product;
            Title = $"أحجام وإضافات — {product.Name}";
            Width = 860; Height = 660;
            MinWidth = 720;
            Background = B("#070b14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            BuildUI();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            root.RowDefinitions.Add(new RowDefinition());                             // content
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // footer

            // ══ HEADER ══
            var header = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B("#a78bfa"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hRow = new Grid();
            hRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hRow.ColumnDefinitions.Add(new ColumnDefinition());
            hRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var hIcon = new Border
            {
                Background = B("#a78bfa"),
                CornerRadius = new CornerRadius(12),
                Width = 50,
                Height = 50,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#a78bfa"),
                BlurRadius = 22,
                ShadowDepth = 0,
                Opacity = 0.55
            };
            hIcon.Child = new TextBlock
            {
                Text = "📏",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = $"{_product.Icon}  {_product.Name}",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "إدارة الأحجام والإضافات المتاحة للمنتج",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });

            // شارة السعر الأساسي
            var priceBadge = new Border
            {
                Background = B("#1a0e08"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            priceBadge.Child = new TextBlock
            {
                Text = $"💰 السعر الأساسي: {_product.Price:F2} ج",
                FontSize = 12,
                FontWeight = FontWeights.Black,
                Foreground = B("#FF6B35")
            };

            Grid.SetColumn(hIcon, 0); hRow.Children.Add(hIcon);
            Grid.SetColumn(hInfo, 1); hRow.Children.Add(hInfo);
            Grid.SetColumn(priceBadge, 2); hRow.Children.Add(priceBadge);
            header.Child = hRow;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ CONTENT ══
            var content = new Grid { Margin = new Thickness(16, 14, 16, 0) };
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            content.ColumnDefinitions.Add(new ColumnDefinition());

            // ── Sizes (left) ──
            var sizesPanel = BuildSection(
                "📐", "الأحجام", "اضبط الأحجام المتاحة وفرق السعر",
                "#ffd166", "#1a1400",
                out _dgSizes, BuildSizesGrid,
                () => AddSize(), () => EditSize(), () => DeleteSize()
            );
            Grid.SetColumn(sizesPanel, 0);
            content.Children.Add(sizesPanel);

            // فاصل
            var divider = new Border { Background = B("#1a2540"), Width = 1 };
            Grid.SetColumn(divider, 1);
            content.Children.Add(divider);

            // ── Extras (right) ──
            var extrasPanel = BuildSection(
                "✨", "الإضافات", "حدد الإضافات الاختيارية وأسعارها",
                "#06d6a0", "#081a10",
                out _dgExtras, BuildExtrasGrid,
                () => AddExtra(), () => EditExtra(), () => DeleteExtra()
            );
            Grid.SetColumn(extrasPanel, 2);
            content.Children.Add(extrasPanel);

            Grid.SetRow(content, 1);
            root.Children.Add(content);

            // ══ FOOTER ══
            var footer = new Border
            {
                Background = B("#090e1a"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 12, 18, 14)
            };
            var footerRow = new StackPanel { Orientation = Orientation.Horizontal };
            footerRow.Children.Add(new TextBlock
            {
                Text = "💡",
                FontSize = 13,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            footerRow.Children.Add(new TextBlock
            {
                Text = $"الأحجام تضيف فرق سعر على السعر الأساسي ({_product.Price:F2} ج)  •  الإضافات تُضاف بشكل اختياري عند الطلب  •  انقر مرتين لتعديل أي عنصر",
                Foreground = B("#3a5070"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });
            footer.Child = footerRow;
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
            LoadData();
        }

        // ══ Section Builder ══════════════════════════
        Grid BuildSection(
            string emoji, string title, string subtitle,
            string accentHex, string bgHex,
            out DataGrid dg,
            Func<DataGrid> gridBuilder,
            Action onAdd, Action onEdit, Action onDelete)
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition());
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Title Bar ──
            var titleBorder = new Border
            {
                Background = B(bgHex),
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(1, 1, 1, 0),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                Padding = new Thickness(16, 12, 16, 12)
            };
            titleBorder.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.2
            };

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var emojiCircle = new Border
            {
                Background = B(accentHex),
                CornerRadius = new CornerRadius(8),
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0)
            };
            emojiCircle.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            emojiCircle.Child = new TextBlock
            {
                Text = emoji,
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Black,
                Foreground = B(accentHex)
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 9,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 2, 0, 0)
            });

            Grid.SetColumn(emojiCircle, 0); titleGrid.Children.Add(emojiCircle);
            Grid.SetColumn(titleStack, 1); titleGrid.Children.Add(titleStack);
            titleBorder.Child = titleGrid;
            Grid.SetRow(titleBorder, 0);
            g.Children.Add(titleBorder);

            // ── DataGrid ──
            dg = gridBuilder();
            dg.MouseDoubleClick += (_, _) => onEdit();

            var gridWrap = new Border
            {
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(1, 0, 1, 0),
                Child = new ScrollViewer
                {
                    Content = dg,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = B("#070b14")
                }
            };
            Grid.SetRow(gridWrap, 1);
            g.Children.Add(gridWrap);

            // ── Action Bar ──
            var actBar = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(0, 0, 12, 12),
                Padding = new Thickness(10, 10, 10, 12)
            };

            var actGrid = new Grid();
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // زر الإضافة
            var addBtn = MakeBtn("➕  إضافة", accentHex, B("#030a06"));
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => onAdd();

            // زر التعديل
            var editBtn = MakeBtn("✏️  تعديل", "#1e3a5f", B("#7ab8f5"));
            editBtn.Click += (_, _) => onEdit();

            // زر الحذف
            var delBtn = MakeBtn("🗑  حذف", "#1a080a", B("#E63946"));
            delBtn.BorderBrush = B("#E63946");
            delBtn.BorderThickness = new Thickness(1);
            delBtn.Click += (_, _) => onDelete();

            Grid.SetColumn(addBtn, 0); actGrid.Children.Add(addBtn);
            Grid.SetColumn(editBtn, 1); actGrid.Children.Add(editBtn);
            Grid.SetColumn(delBtn, 2); actGrid.Children.Add(delBtn);

            actBar.Child = actGrid;
            Grid.SetRow(actBar, 2);
            g.Children.Add(actBar);

            return g;
        }

        // ══ Sizes DataGrid ═══════════════════════════
        DataGrid BuildSizesGrid()
        {
            var dg = MakeGrid();
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "اسم الحجم",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = ColStyle(B("#eef0f2"), FontWeights.Bold)
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "فرق السعر (ج)",
                Binding = new Binding("ExtraPrice") { StringFormat = "{0:F2}" },
                Width = 120,
                ElementStyle = ColStyle(B("#ffd166"), FontWeights.Black, TextAlignment.Center)
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الترتيب",
                Binding = new Binding("SortOrder"),
                Width = 80,
                ElementStyle = ColStyle(B("#7ab8f5"), FontWeights.Normal, TextAlignment.Center)
            });
            dg.ItemsSource = _sizes;
            return dg;
        }

        // ══ Extras DataGrid ══════════════════════════
        DataGrid BuildExtrasGrid()
        {
            var dg = MakeGrid();
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "اسم الإضافة",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = ColStyle(B("#eef0f2"), FontWeights.Bold)
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "السعر (ج)",
                Binding = new Binding("Price") { StringFormat = "{0:F2}" },
                Width = 120,
                ElementStyle = ColStyle(B("#06d6a0"), FontWeights.Black, TextAlignment.Center)
            });
            dg.ItemsSource = _extras;
            return dg;
        }

        // ══ DataGrid Base ════════════════════════════
        DataGrid MakeGrid()
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
                RowHeight = 46,
                ColumnHeaderHeight = 44,
                FontSize = 13,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            var hs = new Style(typeof(DataGridColumnHeader));
            hs.Setters.Add(new Setter(Control.BackgroundProperty, B("#0c1530")));
            hs.Setters.Add(new Setter(Control.ForegroundProperty, B("#ffd166")));
            hs.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            hs.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
            hs.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 0, 14, 0)));
            hs.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 2)));
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
            cs.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(14, 0, 14, 0)));
            cs.Setters.Add(new Setter(DataGridCell.ForegroundProperty, B("#c8d8f0")));
            cs.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));
            var csel = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            csel.Setters.Add(new Setter(DataGridCell.BackgroundProperty, B("#1a2d50")));
            csel.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent));
            cs.Triggers.Add(csel);
            dg.CellStyle = cs;

            return dg;
        }

        Style ColStyle(Brush fg, FontWeight fw, TextAlignment align = TextAlignment.Right)
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(TextBlock.ForegroundProperty, fg));
            s.Setters.Add(new Setter(TextBlock.FontWeightProperty, fw));
            s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, align));
            s.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            return s;
        }

        // ══ Load ═════════════════════════════════════
        void LoadData()
        {
            _sizes.Clear();
            foreach (var s in _db.GetProductSizes(_product.Id)) _sizes.Add(s);
            _extras.Clear();
            foreach (var e in _db.GetProductExtras(_product.Id)) _extras.Add(e);
        }

        // ══ Sizes CRUD ═══════════════════════════════
        void AddSize()
        {
            var dlg = new SizeEntryDialog(null) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            var s = dlg.Result!;
            s.ProductId = _product.Id;
            s.SortOrder = _sizes.Count + 1;
            _db.SaveProductSize(s);
            LoadData();
        }

        void EditSize()
        {
            if (_dgSizes.SelectedItem is not ProductSize sel)
            { Alert("اختر حجماً أولاً"); return; }
            var dlg = new SizeEntryDialog(sel) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            var updated = dlg.Result!;
            updated.Id = sel.Id;
            updated.ProductId = _product.Id;
            _db.SaveProductSize(updated);
            LoadData();
        }

        void DeleteSize()
        {
            if (_dgSizes.SelectedItem is not ProductSize s)
            { Alert("اختر حجماً أولاً"); return; }
            if (Confirm($"حذف حجم \"{s.Name}\"؟"))
            { _db.DeleteProductSize(s.Id); LoadData(); }
        }

        // ══ Extras CRUD ══════════════════════════════
        void AddExtra()
        {
            var dlg = new ExtraEntryDialog(null) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            var e = dlg.Result!;
            e.ProductId = _product.Id;
            _db.SaveProductExtra(e);
            LoadData();
        }

        void EditExtra()
        {
            if (_dgExtras.SelectedItem is not ProductExtra sel)
            { Alert("اختر إضافة أولاً"); return; }
            var dlg = new ExtraEntryDialog(sel) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            var updated = dlg.Result!;
            updated.Id = sel.Id;
            updated.ProductId = _product.Id;
            _db.SaveProductExtra(updated);
            LoadData();
        }

        void DeleteExtra()
        {
            if (_dgExtras.SelectedItem is not ProductExtra e)
            { Alert("اختر إضافة أولاً"); return; }
            if (Confirm($"حذف إضافة \"{e.Name}\"؟"))
            { _db.DeleteProductExtra(e.Id); LoadData(); }
        }

        // ══ Helpers ══════════════════════════════════
        void Alert(string msg) =>
            MessageBox.Show(msg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);

        bool Confirm(string msg) =>
            MessageBox.Show(msg, "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

        Button MakeBtn(string text, string bgHex, Brush fg)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
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
                Padding = new Thickness(14, 8, 14, 8),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0),
                Template = new ControlTemplate(typeof(Button)) { VisualTree = f }
            };
        }
    }

    // ══════════════════════════════════════════════════
    //  SizeEntryDialog — إضافة / تعديل حجم
    // ══════════════════════════════════════════════════
    public class SizeEntryDialog : Window
    {
        public ProductSize? Result { get; private set; }

        readonly ProductSize? _editing;
        TextBox _tbName = null!, _tbExtra = null!, _tbSort = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public SizeEntryDialog(ProductSize? editing)
        {
            _editing = editing;
            Title = editing == null ? "إضافة حجم جديد" : "تعديل الحجم";
            Width = 400;
            SizeToContent = SizeToContent.Height;
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
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            bool isNew = _editing == null;
            string accentHex = "#ffd166";

            // ── Header ──
            var header = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIco = new Border
            {
                Background = B(accentHex),
                CornerRadius = new CornerRadius(10),
                Width = 44,
                Height = 44,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIco.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            hIco.Child = new TextBlock
            {
                Text = isNew ? "📐" : "✏️",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hSp.Children.Add(hIco);
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = isNew ? "إضافة حجم جديد" : $"تعديل: {_editing!.Name}",
                FontSize = 15,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "فرق السعر يُضاف على السعر الأساسي للمنتج",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 18, 20, 8) };

            fields.Children.Add(FLbl("اسم الحجم *"));
            _tbName = MakeTB(_editing?.Name ?? "", accentHex);
            _tbName.Margin = new Thickness(0, 4, 0, 16);
            fields.Children.Add(_tbName);

            // سعر مع hint
            fields.Children.Add(FLbl("فرق السعر (ج) *  —  اكتب 0 إن لم يكن هناك فرق"));
            var priceRow = new Grid { Margin = new Thickness(0, 4, 0, 16) };
            priceRow.ColumnDefinitions.Add(new ColumnDefinition());
            priceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _tbExtra = MakeTB(_editing?.ExtraPrice.ToString("F2") ?? "0.00", accentHex);
            var priceUnit = new Border
            {
                Background = B("#0a1520"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Padding = new Thickness(12, 0, 12, 0)
            };
            priceUnit.Child = new TextBlock
            {
                Text = "ج",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = B("#2a4060"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_tbExtra, 0); priceRow.Children.Add(_tbExtra);
            Grid.SetColumn(priceUnit, 1); priceRow.Children.Add(priceUnit);
            fields.Children.Add(priceRow);

            fields.Children.Add(FLbl("الترتيب"));
            _tbSort = MakeTB(_editing?.SortOrder.ToString() ?? "1", accentHex);
            _tbSort.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbSort);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
            var btnBar = new Border
            {
                Background = B("#090e1a"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 14, 20, 18)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancel = MakeDialogBtn("إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancel.BorderBrush = B("#1e2d4a");
            cancel.BorderThickness = new Thickness(1);

            var save = MakeDialogBtn(
                isNew ? "➕  إضافة الحجم" : "💾  حفظ التعديلات",
                accentHex, B("#0a0800"), Save);
            save.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.45
            };

            Grid.SetColumn(cancel, 0); btnGrid.Children.Add(cancel);
            Grid.SetColumn(save, 2); btnGrid.Children.Add(save);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbName.Text))
            { Warn("أدخل اسم الحجم"); return; }

            var extraStr = _tbExtra.Text.Replace(',', '.');
            if (!double.TryParse(extraStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double extra) || extra < 0)
            { Warn("أدخل فرق سعر صحيح (0 أو أكثر)"); return; }

            int.TryParse(_tbSort.Text, out int sort);
            Result = new ProductSize
            {
                Id = _editing?.Id ?? 0,
                Name = _tbName.Text.Trim(),
                ExtraPrice = extra,
                SortOrder = sort < 1 ? 1 : sort
            };
            DialogResult = true;
            Close();
        }

        void Warn(string msg) =>
            MessageBox.Show(msg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);

        TextBlock FLbl(string t) => new()
        {
            Text = t,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = B("#4a6080")
        };

        TextBox MakeTB(string val, string caretHex) => new()
        {
            Text = val,
            Background = B("#0f1a2e"),
            Foreground = B("#eef0f2"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 11, 12, 11),
            FontSize = 13,
            CaretBrush = B(caretHex),
            SelectionBrush = B(caretHex)
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

    // ══════════════════════════════════════════════════
    //  ExtraEntryDialog — إضافة / تعديل إضافة
    // ══════════════════════════════════════════════════
    public class ExtraEntryDialog : Window
    {
        public ProductExtra? Result { get; private set; }

        readonly ProductExtra? _editing;
        TextBox _tbName = null!, _tbPrice = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public ExtraEntryDialog(ProductExtra? editing)
        {
            _editing = editing;
            Title = editing == null ? "إضافة اختيارية جديدة" : "تعديل الإضافة";
            Width = 400;
            SizeToContent = SizeToContent.Height;
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
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            bool isNew = _editing == null;
            string accentHex = "#06d6a0";

            // ── Header ──
            var header = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B(accentHex),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIco = new Border
            {
                Background = B(accentHex),
                CornerRadius = new CornerRadius(10),
                Width = 44,
                Height = 44,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIco.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            hIco.Child = new TextBlock
            {
                Text = isNew ? "✨" : "✏️",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hSp.Children.Add(hIco);
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = isNew ? "إضافة اختيارية جديدة" : $"تعديل: {_editing!.Name}",
                FontSize = 15,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "تُعرض للعميل عند إضافة المنتج للأوردر",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 18, 20, 8) };

            fields.Children.Add(FLbl("اسم الإضافة *  (مثال: جبن إضافي / صوص حار)"));
            _tbName = MakeTB(_editing?.Name ?? "", accentHex);
            _tbName.Margin = new Thickness(0, 4, 0, 16);
            fields.Children.Add(_tbName);

            fields.Children.Add(FLbl("السعر (ج) *  —  اكتب 0 إن كانت مجانية"));
            var priceRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            priceRow.ColumnDefinitions.Add(new ColumnDefinition());
            priceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _tbPrice = MakeTB(_editing?.Price.ToString("F2") ?? "0.00", accentHex);
            var priceUnit = new Border
            {
                Background = B("#0a1520"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Padding = new Thickness(12, 0, 12, 0)
            };
            priceUnit.Child = new TextBlock
            {
                Text = "ج",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = B("#2a4060"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_tbPrice, 0); priceRow.Children.Add(_tbPrice);
            Grid.SetColumn(priceUnit, 1); priceRow.Children.Add(priceUnit);
            fields.Children.Add(priceRow);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
            var btnBar = new Border
            {
                Background = B("#090e1a"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 14, 20, 18)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancel = MakeDialogBtn("إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancel.BorderBrush = B("#1e2d4a");
            cancel.BorderThickness = new Thickness(1);

            var save = MakeDialogBtn(
                isNew ? "➕  إضافة" : "💾  حفظ التعديلات",
                accentHex, B("#020f0a"), Save);
            save.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accentHex),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.45
            };

            Grid.SetColumn(cancel, 0); btnGrid.Children.Add(cancel);
            Grid.SetColumn(save, 2); btnGrid.Children.Add(save);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbName.Text))
            { Warn("أدخل اسم الإضافة"); return; }

            var priceStr = _tbPrice.Text.Replace(',', '.');
            if (!double.TryParse(priceStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double price) || price < 0)
            { Warn("أدخل سعراً صحيحاً (0 أو أكثر)"); return; }

            Result = new ProductExtra
            {
                Id = _editing?.Id ?? 0,
                Name = _tbName.Text.Trim(),
                Price = price
            };
            DialogResult = true;
            Close();
        }

        void Warn(string msg) =>
            MessageBox.Show(msg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);

        TextBlock FLbl(string t) => new()
        {
            Text = t,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = B("#4a6080")
        };

        TextBox MakeTB(string val, string caretHex) => new()
        {
            Text = val,
            Background = B("#0f1a2e"),
            Foreground = B("#eef0f2"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 11, 12, 11),
            FontSize = 13,
            CaretBrush = B(caretHex),
            SelectionBrush = B(caretHex)
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