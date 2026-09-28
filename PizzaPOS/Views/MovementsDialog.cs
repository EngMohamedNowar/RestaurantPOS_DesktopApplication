// MovementsDialog.cs
// Dialog listing recent stock movements.
//
// Was one of twelve types packed into Views/WarehouseWindow.cs. Split into
// one type per file; the body below is unchanged.

using PizzaPOS.Data;
using PizzaPOS.Helpers;
using PizzaPOS.Models;
using PizzaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Data.Sqlite;

namespace PizzaPOS.Views
{
    public class MovementsDialog : Window
    {
        public MovementsDialog()
        {
            Title = "📊 حركات المخزون";
            Width = 820; Height = 560;
            MinWidth = 700;
            Background = UiHelper.B("#0f1526");
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
            root.RowDefinitions.Add(new RowDefinition());

            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#a78bfa"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(22, 16, 22, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#a78bfa"),
                CornerRadius = new CornerRadius(12),
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "📊",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "حركات المخزون",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#eef0f2")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "آخر 7 أيام",
                FontSize = 10,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var dg = UiHelper.BuildGrid(
                rowBg: "#0d1525",
                altBg: "#0a0f1c",
                headerBg: "#0f1526",
                headerFg: "#ffd166",
                accent: "#a78bfa",
                hoverBg: "#12192e",
                selBg: "#1a2640",
                cellFg: "#eef0f2",
                rowHeight: 42,
                headerHeight: 44);

            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "المادة",
                Binding = new Binding("Ingredient"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
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
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "النوع",
                Binding = new Binding("TypeDisplay"),
                Width = 100,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#a78bfa")),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الكمية",
                Binding = new Binding("Qty") { StringFormat = "{0:F2}" },
                Width = 90,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#eef0f2"))
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "ملاحظة",
                Binding = new Binding("Note"),
                Width = 160,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#8892a4")),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "التاريخ",
                Binding = new Binding("CreatedAt"),
                Width = 140,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, UiHelper.B("#4a6080")),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            var svc = new InventoryService();
            var movements = svc.GetMovements(7);
            dg.ItemsSource = movements;

            var scroll = new ScrollViewer
            {
                Content = dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = UiHelper.B("#0f1526")
            };
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            Content = root;
        }
    }
}
