# Restaurant POS — Desktop Application

نظام كاشير (Point of Sale) لسطح المكتب مبني بـ **WPF** و **.NET 10**، موجّه لمطاعم البيتزا والوجبات السريعة. يدعم العربية، إدارة المخزون، الشيفتات، الديلفري، والطباعة على طابعات Epson (ESC/POS).

## المميزات

- **نقطة البيع:** فئات، منتجات، أحجام، إضافات، خصم (نسبة/مبلغ)، ضريبة، رسوم خدمة
- **أنواع الطلب:** صالة، تيك أواي، ديلفري
- **الشيفتات:** فتح/إغلاق شيفت، مطابقة الكاش
- **المخزون:** مكونات، حركات مخزون، تنبيه مخزون منخفض
- **التقارير:** مبيعات يومية، أرباح، خسائر، تصدير Excel (ClosedXML)
- **الطباعة:** ESC/POS عبر USB/Serial + درج نقدي
- **المستخدمون:** أدوار (admin / cashier) وتسجيل دخول بـ PIN

## المتطلبات

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## التشغيل

```powershell
cd PizzaPOS
dotnet run
```

## بناء نسخة للتوزيع (Self-contained)

```powershell
cd PizzaPOS
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish
```

الملف التنفيذي يظهر في مجلد `PizzaPOS/publish/`.

## بيانات الدخول الافتراضية (أول تشغيل)

| المستخدم | PIN |
|----------|-----|
| `admin` | `1234` |
| `cashier1` | `1234` |

> **مهم:** غيّر PIN الأدمن فوراً بعد أول تشغيل في بيئة الإنتاج.

## قاعدة البيانات

- SQLite محلية في: `%AppData%\PizzaPOS\pos.db`
- يتم إنشاء الجداول والبيانات التجريبية تلقائياً عند أول تشغيل

## هيكل المشروع

```
RestaurantPOS_DesktopApplication/
├── POS.slnx
└── PizzaPOS/
    ├── Data/           # SQLite + DatabaseHelper
    ├── Models/
    ├── Services/       # Inventory, Shift, User, Epson printer
    ├── ViewModels/
    └── Views/          # WPF windows & dialogs
```

## الترخيص

MIT — راجع [LICENSE.txt](LICENSE.txt)
