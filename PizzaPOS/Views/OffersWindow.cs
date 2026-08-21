// Views/OffersWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using PizzaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PizzaPOS.Views
{
    public class OffersWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<Offer> _items = new();
        DataGrid _dg = null!;
        TextBlock _countTxt = null!;

        public OffersWindow()
        {
            Title = "🏷️ العروض والترويج";
            Width = 960; Height = 640;
            MinWidth = 800;
            Background = UiHelper.B("#0f1526");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            BuildUI();
            LoadItems();
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
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border
            {
                Background = UiHelper.B("#E63946"),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 14, 0)
            };
            iconBorder.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#E63946"),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            iconBorder.Child = new TextBlock
            {
                Text = "🏷️",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = "العروض والترويج",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#eef0f2")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "إنشاء وإدارة العروض وإرسالها للزبائن",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });

            var countBadge = new Border
            {
                Background = UiHelper.B("#1a0a0a"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            _countTxt = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#E63946")
            };
            _countTxt.Text = "🏷️  0 عرض";
            countBadge.Child = _countTxt;

            Grid.SetColumn(iconBorder, 0); hGrid.Children.Add(iconBorder);
            Grid.SetColumn(titleStack, 1); hGrid.Children.Add(titleStack);
            Grid.SetColumn(countBadge, 2); hGrid.Children.Add(countBadge);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Toolbar ══
            var toolbar = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 10, 18, 10)
            };
            var tbGrid = new Grid();
            tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tbGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var addBtn = UiHelper.MakeActionButton("➕  عرض جديد", "#06d6a0", UiHelper.B("#0a0a14"));
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => AddOffer();

            var editBtn = UiHelper.MakeActionButton("✏️  تعديل", "#ffd166", UiHelper.B("#0a0a14"));
            editBtn.Click += (_, _) => EditOffer();

            var delBtn = UiHelper.MakeActionButton("🗑️  حذف", "#E63946", UiHelper.B("#ffffff"));
            delBtn.Click += (_, _) => DeleteOffer();

            var sendBtn = UiHelper.MakeActionButton("📱  إرسال للزبائن", "#075e54", UiHelper.B("#ffffff"));
            sendBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#075e54"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            sendBtn.Click += (_, _) => SendToCustomers();

            Grid.SetColumn(addBtn, 0); tbGrid.Children.Add(addBtn);
            Grid.SetColumn(editBtn, 1); tbGrid.Children.Add(editBtn);
            Grid.SetColumn(delBtn, 2); tbGrid.Children.Add(delBtn);
            Grid.SetColumn(sendBtn, 3); tbGrid.Children.Add(sendBtn);
            toolbar.Child = tbGrid;
            Grid.SetRow(toolbar, 1);
            root.Children.Add(toolbar);

            // ══ DataGrid ══
            _dg = BuildGrid();
            _dg.ItemsSource = _items;

            var gridWrapper = new Border
            {
                Margin = new Thickness(18, 14, 18, 0),
                CornerRadius = new CornerRadius(12),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            var scroll = new ScrollViewer
            {
                Content = _dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = UiHelper.B("#0f1526")
            };
            gridWrapper.Child = scroll;
            Grid.SetRow(gridWrapper, 2);
            root.Children.Add(gridWrapper);

            // ══ Footer ══
            var footer = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 10, 18, 10)
            };
            footer.Child = new TextBlock
            {
                Text = "💡 أرسل العروض للزبائن عبر واتساب directly",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            Content = root;
        }

        DataGrid BuildGrid()
        {
            var dg = UiHelper.BuildGrid(
                rowBg: "#0d1525", altBg: "#0a0f1c",
                headerBg: "#0f1526", headerFg: "#E63946",
                accent: "#E63946", hoverBg: "#12192e",
                selBg: "#1a2640", cellFg: "#eef0f2",
                rowHeight: 50, headerHeight: 46);

            var titleCol = new DataGridTextColumn
            {
                Header = "العرض",
                Binding = new Binding("Title"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };
            titleCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2")),
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(titleCol);

            var descCol = UiHelper.Col("الوصف", "Description", 200);
            descCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#8892a4")),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(descCol);

            var discCol = UiHelper.Col("الخصم", "DiscountDisplay", 80);
            discCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#E63946")),
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Black),
                    new Setter(TextBlock.FontSizeProperty, 14.0),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(discCol);

            var promoCol = UiHelper.Col("كود الخصم", "PromoCode", 100);
            promoCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#ffd166")),
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(promoCol);

            var statusCol = new DataGridTemplateColumn
            {
                Header = "الحالة",
                Width = 80
            };
            var statusTpl = new DataTemplate();
            var statusBorder = new FrameworkElementFactory(typeof(Border));
            statusBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            statusBorder.SetValue(Border.PaddingProperty, new Thickness(8, 2, 8, 2));
            statusBorder.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            var statusTxt = new FrameworkElementFactory(typeof(TextBlock));
            statusTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            statusTxt.SetValue(TextBlock.FontSizeProperty, 11.0);
            statusTxt.SetBinding(TextBlock.TextProperty, new Binding("StatusDisplay"));
            statusBorder.AppendChild(statusTxt);
            statusBorder.SetBinding(Border.BackgroundProperty, new Binding("IsActive")
            {
                Converter = new OfferStatusBgConverter()
            });
            statusTxt.SetBinding(TextBlock.ForegroundProperty, new Binding("IsActive")
            {
                Converter = new OfferStatusFgConverter()
            });
            statusTpl.VisualTree = statusBorder;
            statusCol.CellTemplate = statusTpl;
            dg.Columns.Add(statusCol);

            var dateCol = UiHelper.Col("تاريخ الإنشاء", "CreatedAt", 120);
            dateCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#4a6080")),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(dateCol);

            return dg;
        }

        void LoadItems()
        {
            _items.Clear();
            foreach (var o in _db.GetOffers()) _items.Add(o);
            _countTxt.Text = $"🏷️  {_items.Count} عرض";
        }

        Offer? Selected() => _dg.SelectedItem as Offer;

        void AddOffer()
        {
            var dlg = new OfferEditDialog(null) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _db.SaveOffer(dlg.Result!);
                LoadItems();
            }
        }

        void EditOffer()
        {
            var sel = Selected();
            if (sel == null) { MessageBox.Show("اختر عرض أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var dlg = new OfferEditDialog(sel) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _db.SaveOffer(dlg.Result!);
                LoadItems();
            }
        }

        void DeleteOffer()
        {
            var sel = Selected();
            if (sel == null) { MessageBox.Show("اختر عرض أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show($"هل أنت متأكد من حذف \"{sel.Title}\"?", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.DeleteOffer(sel.Id);
                LoadItems();
            }
        }

        void SendToCustomers()
        {
            var sel = Selected();
            if (sel == null) { MessageBox.Show("اختر عرض أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var customers = _db.GetCustomers();
            if (customers.Count == 0)
            {
                MessageBox.Show("مفيش زبائن مسجلين!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SendOfferDialog(sel, customers) { Owner = this };
            dlg.ShowDialog();
        }
    }

    // ══════════════════════════════════════════════════
    //  OfferEditDialog
    // ══════════════════════════════════════════════════
    public class OfferEditDialog : Window
    {
        public Offer? Result { get; private set; }
        readonly Offer? _editing;
        TextBox _tbTitle = null!;
        TextBox _tbDesc = null!;
        TextBox _tbDiscount = null!;
        TextBox _tbPromo = null!;
        CheckBox _chkActive = null!;

        public OfferEditDialog(Offer? editing)
        {
            _editing = editing;
            Title = editing == null ? "➕ عرض جديد" : "✏️ تعديل العرض";
            Width = 480;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#0f1526");
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

            // Header
            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1a0a0a"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = _editing == null ? "➕" : "✏️",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = _editing == null ? "إضافة عرض جديد" : "تعديل العرض",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#E63946")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = _editing == null ? "أدخل بيانات العرض" : $"تعديل: {_editing.Title}",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Fields
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            fields.Children.Add(UiHelper.FieldLabel("عنوان العرض *"));
            _tbTitle = UiHelper.MakeTB(_editing?.Title ?? "", "#E63946");
            _tbTitle.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbTitle);

            fields.Children.Add(UiHelper.FieldLabel("وصف العرض"));
            _tbDesc = UiHelper.MakeTB(_editing?.Description ?? "", "#E63946");
            _tbDesc.Margin = new Thickness(0, 4, 0, 14);
            _tbDesc.Height = 60;
            _tbDesc.TextWrapping = TextWrapping.Wrap;
            _tbDesc.AcceptsReturn = true;
            fields.Children.Add(_tbDesc);

            var numRow = new Grid();
            numRow.ColumnDefinitions.Add(new ColumnDefinition());
            numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            numRow.ColumnDefinitions.Add(new ColumnDefinition());

            var discPanel = new StackPanel();
            discPanel.Children.Add(UiHelper.FieldLabel("نسبة الخصم (%)"));
            _tbDiscount = UiHelper.MakeTB(_editing?.DiscountPercent.ToString("F0") ?? "0", "#E63946");
            _tbDiscount.Margin = new Thickness(0, 4, 0, 14);
            discPanel.Children.Add(_tbDiscount);
            Grid.SetColumn(discPanel, 0);
            numRow.Children.Add(discPanel);

            var promoPanel = new StackPanel();
            promoPanel.Children.Add(UiHelper.FieldLabel("كود الخصم"));
            _tbPromo = UiHelper.MakeTB(_editing?.PromoCode ?? "", "#E63946");
            _tbPromo.Margin = new Thickness(0, 4, 0, 14);
            promoPanel.Children.Add(_tbPromo);
            Grid.SetColumn(promoPanel, 2);
            numRow.Children.Add(promoPanel);

            fields.Children.Add(numRow);

            _chkActive = new CheckBox
            {
                Content = "العرض نشط",
                IsChecked = _editing?.IsActive ?? true,
                Foreground = UiHelper.B("#06d6a0"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 0)
            };
            fields.Children.Add(_chkActive);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // Buttons
            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

            var saveBtn = UiHelper.MakeBtn(
                _editing == null ? "➕ إضافة" : "💾 حفظ",
                "#E63946", UiHelper.B("#ffffff"), Save);

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbTitle.Focus();
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbTitle.Text))
            {
                MessageBox.Show("أدخل عنوان العرض", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            double disc = 0;
            if (!string.IsNullOrWhiteSpace(_tbDiscount.Text))
                double.TryParse(_tbDiscount.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out disc);

            Result = new Offer
            {
                Id = _editing?.Id ?? 0,
                Title = _tbTitle.Text.Trim(),
                Description = _tbDesc.Text.Trim(),
                DiscountPercent = disc,
                PromoCode = _tbPromo.Text.Trim(),
                IsActive = _chkActive.IsChecked == true
            };
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  Converters for Offer status
    // ══════════════════════════════════════════════════
    public class OfferStatusBgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
            => v is true ? UiHelper.B("#082010") : UiHelper.B("#1a080a");
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    public class OfferStatusFgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
            => v is true ? UiHelper.B("#06d6a0") : UiHelper.B("#E63946");
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }
}