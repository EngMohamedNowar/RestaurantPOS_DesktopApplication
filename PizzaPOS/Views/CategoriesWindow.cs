// Views/CategoriesWindow.cs
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
    public class CategoriesWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<Category> _cats = new();
        DataGrid _dg = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public CategoriesWindow()
        {
            Title = "إدارة الفئات";
            Width = 660; Height = 580;
            MinWidth = 580;
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
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // stats
            root.RowDefinitions.Add(new RowDefinition());                             // grid
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // actions

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
                Text = "🗂",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = "إدارة الفئات",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "إضافة وتعديل وحذف فئات المنتجات",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });

            Grid.SetColumn(iconBorder, 0); hGrid.Children.Add(iconBorder);
            Grid.SetColumn(titleStack, 1); hGrid.Children.Add(titleStack);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Stats Bar ══
            var statsBorder = new Border
            {
                Background = B("#090e1a"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(22, 10, 22, 10)
            };
            var statsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var countTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = B("#FF6B35")
            };
            _cats.CollectionChanged += (_, _) => countTxt.Text = _cats.Count.ToString();
            countTxt.Text = "0";

            statsPanel.Children.Add(MakeStatCard("📦 إجمالي الفئات", countTxt, "#FF6B35", "#1a0e08"));
            statsBorder.Child = statsPanel;
            Grid.SetRow(statsBorder, 1);
            root.Children.Add(statsBorder);

            // ══ DataGrid ══
            _dg = BuildGrid();

            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الأيقونة",
                Binding = new Binding("Icon"),
                Width = 80,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.FontSizeProperty, 20.0),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "اسم الفئة",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B("#eef0f2")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.FontSizeProperty, 13.0),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الترتيب",
                Binding = new Binding("SortOrder"),
                Width = 100,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B("#ffd166")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            _dg.ItemsSource = _cats;

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
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addBtn = MakeActionButton("➕  إضافة فئة", "#FF6B35", B("#fff8f5"));
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => AddCat();

            var editBtn = MakeActionButton("✏️  تعديل", "#1e3a5f", B("#7ab8f5"));
            editBtn.Click += (_, _) => EditCat();

            var delBtn = MakeActionButton("🗑  حذف", "#1a0810", B("#E63946"));
            delBtn.BorderBrush = B("#E63946");
            delBtn.BorderThickness = new Thickness(1);
            delBtn.Click += (_, _) => DeleteCat();

            Grid.SetColumn(addBtn, 0); actionGrid.Children.Add(addBtn);
            Grid.SetColumn(editBtn, 1); actionGrid.Children.Add(editBtn);
            Grid.SetColumn(delBtn, 3); actionGrid.Children.Add(delBtn);

            actionBar.Child = actionGrid;
            Grid.SetRow(actionBar, 3);
            root.Children.Add(actionBar);

            Content = root;
            Load();
        }

        // ── Stat Card ──────────────────────────────
        Border MakeStatCard(string label, TextBlock valueTxt, string accent, string bg)
        {
            var card = new Border
            {
                Background = B(bg),
                BorderBrush = B(accent),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            card.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accent),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.15
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = B(accent),
                Margin = new Thickness(0, 0, 0, 2)
            });
            valueTxt.Text = "—";
            sp.Children.Add(valueTxt);
            card.Child = sp;
            return card;
        }

        // ── DataGrid ───────────────────────────────
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

            return dg;
        }

        // ── Action Button ──────────────────────────
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
                Padding = new Thickness(20, 10, 20, 10),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0),
                Template = new ControlTemplate(typeof(Button)) { VisualTree = f }
            };
        }

        // ── CRUD ───────────────────────────────────
        void AddCat()
        {
            var dlg = new CatEditDialog("➕ فئة جديدة") { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveCategory(new Category
            {
                Name = dlg.CatName,
                Icon = dlg.CatIcon,
                SortOrder = _cats.Count + 1
            });
            Load();
        }

        void EditCat()
        {
            if (_dg.SelectedItem is not Category cat)
            {
                Notify("اختر فئة من القائمة أولاً"); return;
            }
            var dlg = new CatEditDialog("✏️ تعديل فئة", cat.Name, cat.Icon, cat.SortOrder) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            cat.Name = dlg.CatName;
            cat.Icon = dlg.CatIcon;
            cat.SortOrder = dlg.CatSort;
            _db.SaveCategory(cat);
            Load();
        }

        void DeleteCat()
        {
            if (_dg.SelectedItem is not Category cat)
            {
                Notify("اختر فئة من القائمة أولاً"); return;
            }

            // ① تحقق لو فيه منتجات مرتبطة
            var products = _db.GetProductsByCategory(cat.Id);
            int count = products.Count();

            if (count > 0)
            {
                // ② رسالة تحذير مخصوصة للمنتجات
                var warn = MessageBox.Show(
                    $"⚠️ تحذير!\n\n" +
                    $"الفئة \"{cat.Name}\" تحتوي على {count} منتج مرتبط بها.\n\n" +
                    $"هل أنت متأكد أنك تريد حذف الفئة وجميع منتجاتها بالقوة؟\n" +
                    $"هذا الإجراء لا يمكن التراجع عنه!",
                    "⚠️ تحذير - يوجد منتجات مرتبطة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (warn != MessageBoxResult.Yes) return;

                // ③ تأكيد ثاني عشان متعملش غلط
                var confirm = MessageBox.Show(
                    $"تأكيد نهائي: سيتم حذف \"{cat.Name}\" و {count} منتج نهائياً.",
                    "تأكيد الحذف النهائي",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Stop);

                if (confirm != MessageBoxResult.Yes) return;
            }
            else
            {
                // ④ مفيش منتجات — تأكيد عادي
                if (MessageBox.Show(
                    $"هل تريد حذف فئة \"{cat.Name}\"؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            }

            _db.DeleteCategory(cat.Id);
            Load();
        }

        void Load()
        {
            _cats.Clear();
            foreach (var c in _db.GetCategories()) _cats.Add(c);
        }

        void Notify(string msg) =>
            MessageBox.Show(msg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ══════════════════════════════════════════════
    //  CatEditDialog — Professional Redesign
    // ══════════════════════════════════════════════
    public class CatEditDialog : Window
    {
        public string CatName { get; private set; } = "";
        public string CatIcon { get; private set; } = "🍕";
        public int CatSort { get; private set; }

        readonly TextBox _tbName, _tbIcon, _tbSort;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public CatEditDialog(
            string title,
            string name = "",
            string icon = "🍕",
            int sort = 0)
        {
            Title = title;
            Width = 400;
            SizeToContent = SizeToContent.Height;
            Background = B("#070b14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // fields
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // buttons

            // ── Header ──
            var header = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = B("#FF6B35"),
                CornerRadius = new CornerRadius(10),
                Width = 42,
                Height = 42,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            hIcon.Child = new TextBlock
            {
                Text = "🗂",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hSp.Children.Add(hIcon);
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "أدخل بيانات الفئة بدقة",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            // اسم الفئة
            fields.Children.Add(FieldLabel("اسم الفئة *"));
            _tbName = MakeTB(name);
            _tbName.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbName);

            // أيقونة — معاينة + picker
            fields.Children.Add(FieldLabel("الأيقونة *"));

            // صف المعاينة + الكتابة اليدوية
            var iconRow = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            iconRow.ColumnDefinitions.Add(new ColumnDefinition());
            iconRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            iconRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });

            _tbIcon = MakeTB(icon);
            _tbIcon.FontSize = 18;
            _tbIcon.ToolTip = "أو اكتب emoji يدوياً";

            var previewBorder = new Border
            {
                Background = B("#FF6B35"),
                CornerRadius = new CornerRadius(12),
                Width = 64,
                Height = 48
            };
            previewBorder.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            var previewTxt = new TextBlock
            {
                Text = icon,
                FontSize = 26,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            previewBorder.Child = previewTxt;
            _tbIcon.TextChanged += (_, _) =>
            {
                var val = _tbIcon.Text.Trim();
                if (!string.IsNullOrEmpty(val)) previewTxt.Text = val;
            };

            Grid.SetColumn(_tbIcon, 0); iconRow.Children.Add(_tbIcon);
            Grid.SetColumn(previewBorder, 2); iconRow.Children.Add(previewBorder);
            fields.Children.Add(iconRow);

            // Emoji Picker Grid
            fields.Children.Add(new TextBlock
            {
                Text = "اختر سريعاً:",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 6)
            });

            var emojiGroups = new (string Em, string Bg, string Br)[]
            {
                // 🟠 وجبات رئيسية
                ("🍕","#2d1200","#FF6B35"), ("🍔","#2d1200","#FF6B35"), ("🌮","#2d1200","#FF6B35"),
                ("🌯","#2d1200","#FF6B35"), ("🥪","#2d1200","#FF6B35"), ("🌭","#2d1200","#FF6B35"),
                ("🍟","#2d1200","#FF6B35"), ("🥙","#2d1200","#FF6B35"), ("🧆","#2d1200","#FF6B35"),
                ("🥓","#2d1200","#FF6B35"),
                // 🔴 لحوم وبحريات
                ("🥩","#2d0808","#E63946"), ("🍗","#2d0808","#E63946"), ("🍖","#2d0808","#E63946"),
                ("🦐","#2d0808","#E63946"), ("🦞","#2d0808","#E63946"), ("🦀","#2d0808","#E63946"),
                ("🦑","#2d0808","#E63946"), ("🍣","#2d0808","#E63946"), ("🍤","#2d0808","#E63946"),
                ("🥚","#2d0808","#E63946"),
                // 🟡 حلويات
                ("🍰","#2d2200","#ffd166"), ("🎂","#2d2200","#ffd166"), ("🧁","#2d2200","#ffd166"),
                ("🍩","#2d2200","#ffd166"), ("🍦","#2d2200","#ffd166"), ("🍨","#2d2200","#ffd166"),
                ("🍧","#2d2200","#ffd166"), ("🍫","#2d2200","#ffd166"), ("🍬","#2d2200","#ffd166"),
                ("🍭","#2d2200","#ffd166"),
                // 🔵 مشروبات
                ("☕","#081828","#60a5fa"), ("🧃","#081828","#60a5fa"), ("🥤","#081828","#60a5fa"),
                ("🧋","#081828","#60a5fa"), ("🍵","#081828","#60a5fa"), ("🧉","#081828","#60a5fa"),
                ("🍹","#081828","#60a5fa"), ("🍸","#081828","#60a5fa"), ("🥛","#081828","#60a5fa"),
                ("🧊","#081828","#60a5fa"),
                // 🟢 خضروات وفواكه
                ("🥗","#082010","#06d6a0"), ("🍝","#082010","#06d6a0"), ("🍜","#082010","#06d6a0"),
                ("🍛","#082010","#06d6a0"), ("🍲","#082010","#06d6a0"), ("🥘","#082010","#06d6a0"),
                ("🍎","#082010","#06d6a0"), ("🍊","#082010","#06d6a0"), ("🍋","#082010","#06d6a0"),
                ("🍇","#082010","#06d6a0"), ("🍓","#082010","#06d6a0"), ("🍑","#082010","#06d6a0"),
                ("🥭","#082010","#06d6a0"), ("🍍","#082010","#06d6a0"), ("🥝","#082010","#06d6a0"),
                ("🍌","#082010","#06d6a0"),
                // 🟣 متنوعات
                ("🧇","#180820","#a78bfa"), ("🥞","#180820","#a78bfa"), ("🍱","#180820","#a78bfa"),
                ("🥡","#180820","#a78bfa"),
            };

            var pickerScroll = new ScrollViewer
            {
                Height = 140,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 0, 14)
            };

            var pickerWrap = new WrapPanel { Orientation = Orientation.Horizontal };

            foreach (var g in emojiGroups)
            {
                var em = g.Em;
                bool isSelected = em == icon;
                var cell = new Border
                {
                    Width = 44,
                    Height = 44,
                    Margin = new Thickness(3),
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
                    previewTxt.Text = em;
                    int idx = 0;
                    foreach (Border b in pickerWrap.Children)
                    {
                        var grp = emojiGroups[idx++];
                        b.Background = B(grp.Bg);
                        b.BorderThickness = new Thickness(1);
                        if (b.Effect is DropShadowEffect fx) fx.BlurRadius = 0;
                    }
                    cell.Background = B(g.Br);
                    cell.BorderThickness = new Thickness(2);
                    if (cell.Effect is DropShadowEffect sfx) sfx.BlurRadius = 14;
                };

                if (em == icon)
                {
                    cell.Background = B(g.Br);
                    cell.BorderThickness = new Thickness(2);
                    if (cell.Effect is DropShadowEffect fx) fx.BlurRadius = 14;
                }

                pickerWrap.Children.Add(cell);
            }

            pickerScroll.Content = pickerWrap;
            fields.Children.Add(pickerScroll);

            // الترتيب
            fields.Children.Add(FieldLabel("الترتيب *"));
            _tbSort = MakeTB(sort == 0 ? "1" : sort.ToString());
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
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = MakeBtn("إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            var saveBtn = MakeBtn("💾  حفظ الفئة", "#FF6B35", B("#fff8f5"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
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
                MessageBox.Show("أدخل اسم الفئة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            CatName = _tbName.Text.Trim();
            CatIcon = string.IsNullOrWhiteSpace(_tbIcon.Text) ? "🍕" : _tbIcon.Text.Trim();
            CatSort = int.TryParse(_tbSort.Text, out int s) ? s : 1;
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

        Button MakeBtn(string text, string bg, Brush fg, Action click)
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