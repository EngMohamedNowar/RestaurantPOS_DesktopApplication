using System;
using System.IO;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// تغطية لـ migrations نفسها.
    ///
    /// لحد دلوقتي كل اختبار في المشروع كان بيعمل الـ schema بإيده في
    /// SetUp. ده معناه إن الـ migrations اللي في DatabaseHelper.Initialize
    /// مكانش ليها أي تغطية إطلاقًا — لو migration كانت غلط في الإنتاج،
    /// الاختبارات كلها هتفضل تنجح. الاختبارات دي بتنداء الكود الحقيقي
    /// على ملف مؤقت.
    /// </summary>
    public class DatabaseMigrationTests : IDisposable
    {
        readonly string _dir;
        readonly string _dbPath;

        public DatabaseMigrationTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "mig_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _dbPath = Path.Combine(_dir, "pos.db");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
        }

        void Open(Action<SqliteConnection> act)
        {
            using var c = new SqliteConnection(DatabaseHelper.CSFor(_dbPath));
            c.Open();
            act(c);
        }

        long Scalar(string sql) { long r = 0; Open(c => { using var k = c.CreateCommand(); k.CommandText = sql; r = Convert.ToInt64(k.ExecuteScalar()); }); return r; }

        void Exec(string sql) => Open(c => { using var k = c.CreateCommand(); k.CommandText = sql; k.ExecuteNonQuery(); });

        [Fact]
        public void Initialize_OnAFreshFile_Succeeds()
        {
            DatabaseHelper.Initialize(_dbPath);

            Assert.True(File.Exists(_dbPath));
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Orders'"));
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='OrderCounters'"));
        }

        [Fact]
        public void Initialize_IsSafeToRunTwice()
        {
            DatabaseHelper.Initialize(_dbPath);
            int productsAfterFirst = (int)Scalar("SELECT COUNT(*) FROM Products");

            DatabaseHelper.Initialize(_dbPath);

            Assert.Equal(productsAfterFirst, Scalar("SELECT COUNT(*) FROM Products"));
        }

        [Fact]
        public void Initialize_SeedsAllThirtyEightProducts()
        {
            DatabaseHelper.Initialize(_dbPath);
            Assert.Equal(38, Scalar("SELECT COUNT(*) FROM Products"));
        }

        [Fact]
        public void Initialize_SeedsRecipesForEveryProduct()
        {
            DatabaseHelper.Initialize(_dbPath);

            Assert.Equal(0, Scalar(@"SELECT COUNT(*) FROM Products p
                WHERE NOT EXISTS (SELECT 1 FROM ProductIngredients pi WHERE pi.ProductId = p.Id)"));
        }

        [Fact]
        public void OrderCounterBackfill_StartsAboveExistingOrders()
        {
            // DB قديمة: فيها أوردرات بترقام قديمة والجدول لسه مش موجود.
            // الـ backfill لازم يطلع العدّاد من فوق أعلى رقم موجود، وإلا
            // هنصدم رقم أوردر موجود من قبل.
            Exec(@"CREATE TABLE Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, OrderNumber TEXT,
                Total REAL DEFAULT 0, Status TEXT DEFAULT 'new');");
            Exec(@"INSERT INTO Orders(OrderNumber,Total) VALUES
                ('20260101-0004',100), ('20260101-0007',100), ('20260102-0002',100);");
            // أوردرات بصيغة تانية (قديم/يدوي) لازم تتجاهلها الـ backfill
            Exec(@"INSERT INTO Orders(OrderNumber,Total) VALUES
                ('LEGACY-12',100), ('20260101-ABC',100);");

            DatabaseHelper.Initialize(_dbPath);

            Assert.Equal(7, Scalar("SELECT LastNumber FROM OrderCounters WHERE Day='20260101'"));
            Assert.Equal(2, Scalar("SELECT LastNumber FROM OrderCounters WHERE Day='20260102'"));
        }

        [Fact]
        public void OrderCounterBackfill_DoesNotLowerAnExistingCounter()
        {
            // لو الـ backfill اتنفذ تاني لازم ماينزلش العدّاد تحت
            // القيمة الموجودة.
            DatabaseHelper.Initialize(_dbPath);
            Exec("INSERT INTO OrderCounters(Day,LastNumber) VALUES('20260101',42);");
            Exec(@"INSERT INTO Orders(OrderNumber,Total) VALUES('20260101-0009',100);");

            DatabaseHelper.Initialize(_dbPath);

            Assert.Equal(42, Scalar("SELECT LastNumber FROM OrderCounters WHERE Day='20260101'"));
        }

        [Fact]
        public void OrderCounter_IssuesConsecutiveNumbers()
        {
            DatabaseHelper.Initialize(_dbPath);
            var db = new AppDbContext(DatabaseHelper.CSFor(_dbPath));

            var first = NextNumber(db);
            var second = NextNumber(db);
            var third = NextNumber(db);

            Assert.EndsWith("-0001", first);
            Assert.EndsWith("-0002", second);
            Assert.EndsWith("-0003", third);
        }

        [Fact]
        public void OrderCounter_NumbersRestartEachDay()
        {
            DatabaseHelper.Initialize(_dbPath);
            var db = new AppDbContext(DatabaseHelper.CSFor(_dbPath));

            string a = NextNumber(db);
            Exec("INSERT INTO OrderCounters(Day,LastNumber) VALUES('19990101',500);");

            string b = NextNumber(db);

            Assert.Equal(a[..8], b[..8]);   // نفس اليوم => نفس البادئة
            Assert.EndsWith("-0002", b);
        }

        [Fact]
        public void OrderCounter_RollsBackWithItsTransaction()
        {
            // أهم واحد: لو الـ commit فشل، العدّاد لازم يرجع. غير كده
            // بيظهر فجوة في ترقيم الأوردرات.
            DatabaseHelper.Initialize(_dbPath);
            var db = new AppDbContext(DatabaseHelper.CSFor(_dbPath));

            using (var c = db.OpenConnection())
            using (var tx = c.BeginTransaction())
            {
                db.GetNextOrderNumber(c, tx);
                db.GetNextOrderNumber(c, tx);
                // نرمي من غير commit => rollback
            }

            Assert.EndsWith("-0001", NextNumber(db));
        }

        [Fact]
        public void OpenShift_UniqueIndexSurvivesRealInitialize()
        {
            // الـ index كان متضاف في migration، والمigration نفسها كانت
            // بتتنفذ قبل إنشاء جدول Shifts — فأي تثبيت جديد كان بيقفل.
            // الاختبار ده بيمشي على الـ Initialize الحقيقية عشان يتأكد
            // إن الـ index موجود فعلاً وبيشتغل.
            DatabaseHelper.Initialize(_dbPath);
            Exec(@"INSERT INTO Users(Username,FullName,PinHash,Role)
                   VALUES('t','Tester','x','cashier');");
            int uid = (int)Scalar("SELECT Id FROM Users WHERE Username='t'");

            Exec($"INSERT INTO Shifts(UserId,Status,OpeningCash) VALUES({uid},'open',0);");

            // الوردية التانية المفتوحة لنفس اليوزر لازم يرفضها الـ DB.
            var ex = Assert.Throws<SqliteException>(() =>
                Exec($"INSERT INTO Shifts(UserId,Status,OpeningCash) VALUES({uid},'open',0);"));
            Assert.Contains("UNIQUE", ex.Message);

            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM Shifts WHERE UserId=" + uid + " AND Status='open'"));

            // يقفل الوردية، يبقى يقدر يفتح تانية
            Exec($"UPDATE Shifts SET Status='closed' WHERE UserId={uid};");
            Exec($"INSERT INTO Shifts(UserId,Status,OpeningCash) VALUES({uid},'open',0);");
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM Shifts WHERE UserId=" + uid + " AND Status='open'"));
        }

        static string NextNumber(AppDbContext db)
        {
            using var c = db.OpenConnection();
            using var tx = c.BeginTransaction();
            string n = db.GetNextOrderNumber(c, tx);
            tx.Commit();
            return n;
        }
    }
}
