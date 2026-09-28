using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PizzaPOS.Services
{
    /// <summary>
    /// حصيلة الفاتورة — منطق نقي، مافيش UI ولا DB.
    ///
    /// كان كله جوه MainViewModel.Recalc()، مربوط بـ WPF وبيعمل
    /// استعلامين في الـ DB مع كل ضغطة مفتاح في خانة الخصم.
    /// </summary>
    public sealed class OrderTotals
    {
        public decimal Subtotal { get; init; }
        public decimal Discount { get; init; }
        /// <summary>المبلغ بعد الخصم وقبل الضريبة/الخدمة — الأساس اللي بيتحسب عليه الاتنين.</summary>
        public decimal AfterDiscount { get; init; }
        public decimal TaxRate { get; init; }
        public decimal ServiceRate { get; init; }
        public decimal Tax { get; init; }
        public decimal ServiceCharge { get; init; }
        public decimal Total { get; init; }

        /// <summary>
        /// الإجمالي = مجموع الأجزاء بالظبط. بنحسبه كده عن قصد: لو
        /// جمعنا الأجزاء المدورة بشكل مستقل، ممكن يطلع فرق ملّي على
        /// الإيصال ومحدش يعرف ليه.

        /// </summary>
        public bool ComponentsSumToTotal() =>
            Math.Round(Subtotal - Discount + Tax + ServiceCharge, 2) == Total;
    }

    public static class OrderCalculator
    {
        public const decimal DefaultTaxRate = 14m;
        public const decimal DefaultServiceRate = 0m;

        /// <summary>كل النسب بتتقفل في 0..100 — مش حد يدخل -50 ولا 5000.</summary>
        const decimal MaxRate = 100m;

        // ══════════════════════════════════════════════════════════
        //  الحساب
        // ══════════════════════════════════════════════════════════

        public static OrderTotals Calculate(
            IEnumerable<decimal> itemSubtotals,
            string? discountInput,
            bool discountIsPercent,
            string? taxSetting,
            string? serviceSetting)
        {
            decimal taxRate = NormalizeRate(ParseRate(taxSetting, DefaultTaxRate));
            decimal serviceRate = NormalizeRate(ParseRate(serviceSetting, DefaultServiceRate));
            return Calculate(itemSubtotals, discountInput, discountIsPercent, taxRate, serviceRate);
        }

        public static OrderTotals Calculate(
            IEnumerable<decimal> itemSubtotals,
            string? discountInput,
            bool discountIsPercent,
            decimal taxRate,
            decimal serviceRate)
        {
            decimal sub = Round(SumSafe(itemSubtotals));

            decimal dv = ParseAmount(discountInput);
            decimal discount = discountIsPercent
                ? Round(sub * Clamp(dv, 0m, 100m) / 100m)
                // خصم مبلغ ثابت مينفعش يزيد عن الفاتورة، ومش بنقبل سالب
                // (سالب معناه زيادة في الفاتورة، ودي مش خصم).
                : Round(Clamp(dv, 0m, sub));

            decimal after = Round(sub - discount);
            decimal tax = Round(after * taxRate / 100m);
            decimal service = Round(after * serviceRate / 100m);

            return new OrderTotals
            {
                Subtotal = sub,
                Discount = discount,
                AfterDiscount = after,
                TaxRate = taxRate,
                ServiceRate = serviceRate,
                Tax = tax,
                ServiceCharge = service,
                // مجموع الأجزاء المدورة بالظبط — مش after + tax + service
                // لأن تجميع Parts مدورة بشكل مستقل ممكن يفرق ملّي.
                Total = Round(after + tax + service)
            };
        }

        // ══════════════════════════════════════════════════════════
        //  النسب والإعدادات
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// SettingsWindow بيحفظ النسبة مقسومة على 100 ("0.1400")، بس
        /// الـ default في الـ code كان "14". الاتنين اتعاملوا بحالة
        /// "أقل من 1 ⇒ ×100".
        ///
        /// (احنا كمان بنقرا بـ InvariantCulture دلوقتي — شوف ParseRate.)
        /// </summary>
        public static decimal NormalizeRate(decimal rate)
        {
            rate = Clamp(rate, 0m, MaxRate);
            if (rate > 0m && rate < 1m) rate *= 100m;
            return Round(rate);
        }

        /// <summary>
        /// الإعدادات بتقرأ وتكتب بـ culture الجهاز، بس بتتقري لازم تكون
        /// Invariant.
        ///
        /// الأرقام دي مخزّنة كنص في جدول Settings، وبتتشارك بين أجهزة
        /// كتير. لو قُرِيت بـ culture الجهاز، على جهاز الـ decimal
        /// separator بتاعه فاصلة (de-DE، ar-DZ، ar-SA...) السلسلة
        /// "0.1400" بتتفسّر على إنها **1400** (الفاصلة هناك فاصل آلاف)
        /// ⇒ ضريبة 14 ضعف المبلغ. سطر واحد بيدفع فاتورة العميل غلط.
        ///
        /// عشان كمان بنقبل الفاصلة comma كمدخل بديل، عشان المستخدم اللي
        /// بيكتب 0,14 في خانة الخصم يتفهم.
        /// </summary>
        public static decimal ParseRate(string? raw, decimal fallback)
        {
            if (TryParseFlexible(raw, out decimal value)) return Clamp(value, 0m, MaxRate);
            return Clamp(fallback, 0m, MaxRate);
        }

        /// <summary>مبلغ الخصم اللي المستخدم بيكتبه. نص غلط ⇒ صفر، مش exception.</summary>
        public static decimal ParseAmount(string? raw)
        {
            return TryParseFlexible(raw, out decimal value) ? value : 0m;
        }

        /// <summary>
        /// نسخة صارمة من <see cref="ParseAmount"/>: بترجع false لو النص
        /// مش رقم خالص، بدل ما تبلعه صفر.
        ///
        /// مهم فيها الحقول اللي الصفر فيها مش مقبول — زي رسوم
        /// التوصيل: "فاضي" معناها صفر مقصود، بس "abc" معناها غلط في
        /// الإدخال ولازم المستخدم يعرفه، مش إننا نسلّم توصيل مجاني
        /// بصمت.
        /// </summary>
        public static bool TryParseAmount(string? raw, out decimal value)
            => TryParseFlexible(raw, out value);

        /// <summary>
        /// يستنتج النسبة اللي اتطبقت فعلاً على أوردر قديم، من أرقامه
        /// المحفوظة في الـ DB.
        ///
        /// بتستعمله الإيصال عشان يطبع النسبة الصح. ماينفعش نقرا
        /// النسبة من الـ Settings هنا: المحل يقدر يكون غيّرها بعد ما
        /// الأوردر اتعمل، فالإيصال يبقى يقول 20% والمبلغ محسوب بـ 14%.
        ///
        /// الضريبة والخدمة بيتحسبوا على (المجموع - الخصم)، فالأساس
        /// هنا هو AfterDiscount.
        /// </summary>
        public static decimal RateFromAmount(decimal amount, decimal subtotal, decimal discount)
        {
            decimal basis = subtotal - discount;
            if (basis <= 0m || amount <= 0m) return 0m;
            return Math.Round(amount * 100m / basis, 2);
        }

        /// <summary>
        /// بيجرب Invariant الأول، وبعدين culture الجهاز. الترتيب مهم:
        /// العكس هو اللي كان بيغلط.
        /// </summary>
        static bool TryParseFlexible(string? raw, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            raw = raw.Trim().Replace(" ", "").Replace("٫", ".");   // ٫ = الفاصلة العربية

            const NumberStyles styles = NumberStyles.Number;
            return decimal.TryParse(raw, styles, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(raw, styles, CultureInfo.CurrentCulture, out value);
        }

        // ══════════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// تحويل آمن لمبلغ من double (الـ models لسه double) لـ decimal.
        ///
        /// `(decimal)double.NaN` بيرمي OverflowException، يعني إن
        /// عملية بيع واحدة غلط تقفل البرنامج. هنا بنرجّع 0 بدل ما
        /// ن.padra exception.
        /// </summary>
        public static decimal Money(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return 0m;
            if (value >= (double)decimal.MaxValue) return decimal.MaxValue;
            if (value <= (double)decimal.MinValue) return decimal.MinValue;
            return (decimal)value;
        }

        static decimal SumSafe(IEnumerable<decimal> values)
        {
            decimal total = 0m;
            foreach (decimal v in values)
            {
                total += v;
            }
            return total;
        }

        static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        static decimal Clamp(decimal v, decimal min, decimal max)
            => v < min ? min : v > max ? max : v;
    }
}
