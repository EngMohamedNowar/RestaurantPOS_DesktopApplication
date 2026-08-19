using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Notes { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public int LoyaltyPoints { get; set; }
        public int TotalOrders { get; set; }
        public double TotalSpent { get; set; }
        public string Display => $"{Name} — {Phone}";
        public string LoyaltyTier => TotalSpent >= 5000 ? "💎 ماسي" :
                                     TotalSpent >= 2000 ? "🥇 ذهبي" :
                                     TotalSpent >= 1000 ? "🥈 فضي" : "🥉 برونزي";
    }
}
