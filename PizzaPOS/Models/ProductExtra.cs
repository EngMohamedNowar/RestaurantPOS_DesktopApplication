using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── ProductExtra ────────────────────────────────
    public class ProductExtra
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public bool IsSelected { get; set; }
    }
}
