// Views/ProductIngredientsDialog.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;

namespace PizzaPOS.Views
{
    public class ProductIngredientsDialog : Window
    {
        readonly AppDbContext _db;
        readonly int _productId;
        readonly string _productName;
        readonly ObservableCollection<ProductIngredient> _items = new();

        ComboBox _ingCombo = null!;
        TextBox _qtyBox = null!;
        DataGrid _dg = null!;
        TextBlock _totalCostTxt = null!;
        TextBlock _price50Txt = null!;
        TextBlock _price100Txt = null!;
        TextBlock _headerCostTxt = null!;
        TextBlock _countTxt = null!;
        Button _addBtn = null!;
        TextBlock _editBadge = null!;

        // ── حالة التعديل ──────────────────────────────
        int? _editingIngredientId = null;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        LinearGradientBrush Grad(string c1, string c2, double angle = 90)
        {
            var pts = angle == 90
                ? (new Point(0, 0), new Point(0, 1))
                : (new Point(0, 0), new Point(1, 0));
            return new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString(c1), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString(c2), 1)
                }, pts.Item1, pts.Item2);
        }

        public ProductIngredientsDialog(AppDbContext db, int productId, string productName)
        {
            _db = db;
            _productId = productId;
            _productName = productName;

            Title = $"ربط المكونات — {productName}";
            Width = 820;
            Height = 620;
            MinWidth = 500;
            MinHeight = 400;
            Background = B("#0f1526");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.CanResizeWithGrip;
            BorderBrush = B("#a78bfa");
            BorderThickness = new Thickness(1);

            BuildUI();
            LoadData();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // add bar
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // datagrid
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // totals
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // buttons

            // ══════════════════════════════════════
            //  HEADER
            // ══════════════════════════════════════
            var header = new Border
            {
                Background = Grad("#111b32", "#0c1220"),
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
                Text = "🧂",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = $"ربط المكونات — {_productName}",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });

            var hSubRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
            _headerCostTxt = new TextBlock
            {
                Text = "التكلفة الحالية: 0.00 ج",
                FontSize = 11,
                Foreground = B("#a78bfa"),
                Margin = new Thickness(0, 0, 16, 0)
            };
            _countTxt = new TextBlock
            {
                Text = "عدد المكونات: 0",
                FontSize = 11,
                Foreground = B("#4a6080")
            };
            hSubRow.Children.Add(_headerCostTxt);
            hSubRow.Children.Add(_countTxt);
            hInfo.Children.Add(hSubRow);

            // شارة عدد المكونات
            var countBadge = new Border
            {
                Background = B("#1a0e2a"),
                BorderBrush = B("#a78bfa"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            countBadge.Child = new TextBlock
            {
                Text = "📋 وصفة المنتج",
                FontSize = 12,
                FontWeight = FontWeights.Black,
                Foreground = B("#a78bfa")
            };

            Grid.SetColumn(hIcon, 0); hRow.Children.Add(hIcon);
            Grid.SetColumn(hInfo, 1); hRow.Children.Add(hInfo);
            Grid.SetColumn(countBadge, 2); hRow.Children.Add(countBadge);
            header.Child = hRow;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══════════════════════════════════════
            //  ADD / EDIT INGREDIENT BAR
            // ══════════════════════════════════════
            var addBar = new Border
            {
                Background = B("#0c1221"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 12, 18, 12)
            };

            var addBarStack = new StackPanel();

            // ── شارة وضع التعديل (تظهر بس وقت التعديل) ──
            var editBadgeRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            _editBadge = new TextBlock
            {
                Text = "✏️  وضع التعديل — بتعدّل كمية مادة موجودة بالفعل",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = B("#ffd166"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var cancelEditLink = new TextBlock
            {
                Text = "  (إلغاء التعديل)",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = B("#E63946"),
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            cancelEditLink.MouseLeftButtonUp += (_, _) => CancelEdit();
            editBadgeRow.Children.Add(_editBadge);
            editBadgeRow.Children.Add(cancelEditLink);
            editBadgeRow.Visibility = Visibility.Collapsed;
            addBarStack.Children.Add(editBadgeRow);
            _editBadgeRow = editBadgeRow;

            var addGrid = new Grid();
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // ══ Dark ComboBox ══
            _ingCombo = BuildDarkComboBox();
            _ingCombo.DisplayMemberPath = "Name";

            // TextBox للكمية
            _qtyBox = UiHelper.MakeTB("1.00");
            _qtyBox.Width = 140;
            _qtyBox.TextAlignment = TextAlignment.Center;
            _qtyBox.GotFocus += (_, _) => _qtyBox.SelectAll();
            _qtyBox.PreviewTextInput += (_, e) =>
            {
                string ch = e.Text ?? "";
                string cur = _qtyBox.Text ?? "";
                if (ch == "." && !cur.Contains(".")) return;
                if (ch == "," && !cur.Contains(","))
                {
                    _qtyBox.Text = cur + ".";
                    _qtyBox.CaretIndex = _qtyBox.Text.Length;
                    e.Handled = true;
                    return;
                }
                e.Handled = ch.Length != 1 || !char.IsDigit(ch[0]);
            };
            _qtyBox.TextChanged += (_, _) => UpdateTotals();

            var qtyUnit = new Border
            {
                Background = B("#0a1520"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Padding = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            qtyUnit.Child = new TextBlock
            {
                Text = "الكمية",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // زر الإضافة / التحديث
            _addBtn = UiHelper.MakeBtn("➕  إضافة مادة", "#06d6a0", B("#020f0a"), () => AddIngredient(),
                paddingV: 12, fontSize: 13);
            _addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.45
            };

            var qtyWrap = new Grid();
            qtyWrap.ColumnDefinitions.Add(new ColumnDefinition());
            qtyWrap.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(_qtyBox, 0); qtyWrap.Children.Add(_qtyBox);
            Grid.SetColumn(qtyUnit, 1); qtyWrap.Children.Add(qtyUnit);

            Grid.SetColumn(_ingCombo, 0); addGrid.Children.Add(_ingCombo);
            Grid.SetColumn(qtyWrap, 2); addGrid.Children.Add(qtyWrap);
            Grid.SetColumn(_addBtn, 4); addGrid.Children.Add(_addBtn);

            addBarStack.Children.Add(addGrid);
            addBar.Child = addBarStack;
            Grid.SetRow(addBar, 1);
            root.Children.Add(addBar);

            // ══════════════════════════════════════
            //  DATAGRID
            // ══════════════════════════════════════
            _dg = UiHelper.BuildGrid(
                rowBg: "#0b1020",
                altBg: "#080d1a",
                headerBg: "#0c1530",
                headerFg: "#a78bfa",
                accent: "#a78bfa",
                hoverBg: "#111d38",
                selBg: "#1a2d50",
                cellFg: "#c8d8f0",
                rowHeight: 48,
                headerHeight: 46
            );
            _dg.IsReadOnly = true;
            _dg.SelectionMode = DataGridSelectionMode.Single;
            _dg.MouseDoubleClick += (_, _) => LoadForEdit(); // ← دبل كليك للتعديل

            // المادة (Ingredient Name)
            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "المادة",
                Binding = new Binding("IngredientName"),
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

            // الوحدة (Unit)
            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الوحدة",
                Binding = new Binding("IngredientUnit"),
                Width = 90,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B("#7ab8f5")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            // الكمية المطلوبة (Qty Used)
            _dg.Columns.Add(UiHelper.ColNum("الكمية المطلوبة", "QtyUsed", 120, "#ffd166"));

            // التكلفة/وحدة (Cost/Unit)
            _dg.Columns.Add(UiHelper.ColNum("التكلفة/وحدة", "CostPerUnit", 120, "#a78bfa"));

            // التكلفة الإجمالية (Total Cost)
            _dg.Columns.Add(UiHelper.ColNum("التكلفة الإجمالية", "TotalCost", 130, "#06d6a0"));

            _dg.ItemsSource = _items;

            var dgHint = new TextBlock
            {
                Text = "💡 دبل كليك على أي صف لتعديل الكمية",
                FontSize = 10,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 6, 0, 0)
            };

            var dgWrap = new Border
            {
                BorderBrush = B("#a78bfa"),
                BorderThickness = new Thickness(1, 0, 1, 0),
                Child = new ScrollViewer
                {
                    Content = _dg,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = B("#0f1526")
                }
            };
            Grid.SetRow(dgWrap, 2);
            root.Children.Add(dgWrap);

            // ══════════════════════════════════════
            //  TOTALS BAR
            // ══════════════════════════════════════
            var totalsBar = new Border
            {
                Background = Grad("#0e1a30", "#0a1020"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(18, 14, 18, 14)
            };
            var totalsGrid = new Grid();
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // إجمالي التكلفة
            var totalStat = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            totalStat.Children.Add(new TextBlock
            {
                Text = "إجمالي التكلفة",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 2)
            });
            _totalCostTxt = new TextBlock
            {
                Text = "0.00 ج",
                FontSize = 20,
                FontWeight = FontWeights.Black,
                Foreground = B("#06d6a0"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _totalCostTxt.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            totalStat.Children.Add(_totalCostTxt);

            // السعر المقترح 50%
            var price50Stat = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            price50Stat.Children.Add(new TextBlock
            {
                Text = "السعر المقترح (هامش 50%)",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 2)
            });
            _price50Txt = new TextBlock
            {
                Text = "0.00 ج",
                FontSize = 20,
                FontWeight = FontWeights.Black,
                Foreground = B("#ffd166"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _price50Txt.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#ffd166"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            price50Stat.Children.Add(_price50Txt);

            // السعر المقترح 100%
            var price100Stat = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            price100Stat.Children.Add(new TextBlock
            {
                Text = "السعر المقترح (هامش 100%)",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 2)
            });
            _price100Txt = new TextBlock
            {
                Text = "0.00 ج",
                FontSize = 20,
                FontWeight = FontWeights.Black,
                Foreground = B("#a78bfa"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _price100Txt.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#a78bfa"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            price100Stat.Children.Add(_price100Txt);

            Grid.SetColumn(totalStat, 0); totalsGrid.Children.Add(totalStat);
            Grid.SetColumn(price50Stat, 1); totalsGrid.Children.Add(price50Stat);
            Grid.SetColumn(price100Stat, 2); totalsGrid.Children.Add(price100Stat);
            totalsBar.Child = totalsGrid;
            Grid.SetRow(totalsBar, 3);
            root.Children.Add(totalsBar);

            // ══════════════════════════════════════
            //  BUTTONS BAR
            // ══════════════════════════════════════
            var btnBar = new Border
            {
                Background = B("#090e1a"),
                BorderBrush = B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 12, 18, 14)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // زر حذف المادة المحددة
            var deleteBtn = UiHelper.MakeBtn("🗑  حذف المادة المحددة", "#1a080a", B("#E63946"), () => DeleteIngredient(),
                paddingV: 12, fontSize: 13);
            deleteBtn.BorderBrush = B("#E63946");
            deleteBtn.BorderThickness = new Thickness(1);

            // زر الإلغاء
            var cancelBtn = UiHelper.MakeBtn("✕  إلغاء", "#12192e", B("#8892a4"), () => { DialogResult = false; Close(); },
                paddingV: 12, fontSize: 13);
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            // زر الحفظ
            var saveBtn = UiHelper.MakeBtn("💾  حفظ الوصفة", "#a78bfa", B("#0a0800"), () => SaveAll(),
                paddingV: 12, fontSize: 13);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#a78bfa"),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.45
            };

            Grid.SetColumn(deleteBtn, 0); btnGrid.Children.Add(deleteBtn);
            Grid.SetColumn(cancelBtn, 2); btnGrid.Children.Add(cancelBtn);
            Grid.SetColumn(saveBtn, 4); btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 4);
            root.Children.Add(btnBar);

            Content = root;
        }

        StackPanel _editBadgeRow = null!;

        // ══════════════════════════════════════
        //  LOAD DATA
        // ══════════════════════════════════════
        void LoadData()
        {
            _items.Clear();
            foreach (var item in _db.GetProductIngredients(_productId))
                _items.Add(item);

            // تحميل كل المكونات المتاحة في الـ ComboBox
            _ingCombo.Items.Clear();
            var ings = new List<Ingredient>();
            using var c = DatabaseHelper.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,Unit,CostPerUnit FROM Ingredients ORDER BY Name";
            using var r = cmd.ExecuteReader();
            while (r.Read()) ings.Add(new Ingredient
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Unit = r.GetString(2),
                CostPerUnit = r.GetDouble(3)
            });

            foreach (var ing in ings)
            {
                bool alreadyLinked = false;
                foreach (var item in _items)
                {
                    if (item.IngredientId == ing.Id) { alreadyLinked = true; break; }
                }
                if (!alreadyLinked)
                    _ingCombo.Items.Add(ing);
            }

            if (_ingCombo.Items.Count > 0)
                _ingCombo.SelectedIndex = 0;

            UpdateTotals();
        }

        // ══════════════════════════════════════
        //  ADD / UPDATE INGREDIENT
        // ══════════════════════════════════════
        void AddIngredient()
        {
            if (_ingCombo.SelectedItem is not Ingredient ing)
            {
                MessageBox.Show("اختر مادة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var qtyStr = _qtyBox.Text.Replace(',', '.');
            if (!double.TryParse(qtyStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صحيحة أكبر من صفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _db.SaveProductIngredient(_productId, ing.Id, qty);

            bool wasEditing = _editingIngredientId != null;
            _editingIngredientId = null;
            SetEditModeVisual(false);

            LoadData();
            _qtyBox.Text = "1.00";

            if (wasEditing)
            {
                _headerCostTxt.Text = _headerCostTxt.Text; // نفس السلوك، بس ممكن تضيف Toast لو حابب
            }
        }

        // ══════════════════════════════════════
        //  LOAD ROW FOR EDIT (double-click)
        // ══════════════════════════════════════
        void LoadForEdit()
        {
            if (_dg.SelectedItem is not ProductIngredient sel) return;

            // المادة دي متشالة من الكومبو لأنها مرتبطة بالفعل — هنضيفها مؤقتًا
            var existing = FindIngredientById(sel.IngredientId);
            if (existing == null) return;

            bool alreadyInCombo = false;
            foreach (var obj in _ingCombo.Items)
                if (obj is Ingredient i && i.Id == existing.Id) { alreadyInCombo = true; break; }

            if (!alreadyInCombo)
                _ingCombo.Items.Insert(0, existing);

            foreach (var obj in _ingCombo.Items)
                if (obj is Ingredient i2 && i2.Id == existing.Id)
                {
                    _ingCombo.SelectedItem = i2;
                    break;
                }

            _qtyBox.Text = sel.QtyUsed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            _qtyBox.SelectAll();
            _qtyBox.Focus();

            _editingIngredientId = sel.IngredientId;
            SetEditModeVisual(true);
        }

        void CancelEdit()
        {
            _editingIngredientId = null;
            SetEditModeVisual(false);
            _qtyBox.Text = "1.00";
            LoadData(); // يرجّع الكومبو للحالة الطبيعية (من غير المادة المضافة مؤقتًا)
        }

        void SetEditModeVisual(bool editing)
        {
            _editBadgeRow.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            _addBtn.Content = editing ? "💾  تحديث الكمية" : "➕  إضافة مادة";
            _addBtn.Background = B(editing ? "#ffd166" : "#06d6a0");
            _addBtn.Foreground = B(editing ? "#241800" : "#020f0a");
        }

        Ingredient? FindIngredientById(int id)
        {
            using var c = DatabaseHelper.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,Unit,CostPerUnit FROM Ingredients WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
                return new Ingredient
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Unit = r.GetString(2),
                    CostPerUnit = r.GetDouble(3)
                };
            return null;
        }

        // ══════════════════════════════════════
        //  DELETE INGREDIENT
        // ══════════════════════════════════════
        void DeleteIngredient()
        {
            if (_dg.SelectedItem is not ProductIngredient sel)
            {
                MessageBox.Show("اختر مادة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(
                $"حذف مادة \"{sel.IngredientName}\" من وصفة المنتج؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            if (_editingIngredientId == sel.IngredientId)
            {
                _editingIngredientId = null;
                SetEditModeVisual(false);
            }

            _db.DeleteProductIngredient(_productId, sel.IngredientId);
            LoadData();
        }

        // ══════════════════════════════════════
        //  SAVE ALL
        // ══════════════════════════════════════
        void SaveAll()
        {
            // تحديث Product.Cost في قاعدة البيانات
            double totalCost = _db.CalculateProductCost(_productId);
            using var c = DatabaseHelper.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Products SET Cost=@co WHERE Id=@id";
            cmd.Parameters.AddWithValue("@co", totalCost);
            cmd.Parameters.AddWithValue("@id", _productId);
            cmd.ExecuteNonQuery();

            MessageBox.Show(
                $"تم حفظ وصفة \"{_productName}\" بنجاح\nتكلفة الوصفة: {totalCost:F2} ج",
                "تم الحفظ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        // ══════════════════════════════════════
        //  UPDATE TOTALS (live)
        // ══════════════════════════════════════
        void UpdateTotals()
        {
            double totalCost = 0;
            foreach (var item in _items)
                totalCost += item.TotalCost;

            _totalCostTxt.Text = $"{totalCost:F2} ج";
            _price50Txt.Text = $"{totalCost * 1.5:F2} ج";
            _price100Txt.Text = $"{totalCost * 2.0:F2} ج";
            _headerCostTxt.Text = $"التكلفة الحالية: {totalCost:F2} ج";
            _countTxt.Text = $"عدد المكونات: {_items.Count}";
        }

        // ══════════════════════════════════════════════
        //  DARK COMBOBOX BUILDER
        // ══════════════════════════════════════════════
        ComboBox BuildDarkComboBox()
        {
            var itemBorderFactory = new FrameworkElementFactory(typeof(Border));
            itemBorderFactory.SetBinding(Border.BackgroundProperty,
                new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            itemBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            itemBorderFactory.SetBinding(Border.PaddingProperty,
                new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var itemCp = new FrameworkElementFactory(typeof(ContentPresenter));
            itemCp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            itemCp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            itemBorderFactory.AppendChild(itemCp);
            var itemTemplate = new ControlTemplate(typeof(ComboBoxItem)) { VisualTree = itemBorderFactory };

            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#0f1526")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#eef0f2")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(12, 10, 12, 10)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.FontSizeProperty, 13.0));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.FontWeightProperty, FontWeights.Bold));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BorderThicknessProperty, new Thickness(0)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.MarginProperty, new Thickness(4, 2, 4, 2)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.TemplateProperty, itemTemplate));
            var hov = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#1a2d50")));
            hov.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#06d6a0")));
            itemStyle.Triggers.Add(hov);
            var sel = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#06d6a0")));
            sel.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#020f0a")));
            itemStyle.Triggers.Add(sel);
            var selHov = new MultiTrigger();
            selHov.Conditions.Add(new Condition(ComboBoxItem.IsSelectedProperty, true));
            selHov.Conditions.Add(new Condition(ComboBoxItem.IsMouseOverProperty, true));
            selHov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#04b888")));
            selHov.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#020f0a")));
            itemStyle.Triggers.Add(selHov);

            var arrowPath = new FrameworkElementFactory(typeof(Path));
            arrowPath.SetValue(Path.DataProperty, Geometry.Parse("M 0 0 L 4 4 L 8 0 Z"));
            arrowPath.SetValue(Path.FillProperty, B("#a78bfa"));
            arrowPath.SetValue(Path.WidthProperty, 8.0);
            arrowPath.SetValue(Path.HeightProperty, 4.0);
            arrowPath.SetValue(Path.StretchProperty, Stretch.Fill);
            arrowPath.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            arrowPath.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var toggleBorder = new FrameworkElementFactory(typeof(Border));
            toggleBorder.SetValue(Border.BackgroundProperty, B("#0f1526"));
            toggleBorder.SetValue(Border.WidthProperty, 32.0);
            toggleBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0));
            toggleBorder.AppendChild(arrowPath);

            var toggleBtn = new FrameworkElementFactory(typeof(ToggleButton));
            toggleBtn.SetValue(ToggleButton.BackgroundProperty, Brushes.Transparent);
            toggleBtn.SetValue(ToggleButton.BorderThicknessProperty, new Thickness(0));
            toggleBtn.SetValue(FrameworkElement.WidthProperty, 32.0);
            toggleBtn.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            toggleBtn.SetBinding(ToggleButton.IsCheckedProperty,
                new Binding("IsDropDownOpen") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            toggleBtn.SetValue(ToggleButton.TemplateProperty,
                new ControlTemplate(typeof(ToggleButton)) { VisualTree = toggleBorder });

            // ── صندوق الاختيار المغلق: template صريح بيقرا Name مباشرة ──
            var selTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            selTextFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            var selTemplate = new DataTemplate { VisualTree = selTextFactory };

            var selContent = new FrameworkElementFactory(typeof(ContentPresenter));
            selContent.SetBinding(ContentPresenter.ContentProperty,
                new Binding("SelectionBoxItem") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            selContent.SetValue(ContentPresenter.ContentTemplateProperty, selTemplate);
            selContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            selContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            selContent.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 8, 0));

            var innerGrid = new FrameworkElementFactory(typeof(Grid));
            innerGrid.AppendChild(selContent);
            innerGrid.AppendChild(toggleBtn);

            var outerBorder = new FrameworkElementFactory(typeof(Border));
            outerBorder.SetValue(Border.BackgroundProperty, B("#0f1526"));
            outerBorder.SetValue(Border.BorderBrushProperty, B("#a78bfa"));
            outerBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            outerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            outerBorder.SetValue(Border.PaddingProperty, new Thickness(0));
            outerBorder.Name = "MainBorder";
            outerBorder.AppendChild(innerGrid);

            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            itemsPresenter.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 4));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.BackgroundProperty, B("#0f1526"));
            scrollViewer.SetValue(ScrollViewer.CanContentScrollProperty, true);
            scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            scrollViewer.AppendChild(itemsPresenter);

            var popupBorder = new FrameworkElementFactory(typeof(Border));
            popupBorder.SetValue(Border.BackgroundProperty, B("#0f1526"));
            popupBorder.SetValue(Border.BorderBrushProperty, B("#a78bfa"));
            popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            popupBorder.SetValue(Border.PaddingProperty, new Thickness(4));
            popupBorder.SetValue(FrameworkElement.MinWidthProperty, 200.0);
            popupBorder.AppendChild(scrollViewer);

            var popup = new FrameworkElementFactory(typeof(Popup));
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Fade);
            popup.SetValue(Popup.StaysOpenProperty, false);
            popup.Name = "PART_Popup";
            popup.SetBinding(Popup.IsOpenProperty,
                new Binding("IsDropDownOpen") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            popup.SetBinding(Popup.MinWidthProperty,
                new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            popup.AppendChild(popupBorder);

            var rootGrid = new FrameworkElementFactory(typeof(Grid));
            rootGrid.AppendChild(outerBorder);
            rootGrid.AppendChild(popup);

            var comboTemplate = new ControlTemplate(typeof(ComboBox)) { VisualTree = rootGrid };
            var focusTrigger = new Trigger { Property = ComboBox.IsDropDownOpenProperty, Value = true };
            focusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, B("#a78bfa"), "MainBorder"));
            focusTrigger.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "MainBorder"));
            comboTemplate.Triggers.Add(focusTrigger);

            var combo = new ComboBox
            {
                Background = B("#0f1526"),
                Foreground = B("#eef0f2"),
                BorderBrush = B("#a78bfa"),
                BorderThickness = new Thickness(1),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Tahoma"),
                Height = 42,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = comboTemplate,
                ItemContainerStyle = itemStyle
            };

            combo.Resources[SystemColors.WindowBrushKey] = B("#0f1526");
            combo.Resources[SystemColors.WindowTextBrushKey] = B("#eef0f2");
            combo.Resources[SystemColors.HighlightBrushKey] = B("#06d6a0");
            combo.Resources[SystemColors.HighlightTextBrushKey] = B("#020f0a");

            return combo;
        }
    }
}