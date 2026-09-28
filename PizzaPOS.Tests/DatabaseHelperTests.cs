using System;
using System.IO;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// busy_timeout — ليه الـ PRAGMA أصلاً.
    ///
    /// البرنامج بيفتح connection جديد في كل عملية (AppDbContext،
    /// InventoryService، UserService، BackupService) وكلهم بيكتبوا على
    /// نفس الملف. من غير timeout، أي حاجة ماسكت الملف لثانية (نسخ احتياطي،
    /// DB Browser، مزامنة OneDrive، antivirus) بتخلي العملية تفشل فوراً
    /// بـ SQLITE_BUSY.
    ///
        /// واللي اكتشفناه وخلّى الموضوع ده أهم من مجرد تحسين: الـ
        /// Default Timeout في connection string مش بيظبط busy_timeout أصلاً.
        /// الاختبار الأول below بيثبّت ده.
    /// </summary>
    public class DatabaseHelperTests : IDisposable
    {
        readonly string _root;
        readonly string _dbPath;
        readonly string _cs;

        public DatabaseHelperTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "pizzapos-dbhelp", Guid.NewGuid().ToString("N"));
            _dbPath = Path.Combine(_root, "pos.db");
            Directory.CreateDirectory(_root);
            _cs = DatabaseHelper.CSFor(_dbPath);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
            catch { }
        }

        static int ReadBusyTimeout(SqliteConnection c)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = "PRAGMA busy_timeout;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        [Fact]
        public void WithoutConfigure_BusyTimeoutIsZero()
        {
            // ده الـ regression الأساسي. لو الـ connection string وحده كان
            // بيظبط الـ timeout، الـ test ده بيفشل ويقولنا إن
            // Configure() redundant — أو إن في حاجة غيرها ماشية غلط.
            using var c = new SqliteConnection(_cs);
            c.Open();

            Assert.Equal(0, ReadBusyTimeout(c));
        }

        [Fact]
        public void Configure_SetsBusyTimeout()
        {
            using var c = new SqliteConnection(_cs);
            c.Open();
            DatabaseHelper.Configure(c);

            Assert.Equal(DatabaseHelper.BusyTimeoutMs, ReadBusyTimeout(c));
        }

        [Fact]
        public void Configure_IsSafeOnUnopenedConnection()
        {
            // Configure بينادى في catch blocks، وممكن ييجي على connection
            // لسه مفتوحش. لازم تبتلع الاستثناء بدل ما ترميه،
            // عشان ما نكسرش تنظيف connection في catch.
            using var c = new SqliteConnection(_cs);
            c.Open();

            var ex = Record.Exception(() => DatabaseHelper.Configure(c));

            Assert.Null(ex);
        }

        [Fact]
        public void Configure_AppliesPerConnectionNotOncePerProcess()
        {
            // الـ PRAGMA في SQLite على الـ connection الواحد بس. لازم
            // كل connection جديد يطلع مطبَّق — مش يتظبط مرّة واحدة
            // ويتسابى على الباقي.
            using var first = new SqliteConnection(_cs);
            first.Open();
            DatabaseHelper.Configure(first);

            using var second = new SqliteConnection(_cs);
            second.Open();

            Assert.Equal(0, ReadBusyTimeout(second));      // جديد = مطبَّق لأوّل مرة
            DatabaseHelper.Configure(second);
            Assert.Equal(DatabaseHelper.BusyTimeoutMs, ReadBusyTimeout(second));
        }
    }
}
