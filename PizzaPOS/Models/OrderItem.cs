using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace PizzaPOS.Models
{
    // ── OrderItem ───────────────────────────────────
    public class OrderItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        void Notify([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "🍕";
        public double Cost { get; set; }

        public string? SizeName { get; set; }
        public double BasePrice { get; set; }
        public double SizeExtraPrice { get; set; }
        public string? ExtrasNote { get; set; }
        public double ExtrasPrice { get; set; }
        public string? ExtrasKey { get; set; }

        public double Price => BasePrice + SizeExtraPrice + ExtrasPrice;
        public double Subtotal => Price * Qty;

        public string SizeAndExtrasDisplay
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(SizeName)) parts.Add(SizeName!);
                if (!string.IsNullOrWhiteSpace(ExtrasNote)) parts.Add(ExtrasNote!);
                return string.Join(" | ", parts);
            }
        }

        int _qty = 1;
        public int Qty
        {
            get => _qty;
            set
            {
                _qty = value;
                Notify();
                Notify(nameof(Subtotal));
                Notify(nameof(Price));
            }
        }
    }

}
