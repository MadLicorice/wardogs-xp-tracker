using WardogsXpTracker.Core.Reading;
using Xunit;

namespace WardogsXpTracker.Core.Tests.Reading;

public class XpNumberParserTests
{
    [Theory]
    [InlineData("56,486/64,500XP", 56486, 64500)]
    [InlineData("56,486 / 64,500 XP", 56486, 64500)]
    [InlineData("XP 56,486 64,500", 56486, 64500)]
    [InlineData("XP56,48664,500", 56486, 64500)]
    [InlineData("XP 31,175 33,750", 31175, 33750)]
    [InlineData("0/500XP", 0, 500)]
    public void ReadsExactNumericFormats(string text, long current, long threshold)
    {
        var pair = Assert.Single(XpNumberParser.FindPairs(text));
        Assert.Equal(current, pair.Current);
        Assert.Equal(threshold, pair.Threshold);
    }

    [Theory]
    [InlineData("56,48O/64,500XP")]
    [InlineData("56,48/64,500XP")]
    [InlineData("56,486/64,50XP")]
    [InlineData("XP5648664500")]
    [InlineData("56.486/64.500XP")]
    [InlineData("XP 56,486 64,50")]
    public void RefusesMalformedOrAmbiguousDigits(string text)
        => Assert.Empty(XpNumberParser.FindPairs(text));

    [Fact]
    public void ReadsEachPairOnOneOcrLine()
    {
        var pairs = XpNumberParser.FindPairs("28,750/28,750XP 31,175/33,750XP 37,500XP");
        Assert.Equal(2, pairs.Count);
        Assert.Equal(31175, pairs[1].Current);
    }

    [Theory]
    [InlineData("1,23")]
    [InlineData("12,3456")]
    [InlineData("-20")]
    [InlineData("20.0")]
    [InlineData("9223372036854775808")]
    public void RefusesInvalidInteger(string text)
        => Assert.False(XpNumberParser.TryParseInteger(text, out _));
}
