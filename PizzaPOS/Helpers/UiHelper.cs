using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PizzaPOS.Helpers
{
    public static class UiHelper
    {
        public static SolidColorBrush B(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        public static TextBox MakeTB(string val, string caretColor = "#FF6B35") => new()
        {
            Text = val,
            Background = B("#0f1a2e"),
            Foreground = B("#eef0f2"),
            BorderBrush = B("#1e2d4a"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10),
            FontSize = 13,
            CaretBrush = B(caretColor),
            SelectionBrush = B(caretColor)
        };

        public static TextBlock FieldLabel(string t) => new()
        {
            Text = t,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = B("#4a6080")
        };

        public static Border MakeStatCard(string label, TextBlock valueTxt, string accent, string bg)
        {
            var card = new Border
            {
                Background = B(bg),
                BorderBrush = B(accent),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            card.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(accent),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.15
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = B(accent),
                Margin = new Thickness(0, 0, 0, 2)
            });
            valueTxt.Text = "\u2014";
            sp.Children.Add(valueTxt);
            card.Child = sp;
            return card;
        }

        public static Button MakeBtn(string text, string bg, Brush fg, Action click,
            double paddingV = 13, double fontSize = 13, double margin = 0,
            string cornerRadius = "10", Brush? borderBrush = null)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(double.Parse(cornerRadius)));
            f.SetBinding(Border.PaddingProperty,
                new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderBrushProperty,
                new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderThicknessProperty,
                new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp);
            var btn = new Button
            {
                Content = text,
                Background = B(bg),
                Foreground = fg,
                BorderThickness = borderBrush != null ? new Thickness(1) : new Thickness(0),
                BorderBrush = borderBrush,
                Padding = new Thickness(0, paddingV, 0, paddingV),
                FontWeight = FontWeights.Black,
                FontSize = fontSize,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, margin, 0),
                Template = new ControlTemplate(typeof(Button)) { VisualTree = f }
            };
            btn.Click += (_, _) => click();
            return btn;
        }

        public static Button MakeActionButton(string text, string bgHex, Brush fg,
            double margin = 10, double paddingH = 18, double paddingV = 10)
        {
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty,
                new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            f.SetBinding(Border.PaddingProperty,
                new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderBrushProperty,
                new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderThicknessProperty,
                new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp);
            return new Button
            {
                Content = text,
                Background = B(bgHex),
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(paddingH, paddingV, paddingH, paddingV),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, margin, 0),
                Template = new ControlTemplate(typeof(Button)) { VisualTree = f }
            };
        }

        public static DataGrid BuildGrid(
            string rowBg = "#0b1020",
            string altBg = "#080d1a",
            string headerBg = "#0c1530",
            string headerFg = "#ffd166",
            string accent = "#FF6B35",
            string hoverBg = "#111d38",
            string selBg = "#1a2d50",
            string cellFg = "#c8d8f0",
            double rowHeight = 48,
            double headerHeight = 46)
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = Brushes.Transparent,
                RowBackground = B(rowBg),
                AlternatingRowBackground = B(altBg),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = B("#111c35"),
                BorderThickness = new Thickness(0),
                CanUserAddRows = false,
                RowHeight = rowHeight,
                ColumnHeaderHeight = headerHeight,
                FontSize = 13,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            var hs = new Style(typeof(Control));
            hs.Setters.Add(new Setter(Control.BackgroundProperty, B(headerBg)));
            hs.Setters.Add(new Setter(Control.ForegroundProperty, B(headerFg)));
            hs.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            hs.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
            hs.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 0, 16, 0)));
            hs.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 2)));
            hs.Setters.Add(new Setter(Control.BorderBrushProperty, B(accent)));
            dg.ColumnHeaderStyle = hs;

            var rs = new Style(typeof(DataGridRow));
            rs.Setters.Add(new Setter(DataGridRow.ForegroundProperty, B(cellFg)));
            var hov = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B(hoverBg)));
            rs.Triggers.Add(hov);
            var sel = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(DataGridRow.BackgroundProperty, B(selBg)));
            rs.Triggers.Add(sel);
            dg.RowStyle = rs;

            var cs = new Style(typeof(DataGridCell));
            cs.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cs.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(16, 0, 16, 0)));
            cs.Setters.Add(new Setter(DataGridCell.ForegroundProperty, B(cellFg)));
            cs.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));
            var csel = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            csel.Setters.Add(new Setter(DataGridCell.BackgroundProperty, B(selBg)));
            csel.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent));
            cs.Triggers.Add(csel);
            dg.CellStyle = cs;

            return dg;
        }

        public static DataGridTextColumn Col(string header, string binding, double width) =>
            new() { Header = header, Binding = new Binding(binding), Width = width };

        public static DataGridTextColumn ColNum(string header, string binding, double width, string colorHex) =>
            new()
            {
                Header = header,
                Binding = new Binding(binding) { StringFormat = "{0:F2}" },
                Width = width,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B(colorHex)),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            };

        public static DataGridTextColumn ColInt(string header, string binding, double width, string colorHex) =>
            new()
            {
                Header = header,
                Binding = new Binding(binding) { StringFormat = "{0:N0}" },
                Width = width,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.ForegroundProperty, B(colorHex)),
                        new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            };

        public static ScrollViewer Scroll(UIElement el) => new()
        {
            Content = el,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = B("#070b14")
        };

        public static TextBlock SectionLabel(string t) => new()
        {
            Text = t,
            FontSize = 13,
            FontWeight = FontWeights.Black,
            Foreground = B("#ffd166"),
            Margin = new Thickness(0, 0, 0, 6)
        };
    }
}
