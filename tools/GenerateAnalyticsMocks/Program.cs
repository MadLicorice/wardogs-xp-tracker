using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WardogsXpTracker.App.Testing;
using WardogsXpTracker.Core.Persistence;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Windows.Reading;

namespace WardogsXpTracker.GenerateAnalyticsMocks;

internal static class Program
{
    private const int Width = 1920, Height = 1080;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly Brush White = Brushes.White;
    private static readonly Brush Gray = Brush("#AEB5B8");
    private static readonly Brush Gold = Brush("#E1BA3E");
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true, Converters = { new JsonStringEnumConverter() }
    };

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2 || args[0] != "--output")
            {
                Console.Error.WriteLine("Use --output <isolated-folder> [--verify]. Existing output files are not replaced.");
                return 2;
            }
            var directory = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(directory);
            if (Directory.EnumerateFileSystemEntries(directory).Any())
                throw new IOException("Choose an empty output folder. Mock generation does not replace existing files.");
            var cases = new List<MockCase>();
            var gains = new[] { 0, 1, 500, 1000, 1500, 2000, 2499, 2500, 3000 };
            for (var i = 0; i < gains.Length; i++)
            {
                var gain = gains[i];
                var file = $"{i + 1:00}-assault-{(gain == 0 ? "baseline" : $"plus-{gain}")}.png";
                Save(directory, file, Unlock(56_486 + gain));
                cases.Add(new(file, ScreenKind.Unlock, ReadStatus.Accepted, 56_486 + gain, 64_500,
                    gain == 0 ? "New Session, then snapshot Assault baseline." : "Check Assault current XP after the first baseline.",
                    gain, gain >= 2500));
            }
            Save(directory, "10-assault-completed-page.png", Unlock(54_750, completed: true));
            cases.Add(new("10-assault-completed-page.png", ScreenKind.Unlock, ReadStatus.NoCurrentXp, null, null,
                "Reject this completed rewards page. Keep current XP 59,486 and show the rejection banner.", null, null));
            Save(directory, "11-score-defeat-placement-unknown.png", Score());
            cases.Add(new("11-score-defeat-placement-unknown.png", ScreenKind.EndScore, ReadStatus.Accepted, null, null,
                "Placement only: DEFEAT means 2nd or 3rd. Freeze live checks. Read image 12 for final XP before recording.", null, null));
            Save(directory, "12-progression-assault-final-plus-6500.png", Progression());
            cases.Add(new("12-progression-assault-final-plus-6500.png", ScreenKind.EndProgression, ReadStatus.Accepted, 62_986, 64_500,
                "Final cumulative XP 62,986 = starting 56,486 + total gain 6,500 INCLUDING bonus. Confirm for the summary, or review later in Saved Results.", null, null));

            var season = DemoStatsFactory.CreateAnalyticsSeason();
            new MarkdownSaveStore(directory).SaveAsync("demo-analytics-season.md", season).GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(new
            {
                Kind = "Explicit mock data for local testing", Version = 1, ImageCount = cases.Count,
                Resolution = "1920x1080", BaselineXp = 56_486, Cases = cases,
                DemoSeasonId = season.SeasonId, DemoSessionCount = season.Sessions.Count,
                Notes = "Images are code-rendered fixtures, not records of real matches. Importing them in Diagnostics Lab never changes a session. Use the app's testing flow for session actions."
            }, Json), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "README.md"), Guide, new UTF8Encoding(false));
            if (args.Contains("--verify", StringComparer.Ordinal)) Verify(directory, cases);
            Console.WriteLine($"Created {cases.Count} mock PNGs and {season.Sessions.Count} isolated demo sessions in {directory}.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Verify(string directory, IReadOnlyList<MockCase> cases)
    {
        using var reader = new XpImageReader();
        var observations = new List<object>();
        var failures = new List<string>();
        foreach (var test in cases)
        {
            var analysis = reader.ReadAsync(XpImageReader.LoadImage(Path.Combine(directory, test.File))).GetAwaiter().GetResult();
            var result = analysis.Result;
            var reading = result.Readings.FirstOrDefault(r => r.Class == XpClass.Assault);
            var passed = result.Screen == test.ExpectedScreen && result.Status == test.ExpectedStatus
                && (test.ExpectedXp is null || reading?.CumulativeXp == test.ExpectedXp && reading.NextThreshold == test.ExpectedThreshold)
                && (test.ExpectedScreen != ScreenKind.EndScore || result.Placement.PossiblePlaces.SequenceEqual(new[] { 2, 3 }));
            observations.Add(new { test.File, Passed = passed, Analysis = analysis });
            Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {test.File}: {result.Screen}/{result.Status} {reading?.CumulativeXp}/{reading?.NextThreshold}");
            if (!passed) failures.Add(test.File);
        }
        File.WriteAllText(Path.Combine(directory, "ocr-verification.json"), JsonSerializer.Serialize(observations, Json), new UTF8Encoding(false));
        if (failures.Count > 0) throw new InvalidDataException($"Mock OCR verification failed: {string.Join(", ", failures)}");
    }

    private static BitmapSource Unlock(long current, bool completed = false)
    {
        return Render(dc =>
        {
            Background(dc, unlock: true);
            dc.DrawRectangle(Brush("#161819"), null, new(89, 113, 462, 887));
            Text(dc, "WARDOG LEVEL", 120, 148, 17, Gray);
            Text(dc, "84", 375, 230, 48, White);
            var names = Enum.GetNames<XpClass>();
            for (var i = 0; i < names.Length; i++)
            {
                var x = i % 2 == 0 ? 127 : 328;
                var y = 346 + i / 2 * 196;
                dc.DrawRectangle(Brush(i == 0 ? "#3D3620" : "#373C3F"), new Pen(i == 0 ? Gold : Gray, 3), new(x, y, 186, 182));
                Center(dc, i == 0 ? "A" : names[i][..1], x + 93, y + 47, 43, i == 0 ? Gold : White);
                Center(dc, "15", x + 93, y + 111, 28, Gray);
                Center(dc, names[i].ToUpperInvariant(), x + 93, y + 145, 21, Gray);
            }
            Text(dc, "WARDOG PROGRESSION", 592, 130, 38, Gray);
            var thresholds = completed ? new long[] { 28_750, 33_750, 37_500, 43_750, 54_750 }
                : new long[] { 64_500, 75_000, 84_750, 95_750, 111_500 };
            for (var i = 0; i < 5; i++)
            {
                var x = 645 + 227 * i;
                dc.DrawRectangle(Brush("#292D2F"), new Pen(Brush("#676B6C"), 1), new(x, 220, 221, 450));
                Text(dc, "LEVEL", x, 199, 14, Gray);
                Text(dc, ((completed ? 11 : 16) + i).ToString(Invariant), x + 69, 198, 16, Gray);
                var xp = completed ? $"{thresholds[i]} / {thresholds[i]} XP"
                    : i == 0 ? $"{current} / {thresholds[i]} XP" : $"{thresholds[i]} XP";
                if (completed) Center(dc, xp, x + 110.5, 231, 18, i == 0 ? White : Gray);
                else XpText(dc, xp, x + 110.5, 231, 18, i == 0 ? White : Gray);
                Center(dc, ((completed ? 11 : 16) + i).ToString(Invariant), x + 110.5, 295, 34, Gray);
                Center(dc, completed ? "UNLOCKED" : "LOCKED", x + 110.5, 640, 14, Gray);
            }
        });
    }

    private static BitmapSource Score() => Render(dc =>
    {
        Background(dc, unlock: false);
        Center(dc, "DEFEAT", 960, 115, 108, White);
        dc.DrawRectangle(Brush("#15191C"), new Pen(Gray, 1), new(500, 330, 920, 450));
        for (var i = 0; i < 3; i++)
        {
            dc.DrawRectangle(Brush("#464B50"), null, new(555, 385 + i * 65, 795, 28));
            dc.DrawRectangle(Brush(i == 0 ? "#64B7E9" : i == 1 ? "#F15A43" : "#64C87D"), null, new(555, 385 + i * 65, i == 0 ? 795 : 20, 28));
        }
        Text(dc, "CASH EARNED", 555, 635, 24, Gray);
        Text(dc, "$12,500", 1125, 635, 24, Gold);
        Center(dc, "NEXT ROUND IN 00:30", 960, 925, 24, White);
    });

    private static BitmapSource Progression() => Render(dc =>
    {
        Background(dc, unlock: false);
        Center(dc, "DEFEAT", 960, 115, 108, White);
        dc.DrawRectangle(Brush("#202426"), new Pen(Gray, 1), new(790, 345, 340, 430));
        Center(dc, "A", 960, 410, 76, White);
        Center(dc, "15", 960, 540, 38, Gray);
        Center(dc, "ASSAULT", 960, 600, 30, Gray);
        dc.DrawRectangle(Brush("#3E381E"), new Pen(Gold, 1), new(877, 650, 166, 28));
        XpText(dc, "XP 62986 64500", 960, 652, 18, Gold);
        Center(dc, "NEXT ROUND IN 00:30", 960, 925, 24, White);
    });

    private static void Background(DrawingContext dc, bool unlock)
    {
        dc.DrawRectangle(Brush("#303638"), null, new(0, 0, Width, Height));
        dc.DrawRectangle(Brush("#101214"), null, new(0, 0, Width, 63));
        Text(dc, unlock ? "PLAY" : "SCORE", 175, 20, 18, Gray);
        Text(dc, unlock ? "UNLOCK" : "PROGRESSION", 320, 20, 18, Gold);
        Text(dc, unlock ? "CUSTOMIZE" : "SCOREBOARD", 535, 20, 18, Gray);
        Text(dc, "MOCK DATA — NOT A REAL MATCH", 600, 83, 20, Gold);
        Text(dc, "Generated OCR fixture · use only with isolated test data", 620, 1045, 18, Gray);
    }

    private static BitmapSource Render(Action<DrawingContext> draw)
    {
        var visual = new DrawingVisual();
        TextOptions.SetTextFormattingMode(visual, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Grayscale);
        using (var dc = visual.RenderOpen()) draw(dc);
        var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static Brush Brush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    private static FormattedText Format(string text, double size, Brush brush) => new(text, Invariant,
        FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, 1);
    private static void Text(DrawingContext dc, string text, double x, double y, double size, Brush brush)
        => dc.DrawText(Format(text, size, brush), new(x, y));
    private static void Center(DrawingContext dc, string text, double x, double y, double size, Brush brush)
    {
        var formatted = Format(text, size, brush);
        dc.DrawText(formatted, new(x - formatted.Width / 2, y));
    }
    private static void XpText(DrawingContext dc, string text, double x, double y, double size, Brush brush)
    {
        var formatted = new FormattedText(text, Invariant, FlowDirection.LeftToRight, new Typeface("Consolas"), size, brush, 1);
        dc.DrawText(formatted, new(x - formatted.Width / 2, y));
    }
    private static void Save(string directory, string file, BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var encoded = new MemoryStream();
        encoder.Save(encoded);
        var png = encoded.ToArray();
        using var stream = new FileStream(Path.Combine(directory, file), FileMode.CreateNew);
        stream.Write(png, 0, 8);
        for (var offset = 8; offset < png.Length;)
        {
            var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            var type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type is "IHDR" or "PLTE" or "tRNS" or "IDAT" or "IEND")
                stream.Write(png, offset, length + 12);
            offset += length + 12;
            if (type == "IEND") break;
        }
    }

    private sealed record MockCase(string File, ScreenKind ExpectedScreen, ReadStatus ExpectedStatus,
        long? ExpectedXp, long? ExpectedThreshold, string Action, long? ExpectedGain, bool? ExpectedTargetReached);

    private const string Guide = """
        # Analytics mock pack

        These are generated test fixtures. They do not describe real matches.

        1. Use an isolated test profile. Start a New Session.
        2. Use image 01 as the Assault baseline, then images 02–09 as current XP checks.
        3. Check the transitions at 500, 1,000, 1,500, 2,000, and exactly 2,500 earned XP.
        4. Image 10 must reject a completed reward page. Image 11 supplies DEFEAT without an exact placement.
        5. Image 12 supplies cumulative final XP of 62,986: a 6,500 increase INCLUDING the bonus. Confirm it separately.

        The app's Diagnostics Lab imports images for diagnosis only. It does not change live tracking.
        Use the app's explicit testing flow to exercise the session actions above.

        `manifest.json` records exact values and expected reader results. `ocr-verification.json` records real OCR verification when requested.
        `demo-analytics-season.md` contains 11 generated session examples. Keep it separate from personal save files.
        The season covers exact placements, DEFEAT uncertainty, unknown completion, missed bonuses, partial coverage, inconsistent rules, pending finals, and a rebase.
        """;
}
