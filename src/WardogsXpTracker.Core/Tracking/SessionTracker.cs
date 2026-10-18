using WardogsXpTracker.Core.Reading;

namespace WardogsXpTracker.Core.Tracking;

/// <summary>Applies explicit tracking actions. It has no screen capture, file I/O, or UI dependencies.</summary>
public sealed partial class SessionTracker
{
    private readonly Func<DateTimeOffset> clock;
    public SeasonDocument Document { get; }
    public RuleSetSnapshot SelectedRules { get; set; } = new();
    public TrackerSession? ActiveSession => Document.Sessions.FirstOrDefault(s => s.Id == Document.ActiveSessionId);
    public FinalResultSet? LastKnownConfirmedFinal => Document.Sessions.Select((s, index) => (Session: s, Index: index))
        .Where(x => x.Session.ConfirmedFinal is not null).OrderBy(x => x.Session.StartedAt).ThenBy(x => x.Index)
        .LastOrDefault().Session?.ConfirmedFinal;

    public SessionTracker(SeasonDocument? document = null, Func<DateTimeOffset>? clock = null)
    {
        Document = document ?? new();
        this.clock = clock ?? (() => DateTimeOffset.UtcNow);
        TrackingDocumentValidator.Validate(Document);
        foreach (var session in Document.Sessions)
        {
            // An interrupted capture request never commits a replacement baseline.
            if (session.ArmedRebase is not null)
            {
                session.ArmedRebase = null;
                session.RebaseInvalidatesOriginal = false;
                AddEvent(session, "RebaseCanceledOnResume", "The interrupted rebase kept its previous baseline.");
            }
        }
    }

    public TrackerActionResult StartSession(bool reuseConfirmedFinal = false, bool confirmStartingTotals = false,
        RuleSetSnapshot? rules = null)
    {
        var selected = rules ?? SelectedRules;
        TrackingDocumentValidator.ValidateRules(selected);
        var previousFinal = LastKnownConfirmedFinal;
        if (ActiveSession is { } previous && previous.State == TrackerSessionState.Active)
        {
            previous.State = TrackerSessionState.Ended;
            previous.EndedAt = clock();
            previous.ArmedRebase = null;
            AddEvent(previous, "SessionReplaced", "A new session ended this counter. The outcome remains unknown.");
        }
        var session = new TrackerSession { StartedAt = clock(), RulesSnapshot = selected.Copy() };
        foreach (var track in session.Tracks.Values)
            track.Enabled = Document.EnabledTracks.GetValueOrDefault(track.Class, true);
        Document.Sessions.Add(session);
        Document.ActiveSessionId = session.Id;
        AddEvent(session, "SessionStarted", "New session. Capture each enabled class with F3 or enter its baseline manually.");
        if (reuseConfirmedFinal && previousFinal is not null)
        {
            foreach (var (trackClass, final) in previousFinal.Tracks)
            {
                if (!session.Tracks[trackClass].Enabled) continue;
                var observation = CopyObservation(final, ReadingSource.ReusedFinal, clock());
                EstablishBaseline(session, session.Tracks[trackClass], observation, false, previousFinal.SessionId);
                if (confirmStartingTotals) ConfirmTrackCoverage(session, session.Tracks[trackClass]);
            }
            AddEvent(session, "FinalTotalsReused", "The user explicitly reused the last known confirmed final totals.");
        }
        return Ok("New session started. Missing classes can be captured independently.");
    }

    public TrackerActionResult SetTrackEnabled(XpClass trackClass, bool enabled)
    {
        if (!Enum.IsDefined(trackClass)) return Fail("Choose a valid class.");
        var changed = Document.EnabledTracks.GetValueOrDefault(trackClass, true) != enabled;
        Document.EnabledTracks[trackClass] = enabled;
        if (ActiveSession is { } session)
        {
            var track = session.Tracks[trackClass];
            changed |= track.Enabled != enabled;
            track.Enabled = enabled;
            if (!enabled && session.ArmedRebase == trackClass)
            {
                session.ArmedRebase = null;
                session.RebaseInvalidatesOriginal = false;
            }
            if (changed) AddEvent(session, enabled ? "TrackEnabled" : "TrackDisabled", $"{trackClass} tracking {(enabled ? "enabled" : "disabled")}.", trackClass);
        }
        return new(true, changed, $"{trackClass} tracking {(enabled ? "enabled" : "disabled")}.");
    }

    public TrackerActionResult ArmRebase(XpClass trackClass, bool originalBaselineWasWrong = false)
    {
        var session = ActiveSession;
        if (session is null || session.State != TrackerSessionState.Active) return Fail("Start or resume an active session before rebasing.");
        if (!session.Tracks.TryGetValue(trackClass, out var track) || !track.Enabled) return Fail("Enable this class before rebasing.");
        session.ArmedRebase = trackClass;
        session.RebaseInvalidatesOriginal = originalBaselineWasWrong;
        AddEvent(session, "RebaseArmed", "The previous baseline stays active until a valid replacement is entered.", trackClass);
        return Ok($"Record {trackClass} XP now with the snapshot key. Cancel keeps the previous baseline.");
    }

    public TrackerActionResult CancelRebase()
    {
        if (ActiveSession is not { ArmedRebase: { } target } session) return new(true, false, "No rebase is pending.");
        session.ArmedRebase = null;
        session.RebaseInvalidatesOriginal = false;
        AddEvent(session, "RebaseCanceled", "The previous baseline was preserved.", target);
        return Ok("Rebase canceled. The previous baseline is unchanged.");
    }

    public TrackerActionResult Observe(ScreenReadResult result, ReadingSource source, bool asSnapshot = false,
        Guid? expectedSessionId = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        var session = ActiveSession;
        if (session is null) return Fail("Select New Session before tracking XP.");
        if (expectedSessionId is { } expected && session.Id != expected)
            return Fail("This read belongs to a different session. Capture again.");
        if (result.IsEndScreen) return ObserveEnding(session, result, source);
        if (result.Screen == ScreenKind.Unlock && session.State != TrackerSessionState.Active)
        {
            var changed = !session.PostEndUnlockObserved;
            session.PostEndUnlockObserved = true;
            if (changed) AddEvent(session, "UnlockAfterEnd", "UNLOCK appeared after the session froze. Start a new session for another match.");
            return new(false, changed, "This session is frozen. Select New Session for the next match.", session.PendingFinal is not null);
        }
        // A locked snapshot must remain a no-op, including scene markers and latest-reading metadata.
        if (asSnapshot && session.ArmedRebase is null &&
            (result.Readings.Count == 1 && session.Tracks.TryGetValue(result.Readings[0].Class, out var snapshotTrack)
                && snapshotTrack.Baseline is not null
            || !session.Tracks.Values.Any(t => t.Enabled && t.Baseline is null)))
            return new(false, false, "This baseline is locked. Use Re-Establish Baseline to replace it.");
        var markerChanged = result.Screen == ScreenKind.Unlock && !session.HasUnlockMarker;
        if (markerChanged)
        {
            session.HasUnlockMarker = true;
            AddEvent(session, "UnlockObserved", "An UNLOCK scene was observed in this session.", source: source, screen: result.Screen);
        }
        if (!result.IsAccepted || result.Screen != ScreenKind.Unlock)
        {
            var changed = AddEvent(session, "ReadRejected", result.Message, source: source, screen: result.Screen);
            return new(false, changed || markerChanged, result.Message);
        }
        if (result.Readings.Count != 1)
            return Reject(session, "A live read must identify exactly one class.", source);
        var reading = result.Readings[0];
        if (!session.Tracks.TryGetValue(reading.Class, out var track) || reading.Screen != ScreenKind.Unlock)
            return Reject(session, "This reading is not a supported live class total.", source);
        if (asSnapshot && session.ArmedRebase is { } target && target != reading.Class)
            return Reject(session, $"Open {target} before replacing its baseline.", source, reading.Class);
        var observation = FromReading(reading, source);
        if (!IsValidObservation(observation)) return Reject(session, "The XP total or threshold is invalid.", source, reading.Class);
        var isRebase = asSnapshot && session.ArmedRebase == reading.Class;
        if (asSnapshot && track.Baseline is not null && !isRebase)
            return new(false, false, "This baseline is locked. Use Re-Establish Baseline to replace it.");
        if (!isRebase && IsDecreasing(track, observation.Xp))
            return Reject(session, "XP decreased. Review the reading or explicitly re-establish this baseline.", source, reading.Class, observation);
        var latestChanged = !SameObservation(track.LatestReading, observation);
        track.LatestReading = observation;
        if (!track.Enabled)
        {
            if (latestChanged) AddEvent(session, "DisabledClassObserved", "Observed XP for a disabled class. Its live counter did not change.",
                reading.Class, observation.Xp, source, reading.Screen, observation);
            return new(true, latestChanged || markerChanged, $"{reading.Class} is disabled. Its live counter did not change.");
        }
        if (asSnapshot)
        {
            EstablishBaseline(session, track, observation, isRebase);
            return Ok(isRebase ? "Baseline replaced. XP rebased mid match. May not be accurate" : $"{reading.Class} baseline ready. Tracking started.");
        }
        var changedReading = !SameObservation(track.Current, observation);
        track.Current = observation;
        if (changedReading) AddEvent(session, "CurrentXp", "Accepted current XP.", reading.Class, observation.Xp, source, reading.Screen, observation);
        var reached = ReachTarget(session, track, observation);
        return new(true, changedReading || markerChanged || reached,
            track.Baseline is null ? $"{reading.Class}: baseline not established. Push F3 or enter a baseline."
                : reached ? $"{reading.Class}: maximum XP tier reached." : $"{reading.Class}: {track.MeasuredGain:N0} XP gained.");
    }

    public TrackerActionResult ManualBaseline(XpClass trackClass, long xp)
    {
        var session = ActiveSession;
        if (session is null || session.State != TrackerSessionState.Active) return Fail("Select New Session before entering a baseline.");
        if (xp < 0 || !session.Tracks.TryGetValue(trackClass, out var track) || !track.Enabled) return Fail("Enter nonnegative XP for an enabled class.");
        if (session.ArmedRebase is { } target && target != trackClass) return Fail($"The pending rebase is for {target}.");
        var rebase = session.ArmedRebase == trackClass;
        if (track.Baseline is not null && !rebase) return Fail("Use Re-Establish Baseline before replacing this value.");
        if (!rebase && IsDecreasing(track, xp)) return Fail("XP decreased. Review the value before establishing this baseline.");
        EstablishBaseline(session, track, ManualObservation(trackClass, xp), rebase);
        return Ok(rebase ? "Baseline replaced. XP rebased mid match. May not be accurate" : "Baseline ready. Tracking started.");
    }

    public TrackerActionResult ManualCurrent(XpClass trackClass, long xp)
    {
        var session = ActiveSession;
        if (session is null || session.State != TrackerSessionState.Active) return Fail("Current XP checks require an active session.");
        if (xp < 0 || !session.Tracks.TryGetValue(trackClass, out var track) || !track.Enabled) return Fail("Enter nonnegative XP for an enabled class.");
        var observation = ManualObservation(trackClass, xp);
        if (IsDecreasing(track, xp)) return Reject(session, "XP decreased. Review the value or re-establish the baseline.", ReadingSource.Manual, trackClass, observation);
        var changed = !SameObservation(track.Current, observation);
        track.Current = track.LatestReading = observation;
        if (changed) AddEvent(session, "CurrentXp", "Manual current XP.", trackClass, xp, ReadingSource.Manual, ScreenKind.Unlock, observation);
        var reached = ReachTarget(session, track, observation);
        return new(true, changed || reached, reached ? "Maximum XP tier reached." : "Current XP recorded.");
    }

    public TrackerActionResult ConfirmCoverage()
    {
        if (ActiveSession is not { } session) return Fail("Start a session before confirming baseline coverage.");
        return ConfirmCoverageForSession(session.Id);
    }

    /// <summary>Lists eligible original baselines that still need the user's timing confirmation.</summary>
    public IReadOnlyList<XpClass> GetConfirmableCoverageClasses(Guid sessionId)
    {
        var session = Document.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return [];
        return Array.AsReadOnly(session.Tracks.Values.Where(track => CanConfirmCoverage(track) && !IsCoverageConfirmed(track))
            .OrderBy(track => track.Class).Select(track => track.Class).ToArray());
    }

    /// <summary>Confirms eligible originals in the named session without selecting it as the active session.</summary>
    public TrackerActionResult ConfirmCoverageForSession(Guid sessionId)
    {
        var session = Document.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return Fail("The selected session no longer exists.");
        var changed = false;
        foreach (var track in session.Tracks.Values.Where(CanConfirmCoverage))
            changed |= ConfirmTrackCoverage(session, track);
        return new(true, changed, changed
            ? "Eligible original starting baselines confirmed for analytics only. Final-result confirmation remains separate."
            : "No eligible unconfirmed starting baselines remain. Final-result confirmation remains separate.");
    }

    public TrackerActionResult ConfirmCoverage(XpClass trackClass)
    {
        if (ActiveSession is not { } session) return Fail("Start a session before confirming baseline coverage.");
        return ConfirmCoverageForSession(session.Id, trackClass);
    }

    /// <summary>Records the user's assertion that this saved original baseline preceded earned XP.</summary>
    public TrackerActionResult ConfirmCoverageForSession(Guid sessionId, XpClass trackClass)
    {
        var session = Document.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return Fail("The selected session no longer exists.");
        if (!Enum.IsDefined(trackClass) || !session.Tracks.TryGetValue(trackClass, out var track))
            return Fail("Choose a valid class.");
        if (!CanConfirmCoverage(track))
            return Fail("Confirm an enabled class's original baseline. Missing, invalid, mismatched, or rebased baselines cannot be certified.");
        var changed = ConfirmTrackCoverage(session, track);
        return new(true, changed, changed
            ? $"{trackClass} starting-baseline timing confirmed. Final-result coverage remains separate."
            : $"{trackClass} starting-baseline timing was already confirmed. Final-result coverage remains separate.");
    }

    public TrackerActionResult ConfirmFinal(int? place = null)
    {
        if (ActiveSession is not { } session) return Fail("There are no pending final results.");
        return ConfirmFinalForSession(session.Id, place);
    }

    public TrackerActionResult ConfirmFinalForSession(Guid sessionId, int? place = null)
    {
        var session = Document.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session?.PendingFinal is not { } pending) return Fail("There are no pending final results.");
        if (place is < 1 or > 3) return Fail("Placement must be first, second, third, or unknown.");
        if (pending.Tracks.Count == 0) return Fail("No final XP totals were observed. Open the match-end PROGRESSION tab.");
        if (pending.ConflictingTracks.Count > 0) return Fail("Final XP totals conflict. Correct those class totals before confirmation.");
        if (pending.HasConflict && place is null) return Fail("Placement evidence conflicts. Enter the final placement before confirmation.");
        PreserveLegacyObservedFinal(session, pending);
        if (place is { } exact)
        {
            pending.PossiblePlaces = [exact];
            pending.HasConflict = false;
            pending.PlacementEvidence.Add(new() { At = clock(), Source = ReadingSource.Manual, PossiblePlaces = [exact], RawText = "User-confirmed placement" });
        }
        pending.ConfirmedAt = clock();
        session.ConfirmedFinal = CopyFinal(pending);
        session.PendingFinal = null;
        session.State = TrackerSessionState.Ended;
        session.Outcome = SessionOutcome.Finished;
        session.EndedAt ??= clock();
        AddEvent(session, "FinalConfirmed", "Observed final totals confirmed. Live earned-XP checks were not advanced.");
        return Ok("Final totals saved. New Session can offer these confirmed totals as baselines.");
    }

    public TrackerActionResult KeepPendingFinal()
    {
        if (ActiveSession is not { } session) return Fail("There are no pending final results.");
        return KeepPendingFinalForSession(session.Id);
    }

    public TrackerActionResult KeepPendingFinalForSession(Guid sessionId)
    {
        var session = Document.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session?.PendingFinal is not { } pending) return Fail("There are no pending final results.");
        if (pending.KeepOldReferencesChosen) return new(true, false, "Both values remain saved for later review.");
        pending.KeepOldReferencesChosen = true;
        AddEvent(session, "FinalDecisionDeferred", "Old references retained. Candidate final totals remain available.");
        return Ok("Both values remain saved. Refresh or explicitly reuse baselines for the next match.");
    }

    public TrackerActionResult End(SessionOutcome outcome)
    {
        if (ActiveSession is not { } session) return Fail("No session is active.");
        if (!Enum.IsDefined(outcome)) return Fail("Choose a supported outcome.");
        session.State = TrackerSessionState.Ended;
        session.Outcome = outcome;
        session.EndedAt ??= clock();
        session.ArmedRebase = null;
        session.RebaseInvalidatesOriginal = false;
        AddEvent(session, "SessionEnded", outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected
            ? "User reported no bonus received. Closing XP can be recorded separately." : $"Session ended: {outcome}.");
        return Ok("Session ended. Existing target checks remain separate from bonus receipt.");
    }

    public TrackerActionResult RecordClosingXp(XpClass trackClass, long xp)
    {
        var session = ActiveSession;
        if (session is null || session.State != TrackerSessionState.Ended
            || session.Outcome is not (SessionOutcome.LeftMatch or SessionOutcome.Disconnected))
            return Fail("Closing XP belongs to an ended match with a reported no-bonus outcome.");
        if (xp < 0 || !session.Tracks.TryGetValue(trackClass, out var track)) return Fail("Enter a nonnegative class total.");
        if (IsDecreasing(track, xp)) return Reject(session, "Closing XP decreased. Review this total.", ReadingSource.Manual, trackClass);
        var observation = ManualObservation(trackClass, xp);
        track.ClosingXp = observation;
        AddEvent(session, "ClosingXp", "Closing XP recorded separately from the live counter and baseline.", trackClass, xp, ReadingSource.Manual, ScreenKind.Unlock, observation);
        return Ok("Closing XP recorded. The starting baseline and live check remain unchanged.");
    }

    public TrackerActionResult ManualFinal(XpClass trackClass, long xp)
    {
        if (ActiveSession is not { } session) return Fail("Enter a nonnegative total while final results are pending.");
        return ManualFinalForSession(session.Id, trackClass, xp);
    }

    public TrackerActionResult ManualFinalForSession(Guid sessionId, XpClass trackClass, long xp)
    {
        var session = Document.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session?.PendingFinal is not { } pending || xp < 0 || !session.Tracks.TryGetValue(trackClass, out var track))
            return Fail("Enter a nonnegative total while final results are pending.");
        if (IsDecreasing(track, xp)) return Fail("Final XP is lower than the last live total. Review it before correction.");
        PreserveLegacyObservedFinal(session, pending);
        var observation = ManualObservation(trackClass, xp);
        observation.Screen = ScreenKind.EndProgression;
        pending.Tracks[trackClass] = observation;
        pending.ConflictingTracks.Remove(trackClass);
        pending.LastObservedAt = clock();
        AddEvent(session, "FinalCorrected", "User corrected candidate final XP.", trackClass, xp, ReadingSource.Manual, ScreenKind.EndProgression, observation);
        return new(true, true, "Final XP corrected. Live checks did not change.", true);
    }

    private TrackerActionResult ObserveEnding(TrackerSession session, ScreenReadResult result, ReadingSource source)
    {
        if (!session.HasUnlockMarker)
        {
            var manualLiveEvidence = session.Tracks.Values.Any(t => t.Baseline?.Origin == ReadingSource.Manual
                || t.Current?.Source == ReadingSource.Manual);
            if (manualLiveEvidence)
            {
                var frozeManualTracking = session.State == TrackerSessionState.Active;
                if (frozeManualTracking)
                {
                    session.State = TrackerSessionState.Frozen;
                    session.EndedAt = clock();
                    session.ArmedRebase = null;
                    session.RebaseInvalidatesOriginal = false;
                    AddEvent(session, "UnlinkedEndDetected", "An end screen froze manual tracking. Final totals remain unlinked without a current-session UNLOCK marker.",
                        source: source, screen: result.Screen);
                }
                return new(false, frozeManualTracking, "End screen detected. Live tracking froze. Final totals could not be linked without a session UNLOCK marker.");
            }
            return Reject(session, "Show a class UNLOCK page in this session before its end results can be linked.", source);
        }
        if (session.PostEndUnlockObserved || session.Outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected)
            return Reject(session, "These end results cannot safely be linked to this session. Start or select the correct session.", source);
        var observedChanged = PreserveObservedFinal(session, result, source);
        if (session.ConfirmedFinal is { } confirmed && session.PendingFinal is null
            && result.Readings.All(r => confirmed.Tracks.TryGetValue(r.Class, out var old) && old.Xp == r.CumulativeXp)
            && (!result.Placement.Observations.Any() || !confirmed.PossiblePlaces.Any()
                || result.Placement.PossiblePlaces.Intersect(confirmed.PossiblePlaces).Any()))
            return new(true, observedChanged, "These final results are already confirmed.");
        var freeze = session.State == TrackerSessionState.Active;
        var changed = freeze || observedChanged;
        if (freeze)
        {
            session.State = TrackerSessionState.Frozen;
            session.EndedAt = clock();
            session.ArmedRebase = null;
            session.RebaseInvalidatesOriginal = false;
            AddEvent(session, "EndDetected", "Live earned-XP checks froze before final totals were collected.");
        }
        var pending = session.PendingFinal ??= new() { SessionId = session.Id, FirstObservedAt = clock(), LastObservedAt = clock() };
        if (result.Placement.Observations.Count > 0)
        {
            foreach (var evidence in result.Placement.Observations)
            {
                if (pending.PlacementEvidence.Any(p => p.Source == source && p.RawText == evidence.RawText
                    && p.PossiblePlaces.SequenceEqual(evidence.PossiblePlaces))) continue;
                var possible = evidence.PossiblePlaces.ToList();
                var hadEvidence = pending.PlacementEvidence.Count > 0;
                pending.PossiblePlaces = hadEvidence ? pending.PossiblePlaces.Intersect(possible).Order().ToList() : possible;
                pending.PlacementEvidence.Add(new() { At = clock(), Source = source, PossiblePlaces = possible, RawText = evidence.RawText });
                pending.HasConflict |= pending.PossiblePlaces.Count == 0;
                changed = true;
                AddEvent(session, "PlacementObserved", evidence.RawText, source: source, screen: result.Screen);
            }
            pending.HasConflict |= result.Placement.HasConflict;
        }
        if (result.IsAccepted && result.Screen == ScreenKind.EndProgression)
        {
            foreach (var reading in result.Readings)
            {
                var latestObserved = session.FinalObservations.LastOrDefault()?.Tracks.GetValueOrDefault(reading.Class);
                var observation = FromReading(reading, source);
                if (SameObservation(latestObserved, observation)) observation = CopyFinalObservation(latestObserved!);
                if (!session.Tracks.TryGetValue(reading.Class, out var track) || !IsValidObservation(observation)) continue;
                pending.Tracks.TryGetValue(reading.Class, out var previous);
                if (IsDecreasing(track, observation.Xp) || previous is not null && observation.Xp < previous.Xp)
                {
                    if (!pending.ConflictingTracks.Contains(reading.Class)) pending.ConflictingTracks.Add(reading.Class);
                    changed |= AddEvent(session, "FinalXpConflict", "Candidate final XP decreased. The previous value stays available.",
                        reading.Class, observation.Xp, source, result.Screen, observation);
                    continue;
                }
                if (SameObservation(previous, observation)) continue;
                pending.Tracks[reading.Class] = observation;
                pending.LastObservedAt = clock();
                changed = true;
                AddEvent(session, "FinalCandidate", "Observed candidate final total. Bonuses can be included.", reading.Class, observation.Xp, source, result.Screen, observation);
            }
        }
        else if (!result.IsAccepted)
            changed |= AddEvent(session, "FinalReadRejected", result.Message, source: source, screen: result.Screen);
        return new(true, changed, "End of Match Detected. Record Final XP results?", true);
    }

    private void EstablishBaseline(TrackerSession session, TrackState track, XpObservation observation, bool rebase, Guid? relatedSession = null)
    {
        var previous = track.Baseline;
        var baseline = new BaselineRecord
        {
            Version = (previous?.Version ?? 0) + 1, Xp = observation.Xp, At = observation.At, Origin = observation.Source,
            Coverage = rebase ? BaselineCoverage.Partial : BaselineCoverage.Unknown, RelatedSessionId = relatedSession
        };
        if (rebase && session.RebaseInvalidatesOriginal && track.OriginalBaseline is { } original)
        {
            original.Validity = BaselineValidity.Invalid;
            original.ValidityReason = "The user reported that the original baseline was wrong.";
        }
        track.Baseline = baseline;
        track.OriginalBaseline ??= baseline;
        track.Current = track.LatestReading = observation;
        track.TargetReachedAt = null;
        track.WasRebased |= rebase;
        session.ArmedRebase = null;
        session.RebaseInvalidatesOriginal = false;
        var trackingEvent = NewEvent(rebase ? "BaselineRebased" : "BaselineEstablished", rebase
            ? "XP rebased mid match. May not be accurate" : "Baseline established.", track.Class, observation.Xp,
            observation.Source, observation.Screen, observation);
        trackingEvent.PreviousXp = previous?.Xp;
        trackingEvent.BaselineId = baseline.Id;
        trackingEvent.BaselineVersion = baseline.Version;
        session.History.Add(trackingEvent);
    }

    private bool ConfirmTrackCoverage(TrackerSession session, TrackState track)
    {
        if (!CanConfirmCoverage(track)) return false;
        var baseline = track.Baseline!;
        if (IsCoverageConfirmed(track)) return false;
        baseline.Coverage = BaselineCoverage.BeforeEarnedXp;
        baseline.Validity = BaselineValidity.Valid;
        if (track.OriginalBaseline?.Id == baseline.Id)
        {
            track.OriginalBaseline.Coverage = baseline.Coverage;
            track.OriginalBaseline.Validity = baseline.Validity;
        }
        var trackingEvent = NewEvent("CoverageConfirmed", "User confirmed this baseline preceded earned XP.", track.Class, baseline.Xp,
            source: ReadingSource.Manual);
        trackingEvent.BaselineId = baseline.Id;
        trackingEvent.BaselineVersion = baseline.Version;
        session.History.Add(trackingEvent);
        return true;
    }

    private static bool CanConfirmCoverage(TrackState track) => track.Enabled && !track.WasRebased
        && track.Baseline is { } baseline && track.OriginalBaseline is { } original
        && baseline.Id != Guid.Empty && baseline.Version > 0 && baseline.Xp >= 0 && baseline.At != default
        && baseline.Id == original.Id && baseline.Version == original.Version
        && baseline.Xp == original.Xp && baseline.At == original.At && baseline.Origin == original.Origin
        && baseline.RelatedSessionId == original.RelatedSessionId && Enum.IsDefined(baseline.Origin)
        && Enum.IsDefined(baseline.Coverage) && Enum.IsDefined(original.Coverage)
        && Enum.IsDefined(baseline.Validity) && Enum.IsDefined(original.Validity)
        && baseline.Coverage != BaselineCoverage.Partial && original.Coverage != BaselineCoverage.Partial
        && baseline.Validity != BaselineValidity.Invalid && original.Validity != BaselineValidity.Invalid;

    private static bool IsCoverageConfirmed(TrackState track) =>
        track.Baseline is { Coverage: BaselineCoverage.BeforeEarnedXp, Validity: BaselineValidity.Valid }
        && track.OriginalBaseline is { Coverage: BaselineCoverage.BeforeEarnedXp, Validity: BaselineValidity.Valid };

    private bool ReachTarget(TrackerSession session, TrackState track, XpObservation observation)
    {
        if (track.TargetReached || track.MeasuredGain is not { } gain || gain < session.RulesSnapshot.TargetXp) return false;
        track.TargetReachedAt = observation.At;
        AddEvent(session, "MaximumTierReached", "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            track.Class, observation.Xp, observation.Source, observation.Screen, observation);
        return true;
    }

    private static bool IsDecreasing(TrackState track, long xp) => xp < (track.Baseline?.Xp ?? 0)
        || xp < (track.Current?.Xp ?? 0) || xp < (track.LatestReading?.Xp ?? 0);
    private static bool IsValidObservation(XpObservation observation) => observation.Xp >= 0
        && (observation.NextThreshold is null || observation.NextThreshold > 0 && observation.Xp <= observation.NextThreshold);
    private static bool SameObservation(XpObservation? first, XpObservation second) => first is not null && first.Xp == second.Xp
        && first.NextThreshold == second.NextThreshold && first.CurrentLevel == second.CurrentLevel && first.ThresholdLevel == second.ThresholdLevel;
    private static XpObservation FromReading(XpReading reading, ReadingSource source) => new()
    {
        Class = reading.Class, Xp = reading.CumulativeXp, NextThreshold = reading.NextThreshold, CurrentLevel = reading.CurrentLevel,
        ThresholdLevel = reading.ThresholdLevel, At = reading.CapturedAt, Source = source, Screen = reading.Screen,
        RawText = reading.RawText, Confidence = reading.Confidence
    };
    private XpObservation ManualObservation(XpClass trackClass, long xp) => new()
    { Class = trackClass, Xp = xp, At = clock(), Source = ReadingSource.Manual, Screen = ScreenKind.Unlock, RawText = "Manual entry" };
    private static XpObservation CopyObservation(XpObservation original, ReadingSource source, DateTimeOffset at) => new()
    {
        Class = original.Class, Xp = original.Xp, NextThreshold = original.NextThreshold, CurrentLevel = original.CurrentLevel,
        ThresholdLevel = original.ThresholdLevel, Source = source, At = at, Screen = original.Screen,
        RawText = original.RawText, Confidence = original.Confidence
    };
    private static XpObservation CopyFinalObservation(XpObservation original)
    {
        var copy = CopyObservation(original, original.Source, original.At);
        copy.Id = original.Id;
        return copy;
    }
    private static FinalResultSet CopyFinal(FinalResultSet source) => new()
    {
        SessionId = source.SessionId, FirstObservedAt = source.FirstObservedAt, LastObservedAt = source.LastObservedAt,
        ConfirmedAt = source.ConfirmedAt, Tracks = source.Tracks.ToDictionary(p => p.Key, p => CopyFinalObservation(p.Value)),
        PossiblePlaces = [.. source.PossiblePlaces], PlacementEvidence = source.PlacementEvidence.Select(p => new PlacementRecord
        { At = p.At, Source = p.Source, PossiblePlaces = [.. p.PossiblePlaces], RawText = p.RawText }).ToList(),
        HasConflict = source.HasConflict, ConflictingTracks = [.. source.ConflictingTracks], KeepOldReferencesChosen = source.KeepOldReferencesChosen
    };

    private bool PreserveObservedFinal(TrackerSession session, ScreenReadResult result, ReadingSource source)
    {
        var legacySeeded = session.PendingFinal is { } pending && PreserveLegacyObservedFinal(session, pending);
        var previous = session.FinalObservations.LastOrDefault();
        var observed = previous is null ? new FinalResultSet
        { SessionId = session.Id, FirstObservedAt = clock(), LastObservedAt = clock() } : CopyFinal(previous);
        var changed = false;
        foreach (var evidence in result.Placement.Observations)
        {
            if (observed.PlacementEvidence.Any(item => item.RawText == evidence.RawText
                && item.PossiblePlaces.SequenceEqual(evidence.PossiblePlaces))) continue;
            var hadEvidence = observed.PlacementEvidence.Count > 0;
            var possible = evidence.PossiblePlaces.ToList();
            observed.PossiblePlaces = hadEvidence ? observed.PossiblePlaces.Intersect(possible).Order().ToList() : possible;
            observed.PlacementEvidence.Add(new()
            { At = clock(), Source = source, PossiblePlaces = possible, RawText = evidence.RawText });
            observed.HasConflict |= observed.PossiblePlaces.Count == 0;
            changed = true;
        }
        if (result.Placement.HasConflict && !observed.HasConflict)
        { observed.HasConflict = true; changed = true; }
        if (result.IsAccepted && result.Screen == ScreenKind.EndProgression)
        {
            foreach (var reading in result.Readings)
            {
                if (!session.Tracks.ContainsKey(reading.Class)) continue;
                var observation = FromReading(reading, source);
                if (!IsValidObservation(observation)) continue;
                observed.Tracks.TryGetValue(reading.Class, out var prior);
                if (SameObservation(prior, observation)) continue;
                if (prior is not null && observation.Xp < prior.Xp && !observed.ConflictingTracks.Contains(reading.Class))
                    observed.ConflictingTracks.Add(reading.Class);
                observed.Tracks[reading.Class] = observation;
                changed = true;
            }
        }
        if (!changed) return legacySeeded;
        observed.LastObservedAt = clock();
        session.FinalObservations.Add(observed);
        return true;
    }

    private bool PreserveLegacyObservedFinal(TrackerSession session, FinalResultSet pending)
    {
        if (session.FinalObservations.Count > 0) return false;
        var observed = CopyFinal(pending);
        observed.ConfirmedAt = null;
        observed.KeepOldReferencesChosen = false;
        observed.Tracks = observed.Tracks.Where(pair => pair.Value.Source != ReadingSource.Manual)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        observed.PlacementEvidence = observed.PlacementEvidence.Where(item => item.Source != ReadingSource.Manual).ToList();
        observed.PossiblePlaces = [];
        foreach (var evidence in observed.PlacementEvidence)
            observed.PossiblePlaces = observed.PossiblePlaces.Count == 0 && !observed.HasConflict
                ? [.. evidence.PossiblePlaces] : observed.PossiblePlaces.Intersect(evidence.PossiblePlaces).Order().ToList();
        if (observed.Tracks.Count == 0 && observed.PlacementEvidence.Count == 0) return false;
        session.FinalObservations.Add(observed);
        return true;
    }
    private TrackerActionResult Reject(TrackerSession session, string message, ReadingSource source, XpClass? trackClass = null, XpObservation? observation = null)
        => new(false, AddEvent(session, "ReadRejected", message, trackClass, observation?.Xp, source, observation?.Screen, observation), message);
    private TrackingEvent NewEvent(string kind, string message, XpClass? trackClass = null, long? xp = null,
        ReadingSource? source = null, ScreenKind? screen = null, XpObservation? observation = null)
        => new() { Kind = kind, Message = message, Class = trackClass, Xp = xp, At = clock(), Source = source, Screen = screen, Observation = observation };
    private bool AddEvent(TrackerSession session, string kind, string message, XpClass? trackClass = null, long? xp = null,
        ReadingSource? source = null, ScreenKind? screen = null, XpObservation? observation = null)
    {
        if (session.History.LastOrDefault() is { } last && last.Kind == kind && last.Message == message
            && last.Class == trackClass && last.Xp == xp && last.Source == source && last.Screen == screen) return false;
        session.History.Add(NewEvent(kind, message, trackClass, xp, source, screen, observation));
        return true;
    }
    private static TrackerActionResult Fail(string message) => new(false, false, message);
    private static TrackerActionResult Ok(string message) => new(true, true, message);
}
