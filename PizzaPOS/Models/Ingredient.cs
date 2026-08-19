using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── Inventory ───────────────────────────────────
    public class Ingredient
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public double Stock { get; set; }
        public double MinStock { get; set; }
        public double CostPerUnit { get; set; }
        public bool IsLow => Stock <= MinStock;
        public string StockStatus => IsLow ? "⚠ منخفض" : "✅ كافي";

        // ── مهم: ده اللي بيخلي الـ ComboBox يعرض الاسم صح ──
        // بدون التعديل ده، الـ ComboBox بيعرض PizzaPOS.Models.Ingredient
        // في صندوق الاختيار المغلق لأن مفيش SelectionBoxItemTemplate صريح
        public override string ToString() => Name;
    }
}