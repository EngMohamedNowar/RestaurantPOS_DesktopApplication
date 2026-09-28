using System.IO;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.Services;

namespace PizzaPOS.Tests;

/// <summary>
/// اختبارات المخزون + قيود الـ FK.
///
/// كل الاختبارات بتستخدم <see cref="DatabaseHelper.CSFor"/> — يعني نفس
/// connection string المستخدم في الإنتاج حرفياً. لو Foreign Keys=True
/// اتشال من الإنتاج، الاختبارات دي هتفشل (مش هتفتعلش نجاح زائف).
/// </summary>
public class InventoryTests : IDisposable
{
    readonly string _root;
    readonly string _dbPath;
    readonly string _cs;

    public InventoryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pizzapos-inv", Guid.NewGuid().ToString("N"));
        _dbPath = Path.Combine(_root, "pos.db");
        Directory.CreateDirectory(_root);
        _cs = DatabaseHelper.CSFor(_dbPath);
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
        Exec(c, "CREATE TABLE IngredientCategories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL);");
        Exec(c, @"CREATE TABLE Ingredients (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CategoryId INTEGER REFERENCES IngredientCategories(Id),
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

        // نفس DatabaseHelper.Initialize: بيزرع فئات، ومفيش مادة من غير فئة
        // دلوقتي (كل category_id لازم يشير لصف موجود)
        Exec(c, "INSERT INTO IngredientCategories(Name) VALUES('عجين'),('أجبان');");
    }

    SqliteConnection Open() { var c = new SqliteConnection(_cs); c.Open(); return c; }
    static void Exec(SqliteConnection c, string sql) { using var k = c.CreateCommand(); k.CommandText = sql; k.ExecuteNonQuery(); }

    int InsertCategory(string name)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "INSERT INTO IngredientCategories(Name) VALUES(@n); SELECT last_insert_rowid();";
        k.Parameters.AddWithValue("@n", name);
        return Convert.ToInt32(k.ExecuteScalar());
    }

    int InsertProduct(string name, double cost = 0)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "INSERT INTO Products(Name,Price,Cost) VALUES(@n,100,@co); SELECT last_insert_rowid();";
        k.Parameters.AddWithValue("@n", name); k.Parameters.AddWithValue("@co", cost);
        return Convert.ToInt32(k.ExecuteScalar());
    }

    int InsertIngredient(string name, double stock, string unit = "كجم", int catId = 1)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = @"INSERT INTO Ingredients(CategoryId,Name,Unit,Stock,MinStock,CostPerUnit,IsActive)
            VALUES(@c,@n,@u,@s,0,10,1); SELECT last_insert_rowid();";
        k.Parameters.AddWithValue("@c", catId); k.Parameters.AddWithValue("@n", name);
        k.Parameters.AddWithValue("@u", unit); k.Parameters.AddWithValue("@s", stock);
        return Convert.ToInt32(k.ExecuteScalar());
    }

    void LinkRecipe(int productId, int ingredientId, double qtyUsed)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "INSERT INTO ProductIngredients(ProductId,IngredientId,QtyUsed) VALUES(@p,@i,@q)";
        k.Parameters.AddWithValue("@p", productId); k.Parameters.AddWithValue("@i", ingredientId);
        k.Parameters.AddWithValue("@q", qtyUsed);
        k.ExecuteNonQuery();
    }

    double StockOf(int ingredientId)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "SELECT Stock FROM Ingredients WHERE Id=@i";
        k.Parameters.AddWithValue("@i", ingredientId);
        return Convert.ToDouble(k.ExecuteScalar());
    }

    (double Qty, string Note, string Type) LastMovement(int ingredientId)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "SELECT Qty,Note,Type FROM StockMovements WHERE IngredientId=@i ORDER BY Id DESC LIMIT 1";
        k.Parameters.AddWithValue("@i", ingredientId);
        using var r = k.ExecuteReader();
        Assert.True(r.Read(), "no movement recorded");
        return (r.GetDouble(0), r.IsDBNull(1) ? "" : r.GetString(1), r.GetString(2));
    }

    int Count(string sql)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = sql;
        return Convert.ToInt32(k.ExecuteScalar());
    }

    double ProductCost(int productId)
    {
        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "SELECT Cost FROM Products WHERE Id=@p";
        k.Parameters.AddWithValue("@p", productId);
        return Convert.ToDouble(k.ExecuteScalar());
    }

    static OrderItem Item(int productId, int qty) => new OrderItem { ProductId = productId, Qty = qty };

    // ══════════════════════════════════════════════════════════
    //  1) قيود الـ FK — أهم إصلاح: كانت متوقفة فعلياً
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void ForeignKeys_ProductionConnectionString_ActuallyEnforcesConstraints()
    {
        CreateSchema();

        var ex = Assert.Throws<SqliteException>(() =>
        {
            using var c = Open();
            using var k = c.CreateCommand();
            k.CommandText = "INSERT INTO StockMovements(IngredientId,Type,Qty) VALUES(9999,'in',1)";
            k.ExecuteNonQuery();
        });
        Assert.Equal(19, ex.SqliteErrorCode);   // SQLITE_CONSTRAINT_FOREIGNKEY
    }

    [Fact]
    public void ForeignKeys_PragmaAloneWouldNotHaveHelped_BecauseItIsPerConnection()
    {
        // هذا بالضبط الـ bug الأصلي: الـ PRAGMA بيتنفّذ على connection واحدة
        // بس. هنا بنفتح connection تانية من غير أي PRAGMA وبنتحقق إن القيود
        // لسه مفعّلة — وده اللي كان مستحيل قبل الإصلاح.
        CreateSchema();

        using (var setup = Open())
            Exec(setup, "PRAGMA foreign_keys=ON;");   // connection #1 بس

        using (var other = new SqliteConnection(_cs))   // connection #2 جديدة
        {
            other.Open();
            using var k = other.CreateCommand();
            k.CommandText = "PRAGMA foreign_keys";
            Assert.Equal(1L, Convert.ToInt64(k.ExecuteScalar()));

            k.CommandText = "INSERT INTO StockMovements(IngredientId,Type,Qty) VALUES(9999,'in',1)";
            Assert.Throws<SqliteException>(() => k.ExecuteNonQuery());
        }
    }

    [Fact]
    public void ForeignKeys_HardDeletingIngredientWithHistory_IsRejected()
    {
        // ده اللي كان بيسيب orphans في StockMovements بصمت
        CreateSchema();
        int ing = InsertIngredient("صلصة", 5);
        NewSvc().AddStock(ing, 10, "وارد", 1);

        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "DELETE FROM Ingredients WHERE Id=@i";
        k.Parameters.AddWithValue("@i", ing);
        var ex = Assert.Throws<SqliteException>(() => k.ExecuteNonQuery());
        Assert.Equal(19, ex.SqliteErrorCode);
        Assert.Equal(1, Count($"SELECT COUNT(*) FROM Ingredients WHERE Id={ing}"));
        Assert.Equal(15, StockOf(ing), 3);
    }

    // ══════════════════════════════════════════════════════════
    //  2) الخصم — إصلاح إخفاء العجز بـ MAX(0,...)
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void Deduct_EnoughStock_DeductsExactlyAndReportsNoShortage()
    {
        CreateSchema();
        int dough = InsertIngredient("دقيق", 10);
        int pizza = InsertProduct("مارغريتا");
        LinkRecipe(pizza, dough, 0.25);

        var r = NewSvc().DeductForOrder(new[] { Item(pizza, 4) }, userId: 1, "A-100");

        Assert.False(r.HasShortage);
        Assert.Equal(1, r.DeductedLines);
        Assert.Equal(9, StockOf(dough), 3);           // 10 - (0.25×4)
        var (qty, note, type) = LastMovement(dough);
        Assert.Equal(-1.0, qty, 3);
        Assert.Equal("out", type);
        Assert.Contains("A-100", note);
    }

    [Fact]
    public void Deduct_InsufficientStock_GoesNegativeAndReportsShortage_InsteadOfHidingIt()
    {
        // الكود القديم: Stock=MAX(0,Stock-@q) → الرصيد يقف عند صفر والعجز يضيع
        CreateSchema();
        int cheese = InsertIngredient("جبنة", 5);
        int pizza = InsertProduct("بيتزا جبنة");
        LinkRecipe(pizza, cheese, 2);

        var r = NewSvc().DeductForOrder(new[] { Item(pizza, 4) }, userId: 1);

        Assert.True(r.HasShortage);
        var s = Assert.Single(r.Shortages);
        Assert.Equal("جبنة", s.Ingredient);
        Assert.Equal(8, s.Required, 3);
        Assert.Equal(5, s.Available, 3);
        Assert.Equal(3, s.Missing, 3);

        // الرصيد السالب = الكود الصادق. صفر كان معناه "في مخزون" وده كذب
        Assert.Equal(-3, StockOf(cheese), 3);

        // والحركة بتسجّل الخصم الكامل زي ما اتسحب فعلاً
        Assert.Equal(-8, LastMovement(cheese).Qty, 3);
    }

    [Fact]
    public void Deduct_NoOrderRef_UsesGenericNote()
    {
        CreateSchema();
        int ing = InsertIngredient("ملح", 10);
        int p = InsertProduct("X");
        LinkRecipe(p, ing, 1);

        NewSvc().DeductForOrder(new[] { Item(p, 1) }, userId: 1);

        Assert.Equal("أوردر", LastMovement(ing).Note);
    }

    [Fact]
    public void Deduct_ProductWithoutRecipe_DeductsNothing()
    {
        // الحالة اللي بتقلب في أي تثبيت جديد: الـ Seed ما بيزرعش وصفة،
        // فكان الخصم ماشي في الكود بس مش بيخصمش حاجة في الواقع.
        CreateSchema();
        int ing = InsertIngredient("دقيق", 10);
        int p = InsertProduct("من غير وصفة");

        var r = NewSvc().DeductForOrder(new[] { Item(p, 3) }, userId: 1);

        Assert.Equal(0, r.DeductedLines);
        Assert.False(r.HasShortage);
        Assert.Equal(10, StockOf(ing));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM StockMovements"));
    }

    [Fact]
    public void Deduct_SoftDeletedIngredient_IsSkippedAndDoesNotCorruptStock()
    {
        CreateSchema();
        int ing = InsertIngredient("زبدة", 10);
        int p = InsertProduct("X");
        LinkRecipe(p, ing, 1);
        NewDb().DeleteIngredient(ing);   // soft delete

        var r = NewSvc().DeductForOrder(new[] { Item(p, 5) }, userId: 1);

        Assert.Equal(0, r.DeductedLines);
        Assert.Equal(10, StockOf(ing));   // ما اتغيرش — ما خصمناش من مادة محذوفة
    }

    [Fact]
    public void Deduct_TwoItemsSharingIngredient_MovesStockOncePerLine()
    {
        CreateSchema();
        int ing = InsertIngredient("صلصة", 10);
        int a = InsertProduct("A"); int b = InsertProduct("B");
        LinkRecipe(a, ing, 1); LinkRecipe(b, ing, 1);

        NewSvc().DeductForOrder(new[] { Item(a, 2), Item(b, 3) }, userId: 1);

        Assert.Equal(5, StockOf(ing), 3);
        Assert.Equal(2, Count($"SELECT COUNT(*) FROM StockMovements WHERE IngredientId={ing}"));
    }

    [Fact]
    public void Deduct_ShortageInOneIngredient_StillDeductsTheOthers()
    {
        CreateSchema();
        int rich = InsertIngredient("متوفر", 100);
        int poor = InsertIngredient("ناقص", 1);
        int p = InsertProduct("X");
        LinkRecipe(p, rich, 1);
        LinkRecipe(p, poor, 10);

        var r = NewSvc().DeductForOrder(new[] { Item(p, 1) }, userId: 1);

        Assert.Equal(2, r.DeductedLines);   // الاتنين اتخصموا
        Assert.Equal(99, StockOf(rich), 3);
        Assert.Equal(-9, StockOf(poor), 3);
        Assert.Equal("ناقص", Assert.Single(r.Shortages).Ingredient);
    }

    // ══════════════════════════════════════════════════════════
    //  3) الحذف — soft delete + تنظيف الوصفات
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void RecipeLink_ToMissingIngredient_IsRejectedByForeignKey()
    {
        // مع تفعيل الـ FK، وصفة تشير لمادة غير موجودة بترمي فوراً وقت الكتابة
        // بدل ما تتسجل وميبقى رابط يتيم ما حدش بيشوفه
        CreateSchema();
        int p = InsertProduct("X");

        using var c = Open();
        using var k = c.CreateCommand();
        k.CommandText = "INSERT INTO ProductIngredients(ProductId,IngredientId,QtyUsed) VALUES(@p,4242,1)";
        k.Parameters.AddWithValue("@p", p);
        var ex = Assert.Throws<SqliteException>(() => k.ExecuteNonQuery());
        Assert.Equal(19, ex.SqliteErrorCode);
        Assert.Equal(0, Count("SELECT COUNT(*) FROM ProductIngredients"));
    }

    [Fact]
    public void DeleteIngredient_SoftDeletes_KeepsHistory_UnlinksRecipes_AndRecalculatesCost()
    {
        CreateSchema();
        int ing = InsertIngredient("حليب", 10);        // CostPerUnit=10
        int p = InsertProduct("مشروب بالحليب", cost: 99);
        LinkRecipe(p, ing, 0.5);                      // التكلفة المتوقعة 0.5×10=5
        NewSvc().AddStock(ing, 20, "وارد", 1);

        Assert.Equal(1, NewDb().GetIngredientUsageCount(ing));

        NewDb().DeleteIngredient(ing);

        // الصف لسه موجود (soft delete) → تاريخ الحركة مقروء
        Assert.Equal(1, Count($"SELECT COUNT(*) FROM Ingredients WHERE Id={ing} AND IsActive=0"));
        Assert.Equal(1, Count($"SELECT COUNT(*) FROM StockMovements WHERE IngredientId={ing}"));

        // والرابط في الوصفة اتشال
        Assert.Equal(0, Count($"SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId={ing}"));
        Assert.Equal(0, NewDb().GetIngredientUsageCount(ing));

        // والتكلفة اتحسبت تاني (اتصفرت لأن الوصفة فاضلة)
        Assert.Equal(0, NewDb().CalculateProductCost(p), 3);
        Assert.Equal(0, ProductCost(p), 3);

        // والمادة المحذوفة مبيبانش في القوائم بس بتفضل في تقرير الحركة
        Assert.DoesNotContain(NewSvc().GetAll(), i => i.Id == ing);
        Assert.Contains(NewSvc().GetMovements(3650), m => m.Ingredient == "حليب");
    }

    [Fact]
    public void DeleteIngredient_KeepsCostOfOtherIngredientsInRecipe()
    {
        CreateSchema();
        int dough = InsertIngredient("دقيق", 50);
        int cheese = InsertIngredient("جبنة", 50);
        int p = InsertProduct("X");
        LinkRecipe(p, dough, 1);   // 10
        LinkRecipe(p, cheese, 2);  // 20  → 30
        Assert.Equal(30, NewDb().CalculateProductCost(p), 3);

        NewDb().DeleteIngredient(cheese);

        Assert.Equal(10, NewDb().CalculateProductCost(p), 3);
        Assert.Equal(10, ProductCost(p), 3);
    }

    [Fact]
    public void DeleteIngredientCategory_SoftDeletesAll_UnlinksEveryRecipe_AndRemovesCategory()
    {
        CreateSchema();
        int cat = InsertCategory("أجبان");
        int c1 = InsertIngredient("موزاريلا", 10, catId: cat);
        int c2 = InsertIngredient("شيدر", 10, catId: cat);
        int keep = InsertIngredient("صلصة", 10, catId: 1);
        int p1 = InsertProduct("A"); int p2 = InsertProduct("B");
        LinkRecipe(p1, c1, 1); LinkRecipe(p2, c2, 1); LinkRecipe(p1, keep, 1);

        int affected = NewDb().DeleteIngredientCategory(cat);

        Assert.Equal(2, affected);
        Assert.Equal(0, Count($"SELECT COUNT(*) FROM Ingredients WHERE CategoryId={cat} AND IsActive=1"));
        // المواد المحذوفة فكّت مرجعها من الفئة (CategoryId=NULL) — لو سبنا
        // الرقم، الـ FK بيرفض حذف الفئة خالص، وده اللي حصل قبل الإصلاح
        Assert.Equal(2, Count("SELECT COUNT(*) FROM Ingredients WHERE CategoryId IS NULL AND IsActive=0"));
        Assert.Equal(0, Count($"SELECT COUNT(*) FROM IngredientCategories WHERE Id={cat}"));

        // روابط الفئة اتفكت، بس ربط المادة اللي بره الفئة اتساب
        Assert.Equal(0, Count($"SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId IN ({c1},{c2})"));
        Assert.Equal(1, Count($"SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId={keep}"));

        // وتكلفة المنتج اتحسبت تاني على الوصفة اللي فاضلة
        Assert.Equal(10, NewDb().CalculateProductCost(p1), 3);
    }

    [Fact]
    public void DeleteIngredientCategory_WithNoIngredients_JustRemovesCategory()
    {
        CreateSchema();
        int cat = InsertCategory("فاضية");
        Assert.Equal(0, NewDb().DeleteIngredientCategory(cat));
        Assert.Equal(0, Count($"SELECT COUNT(*) FROM IngredientCategories WHERE Id={cat}"));
    }

    [Fact]
    public void DeleteIngredientCategory_ThenDeleteSameIngredient_IsANoOp()
    {
        CreateSchema();
        int cat = InsertCategory("أجبان");
        int c1 = InsertIngredient("موزاريلا", 10, catId: cat);
        int p = InsertProduct("A");
        LinkRecipe(p, c1, 1);

        NewDb().DeleteIngredientCategory(cat);
        NewDb().DeleteIngredient(c1);   // المفروض ما يعملش error

        Assert.Equal(0, Count($"SELECT COUNT(*) FROM ProductIngredients WHERE IngredientId={c1}"));
        Assert.Equal(0, NewDb().GetIngredientUsageCount(c1));
    }

    // ══════════════════════════════════════════════════════════
    //  4) باقي الخدمة
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void Save_Edit_DoesNotOverwriteStock()
    {
        // التعديل لازم يعدّل الاسم/الحد الأدنى بس — الكمية بتتغير من
        // AddStock/AdjustStock عشان كل تغيير يبقى له سطر في StockMovements
        CreateSchema();
        int ing = InsertIngredient("قديم", 40);
        NewSvc().AddStock(ing, 5, "وارد", 1);
        Assert.Equal(45, StockOf(ing), 3);

        var ing2 = NewSvc().GetAll().First(x => x.Id == ing);
        ing2.Name = "سميت";
        ing2.MinStock = 7;
        ing2.Stock = 0;              // الـ UI بيبعت 0 مع كل تعديل
        NewSvc().Save(ing2);

        Assert.Equal(45, StockOf(ing), 3);
        var reloaded = NewSvc().GetAll().First(x => x.Id == ing);
        Assert.Equal("سميت", reloaded.Name);
        Assert.Equal(7, reloaded.MinStock, 3);
    }

    [Fact]
    public void Save_Edit_RestoresSoftDeletedIngredient()
    {
        CreateSchema();
        int ing = InsertIngredient("ارجعت", 10);
        NewDb().DeleteIngredient(ing);
        Assert.DoesNotContain(NewSvc().GetAll(), i => i.Id == ing);

        NewSvc().Save(new Ingredient
        {
            Id = ing, CategoryId = 1, Name = "ارجعت",
            Unit = "كجم", Stock = 10, MinStock = 2
        });

        Assert.Contains(NewSvc().GetAll(), i => i.Id == ing);
    }

    [Fact]
    public void GetAll_AndGetLowStock_ExcludeSoftDeleted()
    {
        CreateSchema();
        int high = InsertIngredient("موجود", 1);      // Stock=1 > MinStock=0
        int low = InsertIngredient("منخفض", 0);
        int gone = InsertIngredient("محذوف", 0);
        NewDb().DeleteIngredient(gone);

        var all = NewSvc().GetAll();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, i => i.Id == high);
        Assert.DoesNotContain(all, i => i.Id == gone);

        var lowList = NewSvc().GetLowStock();
        Assert.Contains(lowList, i => i.Id == low);
        Assert.DoesNotContain(lowList, i => i.Id == gone);
    }

    [Fact]
    public void GetAll_Search_DoesNotMatchDeleted()
    {
        CreateSchema();
        int ing = InsertIngredient("جبنة موزاريلا", 5);
        NewDb().DeleteIngredient(ing);
        Assert.Empty(NewSvc().GetAll("موزاريلا"));
    }

    [Fact]
    public void AdjustStock_RecordsSignedDifference()
    {
        CreateSchema();
        int ing = InsertIngredient("سكر", 10);
        NewSvc().AdjustStock(ing, 4, "جرد", 1);
        Assert.Equal(4, StockOf(ing), 3);
        var (qty, note, type) = LastMovement(ing);
        Assert.Equal(-6, qty, 3);
        Assert.Equal("adjust", type);
        Assert.Equal("جرد", note);

        NewSvc().AdjustStock(ing, 20, "جرد", 1);
        Assert.Equal(20, StockOf(ing), 3);
        Assert.Equal(16, LastMovement(ing).Qty, 3);
    }

    [Fact]
    public void AdjustStock_UnknownOrDeletedIngredient_Throws()
    {
        CreateSchema();
        Assert.Throws<InvalidOperationException>(() => NewSvc().AdjustStock(999, 5, "x", 1));

        int ing = InsertIngredient("حذف", 5);
        NewDb().DeleteIngredient(ing);
        Assert.Throws<InvalidOperationException>(() => NewSvc().AdjustStock(ing, 5, "x", 1));
    }

    [Fact]
    public void AddStock_IncreasesAndRecordsMovement()
    {
        CreateSchema();
        int ing = InsertIngredient("طماطم", 0);
        NewSvc().AddStock(ing, 12.5, "شراء", 3);
        Assert.Equal(12.5, StockOf(ing), 3);
        var (qty, _, type) = LastMovement(ing);
        Assert.Equal(12.5, qty, 3);
        Assert.Equal("in", type);
    }

    [Fact]
    public void GetMovements_ShowsBothIncomingAndOutgoing()
    {
        CreateSchema();
        int ing = InsertIngredient("صلصة", 0);
        int p = InsertProduct("X");
        LinkRecipe(p, ing, 2);
        NewSvc().AddStock(ing, 20, "شراء", 1);
        NewSvc().DeductForOrder(new[] { Item(p, 3) }, userId: 1, "A-7");

        var moves = NewSvc().GetMovements(3650);
        Assert.Equal(2, moves.Count);
        Assert.Contains(moves, m => m.Type == "in" && Math.Abs(m.Qty - 20) < 0.001);
        Assert.Contains(moves, m => m.Type == "out" && Math.Abs(m.Qty + 6) < 0.001);
        Assert.Contains(moves, m => m.Note != null && m.Note.Contains("A-7"));
    }

    // ── helpers ────────────────────────────────────

    InventoryService NewSvc() => new(_cs);
    AppDbContext NewDb() => new(_cs);
}
