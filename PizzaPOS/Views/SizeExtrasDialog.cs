// Views/SizeExtrasDialog.cs
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using PizzaPOS.Models;

namespace PizzaPOS.Views
{
    public class SizeExtrasDialog : Window
    {
        public OrderItem? Result { get; private set; }

        readonly Product _product;
        readonly List<ProductSize> _sizes;
        readonly List<ProductExtra> _extras;

        ProductSize? _selectedSize;
        TextBlock _totalTxt = null!;
        TextBlock _totalLabelTxt = null!;

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

        public SizeExtrasDialog(Product product,
                                List<ProductSize> sizes,
                                List<ProductExtra> extras)
        {
            _product = product;
            _sizes = sizes;
            _extras = extras;

            Title = product.Name;
            Width = 460;
            SizeToContent = SizeToContent.Height;
            MaxHeight = 700;
            Background = B("#070b14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;
            BorderBrush = B("#FF6B35");
            BorderThickness = new Thickness(1);

            BuildUI();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // hero
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // scroll
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // total
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // buttons

            // ══════════════════════════════════════
            //  HERO HEADER
            // ══════════════════════════════════════
            var heroBorder = new Border
            {
                Background = Grad("#0f1830", "#0c1220"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 18, 20, 18)
            };

            var heroGrid = new Grid();
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition());
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // ── أيقونة المنتج (دائرة متوهجة) ──
            var iconOuter = new Border
            {
                Width = 68,
                Height = 68,
                CornerRadius = new CornerRadius(34),
                Background = Grad("#1e2d50", "#0f1830"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 0, 16, 0)
            };
            iconOuter.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 28,
                ShadowDepth = 0,
                Opacity = 0.55
            };
            iconOuter.Child = new TextBlock
            {
                Text = _product.Icon,
                FontSize = 34,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // ── معلومات المنتج ──
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoStack.Children.Add(new TextBlock
            {
                Text = _product.Name,
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#f0f4ff"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            // شارة السعر الأساسي
            var basePriceBadge = new Border
            {
                Background = B("#1a0e08"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            basePriceBadge.Child = new TextBlock
            {
                Text = $"💰 {_product.Price:F0} ج",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = B("#FF6B35")
            };
            infoStack.Children.Add(basePriceBadge);

            Grid.SetColumn(iconOuter, 0); heroGrid.Children.Add(iconOuter);
            Grid.SetColumn(infoStack, 1); heroGrid.Children.Add(infoStack);
            heroBorder.Child = heroGrid;
            Grid.SetRow(heroBorder, 0);
            root.Children.Add(heroBorder);

            // ══════════════════════════════════════
            //  SCROLL CONTENT
            // ══════════════════════════════════════
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 360,
                Background = B("#070b14")
            };
            var contentSp = new StackPanel { Margin = new Thickness(16, 14, 16, 6) };

            // ── SIZES ──
            if (_sizes.Count > 0)
            {
                contentSp.Children.Add(SectionLabel("📏", "اختر الحجم"));

                var sizeWrap = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 16)
                };

                _selectedSize = _sizes[0];

                foreach (var size in _sizes)
                {
                    bool isFirst = size == _sizes[0];
                    var cap = size;

                    // كارت الحجم
                    var card = new Border
                    {
                        Width = 118,
                        Margin = new Thickness(0, 0, 8, 8),
                        CornerRadius = new CornerRadius(12),
                        Background = isFirst ? Grad("#1e2d50", "#162238") : B("#0c1525"),
                        BorderBrush = isFirst ? B("#FF6B35") : B("#1e2d4a"),
                        BorderThickness = new Thickness(isFirst ? 2 : 1),
                        Padding = new Thickness(10, 10, 10, 10),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = false // not selected state for toggle
                    };
                    if (isFirst)
                    {
                        card.Effect = new DropShadowEffect
                        {
                            Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                            BlurRadius = 16,
                            ShadowDepth = 0,
                            Opacity = 0.35
                        };
                    }

                    var cardSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

                    // دائرة صغيرة للاختيار
                    var dot = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = isFirst ? B("#FF6B35") : B("#1e2d4a"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 6)
                    };

                    var sizeNameTxt = new TextBlock
                    {
                        Text = size.Name,
                        FontSize = 13,
                        FontWeight = FontWeights.Black,
                        Foreground = isFirst ? B("#FF6B35") : B("#7a90b0"),
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 4)
                    };

                    var priceTxt = new TextBlock
                    {
                        Text = size.ExtraPrice == 0 ? "أساسي" : $"+{size.ExtraPrice:F0} ج",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = size.ExtraPrice == 0 ? B("#06d6a0") : B("#ffd166"),
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    cardSp.Children.Add(dot);
                    cardSp.Children.Add(sizeNameTxt);
                    cardSp.Children.Add(priceTxt);
                    card.Child = cardSp;

                    // جمع كل الكروت لتحديثها عند الضغط
                    sizeWrap.Children.Add(card);

                    card.MouseLeftButtonUp += (_, _) =>
                    {
                        _selectedSize = cap;
                        UpdateTotal();

                        // إعادة رسم كل الكروت
                        foreach (Border b in sizeWrap.Children.OfType<Border>())
                        {
                            bool active = (b == card);
                            b.Background = active
                                ? Grad("#1e2d50", "#162238")
                                : B("#0c1525");
                            b.BorderBrush = active ? B("#FF6B35") : B("#1e2d4a");
                            b.BorderThickness = new Thickness(active ? 2 : 1);
                            b.Effect = active
                                ? new DropShadowEffect
                                {
                                    Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                                    BlurRadius = 16,
                                    ShadowDepth = 0,
                                    Opacity = 0.35
                                }
                                : null;

                            if (b.Child is StackPanel sp)
                            {
                                if (sp.Children[0] is Ellipse e2)
                                    e2.Fill = active ? B("#FF6B35") : B("#1e2d4a");
                                if (sp.Children[1] is TextBlock t1)
                                    t1.Foreground = active ? B("#FF6B35") : B("#7a90b0");
                            }
                        }
                    };
                }

                contentSp.Children.Add(sizeWrap);
            }

            // ── EXTRAS ──
            if (_extras.Count > 0)
            {
                contentSp.Children.Add(SectionLabel("✨", "الإضافات"));

                foreach (var extra in _extras)
                {
                    var cap = extra;

                    var extraCard = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        Background = B("#0c1525"),
                        BorderBrush = B("#1e2d4a"),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(14, 10, 14, 10),
                        Margin = new Thickness(0, 0, 0, 6),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    var extraRow = new Grid();
                    extraRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    extraRow.ColumnDefinitions.Add(new ColumnDefinition());
                    extraRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // مربع الاختيار المخصص
                    var checkBox = new Border
                    {
                        Width = 20,
                        Height = 20,
                        CornerRadius = new CornerRadius(5),
                        Background = B("#0a1020"),
                        BorderBrush = B("#2a3d5a"),
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(0, 0, 12, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var checkMark = new TextBlock
                    {
                        Text = "✓",
                        FontSize = 13,
                        FontWeight = FontWeights.Black,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Visibility = Visibility.Collapsed
                    };
                    checkBox.Child = checkMark;

                    var extraName = new TextBlock
                    {
                        Text = extra.Name,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = B("#c0d0e8"),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var priceBadge = new Border
                    {
                        Background = B("#12200a"),
                        BorderBrush = B("#ffd166"),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 3, 8, 3),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    priceBadge.Child = new TextBlock
                    {
                        Text = $"+{extra.Price:F0} ج",
                        Foreground = B("#ffd166"),
                        FontSize = 11,
                        FontWeight = FontWeights.Black
                    };

                    Grid.SetColumn(checkBox, 0); extraRow.Children.Add(checkBox);
                    Grid.SetColumn(extraName, 1); extraRow.Children.Add(extraName);
                    Grid.SetColumn(priceBadge, 2); extraRow.Children.Add(priceBadge);
                    extraCard.Child = extraRow;

                    // Toggle on click
                    extraCard.MouseLeftButtonUp += (_, _) =>
                    {
                        cap.IsSelected = !cap.IsSelected;
                        if (cap.IsSelected)
                        {
                            checkBox.Background = B("#06d6a0");
                            checkBox.BorderBrush = B("#06d6a0");
                            checkMark.Visibility = Visibility.Visible;
                            extraCard.Background = Grad("#0c2018", "#081810");
                            extraCard.BorderBrush = B("#06d6a0");
                            extraCard.BorderThickness = new Thickness(1.5);
                            extraName.Foreground = B("#06d6a0");
                        }
                        else
                        {
                            checkBox.Background = B("#0a1020");
                            checkBox.BorderBrush = B("#2a3d5a");
                            checkMark.Visibility = Visibility.Collapsed;
                            extraCard.Background = B("#0c1525");
                            extraCard.BorderBrush = B("#1e2d4a");
                            extraCard.BorderThickness = new Thickness(1);
                            extraName.Foreground = B("#c0d0e8");
                        }
                        UpdateTotal();
                    };

                    contentSp.Children.Add(extraCard);
                }
            }

            scroll.Content = contentSp;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // ══════════════════════════════════════
            //  TOTAL BAR
            // ══════════════════════════════════════
            var totalBar = new Border
            {
                Background = Grad("#0e1a30", "#0a1020"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(20, 14, 20, 14)
            };

            var totalGrid = new Grid();
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var totalLabelStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            totalLabelStack.Children.Add(new TextBlock
            {
                Text = "الإجمالي",
                FontSize = 12,
                Foreground = B("#4a6080"),
                FontWeight = FontWeights.Bold
            });
            totalLabelStack.Children.Add(new TextBlock
            {
                Text = "شامل الحجم والإضافات",
                FontSize = 10,
                Foreground = B("#2a3d5a"),
                Margin = new Thickness(0, 2, 0, 0)
            });

            _totalTxt = new TextBlock
            {
                FontSize = 26,
                FontWeight = FontWeights.Black,
                Foreground = B("#06d6a0"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _totalTxt.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.6
            };

            Grid.SetColumn(totalLabelStack, 0); totalGrid.Children.Add(totalLabelStack);
            Grid.SetColumn(_totalTxt, 1); totalGrid.Children.Add(_totalTxt);
            totalBar.Child = totalGrid;
            Grid.SetRow(totalBar, 2);
            root.Children.Add(totalBar);
            UpdateTotal();

            // ══════════════════════════════════════
            //  BUTTONS
            // ══════════════════════════════════════
            var btnBar = new Border
            {
                Background = B("#070b14"),
                Padding = new Thickness(16, 12, 16, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // زر الإلغاء
            var cancelBtn = MakeBtn("✕  إلغاء", "#0f1828", B("#4a6080"), isAccent: false);
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);
            cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };

            // زر الإضافة
            var addBtn = MakeBtn("➕  إضافة للأوردر", "#06d6a0", B("#070b14"), isAccent: true);
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 22,
                ShadowDepth = 0,
                Opacity = 0.55
            };
            addBtn.Click += AddBtn_Click;

            Grid.SetColumn(cancelBtn, 0); btnGrid.Children.Add(cancelBtn);
            Grid.SetColumn(addBtn, 2); btnGrid.Children.Add(addBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 3);
            root.Children.Add(btnBar);

            Content = root;
        }

        // ── Section Label ────────────────────────────
        UIElement SectionLabel(string emoji, string title)
        {
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var pill = new Border
            {
                Background = B("#0f1a2e"),
                BorderBrush = B("#FF6B35"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Padding = new Thickness(10, 4, 10, 6)
            };
            var pillSp = new StackPanel { Orientation = Orientation.Horizontal };
            pillSp.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 13,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            pillSp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Black,
                Foreground = B("#ffd166"),
                VerticalAlignment = VerticalAlignment.Center
            });
            pill.Child = pillSp;
            sp.Children.Add(pill);
            return sp;
        }

        // ── Update Total ─────────────────────────────
        void UpdateTotal()
        {
            double sizeExtra = _selectedSize?.ExtraPrice ?? 0;
            double extrasTotal = _extras.Where(e => e.IsSelected).Sum(e => e.Price);
            double total = _product.Price + sizeExtra + extrasTotal;
            _totalTxt.Text = $"{total:F2} ج";
        }

        // ── Add to Order ─────────────────────────────
        void AddBtn_Click(object s, RoutedEventArgs e)
        {
            double sizeExtra = _selectedSize?.ExtraPrice ?? 0;
            var selExtras = _extras.Where(ex => ex.IsSelected).ToList();
            double extrasTotal = selExtras.Sum(ex => ex.Price);
            string extrasNote = selExtras.Count > 0
                ? string.Join("، ", selExtras.Select(ex => ex.Name)) : "";
            string extrasKey = $"{_selectedSize?.Name ?? ""}|" +
                                 string.Join(",", selExtras.Select(ex => ex.Id).OrderBy(x => x));

            Result = new OrderItem
            {
                ProductId = _product.Id,
                Name = _product.Name,
                Icon = _product.Icon,
                BasePrice = _product.Price,
                Cost = _product.Cost > 0 ? _product.Cost : _product.Price * 0.4,
                SizeName = _selectedSize?.Name,
                SizeExtraPrice = sizeExtra,
                ExtrasNote = string.IsNullOrEmpty(extrasNote) ? null : extrasNote,
                ExtrasPrice = extrasTotal,
                ExtrasKey = extrasKey,
                Qty = 1
            };

            DialogResult = true;
            Close();
        }

        // ── Button Factory ───────────────────────────
        Button MakeBtn(string text, string bgHex, Brush fg, bool isAccent)
        {
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetBinding(Border.BackgroundProperty,
                new Binding("Background")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            factory.SetBinding(Border.PaddingProperty,
                new Binding("Padding")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.BorderBrushProperty,
                new Binding("BorderBrush")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.BorderThicknessProperty,
                new Binding("BorderThickness")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(cp);

            return new Button
            {
                Content = text,
                Background = B(bgHex),
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 14, 0, 14),
                FontWeight = FontWeights.Black,
                FontSize = isAccent ? 14 : 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = new ControlTemplate(typeof(Button)) { VisualTree = factory }
            };
        }
    }
}