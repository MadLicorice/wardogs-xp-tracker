using WardogsXpTracker.Core.Reading;
using Xunit;

namespace WardogsXpTracker.Core.Tests.Reading;

public class XpReadingConsensusTests
{
    [Fact]
    public void ReportedDriverMajorityCannotChooseTheWrongLastDigit()
    {
        var result = XpReadingConsensus.Resolve([
            Pass("gray-3", 59655, .624), Pass("gray-4", 59653, .829),
            Pass("threshold120-3", 59653, .305), Pass("threshold140-3", 59655, .239),
            Pass("threshold120-4", 59655, .410)]);
        Assert.True(result.HasConflict);
        Assert.False(result.IsConfirmed);
        Assert.Null(result.Pair);
    }

    [Fact]
    public void LowConfidenceConflictCannotBeFilteredAway()
    {
        var result = XpReadingConsensus.Resolve([
            Pass("a", 59653, .95), Pass("b", 59653, .97), Pass("c", 59655, .01)]);
        Assert.True(result.HasConflict);
        Assert.Null(result.Pair);
    }

    [Fact]
    public void TwoReadablePassesCanAgreeDespiteAnUnreadablePass()
    {
        var result = XpReadingConsensus.Resolve([Pass("a", 59653, .8), Pass("b", 59653, .9), new("c", null)]);
        Assert.True(result.IsConfirmed);
        Assert.Equal(59653, result.Pair!.Current);
        Assert.Equal(2, result.AgreeingPasses);
    }

    [Fact]
    public void DuplicatePassIdsCannotSupplyExtraSupport()
    {
        var result = XpReadingConsensus.Resolve([Pass("same", 59653, .9), Pass("same", 59653, .9)]);
        Assert.False(result.IsConfirmed);
        Assert.Equal(1, result.AgreeingPasses);
    }

    [Fact]
    public void DisagreementAboutTheThresholdAlsoRejectsTheNumber()
    {
        var result = XpReadingConsensus.Resolve([Pass("a", 59653, .9), new("b", new(59653, 64501, 0, 0, ""), .9)]);
        Assert.True(result.HasConflict);
        Assert.Null(result.Pair);
    }

    [Fact]
    public void MultipleCandidatesInOnePassCannotHideAConflict()
    {
        var result = XpReadingConsensus.Resolve([
            Pass("a", 59653, .9), Pass("a", 59655, .9), Pass("b", 59653, .9), Pass("c", 59653, .9)]);
        Assert.True(result.HasConflict);
        Assert.Null(result.Pair);
    }

    [Fact]
    public void BlankOrLowConfidenceEvidenceCannotEstablishXp()
    {
        Assert.False(XpReadingConsensus.Resolve([]).IsConfirmed);
        Assert.False(XpReadingConsensus.Resolve([new("a", null), new("b", null)]).IsConfirmed);
        Assert.False(XpReadingConsensus.Resolve([Pass("a", 59653, .1), Pass("b", 59653, .2)]).IsConfirmed);
        Assert.False(XpReadingConsensus.Resolve([Pass("a", 59653, .9), new("b", Pass("b", 59653, .9).Pair)]).IsConfirmed);
    }

    [Fact]
    public void PassOrderDoesNotChooseOneOfTheConflictingValues()
    {
        XpConsensusObservation[] passes = [Pass("a", 59653, .9), Pass("b", 59655, .8), Pass("c", 59655, .8)];
        Assert.Equal(XpReadingConsensus.Resolve(passes), XpReadingConsensus.Resolve(passes.Reverse()));
    }

    [Fact]
    public void InvalidConfidenceCannotSupplyASecondVote()
    {
        foreach (var confidence in new double?[] { null, double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1, 1.01 })
        {
            var result = XpReadingConsensus.Resolve([
                Pass("a", 59653, .9), new("b", Pass("b", 59653, .9).Pair, confidence)]);
            Assert.False(result.IsConfirmed);
            Assert.Equal(1, result.AgreeingPasses);
        }
    }

    [Fact]
    public void ConfidenceFloorIncludesTheBoundary()
    {
        Assert.True(XpReadingConsensus.Resolve([Pass("a", 59653, .5), Pass("b", 59653, .5)]).IsConfirmed);
        Assert.False(XpReadingConsensus.Resolve([Pass("a", 59653, .5), Pass("b", 59653, .499)]).IsConfirmed);
    }

    [Fact]
    public void InvalidNumericPairsCannotSupplySupport()
    {
        XpNumberPair[] invalid = [new(-1, 64500, 0, 0, ""), new(0, 0, 0, 0, ""), new(64501, 64500, 0, 0, "")];
        foreach (var pair in invalid)
        {
            var result = XpReadingConsensus.Resolve([Pass("a", 59653, .9), new("b", pair, .9)]);
            Assert.False(result.IsConfirmed);
            Assert.False(result.HasConflict);
            Assert.Equal(1, result.AgreeingPasses);
        }
    }

    [Fact]
    public void HigherMinimumNeedsEnoughDistinctQualifiedPasses()
    {
        XpConsensusObservation[] two = [Pass("a", 59653, .9), Pass("b", 59653, .9)];
        Assert.False(XpReadingConsensus.Resolve(two, minimumPasses: 3).IsConfirmed);
        Assert.True(XpReadingConsensus.Resolve([.. two, Pass("c", 59653, .9)], minimumPasses: 3).IsConfirmed);
        Assert.False(XpReadingConsensus.Resolve([.. two, Pass("b", 59653, .9)], minimumPasses: 3).IsConfirmed);
    }

    private static XpConsensusObservation Pass(string id, long current, double confidence) =>
        new(id, new(current, 64500, 0, 0, $"{current}/64500XP"), confidence);
}
