using System.Text.Json.Serialization;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public enum InteractiveWalkthroughStep
{
    SelectGameWindow, StartNewSession, CaptureBaseline, ConfirmStartingCoverage,
    CheckCurrentXp, CaptureFinalXp, ConfirmFinalXp, OpenAnalytics, Completed
}

/// <summary>Records guide progress independently of player XP and tracker state.</summary>
public sealed record InteractiveWalkthroughState
{
    public int SchemaVersion { get; init; } = 1;
    public Guid RunId { get; init; } = Guid.NewGuid();
    public DateTimeOffset StartedAt { get; init; }
    public InteractiveWalkthroughStep Stage { get; init; } = InteractiveWalkthroughStep.SelectGameWindow;
    public bool IsPaused { get; init; }
    public IReadOnlyList<InteractiveWalkthroughStep> SkippedSteps { get; init; } = [];
    [JsonIgnore] public bool HasSkippedSteps => SkippedSteps is { Count: > 0 };
    public Guid? SeasonId { get; init; }
    public Guid? SessionId { get; init; }
    public DateTimeOffset? SessionStartedAt { get; init; }
    public XpClass? Class { get; init; }
    public Guid? BaselineId { get; init; }
    public int? BaselineVersion { get; init; }
    public DateTimeOffset? BaselineCapturedAt { get; init; }
    public DateTimeOffset? CurrentCapturedAt { get; init; }
    public DateTimeOffset? FinalCapturedAt { get; init; }
    [JsonIgnore] public InteractiveWalkthroughStep Step => Stage;
}

/// <summary>Advances only from evidence supplied by completed, explicit app actions. It never changes a tracker.</summary>
public static class InteractiveWalkthroughProgress
{
    public static InteractiveWalkthroughState Start(DateTimeOffset now) => new() { StartedAt = now };

    public static InteractiveWalkthroughState Pause(InteractiveWalkthroughState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return IsSupported(state) && state.Stage != InteractiveWalkthroughStep.Completed && !state.IsPaused
            ? state with { IsPaused = true } : state;
    }

    public static InteractiveWalkthroughState Resume(InteractiveWalkthroughState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return IsSupported(state) && state.IsPaused ? state with { IsPaused = false } : state;
    }

    public static InteractiveWalkthroughState RestartForNextMatch(InteractiveWalkthroughState state, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(state);
        return IsSupported(state) ? Start(now) : state;
    }

    /// <summary>Moves one visible task forward without supplying any capture or confirmation evidence.</summary>
    public static InteractiveWalkthroughState SkipStep(InteractiveWalkthroughState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!CanAdvance(state) || state.Stage == InteractiveWalkthroughStep.Completed) return state;
        return state with { Stage = state.Stage + 1, SkippedSteps = [.. state.SkippedSteps, state.Stage] };
    }

    public static InteractiveWalkthroughState ObserveWindowSelected(InteractiveWalkthroughState state, bool isRealSelectedWindow)
        => At(state, InteractiveWalkthroughStep.SelectGameWindow) && isRealSelectedWindow
            ? state with { Stage = InteractiveWalkthroughStep.StartNewSession } : state;

    public static InteractiveWalkthroughState ObserveNewSession(InteractiveWalkthroughState state, Guid seasonId,
        TrackerSession session, bool isRealData)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!CanAdvance(state) || !isRealData || state.Stage < InteractiveWalkthroughStep.StartNewSession
            || state.Stage == InteractiveWalkthroughStep.Completed || session.State != TrackerSessionState.Active) return state;
        if (state.Stage != InteractiveWalkthroughStep.StartNewSession
            && (state.SessionId is not null || !Skipped(state, InteractiveWalkthroughStep.StartNewSession))) return state;
        var bound = BindSession(state, seasonId, session);
        if (!MatchesSession(bound, seasonId, session)) return state;
        return bound with { Stage = state.Stage == InteractiveWalkthroughStep.StartNewSession
            ? InteractiveWalkthroughStep.CaptureBaseline : state.Stage };
    }

    public static InteractiveWalkthroughState ObserveAcceptedRead(InteractiveWalkthroughState state, Guid seasonId,
        TrackerSession session, ScreenReadResult read, bool isRealWindowCapture, bool snapshot)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(read);
        if (!CanAdvance(state) || !isRealWindowCapture || !read.IsAccepted) return state;
        if (state.SessionId is null && Skipped(state, InteractiveWalkthroughStep.StartNewSession)) state = BindSession(state, seasonId, session);
        if (!MatchesSession(state, seasonId, session)) return state;

        if (state.Stage == InteractiveWalkthroughStep.CaptureBaseline && snapshot && read.Screen == ScreenKind.Unlock
            && session.State == TrackerSessionState.Active)
        {
            foreach (var reading in read.Readings)
            {
                if (reading.Screen != ScreenKind.Unlock || !session.Tracks.TryGetValue(reading.Class, out var track)
                    || !track.Enabled || track.WasRebased || track.Baseline is not { } baseline
                    || track.OriginalBaseline is not { } original || baseline.Id == Guid.Empty || baseline.Version < 1
                    || !SameBaseline(baseline, original) || baseline.Origin != ReadingSource.SnapshotHotkey
                    || baseline.Validity == BaselineValidity.Invalid || baseline.Coverage == BaselineCoverage.Partial
                    || baseline.At < state.StartedAt || baseline.At < session.StartedAt
                    || baseline.At != reading.CapturedAt || baseline.Xp != reading.CumulativeXp
                    || track.Current is not { Source: ReadingSource.SnapshotHotkey } current || !SameReading(current, reading)) continue;
                return state with { Stage = InteractiveWalkthroughStep.ConfirmStartingCoverage, Class = reading.Class,
                    BaselineId = baseline.Id, BaselineVersion = baseline.Version, BaselineCapturedAt = reading.CapturedAt };
            }
            return state;
        }

        if (state.Class is null && Skipped(state, InteractiveWalkthroughStep.CaptureBaseline) && !snapshot
            && state.Stage is InteractiveWalkthroughStep.CheckCurrentXp or InteractiveWalkthroughStep.CaptureFinalXp)
        {
            var actual = read.Readings.FirstOrDefault(reading => session.Tracks.TryGetValue(reading.Class, out var track)
                && track.Enabled && !track.WasRebased);
            if (actual is not null) state = state with { Class = actual.Class };
        }
        if (!MatchesBaseline(state, session) || state.Class is not { } targetClass || snapshot) return state;
        var targetReading = read.Readings.FirstOrDefault(reading => reading.Class == targetClass);
        if (targetReading is null) return state;
        if (state.Stage == InteractiveWalkthroughStep.CheckCurrentXp && read.Screen == ScreenKind.Unlock
            && targetReading.Screen == ScreenKind.Unlock && session.State == TrackerSessionState.Active
            && targetReading.CapturedAt > BaselineFloor(state, session) && session.Tracks[targetClass].Current is { } currentReading
            && IsOcr(currentReading.Source) && SameReading(currentReading, targetReading))
            return state with { Stage = InteractiveWalkthroughStep.CaptureFinalXp, CurrentCapturedAt = targetReading.CapturedAt };

        if (state.Stage == InteractiveWalkthroughStep.CaptureFinalXp && read.Screen == ScreenKind.EndProgression
            && targetReading.Screen == ScreenKind.EndProgression && targetReading.CapturedAt >= CurrentFloor(state, session)
            && session.PendingFinal is { } pending && pending.SessionId == state.SessionId
            && pending.Tracks.TryGetValue(targetClass, out var finalReading) && IsOcr(finalReading.Source)
            && finalReading.At >= CurrentFloor(state, session) && finalReading.At <= targetReading.CapturedAt
            && SameFinalValues(finalReading, targetReading))
            return state with { Stage = InteractiveWalkthroughStep.ConfirmFinalXp, FinalCapturedAt = finalReading.At };
        return state;
    }

    /// <summary>Replays retained real observations only when the player explicitly resumes this bound guide.</summary>
    public static InteractiveWalkthroughState ReconcileRetainedCaptures(InteractiveWalkthroughState state, Guid seasonId,
        TrackerSession session, bool isRealData)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!CanAdvance(state) || !isRealData || !MatchesSession(state, seasonId, session)) return state;
        if (state.Stage == InteractiveWalkthroughStep.CaptureBaseline)
        {
            var track = session.Tracks.Values.Where(track => track.Enabled && !track.WasRebased
                && track.Baseline is { Origin: ReadingSource.SnapshotHotkey } baseline
                && track.OriginalBaseline is { Origin: ReadingSource.SnapshotHotkey } original
                && baseline.Id != Guid.Empty && baseline.Version > 0 && SameBaseline(baseline, original)
                && baseline.At >= state.StartedAt && baseline.At >= session.StartedAt
                && baseline.Validity != BaselineValidity.Invalid && original.Validity != BaselineValidity.Invalid
                && baseline.Coverage != BaselineCoverage.Partial && original.Coverage != BaselineCoverage.Partial
                && HasSnapshotHistory(session, track.Class, baseline))
                .OrderBy(track => track.Baseline!.At).ThenBy(track => track.Class).FirstOrDefault();
            if (track?.Baseline is { } captured)
                state = state with { Stage = InteractiveWalkthroughStep.ConfirmStartingCoverage, Class = track.Class,
                    BaselineId = captured.Id, BaselineVersion = captured.Version, BaselineCapturedAt = captured.At };
        }
        if (state.Class is null && Skipped(state, InteractiveWalkthroughStep.CaptureBaseline)
            && state.Stage >= InteractiveWalkthroughStep.CheckCurrentXp)
        {
            var retainedClass = session.Tracks.Values.Where(track => track.Enabled && !track.WasRebased)
                .Select(track => track.Current).Where(observation => observation is not null && IsOcr(observation.Source)
                    && observation.At >= session.StartedAt).OrderBy(observation => observation!.At).FirstOrDefault();
            retainedClass ??= session.FinalObservations.SelectMany(final => final.Tracks.Values)
                .Where(observation => IsOcr(observation.Source) && observation.At >= session.StartedAt
                    && session.Tracks.TryGetValue(observation.Class, out var track) && track.Enabled && !track.WasRebased)
                .OrderBy(observation => observation.At).FirstOrDefault();
            if (retainedClass is not null) state = state with { Class = retainedClass.Class };
        }
        if (!MatchesBaseline(state, session) || state.Class is not { } targetClass) return state;

        // These flags already came from an explicit Core confirmation. This check does not write them.
        state = CheckCoverage(state, seasonId, session);
        if (state.Stage == InteractiveWalkthroughStep.CheckCurrentXp)
        {
            var retainedCurrent = session.History.Where(item => item.Kind == "CurrentXp" && item.Class == targetClass
                    && item.Source is { } source && IsOcr(source) && item.Screen == ScreenKind.Unlock)
                .Select(item => item.Observation).Append(session.Tracks[targetClass].Current)
                .Where(observation => observation is { Screen: ScreenKind.Unlock } && observation.Class == targetClass
                    && IsOcr(observation.Source) && observation.At > BaselineFloor(state, session)
                    && observation.Xp >= (session.Tracks[targetClass].OriginalBaseline?.Xp ?? 0))
                .OrderBy(observation => observation!.At).FirstOrDefault();
            if (retainedCurrent is not null)
                state = state with { Stage = InteractiveWalkthroughStep.CaptureFinalXp, CurrentCapturedAt = retainedCurrent.At };
        }
        if (state.Stage == InteractiveWalkthroughStep.CaptureFinalXp)
        {
            var retainedFinal = session.FinalObservations.Append(session.PendingFinal).Append(session.ConfirmedFinal)
                .Where(final => final?.SessionId == state.SessionId)
                .Select(final => final!.Tracks.GetValueOrDefault(targetClass))
                .Where(observation => observation is { Screen: ScreenKind.EndProgression } && observation.Class == targetClass
                    && IsOcr(observation.Source) && observation.At >= CurrentFloor(state, session))
                .OrderBy(observation => observation!.At).FirstOrDefault();
            if (retainedFinal is not null)
                state = state with { Stage = InteractiveWalkthroughStep.ConfirmFinalXp, FinalCapturedAt = retainedFinal.At };
        }
        return ObserveFinalConfirmed(state, seasonId, session);
    }

    public static InteractiveWalkthroughState ObserveCoverageConfirmed(InteractiveWalkthroughState state, Guid seasonId,
        TrackerSession session, XpClass trackClass)
        => Enum.IsDefined(trackClass) ? CheckCoverage(state, seasonId, session) : state;

    /// <summary>Checks retained explicit confirmations after a confirmation or Track action. It does not certify missing coverage.</summary>
    public static InteractiveWalkthroughState CheckCoverage(InteractiveWalkthroughState state, Guid seasonId, TrackerSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!At(state, InteractiveWalkthroughStep.ConfirmStartingCoverage) || !MatchesSession(state, seasonId, session)) return state;
        if (state.Class is null && Skipped(state, InteractiveWalkthroughStep.CaptureBaseline))
        {
            var track = session.Tracks.Values.Where(track => track.Enabled && !track.WasRebased
                && track.Baseline is { Validity: not BaselineValidity.Invalid, Coverage: not BaselineCoverage.Partial } baseline
                && track.OriginalBaseline is { Validity: not BaselineValidity.Invalid, Coverage: not BaselineCoverage.Partial } original
                && SameBaseline(baseline, original)).OrderBy(track => track.Baseline!.At).FirstOrDefault();
            if (track?.Baseline is { } captured)
            {
                state = state with { Class = track.Class };
                if (captured.Origin == ReadingSource.SnapshotHotkey && captured.At >= state.StartedAt
                    && HasSnapshotHistory(session, track.Class, captured))
                    state = state with { BaselineId = captured.Id, BaselineVersion = captured.Version, BaselineCapturedAt = captured.At };
            }
        }
        if (!MatchesBaseline(state, session) || !session.Tracks.Values.Where(track => track.Enabled).All(track =>
                track.OriginalBaseline is { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp })) return state;
        return state with { Stage = InteractiveWalkthroughStep.CheckCurrentXp };
    }

    public static InteractiveWalkthroughState ObserveFinalConfirmed(InteractiveWalkthroughState state, Guid seasonId, TrackerSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!At(state, InteractiveWalkthroughStep.ConfirmFinalXp)) return state;
        if (state.SessionId is null && Skipped(state, InteractiveWalkthroughStep.StartNewSession)) state = BindSession(state, seasonId, session);
        if (!MatchesSession(state, seasonId, session)
            || session.ConfirmedFinal is not { ConfirmedAt: { } confirmedAt, HasConflict: false } final
            || final.SessionId != state.SessionId) return state;
        if (state.Class is null && Skipped(state, InteractiveWalkthroughStep.CaptureBaseline))
        {
            var actual = final.Tracks.Keys.Where(trackClass => session.Tracks.TryGetValue(trackClass, out var track) && track.Enabled)
                .Select(trackClass => (XpClass?)trackClass).FirstOrDefault();
            if (actual is { } selectedClass) state = state with { Class = selectedClass };
        }
        if (!MatchesBaseline(state, session) || state.Class is not { } targetClass
            || confirmedAt < (state.FinalCapturedAt ?? CurrentFloor(state, session))
            || !final.Tracks.ContainsKey(targetClass) || final.ConflictingTracks.Contains(targetClass)) return state;
        return state with { Stage = InteractiveWalkthroughStep.OpenAnalytics };
    }

    public static InteractiveWalkthroughState ObserveAnalyticsOpened(InteractiveWalkthroughState state, Guid seasonId,
        Guid? sessionId, bool isRealCurrentAnalytics)
        => At(state, InteractiveWalkthroughStep.OpenAnalytics) && isRealCurrentAnalytics
            && (state.SeasonId == seasonId || state.SeasonId is null && Skipped(state, InteractiveWalkthroughStep.StartNewSession))
            && (state.SessionId == sessionId || state.SessionId is null && Skipped(state, InteractiveWalkthroughStep.StartNewSession))
                ? state with { Stage = InteractiveWalkthroughStep.Completed } : state;

    public static bool IsSupported(InteractiveWalkthroughState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.SchemaVersion != 1 || state.RunId == Guid.Empty || !Enum.IsDefined(state.Stage)
            || state.SkippedSteps is null || state.SkippedSteps.Any(step => !Enum.IsDefined(step) || step >= state.Stage)
            || state.SkippedSteps.Distinct().Count() != state.SkippedSteps.Count) return false;
        var hasSession = state.SeasonId is not null || state.SessionId is not null || state.SessionStartedAt is not null;
        var fullSession = state.SeasonId is { } season && season != Guid.Empty && state.SessionId is { } id && id != Guid.Empty
            && state.SessionStartedAt is { } started && started >= state.StartedAt;
        if (hasSession && !fullSession || state.Stage >= InteractiveWalkthroughStep.CaptureBaseline
            && !fullSession && !Skipped(state, InteractiveWalkthroughStep.StartNewSession)) return false;
        if (state.Class is { } trackClass && (!Enum.IsDefined(trackClass) || !fullSession)) return false;
        var hasBaseline = state.BaselineId is not null || state.BaselineVersion is not null || state.BaselineCapturedAt is not null;
        var fullBaseline = state.Class is not null && state.BaselineId is { } baselineId && baselineId != Guid.Empty
            && state.BaselineVersion is > 0 && state.BaselineCapturedAt is { } baselineAt && fullSession && baselineAt >= state.SessionStartedAt;
        if (hasBaseline && !fullBaseline || state.Stage >= InteractiveWalkthroughStep.ConfirmStartingCoverage
            && !fullBaseline && !Skipped(state, InteractiveWalkthroughStep.CaptureBaseline)) return false;
        if (state.CurrentCapturedAt is { } current && (!fullSession || state.Class is null
            || current <= (state.BaselineCapturedAt ?? state.SessionStartedAt))) return false;
        if (state.Stage >= InteractiveWalkthroughStep.CaptureFinalXp && state.CurrentCapturedAt is null
            && !Skipped(state, InteractiveWalkthroughStep.CheckCurrentXp)) return false;
        if (state.FinalCapturedAt is { } final && (!fullSession || state.Class is null
            || final < (state.CurrentCapturedAt ?? state.BaselineCapturedAt ?? state.SessionStartedAt))) return false;
        return state.Stage < InteractiveWalkthroughStep.ConfirmFinalXp || state.FinalCapturedAt is not null
            || Skipped(state, InteractiveWalkthroughStep.CaptureFinalXp);
    }

    private static bool CanAdvance(InteractiveWalkthroughState state) => IsSupported(state) && !state.IsPaused;
    private static bool At(InteractiveWalkthroughState state, InteractiveWalkthroughStep step) => CanAdvance(state) && state.Stage == step;
    private static bool MatchesSession(InteractiveWalkthroughState state, Guid seasonId, TrackerSession session)
        => state.SeasonId == seasonId && state.SessionId == session.Id && state.SessionStartedAt == session.StartedAt;
    private static bool MatchesBaseline(InteractiveWalkthroughState state, TrackerSession session)
        => state.Class is { } targetClass && session.Tracks.TryGetValue(targetClass, out var track) && track.Enabled && !track.WasRebased
            && (Skipped(state, InteractiveWalkthroughStep.CaptureBaseline) && state.BaselineId is null
                || track.Baseline is { } baseline && track.OriginalBaseline is { } original && SameBaseline(baseline, original)
            && original.Id == state.BaselineId && original.Version == state.BaselineVersion && original.At == state.BaselineCapturedAt
            && original.Validity != BaselineValidity.Invalid && original.Coverage != BaselineCoverage.Partial);
    private static bool Skipped(InteractiveWalkthroughState state, InteractiveWalkthroughStep step) => state.SkippedSteps.Contains(step);
    private static DateTimeOffset BaselineFloor(InteractiveWalkthroughState state, TrackerSession session)
        => state.BaselineCapturedAt ?? session.StartedAt;
    private static DateTimeOffset CurrentFloor(InteractiveWalkthroughState state, TrackerSession session)
        => state.CurrentCapturedAt ?? BaselineFloor(state, session);
    private static InteractiveWalkthroughState BindSession(InteractiveWalkthroughState state, Guid seasonId, TrackerSession session)
        => state.SessionId is null && seasonId != Guid.Empty && session.Id != Guid.Empty && session.StartedAt >= state.StartedAt
            ? state with { SeasonId = seasonId, SessionId = session.Id, SessionStartedAt = session.StartedAt } : state;
    private static bool SameBaseline(BaselineRecord first, BaselineRecord second)
        => first.Id == second.Id && first.Version == second.Version && first.Xp == second.Xp && first.At == second.At
            && first.Origin == second.Origin;
    private static bool SameReading(XpObservation observation, XpReading reading)
        => observation.Class == reading.Class && observation.Xp == reading.CumulativeXp && observation.At == reading.CapturedAt
            && observation.Screen == reading.Screen;
    private static bool SameFinalValues(XpObservation observation, XpReading reading)
        => observation.Class == reading.Class && observation.Xp == reading.CumulativeXp && observation.Screen == reading.Screen;
    private static bool HasSnapshotHistory(TrackerSession session, XpClass trackClass, BaselineRecord baseline)
        => session.History.Any(item => item.Kind == "BaselineEstablished" && item.Class == trackClass
            && item.BaselineId == baseline.Id && item.BaselineVersion == baseline.Version
            && item.Source == ReadingSource.SnapshotHotkey && item.Screen == ScreenKind.Unlock
            && item.Observation is { Source: ReadingSource.SnapshotHotkey, Screen: ScreenKind.Unlock } observation
            && observation.Class == trackClass && observation.Xp == baseline.Xp && observation.At == baseline.At);
    private static bool IsOcr(ReadingSource source) => source is ReadingSource.CheckHotkey or ReadingSource.AutomaticOcr;
}
