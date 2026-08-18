using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    public class Driver
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public string Display => $"🛵 {Name}";
    }
}
