using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace PizzaPOS.Models
{
    // ── Base ────────────────────────────────────────
    public class ObservableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
