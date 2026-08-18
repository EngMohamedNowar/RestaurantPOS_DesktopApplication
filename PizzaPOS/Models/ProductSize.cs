using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── ProductSize ─────────────────────────────────
    public class ProductSize
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public double ExtraPrice { get; set; }
        public int SortOrder { get; set; }
    }

}
