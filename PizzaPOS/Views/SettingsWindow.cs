using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        // FIX: أضفناهم كـ fields عشان Save() تشوفهم
        TextBox _tbWidth = null!;
        TextBox _tbDrawerFor = null!;

        public SettingsWindow()
        {
            Title = "⚙️ إعدادات النظام";
            Width = 460;
            Height = 620;
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
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF6B35"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.55
            };
            hIcon.Child = new TextBlock
            {
                Text = "⚙️",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إعدادات النظام",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "تخصيص المحل والضرائب والطابعة",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            outer.Children.Add(header);

            // ── Fields ──
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var sp = new StackPanel { Margin = new Thickness(20, 10, 20, 0) };

            // ── قسم: بيانات المحل ──
            sp.Children.Add(SectionHeader("🏪 بيانات المحل"));

            sp.Children.Add(Lbl("اسم المحل"));
            _tbShopName = UiHelper.MakeTB(_db.GetSetting("ShopName", "Pizza POS"));
            sp.Children.Add(_tbShopName);

            sp.Children.Add(Lbl("العنوان", top: 10));
            _tbAddress = UiHelper.MakeTB(_db.GetSetting("Address", ""));
            sp.Children.Add(_tbAddress);

            sp.Children.Add(Lbl("📞 رقم التليفون 1", top: 10));
            _tbPhone = UiHelper.MakeTB(_db.GetSetting("Phone", ""));
            sp.Children.Add(_tbPhone);

            sp.Children.Add(Lbl("📞 رقم التليفون 2", top: 6));
            _tbPhone2 = UiHelper.MakeTB(_db.GetSetting("Phone2", ""));
            sp.Children.Add(_tbPhone2);

            sp.Children.Add(Lbl("📞 رقم التليفون 3", top: 6));
            _tbPhone3 = UiHelper.MakeTB(_db.GetSetting("Phone3", ""));
            sp.Children.Add(_tbPhone3);

            // ── قسم: الضرائب والخصومات ──
            sp.Children.Add(SectionHeader("💰 الضرائب والخصومات"));

            sp.Children.Add(Lbl("نسبة الضريبة % (مثال: 14)"));
            _tbTax = UiHelper.MakeTB(
                (double.TryParse(_db.GetSetting("TaxRate", "0.14"), out var t)
                    ? (t < 1 ? t * 100 : t) : 14).ToString("F0"));
            sp.Children.Add(_tbTax);

            sp.Children.Add(Lbl("🛎 رسوم الخدمة % (0 = تعطيل)", top: 10));
            _tbService = UiHelper.MakeTB(
                (double.TryParse(_db.GetSetting("ServiceRate", "0"), out var sr)
                    ? (sr < 1 ? sr * 100 : sr) : 0).ToString("F0"));
            sp.Children.Add(_tbService);

            sp.Children.Add(Lbl("🏷️ خصم افتراضي % (0 = تعطيل)", top: 10));
            var discGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            discGrid.ColumnDefinitions.Add(new ColumnDefinition());
            discGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _tbDiscount = UiHelper.MakeTB(
                (double.TryParse(_db.GetSetting("DefaultDiscount", "0"), out var dd)
                    ? dd : 0).ToString("F0"));
            var discTypeLbl = new Border
            {
                Background = UiHelper.B("#1a2640"),
                CornerRadius = new CornerRadius(0, 6, 6, 0),
                Padding = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            discTypeLbl.Child = new TextBlock
            {
                Text = "%",
                Foreground = UiHelper.B("#ffd166"),
                FontSize = 14,
                FontWeight = FontWeights.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(discTypeLbl, 1);
            discGrid.Children.Add(_tbDiscount);
            discGrid.Children.Add(discTypeLbl);
            sp.Children.Add(discGrid);
            sp.Children.Add(new TextBlock
            {
                Text = "💡 سيُطبَّق تلقائياً على كل أوردر جديد",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });

            sp.Children.Add(Lbl("🛵 رسوم التوصيل الافتراضية (ج)", top: 10));
            _tbDeliveryFee = UiHelper.MakeTB(_db.GetSetting("DefaultDeliveryFee", "0"));
            sp.Children.Add(_tbDeliveryFee);

            // ── قسم: الفاتورة ──
            sp.Children.Add(SectionHeader("🧾 الفاتورة"));

            sp.Children.Add(Lbl("نص أسفل الفاتورة"));
            _tbFooter = UiHelper.MakeTB(_db.GetSetting("ReceiptFooter", "شكراً لزيارتكم"));
            sp.Children.Add(_tbFooter);

            // ── قسم: الطابعة ──
            sp.Children.Add(SectionHeader("🖨️ الطابعة"));

            sp.Children.Add(Lbl("اسم الطابعة (اتركه فاضي للكشف التلقائي)"));
            _tbPrinter = UiHelper.MakeTB(_db.GetSetting("PrinterName", ""));
            sp.Children.Add(_tbPrinter);

            var printersSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            printersSp.Children.Add(new TextBlock
            {
                Text = "المتاحة: ",
                Foreground = UiHelper.B("#4a6080"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
            foreach (var p in EpsonService.GetAllPrinters())
            {
                var tag = new Border
                {
                    Background = UiHelper.B("#1a2640"),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 5, 0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                tag.Child = new TextBlock
                {
                    Text = p,
                    FontSize = 10,
                    Foreground = UiHelper.B("#ffd166")
                };
                var printer = p;
                tag.MouseLeftButtonDown += (_, _) => _tbPrinter.Text = printer;
                printersSp.Children.Add(tag);
            }
            sp.Children.Add(printersSp);

            sp.Children.Add(Lbl("🔌 المنفذ (USB أو COM1..COM9)", top: 10));
            _tbPort = UiHelper.MakeTB(_db.GetSetting("EpsonPort", "USB"));
            sp.Children.Add(_tbPort);

            var ports = EpsonService.GetComPorts();
            if (ports.Length > 0)
                sp.Children.Add(new TextBlock
                {
                    Text = $"منافذ COM المتاحة: {string.Join("  ", ports)}",
                    FontSize = 10,
                    Foreground = UiHelper.B("#4a6080"),
                    Margin = new Thickness(0, 4, 0, 0)
                });

            sp.Children.Add(Lbl("🗄️ رقم PIN دراج الكاش (2 أو 5)", top: 10));
            _tbDrawerPin = UiHelper.MakeTB(_db.GetSetting("DrawerPin", "2"));
            sp.Children.Add(_tbDrawerPin);

            sp.Children.Add(Lbl("عرض الطابعة (32 للـ 58mm أو 48 للـ 80mm)", top: 12));
            // FIX: بدل local variable → field حتى Save() تشوفه
            _tbWidth = UiHelper.MakeTB(_db.GetSetting("PrinterWidth", "32"));
            sp.Children.Add(_tbWidth);

            sp.Children.Add(Lbl("افتح الدرج مع طرق الدفع (مفصولة بفاصلة)", top: 12));
            // FIX: بدل local variable → field حتى Save() تشوفه
            _tbDrawerFor = UiHelper.MakeTB(_db.GetSetting("OpenDrawerFor", "كاش"));
            sp.Children.Add(new TextBlock
            {
                Text = "مثال: كاش,فيزا/ماستر",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080")
            });
            sp.Children.Add(_tbDrawerFor);

            // FIX: حذفنا SetSetting من هنا — كان بيحفظ فور ما الشاشة تفتح!
            // الحفظ بقى كله في Save() بس

            var testBtn = UiHelper.MakeBtn("🖨️ طباعة فاتورة تجريبية", "#1e3a5f", UiHelper.B("#7ab8f5"), () =>
            {
                _db.SetSetting("PrinterName", _tbPrinter.Text.Trim());
                _db.SetSetting("EpsonPort", _tbPort.Text.Trim());
                _db.SetSetting("DrawerPin", _tbDrawerPin.Text.Trim());
                new EpsonService().PrintTest(_db);
            });
            testBtn.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(testBtn);

            sp.Children.Add(new TextBlock
            {
                Text = "⚠️ التغييرات تطبق على الأوردرات الجديدة فقط",
                FontSize = 11,
                Foreground = UiHelper.B("#ffd166"),
                Margin = new Thickness(0, 14, 0, 16),
                TextWrapping = TextWrapping.Wrap
            });

            scroll.Content = sp;
            Grid.SetRow(scroll, 1);
            outer.Children.Add(scroll);

            // ── Bottom Buttons ──
            var bar = new Border
            {
                Background = UiHelper.B("#090e1a"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 12, 16, 12)
            };
            var btnSp = new StackPanel { Orientation = Orientation.Horizontal };
            btnSp.Children.Add(UiHelper.MakeBtn("💾  حفظ الإعدادات", "#06d6a0", UiHelper.B("#0a1a12"), Save));
            btnSp.Children.Add(UiHelper.MakeBtn("إلغاء", "#1e2d4a", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }));
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

            // FIX: أضفنا validation للـ PrinterWidth
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
            // FIX: دول كانوا بيتحفظوا في BuildUI — نقلناهم هنا
            _db.SetSetting("PrinterWidth", _tbWidth.Text.Trim());
            _db.SetSetting("OpenDrawerFor", _tbDrawerFor.Text.Trim());

            MessageBox.Show("✅ تم حفظ الإعدادات بنجاح", "تم",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        // ── Helpers ──────────────────────────────────
        Border SectionHeader(string title)
        {
            var b = new Border
            {
                Background = UiHelper.B("#0c1530"),
                BorderBrush = UiHelper.B("#FF6B35"),
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new CornerRadius(0, 6, 6, 0),
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 16, 0, 10)
            };
            b.Child = new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            };
            return b;
        }

        TextBlock Lbl(string t, int top = 0) => new()
        {
            Text = t,
            Foreground = UiHelper.B("#8892a4"),
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            Margin = new Thickness(0, top, 0, 4)
        };

    }
}