using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Services;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// تسوية الوردية — المكان اللي بيتحسب فيه فلوس الخزنة الفعلية.
    ///
    /// الـ regression الأساسي هنا: البيع في الكاشير بيتحفظ بـ
    /// Status="new" (شاشة المطبخ هي اللي بتنقله بعدين لـ completed)،
    /// والخدمة كانت بتفلتر على 'completed' بس ⇒ expectedCash كانت
    /// فلوس البداية صفر، وتقفيل الوردية بيقول إن الكاشير ضيّع كل
    /// مبيعات النقدية.
    /// </summary>
    public class ShiftServiceTests : IDisposable
    {
        readonly string _root;
        readonly string _dbPath;
        readonly string _cs;
        readonly ShiftService _svc;

        const string Cash = "كاش";
        const string Card = "بطاقة/فيزا";

        public ShiftServiceTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "pizzapos-shift", Guid.NewGuid().ToString("N"));
            _dbPath = Path.Combine(_root, "pos.db");
            Directory.CreateDirectory(_root);
            _cs = DatabaseHelper.CSFor(_dbPath);
            CreateSchema();
            _svc = new ShiftService(_cs);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
            catch { }
        }

        void CreateSchema()
        {
            using var c = new SqliteConnection(_cs);
            c.Open();
            DatabaseHelper.Configure(c);
            Exec(c, @"CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT, FullName TEXT, Role TEXT, IsActive INTEGER DEFAULT 1);");
            Exec(c, @"CREATE TABLE Shifts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER, OpeningCash REAL DEFAULT 0, ClosingCash REAL,
                ExpectedCash REAL, Difference REAL,
                OpenedAt TEXT DEFAULT (datetime('now','localtime')),
                ClosedAt TEXT, Status TEXT DEFAULT 'open');");
            Exec(c, @"CREATE TABLE Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ShiftId INTEGER, UserId INTEGER, OrderNumber TEXT,
                PayMethod TEXT, Subtotal REAL, Discount REAL, Tax REAL,
                ServiceCharge REAL, Total REAL, Status TEXT,
                CreatedAt TEXT DEFAULT (datetime('now','localtime')));");
            Exec(c, "INSERT INTO Users(Id,Username,FullName,Role) VALUES(1,'admin','المدير','admin');");
        }

        static void Exec(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        /// <summary>بيضيف أوردر بالـ status اللي الكاشير بيحفظ بيه فعلاً.</summary>
        void AddOrder(int shiftId, double total, string payMethod, string status)
        {
            using var c = new SqliteConnection(_cs);
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO Orders
                (ShiftId,UserId,OrderNumber,PayMethod,Subtotal,Total,Status)
                VALUES(@s,1,'1',@p,@t,@t,@st)";
            cmd.Parameters.AddWithValue("@s", shiftId);
            cmd.Parameters.AddWithValue("@p", payMethod);
            cmd.Parameters.AddWithValue("@t", total);
            cmd.Parameters.AddWithValue("@st", status);
            cmd.ExecuteNonQuery();
        }

        // ── الـ regression الأساسي ───────────────────

        [Fact]
        public void CashSale_SavedAsNew_CountsInReconciliation()
        {
            // الـ bug: Status="new" مكنش بيتحسب خالص
            var shift = _svc.OpenShift(1, 200);
            AddOrder(shift.Id, 150, Cash, "new");       // زي ما الكاشير بيحفظ

            var closed = _svc.CloseShift(shift.Id, closingCash: 350);

            Assert.Equal(350, closed.ExpectedCash);     // 200 بداية + 150 بيع
            Assert.Equal(0, closed.Difference);
            Assert.Equal(1, closed.OrderCount);
        }

        [Theory]
        [InlineData("new")]
        [InlineData("kitchen")]
        [InlineData("في المطبخ")]
        [InlineData("جاهز")]
        [InlineData("ready")]
        [InlineData("delivery")]
        [InlineData("قيد التوصيل")]
        [InlineData("completed")]
        public void EveryStageStatus_CountsAsASale(string status)
        {
            // أي حالة وصل بيها الأوردر = اتحصّل فلوسه. مفيش حالة
            // يقعد فيها الفلوس مش محسوبة.
            var shift = _svc.OpenShift(1, 0);
            AddOrder(shift.Id, 100, Cash, status);

            var closed = _svc.CloseShift(shift.Id, closingCash: 100);

            Assert.Equal(100, closed.ExpectedCash);
            Assert.Equal(0, closed.Difference);
        }

        // ── اللي مينفعش يتحسب ───────────────────────

        [Fact]
        public void CancelledOrder_DoesNotCount()
        {
            var shift = _svc.OpenShift(1, 100);
            AddOrder(shift.Id, 200, Cash, "cancelled");

            var closed = _svc.CloseShift(shift.Id, closingCash: 100);

            Assert.Equal(100, closed.ExpectedCash);
            Assert.Equal(0, closed.Difference);
            Assert.Equal(0, closed.OrderCount);
        }

        [Fact]
        public void HeldOrder_DoesNotCount()
        {
            // أوردر معلّق لسه ماتفحش
            var shift = _svc.OpenShift(1, 100);
            AddOrder(shift.Id, 500, Cash, "held");

            var closed = _svc.CloseShift(shift.Id, closingCash: 100);

            Assert.Equal(100, closed.ExpectedCash);
            Assert.Equal(0, closed.Difference);
        }

        [Fact]
        public void EmptyStatus_DoesNotThrowAndCountsAsSale()
        {
            // DatabaseHelper بيعمل migration بيحوّل NULL/'' لـ completed،
            // بس لازم نتحملوا لو slipped.
            var shift = _svc.OpenShift(1, 0);
            AddOrder(shift.Id, 75, Cash, "");

            var closed = _svc.CloseShift(shift.Id, closingCash: 75);

            Assert.Equal(75, closed.ExpectedCash);
        }

        // ── الفيزياء ────────────────────────────────

        [Fact]
        public void CardSales_NotInExpectedCash()
        {
            // الفلوس اللي راحت على البطاقة مش في الخزنة
            var shift = _svc.OpenShift(1, 100);
            AddOrder(shift.Id, 500, Card, "new");

            var closed = _svc.CloseShift(shift.Id, closingCash: 100);

            Assert.Equal(100, closed.ExpectedCash);
            Assert.Equal(0, closed.Difference);
            Assert.Equal(500, closed.TotalSales);   // بس مش في النقدية
        }

        [Fact]
        public void MixedPayments_OnlyCashGoesToDrawer()
        {
            var shift = _svc.OpenShift(1, 0);
            AddOrder(shift.Id, 100, Cash, "new");
            AddOrder(shift.Id, 200, Cash, "kitchen");
            AddOrder(shift.Id, 400, Card, "new");
            AddOrder(shift.Id, 50, Cash, "cancelled");

            var closed = _svc.CloseShift(shift.Id, closingCash: 300);

            Assert.Equal(300, closed.ExpectedCash);   // 100 + 200
            Assert.Equal(0, closed.Difference);
            Assert.Equal(700, closed.TotalSales);     // + 400 بالكارت
            Assert.Equal(3, closed.OrderCount);       // الملغي مش محسوب
        }

        [Fact]
        public void RealShortage_ShowsAsNegativeDifference()
        {
            // فلوس ناقصة فعلاً في الخزنة
            var shift = _svc.OpenShift(1, 100);
            AddOrder(shift.Id, 300, Cash, "new");

            var closed = _svc.CloseShift(shift.Id, closingCash: 200);

            Assert.Equal(400, closed.ExpectedCash);
            Assert.Equal(-200, closed.Difference);
        }

        [Fact]
        public void RealSurplus_ShowsAsPositiveDifference()
        {
            var shift = _svc.OpenShift(1, 100);
            AddOrder(shift.Id, 300, Cash, "new");

            var closed = _svc.CloseShift(shift.Id, closingCash: 500);

            Assert.Equal(400, closed.ExpectedCash);
            Assert.Equal(100, closed.Difference);
        }

        [Fact]
        public void OrdersFromAnotherShift_NotCounted()
        {
            // الورديتين لازم تكونوا متتاليتين مش مفتوحين في نفس الوقت.
            // الاختبار الأول كان بيفتح ورديتين لنفس اليوزر وده بقى ممنوع.
            var shiftA = _svc.OpenShift(1, 100);
            _svc.CloseShift(shiftA.Id, 100);

            var shiftB = _svc.OpenShift(1, 0);
            AddOrder(shiftB.Id, 999, Cash, "new");

            var closedB = _svc.CloseShift(shiftB.Id, closingCash: 999);

            // الوردية الأولى اتقفلت قبل ما الـ 999 تتحصل، فـ ExpectedCash
            // بتاعتها لازم تفضل 100 والخسارة صفر.
            var historyA = _svc.GetHistory()
                .First(x => x.Id == shiftA.Id);
            Assert.Equal(100, historyA.ExpectedCash);
            Assert.Equal(0, historyA.Difference);
            Assert.Equal(0, historyA.OrderCount);

            // والتسوية بتبقى على basis كل أوردر في ورديته بس
            Assert.Equal(999, closedB.ExpectedCash);
            Assert.Equal(0, closedB.Difference);
            Assert.Equal(1, closedB.OrderCount);
        }

        // ── GetOpenShift ────────────────────────────

        [Fact]
        public void GetOpenShift_ReportsSalesWhileShiftIsOpen()
        {
            // العرض أثناء الوردية لازم يطابق التسوية
            var shift = _svc.OpenShift(1, 200);
            AddOrder(shift.Id, 150, Cash, "new");
            AddOrder(shift.Id, 50, Card, "new");

            var open = _svc.GetOpenShift(1);

            Assert.NotNull(open);
            Assert.Equal(200, open.TotalSales);
            Assert.Equal(2, open.OrderCount);
        }

        [Fact]
        public void GetOpenShift_ReturnsNullAfterClose()
        {
            var shift = _svc.OpenShift(1, 100);
            _svc.CloseShift(shift.Id, 100);

            Assert.Null(_svc.GetOpenShift(1));
        }

        // ── مانلوش ورديتين مفتوحتين ─────────────────

        [Fact]
        public void OpeningTwice_Throws()
        {
            _svc.OpenShift(1, 100);

            var ex = Assert.Throws<InvalidOperationException>(
                () => _svc.OpenShift(1, 200));
            Assert.Contains("وردية مفتوحة", ex.Message);
        }

        [Fact]
        public void CanReopenAfterClosing()
        {
            var first = _svc.OpenShift(1, 100);
            _svc.CloseShift(first.Id, 100);

            var second = _svc.OpenShift(1, 50);

            Assert.NotEqual(first.Id, second.Id);
            Assert.Equal(50, second.OpeningCash);
        }

        [Fact]
        public void OtherUser_CanHaveTheirOwnOpenShift()
        {
            // القيد على اليوزر نفسه، مش على المطعم كله
            using (var c = new SqliteConnection(_cs))
            {
                c.Open();
                Exec(c, "INSERT INTO Users(Id,Username,FullName,Role) VALUES(2,'cashier2','كاشير','cashier');");
            }

            var shift1 = _svc.OpenShift(1, 100);
            var shift2 = _svc.OpenShift(2, 50);

            Assert.NotEqual(shift1.Id, shift2.Id);
        }

        // ── السجل ──────────────────────────────────

        [Fact]
        public void History_MatchesTheCloseNumbers()
        {
            // تاريخ الورديات لازم يطابق اللي اتقفل بيه، وإلا التقارير
            // بتختلف عن تقفيل الخزنة لنفس الوردية.
            var shift = _svc.OpenShift(1, 200);
            AddOrder(shift.Id, 150, Cash, "new");
            var closed = _svc.CloseShift(shift.Id, 350);

            var history = _svc.GetHistory();
            var row = Assert.Single(history);

            Assert.Equal(closed.ExpectedCash, row.ExpectedCash);
            Assert.Equal(closed.Difference, row.Difference);
            Assert.Equal(closed.TotalSales, row.TotalSales);
            Assert.Equal(150, row.TotalSales);
            Assert.Equal("closed", row.Status);
        }
    }
}
