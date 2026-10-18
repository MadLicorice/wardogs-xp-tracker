# Native capture checks

Run on an interactive Windows desktop with the .NET 10 SDK:

```powershell
dotnet run --project tests/WardogsXpTracker.Windows.CaptureChecks -- all
```

Use `quiet`, `persistent`, or `guards` in place of `all` for a shorter loop. The check opens temporary synthetic windows. The quiet test gives its own window foreground focus without sending keys. It captures no game or player window and reads no tracker save. It closes its helper processes after each scenario.

Before each positive quiet read, the probe asks its own fixture to take focus and waits for acknowledgment. If focus changes during the copy, it retries that setup at most three times. The expected foreground-loss rejection test does not use this helper. Production capture checks stay unchanged. Run separately from other native UI tests and avoid changing foreground apps until it finishes.

The quiet loop verifies the selected client pixels and rejects any measured yellow-outline appearance. The Windows Graphics Capture loop verifies the same pixels and fails if the yellow outline appears more than once across six reads. Windows can retain a steady capture border in that mode.

The border sampler reads only edge points over the fixture's own solid background. Its yellow-color detector matches the reported Windows 10 symptom. A different OS capture-indicator color may need a detector update; zero yellow samples in WGC mode alone do not prove the absence of another indicator.

Guard checks cover foreground loss, occlusion by a test window, minimizing, hiding, offscreen placement, target close, steady WGC-session reuse, a static scene, resize recovery, explicit Stop, restart, and switching to quiet mode. A texture that changes size during capture may be rejected once before a safe retry. No cached image is substituted for a fresh WGC frame.

Small JSON reports remain in a uniquely named temporary directory. No screenshot files are written. The console prints that directory. This project uses the production Windows capture assembly by default and has no test-framework package.

The local investigation reproduced repeated border appearances in the earlier per-read WGC implementation. That historical implementation is not included in these checks.
