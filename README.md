<div align="center">

# 🍕 NAPOLI Pizza — Restaurant POS Desktop Application

### A full-featured Point of Sale (POS) desktop application built with **WPF** and **.NET 10**

**نظام كاشير (Point of Sale) لسطح المكتب مبني بـ WPF و .NET 10**

[![C%23](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![xUnit](https://img.shields.io/badge/tests-385%2B-E65019?style=for-the-badge&logo=xunit&logoColor=white)](#%EF%B8%8F-testing)
[![License](https://img.shields.io/badge/License-MIT-00A86B?style=for-the-badge&logo=open-source-initiative&logoColor=white)](LICENSE.txt)

<br/>

<img src="https://komarev.com/ghpvc/?username=EngMohamedNowar&repo=RestaurantPOS_DesktopApplication&color=7C3AED&style=for-the-badge&label=CLONES" alt="Clones" />

</div>

---

## 📖 Overview

A production-ready POS built for restaurants that need more than a cash register: orders
(dine-in / takeaway / delivery), recipe-driven inventory, shift reconciliation, and
profit-aware reporting — all in one offline-first desktop app with thermal-printer
support and a hardware-locked licensing system.

نظام متكامل للمطاعم: أوردرات (صالة/تيك أواي/دليفري)، مخزون مرتبط بالوصفات، محاسبة
ورديات، وتقارير أرباح وخسائر دقيقة — يعمل بالكامل بدون إنترنت مع دعم طابعات
الحرارية ونظام ترخيص مربوط بعتاد الجهاز.

---

## ✨ Features

<table>
  <tr>
    <td width="50%" valign="top">
      <h3>🛒 Point of Sale</h3>
      <ul>
        <li>Categories, Products, Sizes & Add-ons</li>
        <li>Discounts (Percentage / Fixed Amount)</li>
        <li>Tax & Service Fee Configuration</li>
        <li>Dine-in, Takeaway & Delivery modes</li>
        <li>Held orders & live order editing</li>
        <li>Real-time order summary</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>📦 Inventory & Recipes</h3>
      <ul>
        <li>Ingredient-linked recipes per product</li>
        <li>Auto product costing from recipes</li>
        <li>Stock tracking with low-stock alerts</li>
        <li>Stock movements (In / Out / Adjustment)</li>
        <li>Purchase entry & ingredient categories</li>
        <li>Automatic stock deduction on checkout</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>📅 Shift Management</h3>
      <ul>
        <li>Open / Close shift workflow</li>
        <li>Cash drawer reconciliation</li>
        <li>Cash matching & discrepancy tracking</li>
        <li>Shift-based sales reporting</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>📊 Reports & Analytics</h3>
      <ul>
        <li>Daily sales & profit/loss reports</li>
        <li>Below-cost sale detection (after discounts)</li>
        <li>Waste & manual loss log</li>
        <li>Best-selling products ranking</li>
        <li>Excel export (ClosedXML)</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>🎁 Offers & Promotions</h3>
      <ul>
        <li>Promotional offers with discount %</li>
        <li>Promo code support</li>
        <li>WhatsApp integration for bulk sending</li>
        <li>Customer list with bulk selection</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>💎 Customer Loyalty Program</h3>
      <ul>
        <li>Customer database with phone numbers</li>
        <li>Points system with tiers:</li>
        <li>Bronze — Silver — Gold — Diamond</li>
        <li>Points earned per order</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>🔐 License Key System</h3>
      <ul>
        <li>Hardware fingerprinting (CPU, motherboard, disk)</li>
        <li>License keys bound to hardware (HMAC-SHA256)</li>
        <li>Temporary keys (30-day trial)</li>
        <li>Permanent keys (lifetime access)</li>
        <li>Startup validation with activation window</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>⚙️ Settings & Users</h3>
      <ul>
        <li>Shop name, address & phone</li>
        <li>Tax rate & service charge config</li>
        <li>Profit margin configuration</li>
        <li>Role-based access: Admin & Cashier</li>
        <li>PIN login with salted PBKDF2 hashing</li>
      </ul>
    </td>
  </tr>
</table>

---

## Screenshots

### Login & Shift

| | | |
|:---:|:---:|:---:|
| ![Login](PizzaPOS/screenshots/01-login.png) | ![Open Shift](PizzaPOS/screenshots/02-open-shift.png) | |

### Point of Sale

| | | |
|:---:|:---:|:---:|
| ![POS Admin](PizzaPOS/screenshots/03-pos-admin.png) | ![POS Admin 2](PizzaPOS/screenshots/04-pos-admin-2.png) | |

### Product Management

| | | |
|:---:|:---:|:---:|
| ![Categories](PizzaPOS/screenshots/05-categories.png) | ![Products](PizzaPOS/screenshots/06-products.png) | ![Add Product](PizzaPOS/screenshots/11-add-product.png) |
| ![Sizes & Extras](PizzaPOS/screenshots/12-sizes-extras.png) | ![Ingredients](PizzaPOS/screenshots/13-ingredients.png) | ![Add Ingredient](PizzaPOS/screenshots/14-add-ingredient.png) |

### Warehouse Management

| | | |
|:---:|:---:|:---:|
| ![Warehouse](PizzaPOS/screenshots/07-warehouse.png) | ![Stock Purchase](PizzaPOS/screenshots/15-stock-purchase.png) | ![Add Stock](PizzaPOS/screenshots/16-add-stock.png) |
| ![Stock Movements](PizzaPOS/screenshots/17-stock-movements.png) | ![Ingredient Categories](PizzaPOS/screenshots/18-ingredient-categories.png) | |

### Offers & Promotions

| | | |
|:---:|:---:|:---:|
| ![Offers](PizzaPOS/screenshots/08-offers.png) | ![Add Offer](PizzaPOS/screenshots/09-add-offer.png) | ![Send WhatsApp](PizzaPOS/screenshots/10-send-whatsapp.png) |

### Reports

| | | |
|:---:|:---:|:---:|
| ![Daily Report](PizzaPOS/screenshots/19-report-daily.png) | ![Top Products](PizzaPOS/screenshots/20-report-top-products.png) | ![Losses](PizzaPOS/screenshots/21-report-losses.png) |

### Customer & Driver Management

| | | |
|:---:|:---:|:---:|
| ![Customers](PizzaPOS/screenshots/22-customers.png) | ![Add Customer](PizzaPOS/screenshots/23-add-customer.png) | ![Drivers](PizzaPOS/screenshots/24-drivers.png) |
| ![Add Driver](PizzaPOS/screenshots/25-add-driver.png) | | |

### User Management

| | | |
|:---:|:---:|:---:|
| ![Users](PizzaPOS/screenshots/26-users.png) | ![Add User](PizzaPOS/screenshots/27-add-user.png) | |

### Settings & Tracking

| | | |
|:---:|:---:|:---:|
| ![Settings](PizzaPOS/screenshots/28-settings.png) | ![Order Tracking](PizzaPOS/screenshots/29-order-tracking.png) | |

---

## 🖨️ Receipt Printing

<div align="center">

| Feature | Status |
|---------|--------|
| ESC/POS Thermal Printing | ✅ Supported |
| Epson TM-T88V | ✅ Auto-Detect |
| USB & Serial Connection | ✅ Supported |
| Cash Drawer Kick | ✅ Supported |
| Arabic Receipt Support | ✅ With Translation Layer |
| Customer Data on Receipt | ✅ Styled Sections |

</div>

---

## 🛠️ Tech Stack

<div align="center">

<img src="https://skillicons.dev/icons?i=cs,dotnet,sqlite,visualstudio,windows" />

</div>

<div align="center">

| Technology | Purpose |
|------------|---------|
| C# / .NET 10 | Core language & runtime |
| WPF | Desktop UI framework |
| SQLite | Local database (Microsoft.Data.Sqlite) |
| ClosedXML | Excel report export |
| System.IO.Ports | Serial printer communication |
| ESC/POS | Thermal printer protocol |
| HMAC-SHA256 | License key generation |
| PBKDF2 | Credential hashing |
| WMI | Hardware fingerprinting |

</div>

---

## 🏗️ Architecture

```
RestaurantPOS_DesktopApplication/
├── POS.slnx                      # Solution
├── PizzaPOS/                     # WPF application
│   ├── Data/                     # SQLite schema, migrations & seed data
│   ├── Helpers/                  # UiHelper (shared UI components)
│   ├── Models/                   # Entities, DTOs & enums
│   ├── Services/                 # Business logic
│   │   ├── Inventory             # Stock & recipe management
│   │   ├── Shift                 # Shift lifecycle & reconciliation
│   │   ├── User                  # Authentication & roles
│   │   ├── License               # Hardware-bound licensing
│   │   ├── Printer               # ESC/POS integration
│   │   └── Backup                # Automatic DB backups
│   ├── ViewModels/               # MVVM ViewModels
│   └── Views/                    # Windows & dialogs
├── PizzaPOS.LicenseGenerator/    # CLI tool for issuing license keys
└── PizzaPOS.Tests/               # Unit & integration tests
```

**Design notes**

- **MVVM** — views are dumb; behavior lives in ViewModels and services.
- **Repository-free data layer** — a small raw-ADO.NET context keeps SQL explicit
  and migrations verifiable.
- **Seed data** — full menu, recipes and pricing ship with the schema, so a fresh
  database is restaurant-ready on first launch.
- **Offline-first** — no network dependency; local SQLite storage with automatic
  backups and Excel/PDF-ready exports.

---

## 🗄️ Data

- Local SQLite database — created, migrated and seeded automatically.
- Schema migrations are versioned and safe to re-run.
- Automatic backups of the database are taken on startup.
- Sensitive values (credentials, license payloads) are never stored in plaintext.

---

## ✅ Testing

<div align="center">

| | |
|:---:|:---:|
| **385+** unit & integration tests | xUnit + Coverlet |

</div>

The suite guards the behaviors that matter most in production:

- **Checkout atomicity** — orders, stock deduction and payments commit or roll back together.
- **Migrations & seeding** — schema, menu (38 products) and recipe coverage (290 ingredient lines).
- **Reporting math** — profit/loss, below-cost sale detection and discount allocation.
- **Security** — credential hashing, license validation and no-silent-catch enforcement.
- **Shifts & counters** — daily order numbering, rollback safety, reconciliation.
- **Services** — backups, single-instance guard, inventory operations.

---

## 🤝 Contributing

Contributions are welcome! Feel free to open issues and pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License — see [LICENSE.txt](LICENSE.txt) for details.

---

## 👨‍💻 Developer

**Eng. Mohamed Nowar** — Junior .NET Backend Developer

<div align="center">

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white)](https://linkedin.com/in/mohamednowar2002)
[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/EngMohamedNowar)
[![Email](https://img.shields.io/badge/Email-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:mohamednowar2002@gmail.com)
[![Portfolio](https://img.shields.io/badge/Portfolio-00A86B?style=for-the-badge&logo=googlechrome&logoColor=white)](https://engmohamednowar.github.io/portfolio/)

</div>
