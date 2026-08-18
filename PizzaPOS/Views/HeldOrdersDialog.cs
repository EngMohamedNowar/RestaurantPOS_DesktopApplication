// Views/HeldOrdersDialog.cs
using System;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.Services;
using PizzaPOS.ViewModels;

namespace PizzaPOS.Views
{
    public class HeldOrdersDialog : Window
    {
        readonly MainViewModel _vm;
        readonly AppDbContext _db = new();

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public HeldOrdersDialog(MainViewModel vm)
        {
            _vm = vm;
            Title = "⏸ الأوردرات المعلقة";
            Width = 480; Height = 540;
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
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ══ Header ══
            var header = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 14, 16, 14)
            };
            var hGrid = new Grid();
            var titleSp = new StackPanel { Orientation = Orientation.Horizontal };

            var iconB = new Border
            {
                Background = B("#1a2640"),
                CornerRadius = new CornerRadius(8),
                Width = 34,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0)
            };
            iconB.Child = new TextBlock
            {
                Text = "⏸",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(iconB);
            titleSp.Children.Add(new TextBlock
            {
                Text = "الأوردرات المعلقة",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2"),
                VerticalAlignment = VerticalAlignment.Center
            });

            var countBorder = new Border
            {
                Background = B("#E63946"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            var countTxt = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.White
            };
            void UpdateCount() => countTxt.Text = $"{_vm.HeldOrders.Count} معلق";
            UpdateCount();
            _vm.HeldOrders.CollectionChanged += (_, _) => UpdateCount();
            countBorder.Child = countTxt;

            hGrid.Children.Add(titleSp);
            hGrid.Children.Add(countBorder);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Content ══
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12, 8, 12, 8)
            };
            var outerSp = new StackPanel();
            var itemsSp = new StackPanel();

            var emptyPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 60, 0, 0),
                Visibility = _vm.HeldOrders.Count == 0
                               ? Visibility.Visible : Visibility.Collapsed
            };
            emptyPanel.Children.Add(new TextBlock
            {
                Text = "📭",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });
            emptyPanel.Children.Add(new TextBlock
            {
                Text = "لا يوجد أوردرات معلقة",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            void RebuildItems()
            {
                itemsSp.Children.Clear();
                emptyPanel.Visibility = _vm.HeldOrders.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;
                foreach (var order in _vm.HeldOrders)
                    itemsSp.Children.Add(BuildCard(order));
            }

            _vm.HeldOrders.CollectionChanged += (_, _) => RebuildItems();
            RebuildItems();

            outerSp.Children.Add(itemsSp);
            outerSp.Children.Add(emptyPanel);
            scroll.Content = outerSp;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // ══ Footer ══
            var footer = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var fGrid = new Grid();
            fGrid.ColumnDefinitions.Add(new ColumnDefinition());
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            var hint = new TextBlock
            {
                Text = "اضغط «استئناف» أو اضغط مرتين",
                Foreground = B("#4a6080"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var closeBtn = MakeBtn("✖ إغلاق", "#12192e", "#1e2d4a",
                B("#8892a4"), () => Close());

            Grid.SetColumn(hint, 0);
            Grid.SetColumn(closeBtn, 2);
            fGrid.Children.Add(hint);
            fGrid.Children.Add(closeBtn);
            footer.Child = fGrid;
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
        }

        // ── Card ─────────────────────────────────────
        Border BuildCard(Order order)
        {
            var card = new Border
            {
                Background = B("#0d1525"),
                CornerRadius = new CornerRadius(10),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            bool isDelivery = order.OrderType == "توصيل" &&
                              !string.IsNullOrWhiteSpace(order.CustomerPhone);
            if (isDelivery)
                card.BorderBrush = B("#1e3a5f");

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            mainGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });

            // ── Info ──
            var infoSp = new StackPanel();
            Grid.SetColumn(infoSp, 0);

            // ── الصف الأول: رقم الأوردر + النوع ──
            var topRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var numBadge = new Border
            {
                Background = B("#E63946"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 8, 0)
            };
            numBadge.Child = new TextBlock
            {
                Text = order.OrderNumber,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Black,
                FontSize = 12
            };
            topRow.Children.Add(numBadge);

            var typeBadge = new Border
            {
                Background = isDelivery ? B("#0f1f35") : Brushes.Transparent,
                CornerRadius = new CornerRadius(5),
                Padding = isDelivery ? new Thickness(6, 2, 6, 2) : new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            typeBadge.Child = new TextBlock
            {
                Text = isDelivery ? $"🛵 {order.OrderType}" : order.OrderType,
                Foreground = isDelivery ? B("#4da8da") : B("#8892a4"),
                FontSize = 12,
                FontWeight = isDelivery ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };
            topRow.Children.Add(typeBadge);
            infoSp.Children.Add(topRow);

            // ── الوقت ──
            infoSp.Children.Add(new TextBlock
            {
                Text = $"🕐 {order.CreatedAt}",
                Foreground = B("#4a6080"),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 2)
            });

            // ── ملاحظات ──
            if (!string.IsNullOrWhiteSpace(order.Notes))
                infoSp.Children.Add(new TextBlock
                {
                    Text = order.Notes,
                    Foreground = B("#ffd166"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                });

            // ── عدد الأصناف ──
            infoSp.Children.Add(new TextBlock
            {
                Text = $"📦 {order.Items.Count} صنف",
                Foreground = B("#4a6080"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            // ══ بيانات التوصيل ══
            if (isDelivery)
            {
                infoSp.Children.Add(new Border
                {
                    Height = 1,
                    Background = B("#1e2d4a"),
                    Margin = new Thickness(0, 6, 0, 6),
                    Opacity = 0.6
                });

                var phoneRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                phoneRow.Children.Add(new TextBlock
                {
                    Text = $"📞 {order.CustomerPhone}",
                    Foreground = B("#4da8da"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 12, 0)
                });
                if (!string.IsNullOrWhiteSpace(order.CustomerName))
                    phoneRow.Children.Add(new TextBlock
                    {
                        Text = order.CustomerName,
                        Foreground = B("#8892a4"),
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                infoSp.Children.Add(phoneRow);

                if (!string.IsNullOrWhiteSpace(order.DeliveryAddress))
                    infoSp.Children.Add(new TextBlock
                    {
                        Text = $"📍 {order.DeliveryAddress}",
                        Foreground = B("#ffd166"),
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 3)
                    });

                if (!string.IsNullOrWhiteSpace(order.DriverName))
                    infoSp.Children.Add(new TextBlock
                    {
                        Text = $"🛵 السائق: {order.DriverName}",
                        Foreground = B("#06d6a0"),
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 2)
                    });
                else
                    infoSp.Children.Add(new TextBlock
                    {
                        Text = "🛵 بدون سائق",
                        Foreground = B("#4a6080"),
                        FontSize = 11,
                        FontStyle = FontStyles.Italic,
                        Margin = new Thickness(0, 0, 0, 2)
                    });

                if (order.DeliveryFee > 0)
                    infoSp.Children.Add(new TextBlock
                    {
                        Text = $"💰 رسوم توصيل: {order.DeliveryFee:F2} ج",
                        Foreground = B("#06d6a0"),
                        FontSize = 11,
                        Margin = new Thickness(0, 0, 0, 0)
                    });
            }

            // ── Actions ──
            var actionSp = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(12, 0, 0, 0)
            };
            Grid.SetColumn(actionSp, 1);

            var totalBorder = new Border
            {
                Background = B("#0f1f14"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 0, 6)
            };
            totalBorder.Child = new TextBlock
            {
                Text = $"{order.Total:F2} ج",
                Foreground = B("#06d6a0"),
                FontWeight = FontWeights.Black,
                FontSize = 14,
                TextAlignment = TextAlignment.Center
            };
            actionSp.Children.Add(totalBorder);

            var resumeBtn = MakeBtn("▶ استئناف", "#06d6a0", "#06d6a0",
                B("#0a0a14"), () =>
                {
                    _vm.ResumeCmd.Execute(order);
                    Close();
                });
            actionSp.Children.Add(resumeBtn);

            var printBtn = MakeBtn("🖨 طباعة", "#0f1a2e", "#1e3a5a",
                B("#4da8da"), () => PrintOrder(order));
            printBtn.Margin = new Thickness(0, 6, 0, 0);
            actionSp.Children.Add(printBtn);

            var cancelOrderBtn = MakeBtn("✖ إلغاء", "#1a0a0a", "#E63946",
                B("#E63946"), () => CancelOrder(order));
            cancelOrderBtn.Margin = new Thickness(0, 6, 0, 0);
            actionSp.Children.Add(cancelOrderBtn);

            mainGrid.Children.Add(infoSp);
            mainGrid.Children.Add(actionSp);
            card.Child = mainGrid;

            card.MouseEnter += (_, _) => card.Background = B("#12192e");
            card.MouseLeave += (_, _) => card.Background = B("#0d1525");
            card.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2)
                {
                    _vm.ResumeCmd.Execute(order);
                    Close();
                }
            };

            return card;
        }

        // ── Print Single Order (Kitchen Ticket) ──────
        void PrintOrder(Order order)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Tahoma"),
                FlowDirection = FlowDirection.RightToLeft,
                PagePadding = new Thickness(30, 20, 30, 20),
                ColumnWidth = double.PositiveInfinity,
                FontSize = 13
            };

            // ══ اسم المطعم ══
            doc.Blocks.Add(new Paragraph(new Run("** KITCHEN **"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Margin = new Thickness(0, 0, 0, 2)
            });

            doc.Blocks.Add(Separator());

            // ══ بيانات الأوردر ══
            doc.Blocks.Add(KV("رقم الأوردر", order.OrderNumber, bold: true, large: true));
            doc.Blocks.Add(KV("النوع", order.OrderType));
            doc.Blocks.Add(KV("الوقت", order.CreatedAt));

            // ══ بيانات العميل ══
            bool hasCustomer = !string.IsNullOrWhiteSpace(order.CustomerPhone);
            if (hasCustomer)
            {
                doc.Blocks.Add(Separator());
                doc.Blocks.Add(new Paragraph(
                    new Run("بيانات العميل") { FontWeight = FontWeights.Black, FontSize = 14 })
                { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });

                if (!string.IsNullOrWhiteSpace(order.CustomerName))
                    doc.Blocks.Add(KV("الاسم", order.CustomerName, bold: true));

                doc.Blocks.Add(KV("التليفون", order.CustomerPhone, bold: true));

                if (!string.IsNullOrWhiteSpace(order.DeliveryAddress))
                    doc.Blocks.Add(KV("العنوان", order.DeliveryAddress));
            }

            // ملاحظات
            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                doc.Blocks.Add(Separator());
                doc.Blocks.Add(new Paragraph(
                    new Run("⚠ ملاحظات:") { FontWeight = FontWeights.Black })
                { Margin = new Thickness(0, 0, 0, 2) });
                doc.Blocks.Add(new Paragraph(new Run(order.Notes))
                { Margin = new Thickness(8, 0, 0, 0) });
            }

            doc.Blocks.Add(Separator());

            // ══ الأصناف ══
            doc.Blocks.Add(new Paragraph(
                new Run("الأصناف") { FontWeight = FontWeights.Black, FontSize = 14 })
            { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 6) });

            foreach (var item in order.Items)
            {
                var itemPara = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
                itemPara.Inlines.Add(new Run($"x{item.Qty}  ")
                { FontWeight = FontWeights.Black, FontSize = 15 });
                itemPara.Inlines.Add(new Run(item.Name)
                { FontWeight = FontWeights.Black, FontSize = 15 });
                doc.Blocks.Add(itemPara);

                if (!string.IsNullOrWhiteSpace(item.SizeName))
                    doc.Blocks.Add(new Paragraph(
                        new Run($"   📐 الحجم: {item.SizeName}"))
                    {
                        Foreground = Brushes.DimGray,
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 1)
                    });

                if (!string.IsNullOrWhiteSpace(item.ExtrasNote))
                    doc.Blocks.Add(new Paragraph(
                        new Run($"   ➕ إضافات: {item.ExtrasNote}"))
                    {
                        Foreground = Brushes.DimGray,
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 1)
                    });

                var pricePara = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                if (item.SizeExtraPrice > 0 || item.ExtrasPrice > 0)
                {
                    if (item.SizeExtraPrice > 0)
                        pricePara.Inlines.Add(new Run($"  +حجم {item.SizeExtraPrice:F2}")
                        { Foreground = Brushes.Gray, FontSize = 11 });
                    if (item.ExtrasPrice > 0)
                        pricePara.Inlines.Add(new Run($"  +إضافات {item.ExtrasPrice:F2}")
                        { Foreground = Brushes.Gray, FontSize = 11 });
                }
                doc.Blocks.Add(pricePara);

                doc.Blocks.Add(new BlockUIContainer(new Border
                {
                    Height = 1,
                    Background = Brushes.LightGray,
                    Margin = new Thickness(0, 0, 0, 8),
                    Opacity = 0.4
                }));
            }

            doc.Blocks.Add(Separator());

            doc.Blocks.Add(new Paragraph(
                new Run($"طُبع: {DateTime.Now:yyyy/MM/dd  hh:mm tt}"))
            {
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 4, 0, 0)
            });

            // ══ إرسال للطابعة تلقائياً ══
            var dlg = new PrintDialog();

            try
            {
                using var server = new LocalPrintServer();

                // ابحث عن EPSON TM-T88V أولاً، ثم أي Epson/POS، وأخيراً الافتراضية
                var target = server.GetPrintQueues()
                    .FirstOrDefault(q =>
                        q.Name.IndexOf("TM-T88V", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        q.Name.IndexOf("TM-T88", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        q.Name.IndexOf("Epson", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        q.Name.IndexOf("POS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        q.Name.IndexOf("Receipt", StringComparison.OrdinalIgnoreCase) >= 0);

                dlg.PrintQueue = target ?? server.DefaultPrintQueue;
            }
            catch
            {
                // لو فشل التعرف التلقائي، اعرض الـ Dialog كـ fallback
                if (dlg.ShowDialog() != true) return;
            }

            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
            dlg.PrintDocument(paginator, $"أوردر {order.OrderNumber}");
        }

        // ── Helpers ──────────────────────────────────
        static Paragraph KV(string key, string val, bool bold = false, bool large = false)
        {
            var p = new Paragraph { Margin = new Thickness(0, 0, 0, 3) };
            p.Inlines.Add(new Run($"{key}: ") { FontWeight = FontWeights.Bold });
            p.Inlines.Add(new Run(val)
            {
                FontWeight = bold ? FontWeights.Black : FontWeights.Normal,
                FontSize = large ? 15 : 13
            });
            return p;
        }

        static BlockUIContainer Separator() => new(new Border
        {
            Height = 1,
            Background = Brushes.Black,
            Margin = new Thickness(0, 6, 0, 6)
        });

        // ── Cancel Order → تسجيل خسارة ──────────────
        void CancelOrder(Order order)
        {
            var result = MessageBox.Show(
                $"هتلغي أوردر {order.OrderNumber}؟\n" +
                $"الإجمالي: {order.Total:F2} ج\n\n" +
                $"هيتسجل تلقائياً في الخسائر.",
                "تأكيد الإلغاء",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var items = string.Join("، ",
                order.Items.ConvertAll(i => $"{i.Name} x{i.Qty}"));

            _db.SaveLoss(new LossEntry
            {
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                Type = "أوردر ملغي",
                Description = $"أوردر {order.OrderNumber} | {order.OrderType} | {items}",
                Amount = order.Total,
                CreatedBy = SessionService.CurrentUser?.FullName ?? "—"
            });

            _vm.HeldOrders.Remove(order);

            MessageBox.Show(
                $"✅ تم إلغاء الأوردر وتسجيله في الخسائر\nالمبلغ: {order.Total:F2} ج",
                "تم الإلغاء",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // ── Round Button ─────────────────────────────
        Button MakeBtn(string text, string bg, string borderColor,
                       SolidColorBrush fg, Action click)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            f.SetBinding(Border.PaddingProperty,
                new Binding("Padding")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
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
                BorderBrush = B(borderColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 7, 12, 7),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }
    }
}
