using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using PizzaPOS.Services;
using Xunit;

namespace PizzaPOS.Tests;

/// <summary>
/// اختبارات الترخيص.
///
/// أهم اختبار هنا هو <see cref="Verify_RejectsSignatureFromAKeyWeDoNotOwn"/>:
/// النظام القديم كان HMAC بسِكِرت داخل كود العميل، فأي حد يقدر يعمل
/// مفتاح لأي جهاز. الاختبار ده بيأكد إن المفتاح العام المدمج هو الوحيد
/// اللي بيقبله البرنامج — أي pair تاني (حتى لو اتعمل على نفس الجهاز)
/// لازم يترفض.
///
/// ملاحظة: مفيش private key في الريبو ولا في الاختبارات. ده مقصود.
/// الاختبارات بتستخدم pair مؤقت للـ round-trips، وبتستخدم المفتاح المدمج
/// لإثبات إن الحاجات التانية مترفضة.
/// </summary>
public class LicenseTests : IDisposable
{
    readonly string _tmpDir;
    readonly string _licensePath;

    public LicenseTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(),
            "PizzaPOS.LicTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
        _licensePath = Path.Combine(_tmpDir, "license.dat");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, true); }
        catch { }
    }

    static string NewHwid() => Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

    static string Pem(RSA rsa) => rsa.ExportSubjectPublicKeyInfoPem();

    // ─────────────────────────── HardwareId ───────────────────────────

    [Fact]
    public void HardwareId_IsStableAcrossCalls()
    {
        string a = HardwareId.Generate();
        string b = HardwareId.Generate();

        Assert.Equal(a, b);
    }

    [Fact]
    public void HardwareId_Is64HexChars()
    {
        string id = HardwareId.Generate();

        Assert.Equal(64, id.Length);
        Assert.Matches("^[0-9A-F]{64}$", id);
    }

    [Fact]
    public void HardwareId_ShortId_IsPrefixOfFull()
    {
        string full = HardwareId.Generate();
        string shortId = HardwareId.GetShortId();

        Assert.Equal(16, shortId.Length);
        Assert.StartsWith(shortId, full, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HardwareId_ShortId_MatchesManagerHwid()
    {
        Assert.Equal(HardwareId.GetShortId(), LicenseManager.GetHwid());
    }

    [Fact]
    public void HardwareId_DerivesFromTheMachineGuidValue()
    {
        // MachineGuid **قيمة** جوه مفتاح Cryptography، مش subkey. لو حد
        // غيّر المسار لـ Cryptography\MachineGuid هترجع null والبرنامج
        // هيسقط على الـ fallback الـ عشوائي من غير ما حد ياخد باله —
        // فاختبار الـ derivation ده هو اللي بيمنع الرجوع للـ bug ده.
        string? guid = null;
        foreach (RegistryHive hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using RegistryKey? root = RegistryKey.OpenBaseKey(hive, view);
                    using RegistryKey? key = root?.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                    if (key?.GetValue("MachineGuid") is string v && !string.IsNullOrWhiteSpace(v))
                    {
                        guid = v.Trim();
                        break;
                    }
                }
                catch { }
            }
            if (guid != null) break;
        }

        // بيئة مالهاش access للـ registry — الـ fallback هو السلوك الصح هنا
        if (guid == null) return;

        string expected = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes("mg|" + guid)));

        Assert.Equal(expected, HardwareId.Generate());
    }

    // ─────────────────────────── Group / Ungroup ───────────────────────────

    [Fact]
    public void Group_ProducesBase64UrlGroupsOfEight()
    {
        string raw = Convert.ToBase64String(new byte[256]);
        string grouped = LicenseSignature.Group(raw);

        string[] groups = grouped.Split(LicenseSignature.GroupSeparator);

        Assert.All(groups, g => Assert.InRange(g.Length, 1, LicenseSignature.GroupSize));
        Assert.Equal(raw.TrimEnd('='), grouped.Replace(".", "").Replace('-', '+').Replace('_', '/'));
    }

    [Fact]
    public void Ungroup_RoundTripsAGroupedSignature()
    {
        byte[] sig = RandomNumberGenerator.GetBytes(256);
        string grouped = LicenseSignature.Group(Convert.ToBase64String(sig));

        byte[]? back = LicenseSignature.Ungroup(grouped);

        Assert.NotNull(back);
        Assert.Equal(sig, back);
    }

    [Fact]
    public void Ungroup_IgnoresJunkFromCopyPaste()
    {
        byte[] sig = RandomNumberGenerator.GetBytes(256);
        string grouped = LicenseSignature.Group(Convert.ToBase64String(sig));

        // لصق من واتساب/إيميل: سطور جديدة، مسافات، وفاصلة زيادة
        string messy = "\n  " + grouped.Replace(".", ",  .\n") + "\r\n ";

        byte[]? back = LicenseSignature.Ungroup(messy);

        Assert.NotNull(back);
        Assert.Equal(sig, back);
    }

    [Fact]
    public void Ungroup_ReturnsNullOnGarbage()
    {
        Assert.Null(LicenseSignature.Ungroup("!!! not base64 @@@ ###"));
        Assert.Null(LicenseSignature.Ungroup(""));
    }

    // ─────────────────────────── TrySplitKey ───────────────────────────

    [Fact]
    public void TrySplitKey_SplitsExpiryAndSignature()
    {
        bool ok = LicenseSignature.TrySplitKey(
            "20271231|AAAA.BBBB.CCCC", out string expiry, out string signature);

        Assert.True(ok);
        Assert.Equal("20271231", expiry);
        Assert.Equal("AAAA.BBBB.CCCC", signature);
    }

    [Fact]
    public void TrySplitKey_NormalizesExpiryCase()
    {
        LicenseSignature.TrySplitKey("permanent|AAAA", out string expiry, out _);

        Assert.Equal("PERMANENT", expiry);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("noSeparatorHere")]
    [InlineData("|AAAA")]
    [InlineData("20271231|")]
    public void TrySplitKey_RejectsMalformed(string key)
    {
        Assert.False(LicenseSignature.TrySplitKey(key, out _, out _));
    }

    [Fact]
    public void FormatExpiry_ShowsPermanent()
    {
        Assert.Equal("PERMANENT",
            LicenseSignature.FormatExpiryForDisplay(LicenseSignature.Permanent));
    }

    [Fact]
    public void FormatExpiry_FormatsDate()
    {
        Assert.Equal("2027-12-31", LicenseSignature.FormatExpiryForDisplay("20271231"));
    }

    [Fact]
    public void FormatExpiry_MarksGarbageAsInvalid()
    {
        Assert.Equal("INVALID", LicenseSignature.FormatExpiryForDisplay("not-a-date"));
    }

    // ─────────────────────────── التوقيع ───────────────────────────

    [Fact]
    public void BuildKey_AndVerify_RoundTrip()
    {
        using RSA rsa = RSA.Create(2048);
        string hwid = NewHwid();

        string key = LicenseSignature.BuildKey(rsa, hwid, "20271231");

        Assert.True(LicenseSignature.TrySplitKey(key, out string expiry, out string signature));
        Assert.Equal("20271231", expiry);
        Assert.True(LicenseSignature.VerifyWithPem(
            LicenseSignature.BuildPayload(hwid, expiry), signature, Pem(rsa)));
    }

    [Fact]
    public void BuildKey_AndVerify_RoundTrip_Permanent()
    {
        using RSA rsa = RSA.Create(2048);
        string hwid = NewHwid();

        string key = LicenseSignature.BuildKey(rsa, hwid, LicenseSignature.Permanent);

        Assert.True(LicenseSignature.TrySplitKey(key, out string expiry, out string signature));
        Assert.Equal(LicenseSignature.Permanent, expiry);
        Assert.True(LicenseSignature.VerifyWithPem(
            LicenseSignature.BuildPayload(hwid, expiry), signature, Pem(rsa)));
    }

    /// <summary>
    /// الاختبارات الجاية بتوقّع بـ pair مؤقت وتتحقق بنفس الـ pair، فالفشل
    /// معناه إن الحقل المتغيّر هو السبب الوحيد. (لو تحققنا بالمفتاح المدمج
    /// كان كلهم هيفشلوا على المفتاح مش على الحقل.)
    /// </summary>
    [Fact]
    public void Verify_FailsWhenHwidDiffers()
    {
        using RSA rsa = RSA.Create(2048);
        string hwid = NewHwid();
        string key = LicenseSignature.BuildKey(rsa, hwid, "20271231");
        LicenseSignature.TrySplitKey(key, out string expiry, out string signature);

        string otherHwid = NewHwid();

        Assert.False(LicenseSignature.VerifyWithPem(
            LicenseSignature.BuildPayload(otherHwid, expiry), signature, Pem(rsa)));
    }

    [Fact]
    public void Verify_FailsWhenExpiryIsUpgradedToPermanent()
    {
        // ده بالظبط الهجوم اللي النظام القديم كان مكشوف له: تفتح الملف
        // وتكتب PERMANENT في سطر التاريخ.
        using RSA rsa = RSA.Create(2048);
        string hwid = NewHwid();
        string key = LicenseSignature.BuildKey(rsa, hwid, "20260101");
        LicenseSignature.TrySplitKey(key, out _, out string signature);

        Assert.False(LicenseSignature.VerifyWithPem(
            LicenseSignature.BuildPayload(hwid, LicenseSignature.Permanent), signature, Pem(rsa)));
    }

    [Fact]
    public void Verify_FailsWhenExpiryIsExtended()
    {
        using RSA rsa = RSA.Create(2048);
        string hwid = NewHwid();
        string key = LicenseSignature.BuildKey(rsa, hwid, "20260101");
        LicenseSignature.TrySplitKey(key, out _, out string signature);

        Assert.False(LicenseSignature.VerifyWithPem(
            LicenseSignature.BuildPayload(hwid, "20271231"), signature, Pem(rsa)));
    }

    [Fact]
    public void Verify_FailsWhenSignatureIsFlipped()
    {
        using RSA rsa = RSA.Create(2048);
        string hwid = NewHwid();
        string key = LicenseSignature.BuildKey(rsa, hwid, "20271231");
        LicenseSignature.TrySplitKey(key, out string expiry, out string signature);

        char first = signature[0];
        char replacement = first == 'A' ? 'B' : 'A';
        string tampered = replacement + signature[1..];

        Assert.False(LicenseSignature.VerifyWithPem(
            LicenseSignature.BuildPayload(hwid, expiry), tampered, Pem(rsa)));
    }

    [Fact]
    public void Verify_FailsOnWrongLengthSignature()
    {
        Assert.False(LicenseSignature.Verify(NewHwid(), "20271231", "AAAA"));
        Assert.False(LicenseSignature.Verify(NewHwid(), "20271231", ""));
    }

    [Fact]
    public void Verify_RejectsSignatureFromAKeyWeDoNotOwn()
    {
        // الاختبار الأهم: pair مش بتاعنا (المفتاح المدمج في البرنامج) لازم
        // يترفض حتى لو اتعمل على نفس الجهاز وبنفس الـ HWID.
        using RSA attacker = RSA.Create(2048);
        string hwid = NewHwid();
        string payload = LicenseSignature.BuildPayload(hwid, LicenseSignature.Permanent);
        string forged = LicenseSignature.SignRaw(payload, attacker);

        Assert.False(LicenseSignature.Verify(hwid, LicenseSignature.Permanent, forged));
    }

    [Fact]
    public void Verify_CrossKey_SignatureDoesNotVerifyAgainstOtherPublicKey()
    {
        using RSA signer = RSA.Create(2048);
        using RSA other = RSA.Create(2048);
        string payload = LicenseSignature.BuildPayload(NewHwid(), "20271231");
        string sig = LicenseSignature.SignRaw(payload, signer);

        Assert.True(LicenseSignature.VerifyWithPem(payload, sig,
            signer.ExportSubjectPublicKeyInfoPem()));
        Assert.False(LicenseSignature.VerifyWithPem(payload, sig,
            other.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Verify_PayloadVersionIsPartOfTheSignature()
    {
        // لو PayloadVersion اتغير، المفاتيح القديمة لازم تترفض.
        using RSA rsa = RSA.Create(2048);
        string hwid = NewHwid();
        string payload = LicenseSignature.BuildPayload(hwid, "20271231");
        string sig = LicenseSignature.SignRaw(payload, rsa);

        Assert.Equal("v1|", LicenseSignature.BuildPayload(hwid, "20271231")[..3]);
        Assert.True(LicenseSignature.VerifyWithPem(payload, sig,
            rsa.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void PublicKeyPem_IsAValidRsa2048Key()
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(LicenseSignature.PublicKeyPem);

        Assert.Equal(2048, rsa.KeySize);
    }

    [Fact]
    public void PublicKeyPem_ContainsNoPrivateMaterial()
    {
        Assert.DoesNotContain("PRIVATE", LicenseSignature.PublicKeyPem);
        Assert.StartsWith("-----BEGIN PUBLIC KEY-----",
            LicenseSignature.PublicKeyPem.Trim());
    }

    // ─────────────────────── LicenseManager: الملف ───────────────────────

    [Fact]
    public void Activate_RejectsKeyFromAnotherKeyPair()
    {
        // مفيشش private key في الريبو، فالطريقة الوحيدة ναختبر المسار
        // المظبوط end-to-end هنا هي نتأكد إن أي pair تاني يترفض.
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);
        using RSA attacker = RSA.Create(2048);

        string forged = LicenseSignature.BuildKey(
            attacker, LicenseManager.GetHwid(), LicenseSignature.Permanent);

        Assert.False(LicenseManager.Activate(forged));
        Assert.False(File.Exists(_licensePath));
    }

    [Fact]
    public void Activate_RejectsMalformedKey()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);

        Assert.False(LicenseManager.Activate(""));
        Assert.False(LicenseManager.Activate("   "));
        Assert.False(LicenseManager.Activate("no-separator-here"));
        Assert.False(LicenseManager.Activate("20271231|AAAA"));
    }

    [Fact]
    public void Activate_RejectsKeyNotSignedForThisDevice()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);
        using RSA rsa = RSA.Create(2048);

        string otherDevice = LicenseSignature.BuildKey(rsa, NewHwid(), LicenseSignature.Permanent);

        Assert.False(LicenseManager.Activate(otherDevice));
        Assert.False(File.Exists(_licensePath));
    }

    [Fact]
    public void TryCheck_MissingFile()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);

        Assert.False(LicenseManager.TryCheck(out string reason));
        Assert.Equal("NO_LICENSE_FILE", reason);
    }

    [Fact]
    public void TryCheck_RejectsHandEditedPermanentExpiry()
    {
        // الترخيص القديم كان: سطر 3 = تاريخ الانتهاء. حد بيفتح الملف
        // وبيكتب PERMANET بياخد ترخيص دائم. لازم يترفض دلوقتي.
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);
        using RSA rsa = RSA.Create(2048);
        string hwid = LicenseManager.GetHwid();
        string key = LicenseSignature.BuildKey(rsa, hwid, "20260101");

        File.WriteAllLines(_licensePath, new[]
        {
            hwid,
            key,
            "2025-01-01 00:00:00"
        });

        Assert.False(LicenseManager.TryCheck(out string reason));
        Assert.Equal("BAD_SIGNATURE", reason);
    }

    [Fact]
    public void TryCheck_RejectsExpiryEditedInTheKeyItself()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);
        using RSA rsa = RSA.Create(2048);
        string hwid = LicenseManager.GetHwid();
        LicenseSignature.TrySplitKey(
            LicenseSignature.BuildKey(rsa, hwid, "20260101"),
            out _, out string signature);

        // حد عدّل تاريخ الانتهاء جوّه السطر 1
        File.WriteAllLines(_licensePath, new[]
        {
            hwid,
            LicenseSignature.Permanent + LicenseSignature.ExpirySeparator + signature,
            "2025-01-01 00:00:00"
        });

        Assert.False(LicenseManager.TryCheck(out string reason));
        Assert.Equal("BAD_SIGNATURE", reason);
    }

    [Fact]
    public void TryCheck_SignatureCheckFiresBeforeTheDeviceCheck()
    {
        // الملفات منسوخ لجهاز تاني: الـ HWID اتغيّر في السطر 0. التوقيع
        // مبني على الـ HWID الأصلي، فيفشل التحقق قبل ما نوصل لخط مقارنة
        // الأجهزة. ده المطلوب — fail fast، وماعندناش path بيوصل لـ
        // WRONG_DEVICE غير بتوقيع صالح لجهاز تاني (مستحيل من غير المفتاح).
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);
        using RSA rsa = RSA.Create(2048);
        string otherHwid = NewHwid();
        string key = LicenseSignature.BuildKey(rsa, otherHwid, LicenseSignature.Permanent);

        File.WriteAllLines(_licensePath, new[]
        {
            LicenseManager.GetHwid(),
            key,
            "2025-01-01 00:00:00"
        });

        Assert.False(LicenseManager.TryCheck(out string reason));
        Assert.Equal("BAD_SIGNATURE", reason);
    }

    [Fact]
    public void TryCheck_RejectsTruncatedFile()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);

        File.WriteAllLines(_licensePath, new[] { LicenseManager.GetHwid(), "20271231|AAAA" });

        Assert.False(LicenseManager.TryCheck(out string reason));
        Assert.Equal("MALFORMED_FILE", reason);
    }

    [Fact]
    public void TryCheck_RejectsLegacyFourLineHmacFile()
    {
        // ملف بالنظام القديم: 4 أسطر ومفتاح HMAC بـ16 محرف. الشكل بقى
        // 3 أسطر، بس الاتنين بيرفضوه — المهم إن المفاتيح القديمة ماتت.
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);

        File.WriteAllLines(_licensePath, new[]
        {
            LicenseManager.GetHwid(), "legacy-hmac-key", "2025-01-01", "2027-12-31"
        });

        Assert.False(LicenseManager.TryCheck(out string reason));
        Assert.Equal("BAD_KEY_FORMAT", reason);
    }

    [Fact]
    public void Deactivate_RemovesTheFile()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);
        File.WriteAllText(_licensePath, "anything");

        LicenseManager.Deactivate();

        Assert.False(File.Exists(_licensePath));
    }

    [Fact]
    public void Deactivate_OnMissingFileDoesNotThrow()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);

        LicenseManager.Deactivate();
    }

    [Fact]
    public void GetExpiryInfo_DoesNotThrowOnAnyState()
    {
        using IDisposable scope = LicenseManager.OverridePath(_licensePath);

        Assert.Equal("NO_LICENSE", LicenseManager.GetExpiryInfo());

        File.WriteAllLines(_licensePath, new[]
        {
            LicenseManager.GetHwid(), "garbage", "2025-01-01", "2027-12-31"
        });
        _ = LicenseManager.GetExpiryInfo();

        File.WriteAllText(_licensePath, "truncated");
        _ = LicenseManager.GetExpiryInfo();
    }
}
