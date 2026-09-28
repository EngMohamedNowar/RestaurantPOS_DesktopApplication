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

        /// <summary>
        /// لو true = المستخدم لازم يغيّر الـ PIN المزروع default قبل ما يدخل الـ POS.
        /// بيتصفّر تلقائياً أول ما يغيّر الـ PIN.
        /// </summary>
        public bool MustChangePin { get; set; }

        public bool IsAdmin => Role == "admin";
        public bool IsManager => Role is "admin" or "manager";
    }
}
