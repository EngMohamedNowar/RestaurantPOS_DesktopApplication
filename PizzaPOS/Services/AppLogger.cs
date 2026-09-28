using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PizzaPOS.Services
{
    /// <summary>
    /// اللوج — ملف لكل يوم في %AppData%\PizzaPOS\logs.
    ///
    /// — كانت المشاكل في النسخة الأولى:
    ///
    /// 1) مافيش retention ولا سقف حجم — ملف جديد كل يوم ومحدش بيحذف.
    ///    جهاز POS شغال سنة = 365 ملف، ومن يوم Storm من الأخطاء (حلقة
    ///    (حلقة بتعمل log في catch) ملف واحد ممكن يبقى عشرات الميغابايت.
    ///
    /// 2) File.AppendAllText بيفتح ويقفل الملف **مع كل سطر**. 22 نداء
    ///    في الكود، وأي error loop بيعمل open/close بلا نهاية.
    ///
    /// 3) الـ static ctor كان بيعمل Directory.CreateDirectory بدون try.
    ///    لو فشل مرة واحدة، TypeInitializationException بيتم **للأبد**
    ///    على كل نداء بعده — والـ try/catch جوه Write مش هينفع لأنه
    ///    بيحصل قبل ما Write يدخل أصلاً. يعني AppData مقفول/صلاحيات
    ///    غلط = البرنامج بيقع في نص أي عملية.
    ///
    /// 4) Error(msg, ex) كان بيكتب ex.ToString() فوقه سطر واحد بس،
    ///    فالـ stack trace متعدد الأسطر كان بيبان كأنه entries جديدة
    ///    من غير timestamp ولا level — ومحدش يقدر يعرف فين entry
    ///    خلص وبدأ تاني.
    /// </summary>
    public static class AppLogger
    {
        /// <summary>كام يوم نحتفظ بيه.</summary>
        public const int MaxLogFiles = 30;

        /// <summary>سقف حجم الملف اليومي — بعده يعمل rotation.</summary>
        public const long MaxFileBytes = 5L * 1024 * 1024;

        /// <summary>كام نسخة متRotation لاليوم الواحد (.1.log, .2.log, .3.log).</summary>
        public const int MaxRotationsPerDay = 3;

        static readonly object _lock = new();

        static StreamWriter? _writer;
        static string? _writerPath;
        static string? _dirCached;
        static string? _dirOverride;
        static bool _pruned;

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message, Exception? ex = null)
            => Write("ERROR", ex != null ? $"{message}\n{ex}" : message);

        static void Write(string level, string message)
        {
            lock (_lock)
            {
                    // اللوج لازم يفضل "best effort": أي فشل هنا يسكت نفسه
                    // ولا يطلع استثناء مهما كان البرنامج شغال.

                try
                {
                    string? dir = ResolveLogDir();
                    if (dir == null) return;

                    try { Directory.CreateDirectory(dir); }
                    catch { return; }   // permissions / AppData مقفول

                    PruneOnce(dir);

                    DateTime now = DateTime.Now;
                    string path = Path.Combine(dir, $"{now:yyyy-MM-dd}.log");
                    RotateIfNeeded(dir, path);

                    StreamWriter? w = GetWriter(path);
                    if (w == null) return;

                    w.Write(Format(now, level, message));
                    w.Flush();
                }
                catch
                {
                    // آخر خط دفاع. لو الكتابة وقعت، نسد الـ writer ونكمل.
                    CloseWriter();
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  المسار
        // ══════════════════════════════════════════════════════════

        static string? ResolveLogDir()
        {
            if (_dirOverride != null) return _dirOverride;
            if (_dirCached != null) return _dirCached;

            try
            {
                _dirCached = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PizzaPOS", "logs");
                return _dirCached;
            }
            catch
            {
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  التنسيق — كل entry يفضل entry واحد مقروء
        // ══════════════════════════════════════════════════════════

        static string Format(DateTime ts, string level, string message)
        {
            string flat = (message ?? "")
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            var sb = new StringBuilder();
            string[] lines = flat.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(i == 0 ? $"[{ts:HH:mm:ss}] [{level}] " : "        | ");
                sb.Append(lines[i].TrimEnd());
            }
            return sb.Append('\n').ToString();
        }

        // ══════════════════════════════════════════════════════════
        //  Writer — مفتوح طول الوقت مش مع كل سطر
        // ══════════════════════════════════════════════════════════

        static StreamWriter? GetWriter(string path)
        {
            if (_writer != null)
            {
                // نفس الملف => نستخدمه. لو اتمسح من بره، نفتح تاني.
                if (string.Equals(_writerPath, path, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(path))
                    return _writer;
                CloseWriter();
            }

            try
            {
                // FileShare.ReadWrite عشان الـ log يفضل مقروء من بره طول
                // ما البرنامج شغال (notepad / tail -f). و FileShare.Delete
                // عشان support أو سكريبت تنظيف يقدر يمسح اللوج من غير
                // IOException — ومن غيره الـ File.Exists check في
                // GetWriter بيبقى dead code لأنه الملف مش هينحذف أصلاً.
                // AutoFlush عشان آخر error قبل crash ما يضيعش (مهم في POS).
                var stream = new FileStream(path, FileMode.Append,
                    FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                _writerPath = path;
                return _writer;
            }
            catch
            {
                CloseWriter();
                return null;
            }
        }

        static void CloseWriter()
        {
            try { _writer?.Dispose(); } catch { }
            _writer = null;
            _writerPath = null;
        }

        // ══════════════════════════════════════════════════════════
        //  Retention + Rotation
        // ══════════════════════════════════════════════════════════

        static void PruneOnce(string dir)
        {
            if (_pruned) return;
            _pruned = true;

            try
            {
                DateTime cutoff = DateTime.Today.AddDays(-MaxLogFiles);
                foreach (FileInfo f in new DirectoryInfo(dir).GetFiles("*.log"))
                {
                    if (LogDate(f) >= cutoff) continue;
                    try { f.Delete(); } catch { /* مفتوح — نسيبه لغيره */ }
                }
            }
            catch { }
        }

        /// <summary>
        /// تاريخ اللوج من **اسم الملف** مش من LastWriteTime.
        ///
        /// الاسم هو اللي إحنا كاتبينه (yyyy-MM-dd.log و .1.log للـ
        /// rotation) فهو المصدر الصادق. LastWriteTime بيتغيّر لو الملف
        /// اتنسخ أو اتعمله restore أو اتمسح من drive تاني — فكان ممكن
        /// نمسح لوج النهاردة أو نسيب لوج من 6 شهور.
        ///
        /// لو الاسم مش مفهوم (حد حط ملف يدوي في المجلد) نرجع
        /// لـ LastWriteTime بدل ما نخسره.
        /// </summary>
        static DateTime LogDate(FileInfo f)
        {
            string stamp = Path.GetFileNameWithoutExtension(f.Name);   // yyyy-MM-dd
            int dot = stamp.IndexOf('.');
            if (dot > 0) stamp = stamp[..dot];                        // 2026-09-27 من .1

            return DateTime.TryParseExact(stamp, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out DateTime d)
                ? d
                : f.LastWriteTime;
        }

        static void RotateIfNeeded(string dir, string path)
        {
            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists || fi.Length < MaxFileBytes) return;

                // لازم نقفل قبل الـ Move، وإلا الـ rename هيفشل على ويندوز
                CloseWriter();

                string stamp = Path.GetFileNameWithoutExtension(path);

                string oldest = Path.Combine(dir, $"{stamp}.{MaxRotationsPerDay}.log");
                if (File.Exists(oldest)) File.Delete(oldest);

                for (int i = MaxRotationsPerDay - 1; i >= 1; i--)
                {
                    string from = Path.Combine(dir, $"{stamp}.{i}.log");
                    if (File.Exists(from))
                        File.Move(from, Path.Combine(dir, $"{stamp}.{i + 1}.log"));
                }

                File.Move(path, Path.Combine(dir, $"{stamp}.1.log"));
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════
        //  للاختبارات
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// بيكتب اللوج في مجلد مؤقت بدل %AppData%. من غير كده اختبارات
        /// الـ retention هتمسح لوجات المستخدم الحقيقية.
        /// </summary>
        internal static void OverrideLogDir(string? dir)
        {
            lock (_lock)
            {
                CloseWriter();
                _dirOverride = dir;
                _pruned = false;
            }
        }

        internal static void CloseForTests()
        {
            lock (_lock) CloseWriter();
        }
    }
}
