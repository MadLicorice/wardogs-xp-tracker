namespace WardogsXpTracker.Core.Reading;

public enum XpClass { Assault, Medic, Recon, Support, Driver, Pilot }
public enum ScreenKind { Unknown, Unlock, EndProgression, EndScore }
public enum ReadStatus
{
    Accepted, UnsupportedScreen, ClassNotEstablished, WrongClass, NoCurrentXp,
    Ambiguous, InvalidNumber, InconsistentLevel, LowConfidence
}

/// <summary>Coordinates relative to the captured image, from zero to one.</summary>
public readonly record struct NormalizedRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
    public bool IsValid => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Width)
        && double.IsFinite(Height) && X >= 0 && Y >= 0 && Width > 0 && Height > 0
        && Right <= 1.001 && Bottom <= 1.001;

    public static NormalizedRect Union(IEnumerable<NormalizedRect> source)
    {
        var rectangles = source.ToArray();
        if (rectangles.Length == 0) return default;
        var x = rectangles.Min(r => r.X);
        var y = rectangles.Min(r => r.Y);
        return new(x, y, rectangles.Max(r => r.Right) - x, rectangles.Max(r => r.Bottom) - y);
    }
}

public sealed record OcrWord(string Text, NormalizedRect Bounds, double? Confidence = null);
public sealed record OcrLine(string Text, NormalizedRect Bounds,
    IReadOnlyList<OcrWord>? Words = null, double? Confidence = null);
public sealed record OcrFrame(IReadOnlyList<OcrLine> Lines, DateTimeOffset CapturedAt);

/// <summary>Visual class selection must come from the same frame as the OCR text.</summary>
public sealed record ReadRequest(
    XpClass? SelectedClass = null,
    XpClass? ExpectedClass = null,
    int? CurrentLevel = null,
    bool IsAggregateTrackSelected = false,
    double MinimumConfidence = 0.75);

public sealed record XpReading(
    XpClass Class,
    long CumulativeXp,
    long NextThreshold,
    int? CurrentLevel,
    int? ThresholdLevel,
    ScreenKind Screen,
    DateTimeOffset CapturedAt,
    NormalizedRect Bounds,
    string RawText,
    double? Confidence);

public sealed record ObservedLevelThreshold(int Level, long CumulativeXp,
    NormalizedRect Bounds, string RawText);

public enum PlacementCue { Victory, Defeat, FirstContribution, SecondContribution, ThirdContribution }
public sealed record PlacementObservation(PlacementCue Cue, IReadOnlyList<int> PossiblePlaces,
    NormalizedRect Bounds, string RawText);
public sealed record PlacementEvidence(IReadOnlyList<int> PossiblePlaces,
    bool HasConflict, IReadOnlyList<PlacementObservation> Observations)
{
    public static PlacementEvidence None { get; } = new([], false, []);
    public bool IsKnown => PossiblePlaces.Count == 1 && !HasConflict;
}

public sealed record ScreenReadResult(
    ScreenKind Screen,
    ReadStatus Status,
    string Message,
    IReadOnlyList<XpReading> Readings,
    PlacementEvidence Placement,
    IReadOnlyList<ObservedLevelThreshold> Thresholds,
    IReadOnlyList<OcrLine> RawLines)
{
    public bool IsAccepted => Status == ReadStatus.Accepted;
    public bool IsEndScreen => Screen is ScreenKind.EndProgression or ScreenKind.EndScore;
}
