using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PizzaPOS.Services
{
    public static class LicenseManager
    {
        static readonly string LicenseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PizzaPOS");

        static readonly string LicenseFile = Path.Combine(LicenseDir, "license.dat");

        static readonly byte[] SecretSalt = Encoding.UTF8.GetBytes("NAPOLI-PIZZA-2025-SECRET-KEY!!");

        public static string GenerateKey(string hwid)
        {
            using var hmac = new HMACSHA256(SecretSalt);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(hwid));

            string s1 = Convert.ToHexString(hash, 0, 2).ToUpperInvariant();
            string s2 = Convert.ToHexString(hash, 2, 2).ToUpperInvariant();
            string s3 = Convert.ToHexString(hash, 4, 2).ToUpperInvariant();
            string s4 = Convert.ToHexString(hash, 6, 2).ToUpperInvariant();

            return $"{s1}-{s2}-{s3}-{s4}";
        }

        public static bool Validate(string hwid, string key)
        {
            string expected = GenerateKey(hwid);
            return string.Equals(expected, key, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsActivated()
        {
            try
            {
                if (!File.Exists(LicenseFile)) return false;

                string[] lines = File.ReadAllLines(LicenseFile);
                if (lines.Length < 4) return false;

                string storedHwid = lines[0].Trim();
                string storedKey = lines[1].Trim();
                string expiryStr = lines[3].Trim();

                string currentHwid = HardwareId.GetShortId();
                if (storedHwid != currentHwid) return false;

                if (!Validate(currentHwid, storedKey)) return false;

                if (expiryStr != "PERMANENT")
                {
                    if (DateTime.TryParse(expiryStr, out DateTime expiry))
                    {
                        if (DateTime.Now > expiry) return false;
                    }
                }

                return true;
            }
            catch { return false; }
        }

        public static bool Activate(string key, int? days = null)
        {
            try
            {
                string hwid = HardwareId.GetShortId();

                if (!Validate(hwid, key)) return false;

                string expiryLine = "PERMANENT";
                if (days.HasValue && days.Value > 0)
                {
                    expiryLine = DateTime.Now.AddDays(days.Value).ToString("yyyy-MM-dd");
                }

                Directory.CreateDirectory(LicenseDir);
                File.WriteAllLines(LicenseFile, new[]
                {
                    hwid,
                    key,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    expiryLine
                });
                return true;
            }
            catch { return false; }
        }

        public static string GetExpiryInfo()
        {
            try
            {
                if (!File.Exists(LicenseFile)) return "NO_LICENSE";

                string[] lines = File.ReadAllLines(LicenseFile);
                if (lines.Length < 4) return "NO_LICENSE";

                string expiryStr = lines[3].Trim();

                if (expiryStr == "PERMANENT") return "PERMANENT";

                if (DateTime.TryParse(expiryStr, out DateTime expiry))
                {
                    TimeSpan remaining = expiry - DateTime.Now;
                    if (remaining.TotalDays <= 0) return "EXPIRED";
                    if (remaining.TotalDays <= 7)
                        return $" expiresIn {remaining.Days}d {remaining.Hours}h";
                    return $" expiresIn {remaining.Days} days";
                }

                return "UNKNOWN";
            }
            catch { return "ERROR"; }
        }

        public static void Deactivate()
        {
            try { if (File.Exists(LicenseFile)) File.Delete(LicenseFile); }
            catch { }
        }

        public static string GetHwid() => HardwareId.GetShortId();
    }
}
