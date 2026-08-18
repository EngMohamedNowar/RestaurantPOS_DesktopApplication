// Views/UsersWindow.cs
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
    public class UsersWindow : Window
    {
        readonly AppDbContext _db = new();
        readonly ObservableCollection<User> _users = new();
        DataGrid _dg = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public UsersWindow()
        {
            Title = "👥 إدارة المستخدمين";
            Width = 720; Height = 520;
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

            // ══ Header ══
            var header = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 14, 18, 14)
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition());
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleSp = new StackPanel { Orientation = Orientation.Horizontal };
            var iconB = new Border
            {
                Background = B("#a78bfa"),
                CornerRadius = new CornerRadius(10),
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 0, 12, 0)
            };
            iconB.Child = new TextBlock
            {
                Text = "👥",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(iconB);
            titleSp.Children.Add(new TextBlock
            {
                Text = "إدارة المستخدمين",
                FontSize = 20,
                FontWeight = FontWeights.Black,
                Foreground = B("#eef0f2"),
                VerticalAlignment = VerticalAlignment.Center
            });

            var countBorder = new Border
            {
                Background = B("#1a2640"),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 4, 12, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            var countTxt = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = B("#a78bfa")
            };
            void UpdateCount() => countTxt.Text = $"{_users.Count} مستخدم";
            _users.CollectionChanged += (_, _) => UpdateCount();
            countBorder.Child = countTxt;

            Grid.SetColumn(titleSp, 0);
            Grid.SetColumn(countBorder, 1);
            hGrid.Children.Add(titleSp);
            hGrid.Children.Add(countBorder);
            header.Child = hGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ══ DataGrid ══
            _dg = BuildGrid();
            _dg.ItemsSource = _users;

            // Role column with colored badge
            var roleCol = new DataGridTemplateColumn
            {
                Header = "الدور",
                Width = 100
            };
            var roleTpl = new DataTemplate();
            var roleFactory = new FrameworkElementFactory(typeof(Border));
            roleFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            roleFactory.SetValue(Border.PaddingProperty, new Thickness(8, 2, 8, 2));
            roleFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            var roleBinding = new Binding("Role");
            // نستخدم TextBlock عادي
            var roleTxt = new FrameworkElementFactory(typeof(TextBlock));
            roleTxt.SetBinding(TextBlock.TextProperty, new Binding("Role")
            {
                Converter = new RoleDisplayConverter()
            });
            roleTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            roleTxt.SetValue(TextBlock.FontSizeProperty, 11.0);
            roleTxt.SetValue(TextBlock.ForegroundProperty,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0a0a14")));
            roleFactory.AppendChild(roleTxt);
            roleTpl.VisualTree = roleFactory;
            roleCol.CellTemplate = roleTpl;
            _dg.Columns.Add(roleCol);

            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "اسم المستخدم",
                Binding = new Binding("Username"),
                Width = 130
            });
            _dg.Columns.Add(new DataGridTextColumn
            {
                Header = "الاسم الكامل",
                Binding = new Binding("FullName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            // Status column
            var statusCol = new DataGridTemplateColumn
            {
                Header = "الحالة",
                Width = 90
            };
            var statusTpl = new DataTemplate();
            var statusFactory = new FrameworkElementFactory(typeof(TextBlock));
            statusFactory.SetBinding(TextBlock.TextProperty,
                new Binding("IsActive") { Converter = new StatusConverter() });
            statusFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            statusTpl.VisualTree = statusFactory;
            statusCol.CellTemplate = statusTpl;
            _dg.Columns.Add(statusCol);

            _dg.MouseDoubleClick += (_, _) =>
            {
                if (_dg.SelectedItem is User) EditUser();
            };

            var scroll = new ScrollViewer
            {
                Content = _dg,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = B("#0a0a14")
            };
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // ══ Bottom Bar ══
            var bar = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(14, 10, 14, 10)
            };
            var barSp = new StackPanel { Orientation = Orientation.Horizontal };

            var addBtn = MakeBtn("➕ إضافة مستخدم", "#06d6a0", B("#0a0a14"));
            var editBtn = MakeBtn("✏️ تعديل", "#ffd166", B("#0a0a14"));
            var toggleBtn = MakeBtn("🔄 تفعيل/تعطيل", "#a78bfa", System.Windows.Media.Brushes.White);
            var pinBtn = MakeBtn("🔑 تغيير PIN", "#1e2d4a", B("#eef0f2"));

            addBtn.Click += (_, _) => AddUser();
            editBtn.Click += (_, _) => EditUser();
            toggleBtn.Click += (_, _) => ToggleUser();
            pinBtn.Click += (_, _) => ChangePin();

            barSp.Children.Add(addBtn);
            barSp.Children.Add(editBtn);
            barSp.Children.Add(toggleBtn);
            barSp.Children.Add(pinBtn);
            bar.Child = barSp;
            Grid.SetRow(bar, 2);
            root.Children.Add(bar);

            Content = root;
            Load();
            UpdateCount();
        }

        // ── CRUD ─────────────────────────────────────
        void Load()
        {
            _users.Clear();
            foreach (var u in _db.GetUsers()) _users.Add(u);
        }

        void AddUser()
        {
            var dlg = new UserEditDialog(null) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveUser(dlg.ResultUser!, dlg.ResultPin);
            Load();
        }

        void EditUser()
        {
            if (_dg.SelectedItem is not User u)
            {
                MessageBox.Show("اختر مستخدم أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            var dlg = new UserEditDialog(u) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveUser(dlg.ResultUser!,
                string.IsNullOrEmpty(dlg.ResultPin) ? null : dlg.ResultPin);
            Load();
        }

        void ToggleUser()
        {
            if (_dg.SelectedItem is not User u)
            {
                MessageBox.Show("اختر مستخدم أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (u.Role == "admin" && u.IsActive)
            {
                MessageBox.Show("لا يمكن تعطيل حساب الأدمن", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            var action = u.IsActive ? "تعطيل" : "تفعيل";
            if (MessageBox.Show($"{action} '{u.FullName}'؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            u.IsActive = !u.IsActive;
            _db.SaveUser(u);
            Load();
        }

        void ChangePin()
        {
            if (_dg.SelectedItem is not User u)
            {
                MessageBox.Show("اختر مستخدم أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            var dlg = new ChangePinDialog(u.FullName) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _db.SaveUser(u, dlg.NewPin);
            MessageBox.Show($"✅ تم تغيير PIN للمستخدم '{u.FullName}'", "تم",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── DataGrid ────────────────────────────────
        DataGrid BuildGrid()
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = System.Windows.Media.Brushes.Transparent,
                RowBackground = B("#0d1525"),
                AlternatingRowBackground = B("#0a0f1c"),
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                BorderThickness = new Thickness(0),
                CanUserAddRows = false,
                RowHeight = 42,
                ColumnHeaderHeight = 42,
                FontSize = 13,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            var hs = new Style(typeof(DataGridColumnHeader));
            hs.Setters.Add(new Setter(Control.BackgroundProperty, B("#0f1526")));
            hs.Setters.Add(new Setter(Control.ForegroundProperty, B("#ffd166")));
            hs.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            hs.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 0, 14, 0)));
            hs.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 2)));
            hs.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, B("#a78bfa")));
            dg.ColumnHeaderStyle = hs;

            var rs = new Style(typeof(DataGridRow));
            rs.Setters.Add(new Setter(DataGridRow.ForegroundProperty, B("#eef0f2")));
            var hover = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B("#12192e")));
            rs.Triggers.Add(hover);
            var sel = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B("#1a2640")));
            rs.Triggers.Add(sel);
            dg.RowStyle = rs;

            var cs = new Style(typeof(DataGridCell));
            cs.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cs.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(14, 0, 14, 0)));
            cs.Setters.Add(new Setter(DataGridCell.ForegroundProperty, B("#eef0f2")));
            cs.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));
            var csel = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            csel.Setters.Add(new Setter(DataGridCell.BackgroundProperty, B("#1a2640")));
            csel.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, System.Windows.Media.Brushes.Transparent));
            cs.Triggers.Add(csel);
            dg.CellStyle = cs;

            return dg;
        }

        Button MakeBtn(string text, string bg, System.Windows.Media.Brush fg)
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

            return new Button
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
        }
    }

    // ── Converters ───────────────────────────────────
    public class RoleDisplayConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
            => v?.ToString() == "admin" ? "👑 أدمن" : "💼 كاشير";
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    public class StatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
            => v is true ? "✅ مفعّل" : "❌ معطّل";
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    // ══════════════════════════════════════════════════
    //  UserEditDialog
    // ══════════════════════════════════════════════════
    public class UserEditDialog : Window
    {
        public User? ResultUser { get; private set; }
        public string? ResultPin { get; private set; }

        readonly User? _editing;
        TextBox _tbUsername = null!;
        TextBox _tbFullName = null!;
        TextBox _tbPin = null!;
        ComboBox _cbRole = null!;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public UserEditDialog(User? editing)
        {
            _editing = editing;
            Title = editing == null ? "➕ مستخدم جديد" : "✏️ تعديل مستخدم";
            Width = 420;
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
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Header ──
            var header = new Border
            {
                Background = B("#0f1526"),
                BorderBrush = _editing == null ? B("#06d6a0") : B("#ffd166"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var hSp = new StackPanel { Orientation = Orientation.Horizontal };
            var hIcon = new Border
            {
                Background = _editing == null ? B("#06d6a0") : B("#ffd166"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 12, 0)
            };
            hIcon.Child = new TextBlock
            {
                Text = _editing == null ? "➕" : "✏️",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var hInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hInfo.Children.Add(new TextBlock
            {
                Text = _editing == null ? "إضافة مستخدم جديد" : "تعديل بيانات المستخدم",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = _editing == null ? B("#06d6a0") : B("#ffd166")
            });
            hInfo.Children.Add(new TextBlock
            {
                Text = _editing == null
                               ? "أدخل بيانات المستخدم الجديد"
                               : $"تعديل: {_editing.FullName}",
                FontSize = 11,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            hSp.Children.Add(hIcon);
            hSp.Children.Add(hInfo);
            header.Child = hSp;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Fields ──
            var fields = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            // اسم المستخدم
            fields.Children.Add(FieldLabel("اسم المستخدم (للدخول) *"));
            _tbUsername = MakeTB(_editing?.Username ?? "");
            _tbUsername.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbUsername);

            // الاسم الكامل
            fields.Children.Add(FieldLabel("الاسم الكامل *"));
            _tbFullName = MakeTB(_editing?.FullName ?? "");
            _tbFullName.Margin = new Thickness(0, 4, 0, 14);
            fields.Children.Add(_tbFullName);

            // الدور
            fields.Children.Add(FieldLabel("الدور *"));
            _cbRole = new ComboBox
            {
                Background = B("#0f1526"),
                Foreground = B("#eef0f2"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 9, 10, 9),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 14),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#0f1526")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#eef0f2")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(12, 9, 12, 9)));
            var hov = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#1a2640")));
            itemStyle.Triggers.Add(hov);
            var selT = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selT.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, B("#a78bfa")));
            selT.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, B("#0a0a14")));
            itemStyle.Triggers.Add(selT);
            _cbRole.ItemContainerStyle = itemStyle;
            _cbRole.Resources.Add(SystemColors.WindowBrushKey, B("#0f1526"));
            _cbRole.Resources.Add(SystemColors.HighlightBrushKey, B("#a78bfa"));
            _cbRole.Resources.Add(SystemColors.ControlBrushKey, B("#0f1526"));

            var cashierItem = new ComboBoxItem
            {
                Content = "💼 كاشير",
                Tag = "cashier",
                Background = B("#0f1526"),
                Foreground = B("#eef0f2")
            };
            var adminItem = new ComboBoxItem
            {
                Content = "👑 أدمن",
                Tag = "admin",
                Background = B("#0f1526"),
                Foreground = B("#eef0f2")
            };
            _cbRole.Items.Add(cashierItem);
            _cbRole.Items.Add(adminItem);
            _cbRole.SelectedItem = _editing?.Role == "admin" ? adminItem : cashierItem;
            fields.Children.Add(_cbRole);

            // PIN
            var pinLabel = _editing == null
                ? "PIN (4 أرقام) *"
                : "PIN جديد (اتركه فاضي لو مش عايز تغيره)";
            fields.Children.Add(FieldLabel(pinLabel));
            _tbPin = MakeTB("");
            _tbPin.MaxLength = 4;
            _tbPin.Margin = new Thickness(0, 4, 0, 0);
            fields.Children.Add(_tbPin);

            // تحذير PIN
            fields.Children.Add(new TextBlock
            {
                Text = "⚠️ PIN يجب أن يكون 4 أرقام فقط",
                Foreground = B("#4a6080"),
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0)
            });

            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            // ── Buttons ──
            var btnBar = new Border
            {
                Background = B("#0d1220"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 16)
            };
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelBtn = MakeActionBtn("إلغاء", "#12192e", B("#8892a4"),
                () => { DialogResult = false; Close(); });
            cancelBtn.BorderBrush = B("#1e2d4a");
            cancelBtn.BorderThickness = new Thickness(1);

            var saveColor = _editing == null ? "#06d6a0" : "#ffd166";
            var saveLabel = _editing == null ? "➕ إضافة" : "💾 حفظ";
            var saveBtn = MakeActionBtn(saveLabel, saveColor, B("#0a0a14"), Save);

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(saveBtn, 2);
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(saveBtn);
            btnBar.Child = btnGrid;
            Grid.SetRow(btnBar, 2);
            root.Children.Add(btnBar);

            Content = root;
            Loaded += (_, _) => _tbUsername.Focus();
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_tbUsername.Text))
            {
                MessageBox.Show("أدخل اسم المستخدم", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbUsername.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(_tbFullName.Text))
            {
                MessageBox.Show("أدخل الاسم الكامل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbFullName.Focus(); return;
            }
            // PIN مطلوب للمستخدم الجديد
            if (_editing == null && _tbPin.Text.Length != 4)
            {
                MessageBox.Show("PIN يجب أن يكون 4 أرقام بالظبط", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbPin.Focus(); return;
            }
            // لو كتب PIN في التعديل لازم يكون 4 أرقام
            if (_editing != null && !string.IsNullOrEmpty(_tbPin.Text)
                && _tbPin.Text.Length != 4)
            {
                MessageBox.Show("PIN يجب أن يكون 4 أرقام بالظبط", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbPin.Focus(); return;
            }
            // تأكد إن PIN أرقام فقط
            if (!string.IsNullOrEmpty(_tbPin.Text) &&
                !_tbPin.Text.All(char.IsDigit))
            {
                MessageBox.Show("PIN يجب أن يحتوي على أرقام فقط", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _tbPin.Focus(); return;
            }

            var role = (_cbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "cashier";

            ResultUser = new User
            {
                Id = _editing?.Id ?? 0,
                Username = _tbUsername.Text.Trim(),
                FullName = _tbFullName.Text.Trim(),
                Role = role,
                IsActive = _editing?.IsActive ?? true
            };
            ResultPin = string.IsNullOrEmpty(_tbPin.Text) ? null : _tbPin.Text;
            DialogResult = true;
            Close();
        }

        TextBlock FieldLabel(string t) => new()
        {
            Text = t,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = B("#4a6080")
        };

        TextBox MakeTB(string val) => new()
        {
            Text = val,
            Background = B("#0f1526"),
            Foreground = B("#eef0f2"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 9, 10, 9),
            FontSize = 13,
            CaretBrush = B("#a78bfa"),
            SelectionBrush = B("#a78bfa")
        };

        Button MakeActionBtn(string text, string bg, SolidColorBrush fg, Action click)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            f.SetBinding(Border.PaddingProperty,
                new Binding("Padding")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderBrushProperty,
                new Binding("BorderBrush")
                { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderThicknessProperty,
                new Binding("BorderThickness")
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
                Padding = new Thickness(0, 13, 0, 13),
                FontWeight = FontWeights.Black,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = tpl
            };
            btn.Click += (_, _) => click();
            return btn;
        }
    }

    // ══════════════════════════════════════════════════
    //  ChangePinDialog
    // ══════════════════════════════════════════════════
    public class ChangePinDialog : Window
    {
        public string NewPin { get; private set; } = "";
        readonly TextBox _tbPin;

        SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public ChangePinDialog(string userName)
        {
            Title = "🔑 تغيير PIN";
            Width = 340;
            SizeToContent = SizeToContent.Height;
            Background = B("#0a0a14");
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;

            var root = new StackPanel { Margin = new Thickness(24) };

            root.Children.Add(new TextBlock
            {
                Text = "🔑 تغيير PIN",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = B("#a78bfa"),
                Margin = new Thickness(0, 0, 0, 6)
            });
            root.Children.Add(new TextBlock
            {
                Text = $"المستخدم: {userName}",
                FontSize = 12,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 16)
            });
            root.Children.Add(new TextBlock
            {
                Text = "PIN الجديد (4 أرقام) *",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 6)
            });

            _tbPin = new TextBox
            {
                Background = B("#0f1526"),
                Foreground = B("#eef0f2"),
                BorderBrush = B("#1e2d4a"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 9, 10, 9),
                FontSize = 20,
                MaxLength = 4,
                TextAlignment = TextAlignment.Center,
                CaretBrush = B("#a78bfa"),
                Margin = new Thickness(0, 0, 0, 20)
            };
            root.Children.Add(_tbPin);

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
                Content = "💾 حفظ",
                Background = B("#a78bfa"),
                Foreground = B("#0a0a14"),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 11, 0, 11),
                FontWeight = FontWeights.Black,
                Template = tpl,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            saveBtn.Click += (_, _) =>
            {
                if (_tbPin.Text.Length != 4 || !_tbPin.Text.All(char.IsDigit))
                {
                    MessageBox.Show("PIN يجب أن يكون 4 أرقام بالظبط", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                NewPin = _tbPin.Text;
                DialogResult = true; Close();
            };

            Grid.SetColumn(cancelBtn, 0); btnGrid.Children.Add(cancelBtn);
            Grid.SetColumn(saveBtn, 2); btnGrid.Children.Add(saveBtn);
            root.Children.Add(btnGrid);

            Content = root;
            Loaded += (_, _) => _tbPin.Focus();
        }
    }
}