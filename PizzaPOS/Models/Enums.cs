namespace PizzaPOS.Models
{
    public static class OrderStatus
    {
        public const string New = "new";
        public const string Kitchen = "kitchen";
        public const string Ready = "ready";
        public const string Delivery = "delivery";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
        public const string Held = "held";
    }

    public static class OrderType
    {
        public const string DineIn = "صالة";
        public const string Takeaway = "تيك أواي";
        public const string Delivery = "ديلفري";
    }

    public static class PayMethod
    {
        public const string Cash = "كاش";
        public const string CashAlt = "نقدي";
        public const string Card = "فيزا/ماستر";
    }

    public static class UserRole
    {
        public const string Admin = "admin";
        public const string Cashier = "cashier";
        public const string Manager = "manager";
    }

    public static class DeliveryStatuses
    {
        public const string Pending = "معلق";
        public const string InTransit = "قيد التوصيل";
        public const string Delivered = "تم التوصيل";
    }

    public static class StockMovementType
    {
        public const string In = "in";
        public const string Out = "out";
        public const string Adjust = "adjust";
    }

    public static class LossTypes
    {
        public const string Operating = "مصروف تشغيلي";
        public const string Spoiled = "خامات تالفة";
        public const string CashShortage = "عجز كاشير";
        public const string CancelledOrder = "أوردر ملغي";
        public const string CustomerReturn = "مردود عميل";
        public const string Other = "أخرى";
    }
}
