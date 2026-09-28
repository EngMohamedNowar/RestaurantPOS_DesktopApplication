using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Text;                 // مهم
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PizzaPOS.Data;
using PizzaPOS.Services;
using PizzaPOS.Views;

namespace PizzaPOS
{
    // يحوّل SolidColorBrush → Color عشان نستخدمه في DropShadowEffect.Color
    // (الـ Freezable مش بيرث DataContext من الشجرة، فبنربط على الـ Brush مباشرة)
    public class BrushToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush) return brush.Color;
            if (value is Color color) return color;
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ScrollBar الرأسي لازم IsDirectionReversed=True عشان اتجاه السكرول ما ينعكسش
    public class OrientationToReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Orientation o && o == Orientation.Vertical;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public partial class App : Application
    {
        /// <summary>
        /// قفل النسخة الواحدة. لازم يفضل محفوظ طول عمر البرنامج —
        /// لو اتقفل بالغلط، النسخة التانية بتعدّي والقفل شغال.
        /// </summary>
        SingleInstanceGuard? _instanceGuard;

        protected override void OnStartup(StartupEventArgs e)
        {
            // ── قفل النسخة الواحدة ─────────────────────
            //
            // البرنامج بيتعامل مع ملف SQLite واحد (journal_mode=WAL).
            // نسختين في نفس الوقت = قفل على pos.db-wal، وممكن
            // lost update في المخزون أو المبيعات.
            _instanceGuard = new SingleInstanceGuard();

            if (!_instanceGuard.IsFirstInstance)

            {
                MessageBox.Show(
                    "البرنامج شغال بالفعل.\n\nاقفل النسخة التانية الأول.",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            // ── معالج الأخطاء العام ───────────────────
            //
            // من غير ده، أي exception في معاملة بيع بتقفل البرنامج
            // نص عملية. بنسجّله في الـ log وبنقوله يبقى شغّال — لحد
            // ما العملية تانية تحتاج الـ DB.
            DispatcherUnhandledException += OnDispatcherUnhandled;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandled;
            TaskScheduler.UnobservedTaskException += OnUnobservedTask;
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            base.OnStartup(e);
            try
            {
                if (!LicenseManager.IsActivated())
                {
                    var licenseWin = new LicenseActivationWindow();
                    if (licenseWin.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                // لازم بعد Initialize: الـ DB لازم يكون موجود ومُهيّأ (WAL مفعّل)
                // قبل ما نحاول ناخد منه نسخة. الكود القديم كان بينسخ الأول
                // فبيطلع على أول تشغيل بدون نسخة أصلاً، وحتى بعد كده بيفقد
                // آخر المعاملات موجودة في ملف -wal.
                DatabaseHelper.Initialize();

                BackupService.CreateBackup();

                var login = new LoginWindow();
                if (login.ShowDialog() != true) { Shutdown(); return; }

                var currentUser = SessionService.CurrentUser;
                if (currentUser != null && currentUser.MustChangePin)
                {
                    var pinWin = new ForceChangePinWindow(currentUser.Id, currentUser.FullName);
                    if (pinWin.ShowDialog() != true) { Shutdown(); return; }
                    currentUser.MustChangePin = false;
                }

                var inv = new InventoryService();
                var low = inv.GetLowStock();

                if (low.Count > 0)
                {
                    string items = string.Join("\n", low.Select(i => $"• {i.Name}: {i.Stock} {i.Unit}"));

                    MessageBox.Show(
                        $"⚠ تنبيه: {low.Count} صنف مخزونهم منخفض!\n\n{items}",
                        "تنبيه المخزون",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                var main = new MainWindow();
                MainWindow = main;
                main.Show();
            }
            catch (Exception ex)
            {
                ReportFatal(ex, "خطأ في التشغيل");

                MessageBox.Show(
                    ex.ToString(),
                    "خطأ في التشغيل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // فكّ القفل قبل الخروج. OnExit مش بتشتغل لو قفلنا بالـ
            // Task Manager، بس الـ Mutex بيتفكّ مع موت الـ process
            // على أي حال — السطر ده مجرد ترتيب نضيف.
            _instanceGuard?.Dispose();
            _instanceGuard = null;

            base.OnExit(e);
        }


        // ══ معالجات الأخطاء ══════════════════════════════════════════

        readonly HashSet<string> _firstChanceSeen = new();

        /// <summary>
        /// بيسجّل كل exception أول مرة(getFirstChance) عشان لو حصل
        /// exception جوّه try متوقع ومنمسكش، يبقى عندنا trace. من غير
        /// ده الـ log بيبقى فاضي والسبب الحقيقي ضايع.
        /// </summary>
        void OnFirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            try
            {
                var ex = e.Exception;
                lock (_firstChanceSeen)
                {
                    if (ex is null) return;
                    string key = ex.GetType().FullName + "|" + ex.Message;
                    if (!_firstChanceSeen.Add(key)) return;
                    if (_firstChanceSeen.Count > 200) _firstChanceSeen.Clear();

                    // INFO مش WARN عن قصد: FirstChance بيشتغل مع أي
                    // exception بيتعمله catch جوّه أي مكتبة — WPF
                    // binding، SQLite retries، XML parsing. لو سجّلنا
                    // ده على WARN، الـ log بيت drowning في ضوضة
                    // والـ WARN/ERROR الحقيقية بتضيع.
                    AppLogger.Info($"[first-chance] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
            catch { /* الـ logger نفسه لو وقع — ما نعملش exception في الـ handler */ }
        }

        void OnDispatcherUnhandled(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ReportFatal(e.Exception, "خطأ في الواجهة");
            e.Handled = true;      // متقفلش البرنامج
        }

        void OnDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ReportFatal(ex, "خطأ في الخلفية");
                return;
            }
            AppLogger.Error("[fatal] AppDomain unhandled: " + e.ExceptionObject);
        }

        void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // بيسجّل exception في Task محدش شافه. مش بنعمله rethrow
            // عشان التطبيقيكمل شغال.
            ReportFatal(e.Exception, "خطأ في مهمة");
            e.SetObserved();
        }

        static void ReportFatal(Exception ex, string context)
        {
            try
            {
                AppLogger.Error($"[{context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
            catch { }
        }
    }
}
