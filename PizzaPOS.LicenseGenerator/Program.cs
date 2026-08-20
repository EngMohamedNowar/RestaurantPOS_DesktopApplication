using System;
using PizzaPOS.Services;

namespace PizzaPOS
{
    class LicenseGenerator
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔═══════════════════════════════╗");
            Console.WriteLine("║    NAPOLI Pizza - License     ║");
            Console.WriteLine("╚═══════════════════════════════╝");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Enter Hardware ID (from activation screen): ");
                string hwid = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(hwid))
                {
                    Console.WriteLine("Empty HWID, try again.");
                    continue;
                }

                string key = LicenseManager.GenerateKey(hwid);

                Console.WriteLine();
                Console.WriteLine("License Type:");
                Console.WriteLine("  1) Permanent (forever)");
                Console.WriteLine("  2) Trial (30 days)");
                Console.Write("Choose (1 or 2): ");
                string choice = Console.ReadLine()?.Trim();

                string typeLabel;
                if (choice == "2")
                    typeLabel = "TRIAL (30 days)";
                else
                    typeLabel = "PERMANENT";

                Console.WriteLine();
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"  License Key : {key}");
                Console.WriteLine($"  Type        : {typeLabel}");
                Console.WriteLine($"  Hardware ID : {hwid}");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine();

                Console.Write("Generate another key? (y/n): ");
                string again = Console.ReadLine()?.Trim().ToLower();
                if (again != "y" && again != "yes") break;

                Console.WriteLine();
            }
        }
    }
}
