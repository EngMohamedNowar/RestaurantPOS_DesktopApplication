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
        static SqliteConnection Open() => DatabaseHelper.Open();

        public List<Ingredient> GetAll(string? search = null)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT i.Id,i.CategoryId,ic.Name,i.Name,i.Unit,i.Stock,i.MinStock,i.CostPerUnit
                FROM Ingredients i JOIN IngredientCategories ic ON ic.Id=i.CategoryId
                WHERE @s IS NULL OR i.Name LIKE '%'||@s||'%'
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
                WHERE i.Stock<=i.MinStock ORDER BY i.Stock";
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
                cmd.CommandText = "INSERT INTO Ingredients(CategoryId,Name,Unit,Stock,MinStock,CostPerUnit) VALUES(@c,@n,@u,@s,@m,@cp)";
            else { cmd.CommandText = "UPDATE Ingredients SET CategoryId=@c,Name=@n,Unit=@u,MinStock=@m,CostPerUnit=@cp WHERE Id=@id"; cmd.Parameters.AddWithValue("@id", ing.Id); }
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
            var cur = conn.CreateCommand();
            cur.CommandText = "SELECT Stock FROM Ingredients WHERE Id=@i";
            cur.Parameters.AddWithValue("@i", ingredientId);
            double current = Convert.ToDouble(cur.ExecuteScalar() ?? 0.0);
            double diff = newQty - current;
            using var tx = conn.BeginTransaction();
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

        public void DeductForOrder(IEnumerable<OrderItem> items, int userId)
        {
            using var conn = Open();
            foreach (var item in items)
            {
                var q = conn.CreateCommand();
                q.CommandText = "SELECT IngredientId,QtyUsed FROM ProductIngredients WHERE ProductId=@pid";
                q.Parameters.AddWithValue("@pid", item.ProductId);
                using var r = q.ExecuteReader();
                while (r.Read())
                {
                    int ingId = r.GetInt32(0);
                    double used = r.GetDouble(1) * item.Qty;
                    using var tx = conn.BeginTransaction();
                    var m = conn.CreateCommand(); m.Transaction = tx;
                    m.CommandText = "INSERT INTO StockMovements(IngredientId,Type,Qty,Note,UserId) VALUES(@i,'out',@q,'أوردر',@u)";
                    m.Parameters.AddWithValue("@i", ingId); m.Parameters.AddWithValue("@q", -used); m.Parameters.AddWithValue("@u", userId);
                    m.ExecuteNonQuery();
                    var u = conn.CreateCommand(); u.Transaction = tx;
                    u.CommandText = "UPDATE Ingredients SET Stock=MAX(0,Stock-@q) WHERE Id=@i";
                    u.Parameters.AddWithValue("@q", used); u.Parameters.AddWithValue("@i", ingId);
                    u.ExecuteNonQuery(); tx.Commit();
                }
            }
        }

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

