// Data/RecipeSeeder.cs
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PizzaPOS.Services;

namespace PizzaPOS.Data
{
    /// <summary>
    /// وصفات الأصناف الـ 38.
    ///
    /// الوضع قبل الشغل ده: 38/38 صنف بدون سطر واحد في
    /// ProductIngredients. معناه إن خصم المخزون في الدفع مبيعملش حاجة
    /// عملياً — DeductForOrder بيلاقي الوصفة فاضية وبيعدّي من غير خصم —
    /// فمخزون المواد بيفضل زي ما هو والمخزون السالب في الوردية مستحيل
    /// يظهر. الشاشة الرئيسية بتعرض أرقام مش بتتحدث.
    ///
    /// الكميات تقديرات معقولة لمطعم بيتزا (عجين ربع كيلو، موزاريلا
    /// 120gm) مش أرقام مطبخية دقيقة. الغرض إن النظام يشتغل صح؛
    /// الدقة بيرجّعها صاحب المطعم من شاشة المنتج/المخزن.
    ///
    /// كل حاجة بتتحل بالاسم (Name) مش بالـ Id: لو الـ seed اتغيّر أو حد
    /// أضاف مادة قبل ما الـ seeder يشتغل، الوصفة بتفضل ماشية. لو المادة
    /// مش موجودة، السطر بيتخطّى وبيتسجّل warning بدل ما يكسر الـ startup.
    /// </summary>
    internal static class RecipeSeeder
    {
        /// <summary>
        /// مواد ناقصة من الـ seed الأصلي، لازم تتضاف الأول قبل ما الوصفات
        /// تتحل. كل واحدة في فئة "Other" (Id 7).
        ///
        /// الأرقام دي افتتاحية تقديرية عشان الأرقام تطلع مقروءة من أول يوم.
        /// لو خلّيناها صفر، كل أوردر بيستخدمها هيسجّل shortage والتكلفة
        /// هتطلع غلط. مقياس التكلفة نفس الـ seed الأصلي (جنيه للوحدة):
        /// تونة معلبة 220، أوريغانو مجفف 400، بقدونس 90، خس 40،
        /// بطاطس قلي مجمدة 55، زبدة 280.
        ///
        /// مفيش overwrite: لو صاحب المطعم دخل الأرقام الحقيقية، التشغيل
        /// الجاي مايلمسهاش. التصحيح بيتم من شاشة المخزن.
        /// </summary>
        static readonly (string Name, double Stock, double Min, double Cost)[] MissingIngredients =
        {
            ("Tuna",          5.0,  2.0, 220),  // تونة محفوظة
            ("Oregano",       2.0,  0.5, 400),  // أوريغانو مجفف
            ("Parsley",       3.0,  1.0,  90),  // بقدونس
            ("Lettuce",       5.0,  2.0,  40),  // خس
            ("Potato Sticks",15.0,  5.0,  55),  // بطاطس قلي مجمدة
            ("Butter",        5.0,  2.0, 280),  // زبدة
        };

        /// <summary>
        /// الوصفة: اسم الصنف، ثم أزواج (اسم المادة، الكمية بوحدتها).
        ///
        /// المخزون بيتحسب من الوصفة دي وقت الدفع، فأي اسم غلط = وصفة
        /// ناقصة. عشان كده الأسماء مكتوبة كاملة ومطابقة حرفياً لجدول
        /// Ingredients — لو الـ seeder مالوش مادة، بيرجع warning.
        /// </summary>
        static readonly (string Product, (string Ingredient, double Qty)[] Parts)[] Recipes =
        {
            // ── بيتزا 1-16 ──
            ("Margherita", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Onions",0.02),("Bell Peppers",0.02),("Mushrooms",0.02),("Olive Oil",0.005) }),
            ("Pepperoni", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Pepperoni",0.07),("Olive Oil",0.005) }),
            ("Cappricciosa", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Mushrooms",0.04),("Onions",0.02),("Bell Peppers",0.02),("Sliced Olives",0.02),
                ("Black Olives",0.01),("Olive Oil",0.005) }),
            ("Prosciutto", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Sujuk",0.06),("Onions",0.02),("Bell Peppers",0.02),("Olive Oil",0.005) }),
            ("Hawaiian", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Tuna",0.05),("Onions",0.02),("Bell Peppers",0.02),("Pine Nuts",0.01) }),
            ("American", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Ground Beef",0.08),("Onions",0.02),("Cheddar Cheese",0.03),("Mustard",0.01),
                ("Mayonnaise",0.01) }),
            ("Milano", new[]{ ("Pizza Dough",0.28),("Mozzarella Cheese",0.14),("Heavy Cream",0.03),
                ("Ricotta Cheese",0.04),("Parmesan Cheese",0.02),("Olive Oil",0.005) }),
            ("Funghi", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.13),
                ("Mushrooms",0.05),("Onions",0.02),("Olive Oil",0.005),("Oregano",0.001) }),
            ("Four Seasons", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.13),
                ("Pepperoni",0.05),("Mushrooms",0.03),("Onions",0.02),("Bell Peppers",0.02),
                ("Sliced Olives",0.02),("Olive Oil",0.005) }),
            ("Quattro Formaggi", new[]{ ("Pizza Dough",0.28),("Heavy Cream",0.04),("Mozzarella Cheese",0.12),
                ("Cheddar Cheese",0.04),("Parmesan Cheese",0.02),("Ricotta Cheese",0.04),
                ("Feta Cheese",0.02),("Olive Oil",0.005) }),
            ("Chicken BBQ", new[]{ ("Pizza Dough",0.28),("BBQ Sauce",0.05),("Mozzarella Cheese",0.12),
                ("Chicken Breast",0.09),("Onions",0.02),("Bell Peppers",0.02) }),
            ("Chicken Ranch", new[]{ ("Pizza Dough",0.28),("Ranch Dressing",0.05),("Mozzarella Cheese",0.12),
                ("Chicken Breast",0.09),("Onions",0.02),("Bell Peppers",0.02) }),
            ("Tuna", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Tuna",0.07),("Onions",0.02),("Bell Peppers",0.02),("Olive Oil",0.005) }),
            ("Mexicano", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Ground Beef",0.08),("Cheddar Cheese",0.03),("Onions",0.02),("Jalapenos",0.015),
                ("Hot Sauce",0.01),("Olive Oil",0.005) }),
            ("Frutti di Mare", new[]{ ("Pizza Dough",0.28),("Tomato Sauce",0.06),("Mozzarella Cheese",0.12),
                ("Shrimp",0.08),("Mushrooms",0.03),("Onions",0.02),("Bell Peppers",0.02),
                ("Olive Oil",0.005),("Parsley",0.005) }),
            ("Napoli Special", new[]{ ("Pizza Dough",0.30),("Tomato Sauce",0.07),("Mozzarella Cheese",0.14),
                ("Sujuk",0.05),("Pepperoni",0.05),("Ground Beef",0.04),("Mushrooms",0.03),
                ("Bell Peppers",0.02),("Onions",0.02),("Garlic",0.005),("Olive Oil",0.005) }),

            // ── فطاير 17-20 — رغيف + fillings ──
            ("Hot Dog Fatayer", new[]{ ("Bread Loaf",1.0),("Sujuk",0.06),("Cheddar Cheese",0.03),
                ("Tomato Sauce",0.03),("Onions",0.01),("Mayonnaise",0.01),("Hot Sauce",0.01) }),
            ("Sujuk Fatayer", new[]{ ("Bread Loaf",1.0),("Sujuk",0.06),("Mozzarella Cheese",0.04),
                ("Onions",0.01),("Bell Peppers",0.01),("Olive Oil",0.005) }),
            ("Cheese Lovers Fatayer", new[]{ ("Bread Loaf",1.0),("Mozzarella Cheese",0.05),
                ("Cheddar Cheese",0.04),("Heavy Cream",0.02),("Olive Oil",0.005) }),
            ("Napoli Fatayer", new[]{ ("Bread Loaf",1.0),("Mozzarella Cheese",0.05),
                ("Ricotta Cheese",0.04),("Spinach",0.02),("Parmesan Cheese",0.015),
                ("Olive Oil",0.005),("Garlic",0.003) }),

            // ── باستا 21-23 ──
            ("Tuscan Fettuccine", new[]{ ("Pasta",0.12),("Heavy Cream",0.06),("Cheddar Cheese",0.03),
                ("Parmesan Cheese",0.02),("Mushrooms",0.04),("Olive Oil",0.005),("Garlic",0.003) }),
            ("Pesto Pasta", new[]{ ("Pasta",0.12),("Pesto Sauce",0.05),("Mozzarella Cheese",0.04),
                ("Parmesan Cheese",0.02),("Olive Oil",0.005),("Tomatoes",0.03) }),
            ("Napoli Sujuk Pasta", new[]{ ("Pasta",0.12),("Tomato Sauce",0.07),("Sujuk",0.06),
                ("Mozzarella Cheese",0.04),("Parmesan Cheese",0.02),("Olive Oil",0.005),("Onions",0.015) }),

            // ── برجر وساندويتش 24-31 ──
            ("Classic Beef Burger", new[]{ ("Bread Loaf",1.0),("Ground Beef",0.10),
                ("Cheddar Cheese",0.03),("Onions",0.02),("Lettuce",0.02),("Tomato Sauce",0.02),
                ("Mayonnaise",0.01),("Mustard",0.01) }),
            ("Casper Burger", new[]{ ("Bread Loaf",1.0),("Ground Beef",0.10),("Cheddar Cheese",0.03),
                ("Mozzarella Cheese",0.02),("Onions",0.02),("Bell Peppers",0.02),("BBQ Sauce",0.02),
                ("Mayonnaise",0.01) }),
            ("Good Melted Burger", new[]{ ("Bread Loaf",1.0),("Ground Beef",0.10),
                ("Cheddar Cheese",0.03),("Heavy Cream",0.02),("Onions",0.02),("Hot Sauce",0.01),
                ("Mayonnaise",0.015) }),
            ("Mexicano Sandwich", new[]{ ("Bread Loaf",1.0),("Ground Beef",0.09),
                ("Cheddar Cheese",0.03),("Onions",0.02),("Spinach",0.02),("Tomato Sauce",0.02),
                ("Mayonnaise",0.01),("Mustard",0.01),("Jalapenos",0.01) }),
            ("Chicken Taouk Sandwich", new[]{ ("Bread Loaf",1.0),("Chicken Breast",0.09),
                ("Tomato Sauce",0.03),("Onions",0.02),("Bell Peppers",0.02),("Mayonnaise",0.01),
                ("Mustard",0.01),("Potato Sticks",0.01) }),
            ("Crispy Crepe", new[]{ ("Crepe Batter",0.09),("Heavy Cream",0.03),("Nutella",0.03),
                ("Cherry",0.03),("Butter",0.01) }),
            ("Chicken Quesadillas", new[]{ ("Bread Loaf",1.0),("Chicken Breast",0.08),
                ("Cheddar Cheese",0.04),("Bell Peppers",0.02),("Onions",0.02),("BBQ Sauce",0.015),
                ("Heavy Cream",0.015),("Mayonnaise",0.01) }),
            ("Napoli Sandwich", new[]{ ("Bread Loaf",1.0),("Doner Meat",0.08),("Mozzarella Cheese",0.04),
                ("Tomato Sauce",0.03),("Onions",0.02),("Parsley",0.005),("Olive Oil",0.005) }),

            // ── أطباق جانبية 32-38 ──
            ("Box Fries", new[]{ ("Potato Sticks",0.15),("Olive Oil",0.01),("Tomato Sauce",0.02) }),
            ("Texas Fries", new[]{ ("Potato Sticks",0.15),("Olive Oil",0.01),("BBQ Sauce",0.02),
                ("Mustard",0.005),("Onions",0.01) }),
            ("Chicken Fingers", new[]{ ("Chicken Breast",0.08),("Bread Loaf",0.5),
                ("Heavy Cream",0.03),("Mayonnaise",0.02),("Mustard",0.005) }),
            ("Mozzarella Sticks", new[]{ ("Mozzarella Cheese",0.10),("Bread Loaf",0.5),
                ("Tomato Sauce",0.02),("Olive Oil",0.005) }),
            ("Mini Burger", new[]{ ("Bread Loaf",1.0),("Ground Beef",0.06),("Cheddar Cheese",0.02),
                ("Onions",0.015),("Mayonnaise",0.01),("Mustard",0.005) }),
            ("Mac & Cheese White", new[]{ ("Pasta",0.10),("Heavy Cream",0.05),
                ("Cheddar Cheese",0.03),("Mozzarella Cheese",0.03),("Parmesan Cheese",0.015),
                ("Butter",0.01) }),
            ("Mac & Meatballs Red", new[]{ ("Pasta",0.12),("Tomato Sauce",0.08),("Ground Beef",0.07),
                ("Mozzarella Cheese",0.04),("Parmesan Cheese",0.02),("Olive Oil",0.005) }),
        };

        /// <summary>
        /// تُستدعى من DatabaseHelper بعد إنشاء الجداول وتعبئة الأصناف.
        /// idempotent — لو الـ seeder اتنفذ تاني، بيبحدّث الكميات بدل ما يكرّر.
        /// </summary>
        public static void Seed(SqliteConnection conn)
        {
            // ── 1) المواد الناقصة ──
            var added = new List<string>();
            foreach (var (name, stock, min, cost) in MissingIngredients)
            {
                using var ins = conn.CreateCommand();
                // NOT EXISTS مش INSERT OR IGNORE: عايز أعرف هل السطر ده
                // اتضاف فعلاً ولا المادة كانت موجودة من زمان (عندها أرقام
                // صاحب المطعم، ومينفعش أطمسها). الـ ROLLBACK جوّه command
                // منفصل عن الـ transaction بتاع الوصفات تحت.
                ins.CommandText = @"INSERT INTO Ingredients
                    (CategoryId,Name,Unit,Stock,MinStock,CostPerUnit,IsActive)
                    SELECT 7, @n, 'kg', @s, @m, @c, 1
                    WHERE NOT EXISTS (SELECT 1 FROM Ingredients WHERE Name = @n);";
                ins.Parameters.AddWithValue("@n", name);
                ins.Parameters.AddWithValue("@s", stock);
                ins.Parameters.AddWithValue("@m", min);
                ins.Parameters.AddWithValue("@c", cost);
                if (ins.ExecuteNonQuery() > 0) added.Add(name);
            }

            // ── 2) خريطة الاسم → Id (بعد إضافة الناقصة) ──
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

            // المواد اللي ضفناها جاية بأرقام افتتاحية تقديرية. التكلفة
            // والتدوير بيظبطهم صاحب المطعم من شاشة المخزن.
            if (added.Count > 0)
                AppLogger.Info(
                    "Recipe seed added ingredients with estimated opening stock/cost "
                    + "(adjust from the Warehouse screen): " + string.Join(", ", added));
        }
    }
}
