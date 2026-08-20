namespace PizzaPOS.Models
{
    public class Offer
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public double DiscountPercent { get; set; }
        public string PromoCode { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public string DiscountDisplay => DiscountPercent > 0 ? $"-{DiscountPercent:F0}%" : "";
        public string StatusDisplay => IsActive ? "نشط" : "منتهي";
    }
}
