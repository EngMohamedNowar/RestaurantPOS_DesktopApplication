// Data/DatabaseHelper.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PizzaPOS.Services;

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

        public static async Task<SqliteConnection> OpenAsync()
        {
            var c = new SqliteConnection(CS);
            await c.OpenAsync();
            return c;
        }

        public static async Task ExecAsync(SqliteConnection conn, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        static void TryMigrate(SqliteConnection conn, string sql, string description)
        {
            try { Exec(conn, sql); }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.ErrorCode == 447)
            {
                // duplicate column - مفيش مشكلة
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                AppLogger.Warn($"Migration '{description}' failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Migration '{description}' unexpected error: {ex.Message}");
            }
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
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN ServiceCharge   REAL    DEFAULT 0", "Orders.ServiceCharge");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN CustomerId      INTEGER DEFAULT 0", "Orders.CustomerId");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN CustomerName    TEXT    DEFAULT ''", "Orders.CustomerName");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN CustomerPhone   TEXT    DEFAULT ''", "Orders.CustomerPhone");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN DeliveryAddress TEXT    DEFAULT ''", "Orders.DeliveryAddress");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN DriverId        INTEGER DEFAULT 0", "Orders.DriverId");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN DriverName      TEXT    DEFAULT ''", "Orders.DriverName");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN DeliveryFee     REAL    DEFAULT 0", "Orders.DeliveryFee");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN DeliveryStatus  TEXT    DEFAULT ''", "Orders.DeliveryStatus");
            TryMigrate(conn, "ALTER TABLE Orders ADD COLUMN Status TEXT DEFAULT 'new'", "Orders.Status");
            TryMigrate(conn, "UPDATE Orders SET Status='completed' WHERE Status IS NULL OR Status=''", "Orders.Status cleanup");

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
            TryMigrate(conn, "ALTER TABLE OrderItems ADD COLUMN SizeName   TEXT", "OrderItems.SizeName");
            TryMigrate(conn, "ALTER TABLE OrderItems ADD COLUMN ExtrasNote TEXT", "OrderItems.ExtrasNote");

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

            // ── Loyalty Points migration ──
            TryMigrate(conn, "ALTER TABLE Customers ADD COLUMN LoyaltyPoints INTEGER DEFAULT 0", "Customers.LoyaltyPoints");

            Seed(conn);
            EnsureDefaultUsers(conn);
        }

        static void EnsureDefaultUsers(SqliteConnection conn)
        {
            var chk = conn.CreateCommand();
            chk.CommandText = "SELECT COUNT(*) FROM Users";
            if ((long)chk.ExecuteScalar()! > 0) return;

            Exec(conn, @"INSERT INTO Users(Username,FullName,PinHash,Role) VALUES
        ('admin',    'Admin',    '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 'admin'),
        ('cashier1', 'Cashier 1','9af15b336e6a9619928537df30b2e6a2376569fcf9d7e773eccede65606529a0', 'cashier');");
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
        (1, 'Margherita',       99,  0, '🍕'),
        (1, 'Pepperoni',        120, 0, '🍕'),
        (1, 'Quattro Formaggi', 135, 0, '🍕'),
        (1, 'Pollo Special',    115, 0, '🍕'),
        (1, 'Napoli Doner',     125, 0, '🍕');");

            // --- بيتزا Napoletana (Cat 2) → IDs 6-10 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (2, 'Margherita Napoletana',       115,  0, '🍕'),
        (2, 'Pepperoni Napoletana',        140, 0, '🍕'),
        (2, 'Quattro Formaggi Napoletana', 155, 0, '🍕'),
        (2, 'Pollo Special Napoletana',    135, 0, '🍕'),
        (2, 'Napoli Doner Napoletana',     145, 0, '🍕');");

            // --- باستا (Cat 3) → IDs 11-13 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (3, 'Napoli Pasta',  89,  0, '🍝'),
        (3, 'Tuscan Pasta',  99,  0, '🍝'),
        (3, 'Pesto Pasta',   92,  0, '🍝');");

            // --- ساندويتش (Cat 4) → IDs 14-17 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (4, 'Beef Burger',          75,  0, '🍔'),
        (4, 'Chicken Fajita Sand.', 65,  0, '🥪'),
        (4, 'Shawarma Sand.',       60,  0, '🥙'),
        (4, 'Crepe Sand.',          55,  0, '🥪');");

            // --- فطاير (Cat 5) → IDs 18-20 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES
        (5, 'Fatayer Sujuk',       50,  0, '🥙'),
        (5, 'Fatayer Minced Beef', 50,  0, '🥙'),
        (5, 'Cherry Napoli',       55,  0, '🥙');");

            // ══════════════════════════════════════════
            // ── ProductSizes ──
            // ══════════════════════════════════════════

            // بيتزا (IDs 1-5) → Small / Medium / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (1,'Small',0,1),(1,'Medium',25,2),(1,'Large',50,3),
        (2,'Small',0,1),(2,'Medium',25,2),(2,'Large',50,3),
        (3,'Small',0,1),(3,'Medium',25,2),(3,'Large',50,3),
        (4,'Small',0,1),(4,'Medium',25,2),(4,'Large',50,3),
        (5,'Small',0,1),(5,'Medium',25,2),(5,'Large',50,3);");

            // بيتزا Napoletana (IDs 6-10) → Small / Medium / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (6, 'Small',0,1),(6, 'Medium',25,2),(6, 'Large',50,3),
        (7, 'Small',0,1),(7, 'Medium',25,2),(7, 'Large',50,3),
        (8, 'Small',0,1),(8, 'Medium',25,2),(8, 'Large',50,3),
        (9, 'Small',0,1),(9, 'Medium',25,2),(9, 'Large',50,3),
        (10,'Small',0,1),(10,'Medium',25,2),(10,'Large',50,3);");

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
        (1,'Extra Cheese',15),(1,'Extra Pepperoni',15),(1,'Mushrooms',10),(1,'Black Olives',10),(1,'Jalapenos',10),(1,'Fresh Basil',5),
        (2,'Extra Cheese',15),(2,'Extra Pepperoni',15),(2,'Mushrooms',10),(2,'Black Olives',10),(2,'Jalapenos',10),(2,'Hot Sauce',5),
        (3,'Extra Cheese',15),(3,'Blue Cheese',20),(3,'Mushrooms',10),(3,'Truffle Oil',25),(3,'Jalapenos',10),
        (4,'Extra Cheese',15),(4,'Extra Chicken',20),(4,'Mushrooms',10),(4,'Sweet Corn',10),(4,'BBQ Sauce',5),
        (5,'Extra Cheese',15),(5,'Extra Doner',25),(5,'Jalapenos',10),(5,'Hot Sauce',5),(5,'Black Olives',10);");

            // بيتزا Napoletana Extras (IDs 6-10)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (6, 'Extra Cheese',15),(6, 'Extra Pepperoni',15),(6, 'Mushrooms',10),(6, 'Black Olives',10),(6, 'Jalapenos',10),(6, 'Fresh Basil',5),
        (7, 'Extra Cheese',15),(7, 'Extra Pepperoni',15),(7, 'Mushrooms',10),(7, 'Black Olives',10),(7, 'Jalapenos',10),(7, 'Hot Sauce',5),
        (8, 'Extra Cheese',15),(8, 'Blue Cheese',20),(8, 'Mushrooms',10),(8, 'Truffle Oil',25),(8, 'Jalapenos',10),
        (9, 'Extra Cheese',15),(9, 'Extra Chicken',20),(9, 'Mushrooms',10),(9, 'Sweet Corn',10),(9, 'BBQ Sauce',5),
        (10,'Extra Cheese',15),(10,'Extra Doner',25),(10,'Jalapenos',10),(10,'Hot Sauce',5),(10,'Black Olives',10);");

            // باستا Extras (IDs 11-13)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (11,'Extra Sauce',5),(11,'Parmesan',10),(11,'Garlic Bread',15),(11,'Extra Meat',20),
        (12,'Extra Cream',10),(12,'Extra Chicken',20),(12,'Mushrooms',10),(12,'Parmesan',10),(12,'Garlic Bread',15),
        (13,'Extra Pesto',10),(13,'Parmesan',10),(13,'Cherry Tomatoes',10),(13,'Garlic Bread',15),(13,'Pine Nuts',15);");

            // ساندويتش Extras (IDs 14-17)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (14,'Extra Meat',15),(14,'Cheese Slice',10),(14,'Caramelized Onion',10),(14,'Jalapenos',5),(14,'Mushrooms',10),
        (15,'Extra Chicken',15),(15,'Cheese Slice',10),(15,'Coleslaw',5),(15,'Jalapenos',5),(15,'BBQ Sauce',5),
        (16,'Extra Meat',15),(16,'Cheese Slice',10),(16,'Jalapenos',5),(16,'Garlic Sauce',5),(16,'Coleslaw',5),
        (17,'Extra Cheese',10),(17,'Nutella',10),(17,'Banana',5),(17,'Strawberry Sauce',10);");

            // فطاير Extras (IDs 18-20)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (18,'Extra Sujuk',15),(18,'Extra Cheese',10),(18,'Jalapenos',5),
        (19,'Extra Meat',15),(19,'Extra Cheese',10),(19,'Jalapenos',5),(19,'Hot Sauce',5),
        (20,'Extra Cherry',10),(20,'Extra Cheese',10),(20,'Cream',10);");

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
            // ── Ingredients — Egypt 2025/2026 wholesale prices (EGP/unit) ──
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO Ingredients(CategoryId,Name,Unit,Stock,MinStock,CostPerUnit) VALUES
        (1,'Pizza Dough',          'kg',   40, 15, 28),
        (1,'Pasta',                'kg',   25,  8, 35),
        (1,'Bread Loaf',           'pcs',  50, 15, 12),
        (1,'Crepe Batter',         'kg',   15,  5, 25),
        (2,'Ground Beef',          'kg',   12,  4,220),
        (2,'Chicken Breast',       'kg',   15,  5,135),
        (2,'Pepperoni',            'kg',    5,  2,380),
        (2,'Sujuk',                'kg',    6,  2,260),
        (2,'Doner Meat',           'kg',    8,  3,200),
        (2,'Shrimp',               'kg',    4,  2,350),
        (3,'Mozzarella Cheese',    'kg',   20,  8,160),
        (3,'Cheddar Cheese',       'kg',   10,  4,130),
        (3,'Parmesan Cheese',      'kg',    4,  2,280),
        (3,'Ricotta Cheese',       'kg',    5,  2,110),
        (3,'Heavy Cream',          'ltr',   8,  3, 70),
        (3,'Eggs',                 'pcs', 120, 40,  6),
        (3,'Feta Cheese',          'kg',    6,  2, 95),
        (4,'Tomatoes',             'kg',   15,  6, 18),
        (4,'Mushrooms',            'kg',    6,  3, 70),
        (4,'Bell Peppers',         'kg',    8,  3, 40),
        (4,'Spinach',              'kg',    5,  2, 25),
        (4,'Onions',               'kg',   10,  4, 12),
        (4,'Garlic',               'kg',    4,  2, 70),
        (4,'Potatoes',             'kg',   20,  8, 18),
        (4,'Cucumbers',            'kg',   10,  4, 15),
        (4,'Carrots',              'kg',    8,  3, 14),
        (4,'Hot Peppers',          'kg',    3,  1, 25),
        (4,'Black Olives',         'kg',    5,  2, 90),
        (4,'Green Olives',         'kg',    5,  2, 80),
        (5,'Tomato Sauce',         'kg',   12,  5, 45),
        (5,'Pesto Sauce',          'kg',    3,  1,220),
        (5,'Olive Oil',            'ltr',   6,  3,180),
        (5,'BBQ Sauce',            'kg',    4,  2, 90),
        (5,'Garlic Sauce',         'kg',    5,  2, 65),
        (5,'Hot Sauce',            'kg',    4,  2, 55),
        (5,'Ranch Dressing',       'kg',    3,  1, 75),
        (5,'Mustard',              'kg',    3,  1, 50),
        (5,'Mayonnaise',           'kg',    5,  2, 55),
        (6,'Water Bottle',         'pcs',  80, 30,  5),
        (6,'Soft Drinks',          'pcs',  60, 25, 12),
        (6,'Orange Juice',         'pcs',  40, 15, 15),
        (6,'Green Tea',            'pcs',  30, 10, 10),
        (7,'Sliced Olives',        'kg',    4,  2,100),
        (7,'Jalapenos',            'kg',    3,  1,120),
        (7,'Cherry',               'kg',    4,  2, 55),
        (7,'Nutella',              'kg',    3,  1,380),
        (7,'Pine Nuts',            'kg',    2,  1,650),
        (7,'Pistachios',           'kg',    2,  1,500),
        (7,'Crushed Almonds',      'kg',    3,  1,400),
        (7,'Cashews',              'kg',    2,  1,550);");

            // ══════════════════════════════════════════
            // ── ProductIngredients (recipes) ──
            // Ingredient IDs: 1=PizzaDough 2=Pasta 3=BreadLoaf 4=CrepeBatter
            //   5=GroundBeef 6=ChickenBreast 7=Pepperoni 8=Sujuk 9=DonerMeat 10=Shrimp
            //   11=Mozzarella 12=Cheddar 13=Parmesan 14=Ricotta 15=HeavyCream 16=Eggs 17=Feta
            //   18=Tomatoes 19=Mushrooms 20=BellPeppers 21=Spinach 22=Onions 23=Garlic
            //   24=Potatoes 25=Cucumbers 26=Carrots 27=HotPeppers 28=BlackOlives 29=GreenOlives
            //   30=TomatoSauce 31=PestoSauce 32=OliveOil 33=BBQSauce 34=GarlicSauce
            //   35=HotSauce 36=Ranch 37=Mustard 38=Mayo
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO ProductIngredients(ProductId,IngredientId,QtyUsed) VALUES
        -- 1 Margherita
        (1,1,0.30),(1,11,0.15),(1,30,0.08),(1,32,0.02),
        -- 2 Pepperoni
        (2,1,0.30),(2,11,0.15),(2,30,0.08),(2,7,0.06),(2,32,0.02),
        -- 3 Quattro Formaggi
        (3,1,0.30),(3,11,0.12),(3,12,0.08),(3,13,0.04),(3,14,0.06),(3,32,0.01),
        -- 4 Pollo Special
        (4,1,0.30),(4,11,0.15),(4,30,0.08),(4,6,0.12),(4,19,0.06),(4,20,0.04),(4,32,0.02),
        -- 5 Napoli Doner
        (5,1,0.30),(5,11,0.12),(5,9,0.10),(5,34,0.04),(5,27,0.02),(5,32,0.02),
        -- 6 Margherita Napoletana
        (6,1,0.35),(6,11,0.18),(6,30,0.10),(6,32,0.02),
        -- 7 Pepperoni Napoletana
        (7,1,0.35),(7,11,0.18),(7,30,0.10),(7,7,0.08),(7,32,0.02),
        -- 8 Quattro Formaggi Napoletana
        (8,1,0.35),(8,11,0.15),(8,12,0.10),(8,13,0.05),(8,14,0.08),(8,32,0.02),
        -- 9 Pollo Special Napoletana
        (9,1,0.35),(9,11,0.18),(9,30,0.10),(9,6,0.15),(9,19,0.08),(9,20,0.05),(9,32,0.02),
        -- 10 Napoli Doner Napoletana
        (10,1,0.35),(10,11,0.15),(10,9,0.12),(10,34,0.05),(10,27,0.02),(10,32,0.02),
        -- 11 Napoli Pasta
        (11,2,0.25),(11,5,0.12),(11,30,0.08),(11,23,0.01),(11,32,0.02),
        -- 12 Tuscan Pasta
        (12,2,0.25),(12,6,0.12),(12,15,0.08),(12,19,0.06),(12,13,0.03),(12,32,0.02),
        -- 13 Pesto Pasta
        (13,2,0.25),(13,31,0.06),(13,13,0.04),(13,47,0.01),(13,32,0.02),
        -- 14 Beef Burger
        (14,3,1),(14,5,0.15),(14,12,0.04),(14,18,0.04),(14,22,0.03),(14,37,0.01),
        -- 15 Chicken Fajita Sand.
        (15,3,1),(15,6,0.12),(15,20,0.06),(15,22,0.04),(15,12,0.03),(15,35,0.01),
        -- 16 Shawarma Sand.
        (16,3,1),(16,9,0.10),(16,34,0.04),(16,18,0.03),(16,25,0.03),
        -- 17 Crepe Sand. (Nutella)
        (17,4,0.15),(17,46,0.05),(17,16,1),
        -- 18 Fatayer Sujuk
        (18,1,0.15),(18,8,0.08),(18,11,0.06),
        -- 19 Fatayer Minced Beef
        (19,1,0.15),(19,5,0.10),(19,22,0.03),
        -- 20 Cherry Napoli
        (20,4,0.15),(20,45,0.08),(20,15,0.05),(20,16,1);");

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
        ('ProfitMargin',  '50'),
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