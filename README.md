# MadLicorice's WARDOGS XP Tracker

A local Windows companion app for class XP in WARDOGS.

**Status: Public beta 0.4.0-beta.1 for Windows 10 and 11, x64.**

[Download the beta EXE or ZIP](https://github.com/MadLicorice/wardogs-xp-tracker/releases/tag/v0.4.0-beta.1) · [Release notes](docs/RELEASE-NOTES-0.4.0-beta.1.md) · [Privacy](docs/PRIVACY.md)

This beta is ready for player testing. Real-game acceptance in borderless mode, including complete match-end XP, remains pending. Controlled tests do not establish production reliability.

The app tracks six classes in a separate window with a dark theme. Cards match the game's two-column, three-row order: Assault/Medic, Recon/Support, and Driver/Pilot. It reads the selected game window with local optical character recognition (OCR). It does not read game memory or control the game.

The [agreed app design](docs/APP-DESIGN.md) describes the full app. This beta includes session tracking, **Saved Results**, a separate **Analytics** tab, and optional mock test tools. It retains match deletion, walkthrough step skipping, quiet capture, and automatic reading enabled by default. Version 0.4.0-beta.1 adds the orc-and-XP application icon and retains the corrected Saved Results action menu. The first-use guide and optional real-game walkthrough remain pending user acceptance. The full How To pages, a rules editor, and tutorial screenshots remain planned.

## Run the beta

1. Download the EXE or ZIP from the [0.4.0-beta.1 release](https://github.com/MadLicorice/wardogs-xp-tracker/releases/tag/v0.4.0-beta.1).
2. If you choose the ZIP, extract it to a folder.
3. Run the downloaded EXE, or `WardogsXpTracker.exe` from the extracted ZIP.

The portable executable includes the .NET runtime and OCR assets. Players do not need Visual Studio, a separate .NET installation, or an app account. Saves stay in your local app data folder.

This beta retains larger class and XP text, pinned end-of-match feedback, and a minimum window size of 816 × 672 WPF units. The Saved Results action column keeps enough width for its three-dot button, including narrow windows.

See [Build and run](docs/BUILD-AND-RUN.md) for build commands, capture limits, and diagnostic reports.

Resize or maximize the app to fit your display. Session Tracker uses a finite viewport with six compact cards and a separate scrolling sidebar. Read status and the end-of-match prompt stay above the cards and For Analytics ONLY. Expanding Quick Guide changes the sidebar content, not the card area or whole page. Other tabs keep page scrolling.

Windows display scaling changes the space available in WPF units. The older 0.3.3 check at 648 pixels high does not establish the new minimum. See the versioned validation results in the build guide.

## First-use setup guide

The guide always opens on first use after successful setup. Existing profiles without completed or deferred markers also receive it. If startup is busy, the guide waits until the app can show it.

Its four pages explain preparing a match, reading XP, recording match-end results, and optional analytics. Back and Next move between pages. Finish records completion. **Skip for now**, X, and Escape record deferral. That guide version does not open automatically again after either choice.

Select **Options → Other Options → Reopen setup guide** to review it later. It shows your current season, data source, and configured capture shortcuts. If a saved session is paused, it reminds you to resume only for the same match. The guide never starts or resumes a session, reads XP, confirms a baseline, changes sound, or loads test data.

No running game, capture, or baseline is required to finish the guide. If its settings cannot save, use Retry or **Close without saving**. An unsaved marker does not count as completion. The guide can appear again on the next launch.

After a saved Finish, **Try guided tracking?** offers **Start guided walkthrough** or **Not now**. Skip, X, Escape, and an unsaved finish do not open this offer.

## Optional guided tracking

The walkthrough uses a modeless panel above For Analytics ONLY. The app and game remain usable. **Pause**, **Continue**, **Restart for new match**, and **Close guide** affect the walkthrough, not the match. Its saved state lets you continue later.

**Skip step** advances one guide task and records it as skipped. It does not capture XP, certify a start, or confirm results. A finished guide with skips says **Walkthrough finished · steps skipped**. Analytics keeps its normal coverage requirements.

Select the real game, then explicitly start a new match session after the walkthrough starts. An existing match does not count as that new session. If you cannot play now, pause the walkthrough until the next match.

Capture a real UNLOCK baseline with your snapshot shortcut. Capture starting baselines for every enabled class, or clear Track for classes you do not need. Then manually confirm every enabled original starting baseline in **For Analytics ONLY**. Confirm only if those snapshots preceded earned XP.

Starting-baseline confirmation and confirmed final results are required for a proper match-wide Actions/Bonus breakdown. The walkthrough never supplies that confirmation automatically. If you already earned XP before a baseline, keep using the tracker and restart the analytics walkthrough for a fresh match.

Next, read real current UNLOCK XP. An unchanged value is valid, and you do not need to earn 2,500 XP to advance. At match end, capture real PROGRESSION totals, then confirm them through Record Final XP or Saved Results. Finish by viewing that match in the separate Analytics tab with the real source selected.

Mocks, imported images, manual XP, and reused final totals cannot complete real-capture steps. The guide binds progress to its season, session, and original baseline versions. A changed source or baseline shows a recovery action instead of false progress. Unknown placement and missing final cards retain their normal uncertainty.

Use **Options → Other Options → Start guided walkthrough** to start it later. The button changes to a resume or view action when a saved walkthrough exists. Opening either guide does not change your selected data source.

## Track a match

1. Open WARDOGS in borderless or windowed mode. Start the tracker, select **Refresh**, and choose the game window.
2. Select **New Session** for this match. Clear **Track** on any class you do not want to measure.
3. In the game, open TAB, then UNLOCK. Show each class's current XP column and push **F3** to establish its baseline.
4. During the match, show a class's current XP and push **F4**. Its card retains the latest accepted value and measured gain.
5. At match end, show PROGRESSION and push **F4**. Select **Record Final XP** when results appear. Otherwise, select **End Session** and report the outcome.

Each class starts tracking as soon as its baseline exists. Missing baselines show red. Ready baselines show gray. Progress changes from yellow toward green at bonus tiers. A large green check means the measured gain reached 2,500 XP under the current rules. Match-end bonuses do not advance that check. Bonus receipt remains separate.

F3 cannot replace an existing baseline during normal checks. Use **Re-Establish Baseline** for an explicit replacement. Canceling that operation preserves the old baseline. **Manual XP** supplies a fallback for baseline, current, or pending final totals. A mid-match rebase keeps a warning beside the track.

Use the **For Analytics ONLY** readiness panel on **Session Tracker** to certify each class's starting baseline. Confirm only a baseline that precedes earned XP in that class. You can make this explicit confirmation later through **Saved Results → Starting baselines…**. It confirms the retained original baseline, not a newly inferred starting value.

**Confirm all captured baselines…** shows its own confirmation for the eligible baselines. Certification does not delay tracking or confirm final XP or bonus receipt. Level forecasts remain estimates and never become recorded final totals automatically.

Exact placement and final cumulative XP do not automatically certify a start. The end screen does not supply independent whole-match earned XP. The app keeps that uncertainty until an eligible original baseline receives explicit confirmation. A bright warning makes this analytics requirement visible without blocking the live counter.

## Sessions and options

Before a session starts, a **New Session** prompt covers the card area inside the app. The six cards retain their readings while you check other classes. Automatic reading checks the selected real game every two seconds when a session needs observations. It defaults to enabled and preserves a saved opt-out. F4 remains available. **Diagnostics Lab** holds screenshot import and reader diagnostics. Imported screenshots never change a session.

The default **Quiet · visible game only** capture needs the game in front, uncovered, and fully on the visible desktop. It copies only that game's client rectangle. **Window capture · Windows border** is the alternative. It reuses its capture session, but Windows can show a capture border. Neither mode falls back to a full-display image.

Automatic reading waits during ordinary gameplay screens without repeated unsupported-screen notices. After an end score freezes the tracker, it keeps watching for PROGRESSION totals. Accepted observations save automatically. Starting-baseline certification and Record Final XP still require explicit confirmation.

Observed final totals remain pending until confirmation. **Decide Later** hides the prompt and preserves both the old references and observed results after restart. Open **Saved Results** to review pending records or inspect confirmed matches later.

The **Use observed** choice states the supported placement, including **2nd or 3rd** after DEFEAT. Manual placement and XP corrections retain the original observed backup. Reviewing an older match does not change the active session or its counters.

The **⋮** menu in the first column of Saved Results contains red **Delete**. It asks **This action cannot be undone. Delete?** with Yes and No. No is the default. Yes removes the whole match and its derived analytics. Deleting the current match leaves no active session. Other matches and imported reports stay unchanged.

If deletion cannot save, it remains removed in memory and the app shows Retry save. Keep the app open to save that deletion. Retry targets the original real or mock file. Exiting with the previous save can restore the old record on the next launch.

The saved summary always uses a bright green bold **MATCH XP RECORDED!** heading and a white known total. If starting baselines remain unconfirmed, an underlined gold warning names **For Analytics ONLY** and directs you to **Saved Results → Starting baselines…**. The success heading confirms the record saved. It does not certify the starting coverage.

A missing end screen does not prove that a bonus arrived. **End Session** includes finished, left-match, disconnected, and unknown outcomes.

After a restart, resume a saved session only for the same game match. Use **New Session** for a different match. If a previous match has confirmed final totals, the app offers them as new baselines. Choose manual snapshots if any unrecorded match occurred afterward.

**Options** lets you add, rename, and select seasons, choose snapshot/check shortcuts, control the completion sound, and enable Test Tools. Sound and Test Tools are grouped under **Other Options**, between Capture Keys and Current Bonus Rules. New profiles have sound enabled. Existing profiles retain their saved preference. Each session retains its rules. The current ruleset is **Oct 15 2026 Patch XP Bonus Rules**.

**Reopen setup guide** and the guided-walkthrough action in Other Options preserve the selected real, mock, or imported source. Test Tools remain off when their preference is absent, and an explicitly enabled preference stays enabled.

## Local saves

The default save folder is `%LOCALAPPDATA%\MadLicorice\WardogsXpTracker`. **Options** shows its exact location.

- `settings.md`: seasons, preferences, setup-guide markers, and optional interactive walkthrough progress.
- `season-<id>.md`: one season's sessions, baselines, observations, pending and confirmed results, observed backups, and history.
- `rules-oct-15-2026-v1.md`: the current bonus rules.

The app saves changes automatically to these Markdown files with readable JSON. Replacement saves retain the previous version as `.bak`. Invalid save files produce an error instead of a fresh replacement. **Retry save** appears only after a save failure. Keep the app open and use it if a save fails.

The app does not upload saves or captures. Game captures save only when you explicitly export a diagnostic read report. Reports can contain private game content, XP, window titles, filenames, or error paths. Review them before sharing. See [Privacy](docs/PRIVACY.md) for the public source audit and local data details.

## Analytics

The separate **Analytics** tab shows received XP by class, a source pie chart, and **Average Missed Bonus**. Green represents supported in-match XP. Gold represents bonus XP. Gray **Indeterminate XP** preserves received XP whose source remains unresolved. The tables show exact values, coverage, and record explanations.

Filled arrows show the current table sort. Selection clears when you leave a table. The mouse wheel scrolls the table while it has room, then continues through the surrounding page.

Pending finals and incomplete records remain separate from received totals. Missed bonuses are opportunity estimates, outside received XP and pie percentages. Their total sums the per-record averages, with equal weight for first, second, and third place. **At least** marks a lower bound when coverage is incomplete.

**Export analytics…** saves a Markdown snapshot with the calculated report, its derivation version, and source season records. **Import analytics…** opens an exported snapshot or season save with a file picker. The app validates the file and recalculates its charts from the source records. The imported filename and season stay visible. **Return to current analytics** removes that selection. Imports do not change the current tracker, active season, or local save files. Canceling or rejecting an import preserves the previous dataset. Existing season saves retain schema version 1.

## Test without the game

In **Options → Other Options**, select **Enable Test Tools**. The tab appears after Options with a brief gold flash, while the current tab stays selected. Then open **Test Tools** and select **Load fake screenshots / restart**. The app starts an isolated mock session and reads the Assault baseline automatically through real OCR. Use **Next mock + read** in the header while Session Tracker stays visible. The PNG pack contains exactly 12 images.

Mock 9 shows 3,000 in-match XP above the initial baseline. Mock 10 intentionally shows an invalid completed page. Its rejection must preserve the previous reading and green check.

Mock 11 supplies DEFEAT placement evidence without final class XP. Mock 12 supplies 62,986 final XP, a 6,500-XP difference that can include a bonus. Confirm it separately, or select **Decide Later** and reopen it from **Saved Results**. Final XP never advances the live counter.

**Load fake stats** shows 11 synthetic sessions on Analytics without changing the tracker. The mock sound toggle is independent of your real sound preference. **Play completion sound** plays one preview.

**Load saved-baseline workflow** creates a separate one-match Assault/Medic example for later manual baseline confirmation. After confirmation, its 11,700 total splits into 4,500 in-match XP and 7,200 bonus XP. It saves as `history-baseline-workflow.md` and does not overwrite the 11-match demo. This example adds no PNGs.

**Real data** in the header restores the real tracker and normal preferences. Mock files stay in `mock-data/0.3.7` below the local save folder. Normal startup returns to real data. Reload the mock walkthrough to start it again.

Test Tools are off for new and existing profiles unless explicitly enabled. The choice persists. Disabling them safely returns from mock data and hides mock controls. Analytics import and export stay available. An imported analytics source stays selected when Test Tools are disabled.

The [tutorial plan](docs/TUTORIAL-PLAN.md) requires acceptance of both onboarding workflows before tutorial screenshots. Version 0.3.6 implements the short guide and optional real-game walkthrough, pending user acceptance. The full screenshot tutorial is not complete.

## Build and tests

Developers need Windows and the .NET 10 SDK. Visual Studio is optional.

```powershell
.\build.ps1 -Action Run
.\build.ps1 -Action Build
.\build.ps1 -Action Test
.\build.ps1 -Action Publish
```

Use `-DotnetPath 'C:\path\to\dotnet.exe'` if the SDK is outside PATH. `Publish` writes `artifacts/capture-lab-win-x64/WardogsXpTracker.exe`.

An optional screenshot check uses private reference images supplied separately:

```powershell
.\scripts\Test-ScreenshotFixtures.ps1 -FixtureDirectory 'C:\path\to\fixtures'
```

Do not commit player captures or local save files. See [asset provenance](assets/PROVENANCE.md) for OCR assets and the user-supplied WARDOGS logo.

Before public distribution, run `tools/AuditPublicRelease.py` against the source and publish folder. It checks personal paths, image metadata, and project Release assemblies. The beta includes synthetic test images, not player captures or save profiles. See [Privacy](docs/PRIVACY.md) and [UI validation](docs/UI-INTERACTION-VALIDATION.md) for audit scope and test limits.

Developers can repeat the shown native-window layout check on Windows:

```powershell
.\scripts\Test-TrackerViewport.ps1
```

This check uses an isolated profile and writes reports under `artifacts/layout-checks`. Pass `-DotnetPath` when the SDK is outside PATH. It does not capture the game or register global shortcuts.

## License

See [LICENSE](LICENSE). Bundled dependencies retain their own licenses in `assets/licenses/`. The WARDOGS logo is a game brand asset, separate from the app's license.
