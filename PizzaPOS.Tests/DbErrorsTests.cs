using System;
using System.Threading;
using Microsoft.Data.Sqlite;
using PizzaPOS.Services;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// DbErrors هي الطبقة اللي بتقول للكاشير إيه اللي حصل. لو غلطت في
    /// الترجمة، المستخدم هيتعامل مع رسالة غلط ومحدش هيعرف.
    /// </summary>
    public class DbErrorsTests
    {
        static SqliteException Sqlite(int rc, int extended, string message = "")
            => new SqliteException(message, rc, extended);

        [Fact]
        public void ForeignKeyViolation_IsDetected()
        {
            // SQLITE_CONSTRAINT_FOREIGNKEY = 19 / 787
            var ex = Sqlite(19, 787);
            Assert.True(DbErrors.IsForeignKeyViolation(ex));
            Assert.False(DbErrors.IsBusy(ex));
        }

        [Fact]
        public void ForeignKeyViolation_IsAlsoDetectedFromTheMessage()
        {
            // بعض المسارات بتعدّي رسالة من غير كود مظبوط
            var ex = Sqlite(19, 0, "FOREIGN KEY constraint failed");
            Assert.True(DbErrors.IsForeignKeyViolation(ex));
        }

        [Fact]
        public void OtherConstraints_AreNotTreatedAsForeignKey()
        {
            Assert.False(DbErrors.IsForeignKeyViolation(Sqlite(19, 2067)));  // UNIQUE
            Assert.False(DbErrors.IsForeignKeyViolation(Sqlite(19, 1299)));  // NOT NULL
            Assert.False(DbErrors.IsForeignKeyViolation(new InvalidOperationException("x")));
        }

        [Fact]
        public void Busy_IsDetected()
        {
            Assert.True(DbErrors.IsBusy(Sqlite(5, 0)));                      // SQLITE_BUSY
            Assert.True(DbErrors.IsBusy(Sqlite(5, 261)));                   // SQLITE_BUSY_SNAPSHOT
            Assert.True(DbErrors.IsBusy(Sqlite(0, 0, "database is locked")));
            Assert.False(DbErrors.IsForeignKeyViolation(Sqlite(5, 0)));
        }

        [Fact]
        public void NonSqliteExceptions_AreNeitherBusyNorForeignKey()
        {
            var ex = new InvalidOperationException("حاجة تانية");
            Assert.False(DbErrors.IsBusy(ex));
            Assert.False(DbErrors.IsForeignKeyViolation(ex));
        }

        [Fact]
        public void Describe_GivesDifferentMessagesForDifferentCauses()
        {
            string fk = DbErrors.Describe(Sqlite(19, 787));
            string busy = DbErrors.Describe(Sqlite(5, 0));
            string other = DbErrors.Describe(Sqlite(1, 1, "no such table: X"));
            string plain = DbErrors.Describe(new InvalidOperationException("boom"));

            Assert.Contains("نقدر نحذف", fk);
            Assert.Contains("مشغولة", busy);
            Assert.Contains("no such table", other);
            Assert.Contains("boom", plain);

            // كل رسالة لازم تكون مختلفة عن التانية، وإلا المستخدم هيشوف
            // نفس الكلام لأسباب مختلفة
            Assert.Equal(4, new System.Collections.Generic.HashSet<string> { fk, busy, other, plain }.Count);
        }

        [Fact]
        public void Describe_NeverLeaksTheRawEnglishExceptionToTheCashier()
        {
            // الرسالة العربية هي اللي بتظهر للمستخدم
            string fk = DbErrors.Describe(Sqlite(19, 787));
            Assert.DoesNotContain("SQLite Error", fk);
            Assert.DoesNotContain("constraint", fk.ToLowerInvariant());
        }

        // ── Retry ─────────────────────────────────────────

        [Fact]
        public void Retry_SucceedsWithoutRetryingWhenNothingIsBusy()
        {
            int calls = 0;
            int result = DbErrors.RetryOnBusy(() => { calls++; return 42; });

            Assert.Equal(42, result);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Retry_RetriesBusyThenSucceeds()
        {
            int calls = 0;
            int result = DbErrors.RetryOnBusy(() =>
            {
                calls++;
                if (calls < 3) throw Sqlite(5, 0);
                return 7;
            });

            Assert.Equal(7, result);
            Assert.Equal(3, calls);
        }

        [Fact]
        public void Retry_GivesUpAfterMaxAttempts()
        {
            int calls = 0;
            Assert.Throws<SqliteException>(() =>
                DbErrors.RetryOnBusy(() => { calls++; throw Sqlite(5, 0); }, maxAttempts: 3));

            Assert.Equal(3, calls);
        }

        [Fact]
        public void Retry_DoesNotRetryNonBusyErrors()
        {
            // FK violation مش هينفع يتعاد — تاني خطأ مشTemporary
            int calls = 0;
            Assert.Throws<SqliteException>(() =>
                DbErrors.RetryOnBusy(() => { calls++; throw Sqlite(19, 787); }));

            Assert.Equal(1, calls);
        }

        [Fact]
        public void Retry_ActionOverloadWorks()
        {
            int calls = 0;
            DbErrors.RetryOnBusy(() =>
            {
                calls++;
                if (calls < 2) throw Sqlite(5, 0);
            });
            Assert.Equal(2, calls);
        }

        [Fact]
        public void Retry_RejectsNonsenseMaxAttempts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DbErrors.RetryOnBusy(() => 1, maxAttempts: 0));
        }
    }
}
