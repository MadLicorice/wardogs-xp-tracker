using System.Text.Json;
using WardogsXpTracker.Core.Analytics;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Tests.Analytics;

public sealed class AnalyticsEngineTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConfirmedDefeatShowsCommonSourcesAndCoverage()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 12_500);
        Finish(tracker, 16_500, 2, 3);
        var report = AnalyticsEngine.Build(tracker.Document, At);
        var row = Row(report);
        Assert.True(row.IncludedInReceived);
        Assert.Equal(new XpSourceTotals(3000, 3000, 500), report.Totals);
        Assert.Equal(6500, report.Totals.TotalXp);
        Assert.Equal(2500, row.SupportedEarnedLowerBound);
        Assert.Equal(1, report.Coverage.IncludedRecords);
        Assert.Equal(5, report.Coverage.DisabledRecords);
        Assert.Equal(6, report.Coverage.ClassRecords);
        Assert.Equal(AnalyticsEngine.CurrentDerivationVersion, report.DerivationVersion);
        Assert.Equal(At, report.GeneratedAt);
    }

    [Fact]
    public void UnknownCompletionAndUnknownPlacementKeepNoBonusPossible()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500);
        tracker.End(SessionOutcome.Unknown);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(new XpSourceTotals(2500, 0, 4000), report.Totals);
        Assert.Null(Row(report).MissedBonus);
    }

    [Fact]
    public void PlacementEvidenceStillProvesCompletionWhenOutcomeLabelIsUnknown()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500, 3);
        tracker.End(SessionOutcome.Unknown);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(new XpSourceTotals(3500, 3000, 0), report.Totals);
    }

    [Fact]
    public void ConfirmedFinishedWithoutPlacementDoesNotIncludeNoBonus()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(new XpSourceTotals(2500, 3000, 1000), report.Totals);
        Assert.DoesNotContain(Row(report).Split!.Solutions, split => split.Outcome == BonusOutcome.NoBonus);
    }

    [Fact]
    public void OriginalBaselineSurvivesANewMeasuringInterval()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 12_000);
        tracker.ArmRebase(XpClass.Assault);
        tracker.ManualBaseline(XpClass.Assault, 12_000);
        tracker.ManualCurrent(XpClass.Assault, 12_500);
        Finish(tracker, 16_500, 2, 3);
        var row = Row(AnalyticsEngine.Build(tracker.Document));
        Assert.Equal(10_000, row.StartingXp);
        Assert.Equal(2500, row.SupportedEarnedLowerBound);
        Assert.Equal(500, tracker.ActiveSession!.Tracks[XpClass.Assault].MeasuredGain);
        Assert.True(row.IncludedInReceived);
        Assert.Equal(500, row.Split!.IndeterminateXp);
    }

    [Fact]
    public void WrongOriginalBaselineIsExcludedButAFreshIntervalSupportsMissedBonus()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 10_200);
        tracker.ArmRebase(XpClass.Assault, true);
        tracker.ManualBaseline(XpClass.Assault, 20_000);
        tracker.ManualCurrent(XpClass.Assault, 23_000);
        tracker.End(SessionOutcome.LeftMatch);
        var row = Row(AnalyticsEngine.Build(tracker.Document));
        Assert.False(row.IncludedInReceived);
        Assert.Equal(AnalyticsRecordStatus.PartialCoverage, row.Status);
        Assert.Equal(3000, row.SupportedEarnedLowerBound);
        Assert.Equal(3500m, row.MissedBonus!.Average);
        Assert.False(row.MissedBonus.IsLowerBound);
    }

    [Fact]
    public void ARejectedReadingNeverEstablishesAnEarnedBound()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 12_500);
        Assert.False(tracker.ManualCurrent(XpClass.Assault, 9000).Applied);
        Finish(tracker, 16_500, 2, 3);
        var row = Row(AnalyticsEngine.Build(tracker.Document));
        Assert.Equal(2500, row.SupportedEarnedLowerBound);
        Assert.Equal(AnalyticsRecordStatus.Included, row.Status);
    }

    [Fact]
    public void ConfirmedFinalBonusDoesNotBecomeAnEarnedObservationOrGreenCheck()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500, 1);
        var track = tracker.ActiveSession!.Tracks[XpClass.Assault];
        Assert.False(track.TargetReached);
        var before = JsonSerializer.Serialize(tracker.Document);
        var row = Row(AnalyticsEngine.Build(tracker.Document, At));
        Assert.Equal(0, row.SupportedEarnedLowerBound);
        Assert.Equal(2500, row.Split!.MinimumEarnedXp);
        Assert.False(track.TargetReached);
        Assert.Equal(before, JsonSerializer.Serialize(tracker.Document));
    }

    [Fact]
    public void PendingFinalTotalsRemainExcludedAndDoNotBecomeZero()
    {
        var tracker = Tracker();
        tracker.Observe(Read(16_500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        var report = AnalyticsEngine.Build(tracker.Document);
        var row = Row(report);
        Assert.Equal(AnalyticsRecordStatus.PendingFinal, row.Status);
        Assert.Equal(6500, row.TotalGain);
        Assert.False(row.IncludedInReceived);
        Assert.Null(row.Split);
        Assert.Equal(0m, report.Totals.TotalXp);
        Assert.Equal(1, report.Coverage.PendingRecords);
    }

    [Theory]
    [InlineData(false, AnalyticsRecordStatus.UnconfirmedBaseline)]
    [InlineData(true, AnalyticsRecordStatus.MissingBaseline)]
    public void MissingOrUnconfirmedStartingCoverageDoesNotEnterReceivedCharts(bool missing, AnalyticsRecordStatus status)
    {
        var tracker = Tracker(confirmCoverage: false);
        Finish(tracker, 16_500, 1);
        if (missing)
        {
            tracker.ActiveSession!.Tracks[XpClass.Assault].OriginalBaseline = null;
            tracker.ActiveSession.Tracks[XpClass.Assault].Baseline = null;
        }
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(status, Row(report).Status);
        Assert.False(Row(report).IncludedInReceived);
        Assert.Equal(0m, report.Totals.TotalXp);
    }

    [Fact]
    public void MissingClassFinalIsNotAssumedToEarnZero()
    {
        var tracker = Tracker();
        tracker.SetTrackEnabled(XpClass.Medic, true);
        tracker.ManualBaseline(XpClass.Medic, 20_000);
        tracker.ConfirmCoverage(XpClass.Medic);
        Finish(tracker, 16_500, 3);
        var report = AnalyticsEngine.Build(tracker.Document);
        var medic = Assert.Single(report.Records, row => row.Class == XpClass.Medic);
        Assert.Equal(AnalyticsRecordStatus.MissingFinal, medic.Status);
        Assert.Null(medic.EndingXp);
        Assert.Null(medic.TotalGain);
        Assert.Equal(1, report.Coverage.MissingFinalRecords);
    }

    [Fact]
    public void ANewPendingClassCardCannotBorrowConfirmationFromAnotherClass()
    {
        var tracker = Tracker();
        tracker.SetTrackEnabled(XpClass.Medic, true);
        tracker.ManualBaseline(XpClass.Medic, 20_000);
        tracker.ConfirmCoverage(XpClass.Medic);
        Finish(tracker, 16_500, 3);
        tracker.Observe(Read(26_500, ScreenKind.EndProgression, XpClass.Medic), ReadingSource.AutomaticOcr);
        var report = AnalyticsEngine.Build(tracker.Document);
        var medic = Assert.Single(report.Records, row => row.Class == XpClass.Medic);
        Assert.Equal(AnalyticsRecordStatus.PendingFinal, medic.Status);
        Assert.False(medic.IncludedInReceived);
        Assert.Equal(26_500, medic.EndingXp);
        Assert.Equal(6500m, report.Totals.TotalXp);
        Assert.Contains("Unconfirmed final candidate", medic.SourceNotes);
    }

    [Fact]
    public void APendingRevisionDoesNotReplaceAnExistingConfirmedTotal()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500, 3);
        tracker.Observe(Read(17_500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(16_500, Row(report).EndingXp);
        Assert.Equal(6500m, report.Totals.TotalXp);
    }

    [Fact]
    public void DisabledClassKeepsObservedFinalButDoesNotEnterReceivedCharts()
    {
        var tracker = Tracker();
        tracker.SetTrackEnabled(XpClass.Assault, false);
        Finish(tracker, 16_500, 3);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(AnalyticsRecordStatus.Disabled, Row(report).Status);
        Assert.Equal(16_500, Row(report).EndingXp);
        Assert.Equal(0m, report.Totals.TotalXp);
    }

    [Fact]
    public void UnresolvedRulesPreserveTheKnownTotalAsGray()
    {
        var tracker = Tracker();
        Finish(tracker, 11_000, 3);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(AnalyticsRecordStatus.RulesMismatch, Row(report).Status);
        Assert.True(Row(report).IncludedInReceived);
        Assert.Equal(new XpSourceTotals(0, 0, 1000), report.Totals);
        Assert.Equal(1, report.Coverage.ConflictRecords);
    }

    [Fact]
    public void ReliableEarnedBoundCannotBeDiscardedToMakeRulesFit()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 13_501);
        Finish(tracker, 16_500, 3);
        var row = Row(AnalyticsEngine.Build(tracker.Document));
        Assert.Equal(AnalyticsRecordStatus.RulesMismatch, row.Status);
        Assert.Equal(6500, row.Split!.IndeterminateXp);
        Assert.Empty(row.Split.Solutions);
    }

    [Fact]
    public void ConflictingConfirmedEvidencePreservesTheTotalWithoutChoosingAPlacement()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500, 3);
        tracker.ActiveSession!.ConfirmedFinal!.HasConflict = true;
        var row = Row(AnalyticsEngine.Build(tracker.Document));
        Assert.Equal(AnalyticsRecordStatus.ConflictingEvidence, row.Status);
        Assert.Equal(6500, row.Split!.IndeterminateXp);
        Assert.Empty(row.Split.Solutions);
    }

    [Fact]
    public void ADecreasedFinalHasAnExplicitInvalidTotalAndStaysOutsideCharts()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500, 3);
        tracker.ActiveSession!.ConfirmedFinal!.Tracks[XpClass.Assault].Xp = 9000;
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(AnalyticsRecordStatus.InvalidTotal, Row(report).Status);
        Assert.Equal(-1000, Row(report).TotalGain);
        Assert.False(Row(report).IncludedInReceived);
        Assert.Equal(0m, report.Totals.TotalXp);
    }

    [Theory]
    [InlineData(SessionOutcome.LeftMatch)]
    [InlineData(SessionOutcome.Disconnected)]
    public void CompleteNoBonusClosingXpIsEntirelyInMatchXp(SessionOutcome outcome)
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 11_200);
        tracker.End(outcome);
        tracker.RecordClosingXp(XpClass.Assault, 16_500);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(new XpSourceTotals(6500, 0, 0), report.Totals);
        Assert.Equal(3500m, Row(report).MissedBonus!.Average);
        Assert.False(Row(report).MissedBonus!.IsLowerBound);
    }

    [Fact]
    public void PartialExitIsExcludedFromReceivedTotalsButSupportsAtLeastMissedBonus()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 11_200);
        tracker.End(SessionOutcome.Disconnected);
        var report = AnalyticsEngine.Build(tracker.Document);
        var missed = Row(report).MissedBonus!;
        Assert.Equal(AnalyticsRecordStatus.PartialCoverage, Row(report).Status);
        Assert.Equal(0m, report.Totals.TotalXp);
        Assert.Equal(1200, missed.Minimum);
        Assert.Equal(1400m, missed.Average);
        Assert.Equal(1600, missed.Maximum);
        Assert.True(missed.IsLowerBound);
        Assert.Equal(1400m, report.AverageMissedBonus);
    }

    [Fact]
    public void ReachingTheMaximumTierMakesMissedBonusExactWithoutClosingXp()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 13_000);
        tracker.End(SessionOutcome.LeftMatch);
        var missed = Row(AnalyticsEngine.Build(tracker.Document)).MissedBonus!;
        Assert.Equal(3000, missed.SupportedGain);
        Assert.Equal(3500m, missed.Average);
        Assert.True(missed.AtMaximumTier);
        Assert.False(missed.IsLowerBound);
    }

    [Fact]
    public void ACompleteClosingTotalBelowMaximumMakesTheReachedTierExact()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 11_000);
        tracker.End(SessionOutcome.LeftMatch);
        tracker.RecordClosingXp(XpClass.Assault, 11_200);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(1400m, Row(report).MissedBonus!.Average);
        Assert.False(Row(report).MissedBonus!.IsLowerBound);
        Assert.Equal(1200m, report.Totals.EarnedXp);
    }

    [Theory]
    [InlineData(SessionOutcome.Unknown)]
    [InlineData(SessionOutcome.Finished)]
    public void UnknownOrFinishedOutcomesDoNotCreateMissedBonus(SessionOutcome outcome)
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 13_000);
        tracker.End(outcome);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Null(Row(report).MissedBonus);
        Assert.Equal(0m, report.AverageMissedBonus);
    }

    [Fact]
    public void AClosingCorrectionRevisesOneMissedRecordInsteadOfAddingAnother()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 11_200);
        tracker.End(SessionOutcome.LeftMatch);
        tracker.RecordClosingXp(XpClass.Assault, 11_300);
        tracker.RecordClosingXp(XpClass.Assault, 12_100);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(2800m, report.AverageMissedBonus);
        Assert.Equal(1, Assert.Single(report.Classes, item => item.Class == XpClass.Assault).MissedRecords);
        Assert.Equal(2100m, report.Totals.TotalXp);
    }

    [Fact]
    public void AValidOriginalRetainsEarlierEarnedXpForMissedBonusAfterRebase()
    {
        var tracker = Tracker();
        tracker.ManualCurrent(XpClass.Assault, 12_000);
        tracker.ArmRebase(XpClass.Assault);
        tracker.ManualBaseline(XpClass.Assault, 12_000);
        tracker.ManualCurrent(XpClass.Assault, 12_500);
        tracker.End(SessionOutcome.LeftMatch);
        var row = Row(AnalyticsEngine.Build(tracker.Document));
        Assert.Equal(2500, row.SupportedEarnedLowerBound);
        Assert.Equal(3500m, row.MissedBonus!.Average);
        Assert.False(row.MissedBonus.IsLowerBound);
    }

    [Fact]
    public void UntrustedOriginalDoesNotJoinSeparateIntervalsOrClaimWholeMatchCoverage()
    {
        var tracker = Tracker(confirmCoverage: false);
        tracker.ManualCurrent(XpClass.Assault, 11_200);
        tracker.ArmRebase(XpClass.Assault);
        tracker.ManualBaseline(XpClass.Assault, 11_200);
        tracker.ManualCurrent(XpClass.Assault, 12_500);
        tracker.End(SessionOutcome.LeftMatch);
        var row = Row(AnalyticsEngine.Build(tracker.Document));
        Assert.Equal(1300, row.SupportedEarnedLowerBound);
        Assert.Equal(1400m, row.MissedBonus!.Average);
        Assert.True(row.MissedBonus.IsLowerBound);
        Assert.False(row.IncludedInReceived);
    }

    [Fact]
    public void CustomRulesKeepFractionalAveragesAndConservativePartialEstimates()
    {
        var rules = new RuleSetSnapshot { Tiers = [new(0, 0, 0, 0), new(100, 1, 2, 4)] };
        var tracker = Tracker(rules: rules);
        tracker.ManualCurrent(XpClass.Assault, 10_100);
        tracker.End(SessionOutcome.LeftMatch);
        var report = AnalyticsEngine.Build(tracker.Document);
        Assert.Equal(7m / 3m, report.AverageMissedBonus);
        Assert.Equal(1m, report.MinimumMissedBonus);
        Assert.Equal(4m, report.MaximumMissedBonus);
        Assert.False(report.HasLowerBoundMissedBonus);

        var decreasing = new RuleSetSnapshot { Tiers = [new(0, 0, 0, 0), new(100, 30, 60, 90), new(200, 3, 6, 9)] };
        tracker = Tracker(rules: decreasing);
        tracker.ManualCurrent(XpClass.Assault, 10_100);
        tracker.End(SessionOutcome.LeftMatch);
        var missed = Row(AnalyticsEngine.Build(tracker.Document)).MissedBonus!;
        Assert.Equal(6m, missed.Average);
        Assert.Equal(3, missed.Minimum);
        Assert.Equal(9, missed.Maximum);
        Assert.True(missed.IsLowerBound);
    }

    [Fact]
    public void ReusedFinalIsAStartingReferenceRatherThanEarnedXp()
    {
        var tracker = Tracker();
        Finish(tracker, 16_500, 3);
        tracker.StartSession(true, true);
        tracker.Observe(Read(16_500), ReadingSource.CheckHotkey);
        Finish(tracker, 23_000, 3);
        var report = AnalyticsEngine.Build(tracker.Document);
        var row = Assert.Single(report.Records, item => item.SessionId == tracker.ActiveSession!.Id && item.Class == XpClass.Assault);
        Assert.Equal(16_500, row.StartingXp);
        Assert.Equal(0, row.SupportedEarnedLowerBound);
        Assert.Equal(6500, row.TotalGain);
        Assert.Equal(13_000m, report.Totals.TotalXp);
    }

    [Fact]
    public void ReportDerivationDoesNotResumeOrChangeAnArmedRebase()
    {
        var tracker = Tracker();
        tracker.ArmRebase(XpClass.Assault);
        var before = JsonSerializer.Serialize(tracker.Document);
        AnalyticsEngine.Build(tracker.Document, At);
        Assert.Equal(XpClass.Assault, tracker.ActiveSession!.ArmedRebase);
        Assert.Equal(before, JsonSerializer.Serialize(tracker.Document));
    }

    [Fact]
    public void EmptySeasonHasSixEmptyClassChartsAndNoInventedRecords()
    {
        var report = AnalyticsEngine.Build(new(), At);
        Assert.Equal(6, report.Classes.Count);
        Assert.Empty(report.Records);
        Assert.Equal(0, report.Coverage.Sessions);
        Assert.Equal(0m, report.Totals.TotalXp);
        Assert.Equal(0m, report.AverageMissedBonus);
    }

    private static SessionTracker Tracker(bool confirmCoverage = true, RuleSetSnapshot? rules = null)
    {
        var tracker = new SessionTracker(clock: () => At);
        foreach (var trackClass in Enum.GetValues<XpClass>().Where(item => item != XpClass.Assault))
            tracker.SetTrackEnabled(trackClass, false);
        tracker.StartSession(rules: rules);
        tracker.Observe(Read(10_000), ReadingSource.SnapshotHotkey, true);
        if (confirmCoverage) tracker.ConfirmCoverage(XpClass.Assault);
        return tracker;
    }

    private static void Finish(SessionTracker tracker, long xp, params int[] places)
    {
        if (places.Length > 0)
            tracker.Observe(new(ScreenKind.EndScore, ReadStatus.Accepted, "Placement fixture.", [],
                new(places, false, [new(places.Length == 1 ? places[0] switch
                    { 1 => PlacementCue.FirstContribution, 2 => PlacementCue.SecondContribution, _ => PlacementCue.ThirdContribution }
                    : PlacementCue.Defeat,
                    places, new(.3, .3, .4, .1), "Placement fixture.")]), [], []), ReadingSource.AutomaticOcr);
        tracker.Observe(Read(xp, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.True(tracker.ConfirmFinal().Applied);
    }

    private static ScreenReadResult Read(long xp, ScreenKind screen = ScreenKind.Unlock, XpClass trackClass = XpClass.Assault)
        => new(screen, ReadStatus.Accepted, "XP fixture.",
            [new(trackClass, xp, 1_000_000, 15, 16, screen, At, new(.4, .21, .1, .02), $"{xp}/1000000XP", .99)],
            PlacementEvidence.None, [], []);

    private static SessionClassAnalytics Row(SeasonAnalytics report)
        => Assert.Single(report.Records, item => item.Class == XpClass.Assault);
}
