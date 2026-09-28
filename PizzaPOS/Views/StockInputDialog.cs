// StockInputDialog.cs
// Dialog for adding stock to an ingredient.
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
    public class StockInputDialog : Window
    {
        public double Qty { get; private set; }
        public string Note { get; private set; } = "";

        readonly Ingredient _ing;
        TextBox _tbQty = null!;
        TextBox _tbNote = null!;

        public StockInputDialog(Ingredient ing, string title, string icon)
        {
            _ing = ing;
            Title = $"{icon} {title}";
            Width = 400;
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

            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#1e3a5f"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1e3a5f"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "📥",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إضافة مخزون",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#7ab8f5")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"المادة: {_ing.Name}  •  المخزون الحالي: {_ing.Stock:F2} {_ing.Unit}",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            fields.Children.Add(UiHelper.FieldLabel($"الكمية المضافة ({_ing.Unit}) *"));
            _tbQty = UiHelper.MakeTB("", "#1e3a5f");
            _tbQty.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbQty);

            fields.Children.Add(UiHelper.FieldLabel("ملاحظة"));
            _tbNote = UiHelper.MakeTB("", "#1e3a5f");
            _tbNote.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbNote);

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

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

            var saveBtn = UiHelper.MakeBtn("📥 إضافة المخزون", "#1e3a5f", UiHelper.B("#7ab8f5"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#1e3a5f"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.4
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbQty.Focus();
        }

        void Save()
        {
            if (!double.TryParse(_tbQty.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صحيحة أكبر من صفر", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            Qty = qty;
            Note = _tbNote.Text.Trim();
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  StockAdjustDialog  –  Adjust Stock
    // ══════════════════════════════════════════════════
}
