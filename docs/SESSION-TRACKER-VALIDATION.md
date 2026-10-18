# Session Tracker validation: 0.2.0

Date: October 16, 2026, America/Los_Angeles.

This record covers the first Session Tracker prototype. It does not establish completion of the full app design.

## Latest live OCR report

Input: `wardogs-read-report-20261016-223238.zip`, exported by Capture Lab 0.1.3.

- The accepted Pilot frame shows **44,702 / 54,750 XP**, current level 14 and threshold level 15. The report agrees with the visible digits.
- The rejected Driver frame shows completed columns, ending at **54,750 / 54,750 XP**. The sidebar shows level 15. Rejection correctly asks for the next progression page.
- The report contains no unexplained accepted conflict. The user also reports that all six class reads now work.

The OCR implementation did not change in 0.2.0. Earlier screenshot and repeated-read results remain in `CAPTURE-VALIDATION.md`.

## Automated and local checks

| Check | Result |
| --- | --- |
| Core unit tests | 142 passed, zero failed or skipped |
| Debug app build | Zero warnings and errors |
| Scratch WPF integration harness | 55 assertions passed |
| Native shortcut harness | Pair changes, swaps, duplicate rejection, conflict rollback, disabled owner delivery, and disposal passed |
| Release single-file publish | One executable, version 0.2.0.0 |
| Published startup | Main window and message loop reached, three initial Markdown saves created |
| Published shutdown | Normal close and save path completed, exit code 0 |
| Layout | All six complete cards fit in a 1132 × 850 render. The footer remains scrollable. |

All persistence and startup probes used isolated workspace folders through `WARDOGS_XP_DATA_DIRECTORY`. They did not modify the player's default local saves. Shortcut probes used test-only function-key combinations and did not inject keys into the game.

The Core tests cover retained class values, exact target boundaries, guarded F3, manual entry, decreasing reads, rebase cancellation and replacement, season data, rules snapshots, final candidates, restart recovery, and malformed saves.

The final six regressions cover changed disabled observations and manual-only end handling. Disabled observations request a save without advancing counters. Recognized UNLOCK scenes establish session context even when numeric OCR fails. A reliable but unlinked end screen freezes manual tracking without inventing candidate results.

The WPF harness checks retained cards, target checks, rejected reads, stale capture contexts, save reload, pending final totals, and blocked-load retry. Failed initialization cannot overwrite local history through Retry save.

## Build identity

```text
File: WardogsXpTracker-0.2.0.exe
Length: 82,243,055 bytes
File version: 0.2.0.0
Product version: 0.2.0+3e4f35462ca243c086dd59ea71e47b6816b876fe
SHA-256: 4FA17F8904AA827CBB675493815666EB145C47F0E2CC01932560A33A4E749444
```

The commit suffix identifies the existing design commit. Implementation changes remain local and uncommitted. The hash identifies this exact test executable.

Publish command:

```powershell
dotnet publish src/WardogsXpTracker.App/WardogsXpTracker.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/session-tracker-win-x64-0.2.0 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded --no-restore
```

The normal `build.ps1 -Action Publish` command still uses `artifacts/capture-lab-win-x64`.

## Limits and next live test

The Session Tracker now needs a real match test. Check F3 baselines, retained F4 readings, restart resume, rebase cancellation, and pending final confirmation.

A session must observe its own UNLOCK context before it can link final candidates. This prevents a still-visible previous match screen from becoming the new match's ending. Manual-only tracking freezes on an unlinked end screen, but cannot confirm those unlinked totals automatically.

The app does not detect an unseen match transition. Select New Session for each match. Reuse confirmed final totals only when they are still the correct starting totals.

Analytics charts, the rules editor, full How To pages, and the screenshot tutorial remain planned. The app stores observations and history for that later work. Level forecasts use the next observed threshold and remain estimates.

Different display layouts, repeated live match transitions, and a clean Windows 11 installation still need tests.

## UI refinement: 0.2.1

The app name is now MadLicorice's WARDOGS XP Tracker. The unchanged supplied logo is bundled in the executable. Diagnostics Lab replaces the Capture Lab navigation label.

The tracker uses the game's two-column, three-row order: Assault/Medic, Recon/Support, and Driver/Pilot. An inactive-session cover blocks card actions until New Session or Resume. Starting a session enables the cards.

Starting-baseline certification now belongs to an Analytics readiness panel. Each class has its own confirmation. Confirm all captured baselines opens a separate dialog listing eligible classes and exact XP. The panel turns green only when enabled starting baselines are confirmed. Final results and bonus attribution remain separate.

Saving remains automatic. Retry save appears only after a write failure. Sound defaults to on for fresh settings, and existing preferences remain intact. Normal save schema and OCR behavior do not change.

Validation results:

- 150 Core tests passed, zero failed or skipped.
- App Debug build completed with zero warnings and errors.
- 98 scratch WPF assertions passed, including per-class and Confirm All cancel/commit paths, inactive/resume guards, saved sound preferences, and save recovery.
- All six complete cards fit at 1132 × 850. Active and analytics-ready card bottoms measure 833.8 pixels. Inactive cards measure 787.9 pixels.
- Published executable startup reached its message loop, showed the new window title, wrote the three initial Markdown files, and closed normally with exit code 0.
- The isolated startup profile saved CompletionSound=true. Existing false preferences survived the WPF save/reload test.
- All three preview images passed visual review. No live XP gain test was possible this turn.

Build identity:

```text
File: WardogsXpTracker-0.2.1.exe
Length: 82,254,054 bytes
File version: 0.2.1.0
Product version: 0.2.1+3e4f35462ca243c086dd59ea71e47b6816b876fe
SHA-256: 7ABBBAAB7598DDCC8B8103DB623A7E0E8CDFF47A2D73B06408517ABBADEE9A4C
```

Publish output: artifacts/session-tracker-win-x64-0.2.1/WardogsXpTracker.exe. Source changes remain local and uncommitted. Tests used isolated save folders and left player data unchanged.
