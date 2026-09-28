using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace PizzaPOS.Services
{
    /// <summary>
    /// بصمة الجهاز — بدون أي عملية خارجية.
    ///
    /// الكود القديم كان بيستدعي `wmic` عبر Process.Start. المشكلة إن wmic
    /// اتشال من Windows 11، فكل الأجهزة بترجع نفس الـ HWID (SHA256 لـ "||")
    /// وبتاخد نفس المفتاح. كمان Serial Number بتاع الهارد بيتغير لما
    /// المستخدم يغيّر الهارد.
    ///
    /// دلوقتي: بنقرأ MachineGuid من الـ registry بس. ثابت عبر تحديثات
    /// الويندوز، ومتاح من Windows 7 لـ 11، ومن غير أي fork عملية.
    /// لو الـ registry مش متاح (حالات نادرة جداً) بنستخدم ID عشوائي
    /// متخزّن محلياً — عشان ما نرجعش لنقطة إن كل الأجهزة نفس البصمة.
    /// </summary>
    public static class HardwareId
    {
        static readonly string StateDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaPOS");

        static readonly string DeviceIdFile = Path.Combine(StateDir, "device.id");

        /// <summary>
        /// MachineGuid بيتسجل مرتين: HKLM (محمي، محتاج admin) وHKCU (نسخة
        /// عادية). بنجرّب الاتنين، وكمان 32/64-bit عشان بعض الأنظمة
        /// بتخزنه في الـ view التاني.
        /// </summary>
        static readonly RegistryHive[] Hives = { RegistryHive.LocalMachine, RegistryHive.CurrentUser };

        static string ReadMachineGuid()
        {
            // مهم: MachineGuid **قيمة** اسمها MachineGuid جوه مفتاح
            // Cryptography — مش subkey. المسار الصح:
            //   HKLM\SOFTWARE\Microsoft\Cryptography   value: MachineGuid
            // (لو كتبت Cryptography\MachineGuid هترجع null وهتلف عربي).
            const string subKey = @"SOFTWARE\Microsoft\Cryptography";
            const string valueName = "MachineGuid";

            foreach (RegistryHive hive in Hives)
            {
                foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using RegistryKey? root = RegistryKey.OpenBaseKey(hive, view);
                        using RegistryKey? key = root?.OpenSubKey(subKey);
                        string? value = key?.GetValue(valueName) as string;
                        if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
                    }
                    catch
                    {
                        // بعض الـ hives محمي — نجرّب اللي بعده
                    }
                }
            }
            return "";
        }

        /// <summary>
        /// ID عشوائي بيتخزّن مرة واحدة. ده اللي يمنع الحالة الخطيرة:
        /// إن كل الأجهزة تطلع بنفس البصمة لما الـ registry يتعطّل.
        /// </summary>
        static string GetOrCreateFallbackId()
        {
            try
            {
                if (File.Exists(DeviceIdFile))
                {
                    string existing = File.ReadAllText(DeviceIdFile).Trim();
                    if (existing.Length > 0) return existing;
                }

                string created = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(StateDir);
                File.WriteAllText(DeviceIdFile, created);
                return created;
            }
            catch
            {
                // لو الكتابة فشلت، لسه بنستخدم قيمة متغيرة — أأمن من ثابت
                return Guid.NewGuid().ToString("N");
            }
        }

        /// <summary>البصمة الكاملة (SHA-256 hex، 64 محرف).</summary>
        public static string Generate()
        {
            string machineGuid = ReadMachineGuid();
            string source = string.IsNullOrWhiteSpace(machineGuid)
                ? "fallback|" + GetOrCreateFallbackId()
                : "mg|" + machineGuid;

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
            return Convert.ToHexString(hash);
        }

        /// <summary>النسخة المعروضة للمستخدم (16 محرف) — بتتحدد في الترخيص.</summary>
        public static string GetShortId()
        {
            string full = Generate();
            return full.Length >= 16 ? full[..16] : full;
        }
    }
}
