using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── Product ─────────────────────────────────────
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public double Cost { get; set; }
        public string Icon { get; set; } = "🍕";
        public bool IsActive { get; set; } = true;

        // ── Card Colors ─────────────────────────────
        public string CardColor { get; set; } = "#12192e";
        public string CardAccent { get; set; } = "#1e2d4a";
        public string CardIconBg { get; set; } = "#0f1526";
        public string CardPriceBg { get; set; } = "#0f1f14";
    }
}
