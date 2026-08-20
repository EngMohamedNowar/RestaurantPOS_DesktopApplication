using PizzaPOS.Services;

namespace PizzaPOS.Tests;

public class UserServiceTests
{
    [Fact]
    public void HashPin_ReturnsDifferentHashesForSamePin()
    {
        string hash1 = UserService.HashPin("1234");
        string hash2 = UserService.HashPin("1234");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPin_CorrectPin_ReturnsTrue()
    {
        string hash = UserService.HashPin("1234");
        Assert.True(UserService.VerifyPin("1234", hash));
    }

    [Fact]
    public void VerifyPin_WrongPin_ReturnsFalse()
    {
        string hash = UserService.HashPin("1234");
        Assert.False(UserService.VerifyPin("5678", hash));
    }

    [Fact]
    public void VerifyPin_LegacySHA256_MigratesCorrectly()
    {
        // admin's PIN is "1234" (SHA256: 03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4)
        string legacyHash = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4";
        Assert.True(UserService.VerifyPin("1234", legacyHash));
        Assert.False(UserService.VerifyPin("5678", legacyHash));
    }

    [Fact]
    public void VerifyPin_EmptyHash_ReturnsFalse()
    {
        Assert.False(UserService.VerifyPin("1234", ""));
    }

    [Fact]
    public void VerifyPin_InvalidBase64_ReturnsFalse()
    {
        Assert.False(UserService.VerifyPin("1234", "not-valid-base64!"));
    }
}
