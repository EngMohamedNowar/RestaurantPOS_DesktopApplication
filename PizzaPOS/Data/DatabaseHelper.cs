// Data/DatabaseHelper.cs
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PizzaPOS.Data
{
    public static class DatabaseHelper
    {
        public static string DbPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PizzaPOS", "pos.db");

        public static string CS => $"Data Source={DbPath}";

        public static SqliteConnection Open()
        {
            var c = new SqliteConnection(CS);
            c.Open();
            return c;
        }

        public static void Initialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            using var conn = Open();

            Exec(conn, "PRAGMA journal_mode=WAL;");
            Exec(conn, "PRAGMA foreign_keys=ON;");

            // ── Categories ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Categories (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT    NOT NULL,
                Icon      TEXT    DEFAULT '🍕',
                SortOrder INTEGER DEFAULT 0);");

            // ── Products ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Products (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId INTEGER REFERENCES Categories(Id),
                Name       TEXT    NOT NULL,
                Price      REAL    NOT NULL,
                Cost       REAL    DEFAULT 0,
                Icon       TEXT    DEFAULT '🍕',
                IsActive   INTEGER DEFAULT 1);");

            // ── ProductSizes ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS ProductSizes (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductId INTEGER REFERENCES Products(Id),
                Name      TEXT    NOT NULL,
                ExtraPrice REAL   DEFAULT 0,
                SortOrder INTEGER DEFAULT 0);");

            // ── ProductExtras ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS ProductExtras (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductId INTEGER REFERENCES Products(Id),
                Name      TEXT    NOT NULL,
                Price     REAL    DEFAULT 0);");

            // ── IngredientCategories ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS IngredientCategories (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT    NOT NULL);");

            // ── Ingredients ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Ingredients (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId  INTEGER REFERENCES IngredientCategories(Id),
                Name        TEXT    NOT NULL,
                Unit        TEXT    NOT NULL,
                Stock       REAL    DEFAULT 0,
                MinStock    REAL    DEFAULT 0,
                CostPerUnit REAL    DEFAULT 0);");

            // ── ProductIngredients ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS ProductIngredients (
                ProductId    INTEGER REFERENCES Products(Id),
                IngredientId INTEGER REFERENCES Ingredients(Id),
                QtyUsed      REAL    NOT NULL,
                PRIMARY KEY(ProductId, IngredientId));");

            // ── StockMovements ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS StockMovements (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                IngredientId INTEGER REFERENCES Ingredients(Id),
                Type         TEXT    NOT NULL,
                Qty          REAL    NOT NULL,
                Note         TEXT,
                UserId       INTEGER,
                CreatedAt    TEXT    DEFAULT (datetime('now','localtime')));");

            // ── Users ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Users (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT    NOT NULL UNIQUE,
                FullName TEXT    NOT NULL,
                PinHash  TEXT    NOT NULL,
                Role     TEXT    DEFAULT 'cashier',
                IsActive INTEGER DEFAULT 1);");

            // ── Shifts ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Shifts (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId       INTEGER REFERENCES Users(Id),
                OpeningCash  REAL    DEFAULT 0,
                ClosingCash  REAL,
                ExpectedCash REAL    DEFAULT 0,
                Difference   REAL    DEFAULT 0,
                OpenedAt     TEXT    DEFAULT (datetime('now','localtime')),
                ClosedAt     TEXT,
                Status       TEXT    DEFAULT 'open');");

            // ── Customers ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Customers (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT    NOT NULL,
                Phone     TEXT    NOT NULL,
                Address   TEXT    NOT NULL DEFAULT '',
                Notes     TEXT    DEFAULT '',
                CreatedAt TEXT    DEFAULT (datetime('now','localtime')));");

            // ── Drivers ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Drivers (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Name     TEXT    NOT NULL,
                Phone    TEXT    NOT NULL,
                IsActive INTEGER DEFAULT 1);");

            // ── Orders ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Orders (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderNumber     TEXT    NOT NULL,
                ShiftId         INTEGER REFERENCES Shifts(Id),
                UserId          INTEGER REFERENCES Users(Id),
                OrderType       TEXT    DEFAULT 'صالة',
                PayMethod       TEXT    DEFAULT 'كاش',
                Subtotal        REAL    DEFAULT 0,
                Discount        REAL    DEFAULT 0,
                Tax             REAL    DEFAULT 0,
                ServiceCharge   REAL    DEFAULT 0,
                Total           REAL    DEFAULT 0,
                PaidAmount      REAL    DEFAULT 0,
                Change          REAL    DEFAULT 0,
                Notes           TEXT,
                CustomerId      INTEGER DEFAULT 0,
                CustomerName    TEXT    DEFAULT '',
                CustomerPhone   TEXT    DEFAULT '',
                DeliveryAddress TEXT    DEFAULT '',
                DriverId        INTEGER DEFAULT 0,
                DriverName      TEXT    DEFAULT '',
                DeliveryFee     REAL    DEFAULT 0,
                DeliveryStatus  TEXT    DEFAULT '',
                CreatedAt       TEXT    DEFAULT (datetime('now','localtime')),
                Status          TEXT    DEFAULT 'completed');");

            // ── Migrations للـ DB القديمة ──
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN ServiceCharge   REAL    DEFAULT 0"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN CustomerId      INTEGER DEFAULT 0"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN CustomerName    TEXT    DEFAULT ''"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN CustomerPhone   TEXT    DEFAULT ''"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN DeliveryAddress TEXT    DEFAULT ''"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN DriverId        INTEGER DEFAULT 0"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN DriverName      TEXT    DEFAULT ''"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN DeliveryFee     REAL    DEFAULT 0"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN DeliveryStatus  TEXT    DEFAULT ''"); } catch { }
            try { Exec(conn, "ALTER TABLE Orders ADD COLUMN Status TEXT DEFAULT 'new'"); } catch { }
            try { Exec(conn, "UPDATE Orders SET Status='completed' WHERE Status IS NULL OR Status=''"); } catch { }

            // ── OrderItems ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS OrderItems (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId    INTEGER REFERENCES Orders(Id),
                ProductId  INTEGER,
                Name       TEXT    NOT NULL,
                Price      REAL    NOT NULL,
                Cost       REAL    DEFAULT 0,
                Qty        INTEGER NOT NULL,
                Subtotal   REAL    NOT NULL,
                SizeName   TEXT,
                ExtrasNote TEXT);");

            // ── Migrations OrderItems ──
            try { Exec(conn, "ALTER TABLE OrderItems ADD COLUMN SizeName   TEXT"); } catch { }
            try { Exec(conn, "ALTER TABLE OrderItems ADD COLUMN ExtrasNote TEXT"); } catch { }

            // ── Settings ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Settings (
                Key   TEXT PRIMARY KEY,
                Value TEXT);");

            // ── Losses ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Losses (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Date        TEXT    NOT NULL DEFAULT (date('now','localtime')),
                Type        TEXT    NOT NULL,
                Description TEXT    NOT NULL,
                Amount      REAL    NOT NULL DEFAULT 0,
                CreatedBy   TEXT    NOT NULL DEFAULT '',
                CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now','localtime')));");

            Seed(conn);
        }

        static void Seed(SqliteConnection conn)
        {
            var chk = conn.CreateCommand();
            chk.CommandText = "SELECT COUNT(*) FROM Categories";
            if ((long)chk.ExecuteScalar()! > 0) return;

            // ══════════════════════════════════════════
            // ── Categories ──
            // IDs: 1=بيتزا, 2=بيتزا Napoletana, 3=باستا, 4=ساندويتش, 5=فطاير
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO Categories(Name,Icon,SortOrder) VALUES
        ('بيتزا',             '🍕', 1),
        ('بيتزا Napoletana',  '🍕', 2),
        ('باستا',             '🍝', 3),
        ('ساندويتش',          '🥪', 4),
        ('فطاير',             '🥙', 5);");

            // ══════════════════════════════════════════
            // ── Products ──
            // ══════════════════════════════════════════

            // --- بيتزا (Cat 1) → IDs 1-5 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (1, 'Margherita',       89,  30, '🍕'),
        (1, 'Pepperoni',        105, 38, '🍕'),
        (1, 'Quattro Formaggi', 115, 42, '🍕'),
        (1, 'Pollo Special',    99,  35, '🍕'),
        (1, 'Napoli Döner',     109, 40, '🍕');");

            // --- بيتزا Napoletana (Cat 2) → IDs 6-10 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (2, 'Margherita Napoletana',       95,  32, '🍕'),
        (2, 'Pepperoni Napoletana',        115, 40, '🍕'),
        (2, 'Quattro Formaggi Napoletana', 125, 45, '🍕'),
        (2, 'Pollo Special Napoletana',    109, 37, '🍕'),
        (2, 'Napoli Döner Napoletana',     119, 43, '🍕');");

            // --- باستا (Cat 3) → IDs 11-13 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (3, 'Napoli Pasta',  79, 25, '🍝'),
        (3, 'Tuscan Pasta',  89, 30, '🍝'),
        (3, 'Pesto Pasta',   80, 26, '🍝');");

            // --- ساندويتش (Cat 4) → IDs 14-17 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (4, 'Beef Burger',          65, 22, '🍔'),
        (4, 'Chicken Fanta Sand.',  60, 20, '🥪'),
        (4, 'Shawarma Sand.',       65, 20, '🥙'),
        (4, 'Crep Sand.',           55, 18, '🥪');");

            // --- فطاير (Cat 5) → IDs 18-20 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (5, 'Fatayer Sujuk',       45, 14, '🥙'),
        (5, 'Fatayer Minced Beef', 45, 14, '🥙'),
        (5, 'Cherry Napoli',       50, 16, '🥙');");

            // ══════════════════════════════════════════
            // ── ProductSizes ──
            // ══════════════════════════════════════════

            // بيتزا (IDs 1-5) → Small / Medium / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (1,'Small',0,1),(1,'Medium',20,2),(1,'Large',40,3),
        (2,'Small',0,1),(2,'Medium',20,2),(2,'Large',40,3),
        (3,'Small',0,1),(3,'Medium',20,2),(3,'Large',40,3),
        (4,'Small',0,1),(4,'Medium',20,2),(4,'Large',40,3),
        (5,'Small',0,1),(5,'Medium',20,2),(5,'Large',40,3);");

            // بيتزا Napoletana (IDs 6-10) → Small / Medium / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (6, 'Small',0,1),(6, 'Medium',20,2),(6, 'Large',40,3),
        (7, 'Small',0,1),(7, 'Medium',20,2),(7, 'Large',40,3),
        (8, 'Small',0,1),(8, 'Medium',20,2),(8, 'Large',40,3),
        (9, 'Small',0,1),(9, 'Medium',20,2),(9, 'Large',40,3),
        (10,'Small',0,1),(10,'Medium',20,2),(10,'Large',40,3);");

            // باستا (IDs 11-13) → Regular / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (11,'Regular',0,1),(11,'Large',20,2),
        (12,'Regular',0,1),(12,'Large',20,2),
        (13,'Regular',0,1),(13,'Large',20,2);");

            // ساندويتش (IDs 14-17) → Regular / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (14,'Regular',0,1),(14,'Large',15,2),
        (15,'Regular',0,1),(15,'Large',15,2),
        (16,'Regular',0,1),(16,'Large',15,2),
        (17,'Regular',0,1),(17,'Large',15,2);");

            // فطاير (IDs 18-20) → بدون أحجام (حجم واحد)

            // ══════════════════════════════════════════
            // ── ProductExtras ──
            // ══════════════════════════════════════════

            // بيتزا Extras (IDs 1-5)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (1,'Extra Cheese',15),(1,'Extra Pepperoni',15),(1,'Mushrooms',10),(1,'Black Olives',10),(1,'Jalapeños',10),(1,'Fresh Basil',5),
        (2,'Extra Cheese',15),(2,'Extra Pepperoni',15),(2,'Mushrooms',10),(2,'Black Olives',10),(2,'Jalapeños',10),(2,'Hot Sauce',5),
        (3,'Extra Cheese',15),(3,'Blue Cheese',20),(3,'Mushrooms',10),(3,'Truffle Oil',25),(3,'Jalapeños',10),
        (4,'Extra Cheese',15),(4,'Extra Chicken',20),(4,'Mushrooms',10),(4,'Sweet Corn',10),(4,'BBQ Sauce',5),
        (5,'Extra Cheese',15),(5,'Döner Meat Extra',25),(5,'Jalapeños',10),(5,'Hot Sauce',5),(5,'Black Olives',10);");

            // بيتزا Napoletana Extras (IDs 6-10) — نفس إضافات البيتزا العادية
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (6, 'Extra Cheese',15),(6, 'Extra Pepperoni',15),(6, 'Mushrooms',10),(6, 'Black Olives',10),(6, 'Jalapeños',10),(6, 'Fresh Basil',5),
        (7, 'Extra Cheese',15),(7, 'Extra Pepperoni',15),(7, 'Mushrooms',10),(7, 'Black Olives',10),(7, 'Jalapeños',10),(7, 'Hot Sauce',5),
        (8, 'Extra Cheese',15),(8, 'Blue Cheese',20),(8, 'Mushrooms',10),(8, 'Truffle Oil',25),(8, 'Jalapeños',10),
        (9, 'Extra Cheese',15),(9, 'Extra Chicken',20),(9, 'Mushrooms',10),(9, 'Sweet Corn',10),(9, 'BBQ Sauce',5),
        (10,'Extra Cheese',15),(10,'Döner Meat Extra',25),(10,'Jalapeños',10),(10,'Hot Sauce',5),(10,'Black Olives',10);");

            // باستا Extras (IDs 11-13)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (11,'Extra Sauce',5),(11,'Parmesan',10),(11,'Garlic Bread',15),(11,'Extra Meat',20),
        (12,'Extra Cream',10),(12,'Extra Chicken',20),(12,'Mushrooms',10),(12,'Parmesan',10),(12,'Garlic Bread',15),
        (13,'Extra Pesto',10),(13,'Parmesan',10),(13,'Cherry Tomatoes',10),(13,'Garlic Bread',15),(13,'Pine Nuts',15);");

            // ساندويتش Extras (IDs 14-17)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (14,'Extra Meat',20),(14,'Cheese Slice',10),(14,'Caramelized Onion',10),(14,'Jalapeños',5),(14,'Mushrooms',10),
        (15,'Extra Chicken',15),(15,'Cheese Slice',10),(15,'Coleslaw',5),(15,'Jalapeños',5),(15,'BBQ Sauce',5),
        (16,'Extra Meat',15),(16,'Cheese Slice',10),(16,'Jalapeños',5),(16,'Garlic Sauce',5),(16,'Coleslaw',5),
        (17,'Extra Cheese',10),(17,'Nutella',10),(17,'Banana',5),(17,'Strawberry Sauce',10);");

            // فطاير Extras (IDs 18-20)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (18,'Extra Sujuk',15),(18,'Extra Cheese',10),(18,'Jalapeños',5),
        (19,'Extra Meat',15),(19,'Extra Cheese',10),(19,'Jalapeños',5),(19,'Hot Sauce',5),
        (20,'Extra Cheese',10),(20,'Cherry Sauce',10),(20,'Cream',10);");

            // ══════════════════════════════════════════
            // ── Ingredient Categories ──
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO IngredientCategories(Name) VALUES
        ('Dough & Bases'),
        ('Meat & Chicken'),
        ('Dairy & Cheese'),
        ('Vegetables'),
        ('Sauces & Oils'),
        ('Beverages'),
        ('Other');");

            // ══════════════════════════════════════════
            // ── Ingredients ──
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO Ingredients(CategoryId,Name,Unit,Stock,MinStock,CostPerUnit) VALUES
        (1,'Pizza Dough',         'kg',  30, 10, 10),
        (1,'Pasta',               'kg',  20,  5, 12),
        (1,'Bread Loaf',          'pcs', 40, 10,  5),
        (1,'Crepe Batter',        'kg',  10,  3, 12),
        (2,'Ground Beef',         'kg',  10,  3,120),
        (2,'Chicken Breast',      'kg',  12,  3, 85),
        (2,'Pepperoni',           'kg',   5,  2,150),
        (2,'Sujuk',               'kg',   4,  2,140),
        (2,'Döner Meat',          'kg',   5,  2,130),
        (3,'Mozzarella Cheese',   'kg',  15,  5, 65),
        (3,'Cheddar Cheese',      'kg',   8,  3, 75),
        (3,'Parmesan Cheese',     'kg',   5,  2, 90),
        (3,'Ricotta Cheese',      'kg',   4,  1, 70),
        (3,'Heavy Cream',         'ltr',  6,  2, 30),
        (3,'Eggs',                'pcs', 60, 20,  3),
        (4,'Tomatoes',            'kg',   8,  3,  8),
        (4,'Mushrooms',           'kg',   5,  2, 20),
        (4,'Bell Peppers',        'kg',   4,  2, 15),
        (4,'Spinach',             'kg',   3,  1, 12),
        (4,'Onions',              'kg',   6,  2,  6),
        (4,'Garlic',              'kg',   3,  1, 15),
        (4,'Potatoes',            'kg',  15,  5,  8),
        (5,'Tomato Sauce',        'kg',  10,  3, 12),
        (5,'Pesto Sauce',         'kg',   3,  1, 45),
        (5,'Olive Oil',           'ltr',  5,  2, 40),
        (5,'BBQ Sauce',           'kg',   3,  1, 25),
        (5,'Garlic Sauce',        'kg',   3,  1, 20),
        (6,'Water Bottle',        'pcs', 60, 20,  4),
        (6,'Soft Drinks',         'pcs', 40, 15,  7),
        (7,'Black Olives',        'kg',   3,  1, 25),
        (7,'Jalapeños',           'kg',   2,  1, 30),
        (7,'Cherry',              'kg',   3,  1, 35),
        (7,'Nutella',             'kg',   2,  1, 80),
        (7,'Pine Nuts',           'kg',   2,  1,180);");

            // ══════════════════════════════════════════
            // ── Users ──
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO Users(Username,FullName,PinHash,Role) VALUES
        ('admin',    'Admin',    '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 'admin'),
        ('cashier1', 'Cashier 1','9af15b336e6a9619928537df30b2e6a2376569fcf9d7e773eccede65606529a0', 'cashier');");

            // ══════════════════════════════════════════
            // ── Settings ──
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO Settings(Key,Value) VALUES
        ('ShopName',      'NAPOLI'),
        ('ShopAddress',   'Kafr Shukr'),
        ('ShopPhone',     '01234567890'),
        ('TaxRate',       '0.14'),
        ('ServiceRate',   '0'),
        ('ReceiptFooter', 'Buon appetito e a presto!'),
        ('PrinterName',   ''),
        ('EpsonPort',     'USB');");
        }

        public static void Exec(SqliteConnection conn, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}