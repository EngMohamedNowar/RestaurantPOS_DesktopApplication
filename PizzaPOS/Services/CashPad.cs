using System;
using System.Globalization;

namespace PizzaPOS.Services
{
    /// <summary>
    /// لوحة الدفع النقدي — state machine للكيبورد + حساب الباقي.
    ///
    /// كان كله جوه CashDialog جوّه WPF، فماخدوش يتختبر خالص، وفيه
    /// bug في الـ culture:
    ///
    /// زرار "EXACT" كان بيحط _total.ToString("F2") حسب culture الجهاز،
    /// بس كل الـ guards (منع أكتر من خانتين عشريتين) كانت بتدور على
    /// '.' حرف. على جهاز الـ decimal separator بتاعه فاصلة (de-DE،
    /// ar-DZ...) الـ input بيطلع "123,45"، الـ Contains('.') بيرجع
    /// false، فالمستخدم يقدر يضيف رقم تاني والـ guard يتفلت منه،
    /// والباقي بيطلع 0.01.
    ///
    /// كل الحسابات هنا Invariant، والعرض لوحده (F2) بياخد culture.
    /// </summary>
    public sealed class CashPad
    {
        readonly string[] _quickAmounts;

        public CashPad(double total, string[]? quickAmounts = null)
        {
            Total = Round(total);
            _quickAmounts = quickAmounts ?? new[] { "50", "100", "200", "500" };
            Input = "";
        }

        public double Total { get; }

        /// <summary>اللي المستخدم كاتبه لحد دلوقتي — دايماً بنقطة عشرية واحدة.</summary>
        public string Input { get; private set; }

        public bool HasValue => Input.Length > 0 && Input != "0.";

        /// <summary>المبلغ المدفوع، أو 0 لو لسه مفيش.</summary>
        public double Paid => HasValue && double.TryParse(Input,
            NumberStyles.Number, CultureInfo.InvariantCulture, out double v) ? Round(v) : 0d;

        /// <summary>الباقي. سالب يعني ناقص.</summary>
        public double Change => Round(Paid - Total);

        public bool IsEnough => HasValue && Change >= 0;

        /// <summary>المبلغ اللي هيتسجل مع الأوردر (المدفوع، أو الإجمالي لوExact).</summary>
        public double SettledAmount => HasValue ? Paid : Total;

        // ══════════════════════════════════════════════════════════
        //  الإدخال
        // ══════════════════════════════════════════════════════════

        public void Press(string tag)
        {
            switch (tag)
            {
                case "B":                                   // backspace
                    if (Input.Length > 0) Input = Input[..^1];
                    break;

                case "C":                                   // clear
                    Input = "";
                    break;

                case "EXACT":
                    Input = Total.ToString("F2", CultureInfo.InvariantCulture);
                    break;

                case ".":
                    if (!Input.Contains('.'))
                        Input += Input.Length == 0 ? "0." : ".";
                    break;

                default:
                    if (_quickAmounts.Contains(tag)) { Input = tag; break; }
                    if (!IsDigit(tag)) break;
                    AppendDigit(tag);
                    break;
            }
        }

        void AppendDigit(string digit)
        {
            // حارمين خانتين عشريتين بس
            int dot = Input.IndexOf('.');
            if (dot >= 0 && Input.Length - dot >= 3) return;

            // مافيش leading zero (يعني "0" لوحدها بتتقفل على أي رقم)
            Input = Input == "0" ? digit : Input + digit;
        }

        static bool IsDigit(string tag) => tag.Length == 1 && tag[0] is >= '0' and <= '9';

        public void Clear() => Input = "";

        static double Round(double v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    }
}
