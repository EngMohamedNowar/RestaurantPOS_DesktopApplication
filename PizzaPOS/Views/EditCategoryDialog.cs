// EditCategoryDialog.cs
// Dialog for renaming an ingredient category.
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
    public class EditCategoryDialog : Window
    {
        public string CategoryName { get; private set; } = "";
        TextBox _tbName = null!;

        public EditCategoryDialog(string currentName)
        {
            Title = "تعديل الفئة";
            Width = 420;
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
                Padding = new Thickness(24, 18, 24, 18)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B("#1a1800"),
                CornerRadius = new CornerRadius(12),
                Width = 44,
                Height = 44,
                Margin = new Thickness(0, 0, 14, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "edit",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = UiHelper.B("#ffd166")
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = "تعديل الفئة",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = $"الاسم الحالي: {currentName}",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var fields = new StackPanel { Margin = new Thickness(24, 20, 24, 8) };
            fields.Children.Add(UiHelper.FieldLabel("اسم الفئة الجديد *"));
            _tbName = UiHelper.MakeTB("", "#ffd166");
            _tbName.Text = currentName;
            _tbName.Margin = new Thickness(0, 6, 0, 0);
            _tbName.FontSize = 14;
            fields.Children.Add(_tbName);
            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 14, 24, 18)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a"));

            var saveBtn = UiHelper.MakeBtn("حفظ التعديل", "#ffd166", UiHelper.B("#0a0a14"), Save, 12, 10);
            saveBtn.MinWidth = 140;
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
    //  IngredientEditDialog
    // ══════════════════════════════════════════════════
}
