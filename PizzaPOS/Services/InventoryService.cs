// Services/InventoryService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;

namespace PizzaPOS.Services
{
    public class InventoryService
    {
        readonly string _cs;

        public InventoryService() : this(DatabaseHelper.CS) { }

        /// <summary>نسخة مثبّتة على قاعدة بيانات محددة — للاختبارات.</summary>
        public InventoryService(string connectionString) => _cs = connectionString;

        SqliteConnection Open()
        {
            var c = new SqliteConnection(_cs);
            c.Open();
            DatabaseHelper.Configure(c);
            return c;
        }


        public List<Ingredient> GetAll(string? search = null)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT i.Id,i.CategoryId,ic.Name,i.Name,i.Unit,i.Stock,i.MinStock,i.CostPerUnit
                FROM Ingredients i JOIN IngredientCategories ic ON ic.Id=i.CategoryId
                WHERE i.IsActive=1 AND (@s IS NULL OR i.Name LIKE '%'||@s||'%')
                ORDER BY i.Stock<=i.MinStock DESC, i.Name";
            cmd.Parameters.AddWithValue("@s", (object?)search ?? DBNull.Value);
            var list = new List<Ingredient>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        public List<Ingredient> GetLowStock()
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT i.Id,i.CategoryId,ic.Name,i.Name,i.Unit,i.Stock,i.MinStock,i.CostPerUnit
                FROM Ingredients i JOIN IngredientCategories ic ON ic.Id=i.CategoryId
                WHERE i.IsActive=1 AND i.Stock<=i.MinStock ORDER BY i.Stock";
            var list = new List<Ingredient>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        static Ingredient Map(SqliteDataReader r) => new Ingredient
        {
            Id = r.GetInt32(0),
            CategoryId = r.GetInt32(1),
            CategoryName = r.GetString(2),
            Name = r.GetString(3),
            Unit = r.GetString(4),
            Stock = r.GetDouble(5),
            MinStock = r.GetDouble(6),
            CostPerUnit = r.GetDouble(7)
        };

        public void Save(Ingredient ing)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            if (ing.Id == 0)
                cmd.CommandText = "INSERT INTO Ingredients(CategoryId,Name,Unit,Stock,MinStock,CostPerUnit,IsActive) VALUES(@c,@n,@u,@s,@m,@cp,1)";
            else
            {
                // Stock مش بيتعدّل هنا عن طريق التعديل — لازم يبقى عن طريق
                // AddStock/AdjustStock عشان يتفسّر في StockMovements.
                cmd.CommandText = "UPDATE Ingredients SET CategoryId=@c,Name=@n,Unit=@u,MinStock=@m,CostPerUnit=@cp,IsActive=1 WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", ing.Id);
            }
            cmd.Parameters.AddWithValue("@c", ing.CategoryId); cmd.Parameters.AddWithValue("@n", ing.Name);
            cmd.Parameters.AddWithValue("@u", ing.Unit); cmd.Parameters.AddWithValue("@s", ing.Stock);
            cmd.Parameters.AddWithValue("@m", ing.MinStock); cmd.Parameters.AddWithValue("@cp", ing.CostPerUnit);
            cmd.ExecuteNonQuery();
        }

        public void AddStock(int ingredientId, double qty, string note, int userId)
        {
            using var conn = Open(); using var tx = conn.BeginTransaction();
            var m = conn.CreateCommand(); m.Transaction = tx;
            m.CommandText = "INSERT INTO StockMovements(IngredientId,Type,Qty,Note,UserId) VALUES(@i,'in',@q,@n,@u)";
            m.Parameters.AddWithValue("@i", ingredientId); m.Parameters.AddWithValue("@q", qty);
            m.Parameters.AddWithValue("@n", (object?)note ?? DBNull.Value); m.Parameters.AddWithValue("@u", userId);
            m.ExecuteNonQuery();
            var u = conn.CreateCommand(); u.Transaction = tx;
            u.CommandText = "UPDATE Ingredients SET Stock=Stock+@q WHERE Id=@i";
            u.Parameters.AddWithValue("@q", qty); u.Parameters.AddWithValue("@i", ingredientId);
            u.ExecuteNonQuery(); tx.Commit();
        }

        public void AdjustStock(int ingredientId, double newQty, string note, int userId)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            // الـ SELECT لازم يكون جوه الـ transaction: لو كان برّاها، ممكن
            // حد تاني يغيّر الرصيد في الفترة بين القراءة والكتابة فالتسوية
            // تكتب قيمة مبنية على رقم قديم.
            var cur = conn.CreateCommand(); cur.Transaction = tx;
            cur.CommandText = "SELECT Stock FROM Ingredients WHERE Id=@i AND IsActive=1";
            cur.Parameters.AddWithValue("@i", ingredientId);
            var currentRaw = cur.ExecuteScalar();
            if (currentRaw == null)
                throw new InvalidOperationException("المادة دي مش موجودة أو محذوفة.");
            double diff = newQty - Convert.ToDouble(currentRaw);

            var m = conn.CreateCommand(); m.Transaction = tx;
            m.CommandText = "INSERT INTO StockMovements(IngredientId,Type,Qty,Note,UserId) VALUES(@i,'adjust',@q,@n,@u)";
            m.Parameters.AddWithValue("@i", ingredientId); m.Parameters.AddWithValue("@q", diff);
            m.Parameters.AddWithValue("@n", (object?)note ?? DBNull.Value); m.Parameters.AddWithValue("@u", userId);
            m.ExecuteNonQuery();
            var u = conn.CreateCommand(); u.Transaction = tx;
            u.CommandText = "UPDATE Ingredients SET Stock=@s WHERE Id=@i";
            u.Parameters.AddWithValue("@s", newQty); u.Parameters.AddWithValue("@i", ingredientId);
            u.ExecuteNonQuery(); tx.Commit();
        }

        /// <summary>
        /// يخصم مكونات الأوردر من المواد الخام.
        /// الكود القديم كان بيستعمل MAX(0,Stock-@q) — وده كان بيقصّ الكمية
        /// الناقصة بصمت: الحركة في StockMovements بتسجّل الخصم الكامل، لكن
        /// الرصيد يقف عند صفر. النتيجة إن المخزون كان بيكذب، والعجز بيضيع
        /// وما حدش بيعرف إن الصنف خلص.
        /// دلوقتي: الخصم كامل وبيسمح بالرصيد السالب (الكود الصادق)،
        /// والعجز بيرجع في النتيجة عشان نقدر نبلّغ المستخدم.
        /// </summary>
        public StockDeductionResult DeductForOrder(IEnumerable<OrderItem> items, int userId, string? orderRef = null)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var res = DeductCore(conn, tx, items, userId, orderRef);
                tx.Commit();
                LogShortage(res, orderRef);
                return res;
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>نفس <see cref="DeductForOrder(IEnumerable{OrderItem},int,string?)"/>
        /// بس جوه transaction بتاع حد تاني — مفيش commit ولا rollback هنا.
        /// الـ connection لازم يكون من نفس الـ CS بتاعنا.
        /// </summary>
        public StockDeductionResult DeductForOrder(IEnumerable<OrderItem> items, int userId,
                                                   string? orderRef,
                                                   SqliteConnection conn, SqliteTransaction tx)
        {
            var res = DeductCore(conn, tx, items, userId, orderRef);
            LogShortage(res, orderRef);
            return res;
        }

        void LogShortage(StockDeductionResult res, string? orderRef)
        {
            if (res.HasShortage)
                AppLogger.Warn($"Stock shortage on '{orderRef ?? "أوردر"}': {res.Summary}");
        }

        StockDeductionResult DeductCore(SqliteConnection conn, SqliteTransaction tx,
                                        IEnumerable<OrderItem> items, int userId, string? orderRef)
        {
            var result = new StockDeductionResult();
            string note = string.IsNullOrWhiteSpace(orderRef) ? "أوردر" : $"أوردر {orderRef}";

            foreach (var item in items)
            {
                // نقرأ الوصفة كاملة ونقفل الـ reader قبل أي كتابة:
                // SQLite مش بيسمح بأكثر من data reader مفتوح على نفس الـ connection
                var recipe = new List<(int IngredientId, double Used)>();
                var q = conn.CreateCommand(); q.Transaction = tx;
                q.CommandText = "SELECT IngredientId,QtyUsed FROM ProductIngredients WHERE ProductId=@pid";
                q.Parameters.AddWithValue("@pid", item.ProductId);
                using (var r = q.ExecuteReader())
                    while (r.Read())
                        recipe.Add((r.GetInt32(0), r.GetDouble(1) * item.Qty));

                foreach (var (ingId, used) in recipe)
                {
                    if (used <= 0) continue;

                    string ingName, ingUnit; double available;
                    var cur = conn.CreateCommand(); cur.Transaction = tx;
                    cur.CommandText = "SELECT Name,Unit,Stock FROM Ingredients WHERE Id=@i AND IsActive=1";
                    cur.Parameters.AddWithValue("@i", ingId);
                    using (var cr = cur.ExecuteReader())
                    {
                        if (!cr.Read())
                        {
                            // مادة محذوفة أو رابط قديم — متنخصشش رصيد مادة
                            // مش موجودة (ده كان هيخليها سالبة إلى الأبد)
                            AppLogger.Warn(
                                $"Stock deduction skipped: ingredient {ingId} missing/inactive for product {item.ProductId}");
                            continue;
                        }
                        ingName = cr.GetString(0);
                        ingUnit = cr.GetString(1);
                        available = cr.GetDouble(2);
                    }

                    if (available < used)
                        result.Shortages.Add(new StockShortage
                        {
                            IngredientId = ingId,
                            Ingredient = ingName,
                            Unit = ingUnit,
                            Required = used,
                            Available = available
                        });

                    var m = conn.CreateCommand(); m.Transaction = tx;
                    m.CommandText = "INSERT INTO StockMovements(IngredientId,Type,Qty,Note,UserId) VALUES(@i,'out',@q,@n,@u)";
                    m.Parameters.AddWithValue("@i", ingId); m.Parameters.AddWithValue("@q", -used);
                    m.Parameters.AddWithValue("@n", note); m.Parameters.AddWithValue("@u", userId);
                    m.ExecuteNonQuery();

                    var u = conn.CreateCommand(); u.Transaction = tx;
                    u.CommandText = "UPDATE Ingredients SET Stock=Stock-@q WHERE Id=@i";
                    u.Parameters.AddWithValue("@q", used); u.Parameters.AddWithValue("@i", ingId);
                    u.ExecuteNonQuery();
                    result.DeductedLines++;
                }
            }
            return result;
        }

        public int GetIngredientUsageCount(int ingredientId)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId=@i";
            cmd.Parameters.AddWithValue("@i", ingredientId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>حركة المخزون. JOIN داخلي مقصود: المواد المحذوفة (soft delete)
        /// صفوفها لسه موجودة، فالتقرير بيعرف يعرض اسمها كمان.</summary>
        public List<StockMovement> GetMovements(int days = 7)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT m.Id,m.IngredientId,i.Name,m.Type,m.Qty,m.Note,m.CreatedAt
                FROM StockMovements m JOIN Ingredients i ON i.Id=m.IngredientId
                WHERE date(m.CreatedAt)>=date('now','localtime',-@d||' days')
                ORDER BY m.Id DESC LIMIT 200";

            cmd.Parameters.AddWithValue("@d", days);
            var list = new List<StockMovement>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new StockMovement { Id = r.GetInt32(0), IngredientId = r.GetInt32(1), Ingredient = r.GetString(2), Type = r.GetString(3), Qty = r.GetDouble(4), Note = r.IsDBNull(5) ? null : r.GetString(5), CreatedAt = r.GetString(6) });
            return list;
        }
    }
}

