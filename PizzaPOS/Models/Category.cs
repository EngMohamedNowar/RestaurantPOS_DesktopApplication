using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── Category ────────────────────────────────────
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "🍕";
        public int SortOrder { get; set; }
    }
}
