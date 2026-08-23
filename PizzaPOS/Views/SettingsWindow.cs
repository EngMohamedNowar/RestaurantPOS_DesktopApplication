using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PizzaPOS.Views
{
    public class SettingsWindow : Window
    {
        readonly AppDbContext _db = new();

        TextBox _tbShopName = null!;
        TextBox _tbAddress = null!;
        TextBox _tbPhone = null!;
        TextBox _tbPhone2 = null!;
        TextBox _tbPhone3 = null!;
        TextBox _tbTax = null!;
        TextBox _tbService = null!;
        TextBox _tbDiscount = null!;
        TextBox _tbDeliveryFee = null!;
        TextBox _tbFooter = null!;
        TextBox _tbPrinter = null!;
        TextBox _tbPort = null!;
        TextBox _tbDrawerPin = null!;
        TextBox _tbWidth = null!;
        TextBox _tbDrawerFor = null!;

        public SettingsWindow()
        {
            Title = "إعدادات النظام";
            Width = 520;
            Height = 700;
            Background = UiHelper.B("#070b14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;
            BuildUI();
        }

        void BuildUI()
        {
            var outer = new Grid();
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outer.RowDefinitions.Add(new RowDefinition());
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ══ Header ══════════════════════════════════════════════════════
            var header = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 18, 24, 18)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#FF6B35"),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 16, 0)
            };
            hIcon.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            hIcon.Child = new TextBlock
            {
                Text = "⚙️",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إعدادات النظام",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "تخصيص المحل والضرائب والطابعة",
                FontSize = 12,
                Foreground = UiHelper.B("#5a6a80"),
                Margin = new Thickness(0, 4, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            outer.Children.Add(header);

            // ══ Fields (Scrollable) ═════════════════════════════════════════
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var sp = new StackPanel { Margin = new Thickness(24, 16, 24, 0) };

            // ── Section: بيانات المحل ──
            sp.Children.Add(SectionHeader("بيانات المحل", "🏪"));

            sp.Children.Add(Lbl("اسم المحل"));
            _tbShopName = UiHelper.MakeTB(_db.GetSetting("ShopName", "Pizza POS"), "#FF6B35");
            _tbShopName.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbShopName);

            sp.Children.Add(Lbl("العنوان"));
            _tbAddress = UiHelper.MakeTB(_db.GetSetting("Address", ""), "#FF6B35");
            _tbAddress.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbAddress);

            sp.Children.Add(Lbl("رقم التليفون 1"));
            _tbPhone = UiHelper.MakeTB(_db.GetSetting("Phone", ""), "#FF6B35");
            _tbPhone.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbPhone);

            sp.Children.Add(Lbl("رقم التليفون 2 (اختياري)"));
            _tbPhone2 = UiHelper.MakeTB(_db.GetSetting("Phone2", ""), "#FF6B35");
            _tbPhone2.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbPhone2);

            sp.Children.Add(Lbl("رقم التليفون 3 (اختياري)"));
            _tbPhone3 = UiHelper.MakeTB(_db.GetSetting("Phone3", ""), "#FF6B35");
            _tbPhone3.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbPhone3);

            // ── Section: الضرائب والخصومات ──
            sp.Children.Add(SectionHeader("الضرائب والخصومات", "💰"));

            var numRow = new Grid();
            numRow.ColumnDefinitions.Add(new ColumnDefinition());
            numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            numRow.ColumnDefinitions.Add(new ColumnDefinition());

            var taxPanel = new StackPanel();
            taxPanel.Children.Add(Lbl("الضريبة %"));
            _tbTax = UiHelper.MakeTB(
                (double.TryParse(_db.GetSetting("TaxRate", "0.14"), out var t)
                    ? (t < 1 ? t * 100 : t) : 14).ToString("F0"), "#FF6B35");
            _tbTax.Margin = new Thickness(0, 4, 0, 14);
            taxPanel.Children.Add(_tbTax);
            Grid.SetColumn(taxPanel, 0);
            numRow.Children.Add(taxPanel);

            var srvPanel = new StackPanel();
            srvPanel.Children.Add(Lbl("رسوم الخدمة %"));
            _tbService = UiHelper.MakeTB(
                (double.TryParse(_db.GetSetting("ServiceRate", "0"), out var sr)
                    ? (sr < 1 ? sr * 100 : sr) : 0).ToString("F0"), "#FF6B35");
            _tbService.Margin = new Thickness(0, 4, 0, 14);
            srvPanel.Children.Add(_tbService);
            Grid.SetColumn(srvPanel, 2);
            numRow.Children.Add(srvPanel);

            sp.Children.Add(numRow);

            sp.Children.Add(Lbl("خصم افتراضي % (0 = تعطيل)"));
            _tbDiscount = UiHelper.MakeTB(
                (double.TryParse(_db.GetSetting("DefaultDiscount", "0"), out var dd)
                    ? dd : 0).ToString("F0"), "#FF6B35");
            _tbDiscount.Margin = new Thickness(0, 4, 0, 6);
            sp.Children.Add(_tbDiscount);
            sp.Children.Add(new TextBlock
            {
                Text = "💡 سيُطبَّق تلقائياً على كل أوردر جديد",
                FontSize = 10,
                Foreground = UiHelper.B("#3a4a60"),
                Margin = new Thickness(0, 0, 0, 14)
            });

            sp.Children.Add(Lbl("رسوم التوصيل الافتراضية (ج)"));
            _tbDeliveryFee = UiHelper.MakeTB(_db.GetSetting("DefaultDeliveryFee", "0"), "#FF6B35");
            _tbDeliveryFee.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbDeliveryFee);

            // ── Section: الفاتورة ──
            sp.Children.Add(SectionHeader("الفاتورة", "🧾"));

            sp.Children.Add(Lbl("نص أسفل الفاتورة"));
            _tbFooter = UiHelper.MakeTB(_db.GetSetting("ReceiptFooter", "شكراً لزيارتكم"), "#FF6B35");
            _tbFooter.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbFooter);

            // ── Section: الطابعة ──
            sp.Children.Add(SectionHeader("الطابعة", "🖨️"));

            sp.Children.Add(Lbl("اسم الطابعة (اتركه فاضي للكشف التلقائي)"));
            _tbPrinter = UiHelper.MakeTB(_db.GetSetting("PrinterName", ""), "#FF6B35");
            _tbPrinter.Margin = new Thickness(0, 4, 0, 10);
            sp.Children.Add(_tbPrinter);

            // Printer tags
            var printersWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
            foreach (var p in EpsonService.GetAllPrinters())
            {
                var tag = new Border
                {
                    Background = UiHelper.B("#1a2640"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                tag.Child = new TextBlock
                {
                    Text = p,
                    FontSize = 11,
                    Foreground = UiHelper.B("#ffd166")
                };
                var printer = p;
                tag.MouseLeftButtonDown += (_, _) => _tbPrinter.Text = printer;
                tag.MouseEnter += (_, _) => tag.Background = UiHelper.B("#263a55");
                tag.MouseLeave += (_, _) => tag.Background = UiHelper.B("#1a2640");
                printersWrap.Children.Add(tag);
            }
            sp.Children.Add(printersWrap);

            sp.Children.Add(Lbl("المنفذ (USB أو COM1..COM9)"));
            _tbPort = UiHelper.MakeTB(_db.GetSetting("EpsonPort", "USB"), "#FF6B35");
            _tbPort.Margin = new Thickness(0, 4, 0, 10);
            sp.Children.Add(_tbPort);

            var ports = EpsonService.GetComPorts();
            if (ports.Length > 0)
                sp.Children.Add(new TextBlock
                {
                    Text = $"منافذ COM المتاحة: {string.Join("  ", ports)}",
                    FontSize = 10,
                    Foreground = UiHelper.B("#3a4a60"),
                    Margin = new Thickness(0, 0, 0, 14)
                });

            sp.Children.Add(Lbl("رقم PIN دراج الكاش (2 أو 5)"));
            _tbDrawerPin = UiHelper.MakeTB(_db.GetSetting("DrawerPin", "2"), "#FF6B35");
            _tbDrawerPin.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbDrawerPin);

            sp.Children.Add(Lbl("عرض الطابعة (32 للـ 58mm أو 48 للـ 80mm)"));
            _tbWidth = UiHelper.MakeTB(_db.GetSetting("PrinterWidth", "32"), "#FF6B35");
            _tbWidth.Margin = new Thickness(0, 4, 0, 14);
            sp.Children.Add(_tbWidth);

            sp.Children.Add(Lbl("افتح الدرج مع طرق الدفع (مفصولة بفاصلة)"));
            _tbDrawerFor = UiHelper.MakeTB(_db.GetSetting("OpenDrawerFor", "كاش"), "#FF6B35");
            _tbDrawerFor.Margin = new Thickness(0, 4, 0, 6);
            sp.Children.Add(_tbDrawerFor);
            sp.Children.Add(new TextBlock
            {
                Text = "مثال: كاش,فيزا/ماستر",
                FontSize = 10,
                Foreground = UiHelper.B("#3a4a60"),
                Margin = new Thickness(0, 0, 0, 14)
            });

            var testBtn = UiHelper.MakeBtn("طباعة فاتورة تجريبية", "#1a3a5f", UiHelper.B("#7ab8f5"), () =>
            {
                _db.SetSetting("PrinterName", _tbPrinter.Text.Trim());
                _db.SetSetting("EpsonPort", _tbPort.Text.Trim());
                _db.SetSetting("DrawerPin", _tbDrawerPin.Text.Trim());
                new EpsonService().PrintTest(_db);
            }, 10, 13);
            testBtn.MinWidth = 180;
            testBtn.Margin = new Thickness(0, 8, 0, 16);
            sp.Children.Add(testBtn);

            sp.Children.Add(new TextBlock
            {
                Text = "⚠️ التغييرات تطبق على الأوردرات الجديدة فقط",
                FontSize = 11,
                Foreground = UiHelper.B("#ffd166"),
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            });

            scroll.Content = sp;
            Grid.SetRow(scroll, 1);
            outer.Children.Add(scroll);

            // ══ Footer ══════════════════════════════════════════════════════
            var bar = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 14, 24, 14)
            };
            var btnSp = new StackPanel { Orientation = Orientation.Horizontal };

            var saveBtn = UiHelper.MakeBtn("حفظ الإعدادات", "#06d6a0", UiHelper.B("#0a0a14"), Save, 10, 14);
            saveBtn.MinWidth = 160;
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.3
            };

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#2a2a3c", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, 10, 14);

            btnSp.Children.Add(saveBtn);
            btnSp.Children.Add(new Spacer { Width = 12 });
            btnSp.Children.Add(cancelBtn);
            bar.Child = btnSp;
            Grid.SetRow(bar, 2);
            outer.Children.Add(bar);

            Content = outer;
        }

        void Save()
        {
            if (!double.TryParse(_tbTax.Text, out double taxPct) || taxPct < 0 || taxPct > 100)
            {
                MessageBox.Show("نسبة الضريبة يجب أن تكون بين 0 و 100",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (!double.TryParse(_tbService.Text, out double srvPct) || srvPct < 0 || srvPct > 100)
            {
                MessageBox.Show("رسوم الخدمة يجب أن تكون بين 0 و 100",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (!double.TryParse(_tbDiscount.Text, out double discPct) || discPct < 0 || discPct > 100)
            {
                MessageBox.Show("نسبة الخصم يجب أن تكون بين 0 و 100",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(_tbShopName.Text))
            {
                MessageBox.Show("أدخل اسم المحل",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (!int.TryParse(_tbWidth.Text, out int pw) || pw < 24 || pw > 80)
            {
                MessageBox.Show("عرض الطابعة يجب أن يكون بين 24 و 80",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            _db.SetSetting("ShopName", _tbShopName.Text.Trim());
            _db.SetSetting("Address", _tbAddress.Text.Trim());
            _db.SetSetting("Phone", _tbPhone.Text.Trim());
            _db.SetSetting("Phone2", _tbPhone2.Text.Trim());
            _db.SetSetting("Phone3", _tbPhone3.Text.Trim());
            _db.SetSetting("TaxRate", (taxPct / 100).ToString("F4"));
            _db.SetSetting("ServiceRate", (srvPct / 100).ToString("F4"));
            _db.SetSetting("DefaultDiscount", discPct.ToString("F2"));
            _db.SetSetting("DefaultDeliveryFee", _tbDeliveryFee.Text.Trim());
            _db.SetSetting("ReceiptFooter", _tbFooter.Text.Trim());
            _db.SetSetting("PrinterName", _tbPrinter.Text.Trim());
            _db.SetSetting("EpsonPort", _tbPort.Text.Trim());
            _db.SetSetting("DrawerPin", _tbDrawerPin.Text.Trim());
            _db.SetSetting("PrinterWidth", _tbWidth.Text.Trim());
            _db.SetSetting("OpenDrawerFor", _tbDrawerFor.Text.Trim());

            MessageBox.Show("تم حفظ الإعدادات بنجاح", "تم",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        // ── Helpers ──────────────────────────────────
        Border SectionHeader(string title, string icon)
        {
            var b = new Border
            {
                Background = UiHelper.B("#0d1a2a"),
                BorderBrush = UiHelper.B("#FF6B35"),
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new CornerRadius(0, 8, 8, 0),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 18, 0, 10)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#FF6B35"),
                VerticalAlignment = VerticalAlignment.Center
            });
            b.Child = sp;
            return b;
        }

        TextBlock Lbl(string t) => new()
        {
            Text = t,
            Foreground = UiHelper.B("#5a6a80"),
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 4)
        };
    }
}
