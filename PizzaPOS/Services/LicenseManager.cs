using System;
using System.IO;

namespace PizzaPOS.Services
{
    /// <summary>
    /// إدارة الترخيص في جانب العميل — **تحقّق فقط**.
    ///
    /// مفيش أي مفتاح سري هنا عن قصد: البرنامج ما بيقدرش يوقّع ترخيص،
    /// بيقدرش يتحقق غير بتوقيعات الأداة بس.
    ///
    /// الملف license.dat في %AppData%\PizzaPOS، 3 أسطر:
    ///   0) HWID الجهاز
    ///   1) المفتاح = تاريخ الانتهاء + التوقيع   ← المصدر الوحيد للحقيقة
    ///   2) تاريخ الإصدار (معلوماتي، ماله أي أثر على الصلاحية)
    ///
    /// مفيش سطر "تاريخ الانتهاء" منفصل عن قصد. في النظام القديم كان
    /// موجود سطر مستقل، والبرنامج كان بيثق فيه — يعني أي حد يفتح الملف
    /// ويكتب PERMANENT وخلاص. دلوقتي التاريخ **جوه المفتاح الموقّع**،
    /// فمافيش سطر عدله في الملف أصلاً: تغيّره معناه إنك ما عندكش توقيع
    /// أصلاً. أي تعديل على السطر 1 بيكسر التحقق فوراً.
    /// </summary>
    public static class LicenseManager
    {
        static readonly string LicenseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PizzaPOS");

        static readonly string LicenseFile = Path.Combine(LicenseDir, "license.dat");

        /// <summary>
        /// مسار بديل للاختبارات بس (internal). بدون ده اختبارات الترخيص
        /// هتكتب على license.dat الحقيقي في %AppData% وتمسح ترخيص المستخدم.
        /// </summary>
        static string? _pathOverride;

        internal static string LicenseFilePath => _pathOverride ?? LicenseFile;

        internal static IDisposable OverridePath(string path)
        {
            string? previous = _pathOverride;
            _pathOverride = path;
            return new PathScope(() => _pathOverride = previous);
        }

        sealed class PathScope : IDisposable
        {
            readonly Action _restore;
            bool _done;
            public PathScope(Action restore) => _restore = restore;
            public void Dispose()
            {
                if (_done) return;
                _done = true;
                _restore();
            }
        }

        public static string GetHwid() => HardwareId.GetShortId();

        /// <summary>يقرأ الملف ويرجّع (متحقق، سبب الرفض).</summary>
        public static bool IsActivated() => TryCheck(out _);

        public static bool TryCheck(out string reason)
        {
            reason = "";

            string[] lines;
            try
            {
                if (!File.Exists(LicenseFilePath)) { reason = "NO_LICENSE_FILE"; return false; }
                lines = File.ReadAllLines(LicenseFilePath);
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"License: cannot read file: {ex.Message}");
                reason = "READ_ERROR";
                return false;
            }

            if (lines.Length < 3) { reason = "MALFORMED_FILE"; return false; }

            string storedHwid = lines[0].Trim();
            string key = lines[1].Trim();

            if (!LicenseSignature.TrySplitKey(key, out string expiryCode, out string signature))
            {
                AppLogger.Warn("License: key is not in the expected format");
                reason = "BAD_KEY_FORMAT";
                return false;
            }

            // (1) التوقيع — أهم خطوة. لو التاريخ اتغيّر أو الـ HWID اتغيّر
            //     أو حد عمل mint مفتاح بنفسه، هتفشل هنا قبل أي تاني.
            if (!LicenseSignature.Verify(storedHwid, expiryCode, signature))
            {
                AppLogger.Warn("License: signature verification FAILED — forged or tampered file");
                reason = "BAD_SIGNATURE";
                return false;
            }

            // (2) الترخيص لجهاز واحد بس
            string currentHwid = GetHwid();
            if (!string.Equals(storedHwid, currentHwid, StringComparison.OrdinalIgnoreCase))
            {
                reason = "WRONG_DEVICE";
                return false;
            }

            // (3) الصلاحية — التاريخ ده جاي من جوه المفتاح الموقّع، فمش
            //     حد يقدر يعدّله من غير ما التوقيع يفشل في الخطوة (1)
            if (!expiryCode.Equals(LicenseSignature.Permanent, StringComparison.OrdinalIgnoreCase))
            {
                if (!DateTime.TryParseExact(expiryCode, "yyyyMMdd", null,
                        System.Globalization.DateTimeStyles.None, out DateTime expiry))
                {
                    reason = "BAD_EXPIRY";
                    return false;
                }
                if (DateTime.Today > expiry)
                {
                    reason = "EXPIRED";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// تفعيل: بيحفظ المفتاح الموقّع اللي المستخدم لصقه. ما بيحددش مدة —
        /// المدة جوه التوقيع، ومين يوقّع هو الأداة بس.
        /// </summary>
        public static bool Activate(string key)
        {
            if (!LicenseSignature.TrySplitKey(key, out string expiryCode, out string signature))
                return false;

            if (!LicenseSignature.Verify(GetHwid(), expiryCode, signature))
            {
                AppLogger.Warn("License: activate rejected — signature invalid for this device");
                return false;
            }

            if (!expiryCode.Equals(LicenseSignature.Permanent, StringComparison.OrdinalIgnoreCase)
                && !DateTime.TryParseExact(expiryCode, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out _))
                return false;

            try
            {
                Directory.CreateDirectory(LicenseDir);
                File.WriteAllLines(LicenseFilePath, new[]
                {
                    GetHwid(),
                    expiryCode + LicenseSignature.ExpirySeparator + signature,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                AppLogger.Info($"License activated ({LicenseSignature.FormatExpiryForDisplay(expiryCode)})");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"License: cannot write file: {ex.Message}");
                return false;
            }
        }

        /// <summary>معلومات الصلاحية للعرض (لا تؤثر على القرار).</summary>
        public static string GetExpiryInfo()
        {
            try
            {
                if (!File.Exists(LicenseFilePath)) return "NO_LICENSE";

                string[] lines = File.ReadAllLines(LicenseFilePath);
                if (lines.Length < 3 || !LicenseSignature.TrySplitKey(lines[1], out string code, out _))
                    return "NO_LICENSE";

                if (code.Equals(LicenseSignature.Permanent, StringComparison.OrdinalIgnoreCase))
                    return "PERMANENT";

                if (!DateTime.TryParseExact(code, "yyyyMMdd", null,
                        System.Globalization.DateTimeStyles.None, out DateTime expiry))
                    return "INVALID";

                TimeSpan remaining = expiry.Date.AddDays(1) - DateTime.Now;
                if (remaining.TotalDays <= 0) return "EXPIRED";
                if (remaining.TotalDays <= 7)
                    return $"expiresIn {remaining.Days}d {remaining.Hours}h";
                return $"expiresIn {remaining.Days} days";
            }
            catch { return "ERROR"; }
        }

        public static void Deactivate()
        {
            try { if (File.Exists(LicenseFilePath)) File.Delete(LicenseFilePath); }
            catch { }
        }
    }
}
