// IngredientEditDialog.cs
// Dialog for creating or editing a single ingredient.
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
    public class IngredientEditDialog : Window
    {
        public Ingredient? Result { get; private set; }

        readonly Ingredient? _editing;
        TextBox _tbName = null!;
        TextBox _tbUnit = null!;
        TextBox _tbStock = null!;
        TextBox _tbMin = null!;
        TextBox _tbCost = null!;
        ComboBox _cbCategory = null!;

        public IngredientEditDialog(Ingredient? editing)
        {
            _editing = editing;
            Title = editing == null ? "إضافة مادة جديدة" : "تعديل المادة";
            Width = 460;
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

            bool isAdd = _editing == null;
            var accentHex = isAdd ? "#06d6a0" : "#ffd166";

            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B(accentHex),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = UiHelper.B(accentHex),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = isAdd ? "➕" : "✏️",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = isAdd ? "إضافة مادة جديدة" : "تعديل بيانات المادة",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B(accentHex)
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = isAdd ? "أدخل بيانات المادة الجديدة" : $"تعديل: {_editing!.Name}",
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

            fields.Children.Add(UiHelper.FieldLabel("اسم المادة *"));
            _tbName = UiHelper.MakeTB(_editing?.Name ?? "", "#a78bfa");
            _tbName.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbName);

            fields.Children.Add(UiHelper.FieldLabel("الفئة *"));
            _cbCategory = new ComboBox
            {
                Background = UiHelper.B("#0f1526"),
                Foreground = UiHelper.B("#eef0f2"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 9, 10, 9),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 14),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#0f1526")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, UiHelper.B("#eef0f2")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(12, 9, 12, 9)));
            var hov = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#1a2640")));
            itemStyle.Triggers.Add(hov);
            var selT = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selT.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, UiHelper.B("#a78bfa")));
            selT.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, UiHelper.B("#0a0a14")));
            itemStyle.Triggers.Add(selT);
            _cbCategory.ItemContainerStyle = itemStyle;
            _cbCategory.Resources.Add(SystemColors.WindowBrushKey, UiHelper.B("#0f1526"));
            _cbCategory.Resources.Add(SystemColors.HighlightBrushKey, UiHelper.B("#a78bfa"));
            _cbCategory.Resources.Add(SystemColors.ControlBrushKey, UiHelper.B("#0f1526"));

            LoadCategories();

            fields.Children.Add(_cbCategory);

            fields.Children.Add(UiHelper.FieldLabel("الوحدة (كجم / لتر / قطعة ...) *"));
            _tbUnit = UiHelper.MakeTB(_editing?.Unit ?? "", "#a78bfa");
            _tbUnit.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbUnit);

            var numRow = new Grid();
            numRow.ColumnDefinitions.Add(new ColumnDefinition());
            numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            numRow.ColumnDefinitions.Add(new ColumnDefinition());

            var stockPanel = new StackPanel();
            stockPanel.Children.Add(UiHelper.FieldLabel("المخزون الحالي *"));
            _tbStock = UiHelper.MakeTB(_editing?.Stock.ToString("F2") ?? "0", "#a78bfa");
            _tbStock.Margin = new Thickness(0, 4, 0, 14);
            stockPanel.Children.Add(_tbStock);
            Grid.SetColumn(stockPanel, 0);
            numRow.Children.Add(stockPanel);

            var minPanel = new StackPanel();
            minPanel.Children.Add(UiHelper.FieldLabel("الحد الأدنى *"));
            _tbMin = UiHelper.MakeTB(_editing?.MinStock.ToString("F2") ?? "0", "#a78bfa");
            _tbMin.Margin = new Thickness(0, 4, 0, 14);
            minPanel.Children.Add(_tbMin);
            Grid.SetColumn(minPanel, 2);
            numRow.Children.Add(minPanel);

            fields.Children.Add(numRow);

            fields.Children.Add(UiHelper.FieldLabel("التكلفة للوحدة (ج) *"));
            _tbCost = UiHelper.MakeTB(_editing?.CostPerUnit.ToString("F2") ?? "0", "#a78bfa");
            _tbCost.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbCost);

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

            var saveBtn = UiHelper.MakeBtn(
                isAdd ? "➕ إضافة" : "💾 حفظ",
                accentHex, UiHelper.B("#0a0a14"), Save);

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

        void LoadCategories()
        {
            try
            {
                using var conn = DatabaseHelper.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Name FROM IngredientCategories ORDER BY Name";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int id = r.GetInt32(0);
                    string name = r.GetString(1);
                    var item = new ComboBoxItem { Content = name, Tag = id };
                    _cbCategory.Items.Add(item);
                    if (_editing != null && id == _editing.CategoryId)
                        _cbCategory.SelectedItem = item;
                }
            }
            catch (Exception ex)
            {
                // الـ combo هيبقى فاضي. قبل الـ catch الفاضي ده كان صامت
                // وأي حفظ بعدها بيحفظ category = 0. دلوقتي فيه أثر في اللوج.
                AppLogger.Warn($"Category combo failed to load in warehouse: {ex.Message}");
            }
            if (_cbCategory.SelectedItem == null && _cbCategory.Items.Count > 0)
                _cbCategory.SelectedIndex = 0;
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbName.Text))
            {
                MessageBox.Show("أدخل اسم المادة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (_cbCategory.SelectedItem is not ComboBoxItem catItem)
            {
                MessageBox.Show("اختر الفئة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(_tbUnit.Text))
            {
                MessageBox.Show("أدخل الوحدة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!double.TryParse(_tbStock.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double stock) || stock < 0)
            {
                MessageBox.Show("المخزون غير صحيح", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!double.TryParse(_tbMin.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double min) || min < 0)
            {
                MessageBox.Show("الحد الأدنى غير صحيح", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!double.TryParse(_tbCost.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double cost) || cost < 0)
            {
                MessageBox.Show("التكلفة غير صحيحة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            int catId = (int)catItem.Tag!;
            string catName = catItem.Content?.ToString() ?? "";

            Result = new Ingredient
            {
                CategoryId = catId,
                CategoryName = catName,
                Name = _tbName.Text.Trim(),
                Unit = _tbUnit.Text.Trim(),
                Stock = stock,
                MinStock = min,
                CostPerUnit = cost
            };
            DialogResult = true;
            Close();
        }
    }

    // ══════════════════════════════════════════════════
    //  StockInputDialog  –  Add Stock
    // ══════════════════════════════════════════════════
}
