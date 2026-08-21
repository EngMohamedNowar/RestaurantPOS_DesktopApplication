// Views/OffersWindow.cs – SendOfferDialog
using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PizzaPOS.Views
{
    public class SendOfferDialog : Window
    {
        readonly Offer _offer;
        readonly List<Customer> _allCustomers;
        readonly ObservableCollection<Customer> _filtered = new();
        CheckBox _chkSelectAll = null!;
        ListBox _lstCustomers = null!;
        TextBlock _countTxt = null!;
        TextBox _searchBox = null!;
        readonly AppDbContext _db = new();

        const string ShopName = "NAPOLI Pizza";

        public SendOfferDialog(Offer offer, List<Customer> customers)
        {
            _offer = offer;
            _allCustomers = customers;
            Title = "إرسال العرض عبر واتساب";
            Width = 600; Height = 640;
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
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ══ Header ══════════════════════════════════════════════════════
            var header = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#075e54"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#075e54"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "💬",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إرسال عبر واتساب",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#25d366")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"العرض: {_offer.Title}",
                FontSize = 12,
                Foreground = UiHelper.B("#5a6a80"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Message Preview ═════════════════════════════════════════════
            var preview = new Border
            {
                Background = UiHelper.B("#0a1f1a"),
                BorderBrush = UiHelper.B("#075e54"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(18, 14, 18, 0),
                Padding = new Thickness(16, 12, 16, 12)
            };
            var previewStack = new StackPanel();
            previewStack.Children.Add(new TextBlock
            {
                Text = "معاينة الرسالة:",
                FontSize = 11,
                Foreground = UiHelper.B("#075e54"),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            });
            previewStack.Children.Add(new TextBlock
            {
                Text = BuildMessagePreview(),
                Foreground = UiHelper.B("#25d366"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                LineHeight = 20
            });
            preview.Child = previewStack;
            Grid.SetRow(preview, 1);
            root.Children.Add(preview);

            // ══ Search + Select All ═════════════════════════════════════════
            var toolbar = new Grid { Margin = new Thickness(18, 12, 18, 0) };
            toolbar.ColumnDefinitions.Add(new ColumnDefinition());
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchWrap = new Border
            {
                Background = UiHelper.B("#0d1a2a"),
                BorderBrush = UiHelper.B("#1a2d50"),
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
                Width = 280,
                CaretBrush = UiHelper.B("#25d366"),
                VerticalContentAlignment = VerticalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            };
            _searchBox.TextChanged += (_, _) => DoSearch();
            searchRow.Children.Add(_searchBox);
            searchWrap.Child = searchRow;
            Grid.SetColumn(searchWrap, 0); toolbar.Children.Add(searchWrap);

            _chkSelectAll = new CheckBox
            {
                Content = "تحديد الكل",
                IsChecked = true,
                Foreground = UiHelper.B("#25d366"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _chkSelectAll.Click += (_, _) =>
            {
                foreach (var c in _filtered)
                    c.IsSelected = _chkSelectAll.IsChecked == true;
                _lstCustomers.Items.Refresh();
                UpdateCount();
            };
            Grid.SetColumn(_chkSelectAll, 1); toolbar.Children.Add(_chkSelectAll);

            Grid.SetRow(toolbar, 2);
            root.Children.Add(toolbar);

            // ══ Customer List ═══════════════════════════════════════════════
            _lstCustomers = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(18, 8, 18, 0)
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(_lstCustomers, ScrollBarVisibility.Disabled);
            _lstCustomers.SelectionChanged += (_, __) => _lstCustomers.UnselectAll();

            var itemTemplate = new DataTemplate();
            var factory = CreateCustomerItem();
            itemTemplate.VisualTree = factory;
            _lstCustomers.ItemTemplate = itemTemplate;
            _lstCustomers.ItemsSource = _filtered;

            Grid.SetRow(_lstCustomers, 3);
            root.Children.Add(_lstCustomers);

            // ══ Footer ══════════════════════════════════════════════════════
            var footer = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 14, 20, 14)
            };
            var fGrid = new Grid();
            fGrid.ColumnDefinitions.Add(new ColumnDefinition());
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _countTxt = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#25d366"),
                VerticalAlignment = VerticalAlignment.Center
            };

            var sendBtn = UiHelper.MakeBtn("إرسال واتساب", "#075e54", Brushes.White, SendWhatsApp, 12, 14);
            sendBtn.MinWidth = 160;
            sendBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#075e54"),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.4
            };

            Grid.SetColumn(_countTxt, 0); fGrid.Children.Add(_countTxt);
            Grid.SetColumn(sendBtn, 1); fGrid.Children.Add(sendBtn);
            footer.Child = fGrid;
            Grid.SetRow(footer, 4);
            root.Children.Add(footer);

            Content = root;
            LoadCustomers();
        }

        FrameworkElementFactory CreateCustomerItem()
        {
            var rootBorder = new FrameworkElementFactory(typeof(Border));
            rootBorder.SetValue(Border.BackgroundProperty, UiHelper.B("#0d1525"));
            rootBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            rootBorder.SetValue(Border.PaddingProperty, new Thickness(14, 10, 14, 10));
            rootBorder.SetValue(Border.MarginProperty, new Thickness(0, 3, 0, 3));
            rootBorder.SetValue(Border.BorderBrushProperty, UiHelper.B("#1a2d50"));
            rootBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var grid = new FrameworkElementFactory(typeof(Grid));
            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, new GridLength(36));
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col0);
            grid.AppendChild(col1);

            // Checkbox
            var cb = new FrameworkElementFactory(typeof(CheckBox));
            cb.SetValue(CheckBox.IsCheckedProperty, new Binding("IsSelected"));
            cb.SetValue(CheckBox.ForegroundProperty, UiHelper.B("#25d366"));
            cb.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            cb.SetValue(CheckBox.CursorProperty, System.Windows.Input.Cursors.Hand);
            cb.SetValue(Grid.ColumnProperty, 0);
            grid.AppendChild(cb);

            // Name + Phone
            var infoStack = new FrameworkElementFactory(typeof(StackPanel));
            infoStack.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);
            infoStack.SetValue(StackPanel.MarginProperty, new Thickness(4, 0, 0, 0));

            var nameTb = new FrameworkElementFactory(typeof(TextBlock));
            nameTb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameTb.SetValue(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2"));
            nameTb.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            nameTb.SetValue(TextBlock.FontSizeProperty, 13.0);
            infoStack.AppendChild(nameTb);

            var phoneTb = new FrameworkElementFactory(typeof(TextBlock));
            phoneTb.SetBinding(TextBlock.TextProperty, new Binding("Phone"));
            phoneTb.SetValue(TextBlock.ForegroundProperty, UiHelper.B("#5a6a80"));
            phoneTb.SetValue(TextBlock.FontSizeProperty, 11.0);
            phoneTb.SetValue(TextBlock.MarginProperty, new Thickness(0, 2, 0, 0));
            infoStack.AppendChild(phoneTb);
            infoStack.SetValue(Grid.ColumnProperty, 1);
            grid.AppendChild(infoStack);

            rootBorder.AppendChild(grid);
            return rootBorder;
        }

        void LoadCustomers()
        {
            _filtered.Clear();
            foreach (var c in _allCustomers)
            {
                c.IsSelected = true;
                _filtered.Add(c);
            }
            _lstCustomers.Items.Refresh();
            UpdateCount();
        }

        void DoSearch()
        {
            var txt = _searchBox.Text.Trim();
            _filtered.Clear();
            foreach (var c in _allCustomers)
            {
                if (string.IsNullOrEmpty(txt) ||
                    c.Name.Contains(txt, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(txt))
                {
                    _filtered.Add(c);
                }
            }
            _lstCustomers.Items.Refresh();
            UpdateCount();
        }

        void UpdateCount()
        {
            int count = 0;
            foreach (var c in _filtered) if (c.IsSelected) count++;
            _countTxt.Text = $"{count} زبون محدد";
        }

        string BuildMessagePreview()
        {
            string shopPhone = _db.GetSetting("Phone", "");

            var msg = "";
            msg += "        *NAPOLI Pizza*\n";
            msg += "━━━━━━━━━━━━━━━━━━━━━━━\n\n";
            msg += $"*{_offer.Title}*\n";

            if (!string.IsNullOrWhiteSpace(_offer.Description))
                msg += $"_{_offer.Description}_\n";

            if (_offer.DiscountPercent > 0)
                msg += $" خصم {_offer.DiscountPercent:F0}%\n";

            if (!string.IsNullOrWhiteSpace(_offer.PromoCode))
                msg += $" كود الخصم: {_offer.PromoCode}\n";

            msg += "\n━━━━━━━━━━━━━━━━━━━━━━━\n";
            msg += "اطلب الآن قبل ما ينتهي العرض\n";

            if (!string.IsNullOrWhiteSpace(shopPhone))
                msg += $"{shopPhone}";

            return msg;
        }

        void SendWhatsApp()
        {
            var selected = new List<Customer>();
            foreach (var c in _filtered)
                if (c.IsSelected) selected.Add(c);

            if (selected.Count == 0)
            {
                MessageBox.Show("اختر زبون واحد على الأقل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = BuildMessagePreview();
            string encoded = Uri.EscapeDataString(message);

            int opened = 0;
            foreach (var c in selected)
            {
                if (string.IsNullOrWhiteSpace(c.Phone)) continue;
                string phone = c.Phone.Replace(" ", "").Replace("-", "").Replace("+", "");
                if (phone.StartsWith("0"))
                    phone = "20" + phone.Substring(1);

                string url = $"https://wa.me/{phone}?text={encoded}";
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    opened++;
                }
                catch { }
            }

            MessageBox.Show(
                $"تم فتح واتساب لـ {opened} زبون!\n\nلو عندك واتساب مثبت على الجهاز، هيفتح لكل زبون مع الرسالة جاهزة.",
                "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }
}
