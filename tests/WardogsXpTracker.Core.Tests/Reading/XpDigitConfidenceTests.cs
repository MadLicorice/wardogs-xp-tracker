using WardogsXpTracker.Core.Reading;
using Xunit;

namespace WardogsXpTracker.Core.Tests.Reading;

public class XpDigitConfidenceTests
{
    [Fact]
    public void WeakestDigitAcrossBothFieldsSetsTheConfidence()
    {
        var symbols = Digits("5889164500");
        symbols[8] = new("0", .51);
        Assert.Equal(.51, XpDigitConfidence.Minimum("XP 58,891 64,500", [new("X", 0), new("P", 0), .. symbols]));
    }

    [Fact]
    public void MissingCurrentDigitCannotUseThresholdConfidenceAlone() =>
        Assert.Null(XpDigitConfidence.Minimum("XP 58,891 64,500", Digits("889164500")));

    [Fact]
    public void ChangedDigitCannotSupplyConfidenceForTheRecognizedText() =>
        Assert.Null(XpDigitConfidence.Minimum("59,653/64,500XP", Digits("5965564500")));

    [Fact]
    public void ExtraDigitCannotBeSilentlyIgnored() =>
        Assert.Null(XpDigitConfidence.Minimum("59,653/64,500XP", Digits("59653645000")));

    [Fact]
    public void MissingOrInvalidDigitConfidenceCannotSupplySupport()
    {
        foreach (var confidence in new double?[] { null, double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1, 1.01 })
        {
            var symbols = Digits("5965364500");
            symbols[4] = new("3", confidence);
            Assert.Null(XpDigitConfidence.Minimum("59,653/64,500XP", symbols));
        }
    }

    [Fact]
    public void MultipleDigitsInOneSymbolHaveAmbiguousAttribution() =>
        Assert.Null(XpDigitConfidence.Minimum("59/64XP", [new("59", .99), new("6", .99), new("4", .99)]));

    [Fact]
    public void TextWithoutDigitsCannotSupplySupport() =>
        Assert.Null(XpDigitConfidence.Minimum("XP", [new("X", .99), new("P", .99)]));

    [Fact]
    public void WeakCurrentDigitCannotHideBehindAStrongThreshold()
    {
        var symbols = Digits("5889164500");
        symbols[4] = new("1", .01);
        var confidence = XpDigitConfidence.Minimum("58,891/64,500XP", symbols);
        var result = XpReadingConsensus.Resolve([
            new("a", new(58891, 64500, 0, 0, ""), confidence),
            new("b", new(58891, 64500, 0, 0, ""), confidence)]);
        Assert.Equal(.01, confidence);
        Assert.False(result.IsConfirmed);
    }

    [Fact]
    public void EqualCurrentAndThresholdNeedBothOccurrences()
    {
        Assert.Null(XpDigitConfidence.Minimum("54,750/54,750XP", Digits("54750")));
        var symbols = Digits("5475054750");
        symbols[9] = new("0", .42);
        Assert.Equal(.42, XpDigitConfidence.Minimum("54,750/54,750XP", symbols));
    }

    private static XpDigitObservation[] Digits(string digits) =>
        digits.Select(digit => new XpDigitObservation(digit.ToString(), .99)).ToArray();
}
