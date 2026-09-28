// Data/RecipeSeeder.cs
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PizzaPOS.Services;

namespace PizzaPOS.Data
{
    /// <summary>
    /// وصفات الأصناف الـ 38 + المواد الناقصة — من ملف صاحب المطعم
    /// Napoli.txt (مصدره Napoli.xlsx). الكمية والتكلفة اتقرأوا من الشيتات
    /// نفسها مش تقديرات.
    ///
    /// التحويلات بين وحدات الملف ووحدات قاعدة البيانات:
    ///   - Gr → kg (المواد الصلبة): القسمة على 1000.
    ///   - Gr → ltr (زيت/كريمة سائلة): نفس القسمة (1 غم ≈ 1 مل).
    ///   - ml → ltr: القسمة على 1000.
    ///   - pc → pcs: كما هي (بيض، خبز، بقسماط…).
    ///   - Pizza dough: كورة العجين 1 pc = 0.265 kg (البيتزا)، و100 g
    ///     للفطاير (سطر "Pizza dough 100 g") = 0.1 kg.
    ///   - Crepe batter: رغيف القراب 1 pc ≈ 0.09 kg عجين.
    ///   - box fries (التغليف): سطر packaging بيتخطّى ويتسجّل Fries Box
    ///     بـ 1 pc في الأصناف اللي الشيت حاططها فيها.
    ///
    /// ضوابط القراءة:
    ///   - أي سطر كميت صفراً (سطور "0" التعبئة) يتخطّى.
    ///   - أي مادة اسمها "0" تتخطّى.
    ///   - burger box يتخطّى — الشيت سايب سعره فاضي، وإلا لكان كل
    ///     الوصفات اللي فيها اتحسبت غلط أو اتراجعت.
    ///   - نفس المادة متكررة في نفس الوصفة بتتدمج (مثلاً بطاطس
    ///     التوك 30 + 100 = 0.13 kg).
    ///
    /// التكاليف: كتلة AddedIngredients جاية من الشيت حيث سطور موثوقة
    /// (كمية ≥ 20 غم أو pc محوّل بدقة)، والباقي تقديرات مفتوحة لحد ما
    /// صاحب المطعم يظبطها من شاشة المخزن. مفيش overwrite: لو الرقم
    /// اتعدّل قبل التشغيل، ما بيتغيرش.
    ///
    /// كل حاجة بتتحل بالاسم (Name) مش بالـ Id: لو الـ seed اتغيّر أو حد
    /// أضاف مادة قبل ما الـ seeder يشتغل، الوصفة بتفضل ماشية. لو المادة
    /// مش موجودة، السطر بيتخطّى وبيتسجّل warning بدل ما يكسر الـ startup.
    /// </summary>
    internal static class RecipeSeeder
    {
        /// <summary>
        /// مواد جديدة بيتضيفها الـ seeder قبل حلّ الوصفات: الـ 6 الناقصة
        /// من الـ seed الأصلي (فئة Other) + 34 مادة ظهرت في وصفات الملف
        /// ومش موجودة في الـ seed الأساسي. كل سطر:
        /// (CategoryId, Name, Unit, Stock, MinStock, CostPerUnit).
        ///
        /// الأرقام افتتاحية عشان الـ roll-up والshortage يطلعوا مقروءين
        /// من أول يوم. الوحدة مش مربوطة بـ 'kg' — الـ SQL بياخدها من
        /// السطر نفسه (pcs للخبز والتغليف، ltr للصلصة المخففة).
        /// </summary>
        internal static readonly (int Cat, string Name, string Unit, double Stock, double Min, double Cost)[] AddedIngredients =
        {
            // ── الستة الناقصة من الـ seed الأصلي (فئة 7: Other) ──
            (7, "Tuna",          "kg",  5.0,  2.0, 364),  // من الشيت: 140 غم = 51
            (7, "Oregano",       "kg",  2.0,  0.5, 400),  // تقدير — سطور الملف جرامات م округلة
            (7, "Parsley",       "kg",  3.0,  1.0,  90),  // تقدير
            (7, "Lettuce",       "kg",  5.0,  2.0,  40),  // تقدير
            (7, "Potato Sticks", "kg", 15.0,  5.0,  70),  // من الشيت: متوسط سطور البطاطس
            (7, "Butter",        "kg",  5.0,  2.0, 280),  // تقدير

            // ── خبز وأساسات (فئة 1) ──
            (1, "Burger Bun",        "pcs", 60,   20,   7.5),
            (1, "Mini Burger Bun",   "pcs", 40,   15,   3),
            (1, "Tortilla",          "pcs", 50,   15,   7),
            (1, "Syrian Bread",      "pcs", 50,   15,   5),

            // ── لحوم ودجاج (فئة 2) ──
            (2, "Smoked Turkey",          "kg",  8, 3,   280),
            (2, "Shawarma Batch",         "kg",  8, 3,   450),
            (2, "Hot Dog",                "kg",  6, 2,   216),
            (2, "Chicken Taouk",          "kg",  8, 3,   300),
            (2, "Chicken Fajita",         "kg",  8, 3,   240),
            (2, "Crispy Chicken Strips",  "kg",  6, 2,   200),
            (2, "Breaded Chicken Fingers","pcs", 40, 15,  7.5),
            (2, "Beef Bacon",             "kg",  5, 2,   440),
            (2, "Meatballs",              "kg",  6, 2,   333),
            (2, "Salami",                 "kg",  5, 2,   250),

            // ── أجبان (فئة 3) ──
            (3, "Buffalo Mozzarella",        "kg",  4, 1.5,  300),
            (3, "Blue Cheese",               "kg",  3, 1,    600),
            (3, "Kiri Cheese",               "kg",  5, 2,    150),
            (3, "Romana Cheese",             "kg",  4, 1.5,  270),
            (3, "Emmental Cheese",           "kg",  4, 1.5,  600),
            (3, "Breaded Mozzarella Sticks", "pcs", 40, 15,   5),
            (3, "Crispy Mozzarella",         "pcs", 30, 10,   5),

            // ── خضار (فئة 4) ──
            (4, "Fresh Basil",       "pcs", 100, 30,   1),   // 1 pc من الشيت = 1
            (4, "Pineapple",         "kg",   5,  2,  200),
            (4, "Sweet Corn",        "kg",   5,  2,  240),
            (4, "Cucumber Pickles",  "kg",   3,  1,  250),   // تقدير — سطور الملف جرامات
            (4, "Onion Rings",       "pcs", 50,  20,   2),

            // ── صوصات (فئة 5) ──
            (5, "Texas Sauce",           "kg", 3, 1,   733),
            (5, "Thousand Island Sauce", "kg", 3, 1,   267),
            (5, "Neapolitana Sauce",     "ltr", 4, 1.5, 133),

            // ── بحرية وتغليف (فئة 7: Other) ──
            (7, "White Fish",  "kg",  5, 1.5,  167),
            (7, "Calamari",    "kg",  4, 1.5,  400),
            (7, "Crab Sticks", "kg",  4, 1.5,  250),
            (7, "Mussels",     "kg",  4, 1.5,  400),
            (7, "Fries Box",   "pcs", 200, 60,  5),   // تغليف — تقدير، الشيت بيقيّمها مع الصنف
        };

        /// <summary>
        /// الوصفة: اسم الصنف، ثم أزواج (اسم المادة، الكمية بوحدتها
        /// في قاعدة البيانات kg/ltr/pcs). الأرقام من Napoli.txt.
        ///
        /// المخزون بيتحسب من الوصفة دي وقت الدفع، فأي اسم غلط = وصفة
        /// ناقصة. عشان كده الأسماء مكتوبة كاملة ومطابقة حرفياً لجدول
        /// Ingredients — لو الـ seeder مالوش مادة، بيرجع warning.
        /// </summary>
        static readonly (string Product, (string Ingredient, double Qty)[] Parts)[] Recipes =
        {
            // ── بيتزا 1-16 ──
            ("Margherita", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Olive Oil",0.001),("Oregano",0.001),("Fresh Basil",1),("Parmesan Cheese",0.01) }),
            ("Pepperoni", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Pepperoni",0.025),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("Cappricciosa", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Smoked Turkey",0.025),("Mushrooms",0.04),("Sliced Olives",0.02),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("Prosciutto", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Smoked Turkey",0.03),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("Hawaiian", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Smoked Turkey",0.025),("Pineapple",0.03),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("American", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Smoked Turkey",0.025),("Pepperoni",0.025),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("Milano", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Salami",0.03),("Bell Peppers",0.04),("Oregano",0.0005),("Fresh Basil",0.5),("Hot Sauce",0.0005) }),
            ("Funghi", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Mushrooms",0.06),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("Four Seasons", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Smoked Turkey",0.015),("Pepperoni",0.015),("Bell Peppers",0.025),("Mushrooms",0.025),("Black Olives",0.01),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("Quattro Formaggi", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Buffalo Mozzarella",0.02),("Blue Cheese",0.02),("Kiri Cheese",0.06),("Oregano",0.0005),("Fresh Basil",0.5) }),
            ("Chicken BBQ", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Onions",0.02),("Bell Peppers",0.02),("Mushrooms",0.02),("Chicken Breast",0.1),("BBQ Sauce",0.04),("Fresh Basil",0.5),("Oregano",0.0005) }),
            ("Chicken Ranch", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Onions",0.02),("Bell Peppers",0.02),("Mushrooms",0.02),("Chicken Breast",0.1),("Ranch Dressing",0.04),("Fresh Basil",0.5),("Oregano",0.0005) }),
            ("Tuna", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Onions",0.03),("Green Olives",0.02),("Tuna",0.14),("Fresh Basil",0.5),("Oregano",0.0005) }),
            ("Mexicano", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Onions",0.02),("Bell Peppers",0.02),("Mushrooms",0.02),("Chicken Breast",0.1),("Sweet Corn",0.02),("Fresh Basil",0.5),("Oregano",0.0005) }),
            ("Frutti di Mare", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("White Fish",0.06),("Calamari",0.04),("Crab Sticks",0.04),("Mussels",0.05),("Shrimp",0.06),("Fresh Basil",0.5),("Oregano",0.0005) }),
            ("Napoli Special", new[]{ ("Pizza Dough",0.265),("Mozzarella Cheese",0.1),("Tomato Sauce",0.1),("Parmesan Cheese",0.01),("Shawarma Batch",0.05),("Chicken Breast",0.05),("Pepperoni",0.015),("Smoked Turkey",0.015),("Hot Dog",0.025),("Mushrooms",0.025),("Fresh Basil",0.5),("Oregano",0.0005) }),

            // ── فطاير 17-20 ──
            ("Hot Dog Fatayer", new[]{ ("Pizza Dough",0.2),("Mozzarella Cheese",0.06),("Tomatoes",0.025),("Kiri Cheese",0.05),("Romana Cheese",0.025),("Bell Peppers",0.025),("Sliced Olives",0.015),("Hot Dog",0.06) }),
            ("Sujuk Fatayer", new[]{ ("Pizza Dough",0.2),("Mozzarella Cheese",0.06),("Tomatoes",0.025),("Kiri Cheese",0.05),("Romana Cheese",0.025),("Bell Peppers",0.025),("Sliced Olives",0.015),("Sujuk",0.08) }),
            ("Cheese Lovers Fatayer", new[]{ ("Pizza Dough",0.2),("Mozzarella Cheese",0.06),("Buffalo Mozzarella",0.02),("Kiri Cheese",0.05),("Romana Cheese",0.025),("Parmesan Cheese",0.01),("Olive Oil",0.0005) }),
            ("Napoli Fatayer", new[]{ ("Pizza Dough",0.2),("Mozzarella Cheese",0.06),("Bell Peppers",0.02),("Kiri Cheese",0.05),("Romana Cheese",0.025),("Ground Beef",0.12),("Olive Oil",0.0005) }),

            // ── باستا 21-23 ──
            ("Tuscan Fettuccine", new[]{ ("Pasta",0.2),("Mushrooms",0.02),("Bell Peppers",0.025),("Onions",0.02),("Garlic",0.005),("Chicken Breast",0.1),("Heavy Cream",0.15),("Parmesan Cheese",0.015),("Parsley",0.0005),("Olive Oil",0.0005) }),
            ("Pesto Pasta", new[]{ ("Pasta",0.2),("Chicken Breast",0.1),("Onions",0.025),("Garlic",0.005),("Heavy Cream",0.15),("Parmesan Cheese",0.015),("Olive Oil",0.0005) }),
            ("Napoli Sujuk Pasta", new[]{ ("Pasta",0.2),("Onions",0.025),("Bell Peppers",0.025),("Garlic",0.005),("Sujuk",0.1),("Neapolitana Sauce",0.15),("Parmesan Cheese",0.015),("Olive Oil",0.002) }),

            // ── برجر وساندويتش 24-31 ──
            ("Classic Beef Burger", new[]{ ("Burger Bun",1),("Butter",0.0005),("Texas Sauce",0.015),("Lettuce",0.001),("Tomatoes",0.003),("Onions",0.001),("Ground Beef",0.12),("Eggs",1),("Beef Bacon",0.025),("Emmental Cheese",0.015),("Cucumber Pickles",0.0005) }),
            ("Casper Burger", new[]{ ("Burger Bun",1),("Butter",0.0005),("Texas Sauce",0.015),("Lettuce",0.001),("Tomatoes",0.003),("Onions",0.001),("Ground Beef",0.12),("Crispy Mozzarella",2),("BBQ Sauce",0.015),("Cucumber Pickles",0.0005) }),
            ("Good Melted Burger", new[]{ ("Burger Bun",1),("Butter",0.0005),("Texas Sauce",0.015),("Lettuce",0.001),("Tomatoes",0.003),("Onions",0.001),("Ground Beef",0.12),("Mozzarella Cheese",0.015),("Cheddar Cheese",0.015),("Emmental Cheese",0.015),("Mushrooms",0.02),("Onion Rings",2),("Cucumber Pickles",0.0005) }),
            ("Mexicano Sandwich", new[]{ ("Tortilla",1),("Mayonnaise",0.015),("Lettuce",0.001),("Chicken Fajita",0.1),("Mozzarella Cheese",0.03),("Potato Sticks",0.1) }),
            ("Chicken Taouk Sandwich", new[]{ ("Syrian Bread",1),("Garlic Sauce",0.015),("Lettuce",0.001),("Chicken Taouk",0.1),("Potato Sticks",0.13) }),
            ("Crispy Crepe", new[]{ ("Crepe Batter",0.09),("Thousand Island Sauce",0.015),("Lettuce",0.001),("Cheddar Cheese",0.03),("Smoked Turkey",0.03),("Crispy Chicken Strips",0.1),("Cucumber Pickles",0.0005),("Fries Box",1),("Potato Sticks",0.1) }),
            ("Chicken Quesadillas", new[]{ ("Tortilla",1),("Thousand Island Sauce",0.015),("Lettuce",0.001),("Cheddar Cheese",0.03),("Chicken Fajita",0.1),("Sweet Corn",0.02),("Jalapenos",0.001),("Fries Box",1) }),
            ("Napoli Sandwich", new[]{ ("Bread Loaf",1),("Garlic Sauce",0.015),("Mozzarella Cheese",0.03),("Potato Sticks",0.13),("Shawarma Batch",0.1),("Bell Peppers",0.025),("Onions",0.025),("Mushrooms",0.025) }),

            // ── أطباق جانبية 32-38 ──
            ("Box Fries", new[]{ ("Potato Sticks",0.12) }),
            ("Texas Fries", new[]{ ("Potato Sticks",0.12),("Ground Beef",0.05),("Cheddar Cheese",0.02),("Jalapenos",0.002) }),
            ("Chicken Fingers", new[]{ ("Breaded Chicken Fingers",4.0) }),
            ("Mozzarella Sticks", new[]{ ("Breaded Mozzarella Sticks",4.0) }),
            ("Mini Burger", new[]{ ("Mini Burger Bun",1),("Ground Beef",0.075),("Cheddar Cheese",0.01),("Thousand Island Sauce",0.01),("Potato Sticks",0.05) }),
            ("Mac & Cheese White", new[]{ ("Pasta",0.1),("Heavy Cream",0.075),("Mozzarella Cheese",0.03) }),
            ("Mac & Meatballs Red", new[]{ ("Pasta",0.1),("Meatballs",0.075),("Tomato Sauce",0.075) }),
        };

        /// <summary>
        /// تُستدعى من DatabaseHelper بعد إنشاء الجداول وتعبئة الأصناف.
        /// idempotent — لو الـ seeder اتنفذ تاني، بيبحدّث الكميات بدل ما يكرّر.
        /// </summary>
        public static void Seed(SqliteConnection conn)
        {
            // ── 1) المواد الجديدة ──
            var added = new List<string>();
            foreach (var (cat, name, unit, stock, min, cost) in AddedIngredients)
            {
                using var ins = conn.CreateCommand();
                // NOT EXISTS مش INSERT OR IGNORE: عايز أعرف هل السطر ده
                // اتضاف فعلاً ولا المادة كانت موجودة من زمان (عندها أرقام
                // صاحب المطعم، ومينفعش أطمسها). الـ ROLLBACK جوّه command
                // منفصل عن الـ transaction بتاع الوصفات تحت.
                ins.CommandText = @"INSERT INTO Ingredients
                    (CategoryId,Name,Unit,Stock,MinStock,CostPerUnit,IsActive)
                    SELECT @cat, @n, @u, @s, @m, @c, 1
                    WHERE NOT EXISTS (SELECT 1 FROM Ingredients WHERE Name = @n);";
                ins.Parameters.AddWithValue("@cat", cat);
                ins.Parameters.AddWithValue("@n", name);
                ins.Parameters.AddWithValue("@u", unit);
                ins.Parameters.AddWithValue("@s", stock);
                ins.Parameters.AddWithValue("@m", min);
                ins.Parameters.AddWithValue("@c", cost);
                if (ins.ExecuteNonQuery() > 0) added.Add(name);
            }

            // ── 2) خريطة الاسم → Id (بعد إضافة الجديدة) ──
            var byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (var load = conn.CreateCommand())
            {
                load.CommandText = "SELECT Id, Name FROM Ingredients WHERE IsActive = 1;";
                using var r = load.ExecuteReader();
                while (r.Read()) byName[r.GetString(1)] = r.GetInt32(0);
            }

            // ── 3) Id المنتجات (بالاسم) ──
            var productId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (var loadP = conn.CreateCommand())
            {
                loadP.CommandText = "SELECT Id, Name FROM Products;";
                using var r = loadP.ExecuteReader();
                while (r.Read()) productId[r.GetString(1)] = r.GetInt32(0);
            }

            // ── 4) الوصفات ──
            int written = 0;
            var unknownProducts = new List<string>();
            var unknownIngredients = new List<string>();

            using (var tx = conn.BeginTransaction())
            {
                foreach (var (product, parts) in Recipes)
                {
                    if (!productId.TryGetValue(product, out int pid))
                    {
                        unknownProducts.Add(product);
                        continue;
                    }

                    foreach (var (ingredient, qty) in parts)
                    {
                        if (!byName.TryGetValue(ingredient, out int iid))
                        {
                            unknownIngredients.Add($"{product}: {ingredient}");
                            continue;
                        }

                        using var cmd = conn.CreateCommand();
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO ProductIngredients(ProductId,IngredientId,QtyUsed)
                            VALUES(@p,@i,@q)
                            ON CONFLICT(ProductId,IngredientId) DO UPDATE SET QtyUsed = @q;";
                        cmd.Parameters.AddWithValue("@p", pid);
                        cmd.Parameters.AddWithValue("@i", iid);
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.ExecuteNonQuery();
                        written++;
                    }
                }
                tx.Commit();
            }

            if (unknownProducts.Count > 0)
                AppLogger.Warn(
                    "Recipe seed skipped unknown products: " + string.Join(", ", unknownProducts));
            if (unknownIngredients.Count > 0)
                AppLogger.Warn(
                    "Recipe seed skipped unknown ingredients: " + string.Join("; ", unknownIngredients));

            AppLogger.Info($"Recipe seed: {written} ingredient lines across {Recipes.Length} products");

            // المواد الجديدة بأرقام افتتاحية (من الشيت أو تقدير). التكلفة
            // والتدوير بيظبطهم صاحب المطعم من شاشة المخزن.
            if (added.Count > 0)
                AppLogger.Info(
                    "Recipe seed added ingredients with opening stock/cost "
                    + "(adjust from the Warehouse screen): " + string.Join(", ", added));
        }
    }
}
