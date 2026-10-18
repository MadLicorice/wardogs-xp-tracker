namespace WardogsXpTracker.Core.Reading;

public sealed record XpDigitObservation(string Text, double? Confidence);

public static class XpDigitConfidence
{
    /// <summary>Requires a confidence for every recognized digit, in the same order.</summary>
    public static double? Minimum(string recognizedText, IEnumerable<XpDigitObservation> symbols)
    {
        ArgumentNullException.ThrowIfNull(recognizedText);
        ArgumentNullException.ThrowIfNull(symbols);
        var expected = recognizedText.Where(char.IsAsciiDigit).ToArray();
        if (expected.Length == 0) return null;
        var actual = new List<char>();
        var minimum = 1d;
        foreach (var symbol in symbols)
        {
            if (symbol.Text is null) return null;
            if (!symbol.Text.Any(char.IsAsciiDigit)) continue;
            if (symbol.Text.Length != 1 || !char.IsAsciiDigit(symbol.Text[0])) return null;
            if (symbol.Confidence is not { } confidence || !double.IsFinite(confidence)
                || confidence is < 0 or > 1) return null;
            actual.Add(symbol.Text[0]);
            minimum = Math.Min(minimum, confidence);
        }
        return expected.SequenceEqual(actual) ? minimum : null;
    }
}
