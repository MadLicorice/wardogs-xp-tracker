# Native tracker viewport checks

Run this check on Windows with an interactive desktop and the repository's .NET SDK:

```powershell
.\scripts\Test-TrackerViewport.ps1
```

Pass `-DotnetPath C:\path\to\dotnet.exe` when the SDK is not on `PATH`.

The script builds the app through a relative project reference. It writes JSON results, console logs, and rendered PNGs under `artifacts/layout-checks/<unique-run>/`. This directory is ignored by Git. An explicit `-OutputDirectory` must remain inside the repository's `artifacts` directory.

The fixture opens one offscreen native `MainWindow` with the production resource dictionaries. Its fixture host has no automatic startup window, and it disconnects the global-hotkey hook. It uses a new isolated local-save directory for each run. Sound and automatic reading stay off. It does not capture the game, inject input, or use real player saves.

Checks cover physical outer sizes 816×672, 1280×672, 1920×672, and 1280×850. The report records actual outer/client dimensions and DPI. All seven tracker states must keep six Manual XP and Re-Establish Baseline buttons inside their cards and viewport without page scrolling.

Guide expansion, sidebar scrolling, and tab returns must preserve left-card and overlay bounds. The check also covers the rejected-read strip, the pending-results action, 816-pixel tab captions, and long shortcut labels. It uses the production styles and shortcut formatter. A failed check returns a nonzero exit code.

The check also enforces larger class/XP text, Analytics ONLY, the pinned match-end actions, scrollbar clearance, default-hidden Test Tools, complete tab borders, and stable selected-hover styling.
