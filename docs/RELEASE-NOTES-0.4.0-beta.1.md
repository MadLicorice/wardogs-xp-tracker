# Release notes: 0.4.0-beta.1

This is the first public beta of MadLicorice's WARDOGS XP Tracker for Windows 10 and 11, x64.

Download the portable EXE or ZIP from the [v0.4.0-beta.1 release](https://github.com/MadLicorice/wardogs-xp-tracker/releases/tag/v0.4.0-beta.1). Extract the ZIP before you run `WardogsXpTracker.exe`. The executable includes the .NET runtime and local OCR assets.

## Included

- The orc-and-XP application icon and the corrected three-dot menu in Saved Results. Its action column retains its minimum width.
- Six class trackers with exact XP baselines, manual fallbacks, remappable F3/F4 shortcuts, and separate match-end confirmation.
- Quiet capture of the visible game window, with Windows window capture as an alternative. Automatic reading defaults to enabled.
- A first-use setup guide and an optional real-game walkthrough with Pause, Continue, Restart, Close, and Skip step.
- Saved Results with observed backups, historical review, explicit corrections, and whole-match deletion with default-No confirmation.
- A separate Analytics tab, honest indeterminate XP, missed-bonus estimates, and local import and export.

Test Tools remain off unless you enable them in **Options → Other Options**. The bundled test images and example records are synthetic.

## Data and confirmation

The app keeps seasons, settings, XP history, and rules in local files. It does not upload saves or captures. A diagnostic screenshot report saves only after you choose its location. See [Privacy](PRIVACY.md).

Starting-baseline certification and final-result confirmation require your choice. Final cumulative XP and placement do not prove the original starting XP. A green live check uses measured in-match gain and excludes the match-end bonus.

## Beta limits

Real-game acceptance in borderless mode remains pending, including the complete end-of-match XP flow. Controlled image, native-window, and state tests do not establish production reliability. Review the displayed XP and use the manual fallback when a read fails.

The setup guide and optional walkthrough remain pending user acceptance. [Issue 6](https://github.com/MadLicorice/wardogs-xp-tracker/issues/6) stays open. The full How To pages, rules editor, and finished tutorial screenshots remain planned.

See [Build and run](BUILD-AND-RUN.md) for capture requirements. See [UI validation](UI-INTERACTION-VALIDATION.md) for measured checks and their limits.

## Source and licenses

The public source excludes player captures, runtime profiles, private absolute paths, and optional image metadata. Required format fields, app versions, functional identifiers, and license notices remain. The source and packaged release have separate audit scopes.

The app uses the [MIT license](../LICENSE). Bundled dependencies retain their own notices in [assets/licenses](../assets/licenses/). The WARDOGS game brand asset remains separate from the app license.
