// كل اختبارات المشروع دي integration tests بتلمس حالة عامة واحدة:
// - SqliteConnection.ClearAllPools() عملية عالمية على كل الـ process
// - ملفات DB حقيقية على القرص
// - AppLogger بيكتب في %AppData% حقيقي
//
// تشغيل الكلاسات بالتوازي (الافتراضي في xUnit) كان بيسبّب الـ ClearAllPools
// من كلاس يخبط connection مفتوح في كلاس تاني شغّال في نفس اللحظة — وده كان
// بيفشل RestoreFrom_DeletesStaleWalAndShmSidecars بشكل عشوائي.
// عشان كده: تعطيل التوازي والنتيجة تبقى ثابتة ومتكررة.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
