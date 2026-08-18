// Data/AppDbContext.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using PizzaPOS.Models;

namespace PizzaPOS.Data
{
    public class AppDbContext
    {
        SqliteConnection Open() => DatabaseHelper.Open();

        // ── Settings ────────────────────────────────
        public string GetSetting(string key, string def = "")
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key=@k";
            cmd.Parameters.AddWithValue("@k", key);
            return cmd.ExecuteScalar()?.ToString() ?? def;
        }

        public void SetSetting(string key, string value)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Settings(Key,Value) VALUES(@k,@v)";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", value);
            cmd.ExecuteNonQuery();
        }

        // ── Order Tracking ───────────────────────────
        public List<Order> GetActiveOrders()
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id,OrderNumber,OrderType,PayMethod,Total,
                       CreatedAt,Status,Notes,
                       CustomerName,CustomerPhone,DeliveryAddress,
                       DriverName,DeliveryFee
                FROM Orders
                WHERE (Status IS NULL OR Status NOT IN ('completed','cancelled'))
                   OR (Status='completed'
                       AND date(CreatedAt)=date('now','localtime'))
                ORDER BY Id DESC";
            var list = new List<Order>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Order
            {
                Id = r.GetInt32(0),
                OrderNumber = r.GetString(1),
                OrderType = r.GetString(2),
                PayMethod = r.GetString(3),
                Total = r.GetDouble(4),
                CreatedAt = r.IsDBNull(5) ? "" : r.GetString(5),
                Status = r.IsDBNull(6) ? "new" : r.GetString(6),
                Notes = r.IsDBNull(7) ? "" : r.GetString(7),
                CustomerName = r.IsDBNull(8) ? "" : r.GetString(8),
                CustomerPhone = r.IsDBNull(9) ? "" : r.GetString(9),
                DeliveryAddress = r.IsDBNull(10) ? "" : r.GetString(10),
                DriverName = r.IsDBNull(11) ? "" : r.GetString(11),
                DeliveryFee = r.IsDBNull(12) ? 0 : r.GetDouble(12)
            });
            return list;
        }

        public void UpdateOrderStatus(int orderId, string status, string? deliveryStatus = null)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (deliveryStatus != null)
            {
                cmd.CommandText = @"UPDATE Orders SET Status=@s, DeliveryStatus=@ds WHERE Id=@id";
                cmd.Parameters.AddWithValue("@ds", deliveryStatus);
            }
            else
            {
                cmd.CommandText = "UPDATE Orders SET Status=@s WHERE Id=@id";
            }
            cmd.Parameters.AddWithValue("@s", status);
            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.ExecuteNonQuery();
        }

        // ── GetOrderById ─────────────────────────────
        public Order? GetOrderById(int id)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id,OrderNumber,OrderType,PayMethod,
                       Subtotal,Discount,Tax,Total,PaidAmount,Change,
                       Notes,CustomerId,CustomerName,CustomerPhone,
                       DeliveryAddress,DriverId,DriverName,DeliveryFee,
                       CreatedAt,Status,DeliveryStatus
                FROM Orders WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            var o = new Order
            {
                Id = r.GetInt32(0),
                OrderNumber = r.GetString(1),
                OrderType = r.GetString(2),
                PayMethod = r.GetString(3),
                Subtotal = r.GetDouble(4),
                Discount = r.GetDouble(5),
                Tax = r.GetDouble(6),
                Total = r.GetDouble(7),
                PaidAmount = r.GetDouble(8),
                Change = r.GetDouble(9),
                Notes = r.IsDBNull(10) ? "" : r.GetString(10),
                CustomerId = r.IsDBNull(11) ? 0 : r.GetInt32(11),
                CustomerName = r.IsDBNull(12) ? "" : r.GetString(12),
                CustomerPhone = r.IsDBNull(13) ? "" : r.GetString(13),
                DeliveryAddress = r.IsDBNull(14) ? "" : r.GetString(14),
                DriverId = r.IsDBNull(15) ? 0 : r.GetInt32(15),
                DriverName = r.IsDBNull(16) ? "" : r.GetString(16),
                DeliveryFee = r.IsDBNull(17) ? 0 : r.GetDouble(17),
                CreatedAt = r.IsDBNull(18) ? "" : r.GetString(18),
                Status = r.IsDBNull(19) ? "" : r.GetString(19),
                DeliveryStatus = r.IsDBNull(20) ? "" : r.GetString(20)
            };

            var ic = c.CreateCommand();
            ic.CommandText = @"SELECT Id,ProductId,Name,Price,Cost,Qty,Subtotal,SizeName,ExtrasNote
                FROM OrderItems WHERE OrderId=@oid";
            ic.Parameters.AddWithValue("@oid", id);
            using var ir = ic.ExecuteReader();
            o.Items = new List<OrderItem>();
            while (ir.Read()) o.Items.Add(new OrderItem
            {
                ProductId = ir.GetInt32(1),
                Name = ir.GetString(2),
                BasePrice = ir.GetDouble(3),
                Cost = ir.GetDouble(4),
                Qty = ir.GetInt32(5),
                SizeName = ir.IsDBNull(7) ? "" : ir.GetString(7),
                ExtrasNote = ir.IsDBNull(8) ? "" : ir.GetString(8)
            });
            return o;
        }

        // ── UpdateOrder ──────────────────────────────
        public void UpdateOrder(Order o)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var cmd = conn.CreateCommand(); cmd.Transaction = tx;
                cmd.CommandText = @"UPDATE Orders SET
                    PayMethod=@pm, Subtotal=@sub, Discount=@disc,
                    Tax=@tax, Total=@tot, PaidAmount=@paid, Change=@chg,
                    Notes=@notes, CustomerName=@cname, CustomerPhone=@cphone,
                    DeliveryAddress=@caddr, DriverId=@did, DriverName=@dname,
                    DeliveryFee=@dfee WHERE Id=@id";
                cmd.Parameters.AddWithValue("@pm", o.PayMethod);
                cmd.Parameters.AddWithValue("@sub", o.Subtotal);
                cmd.Parameters.AddWithValue("@disc", o.Discount);
                cmd.Parameters.AddWithValue("@tax", o.Tax);
                cmd.Parameters.AddWithValue("@tot", o.Total);
                cmd.Parameters.AddWithValue("@paid", o.PaidAmount);
                cmd.Parameters.AddWithValue("@chg", o.Change);
                cmd.Parameters.AddWithValue("@notes", (object?)o.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cname", o.CustomerName);
                cmd.Parameters.AddWithValue("@cphone", o.CustomerPhone);
                cmd.Parameters.AddWithValue("@caddr", o.DeliveryAddress);
                cmd.Parameters.AddWithValue("@did", o.DriverId);
                cmd.Parameters.AddWithValue("@dname", o.DriverName);
                cmd.Parameters.AddWithValue("@dfee", o.DeliveryFee);
                cmd.Parameters.AddWithValue("@id", o.Id);
                cmd.ExecuteNonQuery();

                var del = conn.CreateCommand(); del.Transaction = tx;
                del.CommandText = "DELETE FROM OrderItems WHERE OrderId=@oid";
                del.Parameters.AddWithValue("@oid", o.Id);
                del.ExecuteNonQuery();

                foreach (var item in o.Items)
                {
                    var ic = conn.CreateCommand(); ic.Transaction = tx;
                    ic.CommandText = @"INSERT INTO OrderItems
                        (OrderId,ProductId,Name,Price,Cost,Qty,Subtotal,SizeName,ExtrasNote)
                        VALUES(@oid,@pid,@n,@p,@co,@q,@s,@sz,@ex)";
                    ic.Parameters.AddWithValue("@oid", o.Id);
                    ic.Parameters.AddWithValue("@pid", item.ProductId);
                    ic.Parameters.AddWithValue("@n", item.Name);
                    ic.Parameters.AddWithValue("@p", item.BasePrice);
                    ic.Parameters.AddWithValue("@co", item.Cost);
                    ic.Parameters.AddWithValue("@q", item.Qty);
                    ic.Parameters.AddWithValue("@s", item.Price * item.Qty);
                    ic.Parameters.AddWithValue("@sz", (object?)item.SizeName ?? DBNull.Value);
                    ic.Parameters.AddWithValue("@ex", (object?)item.ExtrasNote ?? DBNull.Value);
                    ic.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public List<Order> GetTodayOrdersByStatus(string status)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id,OrderNumber,OrderType,Total,CreatedAt,
                       Status,CustomerName,DriverName,Notes
                FROM Orders
                WHERE Status=@s
                  AND date(CreatedAt)=date('now','localtime')
                ORDER BY Id DESC";
            cmd.Parameters.AddWithValue("@s", status);
            var list = new List<Order>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Order
            {
                Id = r.GetInt32(0),
                OrderNumber = r.GetString(1),
                OrderType = r.GetString(2),
                Total = r.GetDouble(3),
                CreatedAt = r.IsDBNull(4) ? "" : r.GetString(4),
                Status = r.IsDBNull(5) ? "" : r.GetString(5),
                CustomerName = r.IsDBNull(6) ? "" : r.GetString(6),
                DriverName = r.IsDBNull(7) ? "" : r.GetString(7),
                Notes = r.IsDBNull(8) ? "" : r.GetString(8)
            });
            return list;
        }

        // ── Categories ──────────────────────────────
        public List<Category> GetCategories()
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,Icon,SortOrder FROM Categories ORDER BY SortOrder,Name";
            var list = new List<Category>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Category
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Icon = r.GetString(2),
                SortOrder = r.GetInt32(3)
            });
            return list;
        }

        public void SaveCategory(Category cat)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (cat.Id == 0)
                cmd.CommandText = "INSERT INTO Categories(Name,Icon,SortOrder) VALUES(@n,@i,@so)";
            else
            {
                cmd.CommandText = "UPDATE Categories SET Name=@n,Icon=@i,SortOrder=@so WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", cat.Id);
            }
            cmd.Parameters.AddWithValue("@n", cat.Name);
            cmd.Parameters.AddWithValue("@i", cat.Icon);
            cmd.Parameters.AddWithValue("@so", cat.SortOrder);
            cmd.ExecuteNonQuery();
        }

        // ── GetProductsByCategory ────────────────────
        /// <summary>
        /// بيرجع كل المنتجات الـ active المرتبطة بفئة معينة.
        /// بيُستخدم للتحقق قبل حذف الفئة وعرض عدد المنتجات للمستخدم.
        /// </summary>
        public List<Product> GetProductsByCategory(int categoryId)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, CategoryId, Name, Price, Cost, Icon, IsActive
                FROM Products
                WHERE CategoryId = @cid AND IsActive = 1";
            cmd.Parameters.AddWithValue("@cid", categoryId);
            var list = new List<Product>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Product
            {
                Id = r.GetInt32(0),
                CategoryId = r.GetInt32(1),
                Name = r.GetString(2),
                Price = r.GetDouble(3),
                Cost = r.GetDouble(4),
                Icon = r.GetString(5),
                IsActive = r.GetInt32(6) == 1
            });
            return list;
        }

        // ── DeleteCategory (cascade) ─────────────────
        /// <summary>
        /// بيحذف الفئة وبيعمل soft-delete لكل المنتجات المرتبطة بيها
        /// داخل transaction واحدة لضمان الـ consistency
        /// </summary>
        public void DeleteCategory(int id)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // 1) احذف ProductExtras للمنتجات دي
                var delExtras = conn.CreateCommand(); delExtras.Transaction = tx;
                delExtras.CommandText = @"
            DELETE FROM ProductExtras
            WHERE ProductId IN (SELECT Id FROM Products WHERE CategoryId=@cid)";
                delExtras.Parameters.AddWithValue("@cid", id);
                delExtras.ExecuteNonQuery();

                // 2) احذف ProductSizes للمنتجات دي
                var delSizes = conn.CreateCommand(); delSizes.Transaction = tx;
                delSizes.CommandText = @"
            DELETE FROM ProductSizes
            WHERE ProductId IN (SELECT Id FROM Products WHERE CategoryId=@cid)";
                delSizes.Parameters.AddWithValue("@cid", id);
                delSizes.ExecuteNonQuery();

                // 3) hard-delete المنتجات نفسها
                var delProducts = conn.CreateCommand(); delProducts.Transaction = tx;
                delProducts.CommandText = "DELETE FROM Products WHERE CategoryId=@cid";
                delProducts.Parameters.AddWithValue("@cid", id);
                delProducts.ExecuteNonQuery();

                // 4) احذف الفئة
                var delCat = conn.CreateCommand(); delCat.Transaction = tx;
                delCat.CommandText = "DELETE FROM Categories WHERE Id=@id";
                delCat.Parameters.AddWithValue("@id", id);
                delCat.ExecuteNonQuery();

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
        // ── Products ────────────────────────────────
        public List<Product> GetProducts(int? catId = null, string? search = null)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT p.Id,p.CategoryId,c.Name,p.Name,p.Price,p.Cost,p.Icon,p.IsActive
                FROM Products p JOIN Categories c ON c.Id=p.CategoryId
                WHERE p.IsActive=1
                  AND (@cat IS NULL OR p.CategoryId=@cat)
                  AND (@s   IS NULL OR p.Name LIKE '%'||@s||'%')
                ORDER BY p.Name";
            cmd.Parameters.AddWithValue("@cat", (object?)catId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@s", (object?)search ?? DBNull.Value);
            var list = new List<Product>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Product
            {
                Id = r.GetInt32(0),
                CategoryId = r.GetInt32(1),
                CategoryName = r.GetString(2),
                Name = r.GetString(3),
                Price = r.GetDouble(4),
                Cost = r.GetDouble(5),
                Icon = r.GetString(6),
                IsActive = r.GetInt32(7) == 1
            });
            return list;
        }

        public void SaveProduct(Product p)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (p.Id == 0)
                cmd.CommandText = "INSERT INTO Products(CategoryId,Name,Price,Cost,Icon) VALUES(@c,@n,@p,@co,@i)";
            else
            {
                cmd.CommandText = "UPDATE Products SET CategoryId=@c,Name=@n,Price=@p,Cost=@co,Icon=@i WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", p.Id);
            }
            cmd.Parameters.AddWithValue("@c", p.CategoryId);
            cmd.Parameters.AddWithValue("@n", p.Name);
            cmd.Parameters.AddWithValue("@p", p.Price);
            cmd.Parameters.AddWithValue("@co", p.Cost);
            cmd.Parameters.AddWithValue("@i", p.Icon);
            cmd.ExecuteNonQuery();
        }

        public void DeleteProduct(int id)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Products SET IsActive=0 WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ── ProductSizes ────────────────────────────
        public List<ProductSize> GetProductSizes(int productId)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,ProductId,Name,ExtraPrice,SortOrder
                FROM ProductSizes WHERE ProductId=@pid ORDER BY SortOrder";
            cmd.Parameters.AddWithValue("@pid", productId);
            var list = new List<ProductSize>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new ProductSize
            {
                Id = r.GetInt32(0),
                ProductId = r.GetInt32(1),
                Name = r.GetString(2),
                ExtraPrice = r.GetDouble(3),
                SortOrder = r.GetInt32(4)
            });
            return list;
        }

        public void SaveProductSize(ProductSize s)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (s.Id == 0)
                cmd.CommandText = @"INSERT INTO ProductSizes(ProductId,Name,ExtraPrice,SortOrder)
                    VALUES(@pid,@n,@ep,@so)";
            else
            {
                cmd.CommandText = @"UPDATE ProductSizes
                    SET Name=@n,ExtraPrice=@ep,SortOrder=@so WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", s.Id);
            }
            cmd.Parameters.AddWithValue("@pid", s.ProductId);
            cmd.Parameters.AddWithValue("@n", s.Name);
            cmd.Parameters.AddWithValue("@ep", s.ExtraPrice);
            cmd.Parameters.AddWithValue("@so", s.SortOrder);
            cmd.ExecuteNonQuery();
        }

        public void DeleteProductSize(int id)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM ProductSizes WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ── ProductExtras ───────────────────────────
        public List<ProductExtra> GetProductExtras(int productId)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,ProductId,Name,Price
                FROM ProductExtras WHERE ProductId=@pid ORDER BY Name";
            cmd.Parameters.AddWithValue("@pid", productId);
            var list = new List<ProductExtra>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new ProductExtra
            {
                Id = r.GetInt32(0),
                ProductId = r.GetInt32(1),
                Name = r.GetString(2),
                Price = r.GetDouble(3)
            });
            return list;
        }

        public void SaveProductExtra(ProductExtra e)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (e.Id == 0)
                cmd.CommandText = @"INSERT INTO ProductExtras(ProductId,Name,Price)
                    VALUES(@pid,@n,@p)";
            else
            {
                cmd.CommandText = "UPDATE ProductExtras SET Name=@n,Price=@p WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", e.Id);
            }
            cmd.Parameters.AddWithValue("@pid", e.ProductId);
            cmd.Parameters.AddWithValue("@n", e.Name);
            cmd.Parameters.AddWithValue("@p", e.Price);
            cmd.ExecuteNonQuery();
        }

        public void DeleteProductExtra(int id)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM ProductExtras WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ── Customers ────────────────────────────────
        public List<Customer> GetCustomers(string? search = null)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,Name,Phone,Address,Notes,CreatedAt
                FROM Customers
                WHERE (@s IS NULL OR Name  LIKE '%'||@s||'%'
                                  OR Phone LIKE '%'||@s||'%')
                ORDER BY Name";
            cmd.Parameters.AddWithValue("@s", (object?)search ?? DBNull.Value);
            var list = new List<Customer>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Customer
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Phone = r.GetString(2),
                Address = r.IsDBNull(3) ? "" : r.GetString(3),
                Notes = r.IsDBNull(4) ? "" : r.GetString(4),
                CreatedAt = r.IsDBNull(5) ? "" : r.GetString(5)
            });
            return list;
        }

        public void SaveCustomer(Customer cust)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (cust.Id == 0)
                cmd.CommandText = @"INSERT INTO Customers(Name,Phone,Address,Notes)
                    VALUES(@n,@p,@a,@no)";
            else
            {
                cmd.CommandText = @"UPDATE Customers
                    SET Name=@n,Phone=@p,Address=@a,Notes=@no WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", cust.Id);
            }
            cmd.Parameters.AddWithValue("@n", cust.Name);
            cmd.Parameters.AddWithValue("@p", cust.Phone);
            cmd.Parameters.AddWithValue("@a", cust.Address);
            cmd.Parameters.AddWithValue("@no", cust.Notes);
            cmd.ExecuteNonQuery();

            if (cust.Id == 0)
            {
                var idCmd = c.CreateCommand();
                idCmd.CommandText = "SELECT last_insert_rowid()";
                cust.Id = (int)(long)(idCmd.ExecuteScalar() ?? 0L);
            }
        }

        public Customer? GetCustomerByPhone(string phone)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,Name,Phone,Address,Notes
                FROM Customers WHERE Phone=@p LIMIT 1";
            cmd.Parameters.AddWithValue("@p", phone.Trim());
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new Customer
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Phone = r.GetString(2),
                Address = r.IsDBNull(3) ? "" : r.GetString(3),
                Notes = r.IsDBNull(4) ? "" : r.GetString(4)
            };
        }

        // ── Drivers ──────────────────────────────────
        public List<Driver> GetDrivers()
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,Name,Phone,IsActive
                FROM Drivers WHERE IsActive=1 ORDER BY Name";
            var list = new List<Driver>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Driver
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Phone = r.GetString(2),
                IsActive = r.GetInt32(3) == 1
            });
            return list;
        }

        public void SaveDriver(Driver d)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (d.Id == 0)
                cmd.CommandText = "INSERT INTO Drivers(Name,Phone,IsActive) VALUES(@n,@p,1)";
            else
            {
                cmd.CommandText = "UPDATE Drivers SET Name=@n,Phone=@p,IsActive=@a WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.Parameters.AddWithValue("@a", d.IsActive ? 1 : 0);
            }
            cmd.Parameters.AddWithValue("@n", d.Name);
            cmd.Parameters.AddWithValue("@p", d.Phone);
            cmd.ExecuteNonQuery();
        }

        // ── Orders ──────────────────────────────────
        public string GetNextOrderNumber()
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*)+1 FROM Orders WHERE date(CreatedAt)=date('now','localtime')";
            long n = (long)(cmd.ExecuteScalar() ?? 1L);
            return $"{DateTime.Now:yyyyMMdd}-{n:D4}";
        }

        public int SaveOrder(Order o)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var cmd = conn.CreateCommand(); cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO Orders
                    (OrderNumber,ShiftId,UserId,OrderType,PayMethod,
                     Subtotal,Discount,Tax,ServiceCharge,Total,PaidAmount,Change,Notes,
                     CustomerId,CustomerName,CustomerPhone,DeliveryAddress,
                     DriverId,DriverName,DeliveryFee,DeliveryStatus,Status)
                    VALUES
                    (@num,@sid,@uid,@ot,@pm,
                     @sub,@disc,@tax,@srv,@tot,@paid,@chg,@notes,
                     @cid,@cname,@cphone,@caddr,
                     @did,@dname,@dfee,@dstatus,@status)";

                cmd.Parameters.AddWithValue("@num", o.OrderNumber);
                cmd.Parameters.AddWithValue("@sid", o.ShiftId);
                cmd.Parameters.AddWithValue("@uid", o.UserId);
                cmd.Parameters.AddWithValue("@ot", o.OrderType);
                cmd.Parameters.AddWithValue("@pm", o.PayMethod);
                cmd.Parameters.AddWithValue("@sub", o.Subtotal);
                cmd.Parameters.AddWithValue("@disc", o.Discount);
                cmd.Parameters.AddWithValue("@tax", o.Tax);
                cmd.Parameters.AddWithValue("@srv", o.ServiceCharge);
                cmd.Parameters.AddWithValue("@tot", o.Total);
                cmd.Parameters.AddWithValue("@paid", o.PaidAmount);
                cmd.Parameters.AddWithValue("@chg", o.Change);
                cmd.Parameters.AddWithValue("@notes", (object?)o.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cid", o.CustomerId);
                cmd.Parameters.AddWithValue("@cname", o.CustomerName);
                cmd.Parameters.AddWithValue("@cphone", o.CustomerPhone);
                cmd.Parameters.AddWithValue("@caddr", o.DeliveryAddress);
                cmd.Parameters.AddWithValue("@did", o.DriverId);
                cmd.Parameters.AddWithValue("@dname", o.DriverName);
                cmd.Parameters.AddWithValue("@dfee", o.DeliveryFee);
                cmd.Parameters.AddWithValue("@dstatus", o.DeliveryStatus);
                cmd.Parameters.AddWithValue("@status", o.Status ?? "new");
                cmd.ExecuteNonQuery();

                var rowCmd = conn.CreateCommand(); rowCmd.Transaction = tx;
                rowCmd.CommandText = "SELECT last_insert_rowid()";
                long oid = (long)(rowCmd.ExecuteScalar() ?? 0L);

                foreach (var item in o.Items)
                {
                    var ic = conn.CreateCommand(); ic.Transaction = tx;
                    ic.CommandText = @"INSERT INTO OrderItems
                        (OrderId,ProductId,Name,Price,Cost,Qty,Subtotal,SizeName,ExtrasNote)
                        VALUES(@oid,@pid,@n,@p,@co,@q,@s,@sz,@ex)";
                    ic.Parameters.AddWithValue("@oid", oid);
                    ic.Parameters.AddWithValue("@pid", item.ProductId);
                    ic.Parameters.AddWithValue("@n", item.Name);
                    ic.Parameters.AddWithValue("@p", item.BasePrice);
                    ic.Parameters.AddWithValue("@co", item.Cost);
                    ic.Parameters.AddWithValue("@q", item.Qty);
                    ic.Parameters.AddWithValue("@s", item.Subtotal);
                    ic.Parameters.AddWithValue("@sz", (object?)item.SizeName ?? DBNull.Value);
                    ic.Parameters.AddWithValue("@ex", (object?)item.ExtrasNote ?? DBNull.Value);
                    ic.ExecuteNonQuery();
                }
                tx.Commit();
                return (int)oid;
            }
            catch { tx.Rollback(); throw; }
        }

        // ── Reports ─────────────────────────────────
        public (double Sales, int Count, double Avg) GetTodaySummary()
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(SUM(Total),0),COUNT(*),COALESCE(AVG(Total),0)
                FROM Orders
                WHERE date(CreatedAt)=date('now','localtime')
                  AND Status NOT IN ('cancelled','held')";
            using var r = cmd.ExecuteReader(); r.Read();
            return (r.GetDouble(0), r.GetInt32(1), r.GetDouble(2));
        }

        public List<Order> GetRecentOrders(int limit = 50)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,OrderNumber,OrderType,PayMethod,Total,CreatedAt
                FROM Orders WHERE Status NOT IN ('cancelled','held')
                ORDER BY Id DESC LIMIT @lim";
            cmd.Parameters.AddWithValue("@lim", limit);
            var list = new List<Order>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Order
            {
                Id = r.GetInt32(0),
                OrderNumber = r.GetString(1),
                OrderType = r.GetString(2),
                PayMethod = r.GetString(3),
                Total = r.GetDouble(4),
                CreatedAt = r.GetString(5)
            });
            return list;
        }

        public (double Profit, double Loss) GetTodayProfitLoss()
        {
            using var c = Open();

            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COALESCE(SUM(oi.Subtotal - (oi.Cost * oi.Qty)), 0)
                    - COALESCE((SELECT SUM(Tax) FROM Orders
                                WHERE date(CreatedAt)=date('now','localtime')
                                  AND Status NOT IN ('cancelled','held')), 0)
                    + COALESCE((SELECT SUM(COALESCE(ServiceCharge,0)) FROM Orders
                                WHERE date(CreatedAt)=date('now','localtime')
                                  AND Status NOT IN ('cancelled','held')), 0)
                FROM OrderItems oi
                JOIN Orders o ON o.Id=oi.OrderId
                WHERE date(o.CreatedAt)=date('now','localtime')
                  AND o.Status NOT IN ('cancelled','held')";
            double profit = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);

            var cmd2 = c.CreateCommand();
            cmd2.CommandText = @"SELECT COALESCE(SUM(Amount),0) FROM Losses
                WHERE date(Date)=date('now','localtime')";
            double manualLoss = Convert.ToDouble(cmd2.ExecuteScalar() ?? 0);

            var cmd3 = c.CreateCommand();
            cmd3.CommandText = @"SELECT COALESCE(SUM(Discount),0) FROM Orders
                WHERE date(CreatedAt)=date('now','localtime')
                  AND Status NOT IN ('cancelled','held')";
            double discountLoss = Convert.ToDouble(cmd3.ExecuteScalar() ?? 0);

            var cmd4 = c.CreateCommand();
            cmd4.CommandText = @"SELECT COALESCE(SUM((oi.Cost - oi.Price)*oi.Qty),0)
                FROM OrderItems oi JOIN Orders o ON o.Id=oi.OrderId
                WHERE date(o.CreatedAt)=date('now','localtime')
                  AND o.Status NOT IN ('cancelled','held')
                  AND oi.Price < oi.Cost";
            double belowCostLoss = Convert.ToDouble(cmd4.ExecuteScalar() ?? 0);

            return (profit, manualLoss + discountLoss + belowCostLoss);
        }

        public List<ProductStat> GetTopProducts(DateTime from, DateTime to)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT oi.Name, COALESCE(cat.Name,'—'),
                SUM(oi.Qty), SUM(oi.Subtotal), SUM((oi.Price-oi.Cost)*oi.Qty)
                FROM OrderItems oi
                JOIN Orders o ON o.Id=oi.OrderId
                LEFT JOIN Products     p   ON p.Id=oi.ProductId
                LEFT JOIN Categories cat   ON cat.Id=p.CategoryId
                WHERE o.Status NOT IN ('cancelled','held')
                  AND date(o.CreatedAt) BETWEEN @f AND @t
                GROUP BY oi.Name
                ORDER BY SUM(oi.Subtotal) DESC LIMIT 10";
            cmd.Parameters.AddWithValue("@f", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@t", to.ToString("yyyy-MM-dd"));
            var list = new List<ProductStat>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new ProductStat
            {
                Name = r.GetString(0),
                Category = r.GetString(1),
                Qty = r.GetInt32(2),
                Sales = r.GetDouble(3),
                Profit = r.GetDouble(4)
            });
            return list;
        }

        // ── Losses ───────────────────────────────────
        public List<LossSummary> GetLossesSummary(DateTime from, DateTime to)
        {
            var list = new List<LossSummary>();
            using var c = Open();

            var cmd1 = c.CreateCommand();
            cmd1.CommandText = @"SELECT Type,Description,Amount,Date FROM Losses
                WHERE date(Date) BETWEEN @f AND @t ORDER BY Date DESC";
            cmd1.Parameters.AddWithValue("@f", from.ToString("yyyy-MM-dd"));
            cmd1.Parameters.AddWithValue("@t", to.ToString("yyyy-MM-dd"));
            using (var r = cmd1.ExecuteReader())
                while (r.Read()) list.Add(new LossSummary
                {
                    Type = r.GetString(0),
                    Description = r.GetString(1),
                    Amount = r.GetDouble(2),
                    Date = r.GetString(3),
                    Color = "#E63946"
                });

            var cmd2 = c.CreateCommand();
            cmd2.CommandText = @"SELECT oi.Name,
                SUM((oi.Cost - oi.Price) * oi.Qty) AS Loss,
                date(o.CreatedAt)
                FROM OrderItems oi JOIN Orders o ON o.Id=oi.OrderId
                WHERE o.Status NOT IN ('cancelled','held') AND oi.Cost > oi.Price
                  AND date(o.CreatedAt) BETWEEN @f AND @t
                GROUP BY oi.Name, date(o.CreatedAt)
                ORDER BY date(o.CreatedAt) DESC";
            cmd2.Parameters.AddWithValue("@f", from.ToString("yyyy-MM-dd"));
            cmd2.Parameters.AddWithValue("@t", to.ToString("yyyy-MM-dd"));
            using (var r = cmd2.ExecuteReader())
                while (r.Read()) list.Add(new LossSummary
                {
                    Type = "بيع بأقل من التكلفة",
                    Description = $"{r.GetString(0)} — فرق التكلفة",
                    Amount = r.GetDouble(1),
                    Date = r.GetString(2),
                    Color = "#ffd166"
                });

            var cmd3 = c.CreateCommand();
            cmd3.CommandText = @"SELECT COALESCE(SUM(Discount),0), date(CreatedAt)
                FROM Orders
                WHERE Status NOT IN ('cancelled','held') AND Discount > 0
                  AND date(CreatedAt) BETWEEN @f AND @t
                GROUP BY date(CreatedAt) ORDER BY date(CreatedAt) DESC";
            cmd3.Parameters.AddWithValue("@f", from.ToString("yyyy-MM-dd"));
            cmd3.Parameters.AddWithValue("@t", to.ToString("yyyy-MM-dd"));
            using (var r = cmd3.ExecuteReader())
                while (r.Read()) list.Add(new LossSummary
                {
                    Type = "خصومات",
                    Description = "إجمالي الخصومات المُعطاة",
                    Amount = r.GetDouble(0),
                    Date = r.GetString(1),
                    Color = "#a78bfa"
                });

            return list;
        }

        public double GetTotalLosses(DateTime from, DateTime to)
        {
            double total = 0;
            foreach (var l in GetLossesSummary(from, to)) total += l.Amount;
            return total;
        }

        public void SaveLoss(LossEntry loss)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO Losses(Date,Type,Description,Amount,CreatedBy)
                VALUES(@d,@t,@desc,@a,@cb)";
            cmd.Parameters.AddWithValue("@d", loss.Date);
            cmd.Parameters.AddWithValue("@t", loss.Type);
            cmd.Parameters.AddWithValue("@desc", loss.Description);
            cmd.Parameters.AddWithValue("@a", loss.Amount);
            cmd.Parameters.AddWithValue("@cb", loss.CreatedBy);
            cmd.ExecuteNonQuery();
        }

        public List<string> GetLossTypes() => new()
        {
            "مصروف تشغيلي",
            "خامات تالفة",
            "عجز كاشير",
            "أوردر ملغي",
            "مردود عميل",
            "أخرى"
        };

        // ── GetDailyRange ────────────────────────────
        public List<DailySummary> GetDailyRange(DateTime from, DateTime to)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    date(o.CreatedAt),
                    SUM(o.Total),
                    SUM(CASE WHEN o.PayMethod IN ('كاش','نقدي') THEN o.Total ELSE 0 END),
                    SUM(CASE WHEN o.PayMethod NOT IN ('كاش','نقدي') THEN o.Total ELSE 0 END),
                    COUNT(*),
                    SUM(o.Tax),
                    SUM(o.Discount),
                    SUM(COALESCE(o.ServiceCharge, 0)),

                    COALESCE((
                        SELECT SUM(oi.Subtotal - (oi.Cost * oi.Qty))
                        FROM OrderItems oi
                        JOIN Orders o2 ON o2.Id = oi.OrderId
                        WHERE o2.Status NOT IN ('cancelled','held')
                          AND date(o2.CreatedAt) = date(o.CreatedAt)
                          AND date(o2.CreatedAt) BETWEEN @f AND @t
                    ), 0)
                    - SUM(o.Tax)
                    + SUM(COALESCE(o.ServiceCharge, 0)),

                    COALESCE((
                        SELECT SUM(l.Amount) FROM Losses l
                        WHERE date(l.Date) = date(o.CreatedAt)
                          AND date(l.Date) BETWEEN @f AND @t
                    ), 0)
                    + SUM(o.Discount)
                    + COALESCE((
                        SELECT SUM((oi2.Cost - oi2.Price) * oi2.Qty)
                        FROM OrderItems oi2
                        JOIN Orders o3 ON o3.Id = oi2.OrderId
                        WHERE o3.Status NOT IN ('cancelled','held')
                          AND date(o3.CreatedAt) = date(o.CreatedAt)
                          AND date(o3.CreatedAt) BETWEEN @f AND @t
                          AND oi2.Price < oi2.Cost
                    ), 0)

                FROM Orders o
                WHERE o.Status NOT IN ('cancelled','held')
                  AND date(o.CreatedAt) BETWEEN @f AND @t
                GROUP BY date(o.CreatedAt)
                ORDER BY date(o.CreatedAt) DESC";

            cmd.Parameters.AddWithValue("@f", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@t", to.ToString("yyyy-MM-dd"));

            var list = new List<DailySummary>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new DailySummary
            {
                Date = r.GetString(0),
                Sales = r.GetDouble(1),
                Cash = r.GetDouble(2),
                Card = r.GetDouble(3),
                Orders = r.GetInt32(4),
                Tax = r.GetDouble(5),
                Discount = r.GetDouble(6),
                ServiceCharge = r.GetDouble(7),
                Profit = r.GetDouble(8),
                Loss = r.GetDouble(9)
            });
            return list;
        }

        // ── Users ────────────────────────────────────
        public List<User> GetUsers()
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,Username,FullName,Role,IsActive
                FROM Users ORDER BY Role DESC, FullName";
            var list = new List<User>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new User
            {
                Id = r.GetInt32(0),
                Username = r.GetString(1),
                FullName = r.GetString(2),
                Role = r.GetString(3),
                IsActive = r.GetInt32(4) == 1
            });
            return list;
        }

        public void SaveUser(User u, string? plainPin = null)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            if (u.Id == 0)
            {
                string hash = HashPin(plainPin ?? "0000");
                cmd.CommandText = @"INSERT INTO Users(Username,FullName,PinHash,Role,IsActive)
                    VALUES(@u,@f,@p,@r,1)";
                cmd.Parameters.AddWithValue("@u", u.Username);
                cmd.Parameters.AddWithValue("@f", u.FullName);
                cmd.Parameters.AddWithValue("@p", hash);
                cmd.Parameters.AddWithValue("@r", u.Role);
            }
            else
            {
                if (!string.IsNullOrEmpty(plainPin))
                {
                    string hash = HashPin(plainPin);
                    cmd.CommandText = @"UPDATE Users SET
                        Username=@u,FullName=@f,PinHash=@p,Role=@r,IsActive=@a WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@p", hash);
                }
                else
                {
                    cmd.CommandText = @"UPDATE Users SET
                        Username=@u,FullName=@f,Role=@r,IsActive=@a WHERE Id=@id";
                }
                cmd.Parameters.AddWithValue("@u", u.Username);
                cmd.Parameters.AddWithValue("@f", u.FullName);
                cmd.Parameters.AddWithValue("@r", u.Role);
                cmd.Parameters.AddWithValue("@a", u.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", u.Id);
            }
            cmd.ExecuteNonQuery();
        }

        public void DeleteUser(int id)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Users SET IsActive=0 WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        static string HashPin(string pin)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(pin);
            return BitConverter.ToString(sha.ComputeHash(bytes))
                .Replace("-", "").ToLower();
        }

        // ── Shift Summary ────────────────────────────
        public (double Sales, int Orders, double Avg) GetShiftSummary(int shiftId)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(SUM(Total),0),COUNT(*),COALESCE(AVG(Total),0)
                FROM Orders WHERE ShiftId=@sid AND Status NOT IN ('cancelled','held')";
            cmd.Parameters.AddWithValue("@sid", shiftId);
            using var r = cmd.ExecuteReader(); r.Read();
            return (r.GetDouble(0), r.GetInt32(1), r.GetDouble(2));
        }

        // ── GetShiftPayMethods ───────────────────────
        public (double Cash, double Card) GetShiftPayMethods(int shiftId)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COALESCE(SUM(CASE WHEN PayMethod IN ('كاش','نقدي')     THEN Total ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN PayMethod NOT IN ('كاش','نقدي') THEN Total ELSE 0 END), 0)
                FROM Orders
                WHERE ShiftId=@sid
                  AND Status NOT IN ('cancelled','held')";
            cmd.Parameters.AddWithValue("@sid", shiftId);
            using var r = cmd.ExecuteReader(); r.Read();
            return (r.GetDouble(0), r.GetDouble(1));
        }
    }
}