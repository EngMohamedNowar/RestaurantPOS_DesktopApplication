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
        public string Display => $"{Name} — {Phone}";
    }
}
