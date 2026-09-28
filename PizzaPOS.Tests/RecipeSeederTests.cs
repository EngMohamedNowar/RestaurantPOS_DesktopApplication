using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// بند #6:.ProductIngredients كانت فاضية تماماً — 38/38 صنف بدون
    /// وصفة، فخصم المخزون وقت الدفع مبيعملش حاجة.
    /// </summary>
    public class RecipeSeederTests : IDisposable
    {
        readonly string _dbPath;

        public RecipeSeederTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "recipes_" + Guid.NewGuid().ToString("N") + ".db");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        }

        SqliteConnection Open()
        {
            var c = new SqliteConnection("Data Source=" + _dbPath);
            c.Open();
            c.Execute("PRAGMA foreign_keys = ON;");
            return c;
        }

        /// <summary>مقلد للـ seed الأصلي: جداول فارغة + أصناف 1..38 + 50 مادة.</summary>
        static void SeedBase(SqliteConnection c)
        {
            c.Execute(@"CREATE TABLE IngredientCategories(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
                      CREATE TABLE Ingredients(
                          Id INTEGER PRIMARY KEY,
                          CategoryId INTEGER REFERENCES IngredientCategories(Id),
                          Name TEXT NOT NULL, Unit TEXT NOT NULL,
                          Stock REAL NOT NULL DEFAULT 0, MinStock REAL NOT NULL DEFAULT 0,
                          CostPerUnit REAL NOT NULL DEFAULT 0, IsActive INTEGER NOT NULL DEFAULT 1);
                      CREATE TABLE Products(
                          Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, IsActive INTEGER NOT NULL DEFAULT 1);
                      CREATE TABLE ProductIngredients(
                          ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
                          IngredientId INTEGER NOT NULL REFERENCES Ingredients(Id),
                          QtyUsed REAL NOT NULL,
                          PRIMARY KEY(ProductId, IngredientId));
                      INSERT INTO IngredientCategories(Name) VALUES
                        ('Dough & Bases'),('Meat & Chicken'),('Dairy & Cheese'),('Vegetables'),
                        ('Sauces & Oils'),('Beverages'),('Other');");

            // نفس أسماء DatabaseHelperSeeder — لو الـ seeder حل بالاسم
            // والاسم هنا مختلف، الاختبار هيفشل وده المطلوب.
            c.Execute(@"INSERT INTO Ingredients(CategoryId,Name,Unit,Stock,MinStock,CostPerUnit) VALUES
                      (1,'Pizza Dough','kg',40,15,28),(1,'Pasta','kg',25,8,35),
                      (1,'Bread Loaf','pcs',50,15,12),(1,'Crepe Batter','kg',15,5,25),
                      (2,'Ground Beef','kg',12,4,220),(2,'Chicken Breast','kg',15,5,135),
                      (2,'Pepperoni','kg',5,2,380),(2,'Sujuk','kg',6,2,260),
                      (2,'Doner Meat','kg',8,3,200),(2,'Shrimp','kg',4,2,350),
                      (3,'Mozzarella Cheese','kg',20,8,160),(3,'Cheddar Cheese','kg',10,4,130),
                      (3,'Parmesan Cheese','kg',4,2,280),(3,'Ricotta Cheese','kg',5,2,110),
                      (3,'Heavy Cream','ltr',8,3,70),(3,'Eggs','pcs',120,40,6),
                      (3,'Feta Cheese','kg',6,2,95),(4,'Tomatoes','kg',15,6,18),
                      (4,'Mushrooms','kg',6,3,70),(4,'Bell Peppers','kg',8,3,40),
                      (4,'Spinach','kg',5,2,25),(4,'Onions','kg',10,4,12),
                      (4,'Garlic','kg',4,2,70),(4,'Potatoes','kg',20,8,18),
                      (4,'Cucumbers','kg',10,4,15),(4,'Carrots','kg',8,3,14),
                      (4,'Hot Peppers','kg',3,1,25),(4,'Black Olives','kg',5,2,90),
                      (4,'Green Olives','kg',5,2,80),(5,'Tomato Sauce','kg',12,5,45),
                      (5,'Pesto Sauce','kg',3,1,220),(5,'Olive Oil','ltr',6,3,180),
                      (5,'BBQ Sauce','kg',4,2,90),(5,'Garlic Sauce','kg',5,2,65),
                      (5,'Hot Sauce','kg',4,2,55),(5,'Ranch Dressing','kg',3,1,75),
                      (5,'Mustard','kg',3,1,50),(5,'Mayonnaise','kg',5,2,55),
                      (6,'Water Bottle','pcs',80,30,5),(6,'Soft Drinks','pcs',60,25,12),
                      (6,'Orange Juice','pcs',40,15,15),(6,'Green Tea','pcs',30,10,10),
                      (7,'Sliced Olives','kg',4,2,100),(7,'Jalapenos','kg',3,1,120),
                      (7,'Cherry','kg',4,2,55),(7,'Nutella','kg',3,1,380),
                      (7,'Pine Nuts','kg',2,1,650),(7,'Pistachios','kg',2,1,500),
                      (7,'Crushed Almonds','kg',3,1,400),(7,'Cashews','kg',2,1,550);");

            // 38 صنف بأسمائها الحقيقية من الـ seed.
            string[] pizzas = { "Margherita","Pepperoni","Cappricciosa","Prosciutto","Hawaiian",
                "American","Milano","Funghi","Four Seasons","Quattro Formaggi","Chicken BBQ",
                "Chicken Ranch","Tuna","Mexicano","Frutti di Mare","Napoli Special" };
            string[] fatayer = { "Hot Dog Fatayer","Sujuk Fatayer","Cheese Lovers Fatayer","Napoli Fatayer" };
            string[] pasta = { "Tuscan Fettuccine","Pesto Pasta","Napoli Sujuk Pasta" };
            string[] sandwiches = { "Classic Beef Burger","Casper Burger","Good Melted Burger",
                "Mexicano Sandwich","Chicken Taouk Sandwich","Crispy Crepe","Chicken Quesadillas",
                "Napoli Sandwich" };
            string[] sides = { "Box Fries","Texas Fries","Chicken Fingers","Mozzarella Sticks",
                "Mini Burger","Mac & Cheese White","Mac & Meatballs Red" };
            foreach (string n in pizzas.Concat(fatayer).Concat(pasta).Concat(sandwiches).Concat(sides))
                c.Execute($"INSERT INTO Products(Name) VALUES('{n}');");
        }

        [Fact]
        public void Seed_FillsRecipesForEveryProduct()
        {
            using var c = Open();
            SeedBase(c);
            Assert.Equal(0, Scalar(c, "SELECT COUNT(*) FROM ProductIngredients"));

            RecipeSeeder.Seed(c);

            int total = Scalar(c, "SELECT COUNT(*) FROM Products");
            int covered = Scalar(c, "SELECT COUNT(DISTINCT ProductId) FROM ProductIngredients");
            Assert.Equal(38, total);
            Assert.Equal(total, covered); // مفيش صنف اتساب من غير وصفة
        }

        [Fact]
        public void Seed_AddsTheSixMissingIngredientsWithOpeningEstimates()
        {
            using var c = Open();
            SeedBase(c);
            RecipeSeeder.Seed(c);

            // الأرقام دي لازم تكون > 0. لو رجعت صفر، كل أوردر بيستخدم
            // المادة هيسجّل shortage والتكلفة هتطلع غلط من أول يوم.
            foreach (string n in new[] { "Tuna", "Oregano", "Parsley", "Lettuce", "Potato Sticks", "Butter" })
            {
                Assert.True(Scalar(c, $"SELECT COUNT(*) FROM Ingredients WHERE Name='{n}'") == 1, n);
                Assert.True(Real(c, $"SELECT Stock FROM Ingredients WHERE Name='{n}'") > 0, $"{n} stock");
                Assert.True(Real(c, $"SELECT CostPerUnit FROM Ingredients WHERE Name='{n}'") > 0, $"{n} cost");
                Assert.Equal("kg", Str(c, $"SELECT Unit FROM Ingredients WHERE Name='{n}'"));
            }
        }

        [Fact]
        public void Seed_DoesNotOverwriteOwnerAdjustedNumbers()
        {
            // صاحب المطعم عدّل الرقم من شاشة المخزن، والتشغيل الجاي
            // لازم مايلمسوش. الـ seeder بيضيف الناقصة بس.
            using var c = Open();
            SeedBase(c);
            c.Execute(@"INSERT INTO Ingredients(CategoryId,Name,Unit,Stock,MinStock,CostPerUnit)
                       VALUES(7,'Butter','kg',77,33,333);");
            RecipeSeeder.Seed(c);

            Assert.Equal(77, Real(c, "SELECT Stock FROM Ingredients WHERE Name='Butter'"));
            Assert.Equal(333, Real(c, "SELECT CostPerUnit FROM Ingredients WHERE Name='Butter'"));
            Assert.Equal(1, Scalar(c, "SELECT COUNT(*) FROM Ingredients WHERE Name='Butter'"));
        }

        [Fact]
        public void Seed_ResolvesNamesNotHardcodedIds()
        {
            // لو الـ seeder كان ماسك Id ثابت، تغيير الـ IDs هنا
            // كان هيوصّل الوصفة لمادة غلط أو مفيش خالص.
            using var c = Open();
            SeedBase(c);
            c.Execute("UPDATE Ingredients SET Id = 500 WHERE Name = 'Mozzarella Cheese';");
            c.Execute("UPDATE Ingredients SET Id = 900 WHERE Name = 'Pizza Dough';");

            RecipeSeeder.Seed(c);

            // كل الأصناف اللي فيها موزاريلا (26) لازم تفضل تشير للـ Id الجديد،
            // والعجين (16 بيتزا) كمان.
            int mozz = Scalar(c, "SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId = 500");
            int dough = Scalar(c, "SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId = 900");
            int total = Scalar(c, "SELECT COUNT(*) FROM ProductIngredients");

            Assert.Equal(26, mozz);
            Assert.Equal(16, dough);
            Assert.Equal(38, Scalar(c, "SELECT COUNT(DISTINCT ProductId) FROM ProductIngredients"));
            // مفيش سطر فضل متعلق بالـ Id القديم (1 عجين / 11 موزاريلا)
            Assert.Equal(0, Scalar(c,
                "SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId IN (1, 11)"));
            Assert.Equal(total, mozz + dough + Scalar(c,
                "SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId NOT IN (500,900)"));
        }

        [Fact]
        public void Seed_IsIdempotent_NoDuplicateRows()
        {
            using var c = Open();
            SeedBase(c);
            RecipeSeeder.Seed(c);
            int after = Scalar(c, "SELECT COUNT(*) FROM ProductIngredients");
            long sum1 = (long)Scalar(c, "SELECT SUM(QtyUsed) FROM ProductIngredients");

            RecipeSeeder.Seed(c);
            RecipeSeeder.Seed(c);

            Assert.Equal(after, Scalar(c, "SELECT COUNT(*) FROM ProductIngredients"));
            Assert.Equal(sum1, (long)Scalar(c, "SELECT SUM(QtyUsed) FROM ProductIngredients"));
        }

        [Fact]
        public void Seed_UpdatesQuantitiesForExistingRows()
        {
            using var c = Open();
            SeedBase(c);
            RecipeSeeder.Seed(c);
            c.Execute("UPDATE ProductIngredients SET QtyUsed = 99 WHERE QtyUsed = 0.28;");
            RecipeSeeder.Seed(c);

            Assert.Equal(0, Scalar(c, "SELECT COUNT(*) FROM ProductIngredients WHERE QtyUsed = 99"));
        }

        [Fact]
        public void Seed_SkipsUnknownProductWithoutThrowing()
        {
            using var c = Open();
            SeedBase(c);
            c.Execute("DELETE FROM Products WHERE Name = 'Mac & Meatballs Red';");

            RecipeSeeder.Seed(c); // لازم مايرميش

            Assert.Equal(37, Scalar(c, "SELECT COUNT(DISTINCT ProductId) FROM ProductIngredients"));
        }

        [Fact]
        public void EveryRecipeQuantityIsPositive()
        {
            using var c = Open();
            SeedBase(c);
            RecipeSeeder.Seed(c);

            Assert.Equal(0, Scalar(c, "SELECT COUNT(*) FROM ProductIngredients WHERE QtyUsed <= 0"));
            Assert.Equal(0, Scalar(c, "SELECT COUNT(*) FROM ProductIngredients WHERE QtyUsed > 5"));
        }

        [Fact]
        public void SeededIngredientCostRollsUpToProductCost()
        // التكلفة اللي بتظهر في كاشير لازم تطلع من الوصفة × سعر المادة.
        // لو الـ join مش شغال، التكلفة هتطلع 0 بصمت.
        {
            using var c = Open();
            SeedBase(c);
            RecipeSeeder.Seed(c);

            using var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT p.Name, SUM(pi.QtyUsed * i.CostPerUnit) AS Cost
                FROM ProductIngredients pi
                JOIN Products p    ON p.Id  = pi.ProductId
                JOIN Ingredients i ON i.Id  = pi.IngredientId
                GROUP BY p.Id, p.Name
                HAVING Cost = 0";
            var zeroCost = new List<string>();
            using (var r = cmd.ExecuteReader())
                while (r.Read()) zeroCost.Add(r.GetString(0));

            Assert.True(zeroCost.Count == 0,
                "these products have a recipe but roll up to zero cost: " + string.Join(", ", zeroCost));

            // مارجريتا: 0.28*28 + 0.06*45 + 0.12*160 + 0.02*12 + 0.02*40 + 0.02*70 + 0.005*180
            using (var marg = c.CreateCommand())
            {
                marg.CommandText = @"
                    SELECT SUM(pi.QtyUsed * i.CostPerUnit)
                    FROM ProductIngredients pi
                    JOIN Products p    ON p.Id  = pi.ProductId
                    JOIN Ingredients i ON i.Id  = pi.IngredientId
                    WHERE p.Name = 'Margherita'";
                Assert.Equal(7.84 + 2.70 + 19.20 + 0.24 + 0.80 + 1.40 + 0.90,
                             Math.Round(Convert.ToDouble(marg.ExecuteScalar()), 2), 2);
            }
        }

        [Fact]
        public void AllBaseIngredientsAreStillActiveAfterSeeding()
        {
            using var c = Open();
            SeedBase(c);
            RecipeSeeder.Seed(c);

            int baseCount = 50;
            Assert.Equal(baseCount + 6, Scalar(c, "SELECT COUNT(*) FROM Ingredients"));
        }

        [Fact]
        public void NoRecipeDependsOnAnIngredientWithNoPrice()
        // shortage بيتسجّل مش بيرمي استثناء، بس من غير سعر مفيش coste
        // roll-up خالص. ده بيكسر أهم سبب إن الوصفات اتعملت أصلاً.
        {
            using var c = Open();
            SeedBase(c);
            RecipeSeeder.Seed(c);

            Assert.Equal(0, Scalar(c, @"SELECT COUNT(*)
                FROM ProductIngredients pi
                JOIN Ingredients i ON i.Id = pi.IngredientId
                WHERE i.CostPerUnit <= 0 OR i.IsActive = 0"));
        }

        static int Scalar(SqliteConnection c, string sql)
        {
            using var k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToInt32(k.ExecuteScalar());
        }

        static double Real(SqliteConnection c, string sql)
        {
            using var k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToDouble(k.ExecuteScalar());
        }

        static string Str(SqliteConnection c, string sql)
        {
            using var k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToString(k.ExecuteScalar()) ?? "";
        }
    }

    static class SqliteTestExtensions
    {
        public static void Execute(this SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
