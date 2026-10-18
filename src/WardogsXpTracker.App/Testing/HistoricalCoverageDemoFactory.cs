using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App.Testing;

/// <summary>Creates a completed mock match whose original starting baselines need confirmation.</summary>
public static class HistoricalCoverageDemoFactory
{
    public static SeasonDocument CreateSeason()
    {
        var document = new SeasonDocument { Name = "MOCK — Saved starting-baseline confirmation" };
        var time = new DateTimeOffset(2026, 10, 17, 15, 0, 0, TimeSpan.Zero);
        DateTimeOffset Clock() { var result = time; time = time.AddMinutes(1); return result; }
        var tracker = new SessionTracker(document, Clock);
        void Apply(TrackerActionResult result)
        {
            if (!result.Applied) throw new InvalidOperationException($"Mock setup failed: {result.Message}");
        }
        ScreenReadResult Frame(long assault, long medic, ScreenKind screen, XpClass? onlyClass = null) => new(screen, ReadStatus.Accepted,
            "MOCK: generated starting-baseline confirmation workflow.",
            new XpReading[] { new(XpClass.Assault, assault, 1_000_000, null, null, screen, Clock(), new(.35, .22, .12, .02),
                $"MOCK {assault}/1000000XP", 1),
             new(XpClass.Medic, medic, 1_000_000, null, null, screen, Clock(), new(.35, .22, .12, .02),
                $"MOCK {medic}/1000000XP", 1) }.Where(reading => onlyClass is null || reading.Class == onlyClass).ToArray(),
            PlacementEvidence.None, [], []);

        Apply(tracker.StartSession());
        foreach (var track in Enum.GetValues<XpClass>())
            Apply(tracker.SetTrackEnabled(track, track is XpClass.Assault or XpClass.Medic));
        Apply(tracker.Observe(Frame(10_000, 20_000, ScreenKind.Unlock, XpClass.Assault), ReadingSource.SnapshotHotkey, true));
        Apply(tracker.Observe(Frame(10_000, 20_000, ScreenKind.Unlock, XpClass.Medic), ReadingSource.SnapshotHotkey, true));
        Apply(tracker.Observe(Frame(12_500, 22_000, ScreenKind.Unlock, XpClass.Assault), ReadingSource.CheckHotkey));
        Apply(tracker.Observe(Frame(12_500, 22_000, ScreenKind.Unlock, XpClass.Medic), ReadingSource.CheckHotkey));
        Apply(tracker.Observe(Frame(16_500, 25_200, ScreenKind.EndProgression), ReadingSource.AutomaticOcr));
        Apply(tracker.ConfirmFinal(1));
        tracker.Document.Sessions.Single().History.Add(new()
        {
            Kind = "DemoScenario", At = Clock(), Source = ReadingSource.Manual,
            Message = "MOCK: completed first-place match with unconfirmed Assault and Medic starting baselines. "
                + "In Saved Results, open Starting baselines. Confirm Assault, then confirm all remaining eligible baselines. "
                + "Reopen Summary to see the supported 4,500 in-match XP and 7,200 bonus XP."
        });
        TrackingDocumentValidator.Validate(document);
        return document;
    }
}
