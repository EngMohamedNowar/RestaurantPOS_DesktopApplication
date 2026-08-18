// ══════════════════════════════════════════════
// Dialog إضافة / تعديل منتج
// ══════════════════════════════════════════════
using PizzaPOS.Data;
using PizzaPOS.Models;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

public class ProductEditDialog : Window
{
    public Product? Result { get; private set; }
    readonly AppDbContext _db;
    readonly bool _isEdit;

    TextBox _nameBox = null!;
    TextBox _priceBox = null!;
    TextBox _costBox = null!;
    TextBox _iconBox = null!;
    ComboBox _catBox = null!;

    Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    SolidColorBrush B(string hex) => new(C(hex));

    public ProductEditDialog(Product? product, AppDbContext db)
    {
        _db = db;
        _isEdit = product != null;
        Title = _isEdit ? "✏️ تعديل منتج" : "➕ إضافة منتج";
        Width = 380; Height = 420;
        Background = new SolidColorBrush(Color.FromRgb(26, 26, 46));
        FlowDirection = FlowDirection.RightToLeft;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = new FontFamily("Tahoma");
        ResizeMode = ResizeMode.NoResize;
        BuildUI(product);
    }

    void BuildUI(Product? p)
    {
        var sp = new StackPanel { Margin = new Thickness(24) };

        // Title
        sp.Children.Add(new TextBlock
        {
            Text = _isEdit ? "✏️ تعديل منتج" : "➕ منتج جديد",
            FontSize = 18,
            FontWeight = FontWeights.Black,
            Foreground = B("#FF6B35"),
            Margin = new Thickness(0, 0, 0, 20)
        });

        TextBox MakeTB(string val) => new()
        {
            Text = val,
            Background = B("#16213e"),
            Foreground = B("#eef0f2"),
            BorderBrush = B("#2a3a5c"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 13,
            CaretBrush = B("#eef0f2"),
            Margin = new Thickness(0, 0, 0, 12)
        };

        TextBlock MakeLbl(string t) => new()
        {
            Text = t,
            Foreground = B("#8892a4"),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 4)
        };

        // الاسم
        sp.Children.Add(MakeLbl("اسم المنتج"));
        _nameBox = MakeTB(p?.Name ?? "");
        sp.Children.Add(_nameBox);

        // الفئة
        sp.Children.Add(MakeLbl("الفئة"));
        _catBox = new ComboBox
        {
            Background = B("#16213e"),
            Foreground = B("#eef0f2"),
            BorderBrush = B("#2a3a5c"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        };
        foreach (var cat in _db.GetCategories())
            _catBox.Items.Add(new ComboBoxItem { Content = cat.Name, Tag = cat.Id });
        if (p != null)
            foreach (ComboBoxItem item in _catBox.Items)
                if ((int)item.Tag == p.CategoryId) { _catBox.SelectedItem = item; break; }
        if (_catBox.SelectedIndex < 0 && _catBox.Items.Count > 0)
            _catBox.SelectedIndex = 0;
        sp.Children.Add(_catBox);

        // السعر والتكلفة
        var priceGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        priceGrid.ColumnDefinitions.Add(new ColumnDefinition());
        priceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        priceGrid.ColumnDefinitions.Add(new ColumnDefinition());

        var pricePanel = new StackPanel();
        pricePanel.Children.Add(MakeLbl("السعر (ج)"));
        _priceBox = MakeTB(p?.Price.ToString("F2") ?? "0");
        _priceBox.Margin = new Thickness(0);
        pricePanel.Children.Add(_priceBox);

        var costPanel = new StackPanel();
        costPanel.Children.Add(MakeLbl("التكلفة (ج)"));
        _costBox = MakeTB(p?.Cost.ToString("F2") ?? "0");
        _costBox.Margin = new Thickness(0);
        costPanel.Children.Add(_costBox);

        Grid.SetColumn(pricePanel, 0); priceGrid.Children.Add(pricePanel);
        Grid.SetColumn(costPanel, 2); priceGrid.Children.Add(costPanel);
        sp.Children.Add(priceGrid);

        // الأيقونة
        sp.Children.Add(MakeLbl("الأيقونة (emoji)"));
        _iconBox = MakeTB(p?.Icon ?? "🍕");
        sp.Children.Add(_iconBox);

        // Buttons
        var btnGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

        var cancelBtn = new Button
        {
            Content = "إلغاء",
            Background = B("#2a3a5c"),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0, 12, 0, 12),
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };

        var saveBtn = new Button
        {
            Content = _isEdit ? "💾 حفظ" : "➕ إضافة",
            Background = B("#06d6a0"),
            Foreground = B("#0f0f1a"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0, 12, 0, 12),
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        saveBtn.Click += Save_Click;

        Grid.SetColumn(cancelBtn, 0); btnGrid.Children.Add(cancelBtn);
        Grid.SetColumn(saveBtn, 2); btnGrid.Children.Add(saveBtn);
        sp.Children.Add(btnGrid);

        // Scroll
        var scroll = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Content = scroll;
    }

    void Save_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            MessageBox.Show("اكتب اسم المنتج", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!double.TryParse(_priceBox.Text, out double price) || price < 0)
        {
            MessageBox.Show("السعر غلط", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        double.TryParse(_costBox.Text, out double cost);

        int catId = _catBox.SelectedItem is ComboBoxItem ci ? (int)ci.Tag : 1;

        Result = new Product
        {
            Id = 0, // سيتم تحديده من الـ DB
            Name = _nameBox.Text.Trim(),
            CategoryId = catId,
            Price = price,
            Cost = cost,
            Icon = string.IsNullOrWhiteSpace(_iconBox.Text) ? "🍕" : _iconBox.Text.Trim(),
            IsActive = true
        };

        // لو تعديل — حافظ على الـ Id
        if (_isEdit && _db is not null)
        {
            // سنحتاج Id من الـ DataGrid — نمرره من المنتج الأصلي
        }

        DialogResult = true;
        Close();
    }
}
