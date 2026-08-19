// Services/AppServices.cs — Simple Service Locator
using PizzaPOS.Data;

namespace PizzaPOS.Services
{
    public static class AppServices
    {
        static readonly Lazy<AppDbContext> _db = new(() => new AppDbContext());
        static readonly Lazy<InventoryService> _inv = new(() => new InventoryService());
        static readonly Lazy<EpsonService> _printer = new(() => new EpsonService());

        public static AppDbContext Db => _db.Value;
        public static InventoryService Inventory => _inv.Value;
        public static EpsonService Printer => _printer.Value;
    }
}
