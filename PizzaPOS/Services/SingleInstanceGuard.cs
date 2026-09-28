using System;
using System.Threading;

namespace PizzaPOS.Services
{
    /// <summary>
    /// بيمنع تشغيل نسختين من البرنامج في نفس الوقت.
    ///
    /// البرنامج بيشتغل على ملف SQLite واحد (journal_mode=WAL). نسختين
    /// في نفس الوقت = قفل على pos.db-wal، وممكن lost update في
    /// المخزون: النسختين بتقرا رصيد صنف 5، الاتنين بيخصموا 1 ⇒ رصيد
    /// 3 بدل 4. أو أسوأ، واحدة فيهم بياخد SQLITE_BUSY في نص عملية
    /// بيع والأوردر بيضيع.
    ///
    /// الـ Mutex اسمه عامّ على الجهاز كله (Global\) عشان يشتغل حتى لو
    /// النسختين اتفتحوا من مكانين مختلفين.
    ///
    /// مهم: لازم يفضل محفوظ طول عمر البرنامج. لو اتعمل dispose
    /// بالغلط، النسخة التانية بتعدّي والبرنامج مش محمي.
    /// </summary>
    public sealed class SingleInstanceGuard : IDisposable
    {
        /// <summary>
        /// الاسم لازم يكون ثابت وموحّد بين كل النسخ. لو اتغيّر،
        /// النسخ القديمة والجديدة مش هيتمنعوا بعض.
        /// </summary>
        public const string MutexName = @"Global\PizzaPOS-SingleInstance";

        const string LocalMutexName = @"Local\PizzaPOS-SingleInstance";

        Mutex? _mutex;
        bool _ownsMutex;
        bool _disposed;

        /// <summary>true يعني إحنا النسخة الأولى والبرنامج يكمّل.</summary>
        public bool IsFirstInstance { get; }

        public SingleInstanceGuard()
        {
            // ── الـ Global\ الأساسي ───────────────────
            //
            // لازم نفرّق بين حالتين مختلفتين تماماً:
            //
            //   HeldByOther  = القفل موجود وماسكه نسخة تانية شغالة.
            //                  ده بالظبط اللي عايزين نمنعه. ممنوع
            //                  نعمل fallback هنا — لو نزلنا لـ Local\
            //                  كنا هنفتح قفل فاضي ونسريحتشغل، والحماية
            //                  بتضيع تماماً.
            //
            //   Unavailable  = مقدرناش نفتح القفل أصلاً (صلاحيات).
            //                  هنا بس الـ fallback معقول.
            var result = TryAcquire(MutexName, out _mutex);

            if (result == AcquireResult.Unavailable)
            {
                // Global\ ممكن يرمي UnauthorizedAccessException لو فيه
                // نسخة elevated تانية. بننزل لـ Local\ بدل ما نرفض
                // نشتغل خالص.
                result = TryAcquire(LocalMutexName, out _mutex);
            }

            if (result == AcquireResult.Unavailable)
            {
                // آخر خط دفاع: نشتغل من غير قفل أحسن ما التطبيق
                // ميقفلش نفسه على المستخدم.
                _mutex = null;
                _ownsMutex = false;
                IsFirstInstance = true;
                return;
            }

            if (result == AcquireResult.HeldByOther)
            {
                // مش مالكين القفل، فمفيش لازمة نسيب handle مفتوح.
                // ده مهم: أي handle مطروح بيعلي عمر الـ named object،
                // فـ handle من نسخة مرفوضة ممكن يخلّي النسخة اللي
                // بتخرج متفهمش إنها حرّت القفل.
                _mutex?.Dispose();
                _mutex = null;
                _ownsMutex = false;
                IsFirstInstance = false;
                return;
            }

            _ownsMutex = true;
            IsFirstInstance = true;
        }

        enum AcquireResult
        {
            /// <summary>خُدنا القفل — إحنا النسخة الأولى.</summary>
            Acquired,

            /// <summary>قفل عملية تانية شغال — نقفل البرنامج.</summary>
            HeldByOther,

            /// <summary>مقدرناش نقرر أصلاً (صلاحيات/خطأ) — نكمّل بدون قفل.</summary>
            Unavailable
        }

        static AcquireResult TryAcquire(string name, out Mutex? mutex)
        {
            mutex = null;
            try
            {
                // createdNew هي الإشارة الصح هنا: معناها "مفيش عملية
                // تانية عندها القفل مفتوح دلوقتي".
                //
                // ملاحظة مهمة: Mutex reentrant — لو نفس الـ thread نادى
                // WaitOne تاني على mutex ماسكه، بيرجع true وبيزوّد
                // العدّاد. عشان كده استخدمنا createdNew من الـ
                // constructor مش WaitOne: الـ wait كان هيخلي نسختين في
                // نفس العملية يفتكروا إنهم كلهم النسخة الأولى.
                //
                // لو العملية السابقة اتقتلت من غير ما تفكّ القفل، الـ
                // named object بيتدمر مع آخر handle فيه، فالـ createdNew
                // بيرجع true من جديد — يعني نسريحتشغل.
                mutex = new Mutex(initiallyOwned: true, name, out bool createdNew);
                return createdNew ? AcquireResult.Acquired : AcquireResult.HeldByOther;
            }
            catch (UnauthorizedAccessException)
            {
                // القفل موجود بس ملكه عملية أعلى صلاحية
                mutex = null;
                return AcquireResult.Unavailable;
            }
            catch (Exception)
            {
                mutex = null;
                return AcquireResult.Unavailable;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var mutex = _mutex;
            _mutex = null;
            if (mutex == null) return;

            // ReleaseMutex لازم ينادى من نفس الـ thread اللي خد
            // القفل — ده الـ ownership والـ thread مربوطين ببعض. لو اتقفل
            // البرنامج من thread تاني (أو في الاختبارات، الـ thread
            // بيرجع للـ pool) الـ Release هيرمي ApplicationException،
            // والقفل هيفضل ماسك للأبد.
            //
            // عشان كده الـ Release best-effort، والـ Dispose (قفل
            // الـ handle) هو اللي بينظّف فعليًا: لو كنا آخر handle
            // على الـ named object، غلقه بيهدمه ويفكّ الملكية.
            try
            {
                if (_ownsMutex) mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // مش مالكين القفل من الـ thread ده — مقصود، هنكمل.
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _ownsMutex = false;
                try { mutex.Dispose(); } catch { }
            }
        }
    }
}