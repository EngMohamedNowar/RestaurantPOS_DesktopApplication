using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── Order ───────────────────────────────────────
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public int ShiftId { get; set; }
        public int UserId { get; set; }
        public string OrderType { get; set; } = "صالة";
        public string PayMethod { get; set; } = "كاش";
        public double Subtotal { get; set; }
        public double Discount { get; set; }
        public double Tax { get; set; }
        public double ServiceCharge { get; set; }
        public double Total { get; set; }
        public double PaidAmount { get; set; }
        public double Change { get; set; }
        public string? Notes { get; set; }
        public string CreatedAt { get; set; } = "";
        public string Status { get; set; } = "completed";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string DeliveryAddress { get; set; } = "";
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public double DeliveryFee { get; set; }
        public string DeliveryStatus { get; set; } = "";
        public List<OrderItem> Items { get; set; } = new();
    }
}
