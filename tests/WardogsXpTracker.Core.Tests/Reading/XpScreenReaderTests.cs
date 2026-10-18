using WardogsXpTracker.Core.Reading;
using Xunit;

namespace WardogsXpTracker.Core.Tests.Reading;

public class XpScreenReaderTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 10, 16, 12, 30, 0, TimeSpan.FromHours(-7));

    [Theory]
    [InlineData(XpClass.Assault, 15, 56486, 64500)]
    [InlineData(XpClass.Medic, 15, 58891, 64500)]
    [InlineData(XpClass.Recon, 11, 31175, 33750)]
    [InlineData(XpClass.Support, 14, 48059, 54750)]
    [InlineData(XpClass.Driver, 15, 59653, 64500)]
    [InlineData(XpClass.Pilot, 14, 44702, 54750)]
    public void ReadsAllSixReferenceTotals(XpClass @class, int level, long xp, long threshold)
    {
        var frame = Unlock(@class, level, L($"LEVEL {level + 1}", .34, .18, .09),
            L($"{xp:N0}/{threshold:N0}XP", .34, .21, .10));

        var result = XpScreenReader.Read(frame, new(@class, CurrentLevel: level));

        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(ScreenKind.Unlock, result.Screen);
        var reading = Assert.Single(result.Readings);
        Assert.Equal(@class, reading.Class);
        Assert.Equal(xp, reading.CumulativeXp);
        Assert.Equal(threshold, reading.NextThreshold);
        Assert.Equal(level, reading.CurrentLevel);
        Assert.Equal(level + 1, reading.ThresholdLevel);
        Assert.Equal(CapturedAt, reading.CapturedAt);
    }

    [Theory]
    [InlineData(XpClass.Assault)]
    [InlineData(XpClass.Driver)]
    public void RejectsCompletedPageInsteadOfReadingTheYellowReward(XpClass @class)
    {
        var frame = Unlock(@class, 15,
            L("LEVEL 11", .34, .18), L("28,750/28,750XP", .34, .21),
            L("LEVEL 15", .80, .18), L("54,750/54,750XP", .80, .21));

        var result = XpScreenReader.Read(frame, new(@class, CurrentLevel: 15));

        Assert.Equal(ReadStatus.NoCurrentXp, result.Status);
        Assert.Empty(result.Readings);
        Assert.Contains(result.Thresholds, t => t.Level == 15 && t.CumulativeXp == 54750);
    }

    [Fact]
    public void ReconReadsTheNextColumnDespiteTheSelectedCompletedReward()
    {
        var frame = Unlock(XpClass.Recon, 11,
            L("LEVEL 11", .34, .18), L("28,750/28,750XP", .34, .21),
            L("LEVEL 12", .46, .18), L("31,175/33,750XP", .46, .21));

        var result = XpScreenReader.Read(frame, new(XpClass.Recon, CurrentLevel: 11));

        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(31175, Assert.Single(result.Readings).CumulativeXp);
    }

    [Fact]
    public void CurrentLevelCanComeFromTheSelectedSidebarCard()
    {
        var frame = Unlock(XpClass.Recon, 11,
            L("LEVEL 12", .46, .18), L("31,175/33,750XP", .46, .21));
        var result = XpScreenReader.Read(frame, new(XpClass.Recon));
        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(11, Assert.Single(result.Readings).CurrentLevel);
    }

    [Fact]
    public void UnknownSidebarLevelDoesNotInventACurrentLevel()
    {
        var frame = Unlock(XpClass.Recon, 11,
            L("LEVEL 12", .46, .18), L("31,175/33,750XP", .46, .21));
        frame = frame with { Lines = frame.Lines.Where(line => line.Text != "11").ToArray() };

        var result = XpScreenReader.Read(frame, new(XpClass.Recon));

        Assert.True(result.IsAccepted, result.Message);
        var reading = Assert.Single(result.Readings);
        Assert.Equal(31175, reading.CumulativeXp);
        Assert.Null(reading.CurrentLevel);
        Assert.Equal(12, reading.ThresholdLevel);
    }

    [Fact]
    public void UnknownLevelLabelsRemainUnknownWhileExactXpIsRetained()
    {
        var frame = Unlock(XpClass.Recon, 11, L("31,175/33,750XP", .46, .21));
        frame = frame with { Lines = frame.Lines.Where(line => line.Text != "11").ToArray() };

        var result = XpScreenReader.Read(frame, new(XpClass.Recon));

        Assert.True(result.IsAccepted, result.Message);
        var reading = Assert.Single(result.Readings);
        Assert.Equal(31175, reading.CumulativeXp);
        Assert.Null(reading.CurrentLevel);
        Assert.Null(reading.ThresholdLevel);
    }

    [Fact]
    public void GenuineBoundaryUsesTheNextThresholdNotTheCompletedReward()
    {
        var frame = Unlock(XpClass.Recon, 12,
            L("LEVEL 12", .46, .18), L("33,750/33,750XP", .46, .21),
            L("LEVEL 13", .58, .18), L("33,750/37,500XP", .58, .21));
        var result = XpScreenReader.Read(frame, new(XpClass.Recon, CurrentLevel: 12));
        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(33750, Assert.Single(result.Readings).CumulativeXp);
        Assert.Equal(37500, Assert.Single(result.Readings).NextThreshold);
    }

    [Fact]
    public void ACapOrBoundaryWithoutCurrentProgressRemainsUnresolved()
    {
        var frame = Unlock(XpClass.Assault, 15, L("54,750/54,750XP", .80, .21));
        Assert.Equal(ReadStatus.NoCurrentXp, XpScreenReader.Read(frame, new(XpClass.Assault)).Status);
    }

    [Fact]
    public void RejectsWrongClassDuringTargetedCapture()
    {
        var frame = Unlock(XpClass.Medic, 15, L("58,891/64,500XP", .34, .21));
        var result = XpScreenReader.Read(frame, new(XpClass.Medic, XpClass.Assault, 15));
        Assert.Equal(ReadStatus.WrongClass, result.Status);
        Assert.Empty(result.Readings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesNotUseTheAggregateWardogLevel(bool aggregate)
    {
        var frame = Unlock(XpClass.Medic, 15, L("58,891/64,500XP", .34, .21));
        var result = XpScreenReader.Read(frame, new(aggregate ? XpClass.Medic : null,
            IsAggregateTrackSelected: aggregate));
        Assert.Equal(ReadStatus.ClassNotEstablished, result.Status);
        Assert.Empty(result.Readings);
    }

    [Fact]
    public void RejectsMultiplePartialValues()
    {
        var frame = Unlock(XpClass.Recon, 11, L("31,175/33,750XP", .46, .21),
            L("31,176/37,500XP", .58, .21));
        var result = XpScreenReader.Read(frame, new(XpClass.Recon));
        Assert.Equal(ReadStatus.Ambiguous, result.Status);
        Assert.Empty(result.Readings);
    }

    [Fact]
    public void RejectsNumeratorAboveThreshold()
    {
        var frame = Unlock(XpClass.Recon, 11, L("38,175/33,750XP", .46, .21));
        Assert.Equal(ReadStatus.InvalidNumber, XpScreenReader.Read(frame, new(XpClass.Recon)).Status);
    }

    [Fact]
    public void RejectsContradictoryVisibleLevels()
    {
        var frame = Unlock(XpClass.Recon, 11, L("LEVEL 14", .46, .18), L("31,175/33,750XP", .46, .21));
        Assert.Equal(ReadStatus.InconsistentLevel, XpScreenReader.Read(frame, new(XpClass.Recon)).Status);
    }

    [Fact]
    public void RejectsUnclearDigitsInsteadOfAcceptingAnExactLookingValue()
    {
        var frame = Unlock(XpClass.Recon, 11, L("31,175/33,750XP", .46, .21) with { Confidence = .42 });
        Assert.Equal(ReadStatus.LowConfidence, XpScreenReader.Read(frame, new(XpClass.Recon)).Status);
    }

    [Fact]
    public void UsesWordBoundsWhenOneOcrLineSpansManyColumns()
    {
        var row = new OcrLine("ignored original text", new(.34, .21, .57, .02),
        [
            new("28,750/28,750XP", new(.34, .21, .10, .02)),
            new("31,175/33,750XP", new(.46, .21, .10, .02)),
            new("37,500XP", new(.58, .21, .08, .02))
        ]);
        var result = XpScreenReader.Read(Unlock(XpClass.Recon, 11, row), new(XpClass.Recon));
        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(.46, Assert.Single(result.Readings).Bounds.X);
    }

    [Fact]
    public void ReadsAllSixFinalCardsWithoutTreatingThemAsLiveXp()
    {
        var values = new (XpClass Class, int Level, string Pair)[]
        {
            (XpClass.Assault, 15, "XP 56,486 64,500"), (XpClass.Medic, 15, "XP 58,891 64,500"),
            (XpClass.Recon, 11, "XP 31,175 33,750"), (XpClass.Support, 14, "XP 48,059 54,750"),
            (XpClass.Driver, 15, "XP 59,653 64,500"), (XpClass.Pilot, 14, "XP 44,702 54,750")
        };
        var lines = new List<OcrLine> { L("SCORE PROGRESSION SCOREBOARD", .09, .01, .3) };
        for (var i = 0; i < values.Length; i++)
        {
            var x = .10 + i * .14;
            lines.Add(L(values[i].Level.ToString(), x + .035, .49, .025));
            lines.Add(L(values[i].Class.ToString(), x, .55, .10));
            lines.Add(L(values[i].Pair, x, .60, .10));
        }
        var result = XpScreenReader.Read(new(lines, CapturedAt));
        Assert.True(result.IsAccepted, result.Message);
        Assert.True(result.IsEndScreen);
        Assert.Equal(ScreenKind.EndProgression, result.Screen);
        Assert.Equal(6, result.Readings.Count);
        Assert.All(result.Readings, r => Assert.Equal(ScreenKind.EndProgression, r.Screen));
        Assert.Equal(59653, result.Readings.Single(r => r.Class == XpClass.Driver).CumulativeXp);
    }

    [Fact]
    public void FinalCardCanBeAbsentWithoutInventingZeroXp()
    {
        var frame = Frame(L("SCORE PROGRESSION SCOREBOARD", .09, .01, .3),
            L("MEDIC", .30, .55), L("15", .335, .49, .025), L("XP 58,891 64,500", .30, .60));
        var result = XpScreenReader.Read(frame);
        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(XpClass.Medic, Assert.Single(result.Readings).Class);
    }

    [Theory]
    [InlineData("VICTORY", null, 1)]
    [InlineData("DEFEAT", "2ND PLACE + CONTRIBUTION", 2)]
    [InlineData("DEFEAT", "3RD PLACE + CONTRIBUTION", 3)]
    public void ReadsThePlayersPlacement(string title, string? contribution, int place)
    {
        var lines = new List<OcrLine> { L("SCORE PROGRESSION SCOREBOARD", .09, .01, .3), L(title, .33, .12, .33) };
        if (contribution is not null) lines.Add(L(contribution, .34, .58, .25));
        var result = XpScreenReader.Read(new(lines, CapturedAt));
        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(ScreenKind.EndScore, result.Screen);
        Assert.Equal(new[] { place }, result.Placement.PossiblePlaces);
        Assert.Empty(result.Readings);
    }

    [Fact]
    public void DefeatAloneLeavesSecondAndThirdPossible()
    {
        var result = XpScreenReader.Read(Frame(L("NEXT ROUND IN 00:32", .4, .86), L("DEFEAT", .33, .12, .33)));
        Assert.True(result.IsAccepted, result.Message);
        Assert.Equal(new[] { 2, 3 }, result.Placement.PossiblePlaces);
        Assert.False(result.Placement.IsKnown);
    }

    [Fact]
    public void ContradictoryPlacementNeedsReview()
    {
        var result = XpScreenReader.Read(Frame(L("NEXT ROUND IN 00:32", .4, .86),
            L("VICTORY", .33, .12, .33), L("3RD PLACE + CONTRIBUTION", .34, .58, .25)));
        Assert.Equal(ReadStatus.Ambiguous, result.Status);
        Assert.True(result.Placement.HasConflict);
        Assert.Empty(result.Placement.PossiblePlaces);
    }

    [Fact]
    public void WinnerFactionSplashDoesNotEstablishPlayerPlacement()
    {
        var result = XpScreenReader.Read(Frame(L("MANTICORE WINS", .33, .40, .5)));
        Assert.Equal(ReadStatus.UnsupportedScreen, result.Status);
        Assert.Empty(result.Placement.Observations);
    }

    [Fact]
    public void VictoryInChatDoesNotEstablishPlacement()
    {
        var result = XpScreenReader.Read(Frame(L("[ALL] Player: VICTORY", .02, .1, .18),
            L("NEXT ROUND IN 00:32", .4, .86)));
        Assert.Equal(ReadStatus.UnsupportedScreen, result.Status);
        Assert.Empty(result.Placement.Observations);
    }

    private static OcrFrame Unlock(XpClass selected, int level, params OcrLine[] content)
    {
        var lines = new List<OcrLine>
        {
            L("UNLOCK", .165, .015), L("// WARDOG PROGRESSION", .31, .12, .3),
            L(level.ToString(), .105, .43, .02), L(selected.ToString(), .08, .47, .075),
            L(selected == XpClass.Pilot ? "RECON" : "PILOT", .185, .65, .075)
        };
        lines.AddRange(content);
        return new(lines, CapturedAt);
    }
    private static OcrFrame Frame(params OcrLine[] lines) => new(lines, CapturedAt);
    private static OcrLine L(string text, double x, double y, double width = .10)
        => new(text, new(x, y, width, .025));
}
