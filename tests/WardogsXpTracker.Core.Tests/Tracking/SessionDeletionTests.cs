using System.Text.Json;
using WardogsXpTracker.Core.Analytics;
using WardogsXpTracker.Core.Persistence;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Tests.Tracking;

public sealed class SessionDeletionTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 17, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TrackerSessionState.Active)]
    [InlineData(TrackerSessionState.Frozen)]
    [InlineData(TrackerSessionState.Ended)]
    public void DeleteRemovesTheWholeSelectedRecordInEverySessionState(TrackerSessionState state)
    {
        var tracker = NewTracker();
        Start(tracker);
        var session = tracker.ActiveSession!;
        if (state == TrackerSessionState.Frozen)
            tracker.Observe(Read(16_500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        else if (state == TrackerSessionState.Ended)
            tracker.End(SessionOutcome.LeftMatch);
        Assert.Equal(state, session.State);
        var enabledBefore = JsonSerializer.Serialize(tracker.Document.EnabledTracks);

        var result = tracker.DeleteSession(session.Id);

        Assert.True(result.Applied);
        Assert.True(result.Changed);
        Assert.Empty(tracker.Document.Sessions);
        Assert.Null(tracker.Document.ActiveSessionId);
        Assert.Null(tracker.ActiveSession);
        Assert.Null(tracker.LastKnownConfirmedFinal);
        Assert.Equal(enabledBefore, JsonSerializer.Serialize(tracker.Document.EnabledTracks));
        TrackingDocumentValidator.Validate(tracker.Document);
    }

    [Fact]
    public void DeletingAnOlderMatchPreservesTheActiveMatchAndEveryRemainingRecord()
    {
        var tracker = NewTracker();
        Start(tracker);
        var olderId = tracker.ActiveSession!.Id;
        Finish(tracker, 16_500, 1);
        Start(tracker, 20_000);
        var current = tracker.ActiveSession!;
        tracker.ManualCurrent(XpClass.Assault, 23_000);
        tracker.ArmRebase(XpClass.Assault);
        var before = JsonSerializer.Serialize(current);
        var rules = tracker.SelectedRules;

        Assert.True(tracker.DeleteSession(olderId).Changed);

        Assert.Same(current, tracker.ActiveSession);
        Assert.Equal(current.Id, tracker.Document.ActiveSessionId);
        Assert.Equal(before, JsonSerializer.Serialize(current));
        Assert.Same(rules, tracker.SelectedRules);
        Assert.Single(tracker.Document.Sessions);
        TrackingDocumentValidator.Validate(tracker.Document);
    }

    [Fact]
    public void DeletingTheActiveMatchNeverActivatesAnOlderMatch()
    {
        var tracker = NewTracker();
        Start(tracker);
        var earlier = tracker.ActiveSession!;
        tracker.End(SessionOutcome.Unknown);
        Start(tracker, 20_000);
        var currentId = tracker.ActiveSession!.Id;
        var before = JsonSerializer.Serialize(earlier);

        tracker.DeleteSession(currentId);

        Assert.Null(tracker.ActiveSession);
        Assert.Null(tracker.Document.ActiveSessionId);
        Assert.Same(earlier, Assert.Single(tracker.Document.Sessions));
        Assert.Equal(before, JsonSerializer.Serialize(earlier));
        Assert.False(tracker.ManualCurrent(XpClass.Assault, 21_000).Applied);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownAndEmptyIdsCannotDeleteOrChangeAnything(bool empty)
    {
        var tracker = NewTracker();
        Start(tracker);
        var before = JsonSerializer.Serialize(tracker.Document);

        var result = tracker.DeleteSession(empty ? Guid.Empty : Guid.NewGuid());

        Assert.False(result.Applied);
        Assert.False(result.Changed);
        Assert.Equal(before, JsonSerializer.Serialize(tracker.Document));
    }

    [Fact]
    public void RepeatedDeletionDoesNotDeleteANeighborOrAppendHistory()
    {
        var tracker = NewTracker();
        Start(tracker);
        var id = tracker.ActiveSession!.Id;
        Start(tracker, 20_000);
        Assert.True(tracker.DeleteSession(id).Changed);
        var after = JsonSerializer.Serialize(tracker.Document);

        var repeated = tracker.DeleteSession(id);

        Assert.False(repeated.Applied);
        Assert.False(repeated.Changed);
        Assert.Equal(after, JsonSerializer.Serialize(tracker.Document));
    }

    [Fact]
    public void DeletingTheLatestConfirmedMatchFallsBackToTheRemainingLatestFinal()
    {
        var tracker = NewTracker();
        Start(tracker);
        Finish(tracker, 16_500, 1);
        var first = tracker.ActiveSession!.ConfirmedFinal;
        Start(tracker, 20_000);
        Finish(tracker, 26_500, 1);
        var laterId = tracker.ActiveSession!.Id;
        Assert.NotSame(first, tracker.LastKnownConfirmedFinal);

        tracker.DeleteSession(laterId);

        Assert.Same(first, tracker.LastKnownConfirmedFinal);
        Assert.Null(tracker.ActiveSession);
        tracker.StartSession(reuseConfirmedFinal: true);
        Assert.Equal(16_500, tracker.ActiveSession!.Tracks[XpClass.Assault].Baseline!.Xp);
    }

    [Fact]
    public void DeletingAReusedSourceDoesNotRewriteTheRemainingBaselineOrItsProvenance()
    {
        var tracker = NewTracker();
        Start(tracker);
        Finish(tracker, 16_500, 1);
        var sourceId = tracker.ActiveSession!.Id;
        tracker.StartSession(reuseConfirmedFinal: true);
        var session = tracker.ActiveSession!;
        var before = JsonSerializer.Serialize(session);

        tracker.DeleteSession(sourceId);

        Assert.Equal(before, JsonSerializer.Serialize(session));
        Assert.Equal(sourceId, session.Tracks[XpClass.Assault].Baseline!.RelatedSessionId);
        Assert.Equal(ReadingSource.ReusedFinal, session.Tracks[XpClass.Assault].Baseline!.Origin);
        Assert.Same(session, tracker.ActiveSession);
        TrackingDocumentValidator.Validate(tracker.Document);
    }

    [Fact]
    public void DeletedConfirmedXpAndMissedBonusesDisappearFromDerivedAnalytics()
    {
        var tracker = NewTracker();
        Start(tracker);
        Finish(tracker, 16_500, 1);
        var receivedId = tracker.ActiveSession!.Id;
        Start(tracker, 20_000);
        tracker.ManualCurrent(XpClass.Assault, 23_000);
        tracker.End(SessionOutcome.LeftMatch);
        var missedId = tracker.ActiveSession!.Id;
        var before = AnalyticsEngine.Build(tracker.Document, At);
        Assert.Equal(6500m, before.Totals.TotalXp);
        Assert.Equal(3500m, before.AverageMissedBonus);

        tracker.DeleteSession(receivedId);
        var afterReceived = AnalyticsEngine.Build(tracker.Document, At);
        Assert.Equal(0m, afterReceived.Totals.TotalXp);
        Assert.Equal(3500m, afterReceived.AverageMissedBonus);
        Assert.DoesNotContain(afterReceived.Records, record => record.SessionId == receivedId);
        Assert.Equal(1, afterReceived.Coverage.Sessions);

        tracker.DeleteSession(missedId);
        var empty = AnalyticsEngine.Build(tracker.Document, At);
        Assert.Equal(0m, empty.AverageMissedBonus);
        Assert.Empty(empty.Records);
        Assert.Equal(0, empty.Coverage.ClassRecords);
        Assert.All(empty.Classes, item => Assert.Equal(0m, item.Received.TotalXp));
    }

    [Fact]
    public void DeletedPendingFinalCannotBeConfirmedOrCountAsAwaitingReview()
    {
        var tracker = NewTracker();
        Start(tracker);
        var id = tracker.ActiveSession!.Id;
        tracker.Observe(Read(16_500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.NotNull(tracker.ActiveSession!.PendingFinal);
        Assert.True(AnalyticsEngine.Build(tracker.Document).Coverage.PendingRecords > 0);

        tracker.DeleteSession(id);

        Assert.False(tracker.ConfirmFinalForSession(id).Applied);
        Assert.False(tracker.ManualFinalForSession(id, XpClass.Assault, 16_500).Applied);
        Assert.Equal(0, AnalyticsEngine.Build(tracker.Document).Coverage.PendingRecords);
        Assert.Empty(tracker.Document.Sessions);
    }

    [Fact]
    public async Task MarkdownSaveAndReloadKeepTheDeletionAndUnrelatedSession()
    {
        var temporaryRoot = Path.GetFullPath(Path.GetTempPath());
        var directory = Path.GetFullPath(Path.Combine(temporaryRoot, "wardogs-deletion-" + Guid.NewGuid().ToString("N")));
        Assert.Equal(Path.TrimEndingDirectorySeparator(temporaryRoot), Path.GetDirectoryName(directory));
        try
        {
            var store = new MarkdownSaveStore(directory);
            var tracker = NewTracker();
            Start(tracker);
            var id = tracker.ActiveSession!.Id;
            Finish(tracker, 16_500, 1);
            Start(tracker, 20_000);
            var activeId = tracker.ActiveSession!.Id;
            var remaining = JsonSerializer.Serialize(tracker.ActiveSession);
            await store.SaveAsync("season.md", tracker.Document);

            tracker.DeleteSession(id);
            await store.SaveAsync("season.md", tracker.Document);
            var loaded = new SessionTracker((await store.LoadAsync<SeasonDocument>("season.md"))!);

            Assert.Equal(activeId, loaded.Document.ActiveSessionId);
            Assert.Equal(remaining, JsonSerializer.Serialize(loaded.ActiveSession));
            Assert.DoesNotContain(loaded.Document.Sessions, session => session.Id == id);
            Assert.Single(loaded.Document.Sessions);
            Assert.Equal(0m, AnalyticsEngine.Build(loaded.Document).Totals.TotalXp);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static SessionTracker NewTracker()
    {
        var ticks = 0;
        var tracker = new SessionTracker(clock: () => At.AddSeconds(++ticks));
        foreach (var trackClass in Enum.GetValues<XpClass>().Where(trackClass => trackClass != XpClass.Assault))
            tracker.SetTrackEnabled(trackClass, false);
        return tracker;
    }

    private static void Start(SessionTracker tracker, long xp = 10_000)
    {
        Assert.True(tracker.StartSession().Applied);
        Assert.True(tracker.Observe(Read(xp), ReadingSource.SnapshotHotkey, true).Applied);
        Assert.True(tracker.ConfirmCoverage(XpClass.Assault).Applied);
    }

    private static void Finish(SessionTracker tracker, long final, int place)
    {
        tracker.Observe(Read(final, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        Assert.True(tracker.ConfirmFinal(place).Applied);
    }

    private static ScreenReadResult Read(long xp, ScreenKind screen = ScreenKind.Unlock)
        => new(screen, ReadStatus.Accepted, "Deletion fixture.",
            [new(XpClass.Assault, xp, 1_000_000, 15, 16, screen, At, new(.4, .21, .1, .02), $"{xp}/1000000XP", .99)],
            PlacementEvidence.None, [], []);
}
