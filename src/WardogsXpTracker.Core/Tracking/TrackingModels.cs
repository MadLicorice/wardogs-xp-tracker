using System.Text.Json.Serialization;
using WardogsXpTracker.Core.Reading;

namespace WardogsXpTracker.Core.Tracking;

public enum TrackerSessionState { Active, Frozen, Ended }
public enum SessionOutcome { Unknown, Finished, LeftMatch, Disconnected }
public enum ReadingSource { AutomaticOcr, SnapshotHotkey, CheckHotkey, Manual, ReusedFinal }
public enum BaselineCoverage { Unknown, BeforeEarnedXp, Partial }
public enum BaselineValidity { Unknown, Valid, Invalid }

public sealed class SeasonDocument
{
    [JsonRequired]
    public int SchemaVersion { get; set; } = 1;
    [JsonRequired]
    public Guid SeasonId { get; set; } = Guid.NewGuid();
    [JsonRequired]
    public string Name { get; set; } = "Season 2";
    [JsonRequired]
    public List<TrackerSession> Sessions { get; set; } = [];
    [JsonRequired]
    public Guid? ActiveSessionId { get; set; }
    [JsonRequired]
    public Dictionary<XpClass, bool> EnabledTracks { get; set; } = Enum.GetValues<XpClass>().ToDictionary(c => c, _ => true);
}

public sealed record BonusTier(
    [property: JsonRequired] long MinimumXp,
    [property: JsonRequired] long Third,
    [property: JsonRequired] long Second,
    [property: JsonRequired] long First);

public sealed class RuleSetSnapshot
{
    [JsonRequired]
    public Guid Id { get; set; } = Guid.Parse("6052b351-2d66-4de1-8d62-f03a037b9a2e");
    [JsonRequired]
    public int Version { get; set; } = 1;
    [JsonRequired]
    public DateOnly PatchDate { get; set; } = new(2026, 10, 15);
    [JsonRequired]
    public string Name { get; set; } = "Oct 15 2026 Patch XP Bonus Rules";
    [JsonRequired]
    public List<BonusTier> Tiers { get; set; } =
    [new(0, 0, 0, 0), new(500, 600, 700, 800), new(1000, 1200, 1400, 1600),
        new(1500, 1800, 2100, 2400), new(2000, 2400, 2800, 3200), new(2500, 3000, 3500, 4000)];
    [JsonIgnore] public long TargetXp => Tiers.Max(t => t.MinimumXp);
    public BonusTier TierFor(long gain) => Tiers.Where(t => t.MinimumXp <= Math.Max(0, gain))
        .OrderBy(t => t.MinimumXp).LastOrDefault() ?? new(0, 0, 0, 0);
    public RuleSetSnapshot Copy() => new()
    {
        Id = Id, Version = Version, PatchDate = PatchDate, Name = Name,
        Tiers = Tiers.Select(t => t with { }).ToList()
    };
}

public sealed class TrackerSession
{
    [JsonRequired]
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonRequired]
    public DateTimeOffset StartedAt { get; set; }
    [JsonRequired]
    public DateTimeOffset? EndedAt { get; set; }
    [JsonRequired]
    public TrackerSessionState State { get; set; } = TrackerSessionState.Active;
    [JsonRequired]
    public SessionOutcome Outcome { get; set; } = SessionOutcome.Unknown;
    [JsonRequired]
    public RuleSetSnapshot RulesSnapshot { get; set; } = new();
    [JsonRequired]
    public Dictionary<XpClass, TrackState> Tracks { get; set; } = Enum.GetValues<XpClass>()
        .ToDictionary(c => c, c => new TrackState { Class = c });
    [JsonRequired]
    public List<TrackingEvent> History { get; set; } = [];
    [JsonRequired]
    public FinalResultSet? PendingFinal { get; set; }
    [JsonRequired]
    public FinalResultSet? ConfirmedFinal { get; set; }
    // Optional for schema-1 saves created before observed-result backups were added.
    public List<FinalResultSet> FinalObservations { get; set; } = [];
    [JsonRequired]
    public XpClass? ArmedRebase { get; set; }
    [JsonRequired]
    public bool RebaseInvalidatesOriginal { get; set; }
    [JsonRequired]
    public bool HasUnlockMarker { get; set; }
    [JsonRequired]
    public bool PostEndUnlockObserved { get; set; }
}

public sealed class TrackState
{
    [JsonRequired]
    public XpClass Class { get; set; }
    [JsonRequired]
    public bool Enabled { get; set; } = true;
    [JsonRequired]
    public BaselineRecord? Baseline { get; set; }
    [JsonRequired]
    public BaselineRecord? OriginalBaseline { get; set; }
    [JsonRequired]
    public XpObservation? Current { get; set; }
    [JsonRequired]
    public XpObservation? LatestReading { get; set; }
    [JsonRequired]
    public XpObservation? ClosingXp { get; set; }
    [JsonRequired]
    public DateTimeOffset? TargetReachedAt { get; set; }
    [JsonRequired]
    public bool WasRebased { get; set; }
    [JsonIgnore] public long? MeasuredGain => Baseline is null || Current is null ? null : Current.Xp - Baseline.Xp;
    [JsonIgnore] public bool TargetReached => TargetReachedAt is not null;
}

public sealed class BaselineRecord
{
    [JsonRequired]
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonRequired]
    public int Version { get; set; } = 1;
    [JsonRequired]
    public long Xp { get; set; }
    [JsonRequired]
    public DateTimeOffset At { get; set; }
    [JsonRequired]
    public ReadingSource Origin { get; set; }
    [JsonRequired]
    public BaselineCoverage Coverage { get; set; } = BaselineCoverage.Unknown;
    [JsonRequired]
    public BaselineValidity Validity { get; set; } = BaselineValidity.Unknown;
    [JsonRequired]
    public string? ValidityReason { get; set; }
    [JsonRequired]
    public Guid? RelatedSessionId { get; set; }
}

public sealed class XpObservation
{
    [JsonRequired]
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonRequired]
    public XpClass Class { get; set; }
    [JsonRequired]
    public long Xp { get; set; }
    [JsonRequired]
    public long? NextThreshold { get; set; }
    [JsonRequired]
    public int? CurrentLevel { get; set; }
    [JsonRequired]
    public int? ThresholdLevel { get; set; }
    [JsonRequired]
    public DateTimeOffset At { get; set; }
    [JsonRequired]
    public ReadingSource Source { get; set; }
    [JsonRequired]
    public ScreenKind Screen { get; set; }
    [JsonRequired]
    public string RawText { get; set; } = "";
    [JsonRequired]
    public double? Confidence { get; set; }
}

public sealed class FinalResultSet
{
    [JsonRequired]
    public Guid SessionId { get; set; }
    [JsonRequired]
    public DateTimeOffset FirstObservedAt { get; set; }
    [JsonRequired]
    public DateTimeOffset LastObservedAt { get; set; }
    [JsonRequired]
    public DateTimeOffset? ConfirmedAt { get; set; }
    [JsonRequired]
    public Dictionary<XpClass, XpObservation> Tracks { get; set; } = [];
    [JsonRequired]
    public List<int> PossiblePlaces { get; set; } = [];
    [JsonRequired]
    public List<PlacementRecord> PlacementEvidence { get; set; } = [];
    [JsonRequired]
    public bool HasConflict { get; set; }
    [JsonRequired]
    public List<XpClass> ConflictingTracks { get; set; } = [];
    [JsonRequired]
    public bool KeepOldReferencesChosen { get; set; }
}

public sealed class PlacementRecord
{
    [JsonRequired]
    public DateTimeOffset At { get; set; }
    [JsonRequired]
    public ReadingSource Source { get; set; }
    [JsonRequired]
    public List<int> PossiblePlaces { get; set; } = [];
    [JsonRequired]
    public string RawText { get; set; } = "";
}

public sealed class TrackingEvent
{
    [JsonRequired]
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonRequired]
    public string Kind { get; set; } = "";
    [JsonRequired]
    public DateTimeOffset At { get; set; }
    [JsonRequired]
    public XpClass? Class { get; set; }
    [JsonRequired]
    public long? Xp { get; set; }
    [JsonRequired]
    public long? PreviousXp { get; set; }
    [JsonRequired]
    public Guid? BaselineId { get; set; }
    [JsonRequired]
    public int? BaselineVersion { get; set; }
    [JsonRequired]
    public ReadingSource? Source { get; set; }
    [JsonRequired]
    public ScreenKind? Screen { get; set; }
    [JsonRequired]
    public string Message { get; set; } = "";
    [JsonRequired]
    public XpObservation? Observation { get; set; }
}

public sealed record TrackerActionResult(bool Applied, bool Changed, string Message, bool NeedsFinalConfirmation = false);
