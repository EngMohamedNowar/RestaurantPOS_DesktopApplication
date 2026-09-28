// Services/BackupService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;

namespace PizzaPOS.Services
{
    public record BackupResult(bool Success, string Message)
    {
        public static BackupResult Ok(string msg) => new(true, msg);
        public static BackupResult Fail(string msg) => new(false, msg);
    }

    public record BackupInfo(string Path, DateTime CreatedAt, long SizeBytes)
    {
        public string FileName => System.IO.Path.GetFileName(Path);
        public string SizeDisplay => SizeBytes >= 1024 * 1024
            ? $"{SizeBytes / 1024.0 / 1024.0:0.0} MB"
            : $"{SizeBytes / 1024.0:0.0} KB";
    }

    public static class BackupService
    {
        static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static readonly string BaseDir = Path.Combine(AppData, "PizzaPOS");
        static readonly string DbPath = Path.Combine(BaseDir, "pos.db");
        static readonly string BackupDir = Path.Combine(BaseDir, "backups");

        public const int MaxBackups = 10;

        // ══════════════════════════════════════════════════════════════════
        //  Create
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// نسخة احتياطية متسقة من الـ DB الحي.
        ///
        /// الكود القديم كان بيعمل File.Copy على pos.db مباشرة. ده غلط مع
        /// journal_mode=WAL: المعاملات اللي لسه ما اتعمل لها checkpoint عايشة
        /// في ملف pos.db-wal المجاور لملف الـ db، فالنسخة كانت بتفقد آخر المبيعات.
        ///
        /// الحل: VACUUM INTO — بياخد snapshot متسقة من الـ WAL كله ويكتبها
        /// في ملف واحد مضمون إنه سليم (atomic). محتاج SQLite 3.27+.
        /// </summary>
        public static string? CreateBackup()
        {
            try
            {
                return CreateBackupTo(DbPath, BackupDir);
            }
            catch (Exception ex)
            {
                AppLogger.Error("CreateBackup failed", ex);
                return null;
            }
        }

        /// <summary>نفس <see cref="CreateBackup"/> بس بمسارات صريحة — للاختبارات.</summary>
        public static string? CreateBackupTo(string dbPath, string backupDir)
        {
            if (!File.Exists(dbPath))
            {
                AppLogger.Warn("CreateBackup skipped: database file does not exist yet.");
                return null;
            }

            Directory.CreateDirectory(backupDir);

            string backupFile = NextBackupPath(backupDir, "pos_");
            VacuumInto(dbPath, backupFile);

            if (!File.Exists(backupFile) || new FileInfo(backupFile).Length == 0)
                throw new IOException("النسخة اتعملت بس الملف فاضي — DB ممكن يكون تالف.");

            AppLogger.Info($"Backup created: {backupFile} ({new FileInfo(backupFile).Length} bytes)");
            CleanupOldBackups(backupDir);
            return backupFile;
        }

        /// <summary>
        /// لقطة متسقة من DB كامل (WAL متضمن) في ملف واحد.
        /// VACUUM INTO بيرفض يكتب فوق ملف موجود.
        /// </summary>
        static void VacuumInto(string dbPath, string targetFile)
        {
            // Microsoft.Data.Sqlite بيعمل connection pooling. أي connection متسابة
            // في الـ pool بتمسك pos.db و pos.db-wal open، وده بيخلي اللقطة تطلع
            // من نسخة قديمة أو يمنع حذف الملفات بعدين. فضلنا نفرّغ الـ pool.
            SqliteConnection.ClearAllPools();

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            DatabaseHelper.Configure(conn);

            // VACUUM INTO بيقبل expression، والـ parameter binding معاه
            // غير مضمون على كل إصدارات SQLite، فالـ path بيتحوّل لـ SQL literal
            // مع تهريب علامات الاقتباس المفردة.
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "VACUUM INTO " + SqlLiteral(targetFile);
            cmd.ExecuteNonQuery();
        }


        static string NextBackupPath(string dir, string prefix)
        {
            // VACUUM INTO بيرفض يكتب فوق ملف موجود، فلازم نضمن اسم فريد.
            for (int attempt = 0; attempt < 60; attempt++)
            {
                string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string candidate = Path.Combine(dir, $"{prefix}{stamp}_{attempt:00}.db");
                if (!File.Exists(candidate)) return candidate;
            }
            throw new IOException("مقدرناش نلاقي اسم فريد لملف النسخة الاحتياطية.");
        }

        static string SqlLiteral(string value) => "'" + value.Replace("'", "''") + "'";

        static void CleanupOldBackups(string backupDir)
        {
            try
            {
                if (!Directory.Exists(backupDir)) return;

                var files = new DirectoryInfo(backupDir)
                    .GetFiles("pos_*.db")
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToArray();

                foreach (var file in files.Skip(MaxBackups))
                {
                    try { file.Delete(); }
                    catch (Exception ex) { AppLogger.Warn($"Could not delete old backup {file.Name}: {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"CleanupOldBackups failed: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Query
        // ══════════════════════════════════════════════════════════════════

        public static List<BackupInfo> ListBackups()
        {
            var list = new List<BackupInfo>();
            try
            {
                if (!Directory.Exists(BackupDir)) return list;

                foreach (var f in new DirectoryInfo(BackupDir).GetFiles("pos_*.db")
                             .OrderByDescending(f => f.LastWriteTime))
                {
                    list.Add(new BackupInfo(f.FullName, f.LastWriteTime, f.Length));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"ListBackups failed: {ex.Message}");
            }
            return list;
        }

        public static string? GetLatestBackup()
            => ListBackups().FirstOrDefault()?.Path;

        public static int GetBackupCount() => ListBackups().Count;

        // ══════════════════════════════════════════════════════════════════
        //  Delete
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// حذف نسخة احتياطية نهائياً من مجلد backups.
        ///
        /// المسار *لازم* يكون جوه مجلد النسخ — أي مسار برّيه (بما فيها
        /// pos.db نفسها أو ملفات نظام) بيترفض قبل ما نلمس أي حاجة.
        /// </summary>
        public static BackupResult DeleteBackup(string backupPath)
            => DeleteBackupFrom(backupPath, BackupDir);

        /// <summary>نفس <see cref="DeleteBackup"/> بمسار مجلد صريح — للاختبارات.</summary>
        public static BackupResult DeleteBackupFrom(string backupPath, string backupDir)
        {
            if (string.IsNullOrWhiteSpace(backupPath))
                return BackupResult.Fail("مفيش مسار نسخة.");

            string full = Path.GetFullPath(backupPath);
            string root = Path.GetFullPath(backupDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            // StartsWith بيحمي من traversal (backups\..\pos.db) —
            // Path.GetFullPath بيحل ".." قبل المقارنة، فلو الملف طلع
            // برّه المجلد، المقارنة بتفشل والرفض بيحصل.
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return BackupResult.Fail("الملف ده مش جوه مجلد النسخ الاحتياطي — الرفض.");

            if (!File.Exists(full))
                return BackupResult.Fail("النسخة مش موجودة — يمكن اتمسحت قبل كده.");

            try
            {
                File.Delete(full);
                return BackupResult.Ok($"تم حذف {Path.GetFileName(full)}");
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"DeleteBackup failed for '{Path.GetFileName(full)}': {ex.Message}");
                return BackupResult.Fail($"مقدرناش نحذف النسخة: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Restore
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// استعادة نسخة احتياطية فوق الـ DB الحي.
        ///
        /// الكود القديم كان بيكتب النسخة في فولدر restore/ بس — مفيش أي مستدعٍ،
        /// وحتى لو اتنادى مكنش بيعمل حاجة. الكود ده بيبدّل الـ DB فعلياً، مع:
        ///   1) تحقق إن الملف نسخة SQLite سليمة
        ///   2) نسخة أمان من الحالة الحالية قبل الاستبدال
        ///   3) حذف pos.db-wal / pos.db-shm — بدونهم الـ WAL القديم هيعيد
        ///      كتابة صفحاته فوق الـ DB المستعادة ويخرّبها
        /// </summary>
        public static BackupResult RestoreBackup(string backupPath)
            => RestoreFrom(backupPath, DbPath);

        /// <summary>نفس <see cref="RestoreBackup"/> بس بمسارات صريحة — للاختبارات.</summary>
        public static BackupResult RestoreFrom(string backupPath, string dbPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
                return BackupResult.Fail("ملف النسخة الاحتياطية غير موجود.");

            if (Path.GetFullPath(backupPath) == Path.GetFullPath(dbPath))
                return BackupResult.Fail("الملف المختار هو قاعدة البيانات نفسها — اختار نسخة من مجلد backups.");

            if (!ValidateSqliteFile(backupPath, out string? problem))
                return BackupResult.Fail($"الملف ده مش قاعدة بيانات SQLite سليمة: {problem}");

            try
            {
                var dir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string[] stale = { dbPath + "-wal", dbPath + "-shm" };

                // 0) فرّغ الـ connection pool — أي connection متسابة بتمسك
                //    pos.db و pos.db-wal، وده بيمنع استبدال الملف أو حذف الـ WAL.
                //    لازم يتعمل ده *قبل* اللقطة وبعدها، لأن أي عملية قراءة
                //    تانية هتفتح connection جديد على الملف القديم.
                SqliteConnection.ClearAllPools();

                // 1) شبكة أمان: لقطة متسقة من الحالة الحالية قبل الاستبدال.
                //    لازم VACUUM INTO مش File.Copy — غير كده النسخة هتكون
                //    ناقصة أي معاملة لسه في الـ WAL.
                string safety = NextBackupPath(dir ?? AppData, "pre-restore_");
                bool hadLiveDb = File.Exists(dbPath);
                if (hadLiveDb)
                    VacuumInto(dbPath, safety);

                // 2) اتأكد إننا قادرين نمسح ملفات WAL *قبل* ما نلمس pos.db.
                //    لو الـ WAL القديم فضل موجود، SQLite هيعيد كتابة صفحاته فوق
                //    الـ DB المستعادة والبيانات هتبقى مكسورة — فبنرفض بدل ما
                //    نسيب البرنامج في حالة نصف مستعادة.
                SqliteConnection.ClearAllPools();
                foreach (var f in stale) TryDelete(f);

                var leftover = stale.Where(File.Exists).Select(Path.GetFileName).ToArray();
                if (leftover.Length > 0)
                {
                    AppLogger.Warn(
                        $"Restore aborted before touching the DB: cannot remove {string.Join(", ", leftover)}");
                    TryDelete(safety);
                    return BackupResult.Fail(
                        "في ملفات قاعدة بيانات مفتوحة (WAL) منعت الاستعادة. "
                        + "اقفل كل نوافذ البرنامج — بما فيها أي تقرير مفتوح — وبعدين أعد المحاولة. "
                        + "مفيش أي تعديل اتعمل على بياناتك.");
                }

                // 3) استبدل الـ DB
                SqliteConnection.ClearAllPools();
                File.Copy(backupPath, dbPath, true);

                // 4) تأكد إن اللي اتكتب سليم فعلاً، وإلا ارجع للحالة السابقة
                if (!ValidateSqliteFile(dbPath, out string? postProblem))
                {
                    SqliteConnection.ClearAllPools();
                    if (hadLiveDb && File.Exists(safety)) File.Copy(safety, dbPath, true);
                    foreach (var f in stale) TryDelete(f);
                    return BackupResult.Fail(
                        $"الاستعادة فشلت والتحقق من الملف الجديد رجع: {postProblem}. "
                        + "رجّعنا الحالة السابقة.");
                }

                AppLogger.Info($"Restored DB from '{backupPath}'. Previous state saved as '{safety}'.");
                return BackupResult.Ok(
                    "تمت الاستعادة بنجاح.\n\n"
                    + "اقفل البرنامج وافتحه تاني عشان تشتغل بالبيانات المستعادة.\n"
                    + (hadLiveDb
                        ? $"الحالة القديمة محفوظة في: {Path.GetFileName(safety)}"
                        : "مفيش حالة سابقة كانت محفوظة."));
            }
            catch (Exception ex)
            {
                AppLogger.Error($"RestoreFrom('{backupPath}') failed", ex);
                return BackupResult.Fail($"فشلت الاستعادة: {ex.Message}");
            }
        }

        /// <summary>
        /// بيتأكد إن الملف قاعدة SQLite سليمة عبر فتحه وقراءة sqlite_master.
        /// لو اتنادى على ملف نصي أو نسخة مقطوعة بيرجع false.
        /// </summary>
        public static bool ValidateSqliteFile(string path, out string? problem)
        {
            problem = null;
            try
            {
                if (!File.Exists(path)) { problem = "الملف غير موجود"; return false; }
                if (new FileInfo(path).Length == 0) { problem = "الملف فاضي"; return false; }

                using var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
                conn.Open();
                DatabaseHelper.Configure(conn);
                using var cmd = conn.CreateCommand();
                // integrity_check بيقرأ كل الصفحات — لو في نسخة مقطوعة هيلاقيها.
                cmd.CommandText = "PRAGMA quick_check";
                var result = cmd.ExecuteScalar() as string;
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    problem = result ?? "quick_check رجع نتيجة غير متوقعة";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                problem = ex.Message;
                return false;
            }
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { AppLogger.Warn($"Could not delete '{Path.GetFileName(path)}': {ex.Message}"); }
        }
    }
}
