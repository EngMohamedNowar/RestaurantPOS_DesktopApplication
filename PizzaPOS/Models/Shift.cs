using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── Shift ───────────────────────────────────────
    public class Shift
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public double OpeningCash { get; set; }
        public double? ClosingCash { get; set; }
        public double ExpectedCash { get; set; }
        public double Difference { get; set; }
        public string OpenedAt { get; set; } = "";
        public string? ClosedAt { get; set; }
        public string Status { get; set; } = "open";
        public double TotalSales { get; set; }
        public int OrderCount { get; set; }
    }

}
