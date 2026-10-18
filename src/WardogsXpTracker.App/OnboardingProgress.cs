namespace WardogsXpTracker.App;

/// <summary>Stores explicit onboarding decisions without changing tracking or test preferences.</summary>
public static class OnboardingProgress
{
    public const int CurrentVersion = 1;

    public static bool ShouldShowAutomatically(TrackerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return preferences.OnboardingCompletedVersion < CurrentVersion
            && preferences.OnboardingDeferredVersion < CurrentVersion;
    }

    /// <summary>Records completion in memory. The caller saves the changed preferences.</summary>
    public static bool MarkCompleted(TrackerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var completed = Math.Max(CurrentVersion, preferences.OnboardingCompletedVersion);
        if (completed == preferences.OnboardingCompletedVersion) return false;
        preferences.OnboardingCompletedVersion = completed;
        return true;
    }

    /// <summary>Records a first-time skip or close without erasing any earlier completion.</summary>
    public static bool MarkDeferred(TrackerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (preferences.OnboardingCompletedVersion >= CurrentVersion) return false;
        var deferred = Math.Max(CurrentVersion, preferences.OnboardingDeferredVersion);
        if (deferred == preferences.OnboardingDeferredVersion) return false;
        preferences.OnboardingDeferredVersion = deferred;
        return true;
    }
}
