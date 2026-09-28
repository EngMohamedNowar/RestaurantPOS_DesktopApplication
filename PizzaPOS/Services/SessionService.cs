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
        static bool _shopNameLoaded;

        /// <summary>
        /// اسم المطعم اللي بيظهر في الفواتير والشاشة الرئيسية.
        ///
        /// الكود القديم كان بيبلع الاستثناءات فاضية، فأي فشل في الـ DB
        /// كان بيخلي البرنامج يعرض "NAPOLI" على الفواتير من غير ما يبلّغ
        /// حد — يعني فواتير باسم مطعم غلط بتروح لعملاء ومفيش أثر.
        /// دلوقتي نسجّل وبيبان الـ default زي ما كان.
        ///
        /// _shopNameLoaded منفصل عن قيمة _shopName عشان نفضل نحاول تاني
        /// لو القراءة الأولى فشلت (الـ DB ممكن يكون لسه بيتعمله migrate).
        /// </summary>
        public static string ShopName
        {
            get
            {
                if (!_shopNameLoaded) RefreshShopName();
                return _shopName;
            }
        }

        public static void RefreshShopName()
        {
            try
            {
                _shopName = new AppDbContext().GetSetting("ShopName", "NAPOLI");
                _shopNameLoaded = true;
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Shop name unavailable, using default: {ex.Message}");
            }
        }
    }
}
