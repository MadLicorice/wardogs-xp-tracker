using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Analytics;

/// <summary>Derives local analytics without changing the saved season or live checks.</summary>
public static class AnalyticsEngine
{
    public const string CurrentDerivationVersion = "xp-sources-1";

    public static SeasonAnalytics Build(SeasonDocument document, DateTimeOffset? generatedAt = null)
    {
        TrackingDocumentValidator.Validate(document);
        var records = document.Sessions.OrderBy(session => session.StartedAt)
            .SelectMany(session => Enum.GetValues<XpClass>().Select(trackClass => BuildRecord(session, session.Tracks[trackClass])))
            .ToArray();
        var classes = Enum.GetValues<XpClass>().Select(trackClass => AggregateClass(trackClass,
            records.Where(record => record.Class == trackClass).ToArray())).ToArray();
        return new()
        {
            SeasonId = document.SeasonId, SeasonName = document.Name,
            DerivationVersion = CurrentDerivationVersion, GeneratedAt = generatedAt ?? DateTimeOffset.UtcNow,
            Records = Array.AsReadOnly(records), Classes = Array.AsReadOnly(classes),
            Totals = SumReceived(records),
            MinimumMissedBonus = classes.Sum(item => item.MinimumMissedBonus),
            AverageMissedBonus = classes.Sum(item => item.AverageMissedBonus),
            MaximumMissedBonus = classes.Sum(item => item.MaximumMissedBonus),
            HasLowerBoundMissedBonus = classes.Any(item => item.HasLowerBoundMissedBonus),
            Coverage = new()
            {
                Sessions = document.Sessions.Count, ClassRecords = records.Length,
                IncludedRecords = records.Count(record => record.IncludedInReceived),
                DisabledRecords = Count(AnalyticsRecordStatus.Disabled),
                PendingRecords = Count(AnalyticsRecordStatus.PendingFinal),
                MissingBaselineRecords = Count(AnalyticsRecordStatus.MissingBaseline),
                UnconfirmedBaselineRecords = Count(AnalyticsRecordStatus.UnconfirmedBaseline),
                MissingFinalRecords = Count(AnalyticsRecordStatus.MissingFinal),
                PartialRecords = Count(AnalyticsRecordStatus.PartialCoverage),
                ConflictRecords = records.Count(record => record.Status is AnalyticsRecordStatus.ConflictingEvidence
                    or AnalyticsRecordStatus.RulesMismatch or AnalyticsRecordStatus.InvalidTotal)
            }
        };
        int Count(AnalyticsRecordStatus status) => records.Count(record => record.Status == status);
    }

    private static SessionClassAnalytics BuildRecord(TrackerSession session, TrackState track)
    {
        var original = track.OriginalBaseline;
        var reliableOriginal = IsWholeMatchBaseline(original);
        var noBonus = session.Outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected;
        var final = session.ConfirmedFinal;
        var confirmedEnding = final?.Tracks.GetValueOrDefault(track.Class);
        var pendingEnding = session.PendingFinal?.Tracks.GetValueOrDefault(track.Class);
        var ending = noBonus ? track.ClosingXp
            : confirmedEnding ?? pendingEnding;
        var evidence = CollectEvidence(session, track, original, ending?.At);
        IEnumerable<EvidencePoint> wholeEvidence = reliableOriginal ? evidence.Points.Where(point => IsAfterOriginal(point, original!, evidence.OriginalEventIndex)) : [];
        var wholePoints = wholeEvidence.ToArray();
        var wholeGain = reliableOriginal ? wholePoints.Select(point => Math.Max(0, point.Observation.Xp - original!.Xp)).DefaultIfEmpty(0).Max() : 0;
        var interval = SupportedIntervalGain(track, original, evidence.Points, noBonus ? track.ClosingXp : null);
        var hasConflictingPreBonus = reliableOriginal && wholePoints.Any(point => point.Observation.Xp < original!.Xp);
        var supportedGain = reliableOriginal && !hasConflictingPreBonus ? wholeGain : interval.Gain;
        var completeNoBonus = noBonus && reliableOriginal && track.ClosingXp is not null;
        if (completeNoBonus && track.ClosingXp!.Xp >= original!.Xp)
            supportedGain = Math.Max(supportedGain, track.ClosingXp.Xp - original.Xp);
        var ids = evidence.Points.Select(point => point.Observation.Id).ToList();
        if (ending is not null) ids.Add(ending.Id);
        var record = new SessionClassAnalytics
        {
            SessionId = session.Id, StartedAt = session.StartedAt, Class = track.Class, Outcome = session.Outcome,
            RulesName = session.RulesSnapshot.Name, RulesId = session.RulesSnapshot.Id, RulesVersion = session.RulesSnapshot.Version,
            BaselineId = original?.Id, BaselineVersion = original?.Version,
            StartingXp = original?.Xp, EndingXp = ending?.Xp,
            TotalGain = original is not null && ending is not null ? ending.Xp - original.Xp : null,
            SupportedEarnedLowerBound = supportedGain,
            EvidenceObservationIds = ids.Distinct().ToArray(),
            SourceNotes = SourceNotes(session, track, ending, reliableOriginal),
            MissedBonus = noBonus && track.Enabled && (reliableOriginal || interval.HasEvidence)
                ? EstimateMissed(session.RulesSnapshot, supportedGain,
                    completeNoBonus && !hasConflictingPreBonus && track.ClosingXp!.Xp >= original!.Xp)
                : null
        };

        if (!track.Enabled) return Exclude(record, AnalyticsRecordStatus.Disabled,
            "This class was disabled. Its observed totals remain outside received-XP analytics.");
        if (original is null) return Exclude(record, AnalyticsRecordStatus.MissingBaseline,
            "No whole-match starting baseline was recorded. Missing XP is not zero.");
        if (original.Validity == BaselineValidity.Invalid || original.Coverage == BaselineCoverage.Partial)
            return Exclude(record, AnalyticsRecordStatus.PartialCoverage,
                "The original baseline cannot establish the whole match. Supported intervals remain separate.");
        if (!reliableOriginal) return Exclude(record, AnalyticsRecordStatus.UnconfirmedBaseline,
            "The original baseline was not confirmed as valid and captured before earned XP.");
        if (!noBonus && session.PendingFinal is not null && (final is null || confirmedEnding is null && pendingEnding is not null))
            return Exclude(record, AnalyticsRecordStatus.PendingFinal,
                "Final XP was observed but remains unconfirmed. Candidate totals are not received-XP analytics.");
        if (noBonus && track.ClosingXp is null) return Exclude(record, AnalyticsRecordStatus.PartialCoverage,
            "The no-bonus outcome is confirmed, but closing XP is missing. The last live reading is only a lower bound.");
        if (ending is null || !noBonus && final is null)
            return Exclude(record, AnalyticsRecordStatus.MissingFinal,
                "Confirmed final XP is unavailable for this class. The live reading does not replace it.");
        var gain = ending.Xp - original.Xp;
        if (gain < 0)
            return Exclude(record with { Split = XpBonusSolver.Unresolved(gain, XpSplitStatus.InvalidTotal,
                "The ending XP is below the original baseline.") }, AnalyticsRecordStatus.InvalidTotal,
                "The ending XP is below the original baseline. Review both observations.");

        var conflictingFinal = !noBonus && final is not null
            && (final.HasConflict || final.ConflictingTracks.Contains(track.Class));
        var split = hasConflictingPreBonus || conflictingFinal
            ? XpBonusSolver.Unresolved(gain, XpSplitStatus.ConflictingEvidence,
                "Recorded observations conflict. The known total remains Indeterminate XP until the evidence is corrected.")
            : XpBonusSolver.Solve(gain, session.RulesSnapshot, Outcomes(session, final), supportedGain);
        var status = split.Status switch
        {
            XpSplitStatus.NoSolution or XpSplitStatus.InvalidRules => AnalyticsRecordStatus.RulesMismatch,
            XpSplitStatus.ConflictingEvidence => AnalyticsRecordStatus.ConflictingEvidence,
            XpSplitStatus.InvalidTotal => AnalyticsRecordStatus.InvalidTotal,
            _ => AnalyticsRecordStatus.Included
        };
        return record with
        {
            IncludedInReceived = true, Status = status, Split = split,
            Explanation = split.Explanation
        };
    }

    private static bool IsWholeMatchBaseline(BaselineRecord? baseline) => baseline is
        { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp };

    private static SessionClassAnalytics Exclude(SessionClassAnalytics record, AnalyticsRecordStatus status, string explanation)
        => record with { Status = status, IncludedInReceived = false, Explanation = explanation };

    private static IEnumerable<BonusOutcome> Outcomes(TrackerSession session, FinalResultSet? final)
    {
        if (session.Outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected) return [BonusOutcome.NoBonus];
        if (final?.PossiblePlaces.Count > 0) return final.PossiblePlaces.Select(place => (BonusOutcome)place);
        return session.Outcome == SessionOutcome.Finished
            ? [BonusOutcome.First, BonusOutcome.Second, BonusOutcome.Third]
            : [BonusOutcome.NoBonus, BonusOutcome.First, BonusOutcome.Second, BonusOutcome.Third];
    }

    private static Evidence CollectEvidence(TrackerSession session, TrackState track, BaselineRecord? original, DateTimeOffset? finalAt)
    {
        var cutoff = session.EndedAt;
        if (finalAt is { } observedEnd && (cutoff is null || observedEnd < cutoff)) cutoff = observedEnd;
        var points = new List<EvidencePoint>();
        var seen = new HashSet<Guid>();
        int? originalEventIndex = null;
        for (var index = 0; index < session.History.Count; index++)
        {
            var item = session.History[index];
            if (item.Class != track.Class) continue;
            var startsInterval = item.Kind is "BaselineEstablished" or "BaselineRebased";
            if (startsInterval && item.BaselineId == original?.Id) originalEventIndex = index;
            if (item.Kind is not ("CurrentXp" or "BaselineEstablished" or "BaselineRebased" or "DisabledClassObserved")) continue;
            Add(item.Observation, startsInterval, item.BaselineId, index);
        }
        Add(track.Current, false, null, int.MaxValue - 1);
        Add(track.LatestReading, false, null, int.MaxValue);
        return new(points, originalEventIndex);

        void Add(XpObservation? observation, bool startsInterval, Guid? baselineId, int eventIndex)
        {
            if (observation is null || observation.Class != track.Class || observation.Screen != ScreenKind.Unlock
                || observation.Source == ReadingSource.ReusedFinal || observation.At < session.StartedAt
                || cutoff is not null && observation.At > cutoff || !seen.Add(observation.Id)) return;
            points.Add(new(observation, startsInterval, baselineId, eventIndex));
        }
    }

    private static bool IsAfterOriginal(EvidencePoint point, BaselineRecord original, int? originalEventIndex)
        => originalEventIndex is { } index ? point.EventIndex >= index : point.Observation.At >= original.At;

    private static (long Gain, bool HasEvidence) SupportedIntervalGain(TrackState track, BaselineRecord? original,
        IReadOnlyList<EvidencePoint> points, XpObservation? closing)
    {
        long? anchor = null;
        var hasEvidence = false;
        var gain = 0L;
        if (!points.Any(point => point.StartsInterval) && track.Baseline is { } baseline
            && !(original is { Validity: BaselineValidity.Invalid } && baseline.Id == original.Id))
        { anchor = baseline.Xp; hasEvidence = true; }
        foreach (var point in points)
        {
            if (point.StartsInterval)
            {
                anchor = original is { Validity: BaselineValidity.Invalid } && point.BaselineId == original.Id
                    ? null : point.Observation.Xp;
                hasEvidence |= anchor is not null;
                continue;
            }
            Include(point.Observation.Xp);
        }
        if (closing is not null) Include(closing.Xp);
        return (gain, hasEvidence);

        void Include(long xp)
        {
            hasEvidence = true;
            if (anchor is null || xp < anchor.Value) anchor = xp;
            else gain = Math.Max(gain, xp - anchor.Value);
        }
    }

    private static MissedBonusEstimate EstimateMissed(RuleSetSnapshot rules, long gain, bool completeGain)
    {
        var tier = rules.TierFor(gain);
        var atMaximum = gain >= rules.TargetXp;
        var lowerBound = !completeGain && !atMaximum;
        BonusTier[] reachable = lowerBound ? rules.Tiers.Where(item => item.MinimumXp >= tier.MinimumXp).ToArray() : [tier];
        return new()
        {
            Minimum = reachable.Min(item => Math.Min(item.Third, Math.Min(item.Second, item.First))),
            Average = reachable.Min(item => ((decimal)item.Third + item.Second + item.First) / 3m),
            Maximum = reachable.Min(item => Math.Max(item.Third, Math.Max(item.Second, item.First))),
            SupportedGain = gain, IsLowerBound = lowerBound, AtMaximumTier = atMaximum,
            Explanation = lowerBound
                ? "At least this missed bonus is supported. Closing XP is incomplete, so a higher tier can remain possible. Placements have equal weight."
                : "Calculated using bonus rules with equal weight for third, second, and first place. This bonus was not received."
        };
    }

    private static string SourceNotes(TrackerSession session, TrackState track, XpObservation? ending, bool reliableOriginal)
    {
        var start = reliableOriginal ? "Valid original baseline with confirmed starting coverage." : "Whole-match starting coverage is incomplete.";
        var finish = session.Outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected
            ? ending is null ? "User-reported no bonus. No closing total." : $"User-reported no bonus. Closing total: {ending.Source}."
            : session.ConfirmedFinal?.Tracks.ContainsKey(track.Class) == true ? $"Confirmed final total: {ending?.Source.ToString() ?? "unavailable"}."
            : session.PendingFinal?.Tracks.ContainsKey(track.Class) == true ? "Unconfirmed final candidate." : "No confirmed final total.";
        return $"{start} {finish}{(track.WasRebased ? " A separate measuring interval was rebased." : "")}";
    }

    private static ClassAnalytics AggregateClass(XpClass trackClass, SessionClassAnalytics[] records)
    {
        var misses = records.Where(record => record.MissedBonus is not null).Select(record => record.MissedBonus!).ToArray();
        return new()
        {
            Class = trackClass, Received = SumReceived(records), IncludedRecords = records.Count(record => record.IncludedInReceived),
            ExcludedRecords = records.Count(record => !record.IncludedInReceived), MissedRecords = misses.Length,
            MinimumMissedBonus = misses.Sum(item => (decimal)item.Minimum), AverageMissedBonus = misses.Sum(item => item.Average),
            MaximumMissedBonus = misses.Sum(item => (decimal)item.Maximum), HasLowerBoundMissedBonus = misses.Any(item => item.IsLowerBound)
        };
    }

    private static XpSourceTotals SumReceived(IEnumerable<SessionClassAnalytics> records)
    {
        var splits = records.Where(record => record.IncludedInReceived && record.Split is not null).Select(record => record.Split!).ToArray();
        return new(splits.Sum(split => (decimal)split.MinimumEarnedXp), splits.Sum(split => (decimal)split.MinimumBonusXp),
            splits.Sum(split => (decimal)split.IndeterminateXp));
    }

    private sealed record EvidencePoint(XpObservation Observation, bool StartsInterval, Guid? BaselineId, int EventIndex);
    private sealed record Evidence(IReadOnlyList<EvidencePoint> Points, int? OriginalEventIndex);
}
