using System;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PizzaPOS.Services;

/// <summary>
/// ترجمة أخطاء قاعدة البيانات لرسالة يفهمها الكاشير.
///
/// قبل كده أي DELETE بيفشل كان بيرمي SqliteException للـ global handler،
/// والـ handler بيعمل e.Handled = true — يعني المستخدم مبيشوفش حاجة خالص
/// والصفحة بتفضل زي ما هي. عملية الحذف فشلت وفي حد ما يعرف.
///
/// كل مسارات الحذف بقت تستخدم <see cref="Report"/>، فMessage واحدة في
/// مكان واحد بدل رسالة لكل نافذة (أو ولا واحدة).
/// </summary>
public static class DbErrors
{
    const int SqliteConstraint = 19;
    const int SqliteBusy = 5;

    /// <summary>هل ده انتهاك مفتاح أجنبي؟ (SQLITE_CONSTRAINT_FOREIGNKEY = 787،
    /// وبيرجع جواه SQLITE_CONSTRAINT = 19)</summary>
    public static bool IsForeignKeyViolation(Exception ex)
        => ex is SqliteException s
           && s.SqliteErrorCode == SqliteConstraint
           && (s.SqliteExtendedErrorCode == 787
               || s.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase));

    /// <summary>قاعدة البيانات مشغولة — another write اتقفل عليها (backup/sync/مراجعة).</summary>
    public static bool IsBusy(Exception ex)
        => ex is SqliteException s
           && (s.SqliteErrorCode == SqliteBusy
               || s.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase));

    /// <summary>الرسالة اللي بتتعرض للمستخدم. سطر cause بيتبعت للاوج،
    /// والرسالة العربية هي اللي المستخدم يشوفها في الـ dialog.</summary>
    public static string Describe(Exception ex)
    {
        if (IsForeignKeyViolation(ex))
            return "مفيش نقدر نحذف العنصر ده لسه مستخدم في حاجات تانية.\n\n"
                 + "امسح الحاجات المرتبطة الأول (مثلاً المنتجات أو الفئات)، وبعدين جرّب تاني.";

        if (IsBusy(ex))
            return "قاعدة البيانات مشغولة دلوقتي.\n\n"
                 + "غالباً في عملية نسخ احتياطي أو مزامنة شغالة. استنى ثانية وجرّب تاني.";

        if (ex is SqliteException s)
            return "مشكلة في قاعدة البيانات:\n\n" + s.Message;

        return "حصل خطأ غير متوقع:\n\n" + ex.Message;
    }

    /// <summary>
    /// سطر واحد بتستدعيه أي عملية حذف/حفظ: يسجّل في اللوج المستخدم
    /// والسبب الحقيقي، ويوري الـ dialog رسالة مفهومة.
    /// </summary>
    public static void Report(Exception ex, string action)
    {
        AppLogger.Error($"{action} failed", ex);
        MessageBox.Show(
            $"{action} فشل.\n\n{Describe(ex)}",
            "مشكلة في الحفظ", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
