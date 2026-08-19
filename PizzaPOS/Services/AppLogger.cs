using System;
using System.IO;

namespace PizzaPOS.Services
{
    public static class AppLogger
    {
        static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PizzaPOS", "logs");

        static readonly object _lock = new();

        static AppLogger()
        {
            Directory.CreateDirectory(LogDir);
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message, Exception? ex = null)
        {
            var msg = ex != null ? $"{message}\n{ex}" : message;
            Write("ERROR", msg);
        }

        static void Write(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    var file = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                    var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}\n";
                    File.AppendAllText(file, line);
                }
            }
            catch
            {
                // لا نعمل recursion لو الـ log نفسه فشل
            }
        }
    }
}
