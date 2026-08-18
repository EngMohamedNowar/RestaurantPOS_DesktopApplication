# Restaurant POS — Desktop Application

A desktop Point of Sale (POS) system built with **WPF** and **.NET 10**, designed for pizza and fast-food restaurants. Supports Arabic UI, inventory management, shifts, delivery, and Epson ESC/POS receipt printing.

## Features

- **Point of Sale:** Categories, products, sizes, extras, discount (percent/fixed), tax, service charge
- **Order Types:** Dine-in, takeaway, delivery
- **Shifts:** Open/close shift, cash reconciliation
- **Inventory:** Ingredients, stock movements, low-stock alerts
- **Reports:** Daily sales, profit, losses, Excel export (ClosedXML)
- **Printing:** ESC/POS via USB/Serial + cash drawer
- **Users:** Roles (admin / cashier) and PIN-based login

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```powershell
cd PizzaPOS
dotnet run
```

## Build for Distribution (Self-contained)

```powershell
cd PizzaPOS
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish
```

The executable is output to `PizzaPOS/publish/`.

## Default Login (First Run)

| User       | PIN    |
|------------|--------|
| `admin`    | `1234` |
| `cashier1` | `1234` |

> **Important:** Change the admin PIN immediately after first run in production.

## Database

- Local SQLite at: `%AppData%\PizzaPOS\pos.db`
- Tables and sample data are created automatically on first launch

## Project Structure

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

## License

MIT — see [LICENSE.txt](LICENSE.txt)
