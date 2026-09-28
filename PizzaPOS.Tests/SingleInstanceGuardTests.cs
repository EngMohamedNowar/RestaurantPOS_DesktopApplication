using System;
using System.Threading;
using System.Threading.Tasks;
using PizzaPOS.Services;
using Xunit;

namespace PizzaPOS.Tests
{
    public class SingleInstanceGuardTests
    {
        [Fact]
        public void FirstGuard_IsFirstInstance()
        {
            using var guard = new SingleInstanceGuard();
            Assert.True(guard.IsFirstInstance);
        }

        [Fact]
        public void SecondGuardFromAnotherThread_IsNotFirstInstance()
        {
            // الـ Mutex reentrant: نفس الـ thread لو ماسكه وندى WaitOne
            // تاني بيرجع true. عشان كده الاختبار لازم يشتغل من thread
            // تاني — وده بالظبط سيناريو النسخة التانية اللي في process
            // تاني (كل process له threads الخاصة).
            using var first = new SingleInstanceGuard();
            Assert.True(first.IsFirstInstance);

            bool secondIsFirst = true;
            var t = new Thread(() => secondIsFirst = new SingleInstanceGuard().IsFirstInstance);
            t.Start();
            t.Join();

            Assert.False(secondIsFirst);
        }

        [Fact]
        public void SecondGuardDoesNotReleaseTheFirstOnDispose()
        {
            // لو النسخة التانية عملت ReleaseMutex وهي مش مالكة،
            // النسخة الأولى هتفقد الحماية من غير ما تحس.
            using var first = new SingleInstanceGuard();
            Assert.True(first.IsFirstInstance);

            var secondIsFirst = true;
            var t = new Thread(() =>
            {
                using var second = new SingleInstanceGuard();
                secondIsFirst = second.IsFirstInstance;
            });
            t.Start();
            t.Join();

            Assert.False(secondIsFirst);

            // القفل لسه ماسكه "first" — النسخة الجاية من thread تاني
            // لازم تترفض.
            bool thirdIsFirst = true;
            var t2 = new Thread(() => thirdIsFirst = new SingleInstanceGuard().IsFirstInstance);
            t2.Start();
            t2.Join();

            Assert.False(thirdIsFirst);
        }

        [Fact]
        public void GuardReleased_AllowsNextInstance()
        {
            var first = new SingleInstanceGuard();
            Assert.True(first.IsFirstInstance);
            first.Dispose();

            using var next = new SingleInstanceGuard();
            Assert.True(next.IsFirstInstance);
        }



        [Fact]
        public void Dispose_IsIdempotent()
        {
            var guard = new SingleInstanceGuard();
            guard.Dispose();
            guard.Dispose();       // مينفعش يرمي

            using var next = new SingleInstanceGuard();
            Assert.True(next.IsFirstInstance);
        }

        [Fact]
        public void MutexName_IsStable()
        {
            // لو الـ const ده اتغيّر، النسخ القديمة والجديدة مش هيمنعوا
            // بعض، والمستخدم هيمتحمي نص نص.
            Assert.Equal(@"Global\PizzaPOS-SingleInstance", SingleInstanceGuard.MutexName);
        }


    }
}
