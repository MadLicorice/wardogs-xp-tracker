namespace WardogsXpTracker.Core.Reading;

public sealed record XpConsensusObservation(string PassId, XpNumberPair? Pair, double? Confidence = null);
public sealed record XpConsensusResult(bool IsConfirmed, XpNumberPair? Pair, bool HasConflict, int AgreeingPasses);

/// <summary>Requires exact agreement. A conflicting number cannot lose a majority vote.</summary>
public static class XpReadingConsensus
{
    public static XpConsensusResult Resolve(IEnumerable<XpConsensusObservation> observations,
        int minimumPasses = 2, double minimumConfidence = 0.5)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (minimumPasses < 2) throw new ArgumentOutOfRangeException(nameof(minimumPasses));
        if (!double.IsFinite(minimumConfidence) || minimumConfidence is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(minimumConfidence));

        var evidence = observations.ToArray();
        if (evidence.Any(item => string.IsNullOrWhiteSpace(item.PassId)))
            throw new ArgumentException("Each OCR pass needs an identity.", nameof(observations));

        var readable = evidence.Where(item => item.Pair is { Current: >= 0, Threshold: > 0 } pair
            && pair.Current <= pair.Threshold).ToArray();
        var values = readable.GroupBy(item => (item.Pair!.Current, item.Pair.Threshold)).ToArray();

        // Examine every parsed value before confidence filtering. Low confidence cannot hide a conflict.
        if (values.Length > 1) return new(false, null, true, 0);
        if (values.Length == 0) return new(false, null, false, 0);

        var support = readable.Where(item => item.Confidence is { } confidence
                && double.IsFinite(confidence) && confidence >= minimumConfidence && confidence <= 1)
            .Select(item => item.PassId).Distinct(StringComparer.Ordinal).Count();
        return support >= minimumPasses
            ? new(true, readable[0].Pair, false, support)
            : new(false, null, false, support);
    }
}
