# Build and run

Version 0.3.7 adds whole-match deletion, Skip step in the optional walkthrough, quiet capture, and automatic reading enabled by default. It retains the four-page first-use guide, real-game walkthrough, separate Analytics, and optional Test Tools. Both onboarding workflows remain available from Other Options and await user acceptance.

Session Tracker retains six cards in two columns and three rows. Their game order is Assault/Medic, Recon/Support, and Driver/Pilot. **Diagnostics Lab** contains image diagnostics. **Options** contains seasons, shortcuts, and the completion sound.

The **For Analytics ONLY** readiness panel remains in the Session Tracker sidebar for starting-baseline certification. A separate **Analytics** tab contains charts and record explanations. Both onboarding workflows are implemented, pending user acceptance. The full How To pages, a rules editor, and tutorial screenshots remain planned. See the [agreed app design](APP-DESIGN.md) and [tutorial dependency plan](TUTORIAL-PLAN.md).

## Resize the app

Resize or maximize the app freely, including 1920-wide and larger windows. The earlier 1,080-width cap is removed.

Session Tracker uses the finite client area for its header, read status, six compact cards, and footer. Its sidebar has an independent scroll area. The old 690-unit card surface and the layout that put the sidebar below all six cards are superseded.

Read status and the end-of-match prompt stay above the cards and For Analytics ONLY. Its reserved area keeps a failed or accepted read from moving the card grid. The inactive-session prompt is bounded to the card area. Expanding Quick Guide changes only the sidebar's scrolling content.

The minimum window size is now 816 × 672 WPF units, up from 780 × 640. The increases are about 4.6% and 5%. The card region has a 441-unit maximum, up 5% from 420. Class names use size 16. Baseline, current, and detail text use size 12.

WPF units differ from physical pixels when Windows display scaling changes. Window borders and margins also reduce the available client area. Other tabs retain page scrolling, including the shared table wheel handoff. Changing tabs or tracker state must not change the outer window bounds.

## Version 0.3.7 validation

The isolated deletion Core tests passed 13 checks. Shown WPF deletion workflows passed 78 checks, including default-No cancellation and owner-bound save recovery. A focused follow-up passed 19 checks for date sorting and preference preservation after a failed deletion. Walkthrough state checks passed 148 checks, and shown WPF Skip step workflows passed 64 checks.

The final native capture guards passed 15 checks. On this host, repeated Windows capture produced three border onsets, persistent Windows capture produced one, and quiet capture produced none. These measurements cover the controlled fixture, not every game or Windows configuration.

The complete Core suite passed 248 tests. The actual fixture-pixel OCR scene chain passed 18 checks. The viewport suite passed 4,433 checks. Release publishing, the isolated executable smoke test, and the source/image/compiled-path audit passed. See [UI validation](UI-INTERACTION-VALIDATION.md) for exact package identity and test limits.

## Earlier version 0.3.6 validation

Check fresh and legacy settings, Finish/Skip/X/Escape, replay, remapped shortcuts, paused sessions, and guide-settings save recovery. Guide navigation must work without a game or capture. Opening and closing it must preserve session data, sources, and preferences.

Check the optional offer after a saved Finish, persisted Pause/Continue, real capture gates, explicit new-session timing, baseline-version binding, manual coverage, final confirmation, and the matching real Analytics source. Test mock/import/manual evidence rejection without game automation. Existing XP and session state must not change merely because the guide advances, pauses, or closes.

Run the portable walkthrough checks with `dotnet run --project tests/WardogsXpTracker.App.OnboardingChecks`. They exercise retained captures, match binding, manual coverage gates, and source exclusions without opening a window or player saves. Run `scripts/Test-TrackerViewport.ps1` on Windows for the finite tracker layout and visible walkthrough panel.

Record the measured build and first-use results in [UI validation](UI-INTERACTION-VALIDATION.md). Earlier counts below describe their named releases.

## Earlier version 0.3.5 validation

The mathematical audit passed 32 checks against the saved bonus rules, XP-source solver, SessionTracker, and AnalyticsEngine. Exact placement and final totals leave different true starts possible. Reconstructing a baseline from a split derived from that baseline does not certify it.

The Core tests passed 235 checks. The native viewport check passed 4,282 checks. The summary check passed 224 checks. Release package checks remain separate.

The version's build and native workflow results belong in [UI validation](UI-INTERACTION-VALIDATION.md). The earlier counts below describe their named versions.

## Earlier version 0.3.4 validation

- The analytics-import service passed 26 checks for file format, schema, source recalculation, round trips, cancel, failure, and unchanged file bytes.
- The integrated WPF import check passed 25 checks. It covered visible actions with Test Tools off, source labels, recalculated charts, cancel/failure retention, return to current data, and unchanged real tracker state and saves.
- The import helper builds completed with zero warnings and errors.

The final native regression passed 4,282 checks. Input checks passed 203 checks, and Test Tools checks passed 30. The standalone executable initialized an isolated local save and closed with exit code 0. Native tests covered the 816 × 672 minimum at 100% Windows scaling. See [UI validation](UI-INTERACTION-VALIDATION.md). The earlier results below describe version 0.3.3.

## Earlier version 0.3.3 validation

- Debug and Release builds completed with zero warnings and errors.
- The shown native-window regression passed 4,367 checks with zero failures. It covered the short-window targets, inactive overlays, rejected-read placement, all six card action bounds, pending-final buttons, and the longest supported shortcut labels.
- Quick Guide expansion passed at the top and end of the sidebar, including the former sticky-bottom case. Cards and the inactive overlay retained their bounds.
- Returning from all five other tabs passed at 780-, 1280-, and 1920-wide windows. Tracker page scrolling stayed disabled; other pages retained their scrolling behavior.
- The full OCR/mock integration passed 84 checks. The shared table focus, buttons, arrows, keyboard navigation, and wheel handoff passed 14 checks.

These native layout results cover the tested 96-DPI configurations. Other display scaling settings remain outside this matrix. The published executable started, created its isolated local save, and closed normally with exit code 0.

## Build tools and output

Development needs Windows 10 version 2004 or later, or Windows 11, with the .NET 10 SDK. Visual Studio is optional.

Open PowerShell in the repository folder. If the SDK is on PATH, use:

```powershell
.\build.ps1 -Action Run
```

If the SDK is in a separate folder, pass its full path:

```powershell
.\build.ps1 -Action Run -DotnetPath 'C:\path\to\dotnet.exe'
```

Other actions:

```powershell
.\build.ps1 -Action Build
.\build.ps1 -Action Test
.\build.ps1 -Action Publish
```

`Run` compiles and starts a development build. `Test` runs the Core tests. `Publish` creates a self-contained Windows x64 executable at:

```text
artifacts/capture-lab-win-x64/WardogsXpTracker.exe
```

Distribute that executable to players. It includes the .NET runtime, OCR libraries, and OCR assets. Players do not need an SDK or separate .NET installation. The first launch extracts support files automatically. Build outputs do not belong in Git.

## First-use setup guide

After successful local-save initialization, the app always opens the short guide on first use. Profiles created before guide markers existed also receive it. A busy startup defers the offer until the app can display it. Guide content version 1 uses two optional settings fields for completion and deferral. These versions are separate from the app version.

| Page | Content |
| --- | --- |
| Prepare | Current season/source, Refresh and the game picker, New Session per match, optional Track classes, and the saved-session resume reminder |
| Read XP | Current UNLOCK column, actual configured snapshot/check labels, Manual XP, the completed-reward trap, and the earned target versus bonus receipt |
| Record results | Match-end PROGRESSION, check shortcut, Record Final XP, Decide Later/Saved Results, and End Session after an early exit |
| Optional Analytics | For Analytics ONLY, confirmation that the original start preceded earned XP, later Saved Results → Starting baselines…, and the separate Analytics tab |

Use Back and Next to review the pages. Finish saves completion. **Skip for now**, the window's X, and Escape save deferral. Either marker suppresses automatic replay for that guide version. Select **Options → Other Options → Reopen setup guide** to review it later.

The owned modal targets 680 × 560 WPF units and stays within the available work area. Its body scrolls when needed. The footer and keyboard navigation stay available. The main app keeps its outer bounds.

The guide does not require the game, a capture, or a baseline. It does not create or resume a session, change XP or coverage, load fake data, change sound, or select a new source. A paused session remains paused until you explicitly choose Resume Saved Session or New Session afterward.

Guide state uses a dedicated settings-only save. Other preferences and real/mock session data stay unchanged. A failed write rolls back the version fields and shows Retry and **Close without saving**. Retry reattempts the marker save. Closing without a saved marker allows the guide to appear again next launch. A guide-save failure does not apply the global tracker save block.

New and legacy settings with no Test Tools preference keep it off. A saved true preference remains true. Both onboarding workflows are implemented pending user acceptance. Full How To pages and tutorial screenshots remain future work.

After a saved Finish, **Try guided tracking?** offers **Start guided walkthrough** or **Not now**. Skip, X, Escape, and an unsaved finish do not open this prompt.

## Optional real-game walkthrough

Start it from the Finish offer or **Options → Other Options → Start guided walkthrough**. The Options button changes to a resume or view action for saved walkthrough state.

The modeless task panel sits above For Analytics ONLY. It leaves the game and normal app controls usable. **Pause**, **Continue**, **Restart for new match**, and **Close guide** change only the guide. Pause state persists across restarts. Continuing the guide never resumes a saved game session automatically.

**Skip step** advances one task and saves its name in guide metadata. It creates no session, capture, starting-coverage confirmation, or final confirmation. The finished heading identifies skipped tasks. Missing analytics evidence stays missing. A deleted bound match shows recovery actions instead of restoring that match or claiming completion.

| Step | Required evidence or player action |
| --- | --- |
| Select game | Select a real game window. Continue checks the selection without automatic capture. |
| Start a new session | Explicitly create a session after the walkthrough starts. Pause an existing-match walkthrough until a new match if needed. |
| Capture baselines | Use the configured snapshot shortcut on real UNLOCK. The first real captured class binds the walkthrough's baseline evidence. Capture other enabled classes or clear their Track boxes. |
| Confirm starting coverage | Manually confirm all enabled original starting baselines in For Analytics ONLY. Confirm only snapshots that preceded earned XP. |
| Read current XP | Read real UNLOCK for the bound match. The same XP value can satisfy this read step. No artificial XP gain is required. |
| Capture match end | Read real end-of-match PROGRESSION. Keep missing final class totals explicit. |
| Confirm results | Use Record Final XP or the bound record in Saved Results. Historical review does not activate an older session. |
| View Analytics | Open the bound match's Analytics details with the real source and correct season. Imported or fake overlays cannot complete this step. |

Proper match-wide Actions/Bonus analytics require both confirmed starting coverage and confirmed final results. The guide explains this requirement beside its steps and points to Analytics details. It never certifies a baseline or result automatically.

If a baseline followed earned XP, do not certify it as a match start. The live tracker remains usable for the measured interval. Use the walkthrough's fresh-match path for complete analytics coverage.

An optional `InteractiveWalkthroughState` in settings retains guide progress. An immutable state helper evaluates evidence against its bound season, session, and original baseline IDs/versions. Real capture steps reject mock, imported, manual, and reused-final evidence. Source, session, or baseline mismatches show recovery actions rather than assumed progress.

Closing the panel does not end the match or alter its baselines. Unknown placement, missing finals, and uncertain source splits retain their existing limits. Guide state does not create session data, change preferences, or enable Test Tools.

## Main workflow

1. Open WARDOGS in borderless or windowed mode. Start the executable, select **Refresh**, and select the game window.
2. Select **New Session** for the match. Use each card's **Track** checkbox to choose the classes you want.
3. In the game, open TAB, then UNLOCK. Show a class's current XP column and push **F3**. Repeat for each enabled class.
4. During the match, show a class's current XP column and push **F4**. Check its retained card for measured XP and the completion check.
5. At match end, show PROGRESSION and push **F4**. Select **Record Final XP** when results appear. Use **End Session** if final results are unavailable.

Each accepted baseline starts its class immediately. Other classes can remain without baselines. A red card means **Baseline not established**. A gray card means **Baseline ready**. Yellow-to-green progress follows bonus tiers.

Before a session starts, a **New Session** prompt covers the card area inside the app. A saved session can also offer **Resume Saved Session** there.

The large green check means current XP minus the baseline reached the session's earned-XP target, currently 2,500 XP. Final bonuses never advance it. The check does not establish that the player received a bonus.

Starting-baseline certification is in the **For Analytics ONLY** readiness panel on **Session Tracker**. Leave a class unconfirmed if its baseline does not precede earned XP in that class. Tracking continues with the warning.

## Starting-baseline certification

In the **For Analytics ONLY** readiness panel on **Session Tracker**, each class has its own starting-baseline confirmation. Confirm only an eligible original baseline that precedes any earned XP in that class. A rebase does not qualify as a starting baseline.

**Confirm all captured baselines…** opens a separate confirmation that lists eligible captured baselines for enabled classes. It confirms only those listed baselines. Missing or later-captured baselines do not inherit the answer.

You can confirm an eligible original baseline after the match ends through **Saved Results → Starting baselines…**. Confirm only if you know that its original snapshot preceded earned XP. The app records this later attestation and preserves the original baseline's value, time, ID, version, and observations. Reviewing an older match does not change a newer active session or its counters.

Exact placement and final cumulative XP do not trigger automatic certification. The end screen contains no independent whole-match earned-XP amount. A calculated source split assumes its starting baseline and cannot independently prove that baseline's timing.

The certification panel shows a bright starting-XP warning until every enabled class has a confirmed starting baseline. It then turns green with a starting-baseline confirmation. This confirms neither final XP nor bonus receipt. It does not promise complete analytics data. Charts appear on the separate Analytics tab.

## Baselines and manual entry

F3 records missing enabled baselines. It does not replace an established baseline during normal checks. The **Snapshot** and **Check XP** buttons also work without shortcuts.

For a replacement, select the class's **Re-Establish Baseline** button and confirm the reason. Show the correct game page while the prompt remains open. Push the selected snapshot shortcut, or choose manual entry. The old baseline stays active until a valid replacement arrives. Canceling or closing the prompt keeps the old value.

A replacement starts a new measured interval for that class. The track retains its mid-match rebase warning and can still reach the target. The save retains the old baseline and the replacement history.

**Manual XP** accepts the exact cumulative total, not the XP gained this match. It supplies a baseline when none exists. Otherwise, it supplies current XP or a correction to pending final XP. After a reported early exit, it can record a closing total separately. Record that total before earning XP in another match.

The UNLOCK page can display completed reward columns instead of current progress. For example, `54,750/54,750` is not the current total when Assault has `56,486/64,500`. Use the right arrow to show current progress. The reader rejects unsupported or uncertain readings. A rejected read preserves the previous accepted session value.

## Finals, early exits, and restart

The app stores recognized final XP as pending results and freezes live counters. **Record Final XP** opens the confirmation choices. **Use observed** states the supported placement, such as first place, second or third, or unknown placement. Choose a manual placement only when you know it.

**Decide Later** hides the prompt and retains the old references, pending results, and observed backups. They survive a restart. Open **Saved Results** to review a deferred match, correct its pending XP, or inspect a confirmed summary.

Reviewing an older match does not change the active session, its baselines, or its live counters. The app retains observed XP and placement separately from manual corrections. A score-only result stays pending until at least one class total is observed or entered explicitly.

After confirmed results save, the compact dialog always shows **MATCH XP RECORDED!** in bright green bold text. Its known total stays white. It shows supported source amounts and brief rows for classes with a known increase. Missing totals and unresolved sources do not become false zero values.

When starting baselines remain unconfirmed, replace the old **Recorded values/classes only** line with an underlined gold warning. It names **For Analytics ONLY** and directs you to **Saved Results → Starting baselines…**. Hide that warning when starting coverage is confirmed. The success heading confirms the record saved, not that Analytics coverage is complete. Detailed totals and evidence remain in saved results and analytics.

Predictions never replace final observations. Classes absent from an end screen do not receive invented final totals.

**End Session** offers **Finished**, **Left Match**, **Disconnected**, and **Unknown**. Left-match and disconnected outcomes record the player's report of no bonus. Missing end screens alone do not establish that outcome.

After an app restart, **Resume Saved Session** keeps the saved baselines for the same match. Select **New Session** when the game match changed. If confirmed final totals exist, New Session offers them as baselines. Select **No, snapshot manually** if you played any unrecorded matches afterward.

## Delete a saved match

Open the **⋮** menu in the first Saved Results column, then select red **Delete**. The confirmation reads **This action cannot be undone. Delete?** Its only choices are Yes and No. No receives initial focus and is the default. No, Escape, or X keeps the record.

Yes removes the entire session, including its observations, history, final results, and derived analytics. Other sessions, preferences, and imported reports remain unchanged. Deleting the current session clears its active reference without activating an older match or creating another session.

A failed deletion save keeps the record removed in memory and shows Retry save. Further changes stay blocked. Retry writes the original owner to its original file, including fake results and screenshot-mock sessions. Returning to real data or closing the app must resolve that save or retain the explicit unsaved state.

The close prompt can keep the app open for retry. Exiting with the previous save can restore the old record at the next launch. Normal backup files remain intact. Deletion does not wipe physical files or alter an imported report.

## Options and local saves

Use **Options** to add, rename, or select a season. Each season has its own history. The default season is Season 2.

Snapshot and check shortcuts are remappable from the available choices. The defaults are F3 and F4. Select **Apply Hotkeys** after a change. Shortcuts work while the game has focus. If another app owns a shortcut, the app reports the conflict. Use the buttons or select different shortcuts.

The completion sound is enabled for new profiles. Existing profiles retain their saved setting. You can change the sound preference in **Options → Other Options**. This section sits between Capture Keys and Current Bonus Rules and also contains Enable Test Tools.

The current ruleset is **Oct 15 2026 Patch XP Bonus Rules**. Each new session copies its rules, so later rule changes do not alter older sessions. This prototype displays the rules but does not include a rules editor.

The default save folder is:

```text
%LOCALAPPDATA%\MadLicorice\WardogsXpTracker
```

**Options** shows the exact folder. Its main files are:

| File | Contents |
| --- | --- |
| `settings.md` | Season index, selected season, shortcuts, sound/capture preferences, Test Tools opt-in, setup-guide markers, and interactive progress with skipped tasks |
| `season-<id>.md` | Baselines, sessions, observations, pending and confirmed results, observed backups, outcomes, and history for one season |
| `rules-oct-15-2026-v1.md` | Current bonus rules |
| `*.md.bak` | Previous versions retained after replacement saves |

The app automatically saves accepted readings and state changes in Markdown files with readable JSON. It does not require a manual Save action. It does not overwrite malformed or unsupported saves with empty history. Only one app instance can write to the same save folder.

Version 0.3.7 preserves schema version 1 for season saves. Setup-guide completion and deferral versions are optional settings fields, separate from the app version. Interactive walkthrough state and its skipped-task list are also optional. The observed-backup list is optional for older files. Analytics derives its report from those records and the rules copied into each session.

**Retry save** appears only after a save failure. If it appears, keep the app open and select it. The close prompt offers to keep the window open when the latest state cannot save. Further analytics are planned for a future season. You can also further analyze your local save files.

## Automatic reading and capture limits

**Read every 2 seconds** defaults to enabled in new and legacy settings without a saved preference. A saved opt-out remains off. The `AutomaticReadEnabled` setting persists. F3 still establishes missing baselines, and F4 remains available for manual checks.

Automatic reads require a selected real game, an unpaused session, and available local saves. They run during an active session or a frozen session awaiting confirmed finals. They do not read mocks, initialize a new session, or resume a saved match. Ordinary gameplay scenes do not create repeated unsupported-screen notices. End-score detection freezes live counters but keeps reading for PROGRESSION totals.

Accepted observations save automatically. The app never automatically certifies starting-baseline timing or confirms final results. A save or capture pause does not erase the saved automatic-reading preference.

The default mode is **Quiet · visible game only**. The selected game must be in front, uncovered, and fully within the visible desktop. The capture copies only its client rectangle. It creates no full-display image. Quiet capture waits when another app has focus or covers the game.

**Window capture · Windows border** uses persistent Windows Graphics Capture for the selected window. Windows can show its capture border. This alternative does not silently fall back to another window or the whole display. A minimized, blank, closed, or unsupported target does not supply a valid reading. The app neither reads game memory nor navigates game menus.

The UNLOCK reader initially targets the supplied 1920 × 1080 layout. Other aspect ratios, display scaling, interface changes, and game capture behavior need live tests. A successful OCR read alone does not prove that the number matches the game. Compare exact totals during initial testing.

Level forecasts use observed XP, the current bonus tier, and the visible next threshold. They are estimates for that threshold, not a complete prediction of every later level.

## Analytics, import, and export

Open the separate **Analytics** tab for class bars, the source pie, exact tables, and record explanations. Green shows supported in-match XP. Gold shows bonus XP. Gray **Indeterminate XP** shows received XP whose source cannot be resolved from the available evidence.

The class bars and pie use the same included received-XP records. A dash means no qualifying records, not zero earned XP. Pending finals, unconfirmed starts, and incomplete records remain visible outside received totals. A known total with conflicting evidence can remain indeterminate.

**Average Missed Bonus** applies only to confirmed no-bonus outcomes. Each record averages first-, second-, and third-place bonuses equally. The displayed total sums those record averages. It is missed opportunity, not received XP, and stays outside the pie. **At least** means the observed tier supports a lower bound because coverage is incomplete.

Hover the charts or information icons for explanations. **Export analytics…** saves the report, derivation version, and source season records in a readable Markdown file. The source includes observations and the rules for each session. A mock export identifies its data as mock data.

**Import analytics…** opens an analytics-export or season-save Markdown file with a file picker. The app validates the save container, season schema, records, and copied rules. It recalculates the report from the source season and ignores cached report amounts.

The imported filename and season stay visible. **Return to current analytics** removes only the imported selection. Import does not merge records, change the active season, or write local saves. Canceling or rejecting an import keeps the previous source. An export of imported analytics contains the displayed source season and its mock-data label.

Import, export, their source label, and transfer feedback stay available while Test Tools are off.

Click a table header to sort it. A filled arrow shows the direction. Numeric values and dates use their underlying values. Selection clears after an outside click, focus change, or window deactivation. The mouse wheel scrolls a table while it has room, then continues through the surrounding page.

## Optional testing without WARDOGS

1. In **Options → Other Options**, select **Enable Test Tools**, then open its tab after Options and select **Load fake screenshots / restart**.
2. Wait for the app to return to Session Tracker with an Assault baseline of **56,486**.
3. Use the header's **Next mock + read** to advance through the bundled walkthrough.
4. Open **Analytics** and select **Load fake stats** to inspect the 11-session example dataset.
5. Select **Real data** in the header when finished.

Test Tools are off by default. Enabling them reveals the tab with a brief gold flash without changing the selected tab. The choice persists in settings. Disabling them safely returns from mocks before hiding the tab and mock controls. An imported analytics source stays selected.

The mock screenshots pass through the real local OCR reader. Loading the walkthrough starts a new mock session and reads its baseline automatically. The sequence covers every tier, 2,499 versus 2,500 XP, extra XP, rejection, DEFEAT uncertainty, and final totals.

| Mock | Expected result |
| --- | --- |
| 9 | Accept 59,486 current XP. The gain is 3,000 above the initial baseline, so the target remains complete. |
| 10 | Reject the intentionally invalid completed page. Keep the baseline, last accepted XP, and target state. |
| 11 | Record DEFEAT as second-or-third placement evidence. Freeze live checks. No class final XP appears yet. |
| 12 | Record 62,986 as pending final XP. The 6,500-XP difference can include a bonus and never advances the live check. |

After mock 12, confirm the result to inspect the saved summary. Alternatively, select **Decide Later** and reopen the record from **Saved Results**.

While screenshot mode is active, F3 and F4 read the selected bundled mock image. They do not capture a game window. **Read selected mock** and the scenario list are on Test Tools. The header's **Next mock + read** lets you keep Session Tracker visible.

The mock sound checkbox defaults to enabled and does not change the real sound preference. **Play completion sound** plays one preview without changing progress. Options remains reachable during the screenshot walkthrough. The game-window list and **Refresh** safely restore game capture before changing its source. Real season, shortcut, and sound controls stay unavailable while mock capture is active.

The fake stats loader changes the analytics dataset only. It does not replace the active tracker. **Real data** clears fake stats and restores any real tracker paused for the mock walkthrough.

Mock sessions and bundled test files use this separate folder:

```text
%LOCALAPPDATA%\MadLicorice\WardogsXpTracker\mock-data\0.3.7
```

The folder shown in Test Tools is the exact location. The app extracts bundled mock PNGs there automatically. It does not save real game captures automatically. Normal startup loads real data. To repeat the tier sequence, use **Load fake screenshots / restart** again.

Reuse the 12 bundled images for manual entry, rebase, and outcome tests. Reset the walkthrough before another tier sequence. The distributed mock pack includes a detailed `TEST-GUIDE.md`.

For the short retrospective workflow, select **Load saved-baseline workflow** in Test Tools. It creates one saved Assault/Medic match with unconfirmed starting baselines. Review the warning, then use **Saved Results → Starting baselines…** to confirm only the starts you know were recorded before earned XP. After confirmation, the 11,700 total splits into 4,500 in-match XP and 7,200 bonus XP.

This generated example uses `history-baseline-workflow.md` in the separate mock folder. It never overwrites the 11-match fake-stats demo. It adds no PNGs, so the bundled image pack remains exactly 12.

## Screenshot diagnostics

Select **Diagnostics Lab**, then **Open Screenshot**, to test an original game image. Compare its class, exact total, and threshold with the preview. Imported images never establish baselines or update session progress.

After a read, select **Save read report…** and choose a ZIP file. The report includes the captured image, analysis, diagnostics, and source details. It also includes the latest different successful or rejected attempt when available.

Game captures save only when you explicitly export a report. No image, report, or save uploads automatically. See [Capture validation](CAPTURE-VALIDATION.md) for the confirmed Driver misread and current validation limits.

Developers can repeat the shown native-window layout check on Windows:

```powershell
.\scripts\Test-TrackerViewport.ps1
```

This check uses an isolated profile and writes reports under `artifacts/layout-checks`. Pass `-DotnetPath` when the SDK is outside PATH. It does not capture the game or register global shortcuts.

## Generated fixtures and public-release audit

The 12 bundled mock images already exist. Ordinary builds do not regenerate them. The development generator creates equivalent OCR fixtures and isolated analytics data:

```powershell
dotnet run --project tools/GenerateAnalyticsMocks -- --output 'C:\path\to\empty-folder' --verify
```

The output folder must be empty. These generated images contain no player captures or saves. Exposing the existing generator inside the app has a rough planning estimate of 5,000–10,000 additional AI tokens. The current embedded synthetic fixtures need no additional generator feature.

Before public distribution, run:

```powershell
python .\tools\AuditPublicRelease.py --publish .\artifacts\capture-lab-win-x64
```

The audit checks eligible project text, image metadata, public debug symbols, and project Release assemblies. It examines the input assemblies because single-file compression can conceal embedded path strings from a direct executable search.

The source and 13 public images passed the current path/metadata check. An earlier Release scan found embedded debug paths. Release configuration now uses `DebugType=none` and `PathMap` to remove that disclosure. The final compiled-output audit passed on 113 text files, 13 images, and six project Release input assemblies. The current package identity is in UI-INTERACTION-VALIDATION.md. This audit does not scan ignored private fixtures or runtime saves.

## References

- [.NET run command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)
- [Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)
- [Selected-window capture API](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createforwindow)
