using PizzaPOS.Services;

namespace PizzaPOS.Tests;

public class EpsonServiceTests
{
    [Fact]
    public void SplitNotes_MultiLineNotes_PrintOneLineEach()
    {
        var lines = EpsonService.SplitNotes("بدون بصل\r\nتوصيل: اتصل قبل الصعود");

        Assert.Equal(new[] { "بدون بصل", "توصيل: اتصل قبل الصعود" }, lines);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n \n")]
    public void SplitNotes_NothingToPrint_ReturnsNoLines(string? notes)
    {
        Assert.Empty(EpsonService.SplitNotes(notes));
    }

    [Fact]
    public void SplitNotes_BlankMiddleLines_AreDropped()
    {
        var lines = EpsonService.SplitNotes("ملاحظة أولى\n   \n\nملاحظة تانية");

        Assert.Equal(new[] { "ملاحظة أولى", "ملاحظة تانية" }, lines);
    }
}
