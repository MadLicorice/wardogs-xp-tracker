# Match deletion, guide skips, and capture validation: 0.3.7

Date: October 17, 2026, America/Los_Angeles.

Status: implementation, automated regressions, final package smoke test, and public-release audit complete. Live WARDOGS acceptance remains pending.

## Saved-match deletion

Saved Results places a small **⋮** menu in its first column. Delete is red. The confirmation uses the exact sentence **This action cannot be undone. Delete?** with Yes and No. No receives initial focus and is the default/cancel choice. No and window close leave the saved match unchanged.

Yes removes the whole session and its derived received XP, missed-bonus estimates, pending counts, and saved rows. Deleting a historical match preserves the active session. Deleting the current match clears its active/resume references without activating an older match or creating another session.

The write retains its exact real or mock owner, store, and filename. On failure, deletion remains in memory and changes stay blocked. Global Retry saves that owner without repeating deletion. Return to real data cannot discard a locked fake deletion. A failed close can keep the app open with the same pending owner.

Imported reports remain read only and unchanged. Normal backups remain intact. Deletion does not wipe physical files. A walkthrough bound to the deleted match shows recovery instead of a stale Analytics completion.

The staged Core deletion suite passed 13 tests. The shown WPF deletion suite passed 78 checks. It exercised production popup binding, UI Automation invocation, modal handlers, file locks, real/fake/mock destinations, cancellation, saved-file reload, and unrelated-data preservation. The mock-session case used a synthetic tracker in the normal mock owner path. It did not read a live game.

A focused follow-up passed 19 checks. It proved that the menu header stays unsorted and Match started owns the initial descending arrow. The actual date-header click handler toggled ascending/descending order. Refresh retained the user's sort. A locked deletion write paused the checkbox and timer while preserving `AutomaticReadEnabled=true` and saved settings bytes. Retry saved the deletion, and reload retained the true preference.

## Walkthrough step skipping

**Skip step** advances one task and saves that task in guide metadata. It does not capture XP, create a session, certify starting timing, confirm final results, or claim an Analytics visit. Skipped tasks remain visible after reload.

A guide with any skips finishes as **Walkthrough finished · steps skipped**. Analytics keeps its existing inclusion rules. Later actual evidence can fill a compatible missing binding. It cannot turn skipped evidence into an observed capture.

The pure walkthrough model passed 148 checks, including its earlier cases. The shown WPF Skip step suite passed 64 checks. It covered each task, busy/pause guards, settings persistence and retry, no tracker mutation, source restrictions, and deleted-match recovery.

The broader shown onboarding/interactive suite passed 291 checks. Its capture-flow checks inject accepted results through production capture-context and apply paths. They use a synthetic selected-window target. These tests prove app transitions and evidence guards, not live-game OCR accuracy. The 64 Skip step checks overlap shared setup and must not be added as an independent coverage total.

The short guide still appears on first use. Only a saved Finish opens **Try guided tracking?** The interactive walkthrough remains optional. Test Tools remain off when their preference is absent, while an explicit saved choice remains intact. Both onboarding workflows await user acceptance.

## Quiet capture and automatic reads

The user approved **Quiet · visible game only** as the default. It copies only the selected foreground game's uncovered client rectangle. The entire client rectangle must fit within the visible desktop. It creates no full-display image. Hidden, covered, minimized, offscreen, or background targets do not produce a valid quiet read. Movement during a pixel copy can reject that copy.

**Window capture · Windows border** remains available and reuses a Windows Graphics Capture session. A controlled native fixture measured these border onsets across repeated reads:

| Mode | Observed border onsets |
| --- | ---: |
| Earlier repeated Windows capture | 3 |
| Persistent Windows capture | 1 |
| Quiet visible-client capture | 0 |

Each mode exercised six reads in the controlled fixture. The final native loop passed all 15 guard/lifetime checks, with zero quiet border onsets and one steady Windows-capture onset. These results describe this host and fixture. They do not establish every game, display driver, or Windows configuration.

`AutomaticReadEnabled` defaults to true for new and legacy settings without that field. A saved false value remains false. Automatic reading requires a selected real target and a resumed active or frozen-unconfirmed session. It stops for confirmed finals and stays separate from mocks and imported screenshots.

Ordinary gameplay scenes do not create repeated unsupported-screen notices. End-score detection freezes live counters while reading continues for PROGRESSION totals. Accepted observations save automatically. Timing certification and final confirmation remain explicit player actions.

A capture pause must preserve the saved automatic-reading preference. Quiet foreground preflight waits without repeated busy transitions when another app has focus. Its separate shown WPF regression passed nine checks.

The actual fixture-pixel OCR scene chain passed 18 checks. Its result is separate from the native capture guards and broader WPF workflow suite.

## Public-release privacy

The eligible source and 13 public images passed the current personal-path and metadata check. The image set contains 12 synthetic mock fixtures and the game logo. Private player captures and saves are excluded from public assets.

An earlier Release assembly scan found embedded debug-path references. Release configuration now uses `DebugType=none` and `PathMap`. The final compiled-output audit passed against all six project Release input assemblies. The publish folder contains no PDB files. No personal absolute path matched the audit patterns.

Run `tools/AuditPublicRelease.py` against the source and publish folder. The audit also scans the project Release input assemblies because single-file compression can hide strings from a direct executable search. It rejects public PDB files and custom image metadata. Its scope excludes ignored private fixtures and runtime saves.

The 12 mock images already exist. Normal builds do not recreate them. The development generator remains available with `--verify`. Exposing the existing generator inside the app has a rough planning estimate of 5,000–10,000 additional AI tokens. This is not a measured count. Keeping the current embedded synthetic fixtures needs no additional generator feature.

## Measured checks

| Check | Latest result | Scope/status |
| --- | --- | --- |
| Isolated Core deletion tests | 13 passed | New deletion behavior only |
| Shown WPF deletion | 78 passed | Current/historical, owner routing, cancellation, file locks, retry, close recovery |
| Focused date-sort/preference follow-up | 19 passed | Actual header handler, refresh retention, failed deletion and saved automatic-read preference |
| Pure walkthrough model | 148 passed | Includes earlier model cases |
| Shown WPF Skip step | 64 passed | Includes shared setup |
| Broader shown onboarding/interactive workflows | 291 passed | Accepted readings injected through production app paths |
| Automatic scene policy | 18 passed | Separate policy cases |
| Native capture guards | 15 passed | Final controlled target, guards, and lifetime checks |
| Source/image privacy check | Passed | Eligible source and 13 public images |
| Complete Core suite | 248 passed, 0 failed | Includes the 13 deletion tests |
| Portable viewport regression | 4,433 passed, 0 failed | Stable window/card layout with the guide control |
| OCR-chain regression | 18 passed | Actual fixture pixels through the production OCR/app path, included in the broader 291 checks |
| Quiet foreground preflight | 9 passed | Separate shown WPF checks for waiting without busy churn |
| Release publish and compiled privacy audit | Passed | 113 text files, 13 images, 6 project Release assemblies |
| Published executable smoke test | Passed, exit code 0 | Copy of an isolated already-seen-guide profile, startup and normal close |

The deletion WPF report records the App DLL hash `C6B6773B8AADD3E9697F70013A06BB327473B61B45F72ADD445BE80678E402AE` and Core hash `D492110163B7BC3FB223A3D9C1B1DDCABF4780F21A3853BD83B50BC4BF2A0538`. These identify the tested Debug inputs, not the final Release executable. The quiet-preflight change has its own separate nine-check result.

The later focused follow-up loaded App hash `B7EED1E56B5E3A09BF84C17C1CC990AA38A5D9F3831DCCA344892445A1BCCB96` with the same Core hash. Its result identifies the date-arrow fix and corrected preference assertion. It does not replace the separate native-capture or final package identity.

The final focused WPF probe passed 19 checks after the date-sort arrow correction. The menu header remains unsorted, the date header shows the actual direction, and refresh preserves the chosen order. A forced deletion-save failure also preserves the saved automatic-reading preference. This probe uses the final functional Debug code. Earlier suite hashes remain identified separately.

## Package identity

| Field | Value |
| --- | --- |
| Executable | `WardogsXpTracker-0.3.7.exe` |
| File version | 0.3.7.0 |
| Product version | 0.3.7+3e4f35462ca243c086dd59ea71e47b6816b876fe |
| Bytes | 82,918,671 |
| SHA-256 | `E84551DD2AC2BEB36C4B4AB87D3586C2B63543826321DF645B65CF4C6E9AB756` |

This identity comes from the final published executable. The versioned executable and unversioned download alias match. The ZIP contains that same executable.

## Limits and acceptance

The app workflow suites used isolated profiles and shown WPF windows. Native capture tests used a controlled test window. No suite captured a live WARDOGS match or player save. The deletion checks used no physical game input, global hotkey registration, or audio.

Injected accepted readings test state and owner guards. They do not establish live-game capture or OCR accuracy. Live-game walkthrough acceptance remains pending. Unknown placement, missing classes, and unresolved XP sources retain their existing limits.

Season schema version 1, XP rules, the live target, and manual starting-coverage requirements remain unchanged. No automatic baseline certification follows from cumulative final XP or placement alone.

Full How To pages and tutorial screenshots remain future work. QA previews are test evidence. The onboarding reminder remains deleted. [GitHub issue #6](https://github.com/MadLicorice/wardogs-xp-tracker/issues/6) stays open for user acceptance. No GitHub release or final completion comment belongs to this documentation update.

## Saved Results icon follow-up: 0.3.8

The actions column now has a 42-pixel minimum width and dedicated cell padding. Its centered 28-by-28 button draws three vector dots, with a transparent normal state and the existing hover/focus feedback. No deletion or analytics logic changed.

The targeted shown WPF check passed 46 assertions at actual outer window widths of 816 and 1280 pixels, at 96 DPI. It checked cell/button/dot bounds, hover styling, popup binding, the red Delete action, the exact confirmation, default No, and unchanged memory/file data after No. It caught a WPF column shrink at the minimum window width; the explicit minimum fixes it.

Debug and Release builds completed without warnings or errors. The final published 0.3.8 executable started and closed normally in an isolated profile. This cosmetic follow-up does not repeat or replace the broader 0.3.7 suites or live-game acceptance. Issue #6 remains open.
