<div align="center">

# 🍕 Restaurant POS — Desktop Application

### Point of Sale System for Restaurants & Fast Food

**نظام كاشير (Point of Sale) لسطح المكتب مبني بـ WPF و .NET 10**

[![C%23](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
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
        <li>Tax & Service Fee</li>
        <li>Dine-in, Takeaway, Delivery</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>📅 Shifts Management</h3>
      <ul>
        <li>Open / Close Shift</li>
        <li>Cash Reconciliation</li>
        <li>Shift Reports</li>
        <li>Multi-Cashier Support</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>📦 Inventory System</h3>
      <ul>
        <li>Ingredients & Recipes</li>
        <li>Stock Movements</li>
        <li>Low Stock Alerts</li>
        <li>Automatic Deduction</li>
      </ul>
    </td>
    <td width="50%" valign="top">
      <h3>📊 Reports & Analytics</h3>
      <ul>
        <li>Daily Sales Reports</li>
        <li>Profit & Loss Tracking</li>
        <li>Excel Export (ClosedXML)</li>
        <li>Revenue Analytics</li>
      </ul>
    </td>
  </tr>
</table>

---

## 🖨️ Printer Integration

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

| Category | Technologies |
|----------|-------------|
| **Language** | C# |
| **Framework** | .NET 10, WPF |
| **Architecture** | MVVM, Three-Layer |
| **Database** | SQLite (Local) |
| **ORM** | Entity Framework Core |
| **Printing** | ESC/POS, Win32 API |
| **Export** | ClosedXML (Excel) |
| **IDE** | Visual Studio |

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
└── PizzaPOS/
    ├── Data/              # SQLite + DatabaseHelper
    ├── Models/            # Data Models
    ├── Services/          # Business Logic
    │   ├── Inventory      # Stock Management
    │   ├── Shift          # Shift Management
    │   ├── User           # Authentication
    │   └── Printer        # ESC/POS Integration
    ├── ViewModels/        # MVVM ViewModels
    └── Views/             # WPF Windows & Dialogs
```

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

## 👨‍💻 Author

**Mohamed Nowar** — Junior .NET Backend Developer

<div align="center">

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white)](https://linkedin.com/in/mohamednowar2002)
[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/EngMohamedNowar)
[![Email](https://img.shields.io/badge/Email-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:mohamednowar2002@gmail.com)

</div>
