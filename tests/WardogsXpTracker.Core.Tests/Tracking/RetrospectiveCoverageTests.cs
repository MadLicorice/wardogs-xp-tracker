using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using WardogsXpTracker.Core.Analytics;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Tests.Tracking;

public sealed class RetrospectiveCoverageTests
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };
    private static readonly DateTimeOffset At = new(2026, 10, 17, 23, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HistoricalConfirmationChangesOnlyCoverageAndAddsOneManualEvent()
    {
        var tracker = Completed();
        var saved = tracker.ActiveSession!;
        var baseline = saved.Tracks[XpClass.Assault].Baseline!;
        var baselineIdentity = JsonSerializer.Serialize(new { baseline.Id, baseline.Version, baseline.Xp, baseline.At, baseline.Origin, baseline.RelatedSessionId });
        var final = JsonSerializer.Serialize(saved.ConfirmedFinal, Json);
        var raw = JsonSerializer.Serialize(saved.FinalObservations, Json);
        var start = saved.StartedAt; var end = saved.EndedAt;
        tracker.StartSession();
        tracker.ManualBaseline(XpClass.Medic, 60_000);
        tracker.ManualCurrent(XpClass.Medic, 62_500);
        tracker.ArmRebase(XpClass.Medic);
        var activeId = tracker.Document.ActiveSessionId;
        var active = JsonSerializer.Serialize(tracker.ActiveSession, Json);
        var history = saved.History.Count;

        var result = tracker.ConfirmCoverageForSession(saved.Id, XpClass.Assault);

        Assert.True(result.Applied); Assert.True(result.Changed);
        Assert.Equal(BaselineCoverage.BeforeEarnedXp, baseline.Coverage);
        Assert.Equal(BaselineValidity.Valid, saved.Tracks[XpClass.Assault].OriginalBaseline!.Validity);
        Assert.Equal(baselineIdentity, JsonSerializer.Serialize(new { baseline.Id, baseline.Version, baseline.Xp, baseline.At, baseline.Origin, baseline.RelatedSessionId }));
        Assert.Equal(start, saved.StartedAt); Assert.Equal(end, saved.EndedAt);
        Assert.Equal(final, JsonSerializer.Serialize(saved.ConfirmedFinal, Json));
        Assert.Equal(raw, JsonSerializer.Serialize(saved.FinalObservations, Json));
        Assert.Equal(activeId, tracker.Document.ActiveSessionId);
        Assert.Equal(active, JsonSerializer.Serialize(tracker.ActiveSession, Json));
        Assert.Equal(history + 1, saved.History.Count);
        var recorded = saved.History[^1];
        Assert.Equal("CoverageConfirmed", recorded.Kind); Assert.Equal(ReadingSource.Manual, recorded.Source);
        Assert.Equal(baseline.Id, recorded.BaselineId); Assert.Equal(baseline.Version, recorded.BaselineVersion);
        Assert.Single(saved.History, entry => entry.Kind == "MaximumTierReached");
    }

    [Fact]
    public void CertificationEntersAnalyticsWithoutChangingFinalReceiptOrLiveTarget()
    {
        var tracker = Completed(); var saved = tracker.ActiveSession!;
        Assert.Equal(AnalyticsRecordStatus.UnconfirmedBaseline, AnalyticsEngine.Build(tracker.Document).Records.Single(row => row.Class == XpClass.Assault).Status);
        var targetTime = saved.Tracks[XpClass.Assault].TargetReachedAt;
        tracker.StartSession();
        tracker.ConfirmCoverageForSession(saved.Id, XpClass.Assault);
        var row = AnalyticsEngine.Build(tracker.Document).Records.Single(item => item.SessionId == saved.Id && item.Class == XpClass.Assault);
        Assert.True(row.IncludedInReceived);
        Assert.Equal(3_500, row.Split!.MinimumEarnedXp); Assert.Equal(3_000, row.Split.MinimumBonusXp);
        Assert.Equal(targetTime, saved.Tracks[XpClass.Assault].TargetReachedAt);
        Assert.Null(tracker.ActiveSession!.ConfirmedFinal); Assert.False(tracker.ActiveSession.Tracks[XpClass.Assault].TargetReached);
    }

    [Fact]
    public void LegacyRoundTripCertifiesBothDetachedOriginalCopiesAndIsIdempotent()
    {
        var tracker = Completed(); var id = tracker.ActiveSession!.Id;
        tracker.StartSession();
        var payload = JsonNode.Parse(JsonSerializer.Serialize(tracker.Document, Json))!;
        foreach (var session in payload["Sessions"]!.AsArray()) session!.AsObject().Remove("FinalObservations");
        var restored = new SessionTracker(JsonSerializer.Deserialize<SeasonDocument>(payload.ToJsonString(), Json), () => At.AddHours(1));
        var old = restored.Document.Sessions.Single(session => session.Id == id);
        Assert.NotSame(old.Tracks[XpClass.Assault].Baseline, old.Tracks[XpClass.Assault].OriginalBaseline);
        Assert.Equal([XpClass.Assault], restored.GetConfirmableCoverageClasses(id));
        Assert.True(restored.ConfirmCoverageForSession(id, XpClass.Assault).Changed);
        Assert.Equal(BaselineCoverage.BeforeEarnedXp, old.Tracks[XpClass.Assault].Baseline!.Coverage);
        Assert.Equal(BaselineCoverage.BeforeEarnedXp, old.Tracks[XpClass.Assault].OriginalBaseline!.Coverage);
        var confirmed = JsonSerializer.Serialize(restored.Document, Json);
        var repeat = restored.ConfirmCoverageForSession(id, XpClass.Assault);
        Assert.True(repeat.Applied); Assert.False(repeat.Changed);
        Assert.Empty(restored.GetConfirmableCoverageClasses(id));
        Assert.Equal(confirmed, JsonSerializer.Serialize(restored.Document, Json));
        TrackingDocumentValidator.Validate(restored.Document);
    }

    [Fact]
    public void ConfirmAllUsesOnlyEligibleUnconfirmedTracksFromTheNamedSession()
    {
        var tracker = New(); tracker.StartSession();
        foreach (var track in Enum.GetValues<XpClass>()) tracker.ManualBaseline(track, 10_000);
        tracker.ConfirmCoverage(XpClass.Medic);
        tracker.ArmRebase(XpClass.Support); tracker.ManualBaseline(XpClass.Support, 10_100);
        tracker.SetTrackEnabled(XpClass.Driver, false);
        tracker.ActiveSession!.Tracks[XpClass.Pilot].OriginalBaseline!.Validity = BaselineValidity.Invalid;
        tracker.End(SessionOutcome.Unknown);
        var old = tracker.ActiveSession!;
        tracker.StartSession(); tracker.ManualBaseline(XpClass.Assault, 20_000);
        var active = JsonSerializer.Serialize(tracker.ActiveSession, Json);
        Assert.Equal([XpClass.Assault, XpClass.Recon], tracker.GetConfirmableCoverageClasses(old.Id));
        var before = old.History.Count;
        var result = tracker.ConfirmCoverageForSession(old.Id);
        Assert.True(result.Applied); Assert.True(result.Changed);
        Assert.Equal(before + 2, old.History.Count);
        Assert.Empty(tracker.GetConfirmableCoverageClasses(old.Id));
        Assert.Equal(BaselineCoverage.Partial, old.Tracks[XpClass.Support].Baseline!.Coverage);
        Assert.Equal(BaselineCoverage.Unknown, old.Tracks[XpClass.Driver].OriginalBaseline!.Coverage);
        Assert.Equal(BaselineValidity.Invalid, old.Tracks[XpClass.Pilot].OriginalBaseline!.Validity);
        Assert.Equal(active, JsonSerializer.Serialize(tracker.ActiveSession, Json));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("disabled")]
    [InlineData("rebased")]
    [InlineData("invalid")]
    [InlineData("partial")]
    [InlineData("mismatched-value")]
    [InlineData("mismatched-identity")]
    public void IneligibleHistoricalBaselinesRejectWithoutMutation(string reason)
    {
        var tracker = New(); tracker.StartSession(); tracker.ManualBaseline(XpClass.Assault, 1_000);
        var old = tracker.ActiveSession!; var track = old.Tracks[XpClass.Assault];
        switch (reason)
        {
            case "missing": track.OriginalBaseline = null; break;
            case "disabled": tracker.SetTrackEnabled(XpClass.Assault, false); break;
            case "rebased": tracker.ArmRebase(XpClass.Assault); tracker.ManualBaseline(XpClass.Assault, 1_100); break;
            case "invalid": track.OriginalBaseline!.Validity = BaselineValidity.Invalid; break;
            case "partial": track.Baseline!.Coverage = BaselineCoverage.Partial; break;
            case "mismatched-value":
                track.OriginalBaseline = JsonSerializer.Deserialize<BaselineRecord>(JsonSerializer.Serialize(track.Baseline, Json), Json)!;
                track.OriginalBaseline.Xp++; break;
            case "mismatched-identity":
                track.OriginalBaseline = JsonSerializer.Deserialize<BaselineRecord>(JsonSerializer.Serialize(track.Baseline, Json), Json)!;
                track.OriginalBaseline.Id = Guid.NewGuid(); break;
        }
        tracker.End(SessionOutcome.Unknown); tracker.StartSession();
        var before = JsonSerializer.Serialize(tracker.Document, Json);
        Assert.Empty(tracker.GetConfirmableCoverageClasses(old.Id));
        var result = tracker.ConfirmCoverageForSession(old.Id, XpClass.Assault);
        Assert.False(result.Applied); Assert.False(result.Changed);
        Assert.Equal(before, JsonSerializer.Serialize(tracker.Document, Json));
    }

    [Theory]
    [InlineData(TrackerSessionState.Active)]
    [InlineData(TrackerSessionState.Frozen)]
    [InlineData(TrackerSessionState.Ended)]
    public void NamedSessionCanBeCertifiedWithoutAnActiveSelection(TrackerSessionState state)
    {
        var tracker = New(); tracker.StartSession(); tracker.ManualBaseline(XpClass.Assault, 1_000);
        var saved = tracker.ActiveSession!;
        if (state == TrackerSessionState.Frozen) tracker.Observe(Score(), ReadingSource.AutomaticOcr);
        if (state == TrackerSessionState.Ended) tracker.End(SessionOutcome.Unknown);
        var beforeState = saved.State; var beforeFinal = JsonSerializer.Serialize(saved.PendingFinal, Json);
        tracker.Document.ActiveSessionId = null;
        Assert.True(tracker.ConfirmCoverageForSession(saved.Id, XpClass.Assault).Changed);
        Assert.Null(tracker.Document.ActiveSessionId); Assert.Equal(beforeState, saved.State);
        Assert.Equal(beforeFinal, JsonSerializer.Serialize(saved.PendingFinal, Json));
        Assert.Null(saved.ConfirmedFinal); Assert.False(saved.Tracks[XpClass.Assault].TargetReached);
    }

    [Fact]
    public void UnknownSessionAndUnknownClassRejectWithoutMutation()
    {
        var tracker = Completed(); var before = JsonSerializer.Serialize(tracker.Document, Json);
        Assert.Empty(tracker.GetConfirmableCoverageClasses(Guid.NewGuid()));
        Assert.False(tracker.ConfirmCoverageForSession(Guid.NewGuid(), XpClass.Assault).Applied);
        Assert.False(tracker.ConfirmCoverageForSession(Guid.NewGuid()).Applied);
        Assert.False(tracker.ConfirmCoverageForSession(tracker.ActiveSession!.Id, (XpClass)99).Applied);
        Assert.Equal(before, JsonSerializer.Serialize(tracker.Document, Json));
    }

    private static SessionTracker New() => new(clock: () => At);
    private static SessionTracker Completed()
    {
        var tracker = New(); tracker.StartSession();
        tracker.Observe(Read(10_000), ReadingSource.SnapshotHotkey, true);
        tracker.Observe(Read(12_500), ReadingSource.CheckHotkey);
        tracker.Observe(Read(16_500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        tracker.ConfirmFinal(3);
        return tracker;
    }
    private static ScreenReadResult Read(long xp, ScreenKind screen = ScreenKind.Unlock) => new(screen, ReadStatus.Accepted, "Coverage fixture",
        [new(XpClass.Assault, xp, 1_000_000, 15, 16, screen, At, new(.4, .2, .1, .02), $"{xp}/1000000XP", 1)], PlacementEvidence.None, [], []);
    private static ScreenReadResult Score() => new(ScreenKind.EndScore, ReadStatus.Accepted, "Score fixture", [],
        new([2, 3], false, [new(PlacementCue.Defeat, [2, 3], new(.3, .2, .4, .1), "DEFEAT")]), [], []);
}
