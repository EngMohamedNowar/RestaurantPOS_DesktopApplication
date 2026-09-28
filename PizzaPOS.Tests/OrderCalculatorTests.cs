using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using PizzaPOS.Services;
using Xunit;

namespace PizzaPOS.Tests
{
    public class OrderCalculatorTests
    {
        // ── الأساسيات ────────────────────────────────

        [Fact]
        public void NoItems_AllZero()
        {
            var t = OrderCalculator.Calculate(new decimal[0], "", false, 14m, 0m);

            Assert.Equal(0m, t.Subtotal);
            Assert.Equal(0m, t.Total);
            Assert.True(t.ComponentsSumToTotal());
        }

        [Fact]
        public void SingleItem_NoDiscount_AppliesTax()
        {
            var t = OrderCalculator.Calculate(new[] { 100m }, "", false, 14m, 0m);

            Assert.Equal(100m, t.Subtotal);
            Assert.Equal(0m, t.Discount);
            Assert.Equal(100m, t.AfterDiscount);
            Assert.Equal(14m, t.Tax);          // 14% من 100
            Assert.Equal(0m, t.ServiceCharge);
            Assert.Equal(114m, t.Total);
            Assert.True(t.ComponentsSumToTotal());
        }

        [Fact]
        public void MultipleItems_SummedSubtotal()
        {
            var t = OrderCalculator.Calculate(new[] { 50m, 30.50m, 19.99m }, "", false, 0m, 0m);

            Assert.Equal(100.49m, t.Subtotal);
            Assert.Equal(100.49m, t.Total);
        }

        // ── الخصم ───────────────────────────────────

        [Fact]
        public void PercentDiscount_AppliesToSubtotal()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "10", true, 0m, 0m);

            Assert.Equal(20m, t.Discount);
            Assert.Equal(180m, t.AfterDiscount);
            Assert.Equal(180m, t.Total);
        }

        [Fact]
        public void FixedDiscount_SubtractsDirectly()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "50", false, 0m, 0m);

            Assert.Equal(50m, t.Discount);
            Assert.Equal(150m, t.Total);
        }

        [Fact]
        public void FixedDiscount_CappedAtSubtotal()
        {
            // عميل يدخل خصم 500 على فاتورة 200
            var t = OrderCalculator.Calculate(new[] { 200m }, "500", false, 0m, 0m);

            Assert.Equal(200m, t.Discount);
            Assert.Equal(0m, t.Total);
        }

        [Fact]
        public void FixedDiscount_EqualToSubtotal_ZeroTotal()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "200", false, 0m, 0m);

            Assert.Equal(200m, t.Discount);
            Assert.Equal(0m, t.AfterDiscount);
            Assert.Equal(0m, t.Total);
        }

        [Fact]
        public void NegativeDiscountInput_DoesNotIncreaseTotal()
        {
            // "-50" معناه أصلاً "زيادة 50" مش خصم. لازم يتجاهل.
            var t = OrderCalculator.Calculate(new[] { 200m }, "-50", false, 0m, 0m);

            Assert.Equal(0m, t.Discount);
            Assert.Equal(200m, t.Total);
        }

        [Fact]
        public void PercentDiscount_Above100_ClampedTo100()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "150", true, 0m, 0m);

            Assert.Equal(200m, t.Discount);
            Assert.Equal(0m, t.Total);
        }

        [Fact]
        public void NegativePercentDiscount_TreatedAsZero()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "-20", true, 0m, 0m);

            Assert.Equal(0m, t.Discount);
            Assert.Equal(200m, t.Total);
        }

        [Fact]
        public void DiscountIsTaxedOnAfterDiscountNotSubtotal()
        {
            // 200 - 50 خصم = 150، والضريبة 10% على الـ 150 = 15
            // (لو اتحسبت على الـ 200 كانت 20)
            var t = OrderCalculator.Calculate(new[] { 200m }, "50", false, 10m, 0m);

            Assert.Equal(150m, t.AfterDiscount);
            Assert.Equal(15m, t.Tax);
            Assert.Equal(165m, t.Total);
        }

        [Fact]
        public void EmptyDiscountInput_MeansNoDiscount()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "", false, 0m, 0m);
            Assert.Equal(0m, t.Discount);
            Assert.Equal(200m, t.Total);
        }

        [Fact]
        public void GarbageDiscountInput_MeansNoDiscount_NoThrow()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "abc!!", false, 0m, 0m);
            Assert.Equal(0m, t.Discount);
            Assert.Equal(200m, t.Total);
        }

        [Fact]
        public void NullDiscountInput_MeansNoDiscount()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, null, false, 0m, 0m);
            Assert.Equal(0m, t.Discount);
        }

        // ── رسوم الخدمة ──────────────────────────────

        [Fact]
        public void ServiceCharge_AppliedOnAfterDiscount()
        {
            var t = OrderCalculator.Calculate(new[] { 200m }, "50", false, 0m, 10m);

            Assert.Equal(15m, t.ServiceCharge);   // 10% من 150
            Assert.Equal(165m, t.Total);
        }

        [Fact]
        public void TaxAndService_StackedOnSameBase()
        {
            // 200, خصم 20%, ضريبة 14%, خدمة 5%
            // after = 160، tax = 22.4، srv = 8
            var t = OrderCalculator.Calculate(new[] { 200m }, "20", true, 14m, 5m);

            Assert.Equal(40m, t.Discount);
            Assert.Equal(160m, t.AfterDiscount);
            Assert.Equal(22.40m, t.Tax);
            Assert.Equal(8.00m, t.ServiceCharge);
            Assert.Equal(190.40m, t.Total);
            Assert.True(t.ComponentsSumToTotal());
        }

        // ── الـ rounding ─────────────────────────────

        [Fact]
        public void NoFloatingPointDrift_RepeatedThirds()
        {
            // فواصل الـ double (0\.1\+0\.2): 33.33 * 3
            var t = OrderCalculator.Calculate(new[] { 33.33m, 33.33m, 33.33m }, "", false, 0m, 0m);

            Assert.Equal(99.99m, t.Subtotal);
            Assert.Equal(99.99m, t.Total);
        }

        [Fact]
        public void ComponentsAlwaysSumToTotal_AcrossScenarios()
        {
            var cases = new[]
            {
                (new[] { 200m }, "20", true, 14m, 5m),
                (new[] { 99.99m, 0.01m }, "33.33", true, 14m, 0m),
                (new[] { 7.77m }, "1.11", false, 7.5m, 2.5m),
                (new[] { 1234.56m }, "99.99", true, 14m, 10m),
            };

            foreach (var (items, disc, pct, tax, srv) in cases)
            {
                var t = OrderCalculator.Calculate(items, disc, pct, tax, srv);
                Assert.True(t.ComponentsSumToTotal(),
                    $"المجموع {t.Total} مش مطابق لـ مجموع الأجزاء " +
                    $"({t.Subtotal} - {t.Discount} + {t.Tax} + {t.ServiceCharge})");
            }
        }

        [Fact]
        public void Rounding_IsAwayFromZero_HalfCentUp()
        {
            // 10.005 بتتقرب 10.01 (مش 10.00 زي Banker's rounding)
            var t = OrderCalculator.Calculate(new[] { 10.005m }, "", false, 0m, 0m);
            Assert.Equal(10.01m, t.Subtotal);
        }

        // ── ParseRate ────────────────────────────────

        [Theory]
        [InlineData("0.14", 14)]      // الشكل اللي SettingsWindow بيحفظه
        [InlineData("0.1400", 14)]
        [InlineData("14", 14)]
        [InlineData("14.5", 14.5)]
        [InlineData("0", 0)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        public void ParseRate_NormalizesFractionToPercent(string? raw, double expected)
        {
            Assert.Equal((decimal)expected,
                OrderCalculator.NormalizeRate(OrderCalculator.ParseRate(raw, 0m)));
        }

        [Fact]
        public void ParseRate_NegativeRejected()
        {
            Assert.Equal(0m, OrderCalculator.ParseRate("-5", 0m));
        }

        [Fact]
        public void ParseRate_Above100_Clamped()
        {
            Assert.Equal(100m, OrderCalculator.ParseRate("5000", 0m));
        }

        [Fact]
        public void ParseRate_Garbage_ReturnsFallback()
        {
            Assert.Equal(14m, OrderCalculator.ParseRate("not a number", 14m));
        }

        [Fact]
        public void ParseRate_AcceptsArabicDecimalSeparator()
        {
            // ٫ = decimal point in Arabic locales
            Assert.Equal(14m, OrderCalculator.NormalizeRate(OrderCalculator.ParseRate("0٫14", 0m)));
        }

        [Fact]
        public void ParseRate_AcceptsCommaDecimalSeparator()
        {
            // 0,14 على جهاز comma-culture
            Assert.Equal(14m, OrderCalculator.ParseRate("0,14", 0m));
        }

        // ── الـ bug الأصلي: culture في الـ settings ──

        [Fact]
        public void SettingsTaxRate_ReadsSameOnCommaDecimalCulture()
        {
            // ده الـ regression بتاع الـ culture bomb.
            //
            // "0.1400" متخزّن في جدول Settings. لو اتقرا بـ culture
            // الجهاز، وعلى الجهاز ده فاصله-decimal (de-DE، ar-DZ) النقطة
            // بتفسّرها فاصل آلاف ⇒ 1400 ⇒ ضريبة 14× المبلغ.
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                foreach (var culture in new[] { "de-DE", "ar-DZ", "en-US" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);

                    var t = OrderCalculator.Calculate(
                        new[] { 100m }, "", false,
                        "0.1400",     // نفس النص اللي في الـ DB
                        "0");

                    Assert.Equal(14m, t.TaxRate);
                    Assert.Equal(14m, t.Tax);
                    Assert.Equal(114m, t.Total);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void DiscountInput_ReadsSameOnCommaDecimalCulture()
        {
            // نفس المشكلة في خانة الخصم: "10.50" على جهاز comma-culture
            // ماكانش يتقرا خالص (كان بيبقى 1050 خصم على فاتورة 200).

            Assert.Equal(10.50m, OrderCalculator.ParseAmount("10.50"));
        }

        [Fact]
        public void ParseAmount_DoesNotTreatDotAsThousandsSeparator()
        {
            // de-DE: "1.234" معناها 1234. إحنا عايزينها 1.234 ج.
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal(1.234m, OrderCalculator.ParseAmount("1.234"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Calculate_IsCultureInvariant()
        {
            // نفس المدخلات ⇒ نفس النتيجة، مهما كانت لغة الجهاز.
            var reference = OrderCalculator.Calculate(
                new[] { 200m }, "10", true, "0.14", "0.05");

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                foreach (var culture in new[] { "de-DE", "ar-EG", "en-US", "fr-FR", "tr-TR" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);

                    var t = OrderCalculator.Calculate(
                        new[] { 200m }, "10", true, "0.14", "0.05");

                    Assert.Equal(reference.Subtotal, t.Subtotal);
                    Assert.Equal(reference.Discount, t.Discount);
                    Assert.Equal(reference.Tax, t.Tax);
                    Assert.Equal(reference.ServiceCharge, t.ServiceCharge);
                    Assert.Equal(reference.Total, t.Total);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        // ── Money: حاجز الأمان ───────────────────────

        [Fact]
        public void Money_NaN_BecomesZero()
        {
            // (decimal)double.NaN بيرمي OverflowException. لو ده وصل
            // لعملية بيع، البرنامج بيقفل والأوردر بيضيع.
            Assert.Equal(0m, OrderCalculator.Money(double.NaN));
        }

        [Fact]
        public void Money_PositiveInfinity_BecomesZero()
        {
            Assert.Equal(0m, OrderCalculator.Money(double.PositiveInfinity));
            Assert.Equal(0m, OrderCalculator.Money(double.NegativeInfinity));
        }

        [Fact]
        public void Money_BeyondDecimalRange_ClampsInsteadOfThrowing()
        {
            // double.MaxValue أكبر بكتير من decimal.MaxValue — الـ cast
            // العادي بيرمي exception هنا.
            Assert.Equal(decimal.MaxValue, OrderCalculator.Money(double.MaxValue));
            Assert.Equal(decimal.MinValue, OrderCalculator.Money(double.MinValue));
        }

        [Fact]
        public void Money_NormalValues_PassThrough()
        {
            Assert.Equal(100m, OrderCalculator.Money(100d));
            Assert.Equal(0.3m, OrderCalculator.Money(0.3d));
            Assert.Equal(-50.25m, OrderCalculator.Money(-50.25d));
        }

        [Fact]
        public void Money_NeverThrows_ForAnyDouble()
        {
            // أي double تاني لازم يعدّي من غير exception
            double[] values =
            {
                0, -0, 1e-30, 1e30, -1e30, 7.9e28, -7.9e28,
                double.Epsilon, double.MinValue, double.MaxValue,
                double.NaN, double.PositiveInfinity, double.NegativeInfinity
            };

            foreach (double v in values)
            {
                decimal result = OrderCalculator.Money(v);
                Assert.True(result >= decimal.MinValue && result <= decimal.MaxValue);
            }
        }

        [Fact]
        public void Calculate_WithNaNSubtotal_DoesNotThrow()
        {
            // item price قعّد NaN (قسمة على صفر في مكان ما) —
            // الحساب لازم يكمّل، مش يطيّح.
            var t = OrderCalculator.Calculate(
                new[] { OrderCalculator.Money(double.NaN), 100m },
                "10", true, 14m, 0m);

            Assert.Equal(100m, t.Subtotal);   // الـ NaN اتجاهل
            Assert.Equal(10m, t.Discount);
            Assert.Equal(90m, t.AfterDiscount);
            Assert.Equal(12.60m, t.Tax);
            Assert.Equal(102.60m, t.Total);
        }

        // ── RateFromAmount: النسبة على الإيصال ───────

        [Fact]
        public void RateFromAmount_RecoversTheRateActuallyApplied()
        {
            // 14% على 200
            Assert.Equal(14m, OrderCalculator.RateFromAmount(28m, 200m, 0m));
        }

        [Fact]
        public void RateFromAmount_NotTheDefault14_WhenShopChangedTo20()
        {
            // الـ regression بتاع الـ "14%" المكتوبة hardcode:
            // محل ضريبته 20% لازم الإيصال يطبع 20%.
            Assert.Equal(20m, OrderCalculator.RateFromAmount(40m, 200m, 0m));
        }

        [Fact]
        public void RateFromAmount_HandlesWeirdRates()
        {
            Assert.Equal(7.5m, OrderCalculator.RateFromAmount(7.5m, 100m, 0m));
            Assert.Equal(0.5m, OrderCalculator.RateFromAmount(0.5m, 100m, 0m));
        }

        [Fact]
        public void RateFromAmount_UsesAfterDiscountAsBasis()
        {
            // 200 - 50 خصم = 150، وضريبة 15% على الـ 150 = 22.5
            // لو حسبنا على 200 كانت هتطلع 11.25% وتلخبط صاحب المحل.
            Assert.Equal(15m, OrderCalculator.RateFromAmount(22.5m, 200m, 50m));
        }

        [Fact]
        public void RateFromAmount_ZeroTax_IsZeroNotDivideByZero()
        {
            Assert.Equal(0m, OrderCalculator.RateFromAmount(0m, 200m, 0m));
        }

        [Fact]
        public void RateFromAmount_FullDiscount_ZeroNotDivideByZero()
        {
            // خصم 100% ⇒ الأساس صفر. لازم ترجع 0 مش NaN/Infinity،
            // عشان متطبعش "NaN%" على الإيصال.
            Assert.Equal(0m, OrderCalculator.RateFromAmount(0m, 200m, 200m));
        }

        [Fact]
        public void RateFromAmount_DiscountGreaterThanSubtotal_Zero()
        {
            Assert.Equal(0m, OrderCalculator.RateFromAmount(10m, 200m, 250m));
        }

        [Fact]
        public void RateFromAmount_MatchesCalculatorOutput_RoundTrip()
        {
            // الاستنتاج لازم يدي نفس النسبة اللي دخلت في الحساب،
            // مهما كانت النسبة.
            foreach (decimal rate in new[] { 0m, 0.5m, 7.5m, 14m, 20m, 25m })
            {
                var t = OrderCalculator.Calculate(new[] { 200m }, "10", true, rate, rate);

                decimal taxBack = OrderCalculator.RateFromAmount(t.Tax, t.Subtotal, t.Discount);
                decimal srvBack = OrderCalculator.RateFromAmount(t.ServiceCharge, t.Subtotal, t.Discount);

                Assert.Equal(rate, taxBack);
                Assert.Equal(rate, srvBack);
            }
        }

        // ── حدود ─────────────────────────────────────

        [Fact]
        public void VeryLargeOrder_NoOverflow()
        {
            var t = OrderCalculator.Calculate(new[] { 999_999.99m, 999_999.99m }, "10", true, 14m, 5m);

            Assert.True(t.Total > 0);
            Assert.True(t.ComponentsSumToTotal());
        }

        [Fact]
        public void TaxRate100_IsFullTaxNotClampedAway()
        {
            var t = OrderCalculator.Calculate(new[] { 100m }, "", false, 100m, 0m);

            Assert.Equal(100m, t.TaxRate);
            Assert.Equal(100m, t.Tax);
            Assert.Equal(200m, t.Total);
        }

        [Fact]
        public void ServiceRateZero_StaysZeroNotNormalized()
        {
            // 0 لازم تفضل 0، مش تتحول لـ 0 (زي الكود القديم اللي كان
            // بيقول "is > 0 and < 1")
            var t = OrderCalculator.Calculate(new[] { 100m }, "", false, 0m, 0m);

            Assert.Equal(0m, t.ServiceRate);
            Assert.Equal(0m, t.ServiceCharge);
        }
    }
}
