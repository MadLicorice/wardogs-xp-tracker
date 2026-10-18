# Walkthrough state checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/WardogsXpTracker.App.OnboardingChecks
```

This console check links the production walkthrough model and references Core. It has no test-framework package, WPF window, screen capture, audio, network call, or save-file access. Failures exit with an exception and a named assertion. Success prints the assertion count.

The checks cover real capture evidence, explicit starting coverage for all enabled classes, bound season/session/class/baseline IDs, pending and confirmed finals, manual/mock/import exclusions, stale observations, JSON round trip, and tracker immutability. Pause regressions use the production SessionTracker API, including its final-observation deduplication.

These checks do not replace native UI tests. They test the walkthrough model rather than the production preferences class or startup dialogs. The separate native layout/onboarding checks cover those integration paths.
