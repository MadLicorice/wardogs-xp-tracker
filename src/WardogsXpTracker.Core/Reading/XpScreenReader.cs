using System.Text.RegularExpressions;

namespace WardogsXpTracker.Core.Reading;

/// <summary>Interprets one OCR frame. It does not change a baseline or session.</summary>
public static partial class XpScreenReader
{
    [GeneratedRegex(@"\b(ASSAULT|MEDIC|RECON|SUPPORT|DRIVER|PILOT)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClassPattern();
    [GeneratedRegex(@"\bLEVEL\s*(?<level>\d{1,3})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LevelPattern();
    [GeneratedRegex(@"\b(?<place>[123])(?:ST|ND|RD)\s+PLACE\s*\+?\s*CONTRIBUTION\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContributionPattern();
    [GeneratedRegex(@"\b(VICTORY|DEFEAT)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OutcomePattern();

    public static ScreenReadResult Read(OcrFrame frame, ReadRequest? request = null)
    {
        ArgumentNullException.ThrowIfNull(frame);
        request ??= new();
        if (request.MinimumConfidence is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(request), "Confidence must be between zero and one.");
        var lines = frame.Lines.Where(l => l.Bounds.IsValid).Select(l => new TextLine(l)).ToArray();
        var pairs = lines.SelectMany(line => XpNumberParser.FindPairs(line.Text)
            .Select(p => new Pair(p, line.BoundsFor(p.Start, p.Length), line.ConfidenceFor(p.Start, p.Length))))
            .ToArray();
        var labels = lines.SelectMany(line => ClassPattern().Matches(line.Text).Cast<Match>()
            .Select(m => new ClassLabel(Enum.Parse<XpClass>(m.Value, true), line.BoundsFor(m.Index, m.Length),
                line.ConfidenceFor(m.Index, m.Length)))).ToArray();

        var hasUnlockTab = lines.Any(l => l.Source.Bounds.Y < .15 && HasWord(l.Text, "UNLOCK"));
        var hasProgressionTitle = lines.Any(l => l.Source.Bounds.Y < .3
            && l.Text.Contains("WARDOG", StringComparison.OrdinalIgnoreCase)
            && l.Text.Contains("PROGRESSION", StringComparison.OrdinalIgnoreCase));
        var hasSidebar = labels.Count(l => l.Bounds.Right < .31) >= 2;
        var isUnlock = (hasUnlockTab && (hasProgressionTitle || hasSidebar)) || (hasProgressionTitle && hasSidebar);
        if (isUnlock) return ReadUnlock(frame, request, lines, pairs);

        var hasEndNavigation = lines.Any(l => l.Source.Bounds.Y < .12 && HasWord(l.Text, "PROGRESSION"))
            && lines.Any(l => l.Source.Bounds.Y < .12 && (HasWord(l.Text, "SCORE") || HasWord(l.Text, "SCOREBOARD")));
        var hasNextRound = lines.Any(l => l.Source.Bounds.Y > .65
            && l.Text.Contains("NEXT ROUND", StringComparison.OrdinalIgnoreCase));
        var cards = FindCards(labels, pairs);
        var horizontalCards = cards.Count >= 2
            && cards.Max(c => c.Label.Bounds.CenterY) - cards.Min(c => c.Label.Bounds.CenterY) < .08
            && cards.Max(c => c.Label.Bounds.CenterX) - cards.Min(c => c.Label.Bounds.CenterX) > .1;
        var placement = ReadPlacement(lines, request.MinimumConfidence);
        if ((hasEndNavigation || hasNextRound || horizontalCards) && cards.Count > 0)
            return ReadEndProgression(frame, request, lines, cards, placement);

        if ((hasEndNavigation || hasNextRound) && placement.Observations.Count > 0)
        {
            return Result(frame, ScreenKind.EndScore,
                placement.HasConflict ? ReadStatus.Ambiguous : ReadStatus.Accepted,
                placement.HasConflict ? "Placement cues conflict. Review the match result."
                    : "End screen detected. Placement evidence is separate from final XP.",
                placement: placement);
        }

        return Result(frame, ScreenKind.Unknown, ReadStatus.UnsupportedScreen,
            "Open UNLOCK and select a class, or show the match-end PROGRESSION tab.");
    }

    private static ScreenReadResult ReadUnlock(OcrFrame frame, ReadRequest request,
        TextLine[] lines, Pair[] allPairs)
    {
        if (request.IsAggregateTrackSelected || request.SelectedClass is null)
            return Result(frame, ScreenKind.Unlock, ReadStatus.ClassNotEstablished,
                "Select one class on the left. WARDOG level is not a class total.");
        if (request.ExpectedClass is { } expected && expected != request.SelectedClass)
            return Result(frame, ScreenKind.Unlock, ReadStatus.WrongClass,
                $"Open {expected} before you capture its XP.");

        var pairs = allPairs.Where(p => p.Bounds.X > .29 && p.Bounds.Y is > .13 and < .38).ToArray();
        var invalid = pairs.Any(p => p.Number.Threshold <= 0 || p.Number.Current > p.Number.Threshold);
        if (invalid)
            return Result(frame, ScreenKind.Unlock, ReadStatus.InvalidNumber,
                "The XP numbers do not fit their threshold. Capture the page again.");

        var thresholds = ExtractThresholds(lines, pairs);
        var partial = pairs.Where(p => p.Number.Current < p.Number.Threshold).ToArray();
        if (partial.Length == 0)
            return Result(frame, ScreenKind.Unlock, ReadStatus.NoCurrentXp,
                "Only completed rewards are visible. Show the next progression page and capture again.", thresholds: thresholds);

        var unique = partial.GroupBy(p => (p.Number.Current, p.Number.Threshold)).ToArray();
        if (unique.Length != 1)
            return Result(frame, ScreenKind.Unlock, ReadStatus.Ambiguous,
                "More than one current XP value is visible. Capture the page again.", thresholds: thresholds);
        var candidate = unique[0].OrderByDescending(p => p.Confidence ?? 1).First();
        if (IsLowConfidence(candidate.Confidence, request.MinimumConfidence))
            return Result(frame, ScreenKind.Unlock, ReadStatus.LowConfidence,
                "The XP digits are unclear. Capture the page again or enter XP manually.", thresholds: thresholds);

        var currentLevel = request.CurrentLevel ?? FindSidebarLevel(lines, request.SelectedClass.Value);
        var thresholdLevel = FindColumnLevel(lines, candidate.Bounds);
        if (currentLevel is { } current && thresholdLevel is { } target && target != current + 1)
            return Result(frame, ScreenKind.Unlock, ReadStatus.InconsistentLevel,
                "The class level and XP column do not agree. Show the current progression page.", thresholds: thresholds);
        var reading = new XpReading(request.SelectedClass.Value, candidate.Number.Current, candidate.Number.Threshold,
            currentLevel, thresholdLevel, ScreenKind.Unlock, frame.CapturedAt, candidate.Bounds,
            candidate.Number.RawText, candidate.Confidence);
        return Result(frame, ScreenKind.Unlock, ReadStatus.Accepted,
            $"Read {reading.Class}: {reading.CumulativeXp:N0} XP.", [reading], thresholds: thresholds);
    }

    private static ScreenReadResult ReadEndProgression(OcrFrame frame, ReadRequest request,
        TextLine[] lines, List<Card> cards, PlacementEvidence placement)
    {
        if (placement.HasConflict)
            return Result(frame, ScreenKind.EndProgression, ReadStatus.Ambiguous,
                "Placement cues conflict. Review the match result.", placement: placement);
        var readings = new List<XpReading>();
        foreach (var group in cards.GroupBy(c => c.Label.Class))
        {
            var values = group.Select(c => (c.Pair.Number.Current, c.Pair.Number.Threshold)).Distinct().ToArray();
            if (values.Length != 1)
                return Result(frame, ScreenKind.EndProgression, ReadStatus.Ambiguous,
                    $"Two totals disagree for {group.Key}. Capture the results again.", placement: placement);
            var card = group.First();
            if (card.Pair.Number.Threshold <= 0 || card.Pair.Number.Current > card.Pair.Number.Threshold)
                return Result(frame, ScreenKind.EndProgression, ReadStatus.InvalidNumber,
                    "A final XP total exceeds its threshold. Capture the results again.", placement: placement);
            var confidence = MinimumConfidence(card.Pair.Confidence, card.Label.Confidence);
            if (IsLowConfidence(confidence, request.MinimumConfidence))
                return Result(frame, ScreenKind.EndProgression, ReadStatus.LowConfidence,
                    $"The {group.Key} XP digits are unclear. Capture the results again.", placement: placement);
            var level = FindCardLevel(lines, card.Label.Bounds);
            readings.Add(new(group.Key, card.Pair.Number.Current, card.Pair.Number.Threshold, level,
                level is { } l && card.Pair.Number.Current < card.Pair.Number.Threshold ? l + 1 : null,
                ScreenKind.EndProgression, frame.CapturedAt,
                card.Pair.Bounds, card.Pair.Number.RawText, confidence));
        }
        if (request.ExpectedClass is { } expected && readings.All(r => r.Class != expected))
            return Result(frame, ScreenKind.EndProgression, ReadStatus.WrongClass,
                $"The visible results do not include {expected}.", placement: placement);
        return Result(frame, ScreenKind.EndProgression, ReadStatus.Accepted,
            $"Read {readings.Count} final class total(s). These totals can include match-end bonuses.",
            readings.OrderBy(r => r.Class).ToArray(), placement);
    }

    private static List<Card> FindCards(ClassLabel[] labels, Pair[] pairs)
    {
        var cards = new List<Card>();
        foreach (var label in labels)
        {
            // A card's XP sits below its class name in the same column.
            var candidates = pairs.Where(p => p.Bounds.CenterY > label.Bounds.CenterY
                && p.Bounds.Y - label.Bounds.Bottom < .18
                && Math.Abs(p.Bounds.CenterX - label.Bounds.CenterX) < Math.Max(.045, p.Bounds.Width * .55)).ToArray();
            foreach (var candidate in candidates) cards.Add(new(label, candidate));
        }
        return cards;
    }

    private static PlacementEvidence ReadPlacement(TextLine[] lines, double minimumConfidence)
    {
        var observations = new List<PlacementObservation>();
        foreach (var line in lines)
        {
            foreach (Match match in OutcomePattern().Matches(line.Text))
            {
                var bounds = line.BoundsFor(match.Index, match.Length);
                if (!line.Text.Trim().Equals(match.Value, StringComparison.OrdinalIgnoreCase)
                    || bounds.Y > .43 || bounds.CenterX < .22 || bounds.CenterX > .88
                    || IsLowConfidence(line.ConfidenceFor(match.Index, match.Length), minimumConfidence)) continue;
                var victory = match.Value.Equals("VICTORY", StringComparison.OrdinalIgnoreCase);
                observations.Add(new(victory ? PlacementCue.Victory : PlacementCue.Defeat,
                    victory ? [1] : [2, 3], bounds, match.Value));
            }
            foreach (Match match in ContributionPattern().Matches(line.Text))
            {
                var bounds = line.BoundsFor(match.Index, match.Length);
                if (bounds.Y is < .22 or > .88 || bounds.CenterX < .2 || bounds.CenterX > .88
                    || IsLowConfidence(line.ConfidenceFor(match.Index, match.Length), minimumConfidence)) continue;
                var place = int.Parse(match.Groups["place"].Value, System.Globalization.CultureInfo.InvariantCulture);
                var cue = place switch
                {
                    1 => PlacementCue.FirstContribution,
                    2 => PlacementCue.SecondContribution,
                    _ => PlacementCue.ThirdContribution
                };
                observations.Add(new(cue, [place], bounds, match.Value));
            }
        }
        if (observations.Count == 0) return PlacementEvidence.None;
        IEnumerable<int> possible = [1, 2, 3];
        foreach (var observation in observations) possible = possible.Intersect(observation.PossiblePlaces);
        var places = possible.Order().ToArray();
        return new(places, places.Length == 0, observations);
    }

    private static IReadOnlyList<ObservedLevelThreshold> ExtractThresholds(TextLine[] lines, Pair[] pairs)
        => pairs.Select(p => (Pair: p, Level: FindColumnLevel(lines, p.Bounds)))
            .Where(x => x.Level is not null && x.Pair.Number.Threshold > 0
                && x.Pair.Number.Current <= x.Pair.Number.Threshold)
            .Select(x => new ObservedLevelThreshold(x.Level!.Value, x.Pair.Number.Threshold,
                x.Pair.Bounds, x.Pair.Number.RawText)).DistinctBy(t => (t.Level, t.CumulativeXp)).ToArray();

    private static int? FindColumnLevel(TextLine[] lines, NormalizedRect pair)
    {
        var levels = lines.SelectMany(line => LevelPattern().Matches(line.Text).Cast<Match>()
            .Select(match => (Level: int.Parse(match.Groups["level"].Value, System.Globalization.CultureInfo.InvariantCulture),
                Bounds: line.BoundsFor(match.Index, match.Length))))
            .Where(l => l.Bounds.Bottom <= pair.Y + .015 && pair.Y - l.Bounds.Bottom < .07
                && Math.Abs(l.Bounds.CenterX - pair.CenterX) < Math.Max(.065, pair.Width * .6))
            .OrderBy(l => Math.Abs(l.Bounds.CenterX - pair.CenterX) + Math.Abs(l.Bounds.Bottom - pair.Y)).ToArray();
        return levels.Length > 0 ? levels[0].Level : null;
    }

    private static int? FindSidebarLevel(TextLine[] lines, XpClass selected)
    {
        var label = lines.SelectMany(line => ClassPattern().Matches(line.Text).Cast<Match>()
                .Where(m => m.Value.Equals(selected.ToString(), StringComparison.OrdinalIgnoreCase))
                .Select(m => line.BoundsFor(m.Index, m.Length)))
            .FirstOrDefault(b => b.Right < .31);
        return label.IsValid ? FindCardLevel(lines, label) : null;
    }

    private static int? FindCardLevel(TextLine[] lines, NormalizedRect label)
    {
        var candidates = lines.SelectMany(l => l.StandaloneNumbers())
            .Where(n => n.Bounds.Bottom <= label.Y + .01 && label.Y - n.Bounds.Bottom < .10
                && Math.Abs(n.Bounds.CenterX - label.CenterX) < .045)
            .OrderBy(n => label.Y - n.Bounds.Bottom).ToArray();
        return candidates.Length > 0 ? candidates[0].Value : null;
    }

    private static bool HasWord(string text, string word)
        => Regex.IsMatch(text, @"\b" + Regex.Escape(word) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static bool IsLowConfidence(double? confidence, double minimum)
        => confidence is { } value && (!double.IsFinite(value) || value < minimum || value > 1);
    private static double? MinimumConfidence(double? a, double? b)
        => a is null ? b : b is null ? a : Math.Min(a.Value, b.Value);
    private static ScreenReadResult Result(OcrFrame frame, ScreenKind screen, ReadStatus status, string message,
        IReadOnlyList<XpReading>? readings = null, PlacementEvidence? placement = null,
        IReadOnlyList<ObservedLevelThreshold>? thresholds = null)
        => new(screen, status, message, readings ?? [], placement ?? PlacementEvidence.None,
            thresholds ?? [], frame.Lines);

    private sealed record Pair(XpNumberPair Number, NormalizedRect Bounds, double? Confidence);
    private sealed record ClassLabel(XpClass Class, NormalizedRect Bounds, double? Confidence);
    private sealed record Card(ClassLabel Label, Pair Pair);

    private sealed class TextLine
    {
        public OcrLine Source { get; }
        public string Text { get; }
        private readonly List<(int Start, int Length, OcrWord Word)> _spans = [];
        public TextLine(OcrLine source)
        {
            Source = source;
            if (source.Words is { Count: > 0 })
            {
                var position = 0;
                foreach (var word in source.Words.Where(w => w.Bounds.IsValid))
                {
                    _spans.Add((position, word.Text.Length, word));
                    position += word.Text.Length + 1;
                }
                Text = string.Join(" ", _spans.Select(s => s.Word.Text));
            }
            else Text = source.Text;
        }

        public NormalizedRect BoundsFor(int start, int length)
        {
            var words = _spans.Where(s => s.Start < start + length && start < s.Start + s.Length).ToArray();
            if (words.Length > 0) return NormalizedRect.Union(words.Select(w => w.Word.Bounds));
            var unit = Source.Bounds.Width / Math.Max(1, Text.Length);
            return new(Source.Bounds.X + start * unit, Source.Bounds.Y, Math.Max(unit, length * unit), Source.Bounds.Height);
        }

        public double? ConfidenceFor(int start, int length)
        {
            var values = _spans.Where(s => s.Start < start + length && start < s.Start + s.Length)
                .Select(s => s.Word.Confidence).Append(Source.Confidence).Where(c => c is not null).ToArray();
            return values.Length > 0 ? values.Min() : null;
        }

        public IEnumerable<(int Value, NormalizedRect Bounds)> StandaloneNumbers()
        {
            if (_spans.Count > 0)
            {
                foreach (var span in _spans)
                    if (int.TryParse(span.Word.Text, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out var n) && n is >= 0 and <= 999)
                        yield return (n, span.Word.Bounds);
            }
            else if (int.TryParse(Text.Trim(), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var n) && n is >= 0 and <= 999)
                yield return (n, Source.Bounds);
        }
    }
}
