using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    public class ProductStat
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public int Qty { get; set; }
        public double Sales { get; set; }
        public double Profit { get; set; }
    }
}
