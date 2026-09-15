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
            using var cmd = conn.CreateCommand();
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
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId  INTEGER REFERENCES Categories(Id),
                Name        TEXT    NOT NULL,
                Price       REAL    NOT NULL,
                Cost        REAL    DEFAULT 0,
                Icon        TEXT    DEFAULT '🍕',
                Description TEXT    DEFAULT '',
                IsActive    INTEGER DEFAULT 1);");

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

            // ── Products migration (DB قديمة قبل إضافة الوصف) ──
            TryMigrate(conn, "ALTER TABLE Products ADD COLUMN Description TEXT DEFAULT ''", "Products.Description");

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
            TryMigrate(conn, "ALTER TABLE OrderItems ADD COLUMN SizeExtraPrice REAL DEFAULT 0", "OrderItems.SizeExtraPrice");
            TryMigrate(conn, "ALTER TABLE OrderItems ADD COLUMN ExtrasPrice     REAL DEFAULT 0", "OrderItems.ExtrasPrice");

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

            // ── Offers ──
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Offers (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                Title            TEXT    NOT NULL,
                Description      TEXT    DEFAULT '',
                DiscountPercent  REAL    DEFAULT 0,
                PromoCode        TEXT    DEFAULT '',
                IsActive         INTEGER DEFAULT 1,
                CreatedAt        TEXT    NOT NULL DEFAULT (datetime('now','localtime')));");

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
            // ── Categories — قائمة نابولي الحقيقية ──
            // ══════════════════════════════════════════
            Exec(conn, @"INSERT INTO Categories(Name,Icon,SortOrder) VALUES
        ('بيتزا',              '🍕', 1),
        ('فطاير',              '🥙', 2),
        ('باستا',              '🍝', 3),
        ('برجر وساندويتش',     '🍔', 4),
        ('أطباق جانبية',       '🍟', 5),
        ('قائمة الأطفال',      '🧒', 6);");

            // ══════════════════════════════════════════
            // ── Products (from Napoli.xlsx recipe costing sheet) ──
            // ══════════════════════════════════════════
            // --- بيتزا (Cat 1) → IDs 1-16 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon,Description) VALUES
        (1, 'Margherita', 150, 49.3, '🍕', 'بيتزا كلاسيكية بصلصة الطماطم الطازة، جبنة الموزاريلا، ريحان طازة، أوريجانو وزيت زيتون'),
        (1, 'Pepperoni', 165, 57.4, '🍕', 'بيتزا بصلصة الطماطم، موزاريلا، شرائح ببروني، جبنة بارميزان، أوريجانو وريحان طازة'),
        (1, 'Cappricciosa', 180, 62.8, '🍕', 'بيتزا موزاريلا وصلصة طماطم مع شرائح ديك رومي مدخن، مشروم طازة وزيتون، بارميزان'),
        (1, 'Prosciutto', 165, 56.2, '🍕', 'بيتزا موزاريلا وصلصة طماطم مع شرائح ديك رومي مدخن وجبنة بارميزان'),
        (1, 'Hawaiian', 175, 60.8, '🍕', 'بيتزا موزاريلا وصلصة طماطم مع ديك رومي مدخن وقطع أناناس طازة'),
        (1, 'American', 170, 64.4, '🍕', 'بيتزا موزاريلا وصلصة طماطم مع ديك رومي مدخن وشرائح ببروني وبارميزان'),
        (1, 'Milano', 175, 62.3, '🍕', 'بيتزا موزاريلا وصلصة طماطم مع سلامي، فلفل ألوان مشكل وصوص حار'),
        (1, 'Funghi', 165, 55.8, '🍕', 'بيتزا موزاريلا وصلصة طماطم بكمية مضاعفة من المشروم الطازة وجبنة بارميزان'),
        (1, 'Four Seasons', 185, 73.8, '🍕', 'بيتزا موزاريلا وصلصة طماطم بأربع إضافات: ديك رومي، ببروني، فلفل ألوان، مشروم وزيتون'),
        (1, 'Quattro Formaggi', 200, 74.8, '🍕', 'بيتزا بأربع أنواع جبن: موزاريلا، موزاريلا بافلو، جبنة زرقاء وكيري تشيز'),
        (1, 'Chicken BBQ', 200, 77.8, '🍕', 'بيتزا موزاريلا مع مكعبات دجاج، بصل وفلفل ألوان ومشروم وصوص باربكيو'),
        (1, 'Chicken Ranch', 210, 80.6, '🍕', 'بيتزا موزاريلا مع مكعبات دجاج، بصل وفلفل ألوان ومشروم وصوص رانش'),
        (1, 'Tuna', 200, 101.8, '🍕', 'بيتزا موزاريلا وصلصة طماطم مع تونة، بصل وزيتون أخضر'),
        (1, 'Mexicano', 200, 79.3, '🍕', 'بيتزا موزاريلا مع مكعبات دجاج، بصل وفلفل ألوان ومشروم وذرة حلوة'),
        (1, 'Frutti di Mare', 300, 145.8, '🍕', 'بيتزا مأكولات بحرية فاخرة: سمك أبيض، كاليماري، أعواد كابوريا، بلح البحر وجمبري'),
        (1, 'Napoli Special', 260, 99.7, '🍕', 'بيتزا الشيف الخاصة بالنابولي: شاورما، مكعبات دجاج، ببروني، ديك رومي، هوت دوج ومشروم');");

            // --- فطاير (Cat 2) → IDs 17-20 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon,Description) VALUES
        (2, 'Hot Dog Fatayer', 140, 46.8, '🥙', 'فطيرة مقرمشة بموزاريلا وقطع طماطم وجبنة كيري ورومانا وفلفل أخضر وشرائح هوت دوج'),
        (2, 'Sujuk Fatayer', 160, 63.55, '🥙', 'فطيرة بموزاريلا وجبنة كيري ورومانا وفلفل ألوان وزيتون وسجق بلدي غني'),
        (2, 'Cheese Lovers Fatayer', 140, 47.05, '🥙', 'فطيرة لمحبي الجبن: موزاريلا، جبنة بافلو، كيري، رومانا وبارميزان'),
        (2, 'Napoli Fatayer', 180, 81.55, '🥙', 'فطيرة بموزاريلا وفلفل أخضر وجبنة كيري ورومانا مع لحم مفروم غني');");

            // --- باستا (Cat 3) → IDs 21-23 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon,Description) VALUES
        (3, 'Tuscan Fettuccine', 180, 85, '🍝', 'فيتوتشيني بصوص الكريمة مع مشروم، فلفل ألوان، دجاج وجبنة بارميزان وبقدونس'),
        (3, 'Pesto Pasta', 180, 77, '🍝', 'باستا فوسيلي بصوص البيستو مع مكعبات دجاج وكريمة طازة وبارميزان'),
        (3, 'Napoli Sujuk Pasta', 170, 72.5, '🍝', 'باستا بيني بصلصة نابوليتانا حمراء مع سجق وفلفل أخضر وبارميزان');");

            // --- برجر وساندويتش (Cat 4) → IDs 24-31 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon,Description) VALUES
        (4, 'Classic Beef Burger', 200, 98.5, '🍔', 'برجر لحم بقري بصوص تكساس وجبنة إيمنتال وبيكون بقري وخس وطماطم وبيض'),
        (4, 'Casper Burger', 200, 86, '🍔', 'برجر لحم بقري مع كرسبي موزاريلا وصوص باربكيو وخس وطماطم'),
        (4, 'Good Melted Burger', 200, 102.5, '🍔', 'برجر لحم بقري مغطى بثلاث أنواع جبن (موزاريلا، شيدر، إيمنتال) مع مشروم سوتيه وأنيون رينجز'),
        (4, 'Mexicano Sandwich', 120, 47, '🥪', 'ساندوتش تورتيلا بدجاج فاهيتا وموزاريلا ومايونيز وخس وبطاطس مقرمشة'),
        (4, 'Chicken Taouk Sandwich', 120, 49, '🥙', 'ساندوتش خبز سوري بدجاج طاووق وصوص طحينة وخس وبطاطس مقرمشة'),
        (4, 'Crispy Crepe', 120, 57, '🌯', 'كريب مقرمش بأصابع دجاج كرسبي وجبنة شيدر وديك رومي مدخن وصوص ثاوزند آيلاند'),
        (4, 'Chicken Quesadillas', 120, 62, '🌮', 'تورتيلا بدجاج كيساديا وجبنة شيدر وذرة حلوة وجالابينو مع بطاطس'),
        (4, 'Napoli Sandwich', 120, 63, '🥙', 'ساندوتش خبز دوة بشاورما ولحمة وجبنة موزاريلا وفلفل ألوان وبصل ومشروم');");

            // --- أطباق جانبية (Cat 5) → IDs 32-35 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon,Description) VALUES
        (5, 'Box Fries', 25, 8.4, '🍟', 'بطاطس مقرمشة طازة'),
        (5, 'Texas Fries', 65, 30.4, '🍟', 'بطاطس مقرمشة مغطاة بلحم مفروم وجبنة شيدر وجالابينو'),
        (5, 'Chicken Fingers', 70, 30, '🍗', 'أصابع دجاج مقرمشة'),
        (5, 'Mozzarella Sticks', 60, 20, '🧀', 'أصابع موزاريلا مقرمشة');");

            // --- قائمة الأطفال (Cat 6) → IDs 36-38 ---
            Exec(conn, @"INSERT INTO Products(CategoryId,Name,Price,Cost,Icon,Description) VALUES
        (6, 'Mini Burger', 80, 35, '🍔', 'برجر أطفال صغير بجبنة شيدر وصوص آيلاند وبطاطس مقرمشة'),
        (6, 'Mac & Cheese White', 65, 26, '🍝', 'مكرونة بصوص الجبنة الأبيض والكريمة الطازة'),
        (6, 'Mac & Meatballs Red', 80, 38, '🍝', 'مكرونة بصلصة حمراء مع كرات لحم');");

            // ══════════════════════════════════════════
            // ── ProductSizes ──
            // ══════════════════════════════════════════

            // بيتزا (IDs 1-16) → Small / Medium / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (1,'Small',0,1),(1,'Medium',25,2),(1,'Large',50,3),
        (2,'Small',0,1),(2,'Medium',25,2),(2,'Large',50,3),
        (3,'Small',0,1),(3,'Medium',25,2),(3,'Large',50,3),
        (4,'Small',0,1),(4,'Medium',25,2),(4,'Large',50,3),
        (5,'Small',0,1),(5,'Medium',25,2),(5,'Large',50,3),
        (6,'Small',0,1),(6,'Medium',25,2),(6,'Large',50,3),
        (7,'Small',0,1),(7,'Medium',25,2),(7,'Large',50,3),
        (8,'Small',0,1),(8,'Medium',25,2),(8,'Large',50,3),
        (9,'Small',0,1),(9,'Medium',25,2),(9,'Large',50,3),
        (10,'Small',0,1),(10,'Medium',25,2),(10,'Large',50,3),
        (11,'Small',0,1),(11,'Medium',25,2),(11,'Large',50,3),
        (12,'Small',0,1),(12,'Medium',25,2),(12,'Large',50,3),
        (13,'Small',0,1),(13,'Medium',25,2),(13,'Large',50,3),
        (14,'Small',0,1),(14,'Medium',25,2),(14,'Large',50,3),
        (15,'Small',0,1),(15,'Medium',25,2),(15,'Large',50,3),
        (16,'Small',0,1),(16,'Medium',25,2),(16,'Large',50,3);");

            // باستا (IDs 21-23) → Regular / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (21,'Regular',0,1),(21,'Large',20,2),
        (22,'Regular',0,1),(22,'Large',20,2),
        (23,'Regular',0,1),(23,'Large',20,2);");

            // برجر وساندويتش (IDs 24-31) → Regular / Large
            Exec(conn, @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder) VALUES
        (24,'Regular',0,1),(24,'Large',15,2),
        (25,'Regular',0,1),(25,'Large',15,2),
        (26,'Regular',0,1),(26,'Large',15,2),
        (27,'Regular',0,1),(27,'Large',15,2),
        (28,'Regular',0,1),(28,'Large',15,2),
        (29,'Regular',0,1),(29,'Large',15,2),
        (30,'Regular',0,1),(30,'Large',15,2),
        (31,'Regular',0,1),(31,'Large',15,2);");

            // فطاير، أطباق جانبية، قائمة الأطفال → بدون أحجام (حجم واحد)

            // ══════════════════════════════════════════
            // ── ProductExtras (إضافات) ──
            // ══════════════════════════════════════════

            // بيتزا Extras (IDs 1-16)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (1,'Extra Cheese',15),(1,'Mushrooms',10),(1,'Black Olives',10),(1,'Fresh Basil',5),
        (2,'Extra Cheese',15),(2,'Extra Pepperoni',15),(2,'Jalapenos',10),(2,'Black Olives',10),
        (3,'Extra Cheese',15),(3,'Mushrooms',10),(3,'Black Olives',10),
        (4,'Extra Cheese',15),(4,'Mushrooms',10),
        (5,'Extra Cheese',15),(5,'Mushrooms',10),
        (6,'Extra Cheese',15),(6,'Extra Pepperoni',15),(6,'Black Olives',10),
        (7,'Extra Cheese',15),(7,'Hot Sauce',5),(7,'Mixpeppers',10),
        (8,'Extra Cheese',15),(8,'Mushrooms',10),
        (9,'Extra Cheese',15),(9,'Mushrooms',10),(9,'Black Olives',10),
        (10,'Blue Cheese',20),(10,'Extra Mozzarella',15),
        (11,'Extra Cheese',15),(11,'Extra Chicken',20),(11,'BBQ Sauce',5),
        (12,'Extra Cheese',15),(12,'Extra Chicken',20),(12,'Ranch Sauce',5),
        (13,'Extra Cheese',15),(13,'Green Olives',10),
        (14,'Extra Cheese',15),(14,'Extra Chicken',20),(14,'Sweet Corn',10),
        (15,'Extra Shrimp',30),(15,'Extra Cheese',15),
        (16,'Extra Cheese',15),(16,'Extra Pepperoni',15),(16,'Hot Sauce',5);");

            // فطاير Extras (IDs 17-20)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (17,'Extra Cheese',10),(17,'Hot Dog Slices',10),
        (18,'Extra Sujuk',15),(18,'Extra Cheese',10),
        (19,'Extra Cheese',15),(19,'Blue Cheese',15),
        (20,'Extra Meat',15),(20,'Extra Cheese',10);");

            // باستا Extras (IDs 21-23)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (21,'Extra Chicken',15),(21,'Extra Cream',10),
        (22,'Extra Chicken',15),(22,'Extra Pesto',10),
        (23,'Extra Sujuk',15),(23,'Extra Cheese',10);");

            // برجر وساندويتش Extras (IDs 24-31)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (24,'Extra Cheese',10),(24,'Extra Beef Patty',25),(24,'Beef Bacon',15),
        (25,'Extra Cheese',10),(25,'Extra Beef Patty',25),
        (26,'Extra Cheese',10),(26,'Extra Beef Patty',25),(26,'Onion Rings',10),
        (27,'Extra Chicken',15),(27,'Extra Cheese',10),
        (28,'Extra Chicken',15),(28,'Extra Sauce',5),
        (29,'Extra Chicken Fingers',15),(29,'Extra Cheese',10),
        (30,'Extra Chicken',15),(30,'Jalapenos',5),
        (31,'Extra Meat',15),(31,'Extra Cheese',10);");

            // أطباق جانبية Extras (IDs 32-35)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (32,'Cheese Sauce',10),(32,'Ketchup',0),
        (33,'Extra Cheese',10),(33,'Jalapenos',5),
        (34,'BBQ Sauce',5),(34,'Ranch Sauce',5),
        (35,'Marinara Sauce',5);");

            // قائمة الأطفال Extras (IDs 36-38)
            Exec(conn, @"INSERT INTO ProductExtras(ProductId,Name,Price) VALUES
        (36,'Extra Cheese',8),
        (37,'Extra Cheese',8),
        (38,'Extra Cheese',8);");

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
            // مرجع عام للخامات؛ التكلفة الفعلية لكل منتج محسوبة مباشرة من
            // شيت التسعير (Napoli.xlsx) في عمود Cost أعلاه، فمفيش ربط
            // ProductIngredients لكل صنف دلوقتي — ده جاهز لو حبيت تفعّل
            // حساب التكلفة أوتوماتيك من المكونات لاحقًا.
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

            // ══════════════════════════════════════════
            // ── حساب التكاليف تلقائياً (بدون تأثير — التكلفة متسجلة
            //    مباشرة في Products.Cost من شيت التسعير الأصلي) ──
            // ══════════════════════════════════════════
            CalcProductCosts(conn);
        }

        static void CalcProductCosts(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Products WHERE IsActive=1";
            var ids = new List<int>();
            using (var r = cmd.ExecuteReader())
                while (r.Read()) ids.Add(r.GetInt32(0));

            foreach (var pid in ids)
            {
                var costCmd = conn.CreateCommand();
                costCmd.CommandText = @"SELECT COALESCE(SUM(pi.QtyUsed * i.CostPerUnit), 0)
                    FROM ProductIngredients pi
                    JOIN Ingredients i ON i.Id=pi.IngredientId
                    WHERE pi.ProductId=@pid";
                costCmd.Parameters.AddWithValue("@pid", pid);
                double cost = Convert.ToDouble(costCmd.ExecuteScalar() ?? 0.0);

                if (cost > 0)
                {
                    var upd = conn.CreateCommand();
                    upd.CommandText = "UPDATE Products SET Cost=@co WHERE Id=@id";
                    upd.Parameters.AddWithValue("@co", cost);
                    upd.Parameters.AddWithValue("@id", pid);
                    upd.ExecuteNonQuery();
                }
            }
        }

        public static void Exec(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}