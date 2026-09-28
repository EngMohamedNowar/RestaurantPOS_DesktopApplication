// StockStatusFgConverter.cs
// Binding converter for the low-stock badge foreground.
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
    public class StockStatusFgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            var s = v?.ToString() ?? "";
            return s.Contains("منخفض") ? UiHelper.B("#ffffff") : UiHelper.B("#0a0a14");
        }
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    // ══════════════════════════════════════════════════
    //  StockStatusBgConverter  –  badge background
    // ══════════════════════════════════════════════════
}
