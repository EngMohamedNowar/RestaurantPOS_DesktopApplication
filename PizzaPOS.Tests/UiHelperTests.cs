using System.Windows.Media;
using PizzaPOS.Helpers;

namespace PizzaPOS.Tests;

public class UiHelperTests
{
    [Fact]
    public void B_ReturnsSolidColorBrush()
    {
        var brush = UiHelper.B("#FF6B35");
        Assert.NotNull(brush);
        Assert.IsType<SolidColorBrush>(brush);
    }

    [Fact]
    public void B_ParsesHexColors()
    {
        var red = UiHelper.B("#FF0000");
        Assert.Equal(255, red.Color.R);
        Assert.Equal(0, red.Color.G);
        Assert.Equal(0, red.Color.B);

        var green = UiHelper.B("#00FF00");
        Assert.Equal(0, green.Color.R);
        Assert.Equal(255, green.Color.G);
        Assert.Equal(0, green.Color.B);
    }

    [Fact]
    public void B_DifferentInputs_DifferentOutputs()
    {
        var a = UiHelper.B("#FF0000");
        var b = UiHelper.B("#00FF00");
        Assert.NotEqual(a.Color, b.Color);
    }
}
