using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>مادة طلع رصيدها سالب بعد خصم أوردر.</summary>
    public class StockShortage
    {
        public int IngredientId { get; set; }
        public string Ingredient { get; set; } = "";
        public string Unit { get; set; } = "";
        /// <summary>الكمية اللي المفروض تخصم.</summary>
        public double Required { get; set; }
        /// <summary>اللي كان متاح فعلاً وقت الخصم.</summary>
        public double Available { get; set; }
        public double Missing => Required - Available;
    }

    /// <summary>نتيجة خصم المخزون عن أوردر — عشان نقدر نبلّغ المستخدم بالعجز
    /// بدل ما نخفيه تحت MAX(0,...) زي الكود القديم.</summary>
    public class StockDeductionResult
    {
        public int DeductedLines { get; set; }
        public List<StockShortage> Shortages { get; set; } = new();
        public bool HasShortage => Shortages.Count > 0;
        public string Summary => HasShortage
            ? $"المخزون 부족 في {Shortages.Count} مادة: "
              + string.Join("، ", Shortages.Select(s => $"{s.Ingredient} (ناقص {s.Missing:0.##} {s.Unit})"))
            : "";
    }
}
