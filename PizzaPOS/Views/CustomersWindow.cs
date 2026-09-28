// Views/CustomersWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PizzaPOS.Services;

namespace PizzaPOS.Views
{
    public class CustomersWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<Customer> _customers = new();
        DataGrid _dg = null!;
        TextBox _tbSearch = null!;
        TextBlock _totalCountTxt = null!;
        TextBlock _totalPointsTxt = null!;
        TextBlock _avgSpendTxt = null!;
        TextBlock _topTierTxt = null!;

        public CustomersWindow()
        {
            Title = "ðŸ‘¥ Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ø¹Ù…Ù„Ø§Ø¡";
            Width = 1060; Height = 660;
            MinWidth = 900;
            Background = UiHelper.B("#070b14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
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

            // â•â• Header â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            var header = new Border
            {
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#a78bfa"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border
            {
                Background = UiHelper.B("#a78bfa"),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 14, 0)
            };
            iconBorder.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#a78bfa"),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.5
            };
            iconBorder.Child = new TextBlock
            {
                Text = "ðŸ‘¥",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = "Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ø¹Ù…Ù„Ø§Ø¡",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#eef0f2")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "Ø¥Ø¯Ø§Ø±Ø© Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ø¹Ù…Ù„Ø§Ø¡ ÙˆÙ†Ø¸Ø§Ù… Ù†Ù‚Ø§Ø· Ø§Ù„ÙˆÙ„Ø§Ø¡",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });

            var countBadge = new Border
            {
                Background = UiHelper.B("#1a2640"),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            var headerCountTxt = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#a78bfa")
            };
            _customers.CollectionChanged += (_, _) => headerCountTxt.Text = $"{_customers.Count} Ø¹Ù…ÙŠÙ„";
            countBadge.Child = headerCountTxt;

            Grid.SetColumn(iconBorder, 0); hGrid.Children.Add(iconBorder);
            Grid.SetColumn(titleStack, 1); hGrid.Children.Add(titleStack);
            Grid.SetColumn(countBadge, 2); hGrid.Children.Add(countBadge);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // â•â• Stats Bar â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            var statsBorder = new Border
            {
                Background = UiHelper.B("#090e1a"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(22, 12, 22, 12)
            };
            var statsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _totalCountTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#a78bfa")
            };
            _totalPointsTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#06d6a0")
            };
            _avgSpendTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            };
            _topTierTxt = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#E63946")
            };

            statsPanel.Children.Add(UiHelper.MakeStatCard("ðŸ‘¥ Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„Ø¹Ù…Ù„Ø§Ø¡", _totalCountTxt, "#a78bfa", "#130f20"));
            statsPanel.Children.Add(UiHelper.MakeStatCard("â­ Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„Ù†Ù‚Ø§Ø·", _totalPointsTxt, "#06d6a0", "#0a1f18"));
            statsPanel.Children.Add(UiHelper.MakeStatCard("ðŸ’° Ù…ØªÙˆØ³Ø· Ø§Ù„Ù…Ø´ØªØ±ÙŠØ§Øª", _avgSpendTxt, "#ffd166", "#1a1508"));
            statsPanel.Children.Add(UiHelper.MakeStatCard("ðŸ† ÙƒØ¨Ø§Ø± Ø§Ù„Ø¹Ù…Ù„Ø§Ø¡ (Ù…Ø§Ø³ÙŠ+Ø°Ù‡Ø¨ÙŠ)", _topTierTxt, "#E63946", "#1a080a"));

            statsBorder.Child = statsPanel;
            Grid.SetRow(statsBorder, 1);
            root.Children.Add(statsBorder);

            // â•â• Search Bar â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            var searchBar = new Border
            {
                Background = UiHelper.B("#0c1221"),
                BorderBrush = UiHelper.B("#1a2540"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(22, 10, 22, 10)
            };
            var searchSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var searchLabel = new TextBlock
            {
                Text = "ðŸ”  Ø¨Ø­Ø«:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#4a6080"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            _tbSearch = UiHelper.MakeTB("", "#a78bfa");
            _tbSearch.Width = 320;
            _tbSearch.Height = 36;
            _tbSearch.TextWrapping = TextWrapping.NoWrap;
            _tbSearch.TextChanged += (_, _) => Load(_tbSearch.Text.Trim());

            searchSp.Children.Add(searchLabel);
            searchSp.Children.Add(_tbSearch);
            searchBar.Child = searchSp;
            Grid.SetRow(searchBar, 2);
            root.Children.Add(searchBar);

            // â•â• DataGrid â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            _dg = UiHelper.BuildGrid(
                accent: "#a78bfa",
                headerFg: "#ffd166",
                hoverBg: "#12192e",
                selBg: "#1a2640"
            );
            _dg.ItemsSource = _customers;

            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Ø§Ù„Ø§Ø³Ù…",
                Binding = new Binding("Name"),
                Width = 140,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            _dg.Columns.Add(UiHelper.Col("Ø§Ù„ØªÙ„ÙŠÙÙˆÙ†", "Phone", 120));
            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Ø§Ù„Ø¹Ù†ÙˆØ§Ù†",
                Binding = new Binding("Address"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            _dg.Columns.Add(UiHelper.ColInt("Ø§Ù„Ø·Ù„Ø¨Ø§Øª", "TotalOrders", 80, "#ffd166"));
            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„Ù…Ø´ØªØ±ÙŠØ§Øª",
                Binding = new Binding("TotalSpent") { StringFormat = "{0:F2} Ø¬" },
                Width = 130,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#06d6a0")),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            _dg.Columns.Add(UiHelper.ColInt("Ø§Ù„Ù†Ù‚Ø§Ø·", "LoyaltyPoints", 80, "#a78bfa"));

            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            //  Ø¹Ù…ÙˆØ¯ Ø§Ù„Ù…Ø³ØªÙˆÙ‰ (Tier) â€” ÙƒÙ„ Ù…Ø³ØªÙˆÙ‰ Ø¨Ù„ÙˆÙ†Ù‡ Ø§Ù„Ù…Ù…ÙŠØ²
            //  ðŸ’Ž Ù…Ø§Ø³ÙŠ  â†’ Ø£Ø²Ø±Ù‚ Ù…Ø§Ø³ÙŠ ÙØ§ØªØ­ Ù„Ø§Ù…Ø¹
            //  ðŸ¥‡ Ø°Ù‡Ø¨ÙŠ  â†’ Ø¯Ù‡Ø¨ÙŠ
            //  ðŸ¥ˆ ÙØ¶ÙŠ   â†’ ÙØ¶ÙŠ
            //  ðŸ¥‰ Ø¨Ø±ÙˆÙ†Ø²ÙŠ â†’ Ø¨Ø±ÙˆÙ†Ø²ÙŠ
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            var tierCol = new DataGridTemplateColumn
            {
                Header = "Ø§Ù„Ù…Ø³ØªÙˆÙ‰",
                Width = 120
            };
            var tierTpl = new DataTemplate();

            var tierFactory = new FrameworkElementFactory(typeof(Border));
            tierFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            tierFactory.SetValue(Border.PaddingProperty, new Thickness(8, 3, 8, 3));
            tierFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            tierFactory.SetValue(Border.MarginProperty, new Thickness(0, 6, 0, 6));

            var tierTxt = new FrameworkElementFactory(typeof(TextBlock));
            tierTxt.SetBinding(TextBlock.TextProperty, new Binding("LoyaltyTier"));
            tierTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            tierTxt.SetValue(TextBlock.FontSizeProperty, 11.0);
            tierTxt.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            tierFactory.AppendChild(tierTxt);
            tierTpl.VisualTree = tierFactory;

            // â”€â”€ Style Ø¨ÙŠØ­Ø¯Ø¯ Ù„ÙˆÙ† ÙƒÙ„ Ù…Ø³ØªÙˆÙ‰ Ø¨Ù†Ø§Ø¡Ù‹ Ø¹Ù„Ù‰ Ù‚ÙŠÙ…Ø© LoyaltyTier â”€â”€
            var tierBorderStyle = new Style(typeof(Border));
            // Ø§Ù„Ù„ÙˆÙ† Ø§Ù„Ø§ÙØªØ±Ø§Ø¶ÙŠ (Ø¨Ø±ÙˆÙ†Ø²ÙŠ) ÙƒÙ€ fallback
            tierBorderStyle.Setters.Add(new Setter(Border.BackgroundProperty, UiHelper.B("#cd7f32")));

            var diamondTrigger = new DataTrigger
            {
                Binding = new Binding("LoyaltyTier"),
                Value = "ðŸ’Ž Ù…Ø§Ø³ÙŠ"
            };
            diamondTrigger.Setters.Add(new Setter(Border.BackgroundProperty, UiHelper.B("#7dd3ea"))); // Ø£Ø²Ø±Ù‚ Ù…Ø§Ø³ÙŠ ÙØ§ØªØ­

            var goldTrigger = new DataTrigger
            {
                Binding = new Binding("LoyaltyTier"),
                Value = "ðŸ¥‡ Ø°Ù‡Ø¨ÙŠ"
            };
            goldTrigger.Setters.Add(new Setter(Border.BackgroundProperty, UiHelper.B("#ffd700"))); // Ø¯Ù‡Ø¨ÙŠ

            var silverTrigger = new DataTrigger
            {
                Binding = new Binding("LoyaltyTier"),
                Value = "ðŸ¥ˆ ÙØ¶ÙŠ"
            };
            silverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, UiHelper.B("#c0c0c0"))); // ÙØ¶ÙŠ

            var bronzeTrigger = new DataTrigger
            {
                Binding = new Binding("LoyaltyTier"),
                Value = "ðŸ¥‰ Ø¨Ø±ÙˆÙ†Ø²ÙŠ"
            };
            bronzeTrigger.Setters.Add(new Setter(Border.BackgroundProperty, UiHelper.B("#cd7f32"))); // Ø¨Ø±ÙˆÙ†Ø²ÙŠ

            tierBorderStyle.Triggers.Add(diamondTrigger);
            tierBorderStyle.Triggers.Add(goldTrigger);
            tierBorderStyle.Triggers.Add(silverTrigger);
            tierBorderStyle.Triggers.Add(bronzeTrigger);

            tierFactory.SetValue(FrameworkElement.StyleProperty, tierBorderStyle);

            // Ù†Øµ Ø§Ù„Ù…Ø³ØªÙˆÙ‰ Ø¯Ø§ÙƒÙ† Ø¯Ø§ÙŠÙ…Ø§Ù‹ Ø¹Ø´Ø§Ù† ÙŠØ¨Ø§Ù† ÙˆØ§Ø¶Ø­ ÙÙˆÙ‚ Ø§Ù„Ø£Ù„ÙˆØ§Ù† Ø§Ù„ÙØ§ØªØ­Ø© Ø¯ÙŠ
            tierTxt.SetValue(TextBlock.ForegroundProperty, UiHelper.B("#0a0a14"));

            tierCol.CellTemplate = tierTpl;
            _dg.Columns.Add(tierCol);

            _dg.MouseDoubleClick += (_, _) =>
            {
                if (_dg.SelectedItem is Customer) EditCustomer();
            };

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
            Grid.SetRow(gridWrapper, 3);
            root.Children.Add(gridWrapper);

            // â•â• Action Bar â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addBtn = UiHelper.MakeActionButton("âž•  Ø¥Ø¶Ø§ÙØ© Ø¹Ù…ÙŠÙ„", "#06d6a0", UiHelper.B("#0a0a14"));
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.4
            };
            addBtn.Click += (_, _) => AddCustomer();

            var editBtn = UiHelper.MakeActionButton("âœï¸  ØªØ¹Ø¯ÙŠÙ„", "#ffd166", UiHelper.B("#0a0a14"));
            editBtn.Click += (_, _) => EditCustomer();

            var delBtn = UiHelper.MakeActionButton("ðŸ—‘  Ø­Ø°Ù", "#1a0810", UiHelper.B("#E63946"));
            delBtn.BorderBrush = UiHelper.B("#E63946");
            delBtn.BorderThickness = new Thickness(1);
            delBtn.Click += (_, _) => DeleteCustomer();

            var refreshBtn = UiHelper.MakeActionButton("ðŸ”„  ØªØ­Ø¯ÙŠØ«", "#1e3a5f", UiHelper.B("#7ab8f5"));
            refreshBtn.Click += (_, _) =>
            {
                _tbSearch.Text = "";
                Load();
            };

            Grid.SetColumn(addBtn, 0); actionGrid.Children.Add(addBtn);
            Grid.SetColumn(editBtn, 1); actionGrid.Children.Add(editBtn);
            Grid.SetColumn(delBtn, 2); actionGrid.Children.Add(delBtn);
            Grid.SetColumn(refreshBtn, 4); actionGrid.Children.Add(refreshBtn);

            actionBar.Child = actionGrid;
            Grid.SetRow(actionBar, 4);
            root.Children.Add(actionBar);

            Content = root;
            Load();
            UpdateStats();
        }

        // â”€â”€ CRUD â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        void Load(string? search = null)
        {
            _customers.Clear();
            foreach (var c in _db.GetCustomers(search)) _customers.Add(c);
            UpdateStats();
        }

        void UpdateStats()
        {
            var all = _customers.ToList();
            _totalCountTxt.Text = all.Count.ToString();
            _totalPointsTxt.Text = all.Sum(c => c.LoyaltyPoints).ToString("N0");
            _avgSpendTxt.Text = all.Count > 0
                ? $"{all.Average(c => c.TotalSpent):F2} Ø¬"
                : "0.00 Ø¬";
            _topTierTxt.Text = all.Count(c =>
                c.TotalSpent >= 2000).ToString("N0");
        }

        void AddCustomer()
        {
            var dlg = new CustomerEditDialog(null) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveCustomer(dlg.Result!);
            Load(_tbSearch.Text.Trim());
        }

        void EditCustomer()
        {
            if (_dg.SelectedItem is not Customer c)
            {
                MessageBox.Show("Ø§Ø®ØªØ± Ø¹Ù…ÙŠÙ„ Ø£ÙˆÙ„Ø§Ù‹", "ØªÙ†Ø¨ÙŠÙ‡",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            var dlg = new CustomerEditDialog(c) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveCustomer(dlg.Result!);
            Load(_tbSearch.Text.Trim());
        }

        void DeleteCustomer()
        {
            if (_dg.SelectedItem is not Customer c)
            {
                MessageBox.Show("Ø§Ø®ØªØ± Ø¹Ù…ÙŠÙ„ Ø£ÙˆÙ„Ø§Ù‹", "ØªÙ†Ø¨ÙŠÙ‡",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (MessageBox.Show(
                $"Ù‡Ù„ ØªØ±ÙŠØ¯ Ø­Ø°Ù Ø§Ù„Ø¹Ù…ÙŠÙ„ \"{c.Name}\"ØŸ\nØ³ÙŠØªÙ… Ø­Ø°Ù Ø¬Ù…ÙŠØ¹ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ø¹Ù…ÙŠÙ„ Ù†Ù‡Ø§Ø¦ÙŠØ§Ù‹.",
                "ØªØ£ÙƒÙŠØ¯ Ø§Ù„Ø­Ø°Ù",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                _db.DeleteCustomer(c.Id);
            }
            catch (Exception ex)
            {
                DbErrors.Report(ex, "Ø­Ø°Ù Ø§Ù„Ø¹Ù…ÙŠÙ„");
                return;
            }
            Load(_tbSearch.Text.Trim());
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  CustomerEditDialog
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        public class CustomerEditDialog : Window
        {
            public Customer? Result { get; private set; }
            readonly Customer? _editing;
            TextBox _tbName = null!;
            TextBox _tbPhone = null!;
            TextBox _tbAddress = null!;
            TextBox _tbNotes = null!;

            public CustomerEditDialog(Customer? editing)
            {
                _editing = editing;
                Title = editing == null ? "âž• Ø¹Ù…ÙŠÙ„ Ø¬Ø¯ÙŠØ¯" : "âœï¸ ØªØ¹Ø¯ÙŠÙ„ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ø¹Ù…ÙŠÙ„";
                Width = 440;
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

                // â”€â”€ Header â”€â”€
                var header = new Border
                {
                    Background = UiHelper.B("#0c1221"),
                    BorderBrush = _editing == null
                        ? UiHelper.B("#06d6a0")
                        : UiHelper.B("#ffd166"),
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    Padding = new Thickness(20, 16, 20, 16)
                };
                var hSp = new StackPanel { Orientation = Orientation.Horizontal };
                var hIcon = new Border
                {
                    Background = _editing == null
                        ? UiHelper.B("#06d6a0")
                        : UiHelper.B("#ffd166"),
                    CornerRadius = new CornerRadius(10),
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(0, 0, 14, 0)
                };
                hIcon.Child = new TextBlock
                {
                    Text = _editing == null ? "âž•" : "âœï¸",
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                hInfo.Children.Add(new TextBlock
                {
                    Text = _editing == null ? "Ø¥Ø¶Ø§ÙØ© Ø¹Ù…ÙŠÙ„ Ø¬Ø¯ÙŠØ¯" : "ØªØ¹Ø¯ÙŠÙ„ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ø¹Ù…ÙŠÙ„",
                    FontSize = 16,
                    FontWeight = FontWeights.Black,
                    Foreground = _editing == null
                        ? UiHelper.B("#06d6a0")
                        : UiHelper.B("#ffd166")
                });
                hInfo.Children.Add(new TextBlock
                {
                    Text = _editing == null
                        ? "Ø£Ø¯Ø®Ù„ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ø¹Ù…ÙŠÙ„ Ø§Ù„Ø¬Ø¯ÙŠØ¯"
                        : $"ØªØ¹Ø¯ÙŠÙ„: {_editing.Name}",
                    FontSize = 10,
                    Foreground = UiHelper.B("#4a6080"),
                    Margin = new Thickness(0, 3, 0, 0)
                });
                hSp.Children.Add(hIcon);
                hSp.Children.Add(hInfo);
                header.Child = hSp;
                Grid.SetRow(header, 0);
                root.Children.Add(header);

                // â”€â”€ Fields â”€â”€
                var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

                fields.Children.Add(UiHelper.FieldLabel("Ø§Ù„Ø§Ø³Ù… *"));
                _tbName = UiHelper.MakeTB(_editing?.Name ?? "", "#a78bfa");
                _tbName.Margin = new Thickness(0, 4, 0, 14);
                fields.Children.Add(_tbName);

                fields.Children.Add(UiHelper.FieldLabel("Ø§Ù„ØªÙ„ÙŠÙÙˆÙ† *"));
                _tbPhone = UiHelper.MakeTB(_editing?.Phone ?? "", "#a78bfa");
                _tbPhone.Margin = new Thickness(0, 4, 0, 14);
                fields.Children.Add(_tbPhone);

                fields.Children.Add(UiHelper.FieldLabel("Ø§Ù„Ø¹Ù†ÙˆØ§Ù†"));
                _tbAddress = UiHelper.MakeTB(_editing?.Address ?? "", "#a78bfa");
                _tbAddress.Margin = new Thickness(0, 4, 0, 14);
                fields.Children.Add(_tbAddress);

                fields.Children.Add(UiHelper.FieldLabel("Ù…Ù„Ø§Ø­Ø¸Ø§Øª"));
                _tbNotes = UiHelper.MakeTB(_editing?.Notes ?? "", "#a78bfa");
                _tbNotes.Height = 60;
                _tbNotes.AcceptsReturn = true;
                _tbNotes.TextWrapping = TextWrapping.Wrap;
                _tbNotes.Margin = new Thickness(0, 4, 0, 14);
                fields.Children.Add(_tbNotes);

                Grid.SetRow(fields, 1);
                root.Children.Add(fields);

                // â”€â”€ Button Bar â”€â”€
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

                var cancelBtn = UiHelper.MakeBtn("Ø¥Ù„ØºØ§Ø¡", "#12192e", UiHelper.B("#8892a4"),
                    () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

                var saveColor = _editing == null ? "#06d6a0" : "#ffd166";
                var saveLabel = _editing == null ? "âž• Ø¥Ø¶Ø§ÙØ©" : "ðŸ’¾ Ø­ÙØ¸";
                var saveBtn = UiHelper.MakeBtn(saveLabel, saveColor,
                    UiHelper.B("#0a0a14"), Save);

                Grid.SetColumn(cancelBtn, 0);
                Grid.SetColumn(saveBtn, 2);
                btnGrid.Children.Add(cancelBtn);
                btnGrid.Children.Add(saveBtn);
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
                    MessageBox.Show("Ø£Ø¯Ø®Ù„ Ø§Ø³Ù… Ø§Ù„Ø¹Ù…ÙŠÙ„", "ØªÙ†Ø¨ÙŠÙ‡",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    _tbName.Focus(); return;
                }
                if (string.IsNullOrWhiteSpace(_tbPhone.Text))
                {
                    MessageBox.Show("Ø£Ø¯Ø®Ù„ Ø±Ù‚Ù… Ø§Ù„ØªÙ„ÙŠÙÙˆÙ†", "ØªÙ†Ø¨ÙŠÙ‡",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    _tbPhone.Focus(); return;
                }

                Result = new Customer
                {
                    Id = _editing?.Id ?? 0,
                    Name = _tbName.Text.Trim(),
                    Phone = _tbPhone.Text.Trim(),
                    Address = _tbAddress.Text.Trim(),
                    Notes = _tbNotes.Text.Trim(),
                    CreatedAt = _editing?.CreatedAt ?? "",
                    LoyaltyPoints = _editing?.LoyaltyPoints ?? 0,
                    TotalOrders = _editing?.TotalOrders ?? 0,
                    TotalSpent = _editing?.TotalSpent ?? 0
                };
                DialogResult = true;
                Close();
            }
        }
    }
}
