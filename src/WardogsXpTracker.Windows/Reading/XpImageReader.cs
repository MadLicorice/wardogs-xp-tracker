using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tesseract;
using WardogsXpTracker.Core.Reading;

namespace WardogsXpTracker.Windows.Reading;

public sealed record ImageReadAnalysis(ScreenReadResult Result, XpClass? SelectedClass,
    double ElapsedMilliseconds, IReadOnlyList<string> Diagnostics);

/// <summary>Reads local pixels. This service never changes a session or a baseline.</summary>
public sealed class XpImageReader : IDisposable
{
    private delegate List<OcrLine> RecognizeRegion(NormalizedRect region, int scale, int? threshold,
        PageSegMode mode, string? whitelist, bool linearInterpolation = false);
    private const int OcrBorder = 12;
    private readonly SemaphoreSlim gate = new(1, 1);
    private TesseractEngine? engine;
    private bool disposed;

    public static BitmapSource LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return ToBgra32(decoder.Frames[0]);
    }

    public static BitmapSource ToBgra32(BitmapSource source)
    {
        if (source.PixelWidth < 16 || source.PixelHeight < 16 || (long)source.PixelWidth * source.PixelHeight > 64_000_000)
            throw new InvalidDataException("Choose an image between 16 pixels and 64 megapixels.");
        var image = source.Format == PixelFormats.Bgra32 ? source.Clone() : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        image.Freeze();
        return image;
    }

    public async Task<ImageReadAnalysis> ReadAsync(BitmapSource source, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var image = ToBgra32(source);
        await gate.WaitAsync(cancellationToken);
        try { return await Task.Run(() => Read(image, cancellationToken), cancellationToken); }
        finally { gate.Release(); }
    }

    private ImageReadAnalysis Read(BitmapSource image, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var assets = OcrAssets.EnsureInstalled();
        engine ??= new TesseractEngine(assets.TessdataDirectory, "eng", EngineMode.LstmOnly);
        var diagnostics = new List<string>();
        var digitConfidences = new Dictionary<List<OcrLine>, double?>();
        var now = DateTimeOffset.UtcNow;

        double? DigitConfidence(List<OcrLine> lines) => digitConfidences.GetValueOrDefault(lines);

        List<OcrLine> Recognize(NormalizedRect region, int scale = 2, int? threshold = null,
            PageSegMode mode = PageSegMode.SingleLine, string? whitelist = null, bool linearInterpolation = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rect = PixelRect(image, region);
            var processed = Preprocess(image, rect, scale, threshold, linearInterpolation);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(processed));
            using var memory = new MemoryStream();
            encoder.Save(memory);
            using var pix = Pix.LoadFromMemory(memory.ToArray());
            engine.SetVariable("tessedit_char_whitelist", whitelist ?? "");
            engine.SetVariable("user_defined_dpi", "300");
            using var page = engine.Process(pix, mode);
            using var iterator = page.GetIterator();
            var groups = new Dictionary<(int, int, int, int), List<OcrWord>>();
            iterator.Begin();
            do
            {
                var text = iterator.GetText(PageIteratorLevel.Word)?.Trim();
                if (string.IsNullOrWhiteSpace(text) || !iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var box)
                    || !iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var lineBox)) continue;
                var bounds = new NormalizedRect((rect.X + (box.X1 - OcrBorder) / (double)scale) / image.PixelWidth,
                    (rect.Y + (box.Y1 - OcrBorder) / (double)scale) / image.PixelHeight,
                    box.Width / (double)scale / image.PixelWidth, box.Height / (double)scale / image.PixelHeight);
                var key = (lineBox.X1, lineBox.Y1, lineBox.X2, lineBox.Y2);
                if (!groups.TryGetValue(key, out var words)) groups[key] = words = [];
                words.Add(new(text, bounds, iterator.GetConfidence(PageIteratorLevel.Word) / 100d));
            } while (iterator.Next(PageIteratorLevel.Word));
            var lines = groups.Values.Select(words => new OcrLine(string.Join(" ", words.Select(w => w.Text)),
                NormalizedRect.Union(words.Select(w => w.Bounds)), words, words.Average(w => w.Confidence ?? 0))).ToList();
            var rawPage = page.GetText().TrimEnd('\r', '\n');
            var observed = string.Join(" | ", lines.Select(line => $"{JsonSerializer.Serialize(line.Text)} confidence={line.Confidence:F3} words=[{string.Join(", ", (line.Words ?? []).Select(word => $"{JsonSerializer.Serialize(word.Text)}:{word.Confidence:F3}"))}]"));
            diagnostics.Add($"region={region.X:F6},{region.Y:F6},{region.Width:F6},{region.Height:F6} pixels={rect.X},{rect.Y},{rect.Width},{rect.Height} scale={scale} interpolation={(linearInterpolation ? "bilinear" : "bicubic")} threshold={threshold?.ToString(CultureInfo.InvariantCulture) ?? "gray"} mode={mode} whitelist={JsonSerializer.Serialize(whitelist ?? "")} raw={JsonSerializer.Serialize(rawPage)} meanConfidence={page.GetMeanConfidence():F3} lines={observed}");
            if (linearInterpolation)
            {
                var symbols = new List<(string Text, double? Confidence, NormalizedRect Bounds)>();
                using var symbolIterator = page.GetIterator();
                symbolIterator.Begin();
                do
                {
                    var text = symbolIterator.GetText(PageIteratorLevel.Symbol);
                    if (string.IsNullOrEmpty(text)) continue;
                    var hasBounds = symbolIterator.TryGetBoundingBox(PageIteratorLevel.Symbol, out var box);
                    var bounds = hasBounds ? new NormalizedRect(
                        (rect.X + (box.X1 - OcrBorder) / (double)scale) / image.PixelWidth,
                        (rect.Y + (box.Y1 - OcrBorder) / (double)scale) / image.PixelHeight,
                        box.Width / (double)scale / image.PixelWidth, box.Height / (double)scale / image.PixelHeight) : default;
                    symbols.Add((text, hasBounds ? symbolIterator.GetConfidence(PageIteratorLevel.Symbol) / 100d : null, bounds));
                } while (symbolIterator.Next(PageIteratorLevel.Symbol));
                var sourceText = string.Join(" ", lines.Select(line => line.Text));
                var confidence = XpDigitConfidence.Minimum(sourceText,
                    symbols.Select(symbol => new XpDigitObservation(symbol.Text, symbol.Confidence)));
                digitConfidences[lines] = confidence;
                diagnostics.Add($"XP numeric-symbol minimum={confidence?.ToString("F3", CultureInfo.InvariantCulture) ?? "unavailable"} attributionRaw={JsonSerializer.Serialize(sourceText)} symbols=[{string.Join(", ", symbols.Select(symbol => $"{JsonSerializer.Serialize(symbol.Text)}:{symbol.Confidence?.ToString("F3", CultureInfo.InvariantCulture) ?? "missing"}@{symbol.Bounds.X:F6},{symbol.Bounds.Y:F6},{symbol.Bounds.Width:F6},{symbol.Bounds.Height:F6}"))}]");
            }
            return lines;
        }

        // Check the supported UNLOCK layout before examining any reward numbers.
        var header = Recognize(new(.30, .105, .43, .065));
        var nav = Recognize(new(.08, 0, .44, .075), 2, null, PageSegMode.SparseText);
        var unlockHeader = string.Join(" ", header.Select(l => l.Text));
        var unlockNav = string.Join(" ", nav.Select(l => l.Text));
        if (unlockHeader.Contains("PROGRESSION", StringComparison.OrdinalIgnoreCase)
            && (unlockHeader.Contains("WARDOG", StringComparison.OrdinalIgnoreCase) || unlockNav.Contains("UNLOCK", StringComparison.OrdinalIgnoreCase)))
        {
            var lines = header.Concat(nav).ToList();
            var selected = FindSelectedClass(image, diagnostics);
            int? currentLevel = null;
            foreach (var track in Enum.GetValues<XpClass>())
            {
                var tile = ClassTile(track);
                lines.AddRange(Recognize(new(tile.X + .008, tile.Y + .135, tile.Width - .016, .024), 3));
                if (track != selected) continue;
                var levelRegion = new NormalizedRect(tile.CenterX - 39d / 1920, tile.Y + 112d / 1080,
                    78d / 1920, 32d / 1080);
                var level = ReadLevel(image, levelRegion, $"{track} sidebar", Recognize, diagnostics);
                if (level is null) continue;
                currentLevel = level.Value;
                lines.Add(new(level.Value.ToString(CultureInfo.InvariantCulture), level.Bounds));
            }
            var unstable = false;
            for (var column = 0; column < 5; column++)
            {
                var left = (645d + 227 * column) / 1920;
                var columnLevel = ReadLevel(image, new(left + 62d / 1920, 198d / 1080, 58d / 1920, 20d / 1080),
                    $"Progression column {column + 1}", Recognize, diagnostics);
                if (columnLevel is not null)
                    lines.Add(new($"LEVEL {columnLevel.Value}", columnLevel.Bounds));
                var region = new NormalizedRect(left, 231d / 1080, 221d / 1920, 22d / 1080);
                var consensus = ReadPair(image, region, false, Recognize, DigitConfidence, diagnostics);
                if (consensus.Line is not null) lines.Add(consensus.Line);
                unstable |= consensus.Disagreed;
            }
            var result = XpScreenReader.Read(new(lines, now), new(SelectedClass: selected, CurrentLevel: currentLevel, MinimumConfidence: 0));
            if (unstable && (result.IsAccepted || result.Status == ReadStatus.NoCurrentXp))
                result = result with { Status = ReadStatus.Ambiguous, Readings = [], Message = "XP could not be confirmed. Hold this page still and read again." };
            return new(result, selected, watch.Elapsed.TotalMilliseconds, diagnostics);
        }

        // Broad text establishes card identities and end-screen context. Digits receive separate tight passes.
        var broadScale = image.PixelWidth < 1000 ? 2 : 1;
        var broad = Recognize(new(0, 0, 1, 1), broadScale, null, PageSegMode.SparseText);
        var wordsAll = broad.SelectMany(l => l.Words ?? []).ToArray();
        var classNames = Enum.GetNames<XpClass>();
        var labels = wordsAll.Where(w => classNames.Contains(w.Text, StringComparer.OrdinalIgnoreCase)).ToArray();
        var numericLines = new List<OcrLine>();
        var levelRegions = new List<NormalizedRect>();
        var anyDisagreement = false;
        foreach (var label in labels)
        {
            if (label.Bounds.Y < .2 || label.Bounds.Y > .80) continue;
            var labelHeight = label.Bounds.Height;
            var levelWidth = labelHeight * image.PixelHeight / image.PixelWidth * 3;
            var levelRegion = Clamp(new(label.Bounds.CenterX - levelWidth / 2, label.Bounds.Y - labelHeight * 3,
                levelWidth, labelHeight * 2));
            levelRegions.Add(levelRegion);
            var cardLevel = ReadLevel(image, levelRegion, $"{label.Text} final card", Recognize, diagnostics);
            if (cardLevel is not null)
                numericLines.Add(new(cardLevel.Value.ToString(CultureInfo.InvariantCulture), cardLevel.Bounds));
            var region = FindCardXpBox(image, label.Bounds);
            if (region is null) continue;
            var consensus = ReadPair(image, region.Value, true, Recognize, DigitConfidence, diagnostics);
            if (consensus.Line is not null) numericLines.Add(consensus.Line);
            anyDisagreement |= consensus.Disagreed;
        }
        // Do not let the unvalidated broad OCR digits become accepted XP pairs.
        var context = new List<OcrLine>();
        foreach (var line in broad.Where(line => XpNumberParser.FindPairs(line.Text).Count == 0))
        {
            var words = (line.Words ?? []).Where(word => !int.TryParse(word.Text, out _)
                || !levelRegions.Any(region => word.Bounds.CenterX >= region.X && word.Bounds.CenterX <= region.Right
                    && word.Bounds.CenterY >= region.Y && word.Bounds.CenterY <= region.Bottom)).ToArray();
            if (words.Length > 0)
                context.Add(new(string.Join(" ", words.Select(word => word.Text)), NormalizedRect.Union(words.Select(word => word.Bounds)), words));
        }
        if (broad.Any(line => Regex.IsMatch(line.Text.Trim(), "^(VICTORY|DEFEAT)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
        {
            var contributionLines = new List<(int Place, OcrLine Line)>();
            foreach (var pass in new (int Scale, int? Threshold)[] { (3, null), (4, null), (4, 60), (4, 70), (4, 80), (4, 90) })
            foreach (var line in Recognize(new(.17, .52, .58, .20), pass.Scale, pass.Threshold, PageSegMode.SparseText))
            {
                var match = Regex.Match(line.Text, @"\b(?<place>[123])(?:ST|ND|RD|N0)\s+PLACE\s*\+?\s*CONTRIBUTION\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    var place = int.Parse(match.Groups["place"].Value, CultureInfo.InvariantCulture);
                    var ordinal = place switch { 1 => "1ST", 2 => "2ND", _ => "3RD" };
                    // The rank digit supplies the place. An OCR zero in the ordinal suffix does not change that digit.
                    contributionLines.Add((place, new($"{ordinal} PLACE + CONTRIBUTION", line.Bounds, Confidence: line.Confidence)));
                }
            }
            var votes = contributionLines.GroupBy(item => item.Place).OrderByDescending(group => group.Count()).ToArray();
            if (votes.Length > 0 && votes[0].Count() >= 2 && (votes.Length == 1 || votes[0].Count() > votes[1].Count()))
                context.Add(votes[0].First().Line);
        }
        context.AddRange(numericLines);
        var finalResult = XpScreenReader.Read(new(context, now), new(MinimumConfidence: 0));
        if (anyDisagreement && finalResult.Readings.Count > 0)
            finalResult = finalResult with { Status = ReadStatus.Ambiguous, Readings = [], Message = "The final XP passes disagree. Read the progression page again." };
        return new(finalResult, null, watch.Elapsed.TotalMilliseconds, diagnostics);
    }

    private sealed record LevelRead(int Value, NormalizedRect Bounds);

    private static LevelRead? ReadLevel(BitmapSource image, NormalizedRect region, string name,
        RecognizeRegion recognize, List<string> diagnostics)
    {
        var glyphs = FindLevelGlyphs(image, region);
        if (glyphs is null)
        {
            diagnostics.Add($"{name}: level glyphs are unclear. No level metadata accepted.");
            return null;
        }
        var values = new List<int>();
        foreach (var pass in new (int Scale, int? Threshold)[] { (3, null), (4, null), (3, 100), (3, 140), (4, 120) })
        {
            var raw = string.Concat(recognize(glyphs.Value.Bounds, pass.Scale, pass.Threshold,
                PageSegMode.SingleLine, "0123456789").Select(line => line.Text)).Replace(" ", "", StringComparison.Ordinal);
            if (raw.Length == glyphs.Value.Count && raw.All(char.IsAsciiDigit)
                && int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var level) && level is >= 0 and <= 999)
                values.Add(level);
        }
        var votes = values.GroupBy(value => value).OrderByDescending(group => group.Count()).ToArray();
        if (votes.Length == 0 || votes[0].Count() < 3 || (votes.Length > 1 && votes[0].Count() == votes[1].Count()))
        {
            diagnostics.Add($"{name}: no level consensus matching {glyphs.Value.Count} visible digit(s). No level metadata accepted.");
            return null;
        }
        diagnostics.Add($"{name}: level {votes[0].Key}, agreement {votes[0].Count()}/5, visible digits={glyphs.Value.Count}.");
        return new(votes[0].Key, glyphs.Value.Bounds);
    }

    private static (NormalizedRect Bounds, int Count)? FindLevelGlyphs(BitmapSource image, NormalizedRect region)
    {
        var rect = PixelRect(image, region);
        var input = new byte[rect.Width * rect.Height * 4];
        image.CopyPixels(rect, input, rect.Width * 4, 0);
        var luminance = new int[rect.Width * rect.Height];
        for (var i = 0; i < luminance.Length; i++)
            luminance[i] = (input[i * 4 + 2] * 299 + input[i * 4 + 1] * 587 + input[i * 4] * 114) / 1000;
        var minimum = luminance.Min(); var maximum = luminance.Max();
        if (maximum - minimum < 25) return null;
        var limit = (minimum + maximum) / 2;
        var seen = new bool[luminance.Length];
        var components = new List<Int32Rect>();
        var pending = new Stack<int>();
        for (var i = 0; i < luminance.Length; i++)
        {
            if (seen[i] || luminance[i] < limit) continue;
            seen[i] = true; pending.Push(i);
            var left = rect.Width; var top = rect.Height; var right = 0; var bottom = 0; var area = 0;
            while (pending.TryPop(out var pixel))
            {
                var x = pixel % rect.Width; var y = pixel / rect.Width;
                left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y); area++;
                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = x + dx; var ny = y + dy;
                    if (nx < 0 || nx >= rect.Width || ny < 0 || ny >= rect.Height) continue;
                    var next = ny * rect.Width + nx;
                    if (seen[next] || luminance[next] < limit) continue;
                    seen[next] = true; pending.Push(next);
                }
            }
            if (bottom - top + 1 < Math.Max(5, rect.Height * .4) || area < 8) continue;
            if (left == 0 || top == 0 || right == rect.Width - 1 || bottom == rect.Height - 1)
                return null;
            components.Add(new(left, top, right - left + 1, bottom - top + 1));
        }
        if (components.Count is < 1 or > 3) return null;
        var minX = Math.Max(0, components.Min(component => component.X) - 2);
        var minY = Math.Max(0, components.Min(component => component.Y) - 2);
        var maxX = Math.Min(rect.Width, components.Max(component => component.X + component.Width) + 2);
        var maxY = Math.Min(rect.Height, components.Max(component => component.Y + component.Height) + 2);
        return (new((rect.X + minX) / (double)image.PixelWidth, (rect.Y + minY) / (double)image.PixelHeight,
            (maxX - minX) / (double)image.PixelWidth, (maxY - minY) / (double)image.PixelHeight), components.Count);
    }

    private static (OcrLine? Line, bool Disagreed) ReadPair(BitmapSource image, NormalizedRect region, bool card,
        RecognizeRegion recognize, Func<List<OcrLine>, double?> digitConfidence, List<string> diagnostics)
    {
        // The smaller gold-card font needs its existing two-pixel margin. UNLOCK text needs one pixel.
        var textBounds = FindXpTextBounds(image, region, out var clipped, card ? 2 : 1);
        if (textBounds is null)
        {
            diagnostics.Add(clipped ? "XP text touches the source crop edge. No clipped XP accepted."
                : "The XP text could not be isolated from its background.");
            return (null, clipped);
        }
        region = textBounds.Value;
        var observations = new List<XpConsensusObservation>();
        // Convex interpolation preserves thin strokes. Fixed binarization can erase them before OCR.
        var scales = new[] { 2, 3, 4, 5 };
        foreach (var scale in scales)
        {
            var passId = $"bilinear;scale={scale}";
            var recognized = recognize(region, scale, null, PageSegMode.SingleLine, "0123456789,/XP ", true);
            var raw = string.Join(" ", recognized.Select(line => line.Text));
            // Use every source digit, including both fields, before card-prefix parsing changes any offsets.
            var confidence = digitConfidence(recognized);
            if (card && raw.Contains('/'))
            {
                observations.Add(new(passId, null));
                continue;
            }
            if (card && raw.IndexOf("XP", StringComparison.OrdinalIgnoreCase) is var prefix && prefix >= 0)
                raw = raw[prefix..];
            var candidates = XpNumberParser.FindPairs(raw);
            if (candidates.Count == 0 && card) candidates = XpNumberParser.FindPairs("XP " + raw);
            if (candidates.Count == 0)
                observations.Add(new(passId, null, confidence));
            else
                foreach (var candidate in candidates)
                    observations.Add(new(passId, candidate, confidence));
        }
        var consensus = XpReadingConsensus.Resolve(observations);
        if (!consensus.IsConfirmed || consensus.Pair is null)
        {
            diagnostics.Add(consensus.HasConflict ? "The XP passes contain contradictory values. No XP pair accepted."
                : "No two distinct preprocessing passes established this XP pair.");
            return (null, consensus.HasConflict || consensus.AgreeingPasses > 0);
        }
        var winner = consensus.Pair;
        diagnostics.Add($"Agreement {consensus.AgreeingPasses}/{scales.Length}, no contradictory readable values: {winner.Current}/{winner.Threshold}");
        // Confidence is not a probability. Only the consensus value enters the semantic reader.
        return (new($"{winner.Current.ToString(CultureInfo.InvariantCulture)}/{winner.Threshold.ToString(CultureInfo.InvariantCulture)}XP", region), false);
    }

    private static NormalizedRect? FindXpTextBounds(BitmapSource image, NormalizedRect region, out bool clipped, int margin = 2)
    {
        clipped = false;
        var rect = PixelRect(image, region);
        var input = new byte[rect.Width * rect.Height * 4];
        image.CopyPixels(rect, input, rect.Width * 4, 0);
        var gray = new int[rect.Width * rect.Height];
        for (var i = 0; i < gray.Length; i++)
            gray[i] = (input[i * 4 + 2] * 299 + input[i * 4 + 1] * 587 + input[i * 4] * 114) / 1000;
        var minimum = gray.Min(); var maximum = gray.Max();
        if (maximum - minimum < 25) return null;
        var limit = (minimum + maximum) / 2;
        var seen = new bool[gray.Length];
        var components = new List<Int32Rect>();
        var pending = new Stack<int>();
        for (var i = 0; i < gray.Length; i++)
        {
            if (seen[i] || gray[i] < limit) continue;
            seen[i] = true; pending.Push(i);
            var left = rect.Width; var top = rect.Height; var right = 0; var bottom = 0; var area = 0;
            while (pending.TryPop(out var pixel))
            {
                var x = pixel % rect.Width; var y = pixel / rect.Width;
                left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y); area++;
                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = x + dx; var ny = y + dy;
                    if (nx < 0 || nx >= rect.Width || ny < 0 || ny >= rect.Height) continue;
                    var next = ny * rect.Width + nx;
                    if (seen[next] || gray[next] < limit) continue;
                    seen[next] = true; pending.Push(next);
                }
            }
            var height = bottom - top + 1;
            // Text strokes are taller than background dots and shorter than the column border.
            if (height < Math.Max(5, rect.Height * .35) || height > rect.Height * .85 || area < 8) continue;
            if (left == 0 || top == 0 || right == rect.Width - 1 || bottom == rect.Height - 1)
            {
                clipped = true;
                return null;
            }
            components.Add(new(left, top, right - left + 1, height));
        }
        if (components.Count < 3) return null;
        var minX = Math.Max(0, components.Min(component => component.X) - margin);
        var minY = Math.Max(0, components.Min(component => component.Y) - margin);
        var maxX = Math.Min(rect.Width, components.Max(component => component.X + component.Width) + margin);
        var maxY = Math.Min(rect.Height, components.Max(component => component.Y + component.Height) + margin);
        return new((rect.X + minX) / (double)image.PixelWidth, (rect.Y + minY) / (double)image.PixelHeight,
            (maxX - minX) / (double)image.PixelWidth, (maxY - minY) / (double)image.PixelHeight);
    }

    private static NormalizedRect? FindCardXpBox(BitmapSource image, NormalizedRect label)
    {
        var h = label.Height * image.PixelHeight;
        var center = label.CenterX * image.PixelWidth;
        var left = Math.Max(0, (int)(center - h * 4.5));
        var right = Math.Min(image.PixelWidth, (int)(center + h * 4.5));
        var top = Math.Max(0, (int)(label.Bottom * image.PixelHeight + h * .4));
        var bottom = Math.Min(image.PixelHeight, (int)(label.Bottom * image.PixelHeight + h * 3.5));
        if (right <= left || bottom <= top) return null;
        var rect = new Int32Rect(left, top, right - left, bottom - top);
        var stride = rect.Width * 4;
        var pixels = new byte[stride * rect.Height];
        image.CopyPixels(rect, pixels, stride, 0);
        var runs = new List<(int Left, int Right, int Y)>();
        for (var y = 0; y < rect.Height; y++)
        {
            var start = -1;
            for (var x = 0; x <= rect.Width; x++)
            {
                var gold = false;
                if (x < rect.Width)
                {
                    var p = y * stride + x * 4;
                    var b = pixels[p]; var g = pixels[p + 1]; var r = pixels[p + 2];
                    gold = r > 140 && g > 95 && r > b * 1.3 && g > b * 1.2 && Math.Abs(r - g) < 130;
                }
                if (gold && start < 0) start = x;
                if (gold || start < 0) continue;
                if (x - start > h * 3.5 && x - start < h * 8 && left + start < center && left + x > center)
                    runs.Add((left + start, left + x - 1, top + y));
                start = -1;
            }
        }
        foreach (var first in runs)
        foreach (var last in runs)
        {
            if (last.Y - first.Y < h * .4 || last.Y - first.Y > h * 1.5
                || Math.Abs(first.Left - last.Left) > 3 || Math.Abs(first.Right - last.Right) > 3) continue;
            var x = Math.Max(first.Left, last.Left) + 1;
            var end = Math.Min(first.Right, last.Right);
            return new(x / (double)image.PixelWidth, (first.Y + 2d) / image.PixelHeight,
                (end - x) / (double)image.PixelWidth, (last.Y - first.Y - 3d) / image.PixelHeight);
        }
        return null;
    }

    private static XpClass? FindSelectedClass(BitmapSource image, List<string> diagnostics)
    {
        var scores = new List<(XpClass Class, double Score)>();
        foreach (var track in Enum.GetValues<XpClass>())
        {
            var rect = PixelRect(image, ClassTile(track));
            var stride = rect.Width * 4;
            var pixels = new byte[stride * rect.Height];
            image.CopyPixels(rect, pixels, stride, 0);
            var gold = 0; var count = 0;
            var border = Math.Max(2, image.PixelWidth / 640);
            for (var y = 0; y < rect.Height; y++)
            for (var x = 0; x < rect.Width; x++)
            {
                if (x >= border && x < rect.Width - border && y >= border && y < rect.Height - border) continue;
                var p = y * stride + x * 4; count++;
                var b = pixels[p]; var g = pixels[p + 1]; var r = pixels[p + 2];
                if (r > 100 && g > 70 && r > b * 1.45 && g > b * 1.35 && Math.Abs(r - g) < 125) gold++;
            }
            scores.Add((track, gold / (double)Math.Max(count, 1)));
        }
        var ordered = scores.OrderByDescending(s => s.Score).ToArray();
        diagnostics.Add("Selected-class border scores: " + string.Join(", ", ordered.Select(s => $"{s.Class}={s.Score:F3}")));
        return ordered[0].Score > .10 && ordered[0].Score > ordered[1].Score * 2 ? ordered[0].Class : null;
    }

    private static NormalizedRect ClassTile(XpClass track)
    {
        var index = (int)track;
        return new((index % 2 == 0 ? 127d : 328d) / 1920, (346d + (index / 2) * 196d) / 1080, 186d / 1920, 182d / 1080);
    }

    private static NormalizedRect Clamp(NormalizedRect r)
    {
        var x = Math.Clamp(r.X, 0, .999); var y = Math.Clamp(r.Y, 0, .999);
        return new(x, y, Math.Min(r.Width, 1 - x), Math.Min(r.Height, 1 - y));
    }

    private static Int32Rect PixelRect(BitmapSource image, NormalizedRect r)
    {
        r = Clamp(r);
        var x = Math.Clamp((int)Math.Round(r.X * image.PixelWidth), 0, image.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Round(r.Y * image.PixelHeight), 0, image.PixelHeight - 1);
        return new(x, y, Math.Clamp((int)Math.Round(r.Width * image.PixelWidth), 1, image.PixelWidth - x),
            Math.Clamp((int)Math.Round(r.Height * image.PixelHeight), 1, image.PixelHeight - y));
    }

    private static BitmapSource Preprocess(BitmapSource image, Int32Rect rect, int scale, int? threshold,
        bool linearInterpolation = false)
    {
        var input = new byte[rect.Width * rect.Height * 4];
        image.CopyPixels(rect, input, rect.Width * 4, 0);
        var width = rect.Width * scale + OcrBorder * 2; var height = rect.Height * scale + OcrBorder * 2;
        var gray = new byte[width * height];
        Array.Fill(gray, (byte)255);
        var original = new double[rect.Width * rect.Height];
        for (var i = 0; i < original.Length; i++)
        {
            var p = i * 4;
            original[i] = (input[p + 2] * 19595 + input[p + 1] * 38470 + input[p] * 7471 + 32768) >> 16;
        }
        for (var y = 0; y < rect.Height * scale; y++)
        for (var x = 0; x < rect.Width * scale; x++)
        {
            double value;
            if (scale == 1) value = original[y * rect.Width + x];
            else if (linearInterpolation)
            {
                var sx = (x + .5) / scale - .5; var sy = (y + .5) / scale - .5;
                var ix = (int)Math.Floor(sx); var iy = (int)Math.Floor(sy);
                var dx = sx - ix; var dy = sy - iy;
                var x0 = Math.Clamp(ix, 0, rect.Width - 1); var x1 = Math.Clamp(ix + 1, 0, rect.Width - 1);
                var y0 = Math.Clamp(iy, 0, rect.Height - 1); var y1 = Math.Clamp(iy + 1, 0, rect.Height - 1);
                value = (original[y0 * rect.Width + x0] * (1 - dx) + original[y0 * rect.Width + x1] * dx) * (1 - dy)
                    + (original[y1 * rect.Width + x0] * (1 - dx) + original[y1 * rect.Width + x1] * dx) * dy;
            }
            else
            {
                var sx = (x + .5) / scale - .5;
                var sy = (y + .5) / scale - .5;
                var ix = (int)Math.Floor(sx); var iy = (int)Math.Floor(sy);
                value = 0d; var total = 0d;
                for (var yy = iy - 1; yy <= iy + 2; yy++)
                for (var xx = ix - 1; xx <= ix + 2; xx++)
                {
                    var weight = Cubic(sx - xx) * Cubic(sy - yy);
                    value += original[Math.Clamp(yy, 0, rect.Height - 1) * rect.Width + Math.Clamp(xx, 0, rect.Width - 1)] * weight;
                    total += weight;
                }
                value /= total;
            }
            var luminance = Math.Clamp((int)Math.Round(value), 0, 255);
            gray[(y + OcrBorder) * width + x + OcrBorder] = threshold is { } limit
                ? (byte)(luminance >= limit ? 0 : 255) : (byte)(255 - luminance);
        }
        var result = BitmapSource.Create(width, height, 300, 300, PixelFormats.Gray8, null, gray, width);
        result.Freeze();
        return result;
    }

    private static double Cubic(double value)
    {
        value = Math.Abs(value);
        if (value < 1) return 1.5 * value * value * value - 2.5 * value * value + 1;
        return value < 2 ? -.5 * value * value * value + 2.5 * value * value - 4 * value + 2 : 0;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        engine?.Dispose();
        gate.Dispose();
    }
}
