// Services/SessionService.cs
using PizzaPOS.Data;
using PizzaPOS.Models;
namespace PizzaPOS.Services
{
    public static class SessionService
    {
        public static User? CurrentUser { get; set; }
        public static Shift? CurrentShift { get; set; }
        public static bool IsLoggedIn => CurrentUser != null;
        public static bool HasOpenShift => CurrentShift?.Status == "open";

        static string _shopName = "NAPOLI";
        public static string ShopName
        {
            get
            {
                if (_shopName == "NAPOLI")
                {
                    try { _shopName = new AppDbContext().GetSetting("ShopName", "NAPOLI"); }
                    catch { }
                }
                return _shopName;
            }
        }

        public static void RefreshShopName()
        {
            try { _shopName = new AppDbContext().GetSetting("ShopName", "NAPOLI"); }
            catch { }
        }
    }
}
