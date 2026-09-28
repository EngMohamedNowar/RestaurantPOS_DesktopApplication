using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.Services;

namespace PizzaPOS.Tests;

/// <summary>
/// اختبارات البند 1: منع PIN الافتراضي + إزالة تخزين SHA256 بدون salt.
/// كل حاجة هنا على DB مؤقت في مسار صريح عشان ما تلمسش البيانات الحقيقية.
/// </summary>
public class CredentialSecurityTests : IDisposable
{
    readonly string _root;
    readonly string _dbPath;
    readonly string _cs;

    public CredentialSecurityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pizzapos-tests", Guid.NewGuid().ToString("N"));
        _dbPath = Path.Combine(_root, "pos.db");
        _cs = $"Data Source={_dbPath}";
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
        catch { }
    }

    UserService NewSvc() => new(_cs);
    AppDbContext NewDb() => new(_cs);

    SqliteConnection Open()
    {
        var c = new SqliteConnection(_cs);
        c.Open();
        return c;
    }

    static void Exec(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    // نفس DDL بتاع DatabaseHelper.Initialize لجدول Users
    void CreateUsersTable(bool includeMustChangePin)
    {
        using var c = Open();
        Exec(c, @"CREATE TABLE Users (
            Id       INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT    NOT NULL UNIQUE,
            FullName TEXT    NOT NULL,
            PinHash  TEXT    NOT NULL,
            Role     TEXT    DEFAULT 'cashier',
            IsActive INTEGER DEFAULT 1"
            + (includeMustChangePin ? ",\n            MustChangePin INTEGER DEFAULT 0" : "") + ");");
    }

    void InsertUser(string username, string pin, string role = "admin", int mustChange = 1, string? rawHash = null)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO Users(Username,FullName,PinHash,Role,MustChangePin) VALUES(@u,'T',@p,@r,@m)";
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", rawHash ?? UserService.HashPin(pin));
        cmd.Parameters.AddWithValue("@r", role);
        cmd.Parameters.AddWithValue("@m", mustChange);
        cmd.ExecuteNonQuery();
    }

    void InsertRawSha256(string username, string rawHash)
        => InsertUser(username, "", rawHash: rawHash);

    string ReadHash(string username)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT PinHash FROM Users WHERE Username=@u";
        cmd.Parameters.AddWithValue("@u", username);
        return cmd.ExecuteScalar() as string ?? "";
    }

    /// <summary>
    /// PBKDF2 بيتخزّن كـ Base64 لـ 48 بايت (16 salt + 32 hash) = 64 محرف.
    /// نفس طول SHA256 بالظبط — فالشكل المميز إن كل محارفه hex صغيرة.
    /// </summary>
    static void AssertIsPbkdf2(string hash)
    {
        Assert.Equal(64, hash.Length);
        Assert.False(hash.All(c => "0123456789abcdef".Contains(c)),
            "hash looks like the legacy unsalted SHA256");
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(48, bytes.Length); // 16 salt + 32 hash
    }

    static bool IsLegacySha256(string hash)
        => hash.Length == 64 && hash.All(c => "0123456789abcdef".Contains(c));

    int GetId(string username)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Users WHERE Username=@u";
        cmd.Parameters.AddWithValue("@u", username);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    int GetMustChangeFlag(string username)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT MustChangePin FROM Users WHERE Username=@u";
        cmd.Parameters.AddWithValue("@u", username);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // SHA256("1234") — الشكل القديم المخزّن في الكود
    const string LegacyAdminHash = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4";

    // ══════════════════════════════════════════════════════════════════
    //  ValidateNewPin
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("abcd")]
    [InlineData("12a4")]
    [InlineData("12 4")]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateNewPin_RejectsBadInput(string pin)
    {
        Assert.NotNull(UserService.ValidateNewPin(pin));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("0000")]
    public void ValidateNewPin_RejectsWellKnownPins(string pin)
    {
        Assert.Contains("شائع", UserService.ValidateNewPin(pin));
    }

    [Theory]
    [InlineData("4821")]
    [InlineData("0001")]
    [InlineData("9990")]
    [InlineData("5077")]
    public void ValidateNewPin_AcceptsReasonablePin(string pin)
    {
        Assert.Null(UserService.ValidateNewPin(pin));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Migration: كشف الـ hashes القديمة
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void LegacyDetection_Sha256HashIs64HexChars_SoTheMigrationFires()
    {
        Assert.Equal(64, LegacyAdminHash.Length);
        Assert.All(LegacyAdminHash, c => Assert.Contains(c, "0123456789abcdef"));
    }

    [Fact]
    public void LegacyDetection_Pbkdf2HashIs64Chars_ButNotHex_SoMigrationSkipsIt()
    {
        // 48 بايت (16 salt + 32 hash) → Base64 = 64 محرف بالظبط، نفس طول SHA256.
        // لو الـ migration اشترط الطول بس، كان هيجبر كل المستخدمين على التغيير للأبد.
        string pbkdf2 = UserService.HashPin("4821");
        Assert.Equal(64, pbkdf2.Length);

        // ...بس مش hex خالص، فالـ migration اللي بيشترط hex بيشيله.
        Assert.False(pbkdf2.All(c => "0123456789abcdef".Contains(c)));
    }


    [Fact]
    public void LegacyDetection_MigrationFlagsSha256RowsOnly()
    {
        CreateUsersTable(includeMustChangePin: false);
        using (var c = Open())
        {
            Exec(c, "ALTER TABLE Users ADD COLUMN MustChangePin INTEGER DEFAULT 0");
            using var a = c.CreateCommand();
            a.CommandText = @"INSERT INTO Users(Username,FullName,PinHash,Role,MustChangePin) VALUES
                ('legacy','Legacy',@h,'admin',0),('modern','Modern',@p,'cashier',0)";
            a.Parameters.AddWithValue("@h", LegacyAdminHash);
            a.Parameters.AddWithValue("@p", UserService.HashPin("4821"));
            a.ExecuteNonQuery();
        }

        using (var c = Open())
        {
            // نفس الـ SQL اللي في DatabaseHelper.TryMigrate
            Exec(c, "UPDATE Users SET MustChangePin=1 WHERE PinHash IS NOT NULL "
                  + "AND length(PinHash)=64 AND PinHash NOT GLOB '*[^0-9a-f]*'");
        }

        using (var c = Open())
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Username,MustChangePin FROM Users ORDER BY Username";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (r.GetString(0) == "legacy") Assert.Equal(1, r.GetInt32(1));
                else Assert.Equal(0, r.GetInt32(1));
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Login
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Login_FlaggedUser_ReturnsMustChangePinTrue()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234");

        var user = NewSvc().Login("admin", "1234");

        Assert.NotNull(user);
        Assert.True(user!.MustChangePin);
    }

    [Fact]
    public void Login_NormalUser_ReturnsMustChangePinFalse()
    {
        CreateUsersTable(true);
        InsertUser("cashier", "4821", role: "cashier", mustChange: 0);

        var user = NewSvc().Login("cashier", "4821");

        Assert.NotNull(user);
        Assert.False(user!.MustChangePin);
    }

    [Fact]
    public void Login_LegacySha256Account_StillLogsInAndGetsFlagged()
    {
        CreateUsersTable(true);
        InsertUser("admin", "", rawHash: LegacyAdminHash);

        var user = NewSvc().Login("admin", "1234");

        Assert.NotNull(user);
        Assert.True(user!.MustChangePin);
    }

    // ══════════════════════════════════════════════════════════════════
    //  TryChangeOwnPin
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TryChangeOwnPin_ValidChange_SucceedsAndClearsFlag()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234");

        string? error = NewSvc().TryChangeOwnPin(GetId("admin"), "1234", "4821");

        Assert.Null(error);
        Assert.Equal(0, GetMustChangeFlag("admin"));
        Assert.True(UserService.VerifyPin("4821", ReadHash("admin")));
        Assert.False(UserService.VerifyPin("1234", ReadHash("admin")));
    }

    [Fact]
    public void TryChangeOwnPin_WrongCurrentPin_RejectedAndNothingChanges()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234");

        string? error = NewSvc().TryChangeOwnPin(GetId("admin"), "9999", "4821");

        Assert.NotNull(error);
        Assert.Contains("غير صحيح", error);
        Assert.Equal(1, GetMustChangeFlag("admin"));
        Assert.True(UserService.VerifyPin("1234", ReadHash("admin")));
    }

    [Fact]
    public void TryChangeOwnPin_SameAsCurrent_Rejected()
    {
        CreateUsersTable(true);
        // PIN حالي مش "شائع" عشان نضمن إن الرفض سببه التطابق مش قائمة الشائعة
        InsertUser("admin", "4821");

        string? error = NewSvc().TryChangeOwnPin(GetId("admin"), "4821", "4821");

        Assert.NotNull(error);
        Assert.Contains("مختلف", error);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcd")]
    [InlineData("0000")]
    public void TryChangeOwnPin_WeakOrInvalidNewPin_Rejected(string newPin)
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234");

        Assert.NotNull(NewSvc().TryChangeOwnPin(GetId("admin"), "1234", newPin));
        Assert.Equal(1, GetMustChangeFlag("admin"));
    }

    [Fact]
    public void TryChangeOwnPin_UnknownUser_ReturnsError()
    {
        CreateUsersTable(true);
        Assert.NotNull(NewSvc().TryChangeOwnPin(999, "1234", "4821"));
    }

    [Fact]
    public void TryChangeOwnPin_RewritesLegacySha256ToPbkdf2()
    {
        CreateUsersTable(true);
        InsertRawSha256("admin", LegacyAdminHash);
        Assert.True(IsLegacySha256(ReadHash("admin")));

        Assert.Null(NewSvc().TryChangeOwnPin(GetId("admin"), "1234", "4821"));

        string hash = ReadHash("admin");
        Assert.False(IsLegacySha256(hash));
        AssertIsPbkdf2(hash);
        Assert.True(UserService.VerifyPin("4821", hash));
    }

    // ══════════════════════════════════════════════════════════════════
    //  SaveUser — مفيش "0000" صامت، والتخزين PBKDF2 دايماً
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void SaveUser_NewUserWithoutPin_ThrowsInsteadOfSilentlyUsing0000()
    {
        CreateUsersTable(true);

        Assert.Throws<ArgumentException>(() =>
            NewDb().SaveUser(new User { Username = "x", FullName = "X", Role = "cashier" }, null));
    }

    [Fact]
    public void SaveUser_NewUserWithPin_StoresPbkdf2AndClearsFlag()
    {
        CreateUsersTable(true);

        NewDb().SaveUser(new User { Username = "new", FullName = "New", Role = "cashier" }, "4821");

        var user = NewSvc().Login("new", "4821");
        Assert.NotNull(user);
        Assert.False(user!.MustChangePin);
        AssertIsPbkdf2(ReadHash("new"));
    }

    [Fact]
    public void SaveUser_ChangePin_RehashesWithPbkdf2()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234");
        Assert.True(IsLegacySha256(LegacyAdminHash));
        InsertRawSha256("legacy", LegacyAdminHash);

        NewDb().SaveUser(
            new User { Id = GetId("admin"), Username = "admin", FullName = "T", Role = "admin" }, "4821");

        Assert.True(UserService.VerifyPin("4821", ReadHash("admin")));
        AssertIsPbkdf2(ReadHash("admin"));
    }

    [Fact]
    public void SaveUser_EditWithoutPin_KeepsExistingHash()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234");
        string before = ReadHash("admin");

        NewDb().SaveUser(
            new User { Id = GetId("admin"), Username = "admin", FullName = "Renamed", Role = "admin" }, null);

        Assert.Equal(before, ReadHash("admin"));
        Assert.NotNull(NewSvc().Login("admin", "1234"));
    }

    [Fact]
    public void SaveUser_ChangePinOnFlaggedUser_ClearsFlag()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234", mustChange: 1);

        NewDb().SaveUser(
            new User { Id = GetId("admin"), Username = "admin", FullName = "T", Role = "admin" }, "4821");

        Assert.Equal(0, GetMustChangeFlag("admin"));
    }

    // ══════════════════════════════════════════════════════════════════
    //  GetUsers / GetAll surface the flag
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void GetUsers_ReturnsMustChangePinPerRow()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234", mustChange: 1);
        InsertUser("cashier", "4821", role: "cashier", mustChange: 0);

        var users = NewDb().GetUsers();

        Assert.Equal(2, users.Count);
        Assert.True(users.Single(u => u.Username == "admin").MustChangePin);
        Assert.False(users.Single(u => u.Username == "cashier").MustChangePin);
    }

    [Fact]
    public void GetAll_ReturnsMustChangePinPerRow()
    {
        CreateUsersTable(true);
        InsertUser("admin", "1234", mustChange: 1);
        InsertUser("cashier", "4821", role: "cashier", mustChange: 0);

        var users = NewSvc().GetAll();

        Assert.Equal(2, users.Count);
        Assert.True(users.Single(u => u.Username == "admin").MustChangePin);
        Assert.False(users.Single(u => u.Username == "cashier").MustChangePin);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Edge case: ترتيب الـ migrations في DatabaseHelper.Initialize
    //
    //  التسلسل there: CREATE TABLE Users → TryMigrate(ALTER MustChangePin)
    //  → TryMigrate(UPDATE flag legacy) → Seed → EnsureDefaultUsers.
    //
    //  مافيش INSERT على Users قبل الـ ALTER، فمافيش transaction مفتوحة
    //  تقدر تمنع الـ ALTER (SQLite بيرفض ALTER جوه transaction). لو في يوم
    //  من الأيام حد زرع المستخدم قبل الـ ALTER، الـ UPDATE بيرول برك
    //  والعمود بيبقى موجود بس من غير أي صف مُعلَّم — فمفيش حماية.
    //
    //  الاختبارات دي بتثبّت السلوك عشان لو الترتيب اتغيّر هنعرف.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void MigrationOrdering_AlterRightAfterCreateTable_Succeeds()
    {
        using var c = Open();

        Exec(c, @"CREATE TABLE Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE, FullName TEXT NOT NULL,
            PinHash TEXT NOT NULL, Role TEXT DEFAULT 'cashier', IsActive INTEGER DEFAULT 1)");

        // لازم ينجح — مافيش transaction مفتوحة من الـ CREATE
        Exec(c, "ALTER TABLE Users ADD COLUMN MustChangePin INTEGER DEFAULT 0");

        using var info = c.CreateCommand();
        info.CommandText = "PRAGMA table_info(Users)";
        using var r = info.ExecuteReader();
        var columns = new List<string>();
        while (r.Read()) columns.Add(r.GetString(1));
        Assert.Contains("MustChangePin", columns);
    }

    [Fact]
    public void MigrationOrdering_RepeatedAlter_IsSwallowedAsAlreadyApplied()
    {
        CreateUsersTable(includeMustChangePin: true);

        using var c = Open();
        var ex = Assert.Throws<SqliteException>(() =>
            Exec(c, "ALTER TABLE Users ADD COLUMN MustChangePin INTEGER DEFAULT 0"));

        // الكود القديم كان بيشيك على ex.ErrorCode == 447 وده مش هيتحقق أبداً:
        // 447 مش كود SQLite أصلاً، وduplicate column بيرمي SQLITE_ERROR (1)
        // مع رسالة نصية. فالتشخيص لازم يعتمد على Message مش على رقم.
        Assert.Equal(1, ex.SqliteErrorCode);
        Assert.Contains("duplicate column", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(DatabaseHelper.IsAlreadyApplied(ex),
            "duplicate column must be treated as 'already migrated', not a failure");
    }

    [Fact]
    public void MigrationOrdering_RealSqlError_IsNotMistakenForAlreadyApplied()
    {
        using var c = Open();

        // نفس الـ result code (SQLITE_ERROR = 1) بس رسالة مختلفة → لازم تتعمل
        // log كـ WARN زي أي migration فشلت، ما تتبلعش
        var ex = Assert.Throws<SqliteException>(() =>
            Exec(c, "ALTER TABLE TableThatDoesNotExist ADD COLUMN X INTEGER DEFAULT 0"));

        Assert.Equal(1, ex.SqliteErrorCode);
        Assert.DoesNotContain("duplicate column", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(DatabaseHelper.IsAlreadyApplied(ex));
    }

    [Fact]
    public void MigrationOrdering_UpdateAfterAlterActuallyFlagsLegacyRows()
    {
        // المسار الكامل: table قديم → ALTER → UPDATE
        using (var c = Open())
        {
            Exec(c, @"CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE, FullName TEXT NOT NULL,
                PinHash TEXT NOT NULL, Role TEXT DEFAULT 'cashier', IsActive INTEGER DEFAULT 1)");
            using var ins = c.CreateCommand();
            ins.CommandText = "INSERT INTO Users(Username,FullName,PinHash,Role) VALUES('admin','Admin',@h,'admin')";
            ins.Parameters.AddWithValue("@h", LegacyAdminHash);
            ins.ExecuteNonQuery();

            Exec(c, "ALTER TABLE Users ADD COLUMN MustChangePin INTEGER DEFAULT 0");

            using var upd = c.CreateCommand();
            upd.CommandText = "UPDATE Users SET MustChangePin=1 WHERE PinHash IS NOT NULL "
                            + "AND length(PinHash)=64 AND PinHash NOT GLOB '*[^0-9a-f]*'";
            Assert.Equal(1, upd.ExecuteNonQuery());
        }

        using (var c = Open())
        using (var sel = c.CreateCommand())
        {
            sel.CommandText = "SELECT MustChangePin FROM Users WHERE Username='admin'";
            Assert.Equal(1, Convert.ToInt32(sel.ExecuteScalar()));
        }
    }
}
