using System.Text.Json;
using WardogsXpTracker.App;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

var checks = 0;
var now = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
var season = Guid.NewGuid();
void Check(bool value, string description)
{
    checks++;
    if (!value) throw new InvalidOperationException($"Check {checks} failed: {description}");
}
void Same(InteractiveWalkthroughState first, InteractiveWalkthroughState second, string description) => Check(first == second, description);
TrackerSession Session() => new() { StartedAt = now.AddSeconds(1) };
XpObservation Observation(long xp, ScreenKind screen, ReadingSource source, int second = 2) => new()
{ Class = XpClass.Assault, Xp = xp, At = now.AddSeconds(second), Source = source, Screen = screen };
ScreenReadResult Read(XpObservation observation, ReadStatus status = ReadStatus.Accepted) => new(observation.Screen, status, "test",
    [new(observation.Class, observation.Xp, 64500, 15, 16, observation.Screen, observation.At, default, "test", 1)], PlacementEvidence.None, [], []);
void Baseline(TrackerSession session)
{
    var observation = Observation(56486, ScreenKind.Unlock, ReadingSource.SnapshotHotkey);
    var baseline = new BaselineRecord { At = observation.At, Xp = observation.Xp, Origin = observation.Source };
    session.Tracks[XpClass.Assault].Baseline = baseline;
    session.Tracks[XpClass.Assault].OriginalBaseline = baseline;
    session.Tracks[XpClass.Assault].Current = observation;
}
void Certify(TrackerSession session, XpClass trackClass)
{
    var track = session.Tracks[trackClass];
    track.OriginalBaseline ??= new() { Xp = 100, At = now.AddSeconds(2), Origin = ReadingSource.Manual };
    track.OriginalBaseline.Validity = BaselineValidity.Valid;
    track.OriginalBaseline.Coverage = BaselineCoverage.BeforeEarnedXp;
}
InteractiveWalkthroughState Bind(TrackerSession session)
{
    var state = InteractiveWalkthroughProgress.Start(now);
    state = InteractiveWalkthroughProgress.ObserveWindowSelected(state, true);
    return InteractiveWalkthroughProgress.ObserveNewSession(state, season, session, true);
}
InteractiveWalkthroughState Captured(TrackerSession session)
{
    var state = Bind(session); Baseline(session);
    return InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(session.Tracks[XpClass.Assault].Current!), true, true);
}

var state = InteractiveWalkthroughProgress.Start(now);
Check(state.Step == InteractiveWalkthroughStep.SelectGameWindow && !state.IsPaused, "fresh state waits for window");
Same(state, InteractiveWalkthroughProgress.ObserveWindowSelected(state, false), "no mock or absent window");
var future = state with { SchemaVersion = 2 };
Same(future, InteractiveWalkthroughProgress.ObserveWindowSelected(future, true), "future progress cannot silently advance");
var paused = InteractiveWalkthroughProgress.Pause(state);
Same(paused, InteractiveWalkthroughProgress.ObserveWindowSelected(paused, true), "paused evidence cannot advance");
Check(InteractiveWalkthroughProgress.Resume(paused) == state, "resume preserves run");
state = InteractiveWalkthroughProgress.ObserveWindowSelected(state, true);
var session = Session();
Same(state, InteractiveWalkthroughProgress.ObserveNewSession(state, season, session, false), "mock session rejected");
var oldSession = Session(); oldSession.StartedAt = now.AddSeconds(-1);
Same(state, InteractiveWalkthroughProgress.ObserveNewSession(state, season, oldSession, true), "existing earlier session rejected");
var emptyId = Session(); emptyId.Id = Guid.Empty;
Same(state, InteractiveWalkthroughProgress.ObserveNewSession(state, season, emptyId, true), "empty session identity rejected");
state = InteractiveWalkthroughProgress.ObserveNewSession(state, season, session, true);
Check(state.Step == InteractiveWalkthroughStep.CaptureBaseline && state.SessionId == session.Id && state.SeasonId == season, "bind explicit new session");
Baseline(session); var baselineRead = Read(session.Tracks[XpClass.Assault].Current!);
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, baselineRead, false, true), "screenshot or mock baseline rejected");
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, baselineRead, true, false), "check key cannot claim baseline capture");
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(session.Tracks[XpClass.Assault].Current!, ReadStatus.LowConfidence), true, true), "rejected OCR cannot advance");
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, Guid.NewGuid(), session, baselineRead, true, true), "wrong season rejected");
var wrongSession = Session(); Baseline(wrongSession);
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, wrongSession, Read(wrongSession.Tracks[XpClass.Assault].Current!), true, true), "wrong session rejected");
var original = session.Tracks[XpClass.Assault].OriginalBaseline!;
original.Origin = ReadingSource.Manual;
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, baselineRead, true, true), "manual initial baseline rejected");
original.Origin = ReadingSource.ReusedFinal;
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, baselineRead, true, true), "reused final initial baseline rejected");
original.Origin = ReadingSource.SnapshotHotkey;
var beforeSession = JsonSerializer.Serialize(session);
state = InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, baselineRead, true, true);
Check(state.Step == InteractiveWalkthroughStep.ConfirmStartingCoverage && state.Class == XpClass.Assault && state.BaselineId == original.Id && state.BaselineVersion == original.Version, "bind original real baseline and version");
Check(beforeSession == JsonSerializer.Serialize(session), "capture progress never mutates session");
Same(state, InteractiveWalkthroughProgress.CheckCoverage(state, season, session), "unknown coverage does not advance");
Certify(session, XpClass.Assault);
Same(state, InteractiveWalkthroughProgress.ObserveCoverageConfirmed(state, season, session, XpClass.Assault), "one confirmed class does not certify remaining five");
foreach (var track in session.Tracks.Values.Where(t => t.Class != XpClass.Assault)) track.Enabled = false;
var certifiedBefore = JsonSerializer.Serialize(session);
state = InteractiveWalkthroughProgress.CheckCoverage(state, season, session);
Check(state.Step == InteractiveWalkthroughStep.CheckCurrentXp, "explicit disabled tracks excluded from required coverage");
Check(certifiedBefore == JsonSerializer.Serialize(session), "coverage check never certifies a class");
var sameXpCurrent = Observation(56486, ScreenKind.Unlock, ReadingSource.CheckHotkey, 3);
session.Tracks[XpClass.Assault].Current = sameXpCurrent;
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(sameXpCurrent), false, false), "manual/import current source rejected");
sameXpCurrent.Source = ReadingSource.Manual;
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(sameXpCurrent), true, false), "manual observation cannot pass even when real provenance is asserted");
sameXpCurrent.Source = ReadingSource.CheckHotkey;
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(sameXpCurrent), true, true), "snapshot cannot substitute current check");
var wrongClass = Observation(57000, ScreenKind.Unlock, ReadingSource.CheckHotkey, 3); wrongClass.Class = XpClass.Medic;
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(wrongClass), true, false), "another class cannot satisfy sample check");
state = InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(sameXpCurrent), true, false);
Check(state.Step == InteractiveWalkthroughStep.CaptureFinalXp, "same XP current read allowed without fabricated gain");
var finalObservation = Observation(62986, ScreenKind.EndProgression, ReadingSource.CheckHotkey, 4);
session.PendingFinal = new() { SessionId = session.Id, FirstObservedAt = finalObservation.At, LastObservedAt = finalObservation.At, Tracks = { [XpClass.Assault] = finalObservation } };
var endScore = Observation(62986, ScreenKind.EndScore, ReadingSource.CheckHotkey, 4);
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(endScore), true, false), "score-only page cannot substitute final XP");
Same(state, InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(finalObservation), false, false), "imported final cannot advance");
state = InteractiveWalkthroughProgress.ObserveAcceptedRead(state, season, session, Read(finalObservation), true, false);
Check(state.Step == InteractiveWalkthroughStep.ConfirmFinalXp, "real pending final captured");
Same(state, InteractiveWalkthroughProgress.ObserveFinalConfirmed(state, season, session), "candidate is not confirmation");
session.ConfirmedFinal = session.PendingFinal; session.PendingFinal = null;
Same(state, InteractiveWalkthroughProgress.ObserveFinalConfirmed(state, season, session), "confirmed timestamp required");
session.ConfirmedFinal!.ConfirmedAt = now.AddSeconds(5);
state = InteractiveWalkthroughProgress.ObserveFinalConfirmed(state, season, session);
Check(state.Step == InteractiveWalkthroughStep.OpenAnalytics, "explicit confirmed final advances");
Same(state, InteractiveWalkthroughProgress.ObserveAnalyticsOpened(state, season, session.Id, false), "import/fake analytics cannot complete walkthrough");
Same(state, InteractiveWalkthroughProgress.ObserveAnalyticsOpened(state, Guid.NewGuid(), session.Id, true), "different season analytics rejected");
Same(state, InteractiveWalkthroughProgress.ObserveAnalyticsOpened(state, season, Guid.NewGuid(), true), "different session analytics rejected");
state = InteractiveWalkthroughProgress.ObserveAnalyticsOpened(state, season, session.Id, true);
Check(state.Step == InteractiveWalkthroughStep.Completed, "explicit real analytics opened completes");
Same(state, InteractiveWalkthroughProgress.Pause(state), "completed run cannot become paused");
Check(JsonSerializer.Serialize(JsonSerializer.Deserialize<InteractiveWalkthroughState>(JsonSerializer.Serialize(state))) == JsonSerializer.Serialize(state), "progress round trips independently of tracker");
var next = InteractiveWalkthroughProgress.RestartForNextMatch(state, now.AddHours(1));
Check(next.Step == InteractiveWalkthroughStep.SelectGameWindow && next.SessionId is null && next.Class is null && next.RunId != state.RunId, "later match starts separate unbound attempt");

var allSession = Session(); var allState = Captured(allSession);
foreach (var track in allSession.Tracks.Keys) Certify(allSession, track);
Check(InteractiveWalkthroughProgress.CheckCoverage(allState, season, allSession).Step == InteractiveWalkthroughStep.CheckCurrentXp, "all six explicit valid originals pass");
allSession.Tracks[XpClass.Medic].OriginalBaseline!.Validity = BaselineValidity.Invalid;
Same(allState, InteractiveWalkthroughProgress.CheckCoverage(allState, season, allSession), "invalid enabled original blocks");
allSession.Tracks[XpClass.Medic].OriginalBaseline!.Validity = BaselineValidity.Valid;
allSession.Tracks[XpClass.Medic].OriginalBaseline!.Coverage = BaselineCoverage.Partial;
Same(allState, InteractiveWalkthroughProgress.CheckCoverage(allState, season, allSession), "partial enabled original blocks");
allSession.Tracks[XpClass.Medic].OriginalBaseline = null;
Same(allState, InteractiveWalkthroughProgress.CheckCoverage(allState, season, allSession), "missing enabled original blocks");
allSession.Tracks[XpClass.Assault].WasRebased = true;
Same(allState, InteractiveWalkthroughProgress.CheckCoverage(allState, season, allSession), "sample rebase cannot retain walkthrough proof");
allSession.Tracks[XpClass.Assault].WasRebased = false;
allSession.Tracks[XpClass.Assault].Baseline!.Version++;
Same(allState, InteractiveWalkthroughProgress.CheckCoverage(allState, season, allSession), "changed baseline version rejected");

var coreClock = now.AddSeconds(1);
var core = new SessionTracker(clock: () => coreClock);
foreach (var trackClass in Enum.GetValues<XpClass>().Where(value => value != XpClass.Assault)) core.SetTrackEnabled(trackClass, false);
var coreState = InteractiveWalkthroughProgress.ObserveWindowSelected(InteractiveWalkthroughProgress.Start(now), true);
Check(core.StartSession().Applied, "production Core starts explicit session");
var coreSession = core.ActiveSession!;
coreState = InteractiveWalkthroughProgress.ObserveNewSession(coreState, core.Document.SeasonId, coreSession, true);
var coreBaseline = Read(Observation(56486, ScreenKind.Unlock, ReadingSource.SnapshotHotkey));
Check(core.Observe(coreBaseline, ReadingSource.SnapshotHotkey, true, coreSession.Id).Applied, "production Core accepts real baseline");
coreState = InteractiveWalkthroughProgress.ObserveAcceptedRead(coreState, core.Document.SeasonId, coreSession, coreBaseline, true, true);
Check(coreState.Stage == InteractiveWalkthroughStep.ConfirmStartingCoverage, "production baseline DTO fits guide");
Check(core.ConfirmCoverage(XpClass.Assault).Applied, "production Core explicitly confirms starting coverage");
coreState = InteractiveWalkthroughProgress.ObserveCoverageConfirmed(coreState, core.Document.SeasonId, coreSession, XpClass.Assault);
var coreCheck = Read(Observation(56486, ScreenKind.Unlock, ReadingSource.CheckHotkey, 3));
Check(core.Observe(coreCheck, ReadingSource.CheckHotkey, false, coreSession.Id).Applied, "production Core accepts same-XP check");
coreState = InteractiveWalkthroughProgress.ObserveAcceptedRead(coreState, core.Document.SeasonId, coreSession, coreCheck, true, false);
Check(coreState.Stage == InteractiveWalkthroughStep.CaptureFinalXp, "production unchanged-XP read fits guide");
var coreFinal = Read(Observation(62986, ScreenKind.EndProgression, ReadingSource.CheckHotkey, 4));
Check(core.Observe(coreFinal, ReadingSource.CheckHotkey, false, coreSession.Id).Applied, "production Core stages final");
coreState = InteractiveWalkthroughProgress.ObserveAcceptedRead(coreState, core.Document.SeasonId, coreSession, coreFinal, true, false);
Check(coreState.Stage == InteractiveWalkthroughStep.ConfirmFinalXp, "production pending-final DTO fits guide");
coreClock = now.AddSeconds(5);
Check(core.ConfirmFinal(2).Applied, "production Core explicitly confirms final");
coreState = InteractiveWalkthroughProgress.ObserveFinalConfirmed(coreState, core.Document.SeasonId, coreSession);
Check(coreState.Stage == InteractiveWalkthroughStep.OpenAnalytics, "production confirmed-final DTO fits guide");
var documentBeforeGuide = JsonSerializer.Serialize(core.Document);
coreState = InteractiveWalkthroughProgress.ObserveAnalyticsOpened(coreState, core.Document.SeasonId, coreSession.Id, true);
Check(coreState.Stage == InteractiveWalkthroughStep.Completed, "production end-to-end guide completes");
Check(documentBeforeGuide == JsonSerializer.Serialize(core.Document), "guide leaves production session final and history unchanged");

(SessionTracker Tracker, InteractiveWalkthroughState State) RetainedFixture(InteractiveWalkthroughStep pausedStage,
    bool certify = true, bool current = true, bool final = true, bool confirm = false)
{
    var clock = now.AddSeconds(1);
    var model = new SessionTracker(clock: () => clock);
    foreach (var trackClass in Enum.GetValues<XpClass>().Where(value => value != XpClass.Assault)) model.SetTrackEnabled(trackClass, false);
    var progress = InteractiveWalkthroughProgress.ObserveWindowSelected(InteractiveWalkthroughProgress.Start(now), true);
    model.StartSession(); var match = model.ActiveSession!;
    progress = InteractiveWalkthroughProgress.ObserveNewSession(progress, model.Document.SeasonId, match, true);
    var baselinePage = Read(Observation(56486, ScreenKind.Unlock, ReadingSource.SnapshotHotkey));
    model.Observe(baselinePage, ReadingSource.SnapshotHotkey, true, match.Id);
    if (pausedStage > InteractiveWalkthroughStep.CaptureBaseline)
        progress = InteractiveWalkthroughProgress.ObserveAcceptedRead(progress, model.Document.SeasonId, match, baselinePage, true, true);
    if (certify) { clock = now.AddSeconds(2); model.ConfirmCoverage(XpClass.Assault); }
    if (pausedStage > InteractiveWalkthroughStep.ConfirmStartingCoverage)
        progress = InteractiveWalkthroughProgress.CheckCoverage(progress, model.Document.SeasonId, match);
    var currentPage = Read(Observation(58986, ScreenKind.Unlock, ReadingSource.CheckHotkey, 3));
    if (current) { clock = now.AddSeconds(3); model.Observe(currentPage, ReadingSource.CheckHotkey, false, match.Id); }
    if (pausedStage > InteractiveWalkthroughStep.CheckCurrentXp)
        progress = InteractiveWalkthroughProgress.ObserveAcceptedRead(progress, model.Document.SeasonId, match, currentPage, true, false);
    var finalPage = Read(Observation(62986, ScreenKind.EndProgression, ReadingSource.CheckHotkey, 4));
    if (final) { clock = now.AddSeconds(4); model.Observe(finalPage, ReadingSource.CheckHotkey, false, match.Id); }
    if (pausedStage > InteractiveWalkthroughStep.CaptureFinalXp)
        progress = InteractiveWalkthroughProgress.ObserveAcceptedRead(progress, model.Document.SeasonId, match, finalPage, true, false);
    if (confirm) { clock = now.AddSeconds(5); model.ConfirmFinal(2); }
    return (model, InteractiveWalkthroughProgress.Pause(progress));
}
InteractiveWalkthroughState Reconcile(SessionTracker model, InteractiveWalkthroughState progress, bool real = true)
    => InteractiveWalkthroughProgress.ReconcileRetainedCaptures(InteractiveWalkthroughProgress.Resume(progress),
        model.Document.SeasonId, model.ActiveSession!, real);

var pendingPause = RetainedFixture(InteractiveWalkthroughStep.CaptureFinalXp);
var pendingBefore = JsonSerializer.Serialize(pendingPause.Tracker.Document);
var recoveredPending = Reconcile(pendingPause.Tracker, pendingPause.State);
Check(recoveredPending.Stage == InteractiveWalkthroughStep.ConfirmFinalXp && recoveredPending.FinalCapturedAt == now.AddSeconds(4), "resume uses retained real pending final");
Check(pendingBefore == JsonSerializer.Serialize(pendingPause.Tracker.Document), "resume never changes saved pending XP or coverage");
Same(pendingPause.State, InteractiveWalkthroughProgress.ReconcileRetainedCaptures(pendingPause.State,
    pendingPause.Tracker.Document.SeasonId, pendingPause.Tracker.ActiveSession!, true), "reconciliation does not itself unpause a guide");
var confirmedPause = RetainedFixture(InteractiveWalkthroughStep.CaptureFinalXp, confirm: true);
Check(Reconcile(confirmedPause.Tracker, confirmedPause.State).Stage == InteractiveWalkthroughStep.OpenAnalytics, "resume uses retained real final and prior explicit confirmation");
var currentPause = RetainedFixture(InteractiveWalkthroughStep.CheckCurrentXp, final: false);
Check(Reconcile(currentPause.Tracker, currentPause.State).Stage == InteractiveWalkthroughStep.CaptureFinalXp, "resume uses accepted real current captured while paused");
var baselinePause = RetainedFixture(InteractiveWalkthroughStep.CaptureBaseline, final: false);
var recoveredBaseline = Reconcile(baselinePause.Tracker, baselinePause.State);
Check(recoveredBaseline.Stage == InteractiveWalkthroughStep.CaptureFinalXp && recoveredBaseline.BaselineId == baselinePause.Tracker.ActiveSession!.Tracks[XpClass.Assault].OriginalBaseline!.Id,
    "resume recovers authentic baseline even after its current reading changed");
var allPaused = RetainedFixture(InteractiveWalkthroughStep.CaptureBaseline, confirm: true);
Check(Reconcile(allPaused.Tracker, allPaused.State).Stage == InteractiveWalkthroughStep.OpenAnalytics, "resume can replay all retained real steps but still requires opening Analytics");
var noCoveragePause = RetainedFixture(InteractiveWalkthroughStep.CaptureBaseline, certify: false);
var noCoverageBefore = JsonSerializer.Serialize(noCoveragePause.Tracker.Document);
Check(Reconcile(noCoveragePause.Tracker, noCoveragePause.State).Stage == InteractiveWalkthroughStep.ConfirmStartingCoverage, "resume never infers missing starting coverage");
Check(noCoverageBefore == JsonSerializer.Serialize(noCoveragePause.Tracker.Document), "resume leaves unknown coverage unknown");
var noReal = InteractiveWalkthroughProgress.Resume(pendingPause.State);
Same(noReal, Reconcile(pendingPause.Tracker, pendingPause.State, false), "mock or imported retained data rejected");
Same(noReal, InteractiveWalkthroughProgress.ReconcileRetainedCaptures(noReal, Guid.NewGuid(), pendingPause.Tracker.ActiveSession!, true), "retained wrong season rejected");
Same(noReal, InteractiveWalkthroughProgress.ReconcileRetainedCaptures(noReal, pendingPause.Tracker.Document.SeasonId, Session(), true), "retained wrong session rejected");
var stale = noReal with { CurrentCapturedAt = now.AddSeconds(5) };
Same(stale, InteractiveWalkthroughProgress.ReconcileRetainedCaptures(stale, pendingPause.Tracker.Document.SeasonId, pendingPause.Tracker.ActiveSession!, true), "retained final before current milestone rejected");
var freshDuplicate = Read(Observation(62986, ScreenKind.EndProgression, ReadingSource.CheckHotkey, 8));
Check(pendingPause.Tracker.Observe(freshDuplicate, ReadingSource.CheckHotkey, false, pendingPause.Tracker.ActiveSession!.Id).Applied, "Core accepts repeated final frame");
Check(pendingPause.Tracker.ActiveSession.PendingFinal!.Tracks[XpClass.Assault].At == now.AddSeconds(4), "Core deduplicates final timestamp");
var dedupProgress = InteractiveWalkthroughProgress.ObserveAcceptedRead(noReal, pendingPause.Tracker.Document.SeasonId, pendingPause.Tracker.ActiveSession, freshDuplicate, true, false);
Check(dedupProgress.Stage == InteractiveWalkthroughStep.ConfirmFinalXp && dedupProgress.FinalCapturedAt == now.AddSeconds(4), "fresh repeated final matches retained first observation safely");
Same(stale, InteractiveWalkthroughProgress.ObserveAcceptedRead(stale, pendingPause.Tracker.Document.SeasonId, pendingPause.Tracker.ActiveSession, freshDuplicate, true, false), "new frame cannot make stale retained final satisfy later current milestone");
var manualCurrent = RetainedFixture(InteractiveWalkthroughStep.CheckCurrentXp, current: false, final: false);
manualCurrent.Tracker.ManualCurrent(XpClass.Assault, 58986);
var manualCurrentState = InteractiveWalkthroughProgress.Resume(manualCurrent.State);
Same(manualCurrentState, Reconcile(manualCurrent.Tracker, manualCurrent.State), "manual-only current cannot satisfy retained capture step");
var manualFinal = RetainedFixture(InteractiveWalkthroughStep.CaptureFinalXp, final: false);
manualFinal.Tracker.ActiveSession!.PendingFinal = new() { SessionId = manualFinal.Tracker.ActiveSession.Id,
    FirstObservedAt = now.AddSeconds(4), LastObservedAt = now.AddSeconds(4),
    Tracks = { [XpClass.Assault] = Observation(62986, ScreenKind.EndProgression, ReadingSource.Manual, 4) } };
Same(InteractiveWalkthroughProgress.Resume(manualFinal.State), Reconcile(manualFinal.Tracker, manualFinal.State), "manual-only pending final cannot satisfy retained capture step");
foreach (var origin in new[] { ReadingSource.Manual, ReadingSource.ReusedFinal })
{
    var badBaseline = RetainedFixture(InteractiveWalkthroughStep.CaptureBaseline, final: false);
    badBaseline.Tracker.ActiveSession!.Tracks[XpClass.Assault].Baseline!.Origin = origin;
    badBaseline.Tracker.ActiveSession.Tracks[XpClass.Assault].OriginalBaseline!.Origin = origin;
    Same(InteractiveWalkthroughProgress.Resume(badBaseline.State), Reconcile(badBaseline.Tracker, badBaseline.State), $"retained {origin} baseline rejected");
}
var noHistoryBaseline = RetainedFixture(InteractiveWalkthroughStep.CaptureBaseline, final: false);
noHistoryBaseline.Tracker.ActiveSession!.History.RemoveAll(item => item.Kind == "BaselineEstablished");
Same(InteractiveWalkthroughProgress.Resume(noHistoryBaseline.State), Reconcile(noHistoryBaseline.Tracker, noHistoryBaseline.State), "retained baseline needs authentic source history");
var rebasedPause = RetainedFixture(InteractiveWalkthroughStep.CaptureFinalXp);
rebasedPause.Tracker.ActiveSession!.Tracks[XpClass.Assault].WasRebased = true;
Same(InteractiveWalkthroughProgress.Resume(rebasedPause.State), Reconcile(rebasedPause.Tracker, rebasedPause.State), "rebase cannot reuse earlier retained proof");

var skipped = InteractiveWalkthroughProgress.Start(now);
for (var task = InteractiveWalkthroughStep.SelectGameWindow; task < InteractiveWalkthroughStep.Completed; task++)
{
    var beforeSkip = skipped;
    skipped = InteractiveWalkthroughProgress.SkipStep(skipped);
    Check(skipped.Stage == task + 1 && skipped.SkippedSteps.Contains(task), $"skip moves exactly one task: {task}");
    Check(InteractiveWalkthroughProgress.IsSupported(skipped), $"skipped missing prerequisites remain supported: {task}");
    Check(skipped.SessionId is null && skipped.Class is null && skipped.BaselineId is null
        && skipped.CurrentCapturedAt is null && skipped.FinalCapturedAt is null, $"skip fabricates no evidence: {task}");
    Check(beforeSkip.SkippedSteps.Count + 1 == skipped.SkippedSteps.Count, "skip preserves prior immutable task metadata");
}
Check(skipped.HasSkippedSteps && skipped.SkippedSteps.Count == 8, "all skipped tasks stay explicit at the end");
Check(JsonSerializer.Serialize(JsonSerializer.Deserialize<InteractiveWalkthroughState>(JsonSerializer.Serialize(skipped))) == JsonSerializer.Serialize(skipped),
    "skipped state round trips with no fabricated evidence");
Same(skipped, InteractiveWalkthroughProgress.SkipStep(skipped), "finished skip is idempotent");
var pausedSkip = InteractiveWalkthroughProgress.Pause(InteractiveWalkthroughProgress.Start(now));
Same(pausedSkip, InteractiveWalkthroughProgress.SkipStep(pausedSkip), "pause blocks Skip Step");
Same(future, InteractiveWalkthroughProgress.SkipStep(future), "future schema blocks Skip Step");
Check(!InteractiveWalkthroughProgress.IsSupported(skipped with { SkippedSteps = [InteractiveWalkthroughStep.Completed] }), "future skip metadata is invalid");
Check(!InteractiveWalkthroughProgress.IsSupported(skipped with { SkippedSteps = [InteractiveWalkthroughStep.SelectGameWindow, InteractiveWalkthroughStep.SelectGameWindow] }), "duplicate skip metadata is invalid");
Check(!InteractiveWalkthroughProgress.IsSupported(skipped with { SessionId = Guid.NewGuid() }), "skips do not excuse partial session bindings");
Check(!InteractiveWalkthroughProgress.IsSupported(skipped with { BaselineId = Guid.NewGuid() }), "skips do not excuse partial baseline bindings");
Check(!InteractiveWalkthroughProgress.IsSupported(skipped with { CurrentCapturedAt = now }), "skips cannot fabricate a current timestamp without a real session/class");

var skipToCurrent = InteractiveWalkthroughProgress.Start(now);
for (var i = 0; i < 4; i++) skipToCurrent = InteractiveWalkthroughProgress.SkipStep(skipToCurrent);
var afterSkipSession = Session();
afterSkipSession.Tracks[XpClass.Assault].Current = Observation(58986, ScreenKind.Unlock, ReadingSource.CheckHotkey, 3);
var afterSkipBefore = JsonSerializer.Serialize(afterSkipSession);
Same(skipToCurrent, InteractiveWalkthroughProgress.ObserveAcceptedRead(skipToCurrent, season, afterSkipSession,
    Read(afterSkipSession.Tracks[XpClass.Assault].Current!), false, false), "mock capture after skip cannot bind context");
var afterSkipCurrent = InteractiveWalkthroughProgress.ObserveAcceptedRead(skipToCurrent, season, afterSkipSession,
    Read(afterSkipSession.Tracks[XpClass.Assault].Current!), true, false);
Check(afterSkipCurrent.Stage == InteractiveWalkthroughStep.CaptureFinalXp && afterSkipCurrent.SessionId == afterSkipSession.Id
    && afterSkipCurrent.Class == XpClass.Assault && afterSkipCurrent.CurrentCapturedAt == now.AddSeconds(3),
    "later actual current capture binds its real fresh session/class after skipped prerequisites");
Check(afterSkipCurrent.BaselineId is null && afterSkipCurrent.BaselineCapturedAt is null && afterSkipCurrent.SkippedSteps.Count == 4,
    "late current binding never invents a skipped baseline or removes skip metadata");
Check(afterSkipBefore == JsonSerializer.Serialize(afterSkipSession), "late guide binding leaves tracker data untouched");
Check(InteractiveWalkthroughProgress.IsSupported(afterSkipCurrent), "late current evidence after skips has a supported state");
var oldSkipSession = Session(); oldSkipSession.StartedAt = now.AddSeconds(-1);
oldSkipSession.Tracks[XpClass.Assault].Current = Observation(58986, ScreenKind.Unlock, ReadingSource.CheckHotkey, 3);
Same(skipToCurrent, InteractiveWalkthroughProgress.ObserveAcceptedRead(skipToCurrent, season, oldSkipSession,
    Read(oldSkipSession.Tracks[XpClass.Assault].Current!), true, false), "skip cannot bind a match from before the guide");

afterSkipSession.PendingFinal = new() { SessionId = afterSkipSession.Id, Tracks =
    { [XpClass.Assault] = Observation(62986, ScreenKind.EndProgression, ReadingSource.CheckHotkey, 4) } };
var afterSkipFinal = InteractiveWalkthroughProgress.ObserveAcceptedRead(afterSkipCurrent, season, afterSkipSession,
    Read(afterSkipSession.PendingFinal.Tracks[XpClass.Assault]), true, false);
Check(afterSkipFinal.Stage == InteractiveWalkthroughStep.ConfirmFinalXp && afterSkipFinal.FinalCapturedAt == now.AddSeconds(4), "real final capture works after skipped baseline/coverage");
Check(afterSkipSession.ConfirmedFinal is null, "guide progress after skipped steps never confirms a final result");
var skipFinalConfirmation = InteractiveWalkthroughProgress.SkipStep(afterSkipFinal);
Check(skipFinalConfirmation.Stage == InteractiveWalkthroughStep.OpenAnalytics && afterSkipSession.ConfirmedFinal is null,
    "skipping final confirmation leaves the candidate pending");
var skippedAnalytics = InteractiveWalkthroughProgress.ObserveAnalyticsOpened(skipFinalConfirmation, season, afterSkipSession.Id, true);
Check(skippedAnalytics.Stage == InteractiveWalkthroughStep.Completed && skippedAnalytics.HasSkippedSteps,
    "real Analytics can finish skipped navigation without fabricating final confirmation");
Check(afterSkipSession.Tracks[XpClass.Assault].OriginalBaseline is null && afterSkipSession.ConfirmedFinal is null,
    "skipped navigation does not create original coverage or confirmed totals");

var skipOnlyCoverageSession = Session(); var skipOnlyCoverage = Captured(skipOnlyCoverageSession);
var unknownStart = JsonSerializer.Serialize(skipOnlyCoverageSession);
skipOnlyCoverage = InteractiveWalkthroughProgress.SkipStep(skipOnlyCoverage);
Check(skipOnlyCoverage.Stage == InteractiveWalkthroughStep.CheckCurrentXp && skipOnlyCoverage.SkippedSteps.SequenceEqual([InteractiveWalkthroughStep.ConfirmStartingCoverage]),
    "skip coverage shows current-XP task immediately");
Check(unknownStart == JsonSerializer.Serialize(skipOnlyCoverageSession) && skipOnlyCoverageSession.Tracks[XpClass.Assault].Baseline!.Coverage == BaselineCoverage.Unknown,
    "skipping coverage preserves its unknown attestation");
skipOnlyCoverageSession.Tracks[XpClass.Assault].Current = Observation(58986, ScreenKind.Unlock, ReadingSource.CheckHotkey, 3);
skipOnlyCoverage = InteractiveWalkthroughProgress.ObserveAcceptedRead(skipOnlyCoverage, season, skipOnlyCoverageSession,
    Read(skipOnlyCoverageSession.Tracks[XpClass.Assault].Current!), true, false);
Check(skipOnlyCoverage.Stage == InteractiveWalkthroughStep.CaptureFinalXp, "actual current capture can follow skipped timing without certifying timing");
var skipFinalCapture = InteractiveWalkthroughProgress.SkipStep(skipOnlyCoverage);
Check(skipFinalCapture.Stage == InteractiveWalkthroughStep.ConfirmFinalXp && skipFinalCapture.FinalCapturedAt is null,
    "skipping final capture does not create a final observation time");
Same(skipFinalCapture, InteractiveWalkthroughProgress.ObserveFinalConfirmed(skipFinalCapture, season, skipOnlyCoverageSession),
    "skipped final capture cannot fabricate a final confirmation");
var skipToAnalytics = InteractiveWalkthroughProgress.Start(now);
for (var i = 0; i < 7; i++) skipToAnalytics = InteractiveWalkthroughProgress.SkipStep(skipToAnalytics);
Same(skipToAnalytics, InteractiveWalkthroughProgress.ObserveAnalyticsOpened(skipToAnalytics, season, null, false), "mock/import Analytics cannot fulfill the last visible task after skips");
Check(InteractiveWalkthroughProgress.ObserveAnalyticsOpened(skipToAnalytics, season, null, true).Stage == InteractiveWalkthroughStep.Completed,
    "unbound skip navigation can view real Analytics without creating a match");
var restartedSkipped = InteractiveWalkthroughProgress.RestartForNextMatch(skipped, now.AddDays(1));
Check(!restartedSkipped.HasSkippedSteps && restartedSkipped.Stage == InteractiveWalkthroughStep.SelectGameWindow,
    "restart clears only the old guide's skip metadata");


Check(!InteractiveWalkthroughProgress.IsSupported(skipped with { Class = XpClass.Assault }), "skip metadata cannot invent an unbound class");
var manualCoverageAfterSkip = Bind(Session());
var manualCoverageSession = Session();
manualCoverageAfterSkip = Bind(manualCoverageSession);
manualCoverageAfterSkip = InteractiveWalkthroughProgress.SkipStep(manualCoverageAfterSkip);
var manualCoverageBaseline = new BaselineRecord { Xp = 100, At = now.AddSeconds(2), Origin = ReadingSource.Manual,
    Validity = BaselineValidity.Valid, Coverage = BaselineCoverage.BeforeEarnedXp };
manualCoverageSession.Tracks[XpClass.Assault].Baseline = manualCoverageSession.Tracks[XpClass.Assault].OriginalBaseline = manualCoverageBaseline;
foreach (var track in manualCoverageSession.Tracks.Keys.Where(track => track != XpClass.Assault)) manualCoverageSession.Tracks[track].Enabled = false;
var manualCoverageJson = JsonSerializer.Serialize(manualCoverageSession);
manualCoverageAfterSkip = InteractiveWalkthroughProgress.CheckCoverage(manualCoverageAfterSkip, season, manualCoverageSession);
Check(manualCoverageAfterSkip.Stage == InteractiveWalkthroughStep.CheckCurrentXp && manualCoverageAfterSkip.Class == XpClass.Assault,
    "actual explicit manual-baseline timing confirmation can follow a skipped capture task");
Check(manualCoverageAfterSkip.BaselineId is null && manualCoverageAfterSkip.BaselineCapturedAt is null
    && manualCoverageAfterSkip.SkippedSteps.Contains(InteractiveWalkthroughStep.CaptureBaseline),
    "manual timing after skip never claims a real baseline capture");
Check(manualCoverageJson == JsonSerializer.Serialize(manualCoverageSession), "guide only reads the manual timing confirmation");
var noCertAfterSkip = RetainedFixture(InteractiveWalkthroughStep.ConfirmStartingCoverage, certify: false, current: true, final: true);
var skippedCert = InteractiveWalkthroughProgress.SkipStep(InteractiveWalkthroughProgress.Resume(noCertAfterSkip.State));
var noCertJson = JsonSerializer.Serialize(noCertAfterSkip.Tracker.Document);
var reconciledAfterSkip = InteractiveWalkthroughProgress.ReconcileRetainedCaptures(skippedCert,
    noCertAfterSkip.Tracker.Document.SeasonId, noCertAfterSkip.Tracker.ActiveSession!, true);
Check(reconciledAfterSkip.Stage == InteractiveWalkthroughStep.ConfirmFinalXp && reconciledAfterSkip.SkippedSteps.Contains(InteractiveWalkthroughStep.ConfirmStartingCoverage),
    "resume can use actual retained reads after a task was explicitly skipped");
Check(noCertAfterSkip.Tracker.ActiveSession!.Tracks[XpClass.Assault].Baseline!.Coverage == BaselineCoverage.Unknown
    && noCertJson == JsonSerializer.Serialize(noCertAfterSkip.Tracker.Document), "reconciliation after a skip still never certifies timing or rewrites records");

Console.WriteLine($"PASS {checks} interactive walkthrough state checks");
