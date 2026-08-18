using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaPOS.Models
{
    // ── User ────────────────────────────────────────
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string PinHash { get; set; } = "";
        public string Role { get; set; } = "cashier";
        public bool IsActive { get; set; } = true;

        public bool IsAdmin => Role == "admin";
        public bool IsManager => Role is "admin" or "manager";
    }
}
