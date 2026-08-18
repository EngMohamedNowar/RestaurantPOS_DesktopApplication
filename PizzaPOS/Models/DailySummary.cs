using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{

    // ── Report DTOs ─────────────────────────────────
    public class DailySummary
    {
        public string Date { get; set; } = "";
        public double Sales { get; set; }
        public double Cash { get; set; }
        public double Card { get; set; }
        public int Orders { get; set; }
        public double Tax { get; set; }
        public double Discount { get; set; }
        public double ServiceCharge { get; set; }
        public double Profit { get; set; }
        public double Loss { get; set; }
    }

}
