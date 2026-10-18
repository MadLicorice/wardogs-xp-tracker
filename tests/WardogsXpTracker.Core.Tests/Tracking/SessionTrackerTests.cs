using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;
using Xunit;

namespace WardogsXpTracker.Core.Tests.Tracking;

public class SessionTrackerTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 16, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BaselineRequiresExplicitNewSessionAndSnapshotIntent()
    {
        var tracker = NewTracker();
        Assert.False(tracker.Observe(Read(XpClass.Assault, 56486), ReadingSource.SnapshotHotkey, true).Applied);
        tracker.StartSession();
        tracker.Observe(Read(XpClass.Assault, 56486), ReadingSource.CheckHotkey);
        Assert.Null(Track(tracker).Baseline);
        Assert.Equal(56486, Track(tracker).LatestReading!.Xp);
        Assert.True(tracker.Observe(Read(XpClass.Assault, 56486), ReadingSource.SnapshotHotkey, true).Applied);
        Assert.Equal(56486, Track(tracker).Baseline!.Xp);
        Assert.Null(Track(tracker, XpClass.Medic).Baseline);
    }

    [Fact]
    public void F3CannotSilentlyReplaceAnExistingBaseline()
    {
        var tracker = WithBaseline();
        var baselineId = Track(tracker).Baseline!.Id;
        Assert.False(tracker.Observe(Read(XpClass.Assault, 2000), ReadingSource.SnapshotHotkey, true).Applied);
        Assert.Equal(baselineId, Track(tracker).Baseline!.Id);
        Assert.Equal(1000, Track(tracker).Current!.Xp);
        Assert.Equal(1000, Track(tracker).LatestReading!.Xp);
    }

    [Theory]
    [InlineData(3499, false)]
    [InlineData(3500, true)]
    [InlineData(4000, true)]
    public void RawGainReachesTargetAtExactly2500AndIsNotCapped(long current, bool reached)
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, current), ReadingSource.CheckHotkey);
        Assert.Equal(current - 1000, Track(tracker).MeasuredGain);
        Assert.Equal(reached, Track(tracker).TargetReached);
    }

    [Fact]
    public void DisabledAndMissingClassesDoNotBlockOtherClasses()
    {
        var tracker = NewTracker();
        tracker.SetTrackEnabled(XpClass.Medic, false);
        tracker.StartSession();
        tracker.Observe(Read(XpClass.Assault, 1000), ReadingSource.SnapshotHotkey, true);
        tracker.Observe(Read(XpClass.Medic, 2000), ReadingSource.SnapshotHotkey, true);
        tracker.Observe(Read(XpClass.Assault, 3500), ReadingSource.CheckHotkey);
        Assert.True(Track(tracker).TargetReached);
        Assert.False(Track(tracker, XpClass.Medic).Enabled);
        Assert.Null(Track(tracker, XpClass.Medic).Baseline);
        tracker.StartSession();
        Assert.False(Track(tracker, XpClass.Medic).Enabled);
    }

    [Fact]
    public void ChangedDisabledReadingsAreAuditedWithoutAdvancingTheLiveCounter()
    {
        var tracker = WithBaseline();
        tracker.SetTrackEnabled(XpClass.Assault, false);
        var baseline = Track(tracker).Baseline;
        var current = Track(tracker).Current;
        var observation = Read(XpClass.Assault, 4000);
        Assert.True(tracker.Observe(observation, ReadingSource.CheckHotkey).Changed);
        Assert.Equal(4000, Track(tracker).LatestReading!.Xp);
        Assert.Same(baseline, Track(tracker).Baseline);
        Assert.Same(current, Track(tracker).Current);
        Assert.False(Track(tracker).TargetReached);
        var historyCount = tracker.ActiveSession!.History.Count;
        Assert.False(tracker.Observe(observation, ReadingSource.CheckHotkey).Changed);
        Assert.Equal(historyCount, tracker.ActiveSession.History.Count);
        Assert.Contains(tracker.ActiveSession.History, e => e.Kind == "DisabledClassObserved" && e.Observation!.Xp == 4000);
        Assert.False(tracker.Observe(Read(XpClass.Assault, 3999), ReadingSource.CheckHotkey).Applied);
        Assert.Equal(4000, Track(tracker).LatestReading!.Xp);
    }

    [Fact]
    public void DisabledThresholdChangesAreAuditedAndLockedF3StillDoesNothing()
    {
        var tracker = WithBaseline();
        tracker.SetTrackEnabled(XpClass.Assault, false);
        var observation = Read(XpClass.Assault, 2000);
        tracker.Observe(observation, ReadingSource.CheckHotkey);
        var nextThreshold = observation with { Readings = [observation.Readings[0] with { NextThreshold = 1_100_000 }] };
        Assert.True(tracker.Observe(nextThreshold, ReadingSource.CheckHotkey).Changed);
        Assert.Equal(1_100_000, Track(tracker).LatestReading!.NextThreshold);
        var latest = Track(tracker).LatestReading;
        var count = tracker.ActiveSession!.History.Count;
        Assert.False(tracker.Observe(Read(XpClass.Assault, 3000), ReadingSource.SnapshotHotkey, true).Changed);
        Assert.Same(latest, Track(tracker).LatestReading);
        Assert.Equal(count, tracker.ActiveSession.History.Count);
    }

    [Fact]
    public void RebaseCancelAndWrongClassPreserveTheOldInterval()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 2000), ReadingSource.CheckHotkey);
        var baseline = Track(tracker).Baseline;
        tracker.ArmRebase(XpClass.Assault);
        Assert.Same(baseline, Track(tracker).Baseline);
        Assert.False(tracker.Observe(Read(XpClass.Medic, 3000), ReadingSource.SnapshotHotkey, true).Applied);
        Assert.Null(Track(tracker, XpClass.Medic).Baseline);
        tracker.CancelRebase();
        Assert.Same(baseline, Track(tracker).Baseline);
        Assert.Equal(1000, Track(tracker).MeasuredGain);
    }

    [Fact]
    public void SuccessfulRebaseResetsOnlyItsClassAndRetainsOriginalCoverage()
    {
        var tracker = WithBaseline();
        tracker.ConfirmCoverage();
        tracker.Observe(Read(XpClass.Assault, 3500), ReadingSource.CheckHotkey);
        Assert.True(Track(tracker).TargetReached);
        tracker.ArmRebase(XpClass.Assault);
        tracker.Observe(Read(XpClass.Assault, 3500), ReadingSource.SnapshotHotkey, true);
        Assert.Equal(1000, Track(tracker).OriginalBaseline!.Xp);
        Assert.Equal(BaselineCoverage.BeforeEarnedXp, Track(tracker).OriginalBaseline!.Coverage);
        Assert.Equal(BaselineCoverage.Partial, Track(tracker).Baseline!.Coverage);
        Assert.Equal(2, Track(tracker).Baseline!.Version);
        Assert.Equal(0, Track(tracker).MeasuredGain);
        Assert.False(Track(tracker).TargetReached);
        Assert.True(Track(tracker).WasRebased);
        tracker.Observe(Read(XpClass.Assault, 6000), ReadingSource.CheckHotkey);
        Assert.True(Track(tracker).TargetReached);
        Assert.True(Track(tracker).WasRebased);
    }

    [Fact]
    public void ExplicitWrongBaselineRebaseMarksOriginalInvalidWithoutInventingGain()
    {
        var tracker = WithBaseline();
        tracker.ConfirmCoverage();
        tracker.ArmRebase(XpClass.Assault, true);
        Assert.True(tracker.ManualBaseline(XpClass.Assault, 900).Applied);
        Assert.Equal(BaselineValidity.Invalid, Track(tracker).OriginalBaseline!.Validity);
        Assert.Equal(1000, Track(tracker).OriginalBaseline!.Xp);
        Assert.Equal(0, Track(tracker).MeasuredGain);
    }

    [Fact]
    public void CoverageDoesNotSpreadToLaterBaselinesOrRebases()
    {
        var tracker = WithBaseline();
        tracker.ConfirmCoverage();
        tracker.ManualBaseline(XpClass.Medic, 1000);
        Assert.Equal(BaselineCoverage.Unknown, Track(tracker, XpClass.Medic).Baseline!.Coverage);
        tracker.ArmRebase(XpClass.Assault);
        tracker.ManualBaseline(XpClass.Assault, 2000);
        tracker.ConfirmCoverage();
        Assert.Equal(BaselineCoverage.Partial, Track(tracker).Baseline!.Coverage);
    }

    [Fact]
    public void PerClassCoverageConfirmsOnlyTheNamedBaselineWithoutChangingCounters()
    {
        var tracker = WithBaseline();
        tracker.ManualBaseline(XpClass.Medic, 2000);
        tracker.ManualCurrent(XpClass.Assault, 3500);
        var baselineId = Track(tracker).Baseline!.Id;
        var baselineVersion = Track(tracker).Baseline!.Version;
        var current = Track(tracker).Current;
        var targetAt = Track(tracker).TargetReachedAt;
        var result = tracker.ConfirmCoverage(XpClass.Assault);
        Assert.True(result.Applied);
        Assert.True(result.Changed);
        Assert.Equal(BaselineCoverage.BeforeEarnedXp, Track(tracker).Baseline!.Coverage);
        Assert.Equal(BaselineValidity.Valid, Track(tracker).OriginalBaseline!.Validity);
        Assert.Equal(BaselineCoverage.Unknown, Track(tracker, XpClass.Medic).Baseline!.Coverage);
        Assert.Same(current, Track(tracker).Current);
        Assert.Equal(2500, Track(tracker).MeasuredGain);
        Assert.Equal(targetAt, Track(tracker).TargetReachedAt);
        var recorded = Assert.Single(tracker.ActiveSession!.History, e => e.Kind == "CoverageConfirmed");
        Assert.Equal(XpClass.Assault, recorded.Class);
        Assert.Equal(baselineId, recorded.BaselineId);
        Assert.Equal(baselineVersion, recorded.BaselineVersion);
        Assert.Equal(ReadingSource.Manual, recorded.Source);
        Assert.Null(tracker.ActiveSession.ConfirmedFinal);
    }

    [Fact]
    public void RepeatedPerClassCertificationDoesNotGrowHistoryAndSurvivesRestart()
    {
        var tracker = WithBaseline();
        tracker.ConfirmCoverage(XpClass.Assault);
        var count = tracker.ActiveSession!.History.Count;
        var restored = Restore(tracker.Document);
        var result = restored.ConfirmCoverage(XpClass.Assault);
        Assert.True(result.Applied);
        Assert.False(result.Changed);
        Assert.Equal(count, restored.ActiveSession!.History.Count);
        Assert.Equal(BaselineCoverage.BeforeEarnedXp, Track(restored).OriginalBaseline!.Coverage);
        Assert.Equal(Track(restored).Baseline!.Id, Track(restored).OriginalBaseline!.Id);
    }

    [Theory]
    [InlineData(XpClass.Medic)]
    [InlineData((XpClass)99)]
    public void MissingOrUnknownClassCannotReceiveCoverageCertification(XpClass trackClass)
    {
        var tracker = WithBaseline();
        var count = tracker.ActiveSession!.History.Count;
        var result = tracker.ConfirmCoverage(trackClass);
        Assert.False(result.Applied);
        Assert.False(result.Changed);
        Assert.Equal(count, tracker.ActiveSession.History.Count);
        Assert.Equal(BaselineCoverage.Unknown, Track(tracker).Baseline!.Coverage);
    }

    [Fact]
    public void DisabledRebasedOrInvalidOriginalBaselineCannotReceiveCertification()
    {
        var tracker = WithBaseline();
        tracker.SetTrackEnabled(XpClass.Assault, false);
        Assert.False(tracker.ConfirmCoverage(XpClass.Assault).Applied);
        tracker.SetTrackEnabled(XpClass.Assault, true);
        tracker.ArmRebase(XpClass.Assault, true);
        tracker.ManualBaseline(XpClass.Assault, 2000);
        var count = tracker.ActiveSession!.History.Count;
        Assert.False(tracker.ConfirmCoverage(XpClass.Assault).Applied);
        Assert.Equal(count, tracker.ActiveSession.History.Count);
        Assert.Equal(BaselineCoverage.Partial, Track(tracker).Baseline!.Coverage);
        Assert.Equal(BaselineValidity.Invalid, Track(tracker).OriginalBaseline!.Validity);
        Assert.Equal(0, Track(tracker).MeasuredGain);
    }

    [Theory]
    [InlineData(TrackerSessionState.Frozen)]
    [InlineData(TrackerSessionState.Ended)]
    public void BaselineTimingCanBeCertifiedAfterFreezeOrEndWithoutConfirmingFinals(TrackerSessionState state)
    {
        var tracker = WithBaseline();
        if (state == TrackerSessionState.Frozen)
            tracker.Observe(EndScore(PlacementCue.Defeat, 2, 3), ReadingSource.AutomaticOcr);
        else tracker.End(SessionOutcome.Unknown);
        Assert.True(tracker.ConfirmCoverage(XpClass.Assault).Applied);
        Assert.Equal(state, tracker.ActiveSession!.State);
        Assert.Null(tracker.ActiveSession.ConfirmedFinal);
        Assert.False(Track(tracker).TargetReached);
    }

    [Fact]
    public void CoverageCertificationRequiresAnExistingSession()
    {
        var tracker = NewTracker();
        Assert.False(tracker.ConfirmCoverage(XpClass.Assault).Applied);
        Assert.Empty(tracker.Document.Sessions);
    }

    [Fact]
    public void DecreasingAndFailedReadsLeaveTheLastAcceptedValuesIntact()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 2000), ReadingSource.CheckHotkey);
        var current = Track(tracker).Current;
        Assert.False(tracker.Observe(Read(XpClass.Assault, 1999), ReadingSource.CheckHotkey).Applied);
        Assert.Same(current, Track(tracker).Current);
        Assert.Same(current, Track(tracker).LatestReading);
        tracker.Observe(Read(XpClass.Assault, 3000) with { Status = ReadStatus.NoCurrentXp, Readings = [] }, ReadingSource.CheckHotkey);
        Assert.Same(current, Track(tracker).Current);
    }

    [Fact]
    public void DuplicateIdenticalXpDoesNotGrowHistoryOrRequestAnotherSave()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 2000), ReadingSource.AutomaticOcr);
        var count = tracker.ActiveSession!.History.Count;
        var result = tracker.Observe(Read(XpClass.Assault, 2000), ReadingSource.AutomaticOcr);
        Assert.True(result.Applied);
        Assert.False(result.Changed);
        Assert.Equal(count, tracker.ActiveSession.History.Count);
    }

    [Fact]
    public void EndScoreFreezesBeforeBonusTotalsAndCannotCreateAGreenCheck()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 2000), ReadingSource.CheckHotkey);
        var end = tracker.Observe(EndScore(PlacementCue.Defeat, 2, 3), ReadingSource.AutomaticOcr);
        Assert.True(end.NeedsFinalConfirmation);
        Assert.Equal(TrackerSessionState.Frozen, tracker.ActiveSession!.State);
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.Equal(1000, Track(tracker).MeasuredGain);
        Assert.False(Track(tracker).TargetReached);
        Assert.Equal(6500, tracker.ActiveSession.PendingFinal!.Tracks[XpClass.Assault].Xp);
        Assert.True(tracker.ConfirmFinal(2).Applied);
        Assert.False(Track(tracker).TargetReached);
        Assert.Equal(1000, Track(tracker).Baseline!.Xp);
        Assert.Equal(2000, Track(tracker).Current!.Xp);
    }

    [Fact]
    public void ManualOnlyLiveTrackingFreezesAtReliableEndWithoutLinkingUncertainCandidates()
    {
        var tracker = NewTracker();
        tracker.StartSession();
        tracker.ManualBaseline(XpClass.Assault, 1000);
        tracker.ManualCurrent(XpClass.Assault, 2000);
        Assert.False(tracker.ActiveSession!.HasUnlockMarker);
        var ending = tracker.Observe(EndScore(PlacementCue.Defeat, 2, 3), ReadingSource.AutomaticOcr);
        Assert.True(ending.Changed);
        Assert.Equal(TrackerSessionState.Frozen, tracker.ActiveSession.State);
        Assert.Null(tracker.ActiveSession.PendingFinal);
        Assert.False(tracker.Observe(Read(XpClass.Assault, 6500), ReadingSource.CheckHotkey).Applied);
        Assert.Equal(2000, Track(tracker).Current!.Xp);
        Assert.False(Track(tracker).TargetReached);
    }

    [Fact]
    public void FreshManualBaselineAloneIsEnoughToFreezeAtEndButNotToLinkFinalTotals()
    {
        var tracker = NewTracker();
        tracker.StartSession();
        tracker.ManualBaseline(XpClass.Assault, 1000);
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.Equal(TrackerSessionState.Frozen, tracker.ActiveSession!.State);
        Assert.Null(tracker.ActiveSession.PendingFinal);
        Assert.False(Track(tracker).TargetReached);
    }

    [Fact]
    public void RecognizedUnlockWithFailedDigitsSupportsManualFallbackEndAssociation()
    {
        var tracker = NewTracker();
        tracker.StartSession();
        tracker.ManualBaseline(XpClass.Assault, 1000);
        var unreadable = Read(XpClass.Assault, 1000) with { Status = ReadStatus.LowConfidence, Readings = [] };
        Assert.True(tracker.Observe(unreadable, ReadingSource.CheckHotkey).Changed);
        Assert.True(tracker.ActiveSession!.HasUnlockMarker);
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.Equal(6500, tracker.ActiveSession.PendingFinal!.Tracks[XpClass.Assault].Xp);
        Assert.Equal(1000, Track(tracker).Current!.Xp);
        Assert.False(Track(tracker).TargetReached);
    }

    [Fact]
    public void LockedSnapshotCannotEstablishAStartMarkerForStillVisibleOldResults()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.ConfirmFinal();
        tracker.StartSession(true);
        Assert.False(tracker.ActiveSession!.HasUnlockMarker);
        var count = tracker.ActiveSession.History.Count;
        Assert.False(tracker.Observe(Read(XpClass.Assault, 6500), ReadingSource.SnapshotHotkey, true).Changed);
        Assert.False(tracker.ActiveSession.HasUnlockMarker);
        Assert.Equal(count, tracker.ActiveSession.History.Count);
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.Equal(TrackerSessionState.Active, tracker.ActiveSession.State);
        Assert.Null(tracker.ActiveSession.PendingFinal);
    }

    [Fact]
    public void EndProgressionCanCaptureDisabledClassWithoutEnablingItsCounter()
    {
        var tracker = WithBaseline();
        tracker.SetTrackEnabled(XpClass.Medic, false);
        tracker.Observe(Read(XpClass.Medic, 58891, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.False(Track(tracker, XpClass.Medic).Enabled);
        Assert.Null(Track(tracker, XpClass.Medic).Baseline);
        Assert.Equal(58891, tracker.ActiveSession!.PendingFinal!.Tracks[XpClass.Medic].Xp);
        Assert.False(Track(tracker, XpClass.Medic).TargetReached);
    }

    [Fact]
    public void ARejectedNumericEndSceneStillFreezesLiveChecks()
    {
        var tracker = WithBaseline();
        var scene = EndScore(PlacementCue.Defeat, 2, 3) with { Status = ReadStatus.Ambiguous };
        tracker.Observe(scene, ReadingSource.AutomaticOcr);
        Assert.Equal(TrackerSessionState.Frozen, tracker.ActiveSession!.State);
        Assert.False(tracker.ManualCurrent(XpClass.Assault, 10000).Applied);
        Assert.False(Track(tracker).TargetReached);
    }

    [Fact]
    public void CandidatesAndOldReferencesSurviveJsonRestartAndDeferral()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.KeepPendingFinal();
        var restored = Restore(tracker.Document);
        Assert.Equal(1000, Track(restored).Baseline!.Xp);
        Assert.Equal(6500, restored.ActiveSession!.PendingFinal!.Tracks[XpClass.Assault].Xp);
        Assert.True(restored.ActiveSession.PendingFinal.KeepOldReferencesChosen);
        Assert.Null(restored.ActiveSession.ConfirmedFinal);
        Assert.True(restored.ConfirmFinal().Applied);
    }

    [Fact]
    public void ReuseIsExplicitUsesOnlyConfirmedObservedClassesAndStartsAtZeroGain()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.ConfirmFinal(1);
        tracker.StartSession();
        Assert.Null(Track(tracker).Baseline);
        tracker.StartSession(true, true);
        Assert.Equal(6500, Track(tracker).Baseline!.Xp);
        Assert.Equal(ReadingSource.ReusedFinal, Track(tracker).Baseline!.Origin);
        Assert.Equal(BaselineCoverage.BeforeEarnedXp, Track(tracker).Baseline!.Coverage);
        Assert.Equal(0, Track(tracker).MeasuredGain);
        Assert.Null(Track(tracker, XpClass.Medic).Baseline);
        Assert.False(Track(tracker).TargetReached);
    }

    [Fact]
    public void UnconfirmedCandidatesNeverSupplyNewBaselines()
    {
        var tracker = WithBaseline();
        var old = tracker.ActiveSession!;
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.StartSession(true);
        Assert.Null(Track(tracker).Baseline);
        Assert.NotNull(old.PendingFinal);
    }

    [Fact]
    public void OldStillVisibleEndCardsCannotAttachToANewSession()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.StartSession();
        Assert.False(tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr).Applied);
        Assert.Null(tracker.ActiveSession!.PendingFinal);
        Assert.Equal(TrackerSessionState.Active, tracker.ActiveSession.State);
    }

    [Fact]
    public void AnUnlockAfterTheEndPreventsLaterMatchResultsMergingIntoFrozenSession()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.Observe(Read(XpClass.Assault, 7000), ReadingSource.AutomaticOcr);
        Assert.False(tracker.Observe(Read(XpClass.Assault, 10000, ScreenKind.EndProgression), ReadingSource.AutomaticOcr).Applied);
        Assert.Equal(6500, tracker.ActiveSession!.PendingFinal!.Tracks[XpClass.Assault].Xp);
    }

    [Fact]
    public void AsyncReadingForAnOldSessionCannotChangeTheNewOne()
    {
        var tracker = WithBaseline();
        var oldId = tracker.ActiveSession!.Id;
        tracker.StartSession();
        Assert.False(tracker.Observe(Read(XpClass.Assault, 5000), ReadingSource.SnapshotHotkey, true, oldId).Applied);
        Assert.Null(Track(tracker).Baseline);
        Assert.False(tracker.ActiveSession!.HasUnlockMarker);
    }

    [Fact]
    public void EvenAnUnacceptedUnlockSceneSeparatesLaterEndResults()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.Observe(Read(XpClass.Assault, 7000) with { Status = ReadStatus.ClassNotEstablished, Readings = [] }, ReadingSource.AutomaticOcr);
        Assert.True(tracker.ActiveSession!.PostEndUnlockObserved);
        Assert.False(tracker.Observe(Read(XpClass.Assault, 10000, ScreenKind.EndProgression), ReadingSource.AutomaticOcr).Applied);
        Assert.Equal(6500, tracker.ActiveSession.PendingFinal!.Tracks[XpClass.Assault].Xp);
    }

    [Fact]
    public void RepeatedConfirmedFinalScreenDoesNotCreateAnotherPendingDecision()
    {
        var tracker = WithBaseline();
        var end = Read(XpClass.Assault, 6500, ScreenKind.EndProgression);
        tracker.Observe(end, ReadingSource.AutomaticOcr);
        tracker.ConfirmFinal();
        var count = tracker.ActiveSession!.History.Count;
        var result = tracker.Observe(end, ReadingSource.AutomaticOcr);
        Assert.False(result.Changed);
        Assert.False(result.NeedsFinalConfirmation);
        Assert.Null(tracker.ActiveSession.PendingFinal);
        Assert.Equal(count, tracker.ActiveSession.History.Count);
    }

    [Fact]
    public void PlacementAccumulatesAndConflictsRequireExplicitCorrection()
    {
        var tracker = WithBaseline();
        tracker.Observe(EndScore(PlacementCue.Defeat, 2, 3), ReadingSource.AutomaticOcr);
        tracker.Observe(EndScore(PlacementCue.SecondContribution, 2), ReadingSource.AutomaticOcr);
        Assert.Equal(new[] { 2 }, tracker.ActiveSession!.PendingFinal!.PossiblePlaces);
        tracker.Observe(EndScore(PlacementCue.ThirdContribution, 3), ReadingSource.AutomaticOcr);
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.False(tracker.ConfirmFinal().Applied);
        Assert.True(tracker.ConfirmFinal(2).Applied);
        Assert.Equal(new[] { 2 }, tracker.ActiveSession.ConfirmedFinal!.PossiblePlaces);
        Assert.Equal(4, tracker.ActiveSession.ConfirmedFinal.PlacementEvidence.Count);
    }

    [Fact]
    public void FinalDecreaseRequiresClassCorrectionAndCannotBeFixedByPlacement()
    {
        var tracker = WithBaseline();
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.Observe(Read(XpClass.Assault, 6400, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.False(tracker.ConfirmFinal(1).Applied);
        tracker.ManualFinal(XpClass.Assault, 6500);
        Assert.True(tracker.ConfirmFinal(1).Applied);
        Assert.False(Track(tracker).TargetReached);
    }

    [Theory]
    [InlineData(SessionOutcome.LeftMatch)]
    [InlineData(SessionOutcome.Disconnected)]
    public void NoBonusExitKeepsCountersAndStoresClosingXpSeparately(SessionOutcome outcome)
    {
        var tracker = WithBaseline();
        tracker.ManualCurrent(XpClass.Assault, 2000);
        tracker.End(outcome);
        tracker.RecordClosingXp(XpClass.Assault, 5000);
        Assert.Equal(1000, Track(tracker).Baseline!.Xp);
        Assert.Equal(2000, Track(tracker).Current!.Xp);
        Assert.Equal(5000, Track(tracker).ClosingXp!.Xp);
        Assert.False(Track(tracker).TargetReached);
        Assert.Null(tracker.ActiveSession!.ConfirmedFinal);
    }

    [Fact]
    public void LoadingAnArmedRebaseCancelsRequestWithoutLosingTheOldBaseline()
    {
        var tracker = WithBaseline();
        tracker.ArmRebase(XpClass.Assault);
        var restored = Restore(tracker.Document);
        Assert.Null(restored.ActiveSession!.ArmedRebase);
        Assert.Equal(1000, Track(restored).Baseline!.Xp);
        Assert.Contains(restored.ActiveSession.History, e => e.Kind == "RebaseCanceledOnResume");
    }

    [Fact]
    public void RulesAreCopiedPerSessionAndCustomMaximumSetsTheTarget()
    {
        var tracker = NewTracker();
        var rules = new RuleSetSnapshot { Tiers = [new(0, 0, 0, 0), new(100, 20, 30, 40)] };
        tracker.StartSession(rules: rules);
        tracker.ManualBaseline(XpClass.Assault, 1000);
        rules.Tiers[1] = new(500, 5, 10, 15);
        Assert.Equal(100, tracker.ActiveSession!.RulesSnapshot.TargetXp);
        tracker.ManualCurrent(XpClass.Assault, 1100);
        Assert.True(Track(tracker).TargetReached);
        Assert.Equal(30, tracker.ActiveSession.RulesSnapshot.TierFor(100).Second);
    }

    [Fact]
    public void SeasonDataDoesNotLeakIntoANewSeason()
    {
        var first = WithBaseline();
        var second = new SessionTracker(new SeasonDocument { Name = "Season 3" }, () => At);
        second.StartSession(true);
        Assert.Null(Track(second).Baseline);
        Assert.NotEqual(first.Document.SeasonId, second.Document.SeasonId);
    }

    [Fact]
    public void MalformedSavesAndRulesFailBeforeTrackerMutation()
    {
        Assert.Throws<InvalidDataException>(() => new SessionTracker(new() { SchemaVersion = 99 }));
        Assert.Throws<InvalidDataException>(() => new SessionTracker(new() { SeasonId = Guid.Empty }));
        Assert.Throws<InvalidDataException>(() => new SessionTracker(new() { Sessions = null! }));
        Assert.Throws<InvalidDataException>(() => new SessionTracker(new() { ActiveSessionId = Guid.NewGuid() }));
        var tracker = WithBaseline();
        var originalSessionId = tracker.ActiveSession!.Id;
        var rules = new RuleSetSnapshot { Tiers = [new(0, 0, 0, 0), new(100, 1, 2, 3), new(50, 1, 2, 3)] };
        Assert.Throws<InvalidDataException>(() => tracker.StartSession(rules: rules));
        Assert.Equal(originalSessionId, tracker.ActiveSession.Id);
        var invalid = RestoreDocument(tracker.Document);
        invalid.Sessions[0].State = (TrackerSessionState)99;
        Assert.Throws<InvalidDataException>(() => new SessionTracker(invalid));
    }

    [Fact]
    public void MissingSeasonPayloadCannotBecomeAFreshEmptySeason()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SeasonDocument>("{}", Options()));
        var document = JsonNode.Parse(JsonSerializer.Serialize(WithBaseline().Document, Options()))!.AsObject();
        document.Remove("SeasonId");
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SeasonDocument>(document.ToJsonString(), Options()));
    }

    [Theory]
    [InlineData("Xp")]
    [InlineData("Id")]
    [InlineData("Origin")]
    [InlineData("At")]
    public void MissingBaselineFieldsCannotSilentlyUseDefaults(string field)
    {
        var document = JsonNode.Parse(JsonSerializer.Serialize(WithBaseline().Document, Options()))!;
        document["Sessions"]![0]!["Tracks"]!["Assault"]!["Baseline"]!.AsObject().Remove(field);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SeasonDocument>(document.ToJsonString(), Options()));
    }

    [Fact]
    public void MissingRulesAndBonusFieldsCannotSilentlyUseDefaults()
    {
        var rules = JsonNode.Parse(JsonSerializer.Serialize(new RuleSetSnapshot(), Options()))!.AsObject();
        rules.Remove("Tiers");
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RuleSetSnapshot>(rules.ToJsonString(), Options()));
        rules = JsonNode.Parse(JsonSerializer.Serialize(new RuleSetSnapshot(), Options()))!.AsObject();
        rules["Tiers"]![1]!.AsObject().Remove("Second");
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RuleSetSnapshot>(rules.ToJsonString(), Options()));
    }

    private static SessionTracker NewTracker() => new(clock: () => At);
    private static SessionTracker WithBaseline()
    {
        var tracker = NewTracker();
        tracker.StartSession();
        tracker.Observe(Read(XpClass.Assault, 1000), ReadingSource.SnapshotHotkey, true);
        return tracker;
    }
    private static TrackState Track(SessionTracker tracker, XpClass trackClass = XpClass.Assault) => tracker.ActiveSession!.Tracks[trackClass];
    private static ScreenReadResult Read(XpClass trackClass, long xp, ScreenKind screen = ScreenKind.Unlock)
        => new(screen, ReadStatus.Accepted, "Read fixture.",
            [new(trackClass, xp, 1_000_000, 15, 16, screen, At, new(.4, .21, .1, .02), $"{xp}/1000000XP", .99)],
            PlacementEvidence.None, [], []);
    private static ScreenReadResult EndScore(PlacementCue cue, params int[] places)
        => new(ScreenKind.EndScore, ReadStatus.Accepted, "End fixture.", [],
            new(places, false, [new(cue, places, new(.3, .3, .4, .1), cue.ToString())]), [], []);
    private static JsonSerializerOptions Options() => new() { Converters = { new JsonStringEnumConverter() } };
    private static SeasonDocument RestoreDocument(SeasonDocument document)
        => JsonSerializer.Deserialize<SeasonDocument>(JsonSerializer.Serialize(document, Options()), Options())!;
    private static SessionTracker Restore(SeasonDocument document) => new(RestoreDocument(document), () => At);
}
