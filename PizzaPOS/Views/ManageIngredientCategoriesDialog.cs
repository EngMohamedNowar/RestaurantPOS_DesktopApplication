// ManageIngredientCategoriesDialog.cs
// Dialog for listing, editing and deleting ingredient categories.
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
    public class ManageIngredientCategoriesDialog : Window
    {
        readonly AppDbContext _db = new();
        ListBox _list = null!;
        TextBlock _countTxt = null!;
        TextBox _searchTb = null!;
        List<(int Id, string Name)> _allCategories = new();

        public ManageIngredientCategoriesDialog()
        {
            Title = "إدارة فئات المواد الخام";
            Width = 480;
            Height = 560;
            Background = UiHelper.B("#0f1526");
            Foreground = Brushes.White;
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
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new Border
            {
                Background = UiHelper.B("#0f1526"),
                BorderBrush = UiHelper.B("#ffd166"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(24, 16, 24, 16)
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border
            {
                Background = UiHelper.B("#1a1800"),
                CornerRadius = new CornerRadius(12),
                Width = 44,
                Height = 44,
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = new TextBlock
            {
                Text = "folder",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = UiHelper.B("#ffd166")
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = "إدارة فئات المواد الخام",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = UiHelper.B("#ffd166")
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "إضافة / تعديل / حذف الفئات",
                FontSize = 11,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 2, 0, 0)
            });

            var statsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var countCard = new Border
            {
                Background = UiHelper.B("#1a1800"),
                BorderBrush = UiHelper.B("#ffd166"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 6, 14, 6)
            };
            var countSp = new StackPanel { Orientation = Orientation.Horizontal };
            countSp.Children.Add(new TextBlock
            {
                Text = "الفئات: ",
                FontSize = 12,
                Foreground = UiHelper.B("#8892a4"),
                VerticalAlignment = VerticalAlignment.Center
            });
            _countTxt = new TextBlock
            {
                Text = "0",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#ffd166"),
                VerticalAlignment = VerticalAlignment.Center
            };
            countSp.Children.Add(_countTxt);
            countCard.Child = countSp;
            statsPanel.Children.Add(countCard);

            Grid.SetColumn(iconBorder, 0); hGrid.Children.Add(iconBorder);
            Grid.SetColumn(titleStack, 1); hGrid.Children.Add(titleStack);
            Grid.SetColumn(statsPanel, 2); hGrid.Children.Add(statsPanel);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var searchBorder = new Border
            {
                Background = UiHelper.B("#0a0f1c"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 12, 24, 12)
            };
            var searchSp = new StackPanel { Orientation = Orientation.Horizontal };
            var searchIcon = new TextBlock
            {
                Text = "search",
                FontSize = 14,
                Foreground = UiHelper.B("#4a6080"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            _searchTb = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = UiHelper.B("#eef0f2"),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontFamily = new FontFamily("Tahoma"),
                Width = 300,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            var searchStyle = new Style(typeof(TextBox));
            searchStyle.Setters.Add(new Setter(TextBox.CaretBrushProperty, UiHelper.B("#ffd166")));
            _searchTb.Style = searchStyle;
            _searchTb.TextChanged += (_, _) => FilterCategories();
            searchSp.Children.Add(searchIcon);
            searchSp.Children.Add(_searchTb);
            searchBorder.Child = searchSp;
            Grid.SetRow(searchBorder, 1);
            root.Children.Add(searchBorder);

            _list = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0)
            };
            var lstStyle = new Style(typeof(ListBoxItem));
            lstStyle.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(0)));
            lstStyle.Setters.Add(new Setter(ListBoxItem.MarginProperty, new Thickness(0)));
            lstStyle.Setters.Add(new Setter(ListBoxItem.BorderThicknessProperty, new Thickness(0)));
            var lHov = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            lHov.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, UiHelper.B("#12192e")));
            lstStyle.Triggers.Add(lHov);
            _list.ItemContainerStyle = lstStyle;

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            scroll.Content = _list;
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            var btnBar = new Border
            {
                Background = UiHelper.B("#0d1220"),
                BorderBrush = UiHelper.B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 14, 24, 16)
            };
            var btnSp = new StackPanel { Orientation = Orientation.Horizontal };

            var addBtn = UiHelper.MakeBtn("إضافة فئة جديدة", "#06d6a0", UiHelper.B("#0a0a14"), AddNew, 10, 13);
            addBtn.MinWidth = 150;
            addBtn.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#06d6a0"),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.3
            };
            btnSp.Children.Add(addBtn);
            btnSp.Children.Add(new Border { Width = 10 });
            btnSp.Children.Add(UiHelper.MakeBtn("إغلاق", "#12192e", UiHelper.B("#8892a4"),
                () => { DialogResult = false; Close(); }, borderBrush: UiHelper.B("#1e2d4a")));
            btnBar.Child = btnSp;
            Grid.SetRow(btnBar, 3);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => { LoadCategories(); _searchTb.Focus(); };
        }

        void LoadCategories()
        {
            _allCategories.Clear();
            try
            {
                using var conn = DatabaseHelper.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Name FROM IngredientCategories ORDER BY Name";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    _allCategories.Add((r.GetInt32(0), r.GetString(1)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FilterCategories();
        }

        void FilterCategories()
        {
            var q = (_searchTb?.Text ?? "").Trim();
            var filtered = string.IsNullOrEmpty(q)
                ? _allCategories
                : _allCategories.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            _list.Items.Clear();
            _countTxt.Text = filtered.Count.ToString();

            if (filtered.Count == 0)
            {
                var emptySp = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 60, 0, 0)
                };
                emptySp.Children.Add(new TextBlock
                {
                    Text = "folder_open",
                    FontSize = 48,
                    Foreground = UiHelper.B("#1e2d4a"),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                emptySp.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrEmpty(q) ? "لا توجد فئات بعد" : "لا توجد نتائج",
                    FontSize = 14,
                    Foreground = UiHelper.B("#4a6080"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 12, 0, 0)
                });
                _list.Items.Add(new ListBoxItem { Content = emptySp, IsHitTestVisible = false });
                return;
            }

            for (int i = 0; i < filtered.Count; i++)
            {
                var (id, name) = filtered[i];
                var itemBorder = new Border
                {
                    Background = i % 2 == 0 ? UiHelper.B("#0d1525") : UiHelper.B("#0a0f1c"),
                    Padding = new Thickness(20, 12, 20, 12),
                    BorderBrush = UiHelper.B("#1a2540"),
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var numBadge = new Border
                {
                    Background = UiHelper.B("#1a1800"),
                    CornerRadius = new CornerRadius(8),
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(0, 0, 14, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                numBadge.Child = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = UiHelper.B("#ffd166"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var nameTxt = new TextBlock
                {
                    Text = name,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = UiHelper.B("#eef0f2"),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var btnsSp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var editBtn = UiHelper.MakeBtn("edit", "#1a2640", UiHelper.B("#ffd166"),
                    () => EditCategory(id, name), 8, 8);
                editBtn.MinWidth = 36; editBtn.MinHeight = 36;
                editBtn.ToolTip = "تعديل";
                btnsSp.Children.Add(editBtn);
                btnsSp.Children.Add(new Border { Width = 6 });
                var delBtn = UiHelper.MakeBtn("X", "#2a1a1a", UiHelper.B("#E63946"),
                    () => DeleteCategory(id, name), 8, 8);
                delBtn.MinWidth = 36; delBtn.MinHeight = 36;
                delBtn.ToolTip = "حذف";
                btnsSp.Children.Add(delBtn);

                Grid.SetColumn(numBadge, 0); itemGrid.Children.Add(numBadge);
                Grid.SetColumn(nameTxt, 1); itemGrid.Children.Add(nameTxt);
                Grid.SetColumn(btnsSp, 2); itemGrid.Children.Add(btnsSp);
                itemBorder.Child = itemGrid;

                _list.Items.Add(new ListBoxItem { Content = itemBorder, Tag = id });
            }
        }

        void AddNew()
        {
            var dlg = new AddCategoryDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var conn = DatabaseHelper.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO IngredientCategories(Name) VALUES(@n)";
                    cmd.Parameters.AddWithValue("@n", dlg.CategoryName);
                    cmd.ExecuteNonQuery();
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void EditCategory(int id, string oldName)
        {
            var dlg = new EditCategoryDialog(oldName) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var conn = DatabaseHelper.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE IngredientCategories SET Name=@n WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@n", dlg.CategoryName);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void DeleteCategory(int id, string name)
        {
            int count;
            try
            {
                using var conn = DatabaseHelper.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Ingredients WHERE CategoryId=@id AND IsActive=1";
                cmd.Parameters.AddWithValue("@id", id);
                count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string msg = count > 0
                ? $"الفئة \"{name}\" فيها {count} مادة خام.\nهل تحذف الفئة والمواد المرتبطة بيها؟\n(المواد هتتشال من كل الوصفات، وسجل حركتها هيتفظ)"
                : $"هل أنت متأكد من حذف الفئة \"{name}\"؟";

            if (MessageBox.Show(msg, "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _db.DeleteIngredientCategory(id);
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("DeleteIngredientCategory failed", ex);
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════
    //  EditCategoryDialog
    // ══════════════════════════════════════════════════
}
