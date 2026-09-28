using System.IO;
using Microsoft.Data.Sqlite;
using PizzaPOS.Services;

namespace PizzaPOS.Tests;

/// <summary>
/// اختبارات النسخ الاحتياطي. بتستخدم ملفات مؤقتة في مسار صريح عشان ما تلمسش
/// الـ DB الحقيقي في %AppData%.
/// </summary>
public class BackupServiceTests : IDisposable
{
    readonly string _root;
    readonly string _dbPath;
    readonly string _backupDir;

    public BackupServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pizzapos-tests", Guid.NewGuid().ToString("N"));
        _dbPath = Path.Combine(_root, "pos.db");
        _backupDir = Path.Combine(_root, "backups");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
        catch { /* تنظيف اختباري — التجاهل مقبول */ }
    }

    // ── helpers ────────────────────────────────────

    void CreateLiveDb(bool wal = true)
    {
        using var c = new SqliteConnection($"Data Source={_dbPath}");
        c.Open();
        if (wal) Execute(c, "PRAGMA journal_mode=WAL;");
        Execute(c, "CREATE TABLE Orders(Id INTEGER PRIMARY KEY, Total REAL, Note TEXT);");
        InsertOrder(c, 1, 100, "first");
    }

    static void Execute(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    static void InsertOrder(SqliteConnection c, int id, double total, string note)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO Orders(Id,Total,Note) VALUES(@i,@t,@n)";
        cmd.Parameters.AddWithValue("@i", id);
        cmd.Parameters.AddWithValue("@t", total);
        cmd.Parameters.AddWithValue("@n", note);
        cmd.ExecuteNonQuery();
    }

    int CountOrders(string path)
    {
        using var c = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Orders";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    string ReadNote(string path, int id)
    {
        using var c = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Note FROM Orders WHERE Id=@i";
        cmd.Parameters.AddWithValue("@i", id);
        return cmd.ExecuteScalar() as string ?? "";
    }

    // ── Create ─────────────────────────────────────

    [Fact]
    public void CreateBackupTo_DbDoesNotExist_ReturnsNullAndDoesNotThrow()
    {
        Assert.Null(BackupService.CreateBackupTo(_dbPath, _backupDir));
        Assert.False(Directory.Exists(_backupDir));
    }

    [Fact]
    public void CreateBackupTo_CreatesSingleFileBackup()
    {
        CreateLiveDb();
        string? path = BackupService.CreateBackupTo(_dbPath, _backupDir);

        Assert.NotNull(path);
        Assert.True(File.Exists(path));
        Assert.StartsWith("pos_", Path.GetFileName(path));
        Assert.Equal(1, CountOrders(path!));
    }

    /// <summary>
    /// هذا هو الـ bug الأصلي: File.Copy على pos.db مع WAL مفعّل بيفقد
    /// المعاملات اللي لسه في ملف -wal. VACUUM INTO لازم يحفظها.
    /// </summary>
    [Fact]
    public void CreateBackupTo_WalMode_IncludesUncheckpointedRows()
    {
        CreateLiveDb(wal: true);

        // نكتب صفوفاً ونتركها في الـ WAL بدون أي checkpoint
        using (var c = new SqliteConnection($"Data Source={_dbPath}"))
        {
            c.Open();
            InsertOrder(c, 2, 200, "in-wal");
            InsertOrder(c, 3, 300, "also-in-wal");
        }

        // اتأكد إن صف واحد على الأقل لسه في الـ WAL
        Assert.True(File.Exists(_dbPath + "-wal"), "expected a -wal sidecar file to exist");

        string? path = BackupService.CreateBackupTo(_dbPath, _backupDir);

        Assert.NotNull(path);
        Assert.Equal(3, CountOrders(path!));
        Assert.Equal("in-wal", ReadNote(path!, 2));
        Assert.Equal("also-in-wal", ReadNote(path!, 3));
    }

    [Fact]
    public void CreateBackupTo_TwiceInSameSecond_ProducesDistinctFiles()
    {
        CreateLiveDb();
        string? first = BackupService.CreateBackupTo(_dbPath, _backupDir);
        string? second = BackupService.CreateBackupTo(_dbPath, _backupDir);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CreateBackupTo_KeepsAtMostTenBackups()
    {
        CreateLiveDb();
        for (int i = 0; i < 14; i++) BackupService.CreateBackupTo(_dbPath, _backupDir);

        var files = Directory.GetFiles(_backupDir, "pos_*.db");
        Assert.Equal(BackupService.MaxBackups, files.Length);
    }

    // ── Validate ───────────────────────────────────

    [Fact]
    public void ValidateSqliteFile_ValidDatabase_ReturnsTrue()
    {
        CreateLiveDb();
        Assert.True(BackupService.ValidateSqliteFile(_dbPath, out string? problem));
        Assert.Null(problem);
    }

    [Fact]
    public void ValidateSqliteFile_TextFile_ReturnsFalse()
    {
        string junk = Path.Combine(_root, "junk.db");
        File.WriteAllText(junk, "this is definitely not a sqlite database");

        Assert.False(BackupService.ValidateSqliteFile(junk, out string? problem));
        Assert.NotNull(problem);
    }

    [Fact]
    public void ValidateSqliteFile_EmptyFile_ReturnsFalse()
    {
        string empty = Path.Combine(_root, "empty.db");
        File.WriteAllBytes(empty, Array.Empty<byte>());

        Assert.False(BackupService.ValidateSqliteFile(empty, out string? problem));
        Assert.Equal("الملف فاضي", problem);
    }

    [Fact]
    public void ValidateSqliteFile_MissingFile_ReturnsFalse()
    {
        Assert.False(BackupService.ValidateSqliteFile(Path.Combine(_root, "nope.db"), out _));
    }

    [Fact]
    public void ValidateSqliteFile_TruncatedDatabase_ReturnsFalse()
    {
        CreateLiveDb();
        string? backup = BackupService.CreateBackupTo(_dbPath, _backupDir);
        Assert.NotNull(backup);

        // نقطع نص الملف — ده اللي بيحصل لما الجهاز ي.power off وقت الكتابة
        var bytes = File.ReadAllBytes(backup!);
        File.WriteAllBytes(backup!, bytes[..(bytes.Length / 2)]);

        Assert.False(BackupService.ValidateSqliteFile(backup!, out string? problem));
        Assert.NotNull(problem);
    }

    // ── Restore ────────────────────────────────────

    [Fact]
    public void RestoreFrom_BackupWithOneRow_LiveWithTwoRows_RollsLiveBack()
    {
        CreateLiveDb();
        string? backup = BackupService.CreateBackupTo(_dbPath, _backupDir);
        Assert.NotNull(backup);

        using (var c = new SqliteConnection($"Data Source={_dbPath}"))
        {
            c.Open();
            InsertOrder(c, 2, 200, "added-after-backup");
        }
        Assert.Equal(2, CountOrders(_dbPath));

        var result = BackupService.RestoreFrom(backup!, _dbPath);

        Assert.True(result.Success, result.Message);
        // نقرّر الـ pool عشان مفيش connection قديم ماسك الملف القديم
        SqliteConnection.ClearAllPools();
        Assert.Equal(1, CountOrders(_dbPath));
    }

    /// <summary>
    /// بدون حذف -wal و-shm الـ WAL القديم هيعيد كتابة صفحاته فوق الـ DB
    /// المستعادة والبيانات هتبقى مكسورة. ده أهم assertion في الملف ده.
    /// </summary>
    [Fact]
    public void RestoreFrom_DeletesStaleWalAndShmSidecars()
    {
        CreateLiveDb();
        string? backup = BackupService.CreateBackupTo(_dbPath, _backupDir);
        Assert.NotNull(backup);

        using (var c = new SqliteConnection($"Data Source={_dbPath}"))
        {
            c.Open();
            InsertOrder(c, 2, 200, "post-backup");
        }

        Assert.True(File.Exists(_dbPath + "-wal"));
        Assert.True(File.Exists(_dbPath + "-shm"));

        var result = BackupService.RestoreFrom(backup!, _dbPath);

        Assert.True(result.Success, result.Message);
        Assert.False(File.Exists(_dbPath + "-wal"), "stale -wal must be removed or it overwrites the restored DB");
        Assert.False(File.Exists(_dbPath + "-shm"), "stale -shm must be removed");
        SqliteConnection.ClearAllPools();
        Assert.Equal(1, CountOrders(_dbPath));
    }

    [Fact]
    public void RestoreFrom_SavesSafetyCopyOfLiveDb()
    {
        CreateLiveDb();
        string? backup = BackupService.CreateBackupTo(_dbPath, _backupDir);
        Assert.NotNull(backup);

        using (var c = new SqliteConnection($"Data Source={_dbPath}"))
        {
            c.Open();
            InsertOrder(c, 2, 200, "will-be-lost");
        }

        Assert.True(BackupService.RestoreFrom(backup!, _dbPath).Success);

        var safety = Directory.GetFiles(_root, "pre-restore_*.db");
        Assert.Single(safety);
        // نسخة الأمان لازم تحمل الحالة القديمة (صفين)
        Assert.Equal(2, CountOrders(safety[0]));
    }

    [Fact]
    public void RestoreFrom_CorruptBackup_FailsAndLeavesLiveDbIntact()
    {
        CreateLiveDb();
        using (var c = new SqliteConnection($"Data Source={_dbPath}"))
        {
            c.Open();
            InsertOrder(c, 2, 200, "precious");
        }

        string? backup = BackupService.CreateBackupTo(_dbPath, _backupDir);
        Assert.NotNull(backup);
        var bytes = File.ReadAllBytes(backup!);
        File.WriteAllBytes(backup!, bytes[..(bytes.Length / 2)]);

        var result = BackupService.RestoreFrom(backup!, _dbPath);

        Assert.False(result.Success);
        Assert.Contains("مش قاعدة بيانات SQLite سليمة", result.Message);
        // الـ DB الحي ما اتمسّش
        Assert.Equal(2, CountOrders(_dbPath));
    }

    [Fact]
    public void RestoreFrom_MissingBackup_Fails()
    {
        CreateLiveDb();
        var result = BackupService.RestoreFrom(Path.Combine(_root, "ghost.db"), _dbPath);

        Assert.False(result.Success);
        Assert.Equal(1, CountOrders(_dbPath));
    }

    [Fact]
    public void RestoreFrom_SameFileAsLiveDb_Refuses()
    {
        CreateLiveDb();
        var result = BackupService.RestoreFrom(_dbPath, _dbPath);

        Assert.False(result.Success);
        Assert.Contains("قاعدة البيانات نفسها", result.Message);
    }

    [Fact]
    public void RestoreFrom_BlankPath_Fails()
    {
        CreateLiveDb();
        Assert.False(BackupService.RestoreFrom("   ", _dbPath).Success);
    }

    [Fact]
    public void RestoreFrom_LiveDbMissing_CreatesItFromBackup()
    {
        CreateLiveDb();
        string? backup = BackupService.CreateBackupTo(_dbPath, _backupDir);
        Assert.NotNull(backup);

        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        TryDelete(_dbPath + "-wal");
        TryDelete(_dbPath + "-shm");

        var result = BackupService.RestoreFrom(backup!, _dbPath);

        Assert.True(result.Success, result.Message);
        Assert.True(File.Exists(_dbPath));
        SqliteConnection.ClearAllPools();
        Assert.Equal(1, CountOrders(_dbPath));
    }

    static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
