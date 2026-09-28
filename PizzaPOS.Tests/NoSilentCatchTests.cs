using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace PizzaPOS.Tests
{
    /// <summary>
    /// بند #4: ابتلاع الاستثناءات بصمت.
    ///
    /// أي catch فاضي بيعني إن POS بيكمل شغله وفيه error ضاع ما حدش
    /// عرف بيه — أسوأ من إنه يوقّف ويقولك. الـ catch الفاضي مسموح في
    /// cases معدودة بس لازم يكون allowlisted مع سبب مكتوب، وأي file
    /// تاني فيه catch فاضي بيكسر الاختبار.
    ///
    /// الـ pattern بنفحصه catch فاضي حقيقي بس:
    ///     catch { }
    /// مش catch بيعمل شغل (log/throw) — ده allowed ومقصود.
    /// </summary>
    public class NoSilentCatchTests
    {
        /// <summary>catch فاضي، بأي مسافات أو أسطر فاضية جواه.</summary>
        static readonly Regex EmptyCatch = new(@"catch\s*(\(\s*\w*\s*\))?\s*\{\s*\}",
            RegexOptions.Compiled);

        /// <summary>
        /// الملفات المسموح لها catch فاضي، والسبب. لو ضفت ملف جديد هنا
        /// لازم تكتب سبب حقيقي — الـ TODO comments هما أشهر excuse.
        /// </summary>
        static readonly (string File, string Why)[] Allowlist =
        {
            ("AppLogger.cs",
                "الـ logger نفسه — لو ال log فشل بيرمي، مفيش layer فوقه يسجّل، فبنتركه يطنّش"),
            ("LicenseManager.cs",
                "حذف ملف الترخيص وقت Deactivate — فشل الحذف ماينفعش يمنع إغلاق البرنامج"),
            ("SingleInstanceGuard.cs",
                "dispose الـ mutex — بيتنادى وقت إغلاق العملية، وthrow هنا بيبوظ الإغلاق"),
            ("App.xaml.cs",
                "الـ first-chance handler — لو رمي، الـ process بينتهي من غير رسالة"),
        };

        static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "POS.slnx"))) return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "POS.slnx not found walking up from " + AppContext.BaseDirectory);
        }

        /// <summary>كل ملفات الـ production، مستبعداً obj/bin.</summary>
        static IEnumerable<string> ProductionSources()
        {
            string prod = Path.Combine(RepoRoot(), "PizzaPOS");
            foreach (string f in Directory.EnumerateFiles(prod, "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                 || f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                 || f.Contains($"{Path.DirectorySeparatorChar}publish{Path.DirectorySeparatorChar}"))
                    continue;
                yield return f;
            }
        }

        [Fact]
        public void NoEmptyCatchOutsideTheAllowlist()
        {
            var violations = new List<string>();

            foreach (string file in ProductionSources())
            {
                string name = Path.GetFileName(file);
                string text = File.ReadAllText(file);
                var hits = EmptyCatch.Matches(text);
                if (hits.Count == 0) continue;

                var allowed = Allowlist.FirstOrDefault(
                    a => a.File.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (allowed.File != null) continue;

                violations.Add($"{name} ({hits.Count})");
            }

            Assert.True(violations.Count == 0,
                "These files swallow exceptions silently — log them (AppLogger.Warn) or throw: "
                + string.Join("; ", violations)
                + $". Allowlisted already: {string.Join(", ", Allowlist.Select(a => a.File))}.");
        }

        [Fact]
        public void AllowlistHasNoDeadEntries()
        {
            // entry بت Allowlist لملف اتمسح أو بقى نضيف =idiosyncrasy
            // بتكدّر المراجعة، فبنمطّعهاش.
            var existing = ProductionSources()
                .Select(Path.GetFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var dead = Allowlist
                .Where(a => !existing.Contains(a.File))
                .Select(a => a.File)
                .ToList();

            Assert.True(dead.Count == 0,
                "Allowlist mentions files that no longer exist: " + string.Join(", ", dead));
        }

        [Fact]
        public void AppDbContext_HasNoEmptyCatchAtAll()
        {
            // أكبر file في المشروع وأكثرهم عمليات كتابة — أي استثناء
            // مبتلعش هنا، من غير استثناء حتى.
            string path = ProductionSources()
                .First(f => Path.GetFileName(f) == "AppDbContext.cs");

            Assert.Empty(EmptyCatch.Matches(File.ReadAllText(path)));
        }

        [Fact]
        public void TheRegexActuallyDetectsEmptyCatches()
        {
            // لو الـ regex باظ، الاختبار الرئيسي بيمر على الفاضي
            // وبيحمينا إن الكود نضيف وهو مش نضيف.
            Assert.Single(EmptyCatch.Matches("try { x(); } catch { }"));
            Assert.Single(EmptyCatch.Matches("try { x(); } catch\n{\n}"));
            Assert.Single(EmptyCatch.Matches("try { x(); } catch (Exception) {   }"));
            Assert.Empty(EmptyCatch.Matches("try { x(); } catch { Log(); }"));
            Assert.Empty(EmptyCatch.Matches("try { x(); } catch { throw; }"));
        }
    }
}
