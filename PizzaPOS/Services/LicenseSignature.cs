using System;
using System.Security.Cryptography;
using System.Text;

namespace PizzaPOS.Services
{
    /// <summary>
    /// التوقيع الرقمي للترخيص — الملف ده بيتشارك بين البرنامج وأداة التوليد
    /// (شوف PizzaPOS.LicenseGenerator.csproj).
    ///
    /// ليه توقيع ومش HMAC زي الأول؟
    /// ─────────────────────────────────────────────────────────────────────
    /// النظام القديم كان: key = HMACSHA256(secret, hwid)[0..8]. السِكِرت كان
    /// مكتوب في الكود نفسه ("NAPOLI-PIZZA-2025-SECRET-KEY!!")، والتاريخ
    /// بيكتبه العميل في ملفه. يعني أي حد عنده الملف المصدري يقدر يولّد
    /// مفتاح لأي جهاز، وأي حد يعدّل سطر تاريخ الانتهاء لـ PERMANENT.
    ///
    /// الاستبدال: RSA-2048. المفتاح الخاص موجود **في أداة التوليد بس**،
    /// والبرنامج بياخد المفتاح العام ويفضل **يتحقق بس** — يعني حتى لو
    /// حد فتح السورس كله، مش هيعرف يصمّم مفتاح صالح.
    ///
    /// التوقيع بيغطي HWID + تاريخ الانتهاء مع بعض، فأي تعديل على أي
    /// واحد فيهم بيكسر التحقق. التاريخ بقى "موثوق" لأنه مش وثقه
    /// البرنامج على نفسه، التوقيع هو اللي بيثبته.
    ///
    /// RSA-2048 → توقيع 256 بايت → 344 محرف Base64. عشان كده المفتاح
    /// طويل وبيتبعت بالنسخ/اللصق (مش بالكتابة). ده مقصود: ما فيه أي
    /// تريكات تخليه أقصر من كده مع نفس مستوى الأمان.
    /// </summary>
    public static class LicenseSignature
    {
        /// <summary>إصدار صيغة الـ payload — لو اتغير، المفاتيح القديمة بتترفض
        /// برسالة واضحة بدل ما تتمشي مع منطق تاني.</summary>
        public const string PayloadVersion = "v1";

        /// <summary>عدد المحارف في كل مجموعة (للتسهيل البصري بس).</summary>
        public const int GroupSize = 8;

        /// <summary>فاصل المجموعات — نقطة، وهي مش من حروف Base64URL.</summary>
        public const char GroupSeparator = '.';

        /// <summary>قيمة تاريخ الانتهاء الدائم.</summary>
        public const string Permanent = "PERMANENT";

        /// <summary>فاصل تاريخ الانتهاء عن التوقيع داخل المفتاح.</summary>
        public const char ExpirySeparator = '|';

        /// <summary>طول التوقيع المتوقع بالبايت (RSA-2048).</summary>
        public const int SignatureByteLength = 256;

        // ── المفتاح العام ──────────────────────────────────────────────
        // هذا هو المفتاح العام، وهو آمن إنه يكون في كود العميل.
        // المفتاح الخاص المقابل موجود في أداة التوليد على جهاز المورّد
        // (%AppData%\PizzaPOS.LicenseGenerator\license-signing.key)
        // ولو ضاع، كل المفاتيح القديمة تبطل ولا يمكن استرجاعها.
        public const string PublicKeyPem = """
            -----BEGIN PUBLIC KEY-----
            MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzN7fkjHx3Rh1OSMIs6Ww
            GtMlhwORh0XIoTeQA6QjBvGIZ0Im4hD+1MKp25YeUgB7Ov5CG4vboaDkGOI3AVWq
            rTIGMU7oKCdrAKI7OnCGwqbT4IODdef/liK40V6AimzthdXLiI5aEoAB0FkrGXlz
            kkmUEjXbhXQipWlD/4Wc+kNXxCBh1R9cGl1LeY8XTH9qJrRAI8bWrqzP7bvSNAOv
            x0FFnIy8tRgC7Td1MdNE4PL1us8rtk9MiXGcTqn0aTaKQt5Xtly9BraiywJOMVu3
            NYM+I2E/3XEvLq9Bprhk8/YoK3jjY5hkQFsm+X+AQem0Oyzm4xd+8yahK+aD/7Gd
            fQIDAQAB
            -----END PUBLIC KEY-----
            """;

        // ══════════════════════════════════════════════════════════
        //  صيغة المفتاح
        // ══════════════════════════════════════════════════════════

        /// <summary>البيانات الموقّعة. أي تغيير في أي جزء بيكسر التوقيع.</summary>
        public static string BuildPayload(string hwid, string expiryCode)
            => $"{PayloadVersion}|{hwid}|{expiryCode}";

        /// <summary>يكوّن المفتاح الكامل: <c>الانتهاء|التوقيع</c>.</summary>
        public static string BuildKey(RSA privateKey, string hwid, string expiryCode)
        {
            byte[] sig = privateKey.SignData(
                Encoding.UTF8.GetBytes(BuildPayload(hwid, expiryCode)),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return expiryCode + ExpirySeparator + Group(Convert.ToBase64String(sig));
        }

        public static bool Verify(string hwid, string expiryCode, string signature)
            => VerifyWithPem(BuildPayload(hwid, expiryCode), signature, PublicKeyPem);

        /// <summary>
        /// نفس التحقق بس بـ PEM متغيّر — موجود عشان الاختبارات تقدر تعمل
        /// round trip كامل بـ pair مؤقت. مسار الإنتاج دايماً بيمر على
        /// <see cref="Verify(string,string,string)"/> بالمفتاح المدمج بس.
        /// </summary>
        internal static bool VerifyWithPem(string payload, string signature, string publicKeyPem)
        {
            byte[]? sig = Ungroup(signature);
            if (sig == null || sig.Length != SignatureByteLength) return false;

            try
            {
                using RSA rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);
                return rsa.VerifyData(
                    Encoding.UTF8.GetBytes(payload),
                    sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  التنسيق — بيتحمّل اللصق من أي مكان
        // ══════════════════════════════════════════════════════════

        /// <summary>Base64 عادي → Base64URL مقسّم لمجموعات.</summary>
        public static string Group(string rawBase64)
        {
            string b64url = rawBase64
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            var sb = new StringBuilder(b64url.Length + b64url.Length / GroupSize);
            for (int i = 0; i < b64url.Length; i += GroupSize)
            {
                if (i > 0) sb.Append(GroupSeparator);
                sb.Append(b64url, i, Math.Min(GroupSize, b64url.Length - i));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parse متسامح: بتتجاهل أي حاجة مش من الـ Base64URL (مسافات،
        /// فواصل، سطور جديدة، حروف غلط في اللصق) — عشان المستخدم ما يشتتش
        /// على المفتاح لو نسخه من WhatsApp مثلاً.
        /// </summary>
        public static byte[]? Ungroup(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            var sb = new StringBuilder(key.Length);
            foreach (char c in key)
            {
                if (char.IsAsciiLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_')
                    sb.Append(c);
            }

            string b64 = sb.ToString().Replace('-', '+').Replace('_', '/');
            while (b64.Length % 4 != 0) b64 += '=';

            try { return Convert.FromBase64String(b64); }
            catch (FormatException) { return null; }
        }

        /// <summary>توقيع النص الخام بمفتاح خاص — للاختبارات.</summary>
        internal static string SignRaw(string payload, RSA privateKey)
            => Group(Convert.ToBase64String(privateKey.SignData(
                Encoding.UTF8.GetBytes(payload),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)));

        /// <summary>يقسّم المفتاح إلى (تاريخ الانتهاء، التوقيع).</summary>
        public static bool TrySplitKey(string key, out string expiryCode, out string signature)
        {
            expiryCode = "";
            signature = "";

            if (string.IsNullOrWhiteSpace(key)) return false;

            int idx = key.IndexOf(ExpirySeparator);
            if (idx <= 0 || idx == key.Length - 1) return false;

            expiryCode = key[..idx].Trim().ToUpperInvariant();
            signature = key[(idx + 1)..].Trim();
            return expiryCode.Length > 0 && signature.Length > 0;
        }

        /// <summary>يحوّل كود التاريخ (yyyyMMdd أو PERMANENT) لنص معروض.</summary>
        public static string FormatExpiryForDisplay(string expiryCode)
        {
            if (expiryCode.Equals(Permanent, StringComparison.OrdinalIgnoreCase))
                return Permanent;

            return DateTime.TryParseExact(expiryCode, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out DateTime d)
                ? d.ToString("yyyy-MM-dd")
                : "INVALID";
        }
    }
}
