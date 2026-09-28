using System.IO;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.Services;
using PizzaPOS.ViewModels;
using Xunit;

namespace PizzaPOS.Tests;

/// <summary>
/// ذرّية الدفع: الأوردر + خصم المخزون + نقاط الولاء = عملية واحدة.
///
/// الكود القديم كان بينفّذ التلاتة كتلات مستقلة على نفس الملف.
/// لو اتحفظ الأوردر وفشل خصم المخزون، النتيجة كانت بيع مسجّل
/// بمخزون منقوص — يعني المخزون بيكذب، والفرق في الوردية بيطلع غلط،
/// ومفيش أي أثر يبقى بيقولنا إن العملية اتقطعت في النص.
///
/// الاختبارات دي بتثبت إن أي فشل في أي خطوة بيلغي الخطوات اللي
/// قبلها. التحقق إن الـ operations القديمة (من غير transaction مشترك)
/// لسه شغالة كمان، عشان الـ callers التانية ما تتكسرش.
/// </summary>
public class CheckoutAtomicityTests : IDisposable
{
    readonly string _root;
    readonly string _dbPath;
    readonly string _cs;
    readonly AppDbContext _db;
    readonly InventoryService _inv;

    public CheckoutAtomicityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pizzapos-checkout", Guid.NewGuid().ToString("N"));
        _dbPath = Path.Combine(_root, "pos.db");
        Directory.CreateDirectory(_root);
        _cs = DatabaseHelper.CSFor(_dbPath);
        _db = new AppDbContext(_cs);
        _inv = new InventoryService(_cs);
        CreateSchema();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
        catch { }
    }

    // ── schema مطابق لـ DatabaseHelper.Initialize ─────────────

    void CreateSchema()
    {
        using var c = Open();
        Exec(c, @"CREATE TABLE Ingredients (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, CategoryId INTEGER,
            Name TEXT NOT NULL, Unit TEXT NOT NULL,
            Stock REAL DEFAULT 0, MinStock REAL DEFAULT 0,
            CostPerUnit REAL DEFAULT 0, IsActive INTEGER DEFAULT 1);");
        Exec(c, @"CREATE TABLE ProductIngredients (
            ProductId INTEGER, IngredientId INTEGER REFERENCES Ingredients(Id),
            QtyUsed REAL NOT NULL, PRIMARY KEY(ProductId, IngredientId));");
        Exec(c, @"CREATE TABLE StockMovements (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            IngredientId INTEGER REFERENCES Ingredients(Id),
            Type TEXT NOT NULL, Qty REAL NOT NULL, Note TEXT,
            UserId INTEGER, CreatedAt TEXT DEFAULT (datetime('now','localtime')));");
        Exec(c, @"CREATE TABLE Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, CategoryId INTEGER, Name TEXT NOT NULL,
            Price REAL NOT NULL, Cost REAL DEFAULT 0, IsActive INTEGER DEFAULT 1);");
        Exec(c, @"CREATE TABLE Customers (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Phone TEXT,
            Address TEXT, Notes TEXT, LoyaltyPoints INTEGER DEFAULT 0);");
        Exec(c, @"CREATE TABLE Orders (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, OrderNumber TEXT, ShiftId INTEGER,
            UserId INTEGER, OrderType TEXT, PayMethod TEXT, Subtotal REAL DEFAULT 0,
            Discount REAL DEFAULT 0, Tax REAL DEFAULT 0, ServiceCharge REAL DEFAULT 0,
            Total REAL DEFAULT 0, PaidAmount REAL DEFAULT 0, Change REAL DEFAULT 0,
            Notes TEXT, CustomerId INTEGER, CustomerName TEXT, CustomerPhone TEXT,
            DeliveryAddress TEXT, DriverId INTEGER, DriverName TEXT, DeliveryFee REAL DEFAULT 0,
            DeliveryStatus TEXT, Status TEXT DEFAULT 'new',
            CreatedAt TEXT DEFAULT (datetime('now','localtime')));");
        Exec(c, @"CREATE TABLE OrderItems (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, OrderId INTEGER REFERENCES Orders(Id),
            ProductId INTEGER, Name TEXT, Price REAL DEFAULT 0, Cost REAL DEFAULT 0,
            Qty REAL DEFAULT 0, Subtotal REAL DEFAULT 0, SizeName TEXT, ExtrasNote TEXT,
            SizeExtraPrice REAL DEFAULT 0, ExtrasPrice REAL DEFAULT 0);");
        Exec(c, @"CREATE TABLE OrderCounters (
            Day TEXT PRIMARY KEY, LastNumber INTEGER NOT NULL DEFAULT 0);");

        Exec(c, "INSERT INTO Ingredients(Id,Name,Unit,Stock,CostPerUnit,IsActive) VALUES(1,'عجين','كجم',10,5,1);");
        Exec(c, "INSERT INTO Products(Id,Name,Price) VALUES(1,'مارgrande',100);");
        Exec(c, "INSERT INTO ProductIngredients(ProductId,IngredientId,QtyUsed) VALUES(1,1,0.5);");
        Exec(c, "INSERT INTO Customers(Id,Name,Phone,LoyaltyPoints) VALUES(7,'سارة','0100',20);");
    }

    SqliteConnection Open() { var c = new SqliteConnection(_cs); c.Open(); DatabaseHelper.Configure(c); return c; }
    static void Exec(SqliteConnection c, string sql) { using var k = c.CreateCommand(); k.CommandText = sql; k.ExecuteNonQuery(); }

    long Scalar(string sql)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = sql;
        return Convert.ToInt64(k.ExecuteScalar());
    }

    /// <summary>للأرقام الحقيقية. Scalar ب Convert.ToInt64 بيقرّب 9.5 لـ 10،
    /// فأي assertion على Stock لازم يروح هنا.</summary>
    double Real(string sql)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = sql;
        return Convert.ToDouble(k.ExecuteScalar());
    }

    Order MakeOrder(int customerId = 0, double total = 100) => new()
    {
        OrderNumber = "20260101-0001",
        ShiftId = 1,
        UserId = 1,
        OrderType = "dine-in",
        PayMethod = "كاش",
        Subtotal = total, Total = total, PaidAmount = total, Change = 0,
        CustomerId = customerId,
        Status = OrderStatus.New,
        Items = new List<OrderItem>
        {
            new() { ProductId = 1, Name = "مارgrande", BasePrice = total, Qty = 1 }
        }
    };

    /// <summary>
    /// نفس اللي MainViewModel.CommitCheckout بيعمله بالظبط.
    /// التكرار هنا مقصود: لو الـ checkout في الـ ViewModel اتغيّر،
    /// الاختبار ده لازم يقودنا نحدّثه (ولو يبقى_skipped نشوف).
    /// </summary>
    StockDeductionResult CommitCheckout(Order order)
        => DbErrors.RetryOnBusy(() =>
        {
            using var conn = _db.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                order.OrderNumber = _db.GetNextOrderNumber(conn, tx);
                _db.SaveOrder(order, conn, tx);
                var res = _inv.DeductForOrder(
                    order.Items, order.UserId, order.OrderNumber, conn, tx);
                if (order.CustomerId > 0)
                    _db.AddLoyaltyPoints(
                        order.CustomerId,
                        (int)(order.Total / MainViewModel.LoyaltyPointsPerEgp), conn, tx);
                tx.Commit();
                return res;
            }
            catch { tx.Rollback(); throw; }
        });

    // ── المسار السليم ─────────────────────────────────────

    [Fact]
    public void Success_CommitsAllThreeTogether()
    {
        CommitCheckout(MakeOrder(customerId: 7));

        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM Orders"));
        Assert.Equal(9.5, Real("SELECT Stock FROM Ingredients WHERE Id=1"));   // 10 - 0.5
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM StockMovements"));
        Assert.Equal(30, Real("SELECT LoyaltyPoints FROM Customers WHERE Id=7")); // 20 + 10
    }

    [Fact]
    public void Success_OrderItemsAreCommittedToo()
    {
        CommitCheckout(MakeOrder());

        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM OrderItems"));
    }

    // ── ترقيم الأوردرات: مفيش فجوات ──────────────────────

    [Fact]
    public void OrderNumbersAreSequentialAcrossSuccessfulCheckouts()
    {
        CommitCheckout(MakeOrder());
        CommitCheckout(MakeOrder());
        CommitCheckout(MakeOrder());

        var numbers = Numbers();
        Assert.Equal(3, numbers.Count);
        Assert.Equal(DateTime.Now.ToString("yyyyMMdd"), numbers[0].Day);
        Assert.Equal(new long[] { 1, 2, 3 }, numbers.ConvertAll(n => n.Seq));
    }

    [Fact]
    public void RolledBackCheckout_DoesNotBurnAnOrderNumber()
    {
        // ده بالظبط الـ bug القديم: الرقم كان بيتحسب بـ COUNT(*)+1
        // برّه الـ transaction، فلو الـ commit فشل الرقم كان بيتبتلع
        // وبيبقى فيه فجوة في الترقيم.
        using (var c = Open())
            Exec(c, "DROP TABLE Customers;");
        Assert.Throws<SqliteException>(() => CommitCheckout(MakeOrder(customerId: 7)));

        // لازم العدداد يرجع صفر
        Assert.Equal(0, Scalar("SELECT COALESCE(SUM(LastNumber),0) FROM OrderCounters"));

        CommitCheckout(MakeOrder());
        Assert.Equal(1, Numbers()[0].Seq);
    }

    [Fact]
    public void CancelledOrders_DoNotCreateGaps()
    {
        // الكود القديم كان COUNT(*)+1 فوق جدول Orders، فأي أوردر ملغى
        // كان بيعمل فجوة مرئية في الترقيم. الإلغاء في التطبيق بيغيّر
        // الـ Status مش بيمسح السطر، بس الجواب لازم يبقى واحد:
        // العدّاد مستقل عن جدول الأوردرات.
        CommitCheckout(MakeOrder());
        CommitCheckout(MakeOrder());
        using (var c = Open())
            Exec(c, "UPDATE Orders SET Status='cancelled' WHERE OrderNumber LIKE '%-0002';");

        CommitCheckout(MakeOrder());
        Assert.Equal(3, Numbers().ConvertAll(n => n.Seq)[2]);
    }

    [Fact]
    public void PreviewNumber_DoesNotConsumeTheCounter()
    {
        // GetNextOrderNumber() بدون transaction دالة معاينة بتظهر للمستخدم.
        // لو هي حجزت الرقم كانت هتبوظ الترقيم، ولو الرقم اللي بتعرضه
        // مختلف عن اللي هيحصل فعلاً ده بيبوّض الكاشير.
        var preview = _db.GetNextOrderNumber();
        var preview2 = _db.GetNextOrderNumber();
        Assert.Equal(preview, preview2);

        CommitCheckout(MakeOrder());
        Assert.Equal(preview, Numbers()[0].Number);
    }

    List<(string Day, long Seq, string Number)> Numbers()
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "SELECT OrderNumber FROM Orders ORDER BY Id";
        var list = new List<(string, long, string)>();
        using var r = k.ExecuteReader();
        while (r.Read())
        {
            var s = r.GetString(0);
            if (s.Length < 10) continue;
            list.Add((s[..8], long.Parse(s[9..]), s));
        }
        return list;
    }

    // ── الفشل لازم يرجّع كل الخطوة اللي قبله ─────────────

    [Fact]
    public void LoyaltyFailure_RollsBackOrderAndStock()
    {
        // نكسر جدول العملاء بعد ما الأوردر اتحفظ والخصون
        // اتنفّذوا جوّه نفس الـ transaction. الـ UPDATE لازم يفشل،
        // والـ rollback لازم ياخد الأوردر والخصم معاه.
        using (var c = Open())
            Exec(c, "DROP TABLE Customers;");

        Assert.Throws<SqliteException>(() => CommitCheckout(MakeOrder(customerId: 7)));

        // مفيش بيع نصه محفوظ
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM Orders"));
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM OrderItems"));
        // والمخزون زي ما كان — الخصم اتلغى
        Assert.Equal(10, Real("SELECT Stock FROM Ingredients WHERE Id=1"));
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM StockMovements"));
    }

    [Fact]
    public void StockFailure_RollsBackTheOrder()
    {
        // العكس: نكسر المخزون. الأوردر اتحفظ الأول، فلو الـ rollback
        // مش شغال هنلقى بيع مسجّل بمخزون ما اتخصمش منه حاجة.
        using (var c = Open())
            Exec(c, "DROP TABLE StockMovements;");

        Assert.Throws<SqliteException>(() => CommitCheckout(MakeOrder()));

        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM Orders"));
    }

    [Fact]
    public void AfterRollback_ConnectionIsStillUsable()
    {
        // الـ rollback بيرجّع الـ connection لحالة صالحة. لو الـ
        // connection اتخرمت، الكاشير مش هيقدر يكمّل أي عملية تالية.
        using (var c = Open())
            Exec(c, "DROP TABLE Customers;");

        Assert.Throws<SqliteException>(() => CommitCheckout(MakeOrder(customerId: 7)));

        // عملية جديدة على connection جديد تنجح عادي
        using var c2 = Open();
        using var tx = c2.BeginTransaction();
        _db.SaveOrder(MakeOrder(), c2, tx);
        tx.Commit();
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM Orders"));
    }

    // ── الـ callers القديمة لسه شغالة ──────────────────────

    [Fact]
    public void StandaloneSaveOrder_StillCommitsOnItsOwn()
    {
        // الـ overload القديم (transaction خاص بيه) لسه مستخدم
        // في أماكن تانية، فلازم يفضل شغّال.
        _db.SaveOrder(MakeOrder());

        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM Orders"));
    }

    [Fact]
    public void StandaloneDeductForOrder_StillCommitsOnItsOwn()
    {
        var res = _inv.DeductForOrder(MakeOrder().Items, 1, "20260101-0001");

        Assert.Equal(1, res.DeductedLines);
        Assert.Equal(9.5, Real("SELECT Stock FROM Ingredients WHERE Id=1"));
    }

    [Fact]
    public void StandaloneAddLoyaltyPoints_StillCommitsOnItsOwn()
    {
        _db.AddLoyaltyPoints(7, 10);

        Assert.Equal(30, Real("SELECT LoyaltyPoints FROM Customers WHERE Id=7"));
    }

    [Fact]
    public void StandaloneSaveOrder_RollsBackOnFailure()
    {
        // السلوك القديم: أي استثناء جوه الـ transaction بيرجّع كل
        // حاجة اتكتبت فيه. OrderItems بيفشل لو عمود ناقص.
        using (var c = Open())
            Exec(c, "ALTER TABLE OrderItems DROP COLUMN SizeExtraPrice;");

        var order = MakeOrder();
        Assert.Throws<SqliteException>(() => _db.SaveOrder(order));

        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM Orders"));
    }
}
