using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PizzaPOS.Services
{
    public static class HardwareId
    {
        static string RunWmic(string query)
        {
            try
            {
                var psi = new ProcessStartInfo("wmic", query)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                return output.Trim();
            }
            catch { return ""; }
        }

        static string GetCpuId()
        {
            string raw = RunWmic("cpu get ProcessorId");
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 ? lines[1].Trim() : "";
        }

        static string GetMotherboardSerial()
        {
            string raw = RunWmic("baseboard get SerialNumber");
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 ? lines[1].Trim() : "";
        }

        static string GetDiskSerial()
        {
            string raw = RunWmic("diskdrive get SerialNumber");
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 ? lines[1].Trim() : "";
        }

        public static string Generate()
        {
            string cpu = GetCpuId();
            string mb = GetMotherboardSerial();
            string disk = GetDiskSerial();

            string combined = $"{cpu}|{mb}|{disk}";

            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToHexString(hash).ToUpperInvariant();
        }

        public static string GetShortId()
        {
            string full = Generate();
            return full.Length >= 16 ? full.Substring(0, 16) : full;
        }
    }
}
