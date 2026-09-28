// ProfitMarginDialog.cs
// Dialog for editing the target profit margin.
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
    public class ProfitMarginDialog : Window
    {
        public double MarginValue { get; private set; } = 50;
        TextBox _tbMargin = null!;

        public ProfitMarginDialog()
        {
            Title = "💰 تحديد هامش الربح";
            Width = 380; Height = 280;
            Background = UiHelper.B("#0a0a14");
            Foreground = Brushes.White;
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;

            var db = new AppDbContext();
            string saved = db.GetSetting("ProfitMargin", "50");
            MarginValue = double.TryParse(saved, out var m) ? m : 50;

            var root = new StackPanel { Margin = new Thickness(20) };

            root.Children.Add(new TextBlock
            {
                Text = "📊 تحديد هامش الربح",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#ffd166"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            });

            root.Children.Add(new TextBlock
            {
                Text = "السعر = التكلفة × (1 + هامش الربح / 100)",
                FontSize = 11,
                Foreground = UiHelper.B("#b0c4de"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var lbl = UiHelper.FieldLabel("هامش الربح (%)");
            lbl.Margin = new Thickness(0, 0, 0, 4);
            root.Children.Add(lbl);

            _tbMargin = UiHelper.MakeTB(MarginValue.ToString("F0"));
            _tbMargin.Width = 120;
            _tbMargin.HorizontalAlignment = HorizontalAlignment.Left;
            root.Children.Add(_tbMargin);

            root.Children.Add(new TextBlock
            {
                Text = "مثال: تكلفة 30ج + هامش 50% = سعر 45ج",
                FontSize = 10,
                Foreground = UiHelper.B("#7a8ba8"),
                Margin = new Thickness(0, 8, 0, 16)
            });

            var btnBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var saveBtn = UiHelper.MakeBtn("✅ تحديث الأسعار", "#06d6a0", UiHelper.B("#0a0a14"), () =>
            {
                if (double.TryParse(_tbMargin.Text, out var val) && val >= 0 && val <= 500)
                {
                    MarginValue = val;
                    db.SetSetting("ProfitMargin", val.ToString("F0"));
                    DialogResult = true;
                }
                else
                    MessageBox.Show("أدخل رقم صحيح (0 - 500)", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }, paddingV: 10, margin: 6);

            var cancelBtn = UiHelper.MakeBtn("❌ إلغاء", "#1a2640", UiHelper.B("#b0c4de"), () => DialogResult = false,
                paddingV: 10, margin: 6);

            btnBar.Children.Add(saveBtn);
            btnBar.Children.Add(cancelBtn);
            root.Children.Add(btnBar);

            Content = root;
        }
    }

    // ══════════════════════════════════════════════════
    //  AddCategoryDialog
    // ══════════════════════════════════════════════════
}
