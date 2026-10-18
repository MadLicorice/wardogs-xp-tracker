# Capture Lab validation

This record covers the capture prototype. It does not certify the full Session Tracker or every display configuration.

## Initial build

- Package: self-contained Windows x64 `WardogsXpTracker.exe`.
- Size: 82,153,357 bytes.
- SHA-256: `A85C638B552B6FF6D4AD9CC835EB5111882DA76A9F9FC0D203121E7D3D233BD5`.

## Completed development checks

- The source builds with zero warnings and errors.
- The published output contains one executable.
- The published executable reached its Windows message loop.
- A render check loaded the WPF resources, controls, bindings, and initial layout.
- All 47 parser tests passed.
- All 15 supplied images passed their expected checks.
- The image checks cover six exact UNLOCK totals, two completed-page rejections, two final progression layouts, three placements, and two unrelated images.
- An external fixture window captured at 1920 × 1080 returned Assault 56,486 / 64,500, level 15.
- Missing and minimized window checks passed.

The 960 × 540 fixture did not establish a reliable OCR reading. The app rejected it.

## Diagnostic build 0.1.1

- Package: self-contained Windows x64 `WardogsXpTracker-0.1.1.exe`.
- Size: 82,165,412 bytes.
- SHA-256: `0E096FDA7D00D388A77B3C034E0887DAA87C33D44BDB7185A457E5050FF39FEB`.
- Startup check: reached the Windows message loop.
- Parser tests: all 49 passed.
- Image tests: all 15 passed with exact XP, thresholds, current levels, and next levels where those fields apply.
- Both six-card progression layouts now have all six exact current and next levels.
- Report export check: a local ZIP contains current and rejected attempts, metadata, diagnostics, and unchanged original image pixels.

This build fixes level metadata and adds diagnostic export. It does not establish that the live Driver XP error is fixed.

## Reader correction 0.1.2

- Package: self-contained Windows x64 `WardogsXpTracker-0.1.2.exe`.
- Size: 82,171,260 bytes.
- SHA-256: `5B6539AA927E18EBB28F0A93A8D3AB56383C7690112636C8088D06570274A8E6`.
- Source build: zero warnings and errors.
- Published output: one executable. Its startup reached the Windows message loop.
- Unit tests: all 70 passed.
- Image tests: all 17 passed, including both exact captured Driver frames from the report.
- Both Driver frames read 59,653 / 64,500, current level 15 and next level 16.
- The image checks retain every expected total, threshold, and level from both final-card layouts and all six original UNLOCK examples.
- Completed-page rejections, exact placements, and unrelated-image rejections pass.
- Synthetic crop checks reject text touching each of the four source edges. Whole text beside a full-height column border remains available.
- Report export preserves the original image pixels and includes version 0.1.2, analysis, and diagnostics for current and rejected attempts.

This corrects the reproduced Driver error on saved inputs. A fresh live-game test remains necessary.

## Recon correction 0.1.3

- Package: self-contained Windows x64 `WardogsXpTracker-0.1.3.exe`.
- Size: 82,171,575 bytes.
- SHA-256: `656DC6080D3F4E166975735ED85ACBE7404B7A32D295F6A016E8DAFFF3937390`.
- Source build: zero warnings and errors. The published output contains one executable and its startup reaches the Windows message loop.
- All 70 unit tests and 18 screenshot checks passed.
- Thirteen repeated saved-input checks passed: five Recon reads in one engine, five alternating Recon/Driver reads, and three fresh Recon processes.
- The new Recon frame reads 31,175 / 33,750, current level 11 and next level 12.
- Both Driver report frames and both final-card layouts retain their exact expected values.
- Synthetic clipping checks pass with both layout margins.
- Report checks pass for a success after rejection and a rejection after success. Both exported images retain their original pixels.

The user confirmed that version 0.1.2 reads Driver correctly in the live game. Version 0.1.3 still needs a fresh live Recon test.

## User's live WARDOGS test: October 16, 2026

The user launched the packaged executable and followed the live test steps with WARDOGS open.

The first F4 read for Recon failed. A second F4 read succeeded. The user then supplied app-panel screenshots for all six classes.

This establishes a successful live capture and F4 path on the user's machine. It does not yet establish repeated first-attempt reliability.

The screenshots and the user's confirmation establish:

| Class | Game XP | Initial app XP | Threshold | Initial app level | Expected level |
| --- | ---: | ---: | ---: | ---: | ---: |
| Assault | 56,486 | 56,486 | 64,500 | 5 | 15 |
| Medic | 58,891 | 58,891 | 64,500 | 5 | 15 |
| Recon | 31,175 | 31,175 | 33,750 | 1 | 11 |
| Support | 48,059 | 48,059 | 54,750 | 14 | 14 |
| Driver | 59,653 | 59,655 | 64,500 | 5 | 15 |
| Pilot | 44,702 | 44,702 | 54,750 | 14 | 14 |

The user explicitly confirmed that the game still shows Driver 59,653. The app's 59,655 is a false acceptance, not evidence of two newly earned XP.

The first Recon failure message is still unknown.

## Level metadata correction

The first reader trusted a single sidebar-level pass. It also used broad OCR for levels above end cards. A dropped leading digit could survive while the XP total was correct.

The first image check asserted XP and thresholds but did not assert level metadata. The new check requires the complete expected values.

The corrected reader uses focused glyph crops and separate consensus for current and next levels. It checks visible digit count, contrast, and clipped glyphs. An uncertain level stays unknown.

All six UNLOCK examples and both final progression layouts now have exact level metadata in the image tests.

## Confirmed Driver XP false acceptance

The user supplied `wardogs-read-report-20261016-215740.zip` from version 0.1.1. Its exact current frame shows Driver 59,653 / 64,500.

The accepted analysis contains these five passes:

| Preprocessing | Scale | Current XP | Word confidence |
| --- | ---: | ---: | ---: |
| Grayscale | 3 | 59,655 | 0.624 |
| Grayscale | 4 | 59,653 | 0.829 |
| Threshold 120 | 3 | 59,653 | 0.305 |
| Threshold 140 | 3 | 59,655 | 0.239 |
| Threshold 120 | 4 | 59,655 | 0.410 |

The old algorithm accepted three wrong readings over two correct readings. This reproduces the false acceptance from the actual captured input.

The retained rejected frame also shows current Driver XP, 59,653 / 64,500. It is an unsuccessful read of a current page. It is not a completed reward page or the original failed Recon attempt.

The report confirms that version 0.1.1 reads Driver's current level as 15. The original current frame has SHA-256 `51C2291DE537552A86335D54BEB41BB35BCDF0E27D2CDD33F2B2439F8BE46A4A`.

The correction isolates the text from column borders and background dots. XP enlargement uses bilinear interpolation to preserve thin strokes. Fixed thresholds no longer modify XP glyphs before OCR.

Text-sized components touching a source crop edge reject the crop. Full-height borders and small background dots do not count as text glyphs.

The new consensus checks every valid numeric pair before confidence filtering. Any conflicting current XP or threshold rejects the pair. At least two distinct preprocessing passes must agree and meet the 0.5 confidence floor. This floor is an acceptance rule, not a probability of correctness.

Each pass uses the minimum confidence across every recognized numeric digit. The ordered symbol digits must exactly match all digits in the raw recognized text, covering both XP fields. Missing or unmatched symbols cannot supply support. The nonnumeric XP prefix cannot lower the numeric score. Original word scores remain in diagnostics.

This avoids a separate tokenization failure on small final cards. All numeric readings can agree while the merged XP label receives a low word score. A high score for the threshold alone cannot hide an uncertain current-XP digit.

Unreadable passes do not vote. Duplicate pass identities cannot add support. The reader does not repair digits or infer an exact total from a level threshold.

## Intermittent Recon read

The user first reported a failed Recon read followed by a successful retry. Version 0.1.2 later showed repeated failures around one successful read.

The supplied `wardogs-read-report-20261016-222139.zip` contains one exact failed Recon frame. Its SHA-256 is `362A7B59C4A785357B826DC7718F2664FECE76B9A4125C8A998F088F85D032C3`.

The current progression column is visibly 31,175 / 33,750. The old passes produce:

| Scale | Recognized current XP | Recognized threshold |
| --- | ---: | ---: |
| 2 | 31,175 | 33,750 |
| 3 | 31,175 | 33,750 |
| 4 | 31,175 | 335,750 |
| 5 | 51,175 | 335,750 |

Rejecting these conflicting readings is correct. Controlled replays isolate the text margin as a cause of the larger-pass digit insertion on this frame. UNLOCK text now uses a one-pixel margin. Smaller gold final-card text retains its existing two-pixel margin.

The four scale passes, confidence floor, exact digit attribution, and conflict rejection remain in force. The reader does not discard a conflicting pass because another reading looks more plausible.

The old status was misleading. After the current column failed confirmation, a completed column remained, so the app claimed that only completed rewards were visible. Unstable numeric evidence now overrides both Accepted and NoCurrentXp with Ambiguous. Stable completed pages still receive NoCurrentXp.

The supplied report has no successful Recon frame for comparison. Saved-input replay does not establish why each live attempt differed. Do not attribute that pattern to animation, capture timing, or engine adaptation without evidence.

If the issue recurs, use **Save read report…**. Version 0.1.3 retains the latest successful and rejected attempts in memory. A report includes each distinct comparison attempt, even when a failure follows success. Images save only after the user chooses a report file. Nothing uploads automatically.

Useful distinction:

- A capture error concerns the selected window or Windows capture.
- An unsupported-screen result concerns page recognition.
- A missing-class result concerns the selected class.
- A current-XP or ambiguity result concerns the visible columns and number agreement.

## Remaining live checks

- Retest repeated Recon reads in the live game with version 0.1.3. The report frame and repeated-input tests use saved pixels.
- Retest the other corrected level labels in the live game.
- Preserve a report after any new failure so it includes the latest successful and rejected attempts.
- Check the two completed-page cases in the live game.
- Check automatic reading after manual capture succeeds.
- Test the end-of-match screens in a live match.
- Test other display settings and a clean Windows 11 installation before release.
