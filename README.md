<div align="center">

# 🍕 NAPOLI Pizza — Restaurant POS Desktop Application

### A full-featured Point of Sale (POS) desktop application built with **WPF** and **.NET 10**

**نظام كاشير (Point of Sale) لسطح المكتب مبني بـ WPF و .NET 10**

[![C%23](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![License](https://img.shields.io/badge/License-MIT-00A86B?style=for-the-badge&logo=open-source-initiative&logoColor=white)](LICENSE.txt)

<br/>

<img src="https://komarev.com/ghpvc/?username=EngMohamedNowar&repo=RestaurantPOS_DesktopApplication&color=7C3AED&style=for-the-badge&label=CLONES" alt="Clones" />

</div>

---

## ✨ Features

<table>
  <tr>
    <td width="50%" valign="top">
      <h3>🛒 Point of Sale</h3>
      <ul>
        <li>Categories, Products, Sizes, Add-ons</li>
        <li>Discount (Percentage / Fixed Amount)</li>
        <li>Tax & Service Fee Configuration</li>
        <li>Dine-in, Takeaway, Delivery</li>
        <li>Real-time Order Summary with Item Editing</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>📦 Inventory & Recipes</h3>
      <ul>
        <li>Link Ingredients to Products with Quantities</li>
        <li>Auto-calculate Product Cost from Ingredients</li>
        <li>Bulk Price Update by Percentage</li>
        <li>Stock Tracking with Low-Stock Alerts</li>
        <li>Stock Movement Tracking (In/Out/Adjustment)</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>📅 Shift Management</h3>
      <ul>
        <li>Open / Close Shift</li>
        <li>Cash Drawer Reconciliation</li>
        <li>Cash Matching & Discrepancy Tracking</li>
        <li>Shift-based Sales Reporting</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>📊 Reports & Analytics</h3>
      <ul>
        <li>Daily Sales Reports</li>
        <li>Profit & Loss Tracking</li>
        <li>Waste / Loss Reporting</li>
        <li>Excel Export (ClosedXML)</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>🎁 Offers & Promotions</h3>
      <ul>
        <li>Promotional Offers with Discount %</li>
        <li>Promo Code Support</li>
        <li>WhatsApp Integration for Bulk Sending</li>
        <li>Customer List with Bulk Selection</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>💎 Customer Loyalty Program</h3>
      <ul>
        <li>Customer Database with Phone Numbers</li>
        <li>Points System with Tiers:</li>
        <li>Bronze — Silver — Gold — Diamond</li>
        <li>Points Earned Per Order</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>🔐 License Key System</h3>
      <ul>
        <li>Hardware Fingerprinting (CPU, Motherboard, Disk)</li>
        <li>License Key Validation tied to Hardware</li>
        <li>Temporary Keys (30-day Trial)</li>
        <li>Permanent Keys (Lifetime Access)</li>
        <li>Auto-check on Startup with Activation Window</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>⚙️ Settings & Users</h3>
      <ul>
        <li>Shop Name, Address, Phone Numbers</li>
        <li>Tax Rate & Service Charge Config</li>
        <li>Profit Margin Configuration</li>
        <li>Role-based Access: Admin & Cashier</li>
        <li>PIN-based Login (SHA256)</li>
      </ul>
    </td>
  </tr>
</table>

---

## Screenshots

### Login & Shift

| | | |
|:---:|:---:|:---:|
| ![Login](PizzaPOS/screenshots/01-login.png) | ![Open Shift](PizzaPOS/screenshots/02-open-shift.png) | ![Close Shift](PizzaPOS/screenshots/03-close-shift.png) |
| **Login** | **Open Shift** | **Close Shift** |

### Point of Sale

| | | |
|:---:|:---:|:---:|
| ![POS Admin](PizzaPOS/screenshots/04-pos-admin.png) | ![POS Cashier](PizzaPOS/screenshots/05-pos-cashier.png) | ![Product Detail](PizzaPOS/screenshots/06-product-detail.png) |
| **POS (Admin)** | **POS (Cashier)** | **Product Sizes & Extras** |
| ![Delivery](PizzaPOS/screenshots/07-delivery.png) | ![Cash Payment](PizzaPOS/screenshots/08-cash-payment.png) | ![Order Tracking](PizzaPOS/screenshots/09-order-tracking.png) |
| **Delivery Details** | **Cash Payment** | **Order Tracking Board** |
| ![Held Orders](PizzaPOS/screenshots/10-held-orders.png) | | |
| **Held Orders** | | |

### Product & Category Management

| | | |
|:---:|:---:|:---:|
| ![Categories](PizzaPOS/screenshots/11-categories.png) | ![Products](PizzaPOS/screenshots/12-products.png) | ![Add Product](PizzaPOS/screenshots/13-add-product.png) |
| **Category Management** | **Product Management** | **Add New Product** |
| ![Sizes & Extras](PizzaPOS/screenshots/14-sizes-extras.png) | ![Ingredients](PizzaPOS/screenshots/15-ingredients.png) | |
| **Sizes & Extras** | **Product Recipe (Ingredients)** | |

### Warehouse Management

| | | |
|:---:|:---:|:---:|
| ![Warehouse](PizzaPOS/screenshots/16-warehouse.png) | ![Add Ingredient](PizzaPOS/screenshots/17-add-ingredient.png) | ![Ingredient Categories](PizzaPOS/screenshots/18-ingredient-categories.png) |
| **Warehouse** | **Add New Ingredient** | **Ingredient Categories** |
| ![Stock Purchase](PizzaPOS/screenshots/19-stock-purchase.png) | ![Stock Movements](PizzaPOS/screenshots/20-stock-movements.png) | |
| **Stock Purchase** | **Stock Movements History** | |

### Offers & WhatsApp

| | |
|:---:|:---:|
| ![Offers](PizzaPOS/screenshots/21-offers.png) | ![Send WhatsApp](PizzaPOS/screenshots/22-send-whatsapp.png) |
| **Offers & Promotions** | **Send Offer via WhatsApp** |

### Reports & Analytics

| | | |
|:---:|:---:|:---:|
| ![Daily Report](PizzaPOS/screenshots/23-report-daily.png) | ![Top Products](PizzaPOS/screenshots/24-report-top-products.png) | ![Loss History](PizzaPOS/screenshots/25-report-losses.png) |
| **Daily Report** | **Top Products** | **Loss History** |

### Customer, Driver & User Management

| | | |
|:---:|:---:|:---:|
| ![Customers](PizzaPOS/screenshots/26-customers.png) | ![Drivers](PizzaPOS/screenshots/27-drivers.png) | ![Users](PizzaPOS/screenshots/28-users.png) |
| **Customer Management** | **Driver Management** | **User Management** |

### Settings

| |
|:---:|
| ![Settings](PizzaPOS/screenshots/29-settings.png) |
| **System Settings** |

---

## Tech Stack

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
| C# / .NET 10 | Core Language & Runtime |
| WPF | Desktop UI Framework |
| SQLite | Local Database (via Microsoft.Data.Sqlite) |
| ClosedXML | Excel Report Export |
| System.IO.Ports | Serial Printer Communication |
| ESC/POS | Thermal Printer Protocol |
| HMACSHA256 | License Key Generation |
| WMI | Hardware Fingerprinting |

</div>

---

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run

```powershell
git clone https://github.com/EngMohamedNowar/RestaurantPOS_DesktopApplication.git
cd RestaurantPOS_DesktopApplication/PizzaPOS
dotnet run
```

### Build Release

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish
```

Output: `PizzaPOS/publish/PizzaPOS.exe`

---

## 🔑 Generating License Keys

```powershell
cd PizzaPOS.LicenseGenerator
dotnet run
```

The tool will:
1. Ask for the Hardware ID (from the activation screen)
2. Ask for license type (Permanent or Trial)
3. Generate a license key

---

## 🔐 Default Login

<div align="center">

| User | PIN | Role |
|------|-----|------|
| `admin` | `1234` | Administrator |
| `cashier1` | `1234` | Cashier |

> ⚠️ **Change the admin PIN immediately after first run in production!**

</div>

---

## 📁 Project Structure

```
RestaurantPOS_DesktopApplication/
├── POS.slnx
├── PizzaPOS/
│   ├── Data/              # SQLite context & DatabaseHelper
│   ├── Helpers/           # UiHelper (shared UI components)
│   ├── Models/            # Entity classes & enums
│   ├── Services/          # Business logic
│   │   ├── Inventory      # Stock Management
│   │   ├── Shift          # Shift Management
│   │   ├── User           # Authentication
│   │   ├── License        # License Key System
│   │   └── Printer        # ESC/POS Integration
│   ├── ViewModels/        # MVVM ViewModels
│   └── Views/             # WPF Windows & Dialogs
├── PizzaPOS.LicenseGenerator/  # Console tool for generating license keys
└── PizzaPOS.Tests/        # Unit tests
```

---

## 💾 Database

- SQLite local database at: `%AppData%\PizzaPOS\pos.db`
- Tables and seed data are created automatically on first run
- License file stored at: `%AppData%\PizzaPOS\license.dat`

---

## 📱 Screenshots

<div align="center">

> 📸 *Screenshots coming soon!*

</div>

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
