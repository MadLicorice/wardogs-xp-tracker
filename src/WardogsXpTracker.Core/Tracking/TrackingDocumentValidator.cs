using System.IO;
using System.Diagnostics.CodeAnalysis;
using WardogsXpTracker.Core.Reading;

namespace WardogsXpTracker.Core.Tracking;

/// <summary>Rejects incomplete saved data before a tracker can change it.</summary>
public static class TrackingDocumentValidator
{
    public static void Validate(SeasonDocument document)
    {
        Require(document is not null, "The season document is missing.");
        Require(document!.SchemaVersion == 1, "This season save uses an unsupported schema.");
        Require(document.SeasonId != Guid.Empty && !string.IsNullOrWhiteSpace(document.Name), "The season identity is missing.");
        Require(document.Sessions is not null && document.EnabledTracks is not null, "The season lists are missing.");
        Require(document.EnabledTracks!.Count == 6 && document.EnabledTracks.Keys.All(Enum.IsDefined), "The class choices are incomplete or unknown.");
        Require(document.Sessions!.All(s => s is not null && s.Id != Guid.Empty)
            && document.Sessions.Select(s => s.Id).Distinct().Count() == document.Sessions.Count, "Session identities are missing or duplicated.");
        Require(document.ActiveSessionId is null || document.ActiveSessionId != Guid.Empty
            && document.Sessions.Any(s => s.Id == document.ActiveSessionId), "The active session is missing from the season save.");
        foreach (var session in document.Sessions)
        {
            Require(session.StartedAt != default && Enum.IsDefined(session.State) && Enum.IsDefined(session.Outcome), "The session state or start time is invalid.");
            ValidateRules(session.RulesSnapshot);
            Require(session.Tracks is not null && session.Tracks.Count == 6 && session.Tracks.Keys.All(Enum.IsDefined), "The session classes are incomplete or unknown.");
            Require(session.History is not null && session.History.All(e => e is not null), "The session history is missing.");
            Require(session.ArmedRebase is null || Enum.IsDefined(session.ArmedRebase.Value), "The pending rebase class is invalid.");
            foreach (var (key, track) in session.Tracks!)
            {
                Require(track is not null && track.Class == key, "A track has the wrong class identity.");
                ValidateBaseline(track!.Baseline);
                ValidateBaseline(track.OriginalBaseline);
                ValidateObservation(track.Current, key);
                ValidateObservation(track.LatestReading, key);
                ValidateObservation(track.ClosingXp, key);
                Require(track.Baseline is null || track.Current is null || track.Current.Xp >= track.Baseline.Xp,
                    "Current XP is below the active baseline.");
            }
            foreach (var item in session.History!)
            {
                Require(item.Id != Guid.Empty && item.At != default && !string.IsNullOrWhiteSpace(item.Kind) && item.Message is not null,
                    "A history record is incomplete.");
                Require(item.Class is null || Enum.IsDefined(item.Class.Value), "A history class is unknown.");
                Require(item.Source is null || Enum.IsDefined(item.Source.Value), "A history source is unknown.");
                Require(item.Screen is null || Enum.IsDefined(item.Screen.Value), "A history screen is unknown.");
                ValidateObservation(item.Observation, item.Class);
            }
            ValidateFinal(session.PendingFinal, session.Id, false);
            ValidateFinal(session.ConfirmedFinal, session.Id, true);
            Require(session.FinalObservations is not null && session.FinalObservations.All(final => final is not null),
                "Observed final result backups are incomplete.");
            foreach (var observed in session.FinalObservations!) ValidateFinal(observed, session.Id, false);
            Require(session.PendingFinal is null || session.State != TrackerSessionState.Active, "Pending results cannot advance an active counter.");
        }
    }

    public static void ValidateRules(RuleSetSnapshot rules)
    {
        Require(rules is not null && rules.Id != Guid.Empty && rules.Version > 0 && !string.IsNullOrWhiteSpace(rules.Name)
            && rules.PatchDate != default, "The ruleset identity is incomplete.");
        Require(rules!.Tiers is not null && rules.Tiers.Count >= 2 && rules.Tiers.All(t => t is not null), "Bonus tiers are missing.");
        Require(rules.Tiers![0].MinimumXp == 0 && rules.Tiers[^1].MinimumXp > 0, "Bonus rules need a zero tier and a positive target.");
        long previous = -1;
        foreach (var tier in rules.Tiers)
        {
            Require(tier.MinimumXp > previous && tier.Third >= 0 && tier.Second >= 0 && tier.First >= 0,
                "Bonus thresholds must increase and bonuses must be nonnegative.");
            previous = tier.MinimumXp;
        }
    }

    private static void ValidateBaseline(BaselineRecord? baseline)
    {
        if (baseline is null) return;
        Require(baseline.Id != Guid.Empty && baseline.Version > 0 && baseline.Xp >= 0 && baseline.At != default
            && Enum.IsDefined(baseline.Origin) && Enum.IsDefined(baseline.Coverage) && Enum.IsDefined(baseline.Validity), "A baseline record is invalid.");
        Require(baseline.RelatedSessionId is null || baseline.RelatedSessionId != Guid.Empty, "A reused baseline has no related session identity.");
    }

    private static void ValidateObservation(XpObservation? observation, XpClass? expected)
    {
        if (observation is null) return;
        Require(observation.Id != Guid.Empty && observation.Xp >= 0 && observation.At != default
            && Enum.IsDefined(observation.Class) && (expected is null || observation.Class == expected)
            && Enum.IsDefined(observation.Source) && Enum.IsDefined(observation.Screen) && observation.RawText is not null,
            "An XP observation is invalid.");
        Require(observation.NextThreshold is null || observation.NextThreshold > 0 && observation.Xp <= observation.NextThreshold,
            "An XP observation exceeds its threshold.");
        Require(observation.CurrentLevel is null or >= 0 && observation.ThresholdLevel is null or >= 0, "A level observation is invalid.");
        Require(observation.Confidence is null || double.IsFinite(observation.Confidence.Value)
            && observation.Confidence is >= 0 and <= 1, "An OCR confidence is invalid.");
    }

    private static void ValidateFinal(FinalResultSet? final, Guid sessionId, bool confirmed)
    {
        if (final is null) return;
        Require(final.SessionId == sessionId && final.FirstObservedAt != default && final.LastObservedAt != default,
            "Final results have no matching session or timestamp.");
        Require(!confirmed || final.ConfirmedAt is not null, "Confirmed results have no confirmation time.");
        Require(final.Tracks is not null && final.PossiblePlaces is not null && final.PlacementEvidence is not null
            && final.ConflictingTracks is not null, "Final result lists are missing.");
        foreach (var (track, observation) in final.Tracks!)
        {
            Require(Enum.IsDefined(track) && observation is not null, "A final class is invalid.");
            ValidateObservation(observation, track);
        }
        Require(final.PossiblePlaces!.All(p => p is >= 1 and <= 3) && final.ConflictingTracks!.All(Enum.IsDefined), "Final placement or class evidence is invalid.");
        Require(final.PlacementEvidence!.All(p => p is not null && p.At != default && Enum.IsDefined(p.Source)
            && p.PossiblePlaces is not null && p.PossiblePlaces.All(n => n is >= 1 and <= 3) && p.RawText is not null), "Placement evidence is incomplete.");
    }

    private static void Require([DoesNotReturnIf(false)] bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
