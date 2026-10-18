using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App.Testing;

/// <summary>Creates isolated sample history. It never reads or writes the player's save files.</summary>
public static class DemoStatsFactory
{
    public static SeasonDocument CreateAnalyticsSeason()
    {
        var document = new SeasonDocument
        {
            SeasonId = Guid.Parse("3a5139bc-7099-4f61-b211-e7751ea9f0aa"),
            Name = "DEMO — Analytics examples"
        };
        var time = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset Clock() { var result = time; time = time.AddMinutes(1); return result; }
        var tracker = new SessionTracker(document, Clock);

        void Apply(TrackerActionResult result)
        {
            if (!result.Applied) throw new InvalidOperationException($"Demo setup failed: {result.Message}");
        }

        ScreenReadResult Frame(XpClass track, long xp, ScreenKind screen = ScreenKind.Unlock,
            PlacementEvidence? placement = null) => new(screen, ReadStatus.Accepted, "MOCK: generated example observation.",
            [new(track, xp, 1_000_000, null, null, screen, Clock(), new(.35, .22, .12, .02),
                $"MOCK {xp}/1000000XP", 1)], placement ?? PlacementEvidence.None, [], []);

        void Start(string label, XpClass track, long baseline, bool certify = true)
        {
            Apply(tracker.StartSession());
            foreach (var candidate in Enum.GetValues<XpClass>())
                Apply(tracker.SetTrackEnabled(candidate, candidate == track));
            tracker.ActiveSession!.History.Add(new()
            {
                Kind = "DemoScenario", At = Clock(), Message = label, Source = ReadingSource.Manual
            });
            Apply(tracker.Observe(Frame(track, baseline), ReadingSource.SnapshotHotkey, true));
            if (certify) Apply(tracker.ConfirmCoverage(track));
        }

        void Current(XpClass track, long xp) => Apply(tracker.Observe(Frame(track, xp), ReadingSource.CheckHotkey));
        void Final(XpClass track, long xp, int? place = null, bool confirm = true)
        {
            Apply(tracker.Observe(Frame(track, xp, ScreenKind.EndProgression), ReadingSource.AutomaticOcr));
            if (confirm) Apply(tracker.ConfirmFinal(place));
        }

        Start("01 · Exact first place: Assault actions 2,500 + bonus 4,000", XpClass.Assault, 10_000);
        Current(XpClass.Assault, 12_500);
        Final(XpClass.Assault, 16_500, 1);

        Start("02 · Exact second place: Medic actions 2,000 + bonus 2,800", XpClass.Medic, 20_000);
        Current(XpClass.Medic, 22_000);
        Final(XpClass.Medic, 24_800, 2);

        Start("03 · Exact third place: Driver actions 1,500 + bonus 1,800", XpClass.Driver, 30_000);
        Current(XpClass.Driver, 31_500);
        Final(XpClass.Driver, 33_300, 3);

        Start("04 · DEFEAT: Pilot total 6,500, 500 XP indeterminate", XpClass.Pilot, 40_000);
        Current(XpClass.Pilot, 42_500);
        var defeat = new PlacementEvidence([2, 3], false,
            [new(PlacementCue.Defeat, [2, 3], new(.3, .1, .4, .1), "MOCK DEFEAT")]);
        Apply(tracker.Observe(new(ScreenKind.EndScore, ReadStatus.Accepted, "MOCK DEFEAT without placement.",
            [], defeat, [], []), ReadingSource.AutomaticOcr));
        Final(XpClass.Pilot, 46_500);

        Start("05 · Confirmed Recon total 6,500 with completion unknown", XpClass.Recon, 50_000);
        Current(XpClass.Recon, 52_500);
        Final(XpClass.Recon, 56_500);
        Apply(tracker.End(SessionOutcome.Unknown));

        Start("06 · Left match: Recon actions 3,000, average missed bonus 3,500", XpClass.Recon, 60_000);
        Current(XpClass.Recon, 63_000);
        Apply(tracker.End(SessionOutcome.LeftMatch));
        Apply(tracker.RecordClosingXp(XpClass.Recon, 63_000));

        Start("07 · Disconnected: Support actions 1,200, average missed bonus 1,400", XpClass.Support, 70_000);
        Current(XpClass.Support, 71_200);
        Apply(tracker.End(SessionOutcome.Disconnected));
        Apply(tracker.RecordClosingXp(XpClass.Support, 71_200));

        Start("08 · Partial early exit: Assault observed interval 1,500, missed bonus lower bound", XpClass.Assault, 80_000, false);
        Current(XpClass.Assault, 81_500);
        Apply(tracker.End(SessionOutcome.LeftMatch));

        Start("09 · Rules mismatch: Medic final delta 2,501 contradicts observed tier-five gain", XpClass.Medic, 90_000);
        Current(XpClass.Medic, 92_500);
        Final(XpClass.Medic, 92_501, 1);

        Start("10 · Unconfirmed Driver final candidate is excluded from received totals", XpClass.Driver, 100_000);
        Current(XpClass.Driver, 102_500);
        Final(XpClass.Driver, 106_500, confirm: false);

        Start("11 · Support rebase preserves a certified original: actions 3,000 + bonus 3,500", XpClass.Support, 110_000);
        Current(XpClass.Support, 112_000);
        Apply(tracker.ArmRebase(XpClass.Support));
        Apply(tracker.Observe(Frame(XpClass.Support, 112_000), ReadingSource.SnapshotHotkey, true));
        Current(XpClass.Support, 113_000);
        Final(XpClass.Support, 116_500, 2);

        TrackingDocumentValidator.Validate(document);
        return document;
    }
}
