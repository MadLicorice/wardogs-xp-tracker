using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Analytics;

public enum BonusOutcome { NoBonus = 0, First = 1, Second = 2, Third = 3 }
public enum XpSplitStatus { Exact, Ambiguous, NoSolution, ConflictingEvidence, InvalidTotal, InvalidRules }
public enum AnalyticsRecordStatus
{
    Included, Disabled, MissingBaseline, UnconfirmedBaseline, PartialCoverage,
    PendingFinal, MissingFinal, RulesMismatch, ConflictingEvidence, InvalidTotal
}

public sealed record BonusSplit(BonusOutcome Outcome, long EarnedXp, long BonusXp, long TierMinimumXp);

public sealed record XpSplitResult
{
    public XpSplitStatus Status { get; init; }
    public long TotalGain { get; init; }
    public long MinimumEarnedXp { get; init; }
    public long MinimumBonusXp { get; init; }
    public long IndeterminateXp { get; init; }
    public IReadOnlyList<BonusSplit> Solutions { get; init; } = [];
    public string Explanation { get; init; } = "";
}

public sealed record XpSourceTotals(decimal EarnedXp, decimal BonusXp, decimal IndeterminateXp)
{
    public decimal TotalXp => EarnedXp + BonusXp + IndeterminateXp;
}

public sealed record MissedBonusEstimate
{
    public long Minimum { get; init; }
    public decimal Average { get; init; }
    public long Maximum { get; init; }
    public long SupportedGain { get; init; }
    public bool IsLowerBound { get; init; }
    public bool AtMaximumTier { get; init; }
    public string Explanation { get; init; } = "";
}

public sealed record SessionClassAnalytics
{
    public Guid SessionId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public XpClass Class { get; init; }
    public SessionOutcome Outcome { get; init; }
    public string RulesName { get; init; } = "";
    public Guid RulesId { get; init; }
    public int RulesVersion { get; init; }
    public Guid? BaselineId { get; init; }
    public int? BaselineVersion { get; init; }
    public long? StartingXp { get; init; }
    public long? EndingXp { get; init; }
    public long? TotalGain { get; init; }
    public long SupportedEarnedLowerBound { get; init; }
    public bool IncludedInReceived { get; init; }
    public AnalyticsRecordStatus Status { get; init; }
    public string Explanation { get; init; } = "";
    public string SourceNotes { get; init; } = "";
    public XpSplitResult? Split { get; init; }
    public MissedBonusEstimate? MissedBonus { get; init; }
    public IReadOnlyList<Guid> EvidenceObservationIds { get; init; } = [];
}

public sealed record ClassAnalytics
{
    public XpClass Class { get; init; }
    public XpSourceTotals Received { get; init; } = new(0, 0, 0);
    public decimal AverageMissedBonus { get; init; }
    public decimal MinimumMissedBonus { get; init; }
    public decimal MaximumMissedBonus { get; init; }
    public bool HasLowerBoundMissedBonus { get; init; }
    public int IncludedRecords { get; init; }
    public int ExcludedRecords { get; init; }
    public int MissedRecords { get; init; }
}

public sealed record AnalyticsCoverage
{
    public int Sessions { get; init; }
    public int ClassRecords { get; init; }
    public int IncludedRecords { get; init; }
    public int DisabledRecords { get; init; }
    public int PendingRecords { get; init; }
    public int MissingBaselineRecords { get; init; }
    public int UnconfirmedBaselineRecords { get; init; }
    public int MissingFinalRecords { get; init; }
    public int PartialRecords { get; init; }
    public int ConflictRecords { get; init; }
}

public sealed record SeasonAnalytics
{
    public Guid SeasonId { get; init; }
    public string SeasonName { get; init; } = "";
    public string DerivationVersion { get; init; } = "";
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<ClassAnalytics> Classes { get; init; } = [];
    public XpSourceTotals Totals { get; init; } = new(0, 0, 0);
    public decimal AverageMissedBonus { get; init; }
    public decimal MinimumMissedBonus { get; init; }
    public decimal MaximumMissedBonus { get; init; }
    public bool HasLowerBoundMissedBonus { get; init; }
    public AnalyticsCoverage Coverage { get; init; } = new();
    public IReadOnlyList<SessionClassAnalytics> Records { get; init; } = [];
}
