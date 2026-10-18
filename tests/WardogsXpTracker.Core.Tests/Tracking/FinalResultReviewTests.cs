using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Tests.Tracking;

public sealed class FinalResultReviewTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 17, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HistoricalConfirmationDoesNotChangeTheActiveSessionOrItsCounters()
    {
        var tracker = PendingTracker();
        var historical = tracker.ActiveSession!;
        tracker.StartSession();
        tracker.ManualBaseline(XpClass.Assault, 20_000);
        tracker.ManualCurrent(XpClass.Assault, 22_500);
        tracker.ArmRebase(XpClass.Assault);
        var active = tracker.ActiveSession!;
        var activeBefore = JsonSerializer.Serialize(active, Options());
        Assert.True(tracker.ConfirmFinalForSession(historical.Id, 2).Applied);
        Assert.Equal(active.Id, tracker.Document.ActiveSessionId);
        Assert.Same(active, tracker.ActiveSession);
        Assert.Equal(activeBefore, JsonSerializer.Serialize(active, Options()));
        Assert.Equal(TrackerSessionState.Ended, historical.State);
        Assert.Equal(SessionOutcome.Finished, historical.Outcome);
        Assert.Null(historical.PendingFinal);
        Assert.Equal(6500, historical.ConfirmedFinal!.Tracks[XpClass.Assault].Xp);
        Assert.False(historical.Tracks[XpClass.Assault].TargetReached);
    }

    [Fact]
    public void HistoricalConfirmationDoesNotReplaceANewerLastKnownMatch()
    {
        var now = At;
        var tracker = PendingTracker(() => now);
        var historical = tracker.ActiveSession!;
        now = now.AddDays(1);
        tracker.StartSession();
        tracker.Observe(Read(XpClass.Assault, 20_000), ReadingSource.SnapshotHotkey, true);
        tracker.Observe(Read(XpClass.Assault, 26_500, ScreenKind.EndProgression), ReadingSource.CheckHotkey);
        Assert.True(tracker.ConfirmFinal(1).Applied);
        var newer = tracker.ActiveSession!;
        now = now.AddDays(7);
        Assert.True(tracker.ConfirmFinalForSession(historical.Id, 3).Applied);
        Assert.Equal(newer.Id, tracker.LastKnownConfirmedFinal!.SessionId);
        Assert.Equal(newer.Id, tracker.Document.ActiveSessionId);
    }

    [Fact]
    public void ScoreOnlyUnknownOrNonpendingSessionsCannotBeConfirmed()
    {
        var tracker = StartedTracker();
        tracker.Observe(Score(2, 3), ReadingSource.AutomaticOcr);
        var scoreOnly = tracker.ActiveSession!;
        tracker.StartSession();
        var activeId = tracker.Document.ActiveSessionId;
        var before = JsonSerializer.Serialize(tracker.Document, Options());
        Assert.False(tracker.ConfirmFinalForSession(scoreOnly.Id, 2).Applied);
        Assert.False(tracker.ConfirmFinalForSession(Guid.NewGuid(), 2).Applied);
        Assert.False(tracker.ConfirmFinalForSession(tracker.ActiveSession!.Id, 2).Applied);
        Assert.Equal(activeId, tracker.Document.ActiveSessionId);
        Assert.Equal(before, JsonSerializer.Serialize(tracker.Document, Options()));
        Assert.NotNull(scoreOnly.PendingFinal);
    }

    [Fact]
    public void HistoricalCorrectionChangesOnlyTheNamedPendingClass()
    {
        var tracker = PendingTracker();
        var historical = tracker.ActiveSession!;
        tracker.Observe(Read(XpClass.Medic, 7000, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        var oldAssaultCurrent = historical.Tracks[XpClass.Assault].Current;
        tracker.StartSession();
        tracker.ManualBaseline(XpClass.Assault, 30_000);
        var active = tracker.ActiveSession!;
        var activeBefore = JsonSerializer.Serialize(active, Options());
        Assert.True(tracker.ManualFinalForSession(historical.Id, XpClass.Assault, 6600).Applied);
        Assert.Equal(6600, historical.PendingFinal!.Tracks[XpClass.Assault].Xp);
        Assert.Equal(7000, historical.PendingFinal.Tracks[XpClass.Medic].Xp);
        Assert.Equal(ReadingSource.Manual, historical.PendingFinal.Tracks[XpClass.Assault].Source);
        Assert.Same(oldAssaultCurrent, historical.Tracks[XpClass.Assault].Current);
        Assert.False(historical.Tracks[XpClass.Assault].TargetReached);
        Assert.Equal(activeBefore, JsonSerializer.Serialize(active, Options()));
        Assert.Equal(active.Id, tracker.Document.ActiveSessionId);
        var correction = Assert.Single(historical.History, item => item.Kind == "FinalCorrected");
        Assert.Equal(ReadingSource.Manual, correction.Source);
        Assert.Equal(6600, correction.Observation!.Xp);
    }

    [Fact]
    public void InvalidHistoricalCorrectionsLeaveAllSavedDataUnchanged()
    {
        var tracker = PendingTracker();
        var historical = tracker.ActiveSession!;
        tracker.StartSession();
        var before = JsonSerializer.Serialize(tracker.Document, Options());
        Assert.False(tracker.ManualFinalForSession(Guid.NewGuid(), XpClass.Assault, 6600).Applied);
        Assert.False(tracker.ManualFinalForSession(tracker.ActiveSession!.Id, XpClass.Assault, 6600).Applied);
        Assert.False(tracker.ManualFinalForSession(historical.Id, (XpClass)99, 6600).Applied);
        Assert.False(tracker.ManualFinalForSession(historical.Id, XpClass.Assault, -1).Applied);
        Assert.False(tracker.ManualFinalForSession(historical.Id, XpClass.Assault, 999).Applied);
        Assert.Equal(before, JsonSerializer.Serialize(tracker.Document, Options()));
    }

    [Fact]
    public void DeferredResultsSurviveRestartAndRemainReviewableWithoutActivation()
    {
        var tracker = PendingTracker();
        var historical = tracker.ActiveSession!;
        tracker.StartSession();
        var activeId = tracker.Document.ActiveSessionId;
        Assert.True(tracker.KeepPendingFinalForSession(historical.Id).Changed);
        Assert.False(tracker.KeepPendingFinalForSession(historical.Id).Changed);
        var restored = Restore(tracker.Document);
        var restoredHistorical = Assert.Single(restored.Document.Sessions, item => item.Id == historical.Id);
        Assert.True(restoredHistorical.PendingFinal!.KeepOldReferencesChosen);
        Assert.Equal(1000, restoredHistorical.Tracks[XpClass.Assault].Baseline!.Xp);
        Assert.Equal(6500, restoredHistorical.PendingFinal.Tracks[XpClass.Assault].Xp);
        Assert.NotEmpty(restoredHistorical.FinalObservations);
        Assert.Equal(activeId, restored.Document.ActiveSessionId);
        Assert.True(restored.ConfirmFinalForSession(historical.Id, 3).Applied);
        Assert.Equal(activeId, restored.Document.ActiveSessionId);
        Assert.NotNull(restoredHistorical.ConfirmedFinal);
    }

    [Fact]
    public void ObservedNumbersAndDefeatRemainUnchangedAfterManualOverrides()
    {
        var tracker = PendingTracker();
        var session = tracker.ActiveSession!;
        var observed = session.FinalObservations[^1];
        var observedBefore = JsonSerializer.Serialize(observed, Options());
        var observedId = observed.Tracks[XpClass.Assault].Id;
        Assert.True(tracker.ManualFinal(XpClass.Assault, 6600).Applied);
        Assert.True(tracker.ConfirmFinal(1).Applied);
        Assert.Equal(observedBefore, JsonSerializer.Serialize(observed, Options()));
        Assert.Equal(6500, observed.Tracks[XpClass.Assault].Xp);
        Assert.Equal(new[] { 2, 3 }, observed.PossiblePlaces);
        Assert.Null(observed.ConfirmedAt);
        Assert.Equal(6600, session.ConfirmedFinal!.Tracks[XpClass.Assault].Xp);
        Assert.Equal(new[] { 1 }, session.ConfirmedFinal.PossiblePlaces);
        Assert.Contains(session.ConfirmedFinal.PlacementEvidence, item => item.Source == ReadingSource.Manual);
        Assert.DoesNotContain(observed.PlacementEvidence, item => item.Source == ReadingSource.Manual);
        Assert.Contains(session.History, item => item.Kind == "FinalCandidate" && item.Observation!.Id == observedId);
        var restored = Restore(tracker.Document);
        Assert.Equal(observedBefore, JsonSerializer.Serialize(restored.ActiveSession!.FinalObservations[^1], Options()));
    }

    [Fact]
    public void ConfirmationCopiesPreserveObservationIdentityWithoutSharingMutableObjects()
    {
        var tracker = PendingTracker();
        var pending = tracker.ActiveSession!.PendingFinal!;
        var original = pending.Tracks[XpClass.Assault];
        tracker.ConfirmFinal(3);
        var confirmed = tracker.ActiveSession.ConfirmedFinal!.Tracks[XpClass.Assault];
        Assert.Equal(original.Id, confirmed.Id);
        Assert.NotSame(original, confirmed);
        original.Xp = 99_999;
        Assert.Equal(6500, confirmed.Xp);
        Assert.Equal(6500, tracker.ActiveSession.FinalObservations[^1].Tracks[XpClass.Assault].Xp);
    }

    [Fact]
    public void DuplicateOcrDoesNotAddObservedRevisionsBeforeOrAfterConfirmation()
    {
        var tracker = PendingTracker();
        var session = tracker.ActiveSession!;
        var count = session.FinalObservations.Count;
        for (var index = 0; index < 3; index++)
        {
            tracker.Observe(Score(2, 3), ReadingSource.CheckHotkey);
            tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        }
        Assert.Equal(count, session.FinalObservations.Count);
        tracker.ConfirmFinal(3);
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.Equal(count, session.FinalObservations.Count);
    }

    [Fact]
    public void EveryChangedObservedTotalKeepsThePreviousDetachedRevision()
    {
        var tracker = PendingTracker();
        var session = tracker.ActiveSession!;
        var old = session.FinalObservations[^1];
        var oldBefore = JsonSerializer.Serialize(old, Options());
        tracker.Observe(Read(XpClass.Assault, 6600, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        var latest = session.FinalObservations[^1];
        Assert.NotSame(old, latest);
        Assert.Equal(6500, old.Tracks[XpClass.Assault].Xp);
        Assert.Equal(6600, latest.Tracks[XpClass.Assault].Xp);
        latest.Tracks[XpClass.Assault].RawText = "Changed latest copy";
        latest.PlacementEvidence[0].PossiblePlaces.Clear();
        Assert.Equal(oldBefore, JsonSerializer.Serialize(old, Options()));
    }

    [Fact]
    public void DecreasedObservedTotalsAreArchivedWithoutReplacingTheCandidate()
    {
        var tracker = PendingTracker();
        var session = tracker.ActiveSession!;
        tracker.Observe(Read(XpClass.Assault, 6200, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.Equal(6500, session.PendingFinal!.Tracks[XpClass.Assault].Xp);
        Assert.Equal(6200, session.FinalObservations[^1].Tracks[XpClass.Assault].Xp);
        Assert.Contains(XpClass.Assault, session.FinalObservations[^1].ConflictingTracks);
        var count = session.FinalObservations.Count;
        tracker.Observe(Read(XpClass.Assault, 6200, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.Equal(count, session.FinalObservations.Count);
    }

    [Fact]
    public void OlderSchemaOneSavesCanOmitObservedBackups()
    {
        var tracker = PendingTracker();
        var json = JsonNode.Parse(JsonSerializer.Serialize(tracker.Document, Options()))!;
        json["Sessions"]![0]!.AsObject().Remove("FinalObservations");
        var restored = new SessionTracker(JsonSerializer.Deserialize<SeasonDocument>(json.ToJsonString(), Options())!, () => At);
        Assert.Empty(restored.ActiveSession!.FinalObservations);
        Assert.True(restored.ManualFinal(XpClass.Assault, 6600).Applied);
        Assert.Equal(6500, restored.ActiveSession.FinalObservations[^1].Tracks[XpClass.Assault].Xp);
        Assert.Equal(new[] { 2, 3 }, restored.ActiveSession.FinalObservations[^1].PossiblePlaces);
    }

    [Fact]
    public void ASingleScoreReadAfterUpgradePreservesLegacyXpBeforeAManualCorrection()
    {
        var tracker = PendingTracker();
        var json = JsonNode.Parse(JsonSerializer.Serialize(tracker.Document, Options()))!;
        json["Sessions"]![0]!.AsObject().Remove("FinalObservations");
        var restored = new SessionTracker(JsonSerializer.Deserialize<SeasonDocument>(json.ToJsonString(), Options())!, () => At);
        var result = restored.Observe(Score(2, 3), ReadingSource.AutomaticOcr);
        Assert.True(result.Changed);
        var observed = Assert.Single(restored.ActiveSession!.FinalObservations);
        Assert.Equal(6500, observed.Tracks[XpClass.Assault].Xp);
        Assert.Equal(new[] { 2, 3 }, observed.PossiblePlaces);
        Assert.False(restored.Observe(Score(2, 3), ReadingSource.AutomaticOcr).Changed);
        Assert.Single(restored.ActiveSession.FinalObservations);
        Assert.True(restored.ManualFinal(XpClass.Assault, 6600).Applied);
        Assert.Equal(6500, observed.Tracks[XpClass.Assault].Xp);
        Assert.Equal(6600, restored.ActiveSession.PendingFinal!.Tracks[XpClass.Assault].Xp);
    }

    [Fact]
    public void MalformedObservedBackupsCannotBypassSaveValidation()
    {
        var tracker = PendingTracker();
        var invalid = RestoreDocument(tracker.Document);
        invalid.Sessions[0].FinalObservations = null!;
        Assert.Throws<InvalidDataException>(() => new SessionTracker(invalid));
        invalid = RestoreDocument(tracker.Document);
        invalid.Sessions[0].FinalObservations[0].SessionId = Guid.NewGuid();
        Assert.Throws<InvalidDataException>(() => new SessionTracker(invalid));
        invalid = RestoreDocument(tracker.Document);
        invalid.Sessions[0].FinalObservations[^1].Tracks[XpClass.Assault].Xp = -1;
        Assert.Throws<InvalidDataException>(() => new SessionTracker(invalid));
    }

    private static SessionTracker PendingTracker(Func<DateTimeOffset>? clock = null)
    {
        var tracker = StartedTracker(clock);
        tracker.Observe(Score(2, 3), ReadingSource.AutomaticOcr);
        tracker.Observe(Read(XpClass.Assault, 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        return tracker;
    }

    private static SessionTracker StartedTracker(Func<DateTimeOffset>? clock = null)
    {
        var tracker = new SessionTracker(clock: clock ?? (() => At));
        tracker.StartSession();
        tracker.Observe(Read(XpClass.Assault, 1000), ReadingSource.SnapshotHotkey, true);
        return tracker;
    }

    private static ScreenReadResult Read(XpClass trackClass, long xp, ScreenKind screen = ScreenKind.Unlock)
        => new(screen, ReadStatus.Accepted, "Read fixture.",
            [new(trackClass, xp, 1_000_000, 15, 16, screen, At, new(.4, .21, .1, .02), $"{xp}/1000000XP", .99)],
            PlacementEvidence.None, [], []);

    private static ScreenReadResult Score(params int[] places)
        => new(ScreenKind.EndScore, ReadStatus.Accepted, "Score fixture.", [],
            new(places, false, [new(places.Length == 2 ? PlacementCue.Defeat : PlacementCue.ThirdContribution,
                places, new(.3, .3, .4, .1), "DEFEAT fixture.")]), [], []);

    private static JsonSerializerOptions Options() => new() { Converters = { new JsonStringEnumConverter() } };
    private static SeasonDocument RestoreDocument(SeasonDocument document)
        => JsonSerializer.Deserialize<SeasonDocument>(JsonSerializer.Serialize(document, Options()), Options())!;
    private static SessionTracker Restore(SeasonDocument document) => new(RestoreDocument(document), () => At);
}
