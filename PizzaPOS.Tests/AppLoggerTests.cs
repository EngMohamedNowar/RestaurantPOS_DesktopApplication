using System;
using System.IO;
using System.Linq;
using System.Text;
using PizzaPOS.Services;
using Xunit;

namespace PizzaPOS.Tests;

/// <summary>
/// اختبارات اللوج — تركّز على السلوك اللي كان غايب: retention، سقف
/// الحجم، والـ entries المتعددة الأسطر.
///
/// كل الاختبارات بيكتبوا في مجلد مؤقت عبر AppLogger.OverrideLogDir،
/// عشان نماسش لوجات المستخدم الحقيقية في %AppData%.
/// </summary>
[Collection("sequential")]
public class AppLoggerTests : IDisposable
{
    readonly string _dir;

    public AppLoggerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(),
            "PizzaPOS.LogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        AppLogger.OverrideLogDir(_dir);
    }

    public void Dispose()
    {
        AppLogger.CloseForTests();
        AppLogger.OverrideLogDir(null);
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
    }

    string TodayFile => Path.Combine(_dir, $"{DateTime.Now:yyyy-MM-dd}.log");

    // مهم: نقرأ بـ FileShare.ReadWrite مش File.ReadAllText العادي.
    // الـ writer بيفتح الملف بـ FileAccess.Write، وأي handle تاني
    // لازم يسمح لـ Write — و File.ReadAllText بيفتح بـ FileShare.Read
    // فبيدّي IOException. نفس السبب اللي خلّى الـ logger يفتح
    // بـ FileShare.ReadWrite: اللوج يفضل مقروء من بره وهو شغال.
    string Read(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    // ─────────────────────────── التنسيق ───────────────────────────

    [Fact]
    public void Info_WritesTimestampAndLevel()
    {
        AppLogger.Info("hello");

        Assert.Contains("[INFO] hello", Read(TodayFile));
    }

    [Fact]
    public void Warn_AndError_UseTheirLevels()
    {
        AppLogger.Warn("careful");
        AppLogger.Error("boom");

        string text = Read(TodayFile);
        Assert.Contains("[WARN] careful", text);
        Assert.Contains("[ERROR] boom", text);
    }

    [Fact]
    public void Error_WithException_KeepsItInOneEntry()
    {
        // المشكلة القديمة: ex.ToString() متعدد الأسطر كان بيكسر الشكل،
        // فالأسطر التانية بتبان كأنها entries جديدة من غير timestamp.
        AppLogger.Error("save failed", new InvalidOperationException("db is locked"));

        string[] lines = Read(TodayFile)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Contains("[ERROR] save failed", lines[0]);
        Assert.Contains("InvalidOperationException", lines[1]);
        Assert.Contains("db is locked", lines[1]);

        // سطر الـ continuation لازم يبقى معلّم إن ده Continuation
        Assert.StartsWith("        | ", lines[1]);
    }

    [Fact]
    public void MultiLineMessage_EachLineKeepsItsTimestampOrPipe()
    {
        AppLogger.Info("line one\nline two\r\nline three");

        string[] lines = Read(TodayFile)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.Contains("[INFO] line one", lines[0]);
        Assert.All(lines.Skip(1), l => Assert.StartsWith("        | ", l));
    }

    [Fact]
    public void ConsecutiveEntries_AreLineSeparated()
    {
        AppLogger.Info("first");
        AppLogger.Info("second");
        AppLogger.Info("third");

        string[] lines = Read(TodayFile)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.All(lines, l => Assert.Contains("[INFO]", l));
    }

    // ─────────────────────────── Writer ───────────────────────────

    [Fact]
    public void Writes_AppendToTheSameDayFile()
    {
        AppLogger.Info("a");
        AppLogger.Info("b");
        AppLogger.Info("c");

        Assert.Single(Directory.GetFiles(_dir, "*.log"));
    }

    [Fact]
    public void Writer_SurvivesFileDeletedExternally()
    {
        AppLogger.Info("before");
        // الـ writer مفتوح — لو حد مسح الملف من بره لازم نلاقي
        // طريقنا تاني من غير exception.
        File.Delete(TodayFile);

        AppLogger.Info("after");

        Assert.True(File.Exists(TodayFile));
        Assert.Contains("after", Read(TodayFile));
    }

    [Fact]
    public void LogFile_IsReadableFromOutsideWhileAppIsRunning()
    {
        // سبب FileShare.ReadWrite في الـ writer: لازم حد يقدر يفتح
        // الـ log بـ notepad/ت tail -f طول ما البرنامج شغال.
        AppLogger.Info("visible");

        using var fs = new FileStream(TodayFile, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8);

        Assert.Contains("visible", sr.ReadToEnd());
    }

    // ─────────────────────────── Retention ───────────────────────────

    [Fact]
    public void Prune_DeletesFilesOlderThanRetention()
    {
        // 40 يوم ملف قديم
        string old = Path.Combine(_dir, $"{DateTime.Today.AddDays(-40):yyyy-MM-dd}.log");
        File.WriteAllText(old, "ancient");

        AppLogger.Info("now");

        Assert.False(File.Exists(old));
    }

    [Fact]
    public void Prune_KeepsFilesInsideRetention()
    {
        string recent = Path.Combine(_dir, $"{DateTime.Today.AddDays(-5):yyyy-MM-dd}.log");
        File.WriteAllText(recent, "recent");

        AppLogger.Info("now");

        Assert.True(File.Exists(recent));
    }

    [Fact]
    public void Prune_RunsOnlyOncePerProcess()
    {
        // لو اتنفذت مع كل write، كان هتفتح/تعمل DirectoryInfo مع كل سطر.
        // بنتحقق إنها شغالة once بس: file بينcleanup وإننا نكتب تاني
        // لازم يفضل موجود.
        AppLogger.Info("first");

        string sneaked = Path.Combine(_dir, $"{DateTime.Today.AddDays(-90):yyyy-MM-dd}.log");
        File.WriteAllText(sneaked, "late arrival");

        AppLogger.Info("second");

        Assert.True(File.Exists(sneaked));
    }

    [Fact]
    public void Prune_AlsoHandlesRotatedFiles()
    {
        // ملفات الـ rotation بتاخد نفس الاسم + .N، ولازم التنضيف يشملها
        string rotated = Path.Combine(_dir, $"{DateTime.Today.AddDays(-40):yyyy-MM-dd}.1.log");
        File.WriteAllText(rotated, "rotated ancient");

        AppLogger.Info("now");

        Assert.False(File.Exists(rotated));
    }

    [Fact]
    public void Prune_IgnoresNonLogFiles()
    {
        string other = Path.Combine(_dir, "notes.txt");
        File.WriteAllText(other, "keep me");

        AppLogger.Info("now");

        Assert.True(File.Exists(other));
    }

    [Fact]
    public void Prune_UsesTheFilenameDate_NotLastWriteTime()
    {
        // لوج النهاردة اتنسخ لـ drive تاني ورجع — LastWriteTime بقى
        // قديم، بس الاسم لسه بيقول النهاردة. لازم يفضل موجود.
        string today = Path.Combine(_dir, $"{DateTime.Now:yyyy-MM-dd}.log");
        File.WriteAllText(today, "copied back from somewhere");
        File.SetLastWriteTime(today, DateTime.Today.AddDays(-100));

        AppLogger.Info("now");

        Assert.True(File.Exists(today));
    }

    [Fact]
    public void Prune_KeepsUnparsableNames()
    {
        // حد حط ملف بإيده باسم مش مفهوم — ما نشيلوش، نرجع لـ LastWriteTime
        string weird = Path.Combine(_dir, "crash-dump.log");
        File.WriteAllText(weird, "hand placed");
        File.SetLastWriteTime(weird, DateTime.Now);

        AppLogger.Info("now");

        Assert.True(File.Exists(weird));
    }

    // ─────────────────────────── Rotation ───────────────────────────

    [Fact]
    public void Rotate_TriggersOnceOverSizeCap()
    {
        // نكتب لحد ما الملف يعدّي سقف الحجم (5MB). بعد الـ rotation
        // الملف *الحالي* يبقى صغير — ده الصح. فلازم نقيس الإجمالي
        // عبر كل الملفات، مش الملف الحالي بس.
        string filler = new string('x', 4096);
        for (int i = 0; i < 1400; i++) AppLogger.Info(filler);

        long total = Directory.GetFiles(_dir, "*.log")
            .Sum(p => new FileInfo(p).Length);

        Assert.True(total > AppLogger.MaxFileBytes,
            $"expected > {AppLogger.MaxFileBytes} total, got {total}");
        Assert.True(Directory.GetFiles(_dir, "*.1.log").Length >= 1);
    }

    [Fact]
    public void Rotate_ShiftsPreviousRotations()
    {
        string stamp = DateTime.Now.ToString("yyyy-MM-dd");
        string r1 = Path.Combine(_dir, $"{stamp}.1.log");
        string r2 = Path.Combine(_dir, $"{stamp}.2.log");

        File.WriteAllText(r1, "one");     // 3 bytes
        File.WriteAllText(r2, "two");     // 3 bytes

        string filler = new string('x', 4096);
        for (int i = 0; i < 1400; i++) AppLogger.Info(filler);

        // بعد الـ rotation: .1→.2، .2→.3، الحالي→.1
        // يعني الـ .2 الجديدة لازم تكون فيها "one" (3 bytes)
        Assert.True(File.Exists(r2));
        Assert.Equal(3, new FileInfo(r2).Length);
        Assert.Contains("one", Read(r2));
    }

    [Fact]
    public void Rotate_DoesNotLoseTheNewestLines()
    {
        // أهم حاجة بعد rotation: السطور الجديدة لازم تكون في الملف
        // الحالي، مش ضايعة في الـ rename.
        string filler = new string('x', 4096);
        for (int i = 0; i < 1400; i++) AppLogger.Info(filler);

        AppLogger.Info("SENTINEL-AFTER-ROTATE");

        Assert.Contains("SENTINEL-AFTER-ROTATE", Read(TodayFile));
    }

    [Fact]
    public void Rotate_CapsTheNumberOfRotations()
    {
        string stamp = DateTime.Now.ToString("yyyy-MM-dd");
        string filler = new string('x', 4096);

        // 4 دورات كاملة — المفروض ميقدرش يبقى في أكتر من MaxRotationsPerDay
        for (int round = 0; round < 4; round++)
            for (int i = 0; i < 1400; i++) AppLogger.Info(filler);

        int rotations = Directory.GetFiles(_dir, $"*.{AppLogger.MaxRotationsPerDay}.log").Length;
        Assert.True(rotations <= 1, $"expected at most 1 .{AppLogger.MaxRotationsPerDay}.log, got {rotations}");
        Assert.True(Directory.GetFiles(_dir, "*.log").Length
            <= AppLogger.MaxRotationsPerDay + 1);
    }

    // ─────────────────────────── المتانة ───────────────────────────

    [Fact]
    public void LogDir_ThatDoesNotExist_IsCreated()
    {
        string nested = Path.Combine(_dir, "deep", "nested", "logs");
        AppLogger.OverrideLogDir(nested);

        AppLogger.Info("into the void");

        string file = Path.Combine(nested, $"{DateTime.Now:yyyy-MM-dd}.log");
        Assert.True(File.Exists(file));
        Assert.Contains("into the void", Read(file));
    }

    [Fact]
    public void UnwritableLogDir_IsSilencedNotThrown()
    {
        // الـ static ctor القديم كان بيعمل Directory.CreateDirectory في
        // constructor — لو فشل، TypeInitializationException للأبد.
        // دلوقتي لازم يفضل best-effort.
        AppLogger.OverrideLogDir("Z:\\definitely\\not\\a\\drive\\logs");
        var ex = Record.Exception(() => AppLogger.Info("should not throw"));

        Assert.Null(ex);
        AppLogger.OverrideLogDir(_dir);
    }

    [Fact]
    public void NullLogDir_IsSilencedNotThrown()
    {
        AppLogger.OverrideLogDir(null);
        AppLogger.OverrideLogDir(_dir);
    }

    [Fact]
    public void NullMessage_DoesNotThrow()
    {
        AppLogger.Info(null!);
        AppLogger.Error(null!);

        Assert.True(File.Exists(TodayFile));
    }

    [Fact]
    public void VeryLongMessage_IsWrittenIntact()
    {
        string big = new string('z', 200_000);
        AppLogger.Info(big);

        Assert.Contains(big, Read(TodayFile));
    }

    [Fact]
    public void UnicodeAndArabic_AreWrittenCorrectly()
    {
        AppLogger.Info("طلب رقم 42 — تم بنجاح ✓");

        Assert.Contains("طلب رقم 42 — تم بنجاح ✓", Read(TodayFile));
    }
}
