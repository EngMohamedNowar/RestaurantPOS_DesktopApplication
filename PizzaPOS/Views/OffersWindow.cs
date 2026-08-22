// Views/OffersWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using PizzaPOS.Services;
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
    public class OffersWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<Offer> _items = new();
        DataGrid _dg = null!;
        TextBlock _countTxt = null!;
        TextBlock _activeCountTxt = null!;
        TextBlock _totalDiscountTxt = null!;

        public OffersWindow()
        {
            Title = "العروض والترويج";
            Width = 1000; Height = 660;
            MinWidth = 800;
            Background = UiHelper.B("#070b14");
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

            // ══ Header ══════════════════════════════════════════════════════
            var header = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 18, 24, 18)
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
                Margin = new Thickness(0, 0, 16, 0)
            };
            iconBorder.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#E63946"),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.4
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
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#eef0f2")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "إنشاء وإدارة العروض وإرسالها للزبائن عبر واتساب",
                FontSize = 12,
                Foreground = UiHelper.B("#5a6a80"),
                Margin = new Thickness(0, 4, 0, 0)
            });

            // Stats cards
            var statsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var activeCard = new Border
            {
                Background = UiHelper.B("#082010"),
                BorderBrush = UiHelper.B("#06d6a0"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var activeSp = new StackPanel { Orientation = Orientation.Horizontal };
            activeSp.Children.Add(new TextBlock
            {
                Text = "نشط: ",
                FontSize = 12,
                Foreground = UiHelper.B("#06d6a0"),
                VerticalAlignment = VerticalAlignment.Center
            });
            _activeCountTxt = new TextBlock
            {
                Text = "0",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#06d6a0"),
                VerticalAlignment = VerticalAlignment.Center
            };
            activeSp.Children.Add(_activeCountTxt);
            activeCard.Child = activeSp;
            statsPanel.Children.Add(activeCard);

            var discountCard = new Border
            {
                Background = UiHelper.B("#1a0a0a"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 8, 14, 8)
            };
            var discountSp = new StackPanel { Orientation = Orientation.Horizontal };
            discountSp.Children.Add(new TextBlock
            {
                Text = "إجمالي الخصومات: ",
                FontSize = 12,
                Foreground = UiHelper.B("#E63946"),
                VerticalAlignment = VerticalAlignment.Center
            });
            _totalDiscountTxt = new TextBlock
            {
                Text = "0%",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#E63946"),
                VerticalAlignment = VerticalAlignment.Center
            };
            discountSp.Children.Add(_totalDiscountTxt);
            discountCard.Child = discountSp;
            statsPanel.Children.Add(discountCard);

            Grid.SetColumn(iconBorder, 0); hGrid.Children.Add(iconBorder);
            Grid.SetColumn(titleStack, 1); hGrid.Children.Add(titleStack);
            Grid.SetColumn(statsPanel, 2); hGrid.Children.Add(statsPanel);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Toolbar ═════════════════════════════════════════════════════
            var toolbar = new Border
            {
                Background = UiHelper.B("#0a0f1c"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 12, 24, 12)
            };
            var tbSp = new StackPanel { Orientation = Orientation.Horizontal };

            var addBtn = UiHelper.MakeBtn("عرض جديد", "#06d6a0", UiHelper.B("#0a0a14"), AddOffer, 10, 13);
            addBtn.MinWidth = 120;
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.3
            };

            var editBtn = UiHelper.MakeBtn("تعديل", "#ffd166", UiHelper.B("#0a0a14"), EditOffer, 10, 13);
            editBtn.MinWidth = 90;

            var delBtn = UiHelper.MakeBtn("حذف", "#E63946", Brushes.White, DeleteOffer, 10, 13);
            delBtn.MinWidth = 90;

            var sendBtn = UiHelper.MakeBtn("إرسال واتساب", "#075e54", Brushes.White, SendToCustomers, 10, 13);
            sendBtn.MinWidth = 140;
            sendBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#075e54"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.3
            };

            tbSp.Children.Add(addBtn);
            tbSp.Children.Add(new Spacer());
            tbSp.Children.Add(delBtn);
            tbSp.Children.Add(new Spacer());
            tbSp.Children.Add(editBtn);
            tbSp.Children.Add(new Spacer());
            tbSp.Children.Add(sendBtn);
            toolbar.Child = tbSp;
            Grid.SetRow(toolbar, 1);
            root.Children.Add(toolbar);

            // ══ DataGrid ═══════════════════════════════════════════════════
            _dg = BuildGrid();
            _dg.ItemsSource = _items;

            var gridWrapper = new Border
            {
                Margin = new Thickness(24, 14, 24, 0),
                CornerRadius = new CornerRadius(12),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            var scroll = new ScrollViewer
            {
                Content = _dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = UiHelper.B("#0d1525")
            };
            gridWrapper.Child = scroll;
            Grid.SetRow(gridWrapper, 2);
            root.Children.Add(gridWrapper);

            // ══ Footer ══════════════════════════════════════════════════════
            var footer = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 12, 24, 12)
            };
            var fSp = new StackPanel { Orientation = Orientation.Horizontal };

            _countTxt = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#5a6a80"),
                VerticalAlignment = VerticalAlignment.Center
            };
            _countTxt.Text = "0 عرض";

            var tipTxt = new TextBlock
            {
                Text = "💡 حدد عرض واضغط 'إرسال واتساب' لإرساله للزبائن",
                FontSize = 11,
                Foreground = UiHelper.B("#3a4a60"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            fSp.Children.Add(_countTxt);
            fSp.Children.Add(new Spacer());
            fSp.Children.Add(tipTxt);
            footer.Child = fSp;
            Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            Content = root;
        }

        DataGrid BuildGrid()
        {
            var dg = UiHelper.BuildGrid(
                rowBg: "#0d1525", altBg: "#0a0f1c",
                headerBg: "#0d1525", headerFg: "#06d6a0",
                accent: "#E63946", hoverBg: "#12192e",
                selBg: "#1a2640", cellFg: "#eef0f2",
                rowHeight: 48, headerHeight: 42);

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
                    new Setter(TextBlock.FontSizeProperty, 13.0),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(titleCol);

            var descCol = UiHelper.Col("الوصف", "Description", 180);
            descCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#5a6a80")),
                    new Setter(TextBlock.FontSizeProperty, 12.0),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(descCol);

            var discCol = UiHelper.Col("الخصم", "DiscountDisplay", 90);
            discCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#E63946")),
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
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
                    new Setter(TextBlock.FontSizeProperty, 12.0),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
            dg.Columns.Add(promoCol);

            var statusCol = new DataGridTemplateColumn
            {
                Header = "الحالة",
                Width = 90
            };
            var statusTpl = new DataTemplate();
            var statusBorder = new FrameworkElementFactory(typeof(Border));
            statusBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            statusBorder.SetValue(Border.PaddingProperty, new Thickness(10, 4, 10, 4));
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

            var dateCol = UiHelper.Col("تاريخ الإنشاء", "CreatedAt", 130);
            dateCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.ForegroundProperty, UiHelper.B("#3a4a60")),
                    new Setter(TextBlock.FontSizeProperty, 11.0),
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
            var offers = _db.GetOffers();
            foreach (var o in offers) _items.Add(o);

            _countTxt.Text = $"{_items.Count} عرض";
            _activeCountTxt.Text = offers.Count(o => o.IsActive).ToString();

            var maxDiscount = offers.Where(o => o.IsActive).Select(o => o.DiscountPercent).DefaultIfEmpty(0).Max();
            _totalDiscountTxt.Text = $"حتى {maxDiscount:F0}%";
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
            Title = editing == null ? "عرض جديد" : "تعديل العرض";
            Width = 480;
            SizeToContent = SizeToContent.Height;
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

            // Header
            var header = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
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
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#E63946")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = _editing == null ? "أدخل بيانات العرض" : $"تعديل: {_editing.Title}",
                FontSize = 11,
                Foreground = UiHelper.B("#5a6a80"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Fields
            var fields = new StackPanel { Margin = new Thickness(22, 18, 22, 10) };

            fields.Children.Add(UiHelper.FieldLabel("عنوان العرض *"));
            _tbTitle = UiHelper.MakeTB(_editing?.Title ?? "", "#E63946");
            _tbTitle.Margin = new Thickness(0, 4, 0, 16);
            fields.Children.Add(_tbTitle);

            fields.Children.Add(UiHelper.FieldLabel("وصف العرض"));
            _tbDesc = UiHelper.MakeTB(_editing?.Description ?? "", "#E63946");
            _tbDesc.Margin = new Thickness(0, 4, 0, 16);
            _tbDesc.Height = 60;
            _tbDesc.TextWrapping = TextWrapping.Wrap;
            _tbDesc.AcceptsReturn = true;
            fields.Children.Add(_tbDesc);

            var numRow = new Grid();
            numRow.ColumnDefinitions.Add(new ColumnDefinition());
            numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            numRow.ColumnDefinitions.Add(new ColumnDefinition());

            var discPanel = new StackPanel();
            discPanel.Children.Add(UiHelper.FieldLabel("نسبة الخصم (%)"));
            _tbDiscount = UiHelper.MakeTB(_editing?.DiscountPercent.ToString("F0") ?? "0", "#E63946");
            _tbDiscount.Margin = new Thickness(0, 4, 0, 16);
            discPanel.Children.Add(_tbDiscount);
            Grid.SetColumn(discPanel, 0);
            numRow.Children.Add(discPanel);

            var promoPanel = new StackPanel();
            promoPanel.Children.Add(UiHelper.FieldLabel("كود الخصم"));
            _tbPromo = UiHelper.MakeTB(_editing?.PromoCode ?? "", "#E63946");
            _tbPromo.Margin = new Thickness(0, 4, 0, 16);
            promoPanel.Children.Add(_tbPromo);
            Grid.SetColumn(promoPanel, 2);
            numRow.Children.Add(promoPanel);

            fields.Children.Add(numRow);

            _chkActive = new CheckBox
            {
                Content = "عرض نشط",
                IsChecked = _editing?.IsActive ?? true,
                Foreground = UiHelper.B("#06d6a0"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 20),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            fields.Children.Add(_chkActive);

            // Buttons
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#2a2a3c", UiHelper.B("#8892a4"), () => { DialogResult = false; Close(); }, 10, 13);
            var saveBtn = UiHelper.MakeBtn(_editing == null ? "إضافة" : "حفظ", "#E63946", Brushes.White, () =>
            {
                if (string.IsNullOrWhiteSpace(_tbTitle.Text))
                {
                    MessageBox.Show("أدخل عنوان العرض", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                if (!double.TryParse(_tbDiscount.Text, out double disc)) disc = 0;
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
            }, 10, 13);
            saveBtn.MinWidth = 120;

            Grid.SetColumn(cancelBtn, 0); btnGrid.Children.Add(cancelBtn);
            Grid.SetColumn(saveBtn, 2); btnGrid.Children.Add(saveBtn);
            fields.Children.Add(btnGrid);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // Footer
            var footer = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(22, 12, 22, 12)
            };
            footer.Child = new TextBlock
            {
                Text = _editing == null ? "أدخل بيانات العرض واضغط 'إضافة'" : "عدّل البيانات واضغط 'حفظ'",
                FontSize = 11,
                Foreground = UiHelper.B("#3a4a60"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
            Loaded += (_, _) => _tbTitle.Focus();
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
