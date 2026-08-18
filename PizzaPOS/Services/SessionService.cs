// Services/SessionService.cs
using PizzaPOS.Models;
namespace PizzaPOS.Services
{
    public static class SessionService
    {
        public static User? CurrentUser { get; set; }
        public static Shift? CurrentShift { get; set; }
        public static bool IsLoggedIn => CurrentUser != null;
        public static bool HasOpenShift => CurrentShift?.Status == "open";
    }
}
