// Views/CategoriesWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Helpers;
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

        public CategoriesWindow()
        {
            Title = "إدارة الفئات";
            Width = 660; Height = 580;
            MinWidth = 580;
            Background = UiHelper.B("#070b14");
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
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#FF6B35"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };

            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var iconBorder = new Border
            {
                Background = UiHelper.B("#FF6B35"),
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
                Foreground = UiHelper.B("#eef0f2")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "إضافة وتعديل وحذف فئات المنتجات",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
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
                Background = UiHelper.B("#090e1a"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(22, 10, 22, 10)
            };
            var statsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var countTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#FF6B35")
            };
            _cats.CollectionChanged += (_, _) => countTxt.Text = _cats.Count.ToString();
            countTxt.Text = "0";

            statsPanel.Children.Add(UiHelper.MakeStatCard("📦 إجمالي الفئات", countTxt, "#FF6B35", "#1a0e08"));
            statsBorder.Child = statsPanel;
            Grid.SetRow(statsBorder, 1);
            root.Children.Add(statsBorder);

            // ══ DataGrid ══
            _dg = UiHelper.BuildGrid();

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
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2")),
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
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#ffd166")),
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
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            var scroll = new ScrollViewer
            {
                Content = _dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = UiHelper.B("#070b14")
            };
            gridWrapper.Child = scroll;
            Grid.SetRow(gridWrapper, 2);
            root.Children.Add(gridWrapper);

            // ══ Action Bar ══
            var actionBar = new Border
            {
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 14, 18, 18)
            };

            var actionGrid = new Grid();
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addBtn = UiHelper.MakeActionButton("➕  إضافة فئة", "#FF6B35", UiHelper.B("#fff8f5"));
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => AddCat();

            var editBtn = UiHelper.MakeActionButton("✏️  تعديل", "#1e3a5f", UiHelper.B("#7ab8f5"));
            editBtn.Click += (_, _) => EditCat();

            var delBtn = UiHelper.MakeActionButton("🗑  حذف", "#1a0810", UiHelper.B("#E63946"));
            delBtn.BorderBrush = UiHelper.B("#E63946");
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

        public CatEditDialog(
            string title,
            string name = "",
            string icon = "🍕",
            int sort = 0)
        {
            Title = title;
            Width = 400;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#070b14");
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
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#FF6B35"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#FF6B35"),
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
                Foreground = UiHelper.B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "أدخل بيانات الفئة بدقة",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            // اسم الفئة
            fields.Children.Add(UiHelper.FieldLabel("اسم الفئة *"));
            _tbName = UiHelper.MakeTB(name);
            _tbName.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbName);

            // أيقونة — معاينة + picker
            fields.Children.Add(UiHelper.FieldLabel("الأيقونة *"));

            // صف المعاينة + الكتابة اليدوية
            var iconRow = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            iconRow.ColumnDefinitions.Add(new ColumnDefinition());
            iconRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            iconRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });

            _tbIcon = UiHelper.MakeTB(icon);
            _tbIcon.FontSize = 18;
            _tbIcon.ToolTip = "أو اكتب emoji يدوياً";

            var previewBorder = new Border
            {
                Background = UiHelper.B("#FF6B35"),
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
                Foreground = UiHelper.B("#4a6080"),
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
                    Background = isSelected ? UiHelper.B(g.Br) : UiHelper.B(g.Bg),
                    BorderBrush = UiHelper.B(g.Br),
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
                        cell.Background = UiHelper.B(g.Br);
                        cell.BorderThickness = new Thickness(2);
                        if (cell.Effect is DropShadowEffect fx) fx.BlurRadius = 10;
                    }
                };
                cell.MouseLeave += (_, _) =>
                {
                    if (_tbIcon.Text != em)
                    {
                        cell.Background = UiHelper.B(g.Bg);
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
                        b.Background = UiHelper.B(grp.Bg);
                        b.BorderThickness = new Thickness(1);
                        if (b.Effect is DropShadowEffect fx) fx.BlurRadius = 0;
                    }
                    cell.Background = UiHelper.B(g.Br);
                    cell.BorderThickness = new Thickness(2);
                    if (cell.Effect is DropShadowEffect sfx) sfx.BlurRadius = 14;
                };

                if (em == icon)
                {
                    cell.Background = UiHelper.B(g.Br);
                    cell.BorderThickness = new Thickness(2);
                    if (cell.Effect is DropShadowEffect fx) fx.BlurRadius = 14;
                }

                pickerWrap.Children.Add(cell);
            }

            pickerScroll.Content = pickerWrap;
            fields.Children.Add(pickerScroll);

            // الترتيب
            fields.Children.Add(UiHelper.FieldLabel("الترتيب *"));
            _tbSort = UiHelper.MakeTB(sort == 0 ? "1" : sort.ToString());
            _tbSort.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbSort);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
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

            var saveBtn = UiHelper.MakeBtn("💾  حفظ الفئة", "#FF6B35", UiHelper.B("#fff8f5"), Save);
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

    }
}