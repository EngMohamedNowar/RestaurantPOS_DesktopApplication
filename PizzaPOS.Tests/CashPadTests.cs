using System;
using System.Globalization;
using PizzaPOS.Services;
using Xunit;

namespace PizzaPOS.Tests
{
    public class CashPadTests
    {
        static CashPad Pad(double total = 100) => new CashPad(total);

        // ── الأساسيات ────────────────────────────────

        [Fact]
        public void NewPad_IsEmpty()
        {
            var p = Pad(250.50);

            Assert.Equal("", p.Input);
            Assert.False(p.HasValue);
            Assert.Equal(0d, p.Paid);
            // مفيش مدفوع ⇒ الواجهة بتعرض "—"، لو ضغط Enter بنسجّل الإجمالي
            Assert.Equal(250.50d, p.SettledAmount);
        }

        [Fact]
        public void PressDigits_BuildsAmount()
        {
            var p = Pad(100);
            foreach (var d in "150") p.Press(d.ToString());

            Assert.Equal("150", p.Input);
            Assert.Equal(150d, p.Paid);
            Assert.True(p.HasValue);
        }

        // ── الـ decimal guard ────────────────────────

        [Fact]
        public void DecimalPoint_AddsDot()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("2"); p.Press(".");

            Assert.Equal("12.", p.Input);
        }

        [Fact]
        public void LeadingDecimal_BecomesZeroPoint()
        {
            var p = Pad(100);
            p.Press(".");

            Assert.Equal("0.", p.Input);
        }

        [Fact]
        public void TwoDecimalPlaces_Allowed()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("2"); p.Press("."); p.Press("3"); p.Press("4");

            Assert.Equal("12.34", p.Input);
            Assert.Equal(12.34d, p.Paid);
        }

        [Fact]
        public void ThirdDecimalPlace_Blocked()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("2"); p.Press("."); p.Press("3"); p.Press("4"); p.Press("5");

            Assert.Equal("12.34", p.Input);   // الـ 5 اتهملت
            Assert.Equal(12.34d, p.Paid);
        }

        [Fact]
        public void ManyDigitsBeforeDecimal_StillAllowed()
        {
            var p = Pad(100);
            foreach (var d in "1234567") p.Press(d.ToString());

            Assert.Equal("1234567", p.Input);
        }

        [Fact]
        public void SecondDecimalPoint_Blocked()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("."); p.Press("5"); p.Press(".");

            Assert.Equal("1.5", p.Input);
        }

        [Fact]
        public void NoDecimalPoint_ManyDigits_AllAllowed()
        {
            var p = Pad(100);
            foreach (var d in "123456") p.Press(d.ToString());
            p.Press(".");   // لسه مفيش decimal

            Assert.Equal("123456.", p.Input);
        }

        // ── الـ leading zero ────────────────────────

        [Fact]
        public void LeadingZero_ReplacedNotAppended()
        {
            var p = Pad(100);
            p.Press("0"); p.Press("5");

            Assert.Equal("5", p.Input);
            Assert.Equal(5d, p.Paid);
        }

        [Fact]
        public void ZeroThenDecimalThenDigit_Works()
        {
            var p = Pad(100);
            p.Press("0"); p.Press("."); p.Press("5");

            Assert.Equal("0.5", p.Input);
            Assert.Equal(0.5d, p.Paid);
        }

        [Fact]
        public void LoneZero_CountsAsPaidZero()
        {
            // سلوك محفوظ من الـ dialog القديم (كان الحارس
            // _input != "" && _input != "0." يعني "0" ساعاتها قيمة):
            // نقدر نغيّره، بس ده مش تغيير سلوك صامت في نص إعادة هيكلة.
            var p = Pad(100);
            p.Press("0");

            Assert.Equal("0", p.Input);
            Assert.True(p.HasValue);
            Assert.Equal(0d, p.Paid);
            Assert.False(p.IsEnough);   // 0 < 100
        }

        [Fact]
        public void ZeroPoint_IsNotAValue()
        {
            var p = Pad(100);
            p.Press("0"); p.Press(".");

            Assert.False(p.HasValue);
        }

        // ── Backspace / Clear ────────────────────────

        [Fact]
        public void Backspace_RemovesLastChar()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("2"); p.Press("3");
            p.Press("B");

            Assert.Equal("12", p.Input);
        }

        [Fact]
        public void Backspace_OnEmpty_IsSafe()
        {
            var p = Pad(100);
            p.Press("B"); p.Press("B");

            Assert.Equal("", p.Input);
        }

        [Fact]
        public void Backspace_AfterDecimal_RemovesDecimal()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("2"); p.Press("."); p.Press("5");
            p.Press("B"); p.Press("B");

            Assert.Equal("12", p.Input);
        }

        [Fact]
        public void Clear_EmptiesInput()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("2"); p.Press("3");
            p.Press("C");

            Assert.Equal("", p.Input);
            Assert.False(p.HasValue);
        }

        [Fact]
        public void Clear_Method_EmptiesInput()
        {
            var p = Pad(100);
            p.Press("EXACT");
            p.Clear();

            Assert.Equal("", p.Input);
        }

        // ── EXACT ───────────────────────────────────

        [Fact]
        public void Exact_FillsTotalAsDecimalString()
        {
            var p = Pad(123.45);
            p.Press("EXACT");

            Assert.Equal("123.45", p.Input);
            Assert.Equal(123.45d, p.Paid);
            Assert.Equal(0d, p.Change);
            Assert.True(p.IsEnough);
        }

        [Fact]
        public void Exact_ZeroTotal()
        {
            var p = Pad(0);
            p.Press("EXACT");

            Assert.Equal(0d, p.Change);
            Assert.True(p.IsEnough);
        }

        [Fact]
        public void Exact_RoundTrips()
        {
            // فرق الفاصلة العشرية في الـ double (0.1+0.2) ممكن يطلع
            // إجمالي 0.30000000000000004 — EXACT لازم يملاه 0.30 بالظبط.
            var p = Pad(0.1 + 0.2);
            p.Press("EXACT");

            Assert.Equal("0.30", p.Input);
            Assert.Equal(0d, p.Change);
        }

        // ── الـ regression: culture في EXACT ────────

        [Fact]
        public void Exact_UsesDotOnCommaDecimalCulture()
        {
            // الـ bug: EXACT كان بيحط _total.ToString("F2") حسب culture
            // الجهاز ⇒ "123,45" على جهاز comma-culture. والـ guard كان
            // بيدور على '.' ⇒ حد يقدر يضيف رقم تاني والباقي يطلع غلط.
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                foreach (var culture in new[] { "de-DE", "ar-DZ", "fr-FR" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);

                    var p = Pad(123.45);
                    p.Press("EXACT");

                    Assert.Equal("123.45", p.Input);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Exact_DecimalGuardStillWorks()
        {
            // بعد EXACT، لو المستخدم ضغط رقم تاني، الـ guard لازم
            // يمنعه — ده اللي كان بيتفلت بسبب الـ '.' vs ','.
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                foreach (var culture in new[] { "de-DE", "ar-DZ", "fr-FR", "en-US" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);

                    var p = Pad(123.45);
                    p.Press("EXACT");
                    Assert.Equal("123.45", p.Input);

                    p.Press("9");   // لازم يتجاهل
                    Assert.Equal("123.45", p.Input);
                    Assert.Equal(123.45d, p.Paid);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Change_IsSameOnEveryCulture()
        {
            // التحقق: نفس المدخلات ⇒ نفس الباقي على أي جهاز
            var reference = BuildAndReadChange(200.00, "150");

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                foreach (var culture in new[] { "de-DE", "ar-EG", "en-US", "fr-FR" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);
                    Assert.Equal(reference, BuildAndReadChange(200.00, "150"));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        // ── تغيير / الباقي ──────────────────────────

        [Fact]
        public void Change_WhenOverpaid()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("5"); p.Press("0");

            Assert.Equal(50d, p.Change);
            Assert.True(p.IsEnough);
        }

        [Fact]
        public void Change_WhenExactlyPaid()
        {
            var p = Pad(100);
            p.Press("1"); p.Press("0"); p.Press("0");

            Assert.Equal(0d, p.Change);
            Assert.True(p.IsEnough);
        }

        [Fact]
        public void Change_WhenUnderpaid_IsNegative()
        {
            var p = Pad(100);
            p.Press("5"); p.Press("0");

            Assert.Equal(-50d, p.Change);
            Assert.False(p.IsEnough);
        }

        [Fact]
        public void Change_WhenEmpty_IsNegativeTotal()
        {
            var p = Pad(100);
            Assert.Equal(-100d, p.Change);
            Assert.False(p.IsEnough);
        }

        [Fact]
        public void IsEnough_IgnoresFloatingPointNoise()
        {
            // عميد بيدفع 0.3 على فاتورة 0.1+0.2.
            var p = new CashPad(0.1 + 0.2);
            p.Press("0"); p.Press("."); p.Press("3");

            Assert.Equal(0.3d, p.Paid);
            Assert.Equal(0d, p.Change);       // مش -1.4e-17
            Assert.True(p.IsEnough);
        }

        // ── SettledAmount ───────────────────────────

        [Fact]
        public void SettledAmount_Empty_FallsBackToTotal()
        {
            // المستخدم ضغط Enter من غير ما يدخل حاجة ⇒ نسجّل الإجمالي.
            var p = Pad(137.25);
            Assert.Equal(137.25d, p.SettledAmount);
        }

        [Fact]
        public void SettledAmount_WithInput_UsesPaid()
        {
            var p = Pad(100);
            p.Press("2"); p.Press("0"); p.Press("0");

            Assert.Equal(200d, p.SettledAmount);
        }

        // ── الأزرار السريعة ─────────────────────────

        [Fact]
        public void QuickAmount_ReplacesInput()
        {
            var p = new CashPad(100, new[] { "50", "100", "200" });
            p.Press("1"); p.Press("2"); p.Press("3");
            p.Press("200");

            Assert.Equal("200", p.Input);
            Assert.Equal(200d, p.Paid);
        }

        [Fact]
        public void UnknownTag_IsIgnored()
        {
            var p = Pad(100);
            p.Press("1");
            p.Press("NOT_A_BUTTON");

            Assert.Equal("1", p.Input);
        }

        [Fact]
        public void MultiCharTag_IsIgnored()
        {
            // "12" مش رقم واحد — لازم يتجاهل
            var p = Pad(100);
            p.Press("12");

            Assert.Equal("", p.Input);
        }

        [Fact]
        public void NonDigitChar_IsIgnored()
        {
            var p = Pad(100);
            p.Press("a"); p.Press("-"); p.Press("*");

            Assert.Equal("", p.Input);
        }

        // ── Total snapping ───────────────────────────

        [Fact]
        public void Total_IsRoundedOnConstruction()
        {
            var p = new CashPad(10.005);
            p.Press("EXACT");

            Assert.Equal("10.01", p.Input);
        }

        // ── helpers ──────────────────────────────────

        static double BuildAndReadChange(double total, string keys)
        {
            var p = new CashPad(total);
            foreach (var k in keys) p.Press(k.ToString());
            return p.Change;
        }
    }
}
