using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    public class ProductIngredient
    {
        public int ProductId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = "";
        public string IngredientUnit { get; set; } = "";
        public double QtyUsed { get; set; }
        public double CostPerUnit { get; set; }
        public double TotalCost => QtyUsed * CostPerUnit;
    }
}
