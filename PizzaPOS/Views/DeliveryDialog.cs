// Views/DeliveryDialog.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using PizzaPOS.Data;
using PizzaPOS.Models;

namespace PizzaPOS.Views
{
    public class DeliveryDialog : Window
    {
        public string CustomerName { get; private set; } = "";
        public string CustomerPhone { get; private set; } = "";
        public string DeliveryAddress { get; private set; } = "";
        public string DriverName { get; private set; } = "";
        public int DriverId { get; private set; }
        public int CustomerId { get; private set; }
        public double DeliveryFee { get; private set; }
        public bool IsHeld { get; private set; }

        readonly AppDbContext _db;
        readonly List<Driver> _drivers;
        readonly double _orderTotal;

        TextBox _tbName = null!;
        TextBox _tbPhone = null!;
        TextBox _tbAddress = null!;
        TextBox _tbNotes = null!;
        TextBox _tbFee = null!;
        ComboBox _cbDriver = null!;
        TextBlock _historyTxt = null!;

        Customer? _foundCustomer;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public DeliveryDialog(AppDbContext db, double orderTotal)
        {
            _db = db;
            _orderTotal = orderTotal;
            _drivers = db.GetDrivers();

            Title = "🛵 بيانات التوصيل";
            Width = 480;
            SizeToContent = SizeToContent.Height;
            Background = B("#0a0a14");
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

            // ══ Header ═══════════════════════════════════════════════════════
            var header = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#06d6a0"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = B("#06d6a0"),
                CornerRadius = new CornerRadius(10),
                Width = 42,
                Height = 42,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "🛵",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "بيانات التوصيل",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"قيمة الأوردر: {_orderTotal:F2} ج",
                FontSize = 11,
                Foreground = B("#06d6a0"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Fields ════════════════════════════════════════════════════════
            var fields = new StackPanel { Margin = new Thickness(20, 14, 20, 0) };

            // ── رقم التليفون ──────────────────────────────────────────────────
            fields.Children.Add(FieldLabel("📞 رقم التليفون *"));
            var phoneGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            phoneGrid.ColumnDefinitions.Add(new ColumnDefinition());
            phoneGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _tbPhone = MakeTB("", "01xxxxxxxxx");
            _tbPhone.Margin = new Thickness(0);
            _tbPhone.TextChanged += (_, _) => OnPhoneChanged();

            var searchBtn = new Border
            {
                Background = B("#1a2640"),
                CornerRadius = new CornerRadius(0, 8, 8, 0),
                Padding = new Thickness(12, 0, 12, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "بحث عن العميل"
            };
            searchBtn.Child = new TextBlock
            {
                Text = "🔍",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            searchBtn.MouseLeftButtonDown += (_, _) => SearchCustomer();

            Grid.SetColumn(_tbPhone, 0);
            Grid.SetColumn(searchBtn, 1);
            phoneGrid.Children.Add(_tbPhone);
            phoneGrid.Children.Add(searchBtn);
            fields.Children.Add(phoneGrid);

            // ── Customer History ──────────────────────────────────────────────
            _historyTxt = new TextBlock
            {
                FontSize = 11,
                Foreground = B("#ffd166"),
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };
            fields.Children.Add(_historyTxt);

            // ── اسم العميل ────────────────────────────────────────────────────
            fields.Children.Add(FieldLabel("👤 اسم العميل *"));
            _tbName = MakeTB("");
            _tbName.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbName);

            // ── العنوان ───────────────────────────────────────────────────────
            fields.Children.Add(FieldLabel("📍 عنوان التوصيل"));
            _tbAddress = new TextBox
            {
                Background = B("#0f1526"),
                Foreground = B("#ffffff"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13,
                Height = 70,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                CaretBrush = B("#06d6a0"),
                Margin = new Thickness(0, 4, 0, 14)
            };
            fields.Children.Add(_tbAddress);

            // ── السائق ────────────────────────────────────────────────────────
            fields.Children.Add(FieldLabel("🛵 السائق"));
            _cbDriver = BuildDarkComboBox();
            _cbDriver.Margin = new Thickness(0, 4, 0, 14);

            var noDriver = new ComboBoxItem
            {
                Content = "— بدون سائق —",
                Tag = 0,
                Foreground = B("#4a6080"),
                FontStyle = FontStyles.Italic
            };
            _cbDriver.Items.Add(noDriver);
            _cbDriver.SelectedItem = noDriver;

            foreach (var d in _drivers)
                _cbDriver.Items.Add(new ComboBoxItem
                {
                    Content = $"🛵  {d.Display}",
                    Tag = d.Id,
                    Foreground = B("#eef0f2")
                });

            if (_drivers.Count == 0)
                fields.Children.Add(new TextBlock
                {
                    Text = "⚠️ لا يوجد سائقين — أضفهم من إدارة السائقين",
                    FontSize = 11,
                    Foreground = B("#ffd166"),
                    Margin = new Thickness(0, -10, 0, 4)
                });
            fields.Children.Add(_cbDriver);

            // ── رسوم التوصيل ─────────────────────────────────────────────────
            fields.Children.Add(FieldLabel("💰 رسوم التوصيل (ج)"));
            _tbFee = MakeTB(_db.GetSetting("DefaultDeliveryFee", "0"));
            _tbFee.Margin = new Thickness(0, 4, 0, 4);
            fields.Children.Add(_tbFee);

            // ── ملاحظات ───────────────────────────────────────────────────────
            fields.Children.Add(FieldLabel("📝 ملاحظات إضافية", top: 8));
            _tbNotes = MakeTB("");
            _tbNotes.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbNotes);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ══ Buttons ═══════════════════════════════════════════════════════
            var btnBar = new Border
            {
                Background = B("#0d1220"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16),
                Margin = new Thickness(0, 14, 0, 0)
            };

            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // إلغاء
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });  // gap
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // تعليق
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });  // gap
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());                               // تأكيد

            // ── إلغاء ──
            var cancelBtn = MakeBtn("✖ إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            // ── تعليق ──
            var holdBtn = MakeBtn("⏸ تعليق", "#1c1a08", B("#ffd166"), Hold);
            holdBtn.BorderBrush = B("#ffd166");
            holdBtn.BorderThickness = new Thickness(1);
            holdBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#ffd166"),
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.35
            };

            // ── تأكيد ──
            var confirmBtn = MakeBtn("✅ تأكيد التوصيل", "#06d6a0", B("#0a0a14"), Confirm);
            confirmBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.5
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(holdBtn, 2);
            Grid.SetColumn(confirmBtn, 4);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(holdBtn);
            btnGrid.Children.Add(confirmBtn);

            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbPhone.Focus();
        }

        // ══ Dark ComboBox ═════════════════════════════════════════════════════
        ComboBox BuildDarkComboBox()
        {
            // ── Item style ───────────────────────────────────────────────────
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
            // Hover
            var hov = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#1a2d50")));
            hov.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#06d6a0")));
            itemStyle.Triggers.Add(hov);
            // Selected
            var sel = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#06d6a0")));
            sel.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#021a12")));
            itemStyle.Triggers.Add(sel);
            // Selected + Hover
            var selHov = new MultiTrigger();
            selHov.Conditions.Add(new Condition(ComboBoxItem.IsSelectedProperty, true));
            selHov.Conditions.Add(new Condition(ComboBoxItem.IsMouseOverProperty, true));
            selHov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#04b888")));
            selHov.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#021a12")));
            itemStyle.Triggers.Add(selHov);

            // ── Arrow ────────────────────────────────────────────────────────
            var arrowPath = new FrameworkElementFactory(typeof(Path));
            arrowPath.SetValue(Path.DataProperty, Geometry.Parse("M 0 0 L 4 4 L 8 0 Z"));
            arrowPath.SetValue(Path.FillProperty, B("#06d6a0"));
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

            // ── Selected item presenter ──────────────────────────────────────
            var selContent = new FrameworkElementFactory(typeof(ContentPresenter));
            selContent.SetBinding(ContentPresenter.ContentProperty,
                new Binding("SelectionBoxItem") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            selContent.SetBinding(ContentPresenter.ContentTemplateProperty,
                new Binding("SelectionBoxItemTemplate") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            selContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            selContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            selContent.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 8, 0));

            var innerGrid = new FrameworkElementFactory(typeof(Grid));
            innerGrid.AppendChild(selContent);
            innerGrid.AppendChild(toggleBtn);

            var outerBorder = new FrameworkElementFactory(typeof(Border));
            outerBorder.SetValue(Border.BackgroundProperty, B("#0f1526"));
            outerBorder.SetValue(Border.BorderBrushProperty, B("#1e3a5f"));
            outerBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            outerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            outerBorder.SetValue(Border.PaddingProperty, new Thickness(0));
            outerBorder.Name = "MainBorder";
            outerBorder.AppendChild(innerGrid);

            // ── Popup ────────────────────────────────────────────────────────
            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            itemsPresenter.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 4));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.BackgroundProperty, B("#0f1526"));
            scrollViewer.SetValue(ScrollViewer.CanContentScrollProperty, true);
            scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            scrollViewer.AppendChild(itemsPresenter);

            var popupBorder = new FrameworkElementFactory(typeof(Border));
            popupBorder.SetValue(Border.BackgroundProperty, B("#0f1526"));
            popupBorder.SetValue(Border.BorderBrushProperty, B("#06d6a0"));
            popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            popupBorder.SetValue(Border.PaddingProperty, new Thickness(4));
            popupBorder.SetValue(FrameworkElement.MinWidthProperty, 200.0);
            popupBorder.SetValue(UIElement.EffectProperty, new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.25
            });
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
            focusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, B("#06d6a0"), "MainBorder"));
            focusTrigger.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "MainBorder"));
            comboTemplate.Triggers.Add(focusTrigger);

            // ── ComboBox ─────────────────────────────────────────────────────
            var cb = new ComboBox
            {
                Background = B("#0f1526"),
                Foreground = B("#eef0f2"),
                BorderBrush = B("#1e3a5f"),
                BorderThickness = new Thickness(1),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Tahoma"),
                Height = 42,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = comboTemplate,
                ItemContainerStyle = itemStyle
            };

            cb.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.15
            };

            cb.PreviewMouseLeftButtonDown += (_, e) =>
            {
                if (!cb.IsDropDownOpen)
                {
                    cb.IsDropDownOpen = true;
                    e.Handled = true;
                }
            };

            cb.SelectionChanged += (_, _) =>
            {
                cb.IsDropDownOpen = false;
            };

            return cb;
        }

        // ══ Logic ═════════════════════════════════════════════════════════════
        void OnPhoneChanged()
        {
            if (_tbPhone.Text.Length >= 7)
                SearchCustomer();
        }

        void SearchCustomer()
        {
            string phone = _tbPhone.Text.Trim();
            if (string.IsNullOrEmpty(phone)) return;

            var cust = _db.GetCustomerByPhone(phone);
            if (cust != null)
            {
                _foundCustomer = cust;
                _tbName.Text = cust.Name;
                _tbAddress.Text = cust.Address;
                _tbNotes.Text = cust.Notes;
                _historyTxt.Text = $"✅ عميل موجود: {cust.Name} — آخر طلب من العنوان: {cust.Address}";
                _historyTxt.Foreground = B("#ffd166");
                _historyTxt.Visibility = Visibility.Visible;
            }
            else
            {
                _foundCustomer = null;
                _historyTxt.Text = "🆕 عميل جديد — سيتم حفظه تلقائياً";
                _historyTxt.Foreground = B("#06d6a0");
                _historyTxt.Visibility = Visibility.Visible;
            }
        }

        void Confirm()
        {
            if (string.IsNullOrWhiteSpace(_tbPhone.Text))
            {
                MessageBox.Show("أدخل رقم التليفون", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbPhone.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(_tbName.Text))
            {
                MessageBox.Show("أدخل اسم العميل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbName.Focus(); return;
            }

            double.TryParse(_tbFee.Text, out double fee);

            var cust = _foundCustomer ?? new Customer();
            cust.Name = _tbName.Text.Trim();
            cust.Phone = _tbPhone.Text.Trim();
            cust.Address = _tbAddress.Text.Trim();
            cust.Notes = _tbNotes.Text.Trim();
            _db.SaveCustomer(cust);

            CustomerName = cust.Name;
            CustomerPhone = cust.Phone;
            DeliveryAddress = cust.Address;
            CustomerId = cust.Id;
            DeliveryFee = fee;

            if (_cbDriver.SelectedItem is ComboBoxItem ci && (int)(ci.Tag ?? 0) > 0)
            {
                DriverId = (int)ci.Tag;
                DriverName = _drivers.FirstOrDefault(d => d.Id == DriverId)?.Name ?? "";
            }

            DialogResult = true;
            Close();
        }

        void Hold()
        {
            if (string.IsNullOrWhiteSpace(_tbPhone.Text))
            {
                MessageBox.Show("أدخل رقم التليفون على الأقل للتعليق", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbPhone.Focus(); return;
            }

            double.TryParse(_tbFee.Text, out double fee);

            CustomerName = _tbName.Text.Trim();
            CustomerPhone = _tbPhone.Text.Trim();
            DeliveryAddress = _tbAddress.Text.Trim();
            DeliveryFee = fee;

            if (_cbDriver.SelectedItem is ComboBoxItem ci && (int)(ci.Tag ?? 0) > 0)
            {
                DriverId = (int)ci.Tag;
                DriverName = _drivers.FirstOrDefault(d => d.Id == DriverId)?.Name ?? "";
            }

            IsHeld = true;
            DialogResult = false;
            Close();
        }

        // ══ Helpers ═══════════════════════════════════════════════════════════
        TextBlock FieldLabel(string t, int top = 0) => new()
        {
            Text = t,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = B("#4a6080"),
            Margin = new Thickness(0, top, 0, 0)
        };

        TextBox MakeTB(string val, string? placeholder = null) => new()
        {
            Text = val,
            Background = B("#0f1526"),
            Foreground = B("#ffffff"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 9, 10, 9),
            FontSize = 13,
            CaretBrush = B("#06d6a0"),
            SelectionBrush = B("#06d6a0")
        };

        Button MakeBtn(string text, string bg, SolidColorBrush fg, Action click)
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
            var tpl = new ControlTemplate(typeof(Button)) { VisualTree = f };
            var btn = new Button
            {
                Content = text,
                Background = B(bg),
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 13, 0, 13),
                FontWeight = FontWeights.Black,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }
    }
}