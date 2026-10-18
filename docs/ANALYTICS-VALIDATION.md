# Analytics, saved results, and mock workflow validation: 0.3.1

Date: October 17, 2026, America/Los_Angeles.

## Delivered behavior

Analytics is a separate tab with stacked source bars by class, a matching source pie, exact values, and separate Average Missed Bonus bars. Gray Indeterminate XP preserves received totals without guessing their source.

The report uses each session's saved rules, confirmed original starting coverage, actual ending evidence, and supported pre-bonus observations. Pending or incomplete records remain outside received totals. Rules mismatches preserve the observed total as unresolved XP. Missed bonuses require an explicit no-bonus outcome and never enter received totals or percentages.

Exports include the calculated report, derivation version, and original source season. Calculations do not alter historical inputs or live completion checks.

Saved Results keeps pending and confirmed matches available for later review. Decide Later hides the prompt and preserves the observed backup. Historical confirmation and correction target the selected record without changing a newer active session.

The placement choice states the observed evidence. Saved-match summaries retain known totals with explicit source and coverage uncertainty. Table selections remain dark without focus, and sort arrows identify typed numeric and date ordering.

## Checks

| Check | Result |
| --- | --- |
| Core tests | 220 passed, zero failed or skipped |
| Analytics and Test Tools integration checks | 86 passed |
| Actual WPF focus and sorting assertions | 115 passed |
| Generated mock PNGs through real OCR | All 12 passed in the integration sequence |
| Final App Debug build and Release publish | Zero warnings or errors |
| Saved-summary model and actual modal checks | 68 passed |
| Saved Results refresh and layout checks | 36 passed |
| Published single-file output | One executable, version 0.3.1.0 |
| Published startup and shutdown | Initialized local saves, reached message loop, normal close, exit 0 |

Core tests cover generic tier boundaries, exact and uncertain splits, no-bonus outcomes, conflicting evidence, incompatible rules, pre-bonus bounds, rebases, incomplete coverage, fractional bonus averages, revisions counted once, and pending-versus-confirmed class safety.

The new Core checks cover historical confirmation and correction, active-session isolation, match chronology, deferred review after restart, observed backups before manual choices, duplicate OCR, and schema-1 migration. A score-only read after an upgrade preserves earlier observed pending XP before a manual correction.

The integration checks cover source bars, source pie, exact tables, missed-bonus exclusion, empty charts, uncertainty details, and real-data isolation. They also cover all 12 real-OCR mocks, a disabled score-only Record action, deferred save/reload, historical confirmation, and a summary after saving. The active session remains unchanged during historical review.

The actual WPF table checks cover focused and unfocused selection colors, typed numeric and date sort paths, visible sort arrows, retained sort order after report refresh, and counts that exclude disabled role slots.

Summary checks cover exact and uncertain source splits, uncertified and partial-rebase intervals, manual correction, safe sums above Int64 limits, and unchanged inputs. The actual modal disables its owner until closure and keeps its close button visible within the work area.

The final layout checks render loaded app content at widths of 1,080 and 980. Detached-window previews were rejected after visual inspection. A loaded offscreen host confirmed readable headers and rows. Final UTF-8 labels were checked in the regenerated previews. Saved Results retains one sort description and the selected row through repeated default and user-sort refreshes. Long text and banners scroll vertically without horizontal overflow or native window resizing.

The earlier 0.3.0 completion preview API succeeded once. Its second UI run stayed silent. The supplied WAV uses the app's generated PCM audio. No new 0.3.1 audible-playback claim is made here.

The preview shows the expected synthetic received total:

```text
18,700 in-match XP + 15,100 bonus XP + 7,001 indeterminate XP = 40,801 received XP
Average Missed Bonus: at least 7,000 XP
Observed-tier placement range: 6,000–8,000 XP
```

The missed total is the sum of eligible record averages, with equal weight for first, second, and third place. It is not an average across matches.

## Isolated test data

The pack contains exactly 12 code-rendered mock PNGs and one synthetic season with 11 match records. It covers baseline readiness, initial progress, every tier, 2,499 versus 2,500, uncapped gains, a completed-page rejection, DEFEAT uncertainty, and pending final XP. The same images support rebase and manual-entry tests.

The fake season covers exact placements, uncertain placement and completion, confirmed exits, partial data, a rules mismatch, pending results, and retained original evidence after a rebase.

Mock 9 shows 3,000 in-match XP. Mock 10 intentionally rejects a completed page and preserves the previous value and target state. Mock 11 supplies DEFEAT without class final XP. Mock 12 supplies 62,986 final XP, a 6,500-XP difference that can include a bonus. It remains a separate pending result until confirmation.

Temporary loader buttons extract bundled files to `mock-data/0.3.1` below the local data folder. Mock session writes route there. The real season and preferences remain separate. Returning to real data restores their original state. Normal startup loads real data.

Diagnostic screenshot import remains diagnostic. The temporary walkthrough explicitly runs bundled mock pixels through the same local OCR and tracking paths.

Automated probes used isolated workspace profiles. User saves remained unchanged. The published executable initialized settings and a season, reached its Windows message loop, and closed normally with exit code 0.

## Build identity

```text
File: WardogsXpTracker-0.3.1.exe
Length: 82,867,662 bytes
File version: 0.3.1.0
Product version: 0.3.1+3e4f35462ca243c086dd59ea71e47b6816b876fe
SHA-256: 14A37A2952323C8A711BF90FF4DF7928DBBBDB1232DFD61012591E4E3FAF5AA7
Derivation version: xp-sources-1
```

Implementation changes remain local and uncommitted. The product-version suffix identifies the existing design commit. The SHA-256 identifies the exact executable tested here.

## Remaining work

Real match transitions and XP gains still need a live game test. Different display layouts and a clean Windows 11 installation also need tests.

The temporary test controls remain in this prototype. Onboarding must precede final tutorial screenshots. That dependency is recorded in `TUTORIAL-PLAN.md` and the quiet onboarding reminder.
