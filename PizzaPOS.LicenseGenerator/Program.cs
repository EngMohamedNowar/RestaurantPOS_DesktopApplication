using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PizzaPOS.Services;

namespace PizzaPOS.LicenseGenerator
{
    /// <summary>
    /// أداة المورّد — بتوقّع التراخيص.
    ///
    /// دي الأداة الوحيدة اللي فيها المفتاح الخاص. البرنامج نفسه مبيقدرش
    /// يوقّع ولا يولّد مفاتيح، فالعميل لو حد فتح سورسه بالكامل مش هيعرف
    /// يصمّم ترخيص صالح.
    ///
    /// المفتاح الخاص بيتخزّن في:
    ///   %AppData%\PizzaPOS.LicenseGenerator\license-signing.key
    /// وده ملف PEM عادي. لو ضاع أو اتمسح، كل التراخيص اللي اتوقّعت بيه
    /// تبطل للأبد — مفيش استرجاع. فـ خد نسخة احتياطية منه بعيد عن الجهاز.
    ///
    /// تشغيل:
    ///   dotnet run --project PizzaPOS.LicenseGenerator -- <HWID> [أيام]
    ///   Hwid = رقم الجهاز اللي طلب التفعيل (16 محرف)
    ///   أيام  = عدد الأيام من النهاردة (0 أو PERMANENT = دائم)
    ///
    ///   مثال:  dotnet run --project PizzaPOS.LicenseGenerator -- 1A2B3C4D5E6F7890 30
    ///   دائم:  dotnet run --project PizzaPOS.LicenseGenerator -- 1A2B3C4D5E6F7890 PERMANENT
    /// </summary>
    public static class Program
    {
        static readonly string KeyDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PizzaPOS.LicenseGenerator");

        static readonly string KeyFile = Path.Combine(KeyDir, "license-signing.key");

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== PizzaPOS License Generator ===");
            Console.WriteLine();

            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string hwid = args[0].Trim();
            if (hwid.Length == 0)
            {
                Console.WriteLine("[X] HWID فاضي.");
                return 1;
            }

            if (!IsValidHwidShape(hwid))
            {
                Console.WriteLine($"[X] HWID غير صالح: '{hwid}'");
                Console.WriteLine("    المفروض 16 محرف hex (0-9, A-F).");
                return 1;
            }

            string duration = args.Length > 1 ? args[1].Trim() : "30";

            RSA rsa;
            try
            {
                rsa = LoadOrCreateSigningKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[X] مشكلة في المفتاح: {ex.Message}");
                return 1;
            }

            using (rsa)
            {
                if (!MatchesCompiledPublicKey(rsa))
                {
                    Console.WriteLine("[X] المفتاح-loaded لا يطابق المفتاح العام المركّب في البرنامج.");
                    Console.WriteLine("    (= المفتاح العام في LicenseSignature.PublicKeyPem)");
                    Console.WriteLine("    لو أنت بتعمل pair جديد، حدّث الـ PEM في الكود وأعد البناء أولاً.");
                    return 1;
                }

                if (!TryResolveExpiry(duration, out string expiryCode, out DateTime? expiresOn))
                {
                    Console.WriteLine($"[X] مدة غير صالحة: '{duration}'");
                    return 1;
                }

                string key = LicenseSignature.BuildKey(rsa, hwid.ToUpperInvariant(), expiryCode);

                Console.WriteLine($"  HWID      : {hwid.ToUpperInvariant()}");
                Console.WriteLine($"  Expires   : {expiryCode}"
                    + (expiresOn.HasValue ? $"  ({expiresOn:yyyy-MM-dd})" : ""));
                Console.WriteLine($"  Key length: {key.Length} محرف");
                Console.WriteLine();
                Console.WriteLine("  ── انسخ السطر ده كله وابعته للعميل ──");
                Console.WriteLine();
                Console.WriteLine(key);
                Console.WriteLine();
                Console.WriteLine("  ───────────────────────────────────────");
                Console.WriteLine();

                if (expiresOn.HasValue)
                {
                    TimeSpan left = expiresOn.Value.Date.AddDays(1) - DateTime.Today;
                    Console.WriteLine($"  [!] ينتهي بعد {left.Days} يوم. العميل بيتفشل بعد منتصف ليل يوم الانتهاء.");
                }
                else
                {
                    Console.WriteLine("  [!] ده ترخيص دائم.");
                }
            }

            return 0;
        }

        // ══════════════════════════════════════════════════════════
        //  المفتاح الخاص
        // ══════════════════════════════════════════════════════════

        static RSA LoadOrCreateSigningKey()
        {
            if (File.Exists(KeyFile))
            {
                Console.WriteLine($"[*] المفتاح الخاص موجود: {KeyFile}");
                RSA loaded = RSA.Create();
                loaded.ImportFromPem(File.ReadAllText(KeyFile));
                return loaded;
            }

            Console.WriteLine("[!] مفيش مفتاح خاص — بنعمل واحد جديد (مرة واحدة بس).");

            RSA created = RSA.Create(2048);
            Directory.CreateDirectory(KeyDir);
            File.WriteAllText(KeyFile, created.ExportPkcs8PrivateKeyPem());

            Console.WriteLine($"[*] اتحفظ في: {KeyFile}");
            Console.WriteLine();
            Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
            Console.WriteLine("  ║  خد نسخة احتياطية من الملف ده دلوقتي              ║");
            Console.WriteLine("  ║  لو ضاع، كل التراخيص القديمة تبطل للأبد.           ║");
            Console.WriteLine("  ║  ومتحطوش في git ولا على جهاز العميل.              ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("  الـ Public Key المقابل (للتحقق منه مقابل الكود):");
            Console.WriteLine("  ─────────────────────────────────────────────────");
            Console.WriteLine(created.ExportSubjectPublicKeyInfoPem());
            Console.WriteLine("  ─────────────────────────────────────────────────");
            Console.WriteLine("  لو ده مختلف عن PublicKeyPem في LicenseSignature.cs،");
            Console.WriteLine("  حدّث الكود هناك وأعد بناء البرنامج.");
            Console.WriteLine();

            return created;
        }

        /// <summary>يقارن المفتاح العام الخاص بـ private بالـ PEM المركّب في العميل،
        /// عشان لو اتعمل pair جديد من غير تحديث الكود، الأداة تصرخ بدل ما
        /// تولّد مفاتيح مينفعش حد يفعّل بيها.</summary>
        static bool MatchesCompiledPublicKey(RSA rsa)
        {
            try
            {
                using RSA pub = RSA.Create();
                pub.ImportFromPem(LicenseSignature.PublicKeyPem);
                return rsa.ExportSubjectPublicKeyInfo()
                    .AsSpan().SequenceEqual(pub.ExportSubjectPublicKeyInfo());
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  المدة
        // ══════════════════════════════════════════════════════════

        static bool TryResolveExpiry(string duration, out string expiryCode, out DateTime? expiresOn)
        {
            expiryCode = "";
            expiresOn = null;

            if (duration.Equals("PERMANENT", StringComparison.OrdinalIgnoreCase)
                || duration == "0")
            {
                expiryCode = LicenseSignature.Permanent;
                return true;
            }

            if (!int.TryParse(duration, out int days) || days <= 0 || days > 3650)
                return false;

            DateTime end = DateTime.Today.AddDays(days);
            expiryCode = end.ToString("yyyyMMdd");
            expiresOn = end;
            return true;
        }

        static bool IsValidHwidShape(string hwid)
        {
            if (hwid.Length != 16) return false;
            foreach (char c in hwid)
                if (!Uri.IsHexDigit(c)) return false;
            return true;
        }

        static void PrintUsage()
        {
            Console.WriteLine("الاستخدام:");
            Console.WriteLine("  PizzaPOS.LicenseGenerator <HWID> [أيام|PERMANENT]");
            Console.WriteLine();
            Console.WriteLine("  HWID    رقم الجهاز (16 محرف hex) — البرنامج بيعرضه عند أول تشغيل");
            Console.WriteLine("  أيام    عدد الأيام من النهاردة (افتراضي 30، الحد الأقصى 3650)");
            Console.WriteLine("  PERMANENT / 0   ترخيص دائم");
            Console.WriteLine();
            Console.WriteLine("أمثلة:");
            Console.WriteLine("  PizzaPOS.LicenseGenerator 1A2B3C4D5E6F7890 30");
            Console.WriteLine("  PizzaPOS.LicenseGenerator 1A2B3C4D5E6F7890 PERMANENT");
        }
    }
}
