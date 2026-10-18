using System.Globalization;
using System.Text.RegularExpressions;

namespace WardogsXpTracker.Core.Reading;

/// <summary>Reads exact integers. It never changes letters into guessed digits.</summary>
public static partial class XpNumberParser
{
    private const string Number = @"(?:\d{1,3}(?:,\d{3})+|\d+)";

    [GeneratedRegex(@"^(?:\d{1,3}(?:,\d{3})+|\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex IntegerPattern();

    [GeneratedRegex(@"(?<![\p{L}\d,.])(?:XP\s*)?(?<n>" + Number + @")\s*/\s*(?<d>" + Number
        + @")(?:\s*XP)?(?![\p{L}\d,.])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SlashPattern();

    [GeneratedRegex(@"\bXP\s*(?<n>" + Number + @")\s+(?<d>" + Number + @")(?![\p{L}\d,])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CardPattern();

    [GeneratedRegex(@"\bXP\s*(?<numbers>[\d,]+)(?![\p{L}\d,])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex JoinedCardPattern();

    public static bool TryParseInteger(string text, out long value)
    {
        value = 0;
        return IntegerPattern().IsMatch(text) && long.TryParse(text.Replace(",", "", StringComparison.Ordinal),
            NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    public static IReadOnlyList<XpNumberPair> FindPairs(string text)
    {
        var pairs = new List<XpNumberPair>();
        AddMatches(SlashPattern().Matches(text), text, pairs);
        AddMatches(CardPattern().Matches(text), text, pairs);
        foreach (Match match in JoinedCardPattern().Matches(text))
        {
            if (pairs.Any(p => p.Start < match.Index + match.Length && match.Index < p.Start + p.Length)) continue;
            var numbers = match.Groups["numbers"].Value;
            var options = new List<(long Current, long Threshold)>();
            for (var split = 1; split < numbers.Length; split++)
            {
                var left = numbers[..split];
                var right = numbers[split..];
                // Without separators, an adjacent pair has no unique digit boundary.
                if (!left.Contains(',') || !right.Contains(',')) continue;
                if (TryParseInteger(left, out var n) && TryParseInteger(right, out var d) && n <= d && d > 0)
                    options.Add((n, d));
            }
            if (options.Count == 1)
                pairs.Add(new(options[0].Current, options[0].Threshold, match.Index, match.Length, match.Value));
        }
        return pairs.OrderBy(p => p.Start).ToArray();
    }

    private static void AddMatches(MatchCollection matches, string text, List<XpNumberPair> result)
    {
        foreach (Match match in matches)
        {
            if (!TryParseInteger(match.Groups["n"].Value, out var n)
                || !TryParseInteger(match.Groups["d"].Value, out var d)) continue;
            if (result.Any(p => p.Start == match.Index && p.Length == match.Length)) continue;
            result.Add(new(n, d, match.Index, match.Length, text.Substring(match.Index, match.Length)));
        }
    }
}

public sealed record XpNumberPair(long Current, long Threshold, int Start, int Length, string RawText);
