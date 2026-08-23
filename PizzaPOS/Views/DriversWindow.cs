// Views/DriversWindow.cs
using PizzaPOS.Data;
using PizzaPOS.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using PizzaPOS.Helpers;

namespace PizzaPOS.Views
{
    public class DriversWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<Driver> _drivers = new();
        DataGrid _dg = null!;
        TextBox _tbSearch = null!;
        TextBlock _totalCountTxt = null!;

        public DriversWindow()
        {
            Title = "إدارة السائقين";
            Width = 620; Height = 520;
            MinWidth = 500;
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
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ══ Header ══════════════════════════════════════════════════════
            var header = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new Border
            {
                Background = UiHelper.B("#06d6a0"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 14, 0)
            };
            icon.Child = new TextBlock
            {
                Text = "🛵",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hSp.Children.Add(icon);

            var titleSp = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(new TextBlock
            {
                Text = "إدارة السائقين",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#eef0f2")
            });
            titleSp.Children.Add(new TextBlock
            {
                Text = "إضافة وتعديل وحذف سائقين",
                FontSize = 12,
                Foreground = UiHelper.B("#5a6a80"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            hSp.Children.Add(titleSp);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ Stats + Search Bar ══════════════════════════════════════════
            var toolbar = new Border
            {
                Background = UiHelper.B("#0a0f1c"),
                Padding = new Thickness(20, 10, 20, 10)
            };
            var toolbarSp = new StackPanel { Orientation = Orientation.Horizontal };

            var statsCard = new Border
            {
                Background = UiHelper.B("#0d1a2a"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 12, 0)
            };
            var statsSp = new StackPanel { Orientation = Orientation.Horizontal };
            statsSp.Children.Add(new TextBlock
            {
                Text = "السائقين: ",
                FontSize = 13,
                Foreground = UiHelper.B("#5a6a80"),
                VerticalAlignment = VerticalAlignment.Center
            });
            _totalCountTxt = new TextBlock
            {
                Text = "0",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#06d6a0"),
                VerticalAlignment = VerticalAlignment.Center
            };
            statsSp.Children.Add(_totalCountTxt);
            statsCard.Child = statsSp;
            toolbarSp.Children.Add(statsCard);

            var searchBorder = new Border
            {
                Background = UiHelper.B("#0d1a2a"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            _tbSearch = new TextBox
            {
                Width = 220,
                Height = 34,
                Background = Brushes.Transparent,
                Foreground = UiHelper.B("#eef0f2"),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft,
                Tag = "بحث بالاسم أو التليفون..."
            };
            _tbSearch.GotFocus += (_, __) =>
            {
                if (_tbSearch.Text == "بحث بالاسم أو التليفون...")
                    _tbSearch.Text = "";
            };
            _tbSearch.LostFocus += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(_tbSearch.Text))
                    _tbSearch.Text = "بحث بالاسم أو التليفون...";
            };
            _tbSearch.TextChanged += (_, __) => ApplyFilter();
            searchBorder.Child = _tbSearch;
            toolbarSp.Children.Add(searchBorder);

            toolbar.Child = toolbarSp;
            Grid.SetRow(toolbar, 1);
            root.Children.Add(toolbar);

            // ══ DataGrid ══════════════════════════════════════════════════
            _dg = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = Brushes.Transparent,
                RowBackground = UiHelper.B("#0d1525"),
                AlternatingRowBackground = UiHelper.B("#0a0f1c"),
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                BorderThickness = new Thickness(0),
                CanUserAddRows = false,
                RowHeight = 42,
                ColumnHeaderHeight = 40,
                FontSize = 13,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                ItemsSource = _dg != null ? _drivers : _drivers
            };

            var hs = new Style(typeof(Control));
            hs.Setters.Add(new Setter(Control.BackgroundProperty, UiHelper.B("#0d1525")));
            hs.Setters.Add(new Setter(Control.ForegroundProperty, UiHelper.B("#06d6a0")));
            hs.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            hs.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
            hs.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 0, 14, 0)));
            hs.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            hs.Setters.Add(new Setter(Control.BorderBrushProperty, UiHelper.B("#1a2d50")));
            _dg.ColumnHeaderStyle = hs;

            var rs = new Style(typeof(DataGridRow));
            rs.Setters.Add(new Setter(DataGridRow.ForegroundProperty, UiHelper.B("#eef0f2")));
            rs.Setters.Add(new Setter(DataGridRow.CursorProperty, System.Windows.Input.Cursors.Hand));
            var hov = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(DataGridRow.BackgroundProperty, UiHelper.B("#12192e")));
            rs.Triggers.Add(hov);
            var sel = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(DataGridRow.BackgroundProperty, UiHelper.B("#1a2640")));
            rs.Triggers.Add(sel);
            _dg.RowStyle = rs;

            var cs = new Style(typeof(DataGridCell));
            cs.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cs.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(14, 0, 14, 0)));
            cs.Setters.Add(new Setter(DataGridCell.ForegroundProperty, UiHelper.B("#eef0f2")));
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
                Width = 160
            });
            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الحالة",
                Binding = new Binding("IsActive"),
                Width = 80
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
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            // ══ Bottom Action Bar ══════════════════════════════════════════
            var bar = new Border
            {
                Background = UiHelper.B("#0d1525"),
                BorderBrush = UiHelper.B("#1a2d50"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 14, 20, 14)
            };
            var barSp = new StackPanel { Orientation = Orientation.Horizontal };

            var addBtn = UiHelper.MakeBtn("إضافة سائق", "#06d6a0", UiHelper.B("#0a0a14"), AddDriver, 10, 13);
            addBtn.MinWidth = 120;
            var editBtn = UiHelper.MakeBtn("تعديل", "#ffd166", UiHelper.B("#0a0a14"), EditDriver, 10, 13);
            editBtn.MinWidth = 90;
            var deleteBtn = UiHelper.MakeBtn("حذف", "#E63946", Brushes.White, DeleteDriver, 10, 13);
            deleteBtn.MinWidth = 90;

            barSp.Children.Add(addBtn);
            barSp.Children.Add(new Spacer());
            barSp.Children.Add(deleteBtn);
            barSp.Children.Add(new Spacer());
            barSp.Children.Add(editBtn);
            bar.Child = barSp;
            Grid.SetRow(bar, 3);
            root.Children.Add(bar);

            Content = root;
            Load();
            _dg.Focus();
        }

        void ApplyFilter()
        {
            string q = (_tbSearch.Text == "بحث بالاسم أو التليفون..." ? "" : _tbSearch.Text).Trim().ToLower();
            var all = _db.GetDrivers();

            if (!string.IsNullOrEmpty(q))
                all = all.Where(d => d.Name.ToLower().Contains(q) || d.Phone.ToLower().Contains(q)).ToList();

            _drivers.Clear();
            foreach (var d in all) _drivers.Add(d);
            _totalCountTxt.Text = _drivers.Count.ToString();
        }

        void Load() => ApplyFilter();

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
    }

    // ── Driver Edit Dialog ───────────────────────────
    public class DriverEditDialog : Window
    {
        public Driver? Result { get; private set; }
        readonly Driver? _editing;
        TextBox _tbName = null!;
        TextBox _tbPhone = null!;

        public DriverEditDialog(Driver? editing)
        {
            _editing = editing;
            Title = editing == null ? "سائق جديد" : "تعديل سائق";
            Width = 400;
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
            var root = new StackPanel { Margin = new Thickness(28) };

            var headerSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 24) };
            var icon = new Border
            {
                Background = UiHelper.B("#06d6a0"),
                CornerRadius = new CornerRadius(8),
                Width = 34,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0)
            };
            icon.Child = new TextBlock
            {
                Text = _editing == null ? "➕" : "✏️",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerSp.Children.Add(icon);
            headerSp.Children.Add(new TextBlock
            {
                Text = _editing == null ? "إضافة سائق جديد" : "تعديل بيانات السائق",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#eef0f2"),
                VerticalAlignment = VerticalAlignment.Center
            });
            root.Children.Add(headerSp);

            root.Children.Add(UiHelper.FieldLabel("الاسم *"));
            _tbName = UiHelper.MakeTB(_editing?.Name ?? "");
            _tbName.Margin = new Thickness(0, 0, 0, 16);
            root.Children.Add(_tbName);

            root.Children.Add(UiHelper.FieldLabel("التليفون *"));
            _tbPhone = UiHelper.MakeTB(_editing?.Phone ?? "");
            _tbPhone.Margin = new Thickness(0, 0, 0, 28);
            root.Children.Add(_tbPhone);

            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = UiHelper.MakeBtn("إلغاء", "#2a2a3c", UiHelper.B("#8892a4"), () => { DialogResult = false; Close(); }, 10, 13);
            var saveBtn = UiHelper.MakeBtn(_editing == null ? "إضافة" : "حفظ", "#06d6a0", UiHelper.B("#0a0a14"), () =>
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
            }, 10, 13);

            Grid.SetColumn(cancelBtn, 0); btnGrid.Children.Add(cancelBtn);
            Grid.SetColumn(saveBtn, 2); btnGrid.Children.Add(saveBtn);
            root.Children.Add(btnGrid);

            Content = root;
            Loaded += (_, _) => _tbName.Focus();
        }
    }

    class Spacer : FrameworkElement
    {
        protected override Size MeasureOverride(Size constraint) { return new Size(10, 0); }
        protected override Size ArrangeOverride(Size arrangeBounds) { return arrangeBounds; }
    }
}
