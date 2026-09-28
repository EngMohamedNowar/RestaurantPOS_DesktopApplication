// AddCategoryDialog.cs
// Dialog for creating an ingredient category.
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
    public class AddCategoryDialog : Window
    {
        public string CategoryName { get; private set; } = "";
        TextBox _tbName = null!;

        public AddCategoryDialog()
        {
            Title = "📁 إضافة فئة جديدة";
            Width = 400;
            SizeToContent = SizeToContent.Height;
            Background = UiHelper.B("#0f1526");
            Foreground = Brushes.White;
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#ffd166"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1a1800"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "📁",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "إضافة فئة جديدة",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = "أدخل اسم الفئة الجديدة للمواد الخام",
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
            fields.Children.Add(UiHelper.FieldLabel("اسم الفئة *"));
            _tbName = UiHelper.MakeTB("", "#ffd166");
            _tbName.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbName);
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

            var saveBtn = UiHelper.MakeBtn("📁 إضافة الفئة", "#ffd166", UiHelper.B("#0a0a14"), Save);
            saveBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#ffd166"),
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
            Loaded += (_, _) => _tbName.Focus();
        }

        void Save()
        {
            string name = _tbName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("أدخل اسم الفئة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CategoryName = name;
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  ManageIngredientCategoriesDialog
    // ══════════════════════════════════════════════════
}
