// Views/DriversWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace PizzaPOS.Views
{
    public class DriversWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<Driver> _drivers = new();
        DataGrid _dg = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public DriversWindow()
        {
            Title = "🛵 إدارة السائقين";
            Width = 560; Height = 460;
            Background = B("#0a0a14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            BuildUI();
        }

        void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#06d6a0"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 14, 18, 14)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = B("#06d6a0"),
                CornerRadius = new CornerRadius(10),
                Width = 36,
                Height = 36,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = "🛵",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hSp.Children.Add(hIcon);
            hSp.Children.Add(new TextBlock
            {
                Text = "إدارة السائقين",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2"),
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // DataGrid
            _dg = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = Brushes.Transparent,
                RowBackground = B("#0d1525"),
                AlternatingRowBackground = B("#0a0f1c"),
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                BorderThickness = new Thickness(0),
                CanUserAddRows = false,
                RowHeight = 40,
                ColumnHeaderHeight = 40,
                FontSize = 13,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                ItemsSource = _drivers
            };

            var hs = new Style(typeof(DataGridColumnHeader));
            hs.Setters.Add(new Setter(Control.BackgroundProperty, B("#0f1526")));
            hs.Setters.Add(new Setter(Control.ForegroundProperty, B("#06d6a0")));
            hs.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            hs.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 0, 12, 0)));
            _dg.ColumnHeaderStyle = hs;

            var rs = new Style(typeof(DataGridRow));
            rs.Setters.Add(new Setter(DataGridRow.ForegroundProperty, B("#eef0f2")));
            var hov = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B("#12192e")));
            rs.Triggers.Add(hov);
            var sel = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B("#1a2640")));
            rs.Triggers.Add(sel);
            _dg.RowStyle = rs;

            var cs = new Style(typeof(DataGridCell));
            cs.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cs.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(12, 0, 12, 0)));
            cs.Setters.Add(new Setter(DataGridCell.ForegroundProperty, B("#eef0f2")));
            cs.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));
            _dg.CellStyle = cs;

            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الاسم",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "التليفون",
                Binding = new Binding("Phone"),
                Width = 140
            });

            _dg.MouseDoubleClick += (_, _) =>
            {
                if (_dg.SelectedItem is Driver) EditDriver();
            };

            var scroll = new ScrollViewer
            {
                Content = _dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // Bottom Bar
            var bar = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(14, 10, 14, 10)
            };
            var barSp = new StackPanel { Orientation = Orientation.Horizontal };

            var addBtn = MakeBtn("➕ إضافة سائق", "#06d6a0", B("#0a0a14"), AddDriver);
            var editBtn = MakeBtn("✏️ تعديل", "#ffd166", B("#0a0a14"), EditDriver);
            var deleteBtn = MakeBtn("🗑 حذف", "#E63946", Brushes.White, DeleteDriver);

            barSp.Children.Add(addBtn);
            barSp.Children.Add(editBtn);
            barSp.Children.Add(deleteBtn);
            bar.Child = barSp;
            Grid.SetRow(bar, 2);
            root.Children.Add(bar);

            Content = root;
            Load();
        }

        void Load()
        {
            _drivers.Clear();
            foreach (var d in _db.GetDrivers()) _drivers.Add(d);
        }

        void AddDriver()
        {
            var dlg = new DriverEditDialog(null) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveDriver(dlg.Result!);
            Load();
        }

        void EditDriver()
        {
            if (_dg.SelectedItem is not Driver d)
            {
                MessageBox.Show("اختر سائق أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            var dlg = new DriverEditDialog(d) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveDriver(dlg.Result!);
            Load();
        }

        void DeleteDriver()
        {
            if (_dg.SelectedItem is not Driver d)
            {
                MessageBox.Show("اختر سائق أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (MessageBox.Show($"حذف '{d.Name}'؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            d.IsActive = false;
            _db.SaveDriver(d);
            Load();
        }

        Button MakeBtn(string text, string bg, Brush fg, Action click)
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
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16, 9, 16, 9),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0),
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }
    }

    // ── Driver Edit Dialog ───────────────────────────
    public class DriverEditDialog : Window
    {
        public Driver? Result { get; private set; }
        readonly Driver? _editing;
        TextBox _tbName = null!;
        TextBox _tbPhone = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public DriverEditDialog(Driver? editing)
        {
            _editing = editing;
            Title = editing == null ? "➕ سائق جديد" : "✏️ تعديل سائق";
            Width = 360;
            SizeToContent = SizeToContent.Height;
            Background = B("#0a0a14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;
            BuildUI();
        }

        void BuildUI()
        {
            var root = new StackPanel { Margin = new Thickness(24) };

            root.Children.Add(new TextBlock
            {
                Text = _editing == null ? "➕ إضافة سائق" : "✏️ تعديل سائق",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = B("#06d6a0"),
                Margin = new Thickness(0, 0, 0, 20)
            });

            root.Children.Add(new TextBlock
            {
                Text = "الاسم *",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 6)
            });
            _tbName = MakeTB(_editing?.Name ?? "");
            _tbName.Margin = new Thickness(0, 0, 0, 14);
            root.Children.Add(_tbName);

            root.Children.Add(new TextBlock
            {
                Text = "التليفون *",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 6)
            });
            _tbPhone = MakeTB(_editing?.Phone ?? "");
            _tbPhone.Margin = new Thickness(0, 0, 0, 24);
            root.Children.Add(_tbPhone);

            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

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

            var cancelBtn = new Button
            {
                Content = "إلغاء",
                Background = B("#12192e"),
                Foreground = B("#8892a4"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0, 11, 0, 11),
                FontWeight = FontWeights.Bold,
                Template = tpl,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };

            var saveBtn = new Button
            {
                Content = _editing == null ? "➕ إضافة" : "💾 حفظ",
                Background = B("#06d6a0"),
                Foreground = B("#0a0a14"),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 11, 0, 11),
                FontWeight = FontWeights.Black,
                Template = tpl,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            saveBtn.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_tbName.Text))
                {
                    MessageBox.Show("أدخل الاسم", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                if (string.IsNullOrWhiteSpace(_tbPhone.Text))
                {
                    MessageBox.Show("أدخل التليفون", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                Result = new Driver
                {
                    Id = _editing?.Id ?? 0,
                    Name = _tbName.Text.Trim(),
                    Phone = _tbPhone.Text.Trim(),
                    IsActive = true
                };
                DialogResult = true; Close();
            };

            Grid.SetColumn(cancelBtn, 0); btnGrid.Children.Add(cancelBtn);
            Grid.SetColumn(saveBtn, 2); btnGrid.Children.Add(saveBtn);
            root.Children.Add(btnGrid);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }

        TextBox MakeTB(string val) => new()
        {
            Text = val,
            Background = B("#0f1526"),
            Foreground = B("#ffffff"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 9, 10, 9),
            FontSize = 13,
            CaretBrush = B("#06d6a0")
        };
    }
}