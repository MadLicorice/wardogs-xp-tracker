using System.IO;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Analytics;

/// <summary>Finds every integer split allowed by the recorded rules and outcome evidence.</summary>
public static class XpBonusSolver
{
    public static XpSplitResult Solve(long totalGain, RuleSetSnapshot rules,
        IEnumerable<BonusOutcome> possibleOutcomes, long minimumEarnedXp = 0)
    {
        if (totalGain < 0 || minimumEarnedXp < 0)
            return Unresolved(totalGain, XpSplitStatus.InvalidTotal, "The recorded XP total or earned-XP bound is negative.");
        try { TrackingDocumentValidator.ValidateRules(rules); }
        catch (InvalidDataException exception)
        { return Unresolved(totalGain, XpSplitStatus.InvalidRules, exception.Message); }

        var outcomes = possibleOutcomes?.Distinct().Order().ToArray() ?? [];
        if (outcomes.Length == 0 || outcomes.Any(outcome => !Enum.IsDefined(outcome)))
            return Unresolved(totalGain, XpSplitStatus.ConflictingEvidence, "No supported outcome remains after the recorded evidence.");

        var solutions = new List<BonusSplit>();
        foreach (var outcome in outcomes)
        {
            if (outcome == BonusOutcome.NoBonus)
            {
                if (totalGain >= minimumEarnedXp)
                    solutions.Add(new(outcome, totalGain, 0, rules.TierFor(totalGain).MinimumXp));
                continue;
            }
            for (var index = 0; index < rules.Tiers.Count; index++)
            {
                var tier = rules.Tiers[index];
                var bonus = BonusFor(tier, outcome);
                if (bonus > totalGain) continue;
                var earned = totalGain - bonus;
                if (earned < minimumEarnedXp || earned < tier.MinimumXp) continue;
                if (index + 1 < rules.Tiers.Count && earned >= rules.Tiers[index + 1].MinimumXp) continue;
                solutions.Add(new(outcome, earned, bonus, tier.MinimumXp));
            }
        }
        if (solutions.Count == 0)
            return Unresolved(totalGain, XpSplitStatus.NoSolution,
                "The observed total does not fit the bonus rules, outcome evidence, and recorded in-match XP.");

        var minimumEarned = solutions.Min(solution => solution.EarnedXp);
        var minimumBonus = solutions.Min(solution => solution.BonusXp);
        var gray = totalGain - minimumEarned - minimumBonus;
        var distinctSplits = solutions.Select(solution => (solution.EarnedXp, solution.BonusXp)).Distinct().Count();
        return new()
        {
            Status = distinctSplits == 1 ? XpSplitStatus.Exact : XpSplitStatus.Ambiguous,
            TotalGain = totalGain, MinimumEarnedXp = minimumEarned, MinimumBonusXp = minimumBonus,
            IndeterminateXp = gray, Solutions = solutions.AsReadOnly(),
            Explanation = Explain(outcomes, gray)
        };
    }

    internal static XpSplitResult Unresolved(long gain, XpSplitStatus status, string explanation) => new()
    {
        Status = status, TotalGain = gain, IndeterminateXp = Math.Max(0, gain), Explanation = explanation
    };

    internal static long BonusFor(BonusTier tier, BonusOutcome outcome) => outcome switch
    {
        BonusOutcome.Third => tier.Third, BonusOutcome.Second => tier.Second,
        BonusOutcome.First => tier.First, BonusOutcome.NoBonus => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    private static string Explain(BonusOutcome[] outcomes, long gray)
    {
        if (gray == 0) return "Calculated using bonus rules. Every supported split assigns the same amounts to both XP sources.";
        if (outcomes.Contains(BonusOutcome.NoBonus))
            return "Completion is unknown. This XP could be in-match XP or bonus XP, including a possible no-bonus outcome.";
        if (outcomes.Length == 2 && outcomes.Contains(BonusOutcome.Second) && outcomes.Contains(BonusOutcome.Third))
            return "This XP could be either in-match XP or bonus XP because second or third place was not recorded.";
        return "Calculated using bonus rules. This XP can move between in-match XP and bonus XP across the supported splits.";
    }
}
