using System;
using System.IO;
using System.Linq;

namespace PizzaPOS.Services
{
    public static class BackupService
    {
        static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static readonly string BaseDir = Path.Combine(AppData, "PizzaPOS");
        static readonly string DbPath = Path.Combine(BaseDir, "pos.db");
        static readonly string BackupDir = Path.Combine(BaseDir, "backups");

        const int MaxBackups = 10;

        public static void CreateBackup()
        {
            try
            {
                if (!File.Exists(DbPath)) return;

                Directory.CreateDirectory(BackupDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string backupFile = Path.Combine(BackupDir, $"pos_{timestamp}.db");

                File.Copy(DbPath, backupFile, true);

                CleanupOldBackups();
            }
            catch
            {
            }
        }

        static void CleanupOldBackups()
        {
            try
            {
                if (!Directory.Exists(BackupDir)) return;

                var files = new DirectoryInfo(BackupDir)
                    .GetFiles("pos_*.db")
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToArray();

                foreach (var file in files.Skip(MaxBackups))
                {
                    try { file.Delete(); }
                    catch { }
                }
            }
            catch { }
        }

        public static string GetLatestBackup()
        {
            try
            {
                if (!Directory.Exists(BackupDir)) return null;

                var latest = new DirectoryInfo(BackupDir)
                    .GetFiles("pos_*.db")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                return latest?.FullName;
            }
            catch { return null; }
        }

        public static int GetBackupCount()
        {
            try
            {
                if (!Directory.Exists(BackupDir)) return 0;
                return new DirectoryInfo(BackupDir).GetFiles("pos_*.db").Length;
            }
            catch { return 0; }
        }

        public static bool RestoreBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath)) return false;

                string restoreDir = Path.Combine(BaseDir, "restore");
                Directory.CreateDirectory(restoreDir);
                string restorePath = Path.Combine(restoreDir, "pos.db");

                File.Copy(backupPath, restorePath, true);
                return true;
            }
            catch { return false; }
        }
    }
}
