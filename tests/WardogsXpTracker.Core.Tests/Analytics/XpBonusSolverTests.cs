using WardogsXpTracker.Core.Analytics;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.Core.Tests.Analytics;

public sealed class XpBonusSolverTests
{
    [Theory]
    [InlineData(BonusOutcome.Third, 3500, 3000)]
    [InlineData(BonusOutcome.Second, 3000, 3500)]
    [InlineData(BonusOutcome.First, 2500, 4000)]
    [InlineData(BonusOutcome.NoBonus, 6500, 0)]
    public void KnownOutcomeSplitsTheRecordedTotal(BonusOutcome outcome, long earned, long bonus)
    {
        var result = XpBonusSolver.Solve(6500, new(), [outcome]);
        Assert.Equal(XpSplitStatus.Exact, result.Status);
        Assert.Equal(earned, result.MinimumEarnedXp);
        Assert.Equal(bonus, result.MinimumBonusXp);
        Assert.Equal(0, result.IndeterminateXp);
        Assert.Single(result.Solutions);
    }

    [Fact]
    public void DefeatPreservesOnlyTheCommonMinimums()
    {
        var result = XpBonusSolver.Solve(6500, new(), [BonusOutcome.Second, BonusOutcome.Third]);
        Assert.Equal(XpSplitStatus.Ambiguous, result.Status);
        Assert.Equal(3000, result.MinimumEarnedXp);
        Assert.Equal(3000, result.MinimumBonusXp);
        Assert.Equal(500, result.IndeterminateXp);
        Assert.Contains("second or third", result.Explanation);
    }

    [Fact]
    public void UnknownCompletionIncludesNoBonusWithoutInventingReceipt()
    {
        var result = XpBonusSolver.Solve(6500, new(), Enum.GetValues<BonusOutcome>());
        Assert.Equal(2500, result.MinimumEarnedXp);
        Assert.Equal(0, result.MinimumBonusXp);
        Assert.Equal(4000, result.IndeterminateXp);
        Assert.Contains("Completion is unknown", result.Explanation);
    }

    [Fact]
    public void ReliableEarnedObservationCanEliminateAnOtherwisePossiblePlacement()
    {
        var result = XpBonusSolver.Solve(6500, new(), [BonusOutcome.Second, BonusOutcome.Third], 3200);
        Assert.Equal(XpSplitStatus.Exact, result.Status);
        Assert.Equal(3500, result.MinimumEarnedXp);
        Assert.Equal(3000, result.MinimumBonusXp);
        Assert.Equal(BonusOutcome.Third, Assert.Single(result.Solutions).Outcome);
    }

    [Theory]
    [InlineData(499, 499, 0)]
    [InlineData(1100, 500, 600)]
    [InlineData(1599, 999, 600)]
    [InlineData(2200, 1000, 1200)]
    [InlineData(2699, 1499, 1200)]
    [InlineData(3300, 1500, 1800)]
    [InlineData(4400, 2000, 2400)]
    [InlineData(5500, 2500, 3000)]
    public void TierIntervalsUseExactIntegerBoundaries(long total, long earned, long bonus)
    {
        var result = XpBonusSolver.Solve(total, new(), [BonusOutcome.Third]);
        Assert.Equal(XpSplitStatus.Exact, result.Status);
        Assert.Equal(earned, result.MinimumEarnedXp);
        Assert.Equal(bonus, result.MinimumBonusXp);
    }

    [Fact]
    public void CustomThresholdsAndBonusesDoNotAssumeFiveHundredXpSteps()
    {
        var rules = new RuleSetSnapshot { Tiers = [new(0, 0, 0, 0), new(100, 23, 31, 41), new(250, 70, 80, 90)] };
        var result = XpBonusSolver.Solve(230, rules, [BonusOutcome.Second]);
        Assert.Equal(199, result.MinimumEarnedXp);
        Assert.Equal(31, result.MinimumBonusXp);
        Assert.Equal(100, Assert.Single(result.Solutions).TierMinimumXp);
    }

    [Fact]
    public void NonmonotonicRulesCanHaveMultipleSplitsForOnePlacement()
    {
        var rules = new RuleSetSnapshot { Tiers = [new(0, 0, 0, 0), new(100, 500, 500, 500), new(200, 100, 100, 100)] };
        var result = XpBonusSolver.Solve(650, rules, [BonusOutcome.Second]);
        Assert.Equal(XpSplitStatus.Ambiguous, result.Status);
        Assert.Equal(150, result.MinimumEarnedXp);
        Assert.Equal(100, result.MinimumBonusXp);
        Assert.Equal(400, result.IndeterminateXp);
        Assert.Equal(2, result.Solutions.Count);
    }

    [Fact]
    public void BelowTierOneCanBeExactWithoutAnExactPlacement()
    {
        var result = XpBonusSolver.Solve(499, new(), [BonusOutcome.First, BonusOutcome.Second, BonusOutcome.Third]);
        Assert.Equal(XpSplitStatus.Exact, result.Status);
        Assert.Equal(499, result.MinimumEarnedXp);
        Assert.Equal(0, result.MinimumBonusXp);
        Assert.Equal(3, result.Solutions.Count);
    }

    [Fact]
    public void RulesMismatchPreservesTheObservedTotalAsGray()
    {
        var result = XpBonusSolver.Solve(1000, new(), [BonusOutcome.Third]);
        Assert.Equal(XpSplitStatus.NoSolution, result.Status);
        Assert.Equal(1000, result.TotalGain);
        Assert.Equal(1000, result.IndeterminateXp);
        Assert.Equal(0, result.MinimumEarnedXp);
        Assert.Equal(0, result.MinimumBonusXp);
        Assert.Empty(result.Solutions);
    }

    [Fact]
    public void ConflictingOrInvalidOutcomesAreNotTreatedAsUnknown()
    {
        Assert.Equal(XpSplitStatus.ConflictingEvidence, XpBonusSolver.Solve(6500, new(), []).Status);
        Assert.Equal(XpSplitStatus.ConflictingEvidence, XpBonusSolver.Solve(6500, new(), [(BonusOutcome)99]).Status);
    }

    [Fact]
    public void InvalidTotalsRulesAndImpossibleBoundsHaveExplicitStatuses()
    {
        Assert.Equal(XpSplitStatus.InvalidTotal, XpBonusSolver.Solve(-1, new(), [BonusOutcome.NoBonus]).Status);
        Assert.Equal(XpSplitStatus.InvalidTotal, XpBonusSolver.Solve(1, new(), [BonusOutcome.NoBonus], -1).Status);
        Assert.Equal(XpSplitStatus.NoSolution, XpBonusSolver.Solve(6500, new(), [BonusOutcome.Third], 3501).Status);
        Assert.Equal(XpSplitStatus.InvalidRules, XpBonusSolver.Solve(6500, new() { Tiers = [] }, [BonusOutcome.Third]).Status);
    }

    [Fact]
    public void DuplicateOutcomeEvidenceDoesNotDuplicateSolutions()
    {
        var result = XpBonusSolver.Solve(6500, new(), [BonusOutcome.Third, BonusOutcome.Third]);
        Assert.Single(result.Solutions);
    }

    [Fact]
    public void LargeXpTotalsDoNotRequireIterationOrOverflow()
    {
        var result = XpBonusSolver.Solve(long.MaxValue, new(), [BonusOutcome.Third]);
        Assert.Equal(XpSplitStatus.Exact, result.Status);
        Assert.Equal(long.MaxValue - 3000, result.MinimumEarnedXp);
        Assert.Equal(3000, result.MinimumBonusXp);
        Assert.Equal(0, result.IndeterminateXp);
    }
}
