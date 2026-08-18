using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    public class LossSummary
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public double Amount { get; set; }
        public string Date { get; set; } = "";
        public string Color { get; set; } = "#E63946";
    }
}
