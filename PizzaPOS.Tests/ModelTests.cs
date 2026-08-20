using PizzaPOS.Models;

namespace PizzaPOS.Tests;

public class ModelTests
{
    [Fact]
    public void Customer_Display_FormatsCorrectly()
    {
        var c = new Customer { Name = "أحمد", Phone = "01234567890" };
        Assert.Equal("أحمد — 01234567890", c.Display);
    }

    [Fact]
    public void Customer_LoyaltyTier_Bronze_Under1000()
    {
        var c = new Customer { TotalSpent = 500 };
        Assert.Equal("🥉 برونزي", c.LoyaltyTier);
    }

    [Fact]
    public void Customer_LoyaltyTier_Silver_1000To1999()
    {
        var c = new Customer { TotalSpent = 1500 };
        Assert.Equal("🥈 فضي", c.LoyaltyTier);
    }

    [Fact]
    public void Customer_LoyaltyTier_Gold_2000To4999()
    {
        var c = new Customer { TotalSpent = 3000 };
        Assert.Equal("🥇 ذهبي", c.LoyaltyTier);
    }

    [Fact]
    public void Customer_LoyaltyTier_Diamond_5000Plus()
    {
        var c = new Customer { TotalSpent = 6000 };
        Assert.Equal("💎 ماسي", c.LoyaltyTier);
    }

    [Fact]
    public void Ingredient_IsLow_TrueWhenAtMin()
    {
        var i = new Ingredient { Stock = 3, MinStock = 3 };
        Assert.True(i.IsLow);
    }

    [Fact]
    public void Ingredient_IsLow_FalseWhenAbove()
    {
        var i = new Ingredient { Stock = 5, MinStock = 3 };
        Assert.False(i.IsLow);
    }

    [Fact]
    public void User_IsAdmin_TrueForAdmin()
    {
        var u = new User { Role = "admin" };
        Assert.True(u.IsAdmin);
    }

    [Fact]
    public void User_IsManager_ForAdminAndManager()
    {
        var admin = new User { Role = "admin" };
        var manager = new User { Role = "manager" };
        var cashier = new User { Role = "cashier" };

        Assert.True(admin.IsManager);
        Assert.True(manager.IsManager);
        Assert.False(cashier.IsManager);
    }

    [Fact]
    public void OrderType_Delivery_IsCorrect()
    {
        Assert.Equal("ديلفري", OrderType.Delivery);
    }

    [Fact]
    public void OrderType_DineIn_IsCorrect()
    {
        Assert.Equal("صالة", OrderType.DineIn);
    }

    [Fact]
    public void PayMethod_Cash_IsCorrect()
    {
        Assert.Equal("كاش", PayMethod.Cash);
    }

    [Fact]
    public void PayMethod_Card_IsCorrect()
    {
        Assert.Equal("فيزا/ماستر", PayMethod.Card);
    }

    [Fact]
    public void OrderStatus_Constants_AreCorrect()
    {
        Assert.Equal("new", OrderStatus.New);
        Assert.Equal("completed", OrderStatus.Completed);
        Assert.Equal("cancelled", OrderStatus.Cancelled);
    }
}
