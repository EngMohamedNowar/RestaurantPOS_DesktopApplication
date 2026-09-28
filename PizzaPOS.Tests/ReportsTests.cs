using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// حسابات التقارير: ربح/خسارة اليوم، سجل الخسائر، الأكثر مبيع.
    ///
    /// Bugs اللي الاختبارات دي بتمنع رجوعها:
    /// 1) "بيع بأقل من التكلفة" كان بيقارن التكلفة بـ BasePrice بدل
    ///    صافي إيراد السطر بعد الخصم (الحجم والإضافات جوّه Subtotal) —
    ///    بيفوّت أوردرات اتخصمت وبتطلّع خسائر وهمية للحدات.
    /// 2) الربح كان بيطرح Orders.Tax مع إنها مضافّة فوق الإجمالي في
    ///    OrderCalculator — يعني طرحها تاني وبيبخّض الربح.
    /// </summary>
    public class ReportsTests : IDisposable
    {
        readonly string _dbPath;
        readonly AppDbContext _db;

        public ReportsTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "rep_" + Guid.NewGuid().ToString("N") + ".db");
            DatabaseHelper.Initialize(_dbPath);
            _db = new AppDbContext(DatabaseHelper.CSFor(_dbPath));
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        }

        long InsertOrder(double subtotal, double discount = 0, double tax = 0, double service = 0)
        {
            using var c = new SqliteConnection(DatabaseHelper.CSFor(_dbPath));
            c.Open();
            using (var ins = c.CreateCommand())
            {
                ins.CommandText = @"INSERT INTO Orders(OrderNumber,Subtotal,Discount,Tax,ServiceCharge,Total,Status)
                    VALUES(@n,@s,@d,@t,@v,@tot,'completed')";
                ins.Parameters.AddWithValue("@n", "T-" + Guid.NewGuid().ToString("N")[..12]);
                ins.Parameters.AddWithValue("@s", subtotal);
                ins.Parameters.AddWithValue("@d", discount);
                ins.Parameters.AddWithValue("@t", tax);
                ins.Parameters.AddWithValue("@v", service);
                ins.Parameters.AddWithValue("@tot", subtotal - discount + tax + service);
                ins.ExecuteNonQuery();
            }
            using (var id = c.CreateCommand())
            {
                id.CommandText = "SELECT last_insert_rowid()";
                return Convert.ToInt64(id.ExecuteScalar());
            }
        }

        long InsertItem(long orderId, double unitPrice, double unitCost, int qty, double? subtotal = null)
        {
            using var c = new SqliteConnection(DatabaseHelper.CSFor(_dbPath));
            c.Open();
            using (var ins = c.CreateCommand())
            {
                // Price = السعر الأساسي، Subtotal = الإيراد الفعلي (يشمل
                // زيادة الحجم والإضافات) × الكمية — نفس ما بينزل وقت الدفع.
                ins.CommandText = @"INSERT INTO OrderItems(OrderId,ProductId,Name,Price,Cost,Qty,Subtotal)
                    VALUES(@oid,1,'Test Item',@p,@co,@q,@s)";
                ins.Parameters.AddWithValue("@oid", orderId);
                ins.Parameters.AddWithValue("@p", unitPrice);
                ins.Parameters.AddWithValue("@co", unitCost);
                ins.Parameters.AddWithValue("@q", qty);
                ins.Parameters.AddWithValue("@s", subtotal ?? unitPrice * qty);
                ins.ExecuteNonQuery();
            }
            using (var id = c.CreateCommand())
            {
                id.CommandText = "SELECT last_insert_rowid()";
                return Convert.ToInt64(id.ExecuteScalar());
            }
        }

        [Fact]
        public void BelowCost_UsesNetRevenueAfterDiscount()
        {
            // سطر بسعر 150 وتكلفة 100 اتخصم منه 70 → الإيراد الصافي 80
            // → خسارة 20. المقارنة القديمة (التكلفة 100 < BasePrice 150)
            // كانت بتعدّي السطر ده من غير ما تسجّل خسارة أصلاً.
            long oid = InsertOrder(subtotal: 150, discount: 70);
            InsertItem(oid, unitPrice: 150, unitCost: 100, qty: 1);

            var below = _db.GetLossesSummary(DateTime.Today, DateTime.Today)
                .Where(l => l.Type == "بيع بأقل من التكلفة").ToList();

            var row = Assert.Single(below);
            Assert.Equal(20, row.Amount, 2);

            var (profit, loss) = _db.GetTodayProfitLoss();
            Assert.Equal(80 - 100, profit, 2);
            Assert.Equal(20, loss, 2);

            // إجمالي الخسائر = يدوية (0) + تحت التكلفة (20) + خصومات (70)
            Assert.Equal(90, _db.GetTotalLosses(DateTime.Today, DateTime.Today), 2);
        }

        [Fact]
        public void BelowCost_IgnoresBasePriceWhenSizeAndExtrasCoverCost()
        {
            // تكلفة 160 والسعر الأساسي 150 — بس الحجم/الإضافات رفعوا
            // الإيراد الفعلي لـ 200 → مفيش خسارة. الكود القديم كان
            // يشوف (160 > 150) ويطلّع خسارة وهمية 10.
            long oid = InsertOrder(subtotal: 200);
            InsertItem(oid, unitPrice: 150, unitCost: 160, qty: 1, subtotal: 200);

            Assert.Equal(0, _db.GetLossesSummary(DateTime.Today, DateTime.Today)
                .Count(l => l.Type == "بيع بأقل من التكلفة"));

            var (profit, loss) = _db.GetTodayProfitLoss();
            Assert.Equal(200 - 160, profit, 2);
            Assert.Equal(0, loss, 2);
        }

        [Fact]
        public void Profit_DoesNotSubtractCollectedTaxTwice()
        {
            // إيراد الأصناف 150 وتكلفة 49.3 → ربح 100.7. الضريبة 21
            // مضافّة فوق الإجمالي للزبون وموجودة في Orders.Tax — مش
            // جزء من إيراد الأصناف، فطرحها كان طرحها تاني (كان بيبخّض
            // الربح لـ 79.7).
            long oid = InsertOrder(subtotal: 150, tax: 21);
            InsertItem(oid, unitPrice: 150, unitCost: 49.3, qty: 1);

            var (profit, loss) = _db.GetTodayProfitLoss();
            Assert.Equal(150 - 49.3, profit, 2);
            Assert.Equal(0, loss, 2);

            var day = Assert.Single(_db.GetDailyRange(DateTime.Today, DateTime.Today));
            Assert.Equal(150 - 49.3, day.Profit, 2);
            Assert.Equal(171, day.Sales, 2);         // Total = 150 + ضريبة 21
            Assert.Equal(21, day.Tax, 2);
        }

        [Fact]
        public void TopProductsNegativeProfit_MatchesTheBelowCostLoss()
        {
            // 2 × بسعر 150/تكلفة 120 مع خصم 120 → صافي 180 مقابل تكلفة
            // 240 → ربح الأصناف −60، ونفس الفرق لازم يظهر في سجل
            // الخسائر كـ"بيع بأقل من التكلفة" 60 — التقارير ماتبقاش
            // متناقضة (ربح بالسالب من غير خسارة مسجّلة).
            long oid = InsertOrder(subtotal: 300, discount: 120);
            InsertItem(oid, unitPrice: 150, unitCost: 120, qty: 2, subtotal: 300);

            var item = Assert.Single(_db.GetTopProducts(DateTime.Today, DateTime.Today));
            Assert.Equal(180 - 240, item.Profit, 2);

            var below = Assert.Single(_db.GetLossesSummary(DateTime.Today, DateTime.Today),
                l => l.Type == "بيع بأقل من التكلفة");
            Assert.Equal(60, below.Amount, 2);
            Assert.Equal(-item.Profit, below.Amount, 2);
        }

        [Fact]
        public void DailyLossColumn_IncludesManualAndBelowCostRows()
        {
            long oid = InsertOrder(subtotal: 150, discount: 70);
            InsertItem(oid, unitPrice: 150, unitCost: 100, qty: 1);

            using (var c = new SqliteConnection(DatabaseHelper.CSFor(_dbPath)))
            {
                c.Open();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"INSERT INTO Losses(Date,Type,Description,Amount,CreatedBy)
                    VALUES(date('now','localtime'),'مصروف تشغيلي','بنزين توصيل',50,1)";
                cmd.ExecuteNonQuery();
            }

            var day = Assert.Single(_db.GetDailyRange(DateTime.Today, DateTime.Today));
            Assert.Equal(50 + 20, day.Loss, 2);      // يدوي + تحت التكلفة
            Assert.Equal(80 - 100, day.Profit, 2);   // صافي بعد الخصم، من غير طرح ضريبة
        }
    }
}
