using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public string Ingredient { get; set; } = "";
        public string Type { get; set; } = "";
        public double Qty { get; set; }
        public string? Note { get; set; }
        public string CreatedAt { get; set; } = "";
        public string TypeDisplay =>
            Type == "in" ? "📥 وارد" :
            Type == "out" ? "📤 صادر" : "🔄 تسوية";
    }
}
