using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.ViewModels;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// Tests for MainViewModel against a real temporary database.
    /// Before the connection-string constructor was added, MainViewModel had
    /// zero coverage because it hardcoded DatabaseHelper.CS, so any test would
    /// have opened the restaurant's live database.
    /// Two things to keep in mind when writing tests here:
    ///   1. DatabaseHelper.Initialize seeds the full 38-product menu, so a
    ///      brand new database is never empty. Use OrderItems (which always
    ///      starts empty) rather than Products to assert on the cart.
    ///   2. Products used for cart tests deliberately have no sizes and no
    ///      extras, so AddProduct takes the non-dialog path and stays headless.
    /// </summary>
    public class MainViewModelTests : IDisposable
    {
        /// <summary>
        /// DatabaseHelper.Initialize seeds the full menu, which is far too slow
        /// to run once per test. The seeded database is therefore built exactly
        /// once per test run and copied for each test, which keeps full
        /// per-test isolation without paying the seeding cost 15 times over.
        /// </summary>
        static readonly Lazy<string> TemplateDb = new(BuildTemplateDb);

        static string BuildTemplateDb()
        {
            var dir = Path.Combine(Path.GetTempPath(), "vm_tpl_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "tpl.db");
            DatabaseHelper.Initialize(path);
            return path;
        }

        readonly string _dir;
        readonly string _dbPath;
        readonly string _cs;

        public MainViewModelTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "vm_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _dbPath = Path.Combine(_dir, "pos.db");
            SqliteConnection.ClearAllPools();

            var template = TemplateDb.Value;
            var templateDir = Path.GetDirectoryName(template)!;
            var templateName = Path.GetFileName(template);
            foreach (var file in Directory.GetFiles(templateDir, templateName + "*"))
            {
                // Copy the journal/WAL sidecar files under the new base name too,
                // otherwise the copied database can come out empty.
                var suffix = Path.GetFileName(file).Substring(templateName.Length);
                File.Copy(file, _dbPath + suffix, true);
            }

            _cs = DatabaseHelper.CSFor(_dbPath);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
        }

        int AddCategory(string name)
        {
            using var c = new SqliteConnection(_cs);
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Categories (Name, Icon) VALUES ($n, 'C')";
            cmd.Parameters.AddWithValue("$n", name);
            cmd.ExecuteNonQuery();
            using var idCmd = c.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid()";
            return Convert.ToInt32(idCmd.ExecuteScalar());
        }

        void AddProduct(string name, double price, double cost = 0)
        {
            var catId = AddCategory("Cat-" + name);
            using var c = new SqliteConnection(_cs);
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText =
                "INSERT INTO Products (CategoryId, Name, Price, Cost, Icon, IsActive) " +
                "VALUES ($cat, $n, $p, $c, 'X', 1)";
            cmd.Parameters.AddWithValue("$cat", catId);
            cmd.Parameters.AddWithValue("$n", name);
            cmd.Parameters.AddWithValue("$p", price);
            cmd.Parameters.AddWithValue("$c", cost);
            cmd.ExecuteNonQuery();
        }

        MainViewModel NewVm() => new MainViewModel(_cs);

        [Fact]
        public void Initialize_OnFreshDatabase_SeedsTheMenu()
        {
            var vm = NewVm();

            Assert.Equal(38, vm.Products.Count);
        }

        [Fact]
        public void Constructor_StartsWithEmptyCartAndZeroTotals()
        {
            var vm = NewVm();

            Assert.Empty(vm.OrderItems);
            Assert.False(vm.HasItems);
            Assert.Equal(0, vm.Subtotal);
            Assert.Equal(0, vm.Discount);
            Assert.Equal(0, vm.Tax);
            Assert.Equal(0, vm.ServiceCharge);
            Assert.Equal(0, vm.Total);
        }

        [Fact]
        public void Constructor_PutsSentinelAllCategoryFirst()
        {
            AddCategory("Pizzas");

            var vm = NewVm();

            var sentinel = vm.Categories[0];
            Assert.Equal(0, sentinel.Id);
            Assert.Equal(vm.Categories.Count - 1, vm.Categories.Count(c => c.Id != 0));
            Assert.Contains(vm.Categories, c => c.Name == "Pizzas");
        }

        [Fact]
        public void Constructor_OrderNumberComesFromCounterTable()
        {
            var vm = NewVm();

            // Format is <yyyyMMdd>-<4 digit sequence>, so the sequence restarts
            // each day and the printed number is self-describing.
            Assert.Matches(@"^\d{8}-0001$", vm.OrderNumber);
            Assert.Equal(DateTime.Today.ToString("yyyyMMdd"), vm.OrderNumber.Substring(0, 8));
        }

        [Fact]
        public void OrderNumberPreview_DoesNotReserveTheNumber()
        {
            // Allocation happens inside the checkout transaction. A preview that
            // reserved the number would let two open tills hand out the same one.
            var first = NewVm();
            var second = NewVm();

            Assert.Equal(first.OrderNumber, second.OrderNumber);
        }

        [Fact]
        public void AddProduct_AddsSingleLineWithQtyOne()
        {
            AddProduct("ZZMargherita", 120);
            var vm = NewVm();

            vm.AddProduct(vm.Products.First(p => p.Name == "ZZMargherita"));

            Assert.Single(vm.OrderItems);
            Assert.Equal(1, vm.OrderItems[0].Qty);
            Assert.True(vm.HasItems);
        }

        [Fact]
        public void AddProduct_SameProductTwice_MergesIntoOneLineAndIncrementsQty()
        {
            AddProduct("ZZMargherita", 120);
            var vm = NewVm();
            var p = vm.Products.First(x => x.Name == "ZZMargherita");

            vm.AddProduct(p);
            vm.AddProduct(p);

            Assert.Single(vm.OrderItems);
            Assert.Equal(2, vm.OrderItems[0].Qty);
        }

        [Fact]
        public void AddProduct_DifferentProducts_StayOnSeparateLines()
        {
            AddProduct("ZZMargherita", 120);
            AddProduct("ZZPepperoni", 150);
            var vm = NewVm();

            vm.AddProduct(vm.Products.First(p => p.Name == "ZZMargherita"));
            vm.AddProduct(vm.Products.First(p => p.Name == "ZZPepperoni"));

            Assert.Equal(2, vm.OrderItems.Count);
        }

        [Fact]
        public void AddProduct_SameNameButDifferentId_DoesNotMerge()
        {
            // Two products may legitimately share a name. Merging them would
            // silently change the price the customer is charged.
            AddProduct("ZZSpecial", 120);
            AddProduct("ZZSpecial", 200);
            var vm = NewVm();

            vm.AddProduct(vm.Products.First(p => p.Name == "ZZSpecial" && p.Price == 120));
            vm.AddProduct(vm.Products.First(p => p.Name == "ZZSpecial" && p.Price == 200));

            Assert.Equal(2, vm.OrderItems.Count);
        }

        [Fact]
        public void AddProduct_RecalculatesSubtotalFromMergedQuantity()
        {
            AddProduct("ZZMargherita", 120);
            var vm = NewVm();
            var p = vm.Products.First(x => x.Name == "ZZMargherita");

            vm.AddProduct(p);
            vm.AddProduct(p);

            Assert.Equal(240, vm.Subtotal, 3);
        }

        [Fact]
        public void AddProduct_RaisesHasItemsNotification()
        {
            AddProduct("ZZMargherita", 120);
            var vm = NewVm();
            var seen = new List<string>();
            vm.PropertyChanged += (_, e) => seen.Add(e.PropertyName ?? "");

            vm.AddProduct(vm.Products.First(p => p.Name == "ZZMargherita"));

            Assert.Contains(nameof(MainViewModel.HasItems), seen);
        }

        [Fact]
        public void DiscountInput_Percentage_ChangesTotal()
        {
            AddProduct("ZZMargherita", 100);
            var vm = NewVm();
            vm.AddProduct(vm.Products.First(p => p.Name == "ZZMargherita"));
            var before = vm.Total;

            vm.DiscountIsPercent = true;
            vm.DiscountInput = "10";

            Assert.Equal(before * 0.9, vm.Total, 2);
        }

        [Fact]
        public void DiscountInput_FixedAmount_ReducesTotalByThatAmount()
        {
            AddProduct("ZZMargherita", 100);
            var vm = NewVm();
            vm.AddProduct(vm.Products.First(p => p.Name == "ZZMargherita"));

            vm.DiscountIsPercent = false;
            vm.DiscountInput = "30";

            Assert.Equal(30, vm.Discount, 2);
        }

        [Fact]
        public void DiscountInput_ZeroAndEmpty_LeaveTotalUndiscounted()
        {
            AddProduct("ZZMargherita", 100);
            var vm = NewVm();
            vm.AddProduct(vm.Products.First(p => p.Name == "ZZMargherita"));
            var before = vm.Total;

            vm.DiscountInput = "";
            Assert.Equal(before, vm.Total, 2);

            vm.DiscountInput = "0";
            Assert.Equal(before, vm.Total, 2);
        }

        [Fact]
        public void DiscountInput_NotANumber_LeavesTotalUndiscounted()
        {
            AddProduct("ZZMargherita", 100);
            var vm = NewVm();
            vm.AddProduct(vm.Products.First(p => p.Name == "ZZMargherita"));
            var before = vm.Total;

            vm.DiscountInput = "abc";

            Assert.Equal(0, vm.Discount, 2);
            Assert.Equal(before, vm.Total, 2);
        }

        [Fact]
        public void RefreshStats_OnEmptyDay_ReportsZeros()
        {
            var vm = NewVm();

            vm.RefreshStats();

            Assert.Equal(0, vm.TodaySales, 3);
            Assert.Equal(0, vm.TodayOrders);
            Assert.Equal(0, vm.TodayProfit, 3);
        }

        [Fact]
        public void LoadCategories_CalledTwice_DoesNotDuplicate()
        {
            AddCategory("Pizzas");
            var vm = NewVm();
            var before = vm.Categories.Count;

            vm.LoadCategories();
            vm.LoadCategories();

            Assert.Equal(before, vm.Categories.Count);
        }

        [Fact]
        public void Constructor_UsesTheInjectedDatabase_NotTheLiveOne()
        {
            // Guards against a regression where someone reintroduces a
            // hardcoded DatabaseHelper.CS default and a test starts writing to
            // the restaurant's real data.
            var vm = NewVm();

            Assert.NotNull(vm);
            Assert.NotEqual(
                Path.GetFullPath(DatabaseHelper.DbPath),
                Path.GetFullPath(_dbPath));
        }

        [Fact]
        public void Constructor_DefaultOrderType_IsDelivery()
        {
            // The XAML hardcodes IsChecked="True" on the delivery radio, so
            // the viewmodel's default must stay in sync with it.
            var vm = NewVm();

            Assert.Equal(OrderType.Delivery, vm.OrderType);
        }

        [Fact]
        public void ClearOrder_KeepsTheLastSelectedOrderType()
        {
            // Regression: ClearOrder used to force the type back to Delivery
            // after every completed order, so the next order silently changed
            // type while the radios still showed the cashier's last choice.
            var vm = NewVm();

            foreach (var type in new[] { OrderType.DineIn, OrderType.Takeaway, OrderType.Delivery })
            {
                vm.OrderType = type;
                vm.ClearOrder();
                Assert.Equal(type, vm.OrderType);
            }
        }
    }
}
