# MadLicorice's WARDOGS XP Tracker: agreed app design

Status: agreed product design, with prototype amendments through version 0.3.7.

Design record date: October 16, 2026.

Repository: [MadLicorice/wardogs-xp-tracker](https://github.com/MadLicorice/wardogs-xp-tracker).

This document records the user's accepted decisions. It separates those decisions from technical choices and release checks that still need work.

## 1. Purpose and release scope

The app tracks XP earned in each of six WARDOGS classes during one match. It compares current XP with a captured baseline.

The main goal is a clear signal that a class reached the maximum match-end bonus tier. Under the initial rules, this requires 2,500 in-match XP.

Match-end bonus XP never counts toward this target. The green check means the earned-XP target is complete. It does not confirm bonus receipt.

The first release is a Windows 10 and Windows 11 app. Users download an executable and double-click it.

The accepted stack is C#, .NET 10, and Windows Presentation Foundation (WPF). Publish a self-contained, single-file application with the runtime included.

Bundle the local OCR resources. Users must not install a development tool or a separate .NET runtime. Automatic extraction of support files is acceptable.

The app uses a separate window. An overlay is outside the first release. A browser version can follow later, after the Windows app works.

Offer a sound when a track reaches its target. Enable it by default for new profiles. Preserve the saved preference for existing profiles.

All screen reading, calculations, settings, and saves stay on the player's PC. The app needs no account, server, paid processing service, or automatic updater.

App development is not part of this document-saving task. No app code, framework scaffold, or language-specific `.gitignore` is required at this stage.

## 2. Terms and boundaries

| Term | Meaning |
| --- | --- |
| Track | One of Assault, Medic, Recon, Support, Driver, or Pilot |
| Baseline | A class's cumulative XP when its measurement starts |
| Active baseline | The latest accepted baseline that drives the class's live target counter |
| Original baseline | The retained initial baseline, with its own validity and coverage status |
| Current XP | A later accepted cumulative XP reading during the match |
| In-match XP | XP earned through player actions before the match-end bonus |
| Session | The app's record for one match, with explicit coverage limits |
| Rebase | Replace a class baseline and restart that class's measured gain |
| Closing XP | A class total captured after an early exit, before another match adds XP |
| Final XP | A class total observed on the match-end progression screen |
| Candidate results | Saved final observations that the user did not yet confirm |
| Ruleset | A dated version of the tier, bonus, and relevant level rules |
| Whole-match coverage | A baseline from before the first earned XP and a reliable ending total |
| Partial coverage | A measured interval that does not establish the whole match's XP |

Screen reading means screen capture plus optical character recognition (OCR). The player opens the game screens and controls the menus.

The app does not read game memory. It cannot read XP from a hidden page through OCR. It does not automate the game's menu clicks.

The six class totals are cumulative XP values. A level's denominator is a cumulative threshold, not XP still needed within that level.

WARDOG level is the aggregate progression track. Do not use it as a substitute for any of the six class measurements.

## 3. Pages and main layout

The app opens on **Session Tracker**. Keep the primary window concise and dark.

Provide separate navigation for these areas:

- Session Tracker
- Analytics
- Options
- How Bonus XP Works
- How to Use the App

The full app's Analytics charts have their own page and navigation button. Do not place those charts on the Session Tracker page. The current prototype has an Analytics readiness panel in the Session Tracker sidebar.

The tracker contains six class panels. Each panel shows the class, tracking toggle, baseline state, current gain, target progress, and completion state.

Arrange the panels in two columns and three rows to match the game's class selector:

| Left | Right |
| --- | --- |
| Assault | Medic |
| Recon | Support |
| Driver | Pilot |

Each panel also gives a compact conditional level forecast. An expanded view can show all three placement forecasts.

Show the selected season and the session's ruleset. Keep capture status and session actions easy to find.

### 3.1 Panel states

| State | Required presentation |
| --- | --- |
| Tracking disabled | Muted panel with an explicit disabled state |
| Tracking enabled, baseline missing | Clear red indicator and `Baseline not established` |
| Baseline accepted, no measured gain | Gray panel, distinct from the background, with `Baseline ready` |
| Gain below first tier | A clear progress state with the exact measured gain |
| Gain within a bonus tier | Faint yellow progressing toward green at each tier threshold |
| Maximum tier reached | Green panel treatment and a large green check |
| Read failed or page is wrong | Clear error with the next useful action |
| Mid-match rebase | Persistent minor warning beside the normal progress states |
| Frozen session | Finalization state that does not suggest live readings still count |

The maximum-tier check must remain distinct from bonus receipt. Use text and icons as well as color.

The rebase warning is **`XP rebased mid match. May not be accurate`**. It persists after the class reaches the green check.

Exact panel dimensions, colors, font sizes, and icons remain UI choices. Material UI is a visual baseline, not a requirement to use its web library.

## 4. Capture controls

Default controls are **F3: snapshot baseline** and **F4: check current XP**. Users can remap both controls.

The shortcuts must work while the game has focus. The app must report a shortcut conflict and let the user choose another binding.

F4 remains available even when automatic screen reading is enabled. A manual XP entry path remains available when OCR fails.

Automatic screen reading should detect supported XP pages and update accepted readings without F4. Its performance and reliability require a local prototype.

Automatic reading must also support the transient end screens where possible. F4 cannot be the only path to their capture.

No shortcut may silently replace an existing baseline during normal checks. F3 is active only for missing baselines in a new session or an explicit baseline operation.

### 4.1 New Session

1. The user selects **New Session**.
2. The app starts the session and identifies enabled classes without a baseline.
3. The user opens a class's current progression and pushes F3, or enters its baseline manually.
4. That class starts measuring as soon as the app accepts its baseline.

The user does not need all six baselines. Missing or disabled classes do not stop other classes.

There is no separate Start Tracking gate. F3 does not capture a baseline and lock the entire session in one action.

After a class receives its baseline, an accidental F3 must not replace it. Other missing classes remain eligible for setup capture.

The app identifies the class from the visible game page. A capture must not silently apply to a different class.

### 4.2 Reuse the last confirmed final XP

When the user starts a new session, offer this choice if the last known match has confirmed final totals:

> Final XP from last KNOWN match is already recorded. Would you like me to use that as your new baseline?

Choices:

- **Yes**
- **No, snapshot manually**

Reuse only observed and confirmed class totals. A class missing from the results screen does not acquire a guessed baseline.

The explicit choice matters because the player can play unrecorded matches. The app must not assume its latest known total is current.

If the user chooses manual snapshots, retain old values as history or references. Mark enabled classes without refreshed baselines red.

Predictions never supply actual final totals or new baselines.

### 4.3 Confirm baseline coverage

Fresh snapshots initially have **Coverage unknown** for analytics. Tracking starts without waiting for a coverage answer.

Place starting-baseline certification in the **Analytics** readiness panel on **Session Tracker**. The future Analytics charts remain on a separate page.

Offer a confirmation for each class's eligible active original baseline. The answer confirms that the baseline precedes any earned XP in that class. A rebase does not qualify as a new starting-baseline certification.

Also offer **Confirm All** with its own confirmation dialog. List the eligible captured baselines for enabled classes in that dialog. Confirm only those listed baseline versions.

Record which classes and baseline versions the answer covers. A later snapshot must not inherit an earlier coverage answer without evidence.

The panel stays red until all enabled classes have confirmed starting baselines. It then turns green with **Starting baselines confirmed**. This state confirms neither final XP nor bonus receipt. It does not establish complete analytics data.

Reusing a last-known final records its origin. It does not prove that the value is current.

The reuse choice can establish coverage when the user confirms these are this match's actual starting totals, with no intervening unrecorded match.

Explain this condition in the existing reuse choice. Otherwise retain unknown coverage and offer the existing coverage confirmation. Do not add a tracking gate.

A mid-match rebase creates partial coverage for the new live counter. It does not recover earlier XP from an incorrect baseline.

Keep a valid original baseline for whole-match calculations where it exists. Historical storage alone does not establish that a baseline is trustworthy.

### 4.4 Re-establish a baseline

Each class panel has a small **Re-Establish Baseline** button.

1. The user selects the button and confirms the rebase request.
2. The app keeps the current baseline active in memory and on disk.
3. A modal says `Record Assault XP now with F3!`, with the actual class and selected shortcut.
4. A valid new snapshot or accepted manual value replaces the baseline.

Confirmation alone does not erase or replace anything. Closing the modal, clicking away, canceling, or a failed capture restores the existing state.

Here, clicking away means dismissing the operation in the app. Merely returning focus to the game must leave the capture operation armed.

This distinction lets the player show the correct game page and use the global F3 shortcut.

The retained value can come from the last accepted snapshot or explicitly reused final XP. Preserve its history and origin.

Reject the wrong class or an invalid page while a targeted rebase is open. Do not replace the baseline with an uncertain OCR result.

After a successful mid-match rebase, measure gain from the new value. Do not add an inferred amount of earlier XP.

Keep the original baseline, replacement, timestamp, reason, and affected coverage in history. A normal target check can still turn green after rebasing.

An original trustworthy baseline can still support whole-match analytics after a rebase. Do not use an original baseline known to be wrong.

If the original baseline's validity is unknown, retain that uncertainty. The new interval alone cannot establish the whole match's gain.

## 5. Supported game screens and supplied evidence

The initial screen reader must support the UNLOCK class pages and the match-end PROGRESSION cards.

The end screen alone is insufficient for live checks. It only appears after a match, requires the PROGRESSION tab, and may omit classes without gains.

During a match, the player opens TAB, then UNLOCK. The default WARDOG track is not useful for class calculations.

The user selects one of the six class buttons on the left. The selected class is yellow in the supplied examples.

The selected reward column is not necessarily the current XP column. The app must inspect all visible progression columns.

### 5.1 Reference values from the supplied screenshots

| Class | Current level | Actual cumulative XP | Next cumulative threshold |
| --- | ---: | ---: | ---: |
| Assault | 15 | 56,486 | 64,500 |
| Medic | 15 | 58,891 | 64,500 |
| Recon | 11 | 31,175 | 33,750 |
| Support | 14 | 48,059 | 54,750 |
| Driver | 15 | 59,653 | 64,500 |
| Pilot | 14 | 44,702 | 54,750 |

These values are test references. They are not player defaults or evidence that another session has the same values.

### 5.2 Wrong-page examples

- Assault level 15 initially shows the completed reward value `54,750/54,750`.
- The next page shows actual Assault progress `56,486/64,500` toward level 16.
- Driver has the same completed-page trap. Its actual value is `59,653/64,500`.
- Recon highlights the completed level 11 reward at `28,750/28,750`.
- Recon's actual progress is in the next visible column: `31,175/33,750`, toward level 12.
- Support and Pilot also show completed columns beside the actual current column.

Reject a page that does not establish actual current XP. Tell the user to show the next progression page when needed.

Do not accept the first XP pair, the yellow reward, or a completed denominator as current progress by default.

Do not reject every equal numerator and denominator without context. A genuine level boundary or a level cap needs separate evidence and tests.

### 5.3 Capture acceptance

An accepted reading needs a class identity, a recognized screen type, and an exact integer XP value.

Check the visible level and threshold relationship where the screen supplies them. Keep completed rewards separate from current progress.

Uncertain, contradictory, or decreasing readings need review. Do not silently clamp a negative gain to zero or infer a season reset.

Record a rejected read's reason without adding it to earned XP. A rejected read must leave the last accepted value intact.

Manual entry must distinguish baseline entry, current-XP entry, and closing or final entry. Each has a different effect.

## 6. Earned-XP target calculation

For one class and the active baseline interval:

```text
measured_gain = current_cumulative_xp - active_baseline_xp
```

Under the initial rules:

```text
maximum_tier_reached = measured_gain >= 2,500
```

Example: an Assault baseline of 56,486 reaches the green check at 58,986 during the match.

The active baseline drives this check, even after a rebase. An original baseline can separately support the whole-match calculations described below.

The raw measurement, not a projection, drives this check. A bonus-inclusive final total must never create or advance a live green check.

A calculated split after the match also must not retroactively trigger a green check. Preserve the distinction between observed target completion and later analysis.

Keep an existing green check when the match ends. Record the ending bonus status separately.

Reaching the green check does not stop observations or cap actual earned XP. Only the bonus tier caps under the rules.

Keep later accepted readings, exact gains, timestamps, and forecasts current. A gain of 3,000 remains 3,000 in history, rather than 2,500.

## 7. Initial bonus rules and version history

Initial season: **Season 2**.

Initial ruleset name: **Oct 15 2026 Patch XP Bonus Rules**.

The initial table follows the user-supplied Twinsticks and Ties chart:

| Tier | In-match XP in this class | Third place bonus | Second place bonus | First place bonus |
| --- | ---: | ---: | ---: | ---: |
| Below Tier 1 | 0-499 | 0 | 0 | 0 |
| Tier 1 | 500-999 | 600 | 700 | 800 |
| Tier 2 | 1,000-1,499 | 1,200 | 1,400 | 1,600 |
| Tier 3 | 1,500-1,999 | 1,800 | 2,100 | 2,400 |
| Tier 4 | 2,000-2,499 | 2,400 | 2,800 | 3,200 |
| Tier 5 / Maximum | 2,500+ | 3,000 | 3,500 | 4,000 |

For these rules only:

```text
tier(E) = min(5, floor(E / 500))
bonus(E, third) = 600 * tier(E)
bonus(E, second) = 700 * tier(E)
bonus(E, first) = 800 * tier(E)
bonus(E, confirmed_no_bonus) = 0
```

`E` is nonnegative in-match XP for the class. The placement is the player's final team placement.

Future rules must use editable tier thresholds and bonus values. Do not assume every future patch uses 500-XP increments or five tiers.

The highest tier's threshold supplies that ruleset's green-check target. A future custom ruleset can change the current 2,500-XP target.

Store dated rulesets separately from season saves. A season can contain sessions under multiple patches.

Rulesets remain selectable in Options for preservation and comparison. A new patch does not erase an earlier ruleset.

Editing a saved ruleset creates a new version. Each session retains its exact immutable rules, rather than only a name that can change.

Keep that exact rules snapshot in the season save. Editing or deleting an external rules file must not change historical calculations.

Selecting a different ruleset affects new sessions. Existing sessions and their history retain the rules they used.

The ruleset's chosen patch name is user-supplied metadata. It is not proof that the supplied chart independently verifies that patch.

## 8. Level forecasts

Level forecasts are conditional estimates. They never become actual final results without an observed final total.

Keep the live counter separate from the gain that supports a whole-match bonus forecast:

```text
A = current_pre_bonus_xp - active_baseline_xp
G = current_pre_bonus_xp - valid_original_pre_XP_baseline
```

`A` drives the green check. `G` establishes the reached whole-match tier when the original baseline is valid and its timing is confirmed.

If that original baseline is wrong, untrusted, or of unknown timing, use only a supported lower bound. Do not reconstruct missing XP.

For each possible normal placement:

```text
projected_final_xp = accepted_current_pre_bonus_xp + bonus(supported_gain_G, placement)
projected_level = highest known level whose cumulative threshold <= projected_final_xp
```

Use the session's rules and known level thresholds. When `G` is only a lower bound, label the projection accordingly.

Example: a player earns 2,000 XP, rebases, then earns 500 more. The active counter shows 500 and does not turn green.

If the original baseline remains valid, the whole-match gain is 2,500. Bonus forecasts can use Tier 5 with the rebase warning still visible.

If that original baseline was wrong or lacked valid timing, the app cannot claim 2,500 from its stored value.

One compact card can say **Lv 16 with 2nd or better**. An expanded view gives the third-, second-, and first-place results.

Example with the initial rules:

| Medic measurement | XP |
| --- | ---: |
| Baseline | 58,891 |
| In-match gain | 2,500 |
| Current total before bonus | 61,391 |
| Third-place projection | 64,391, level 15 |
| Second-place projection | 64,891, level 16 |
| First-place projection | 65,391, level 16 |

The known level 16 threshold is 64,500. Do not assume the player earns no more XP after the latest reading.

Learn level thresholds from supported game screens and allow manual entries. Unknown higher thresholds produce **At least level X**.

The supplied screens establish thresholds for levels 11-20. They do not establish a complete level table:

| Level reached | Cumulative threshold |
| --- | ---: |
| 11 | 28,750 |
| 12 | 33,750 |
| 13 | 37,500 |
| 14 | 43,750 |
| 15 | 54,750 |
| 16 | 64,500 |
| 17 | 75,000 |
| 18 | 84,750 |
| 19 | 95,750 |
| 20 | 111,500 |

Store the source of observed or manual thresholds. Preserve the rule and threshold information used by a historical forecast.

## 9. Match-end detection and pending results

Detect match endings from supported end screens. Freeze the session's live earned-XP checks when a reliable end screen appears.

Collect placement evidence and final class XP independently. They can appear on different tabs or at different times.

Show this prompt:

> End of Match Detected. Record Final XP results?

Capture and save visible final XP in the background while the prompt remains open. The prompt must not block the reader.

Capture all visible class cards for candidate results, including classes without an active live counter. This does not enable their live tracking.

A disabled track or a missing baseline cannot supply a whole-match gain. Preserve its observed final total without inventing earlier XP.

Keep candidate results when the player is AFK, closes the prompt, or closes the app. Do not treat dismissal as confirmation or deletion.

Save both the old session state and candidate final totals. On restart, ask whether to use the updated totals or retain the old references.

Using updated results records actual observed final XP for that match. Offer confirmed totals as possible baselines only when the user starts a new session.

Retaining old references does not make them valid baselines for another match. Enabled classes require refreshed or explicitly reused values.

Missing result cards mean **Final XP unavailable** for those classes. Do not infer a missing class earned zero XP.

Keep the original baseline and all intermediate readings after confirmation. Preserve candidate revisions and user corrections with their sources.

Keep every candidate attached to its original session. Starting a new session must not attach a still-visible old end screen to the new match.

Order known matches by match chronology, not by a later confirmation timestamp. Confirming an older candidate must not replace a newer last-known match.

Do not let a later match's readings advance the frozen session. A new session requires an explicit session action.

Associating screens across AFK periods or multiple matches requires a careful design. Do not silently merge conflicting evidence into one match.

If the app misses the entire match transition, OCR alone cannot prove which match a later UNLOCK reading belongs to.

Show the current session context and explain that the player must select New Session for each match. Do not invent an automatic match association.

## 10. Placement evidence

| Visible evidence | What it establishes |
| --- | --- |
| VICTORY | First place |
| DEFEAT without a contribution rank | Second or third place |
| `1ST PLACE + CONTRIBUTION` | First place |
| `2ND PLACE + CONTRIBUTION` | Second place |
| `3RD PLACE + CONTRIBUTION` | Third place |
| Winning faction splash | Winning faction only, not the player's placement |
| No recognized outcome | No reliable placement evidence |

Players can switch teams. Do not rely on a remembered faction color to establish placement.

The user can optionally enter **1st / 2nd / 3rd / Unknown** when confirming results. Preserve manual and observed sources separately.

Accumulate compatible evidence from the end sequence. More specific evidence can reduce uncertainty. Contradictory evidence needs review.

The app cannot assume it sees every end screen. Players can switch tabs before the placement text appears.

## 11. Early exits, disconnects, and absent outcomes

**End Session** offers **Finished / Left Match / Disconnected / Unknown**.

A temporary disconnect can resume the same session. Keep its baselines available unless the player explicitly ends or replaces that session.

Losing capture or closing a game page does not establish an outcome. Missing results never mean zero bonus.

Explicitly ending as **Left Match** or **Disconnected** records a user-reported no-bonus outcome under the agreed model.

A complete normal match with no placement evidence can still have an unknown placement. An unknown completion can also include no bonus.

After an early exit, offer **Record Closing XP** through UNLOCK class pages or manual entry. This action is optional.

Closing totals do not replace starting baselines. Ask the user to capture them before earning XP in another match.

Without closing totals, preserve the last readings and mark coverage incomplete. Do not imply they capture all XP earned before the exit.

Examples of distinct statuses:

- **Target reached · Bonus received**, when receipt is supported.
- **Target reached · No bonus received**, after a confirmed no-bonus exit.
- **Target reached · Bonus unknown**, when the outcome remains unresolved.

## 12. XP-source analytics and uncertainty

The Analytics page contains stacked bars by class and a pie chart for recorded XP sources.

The received-XP categories are **In-match XP**, **Bonus XP**, and gray **Indeterminate XP**.

Default analytics use confirmed records with valid coverage. Show coverage counts and incomplete records separately.

Keep partial observations available without presenting them as whole-match totals. Missing data is not zero.

### 12.1 Determine possible splits

With a reliable whole-match baseline and final total:

```text
total_gain D = final_xp - whole_match_baseline_xp
D = in_match_xp E + bonus(E, outcome)
```

Find nonnegative integer solutions that obey the exact session rules and supported outcome evidence.

Supported normal placement narrows the outcomes to that placement. DEFEAT narrows them to second or third.

When completion and placement are unknown, include no bonus, third, second, and first where they fit the observations.

Use reliable observations to constrain solutions. Keep a rebase interval separate from the original whole-match baseline.

If no solution fits, flag the mismatch. Preserve the observed total and mark the split unresolved rather than inventing source amounts.

A result calculated from the rules is labeled **Calculated using bonus rules**. It is not a direct observation of a bonus transaction.

### 12.2 Preserve the common minimums

For the set of valid splits:

```text
minimum_in_match = min(E across valid splits)
minimum_bonus = min(B across valid splits)
indeterminate = D - minimum_in_match - minimum_bonus
```

The gray segment is the amount that can move between the two sources. Do not choose a placement merely to make a chart definite.

For a 6,500-XP increase under the initial rules:

| Outcome | Possible in-match XP | Possible bonus XP |
| --- | ---: | ---: |
| Third place | 3,500 | 3,000 |
| Second place | 3,000 | 3,500 |
| First place | 2,500 | 4,000 |
| Confirmed no bonus | 6,500 | 0 |

With **DEFEAT** and no exact placement, chart 3,000 in-match, 3,000 bonus, and 500 Indeterminate XP.

With unknown completion and no placement cue, chart 2,500 in-match, zero confirmed bonus, and 4,000 Indeterminate XP.

With confirmed no bonus and complete coverage, chart all 6,500 as in-match XP.

An information icon explains the uncertainty. For the second-versus-third case:

> This XP could be either in-match XP or bonus XP because second or third place was not recorded.

Use a different explanation when completion is unknown or coverage is incomplete. Do not describe every uncertainty as second versus third.

### 12.3 Charts and totals

Stack each class's received XP by the supported source amounts. Aggregate the same amounts into the pie chart.

Show each slice's percentage of the recorded total. Keep the scope and coverage visible so the chart does not imply a complete season.

Do not turn a prediction into actual earned XP. Do not use a modeled split to retroactively complete live targets.

Preserve input observations and the derivation version. Future analysis must be able to inspect or recalculate derived values.

## 13. Average Missed Bonus

Record missed bonuses only for sessions confirmed to receive no bonus. Unknown outcomes do not count as confirmed misses.

Use the class's reached tier and average the bonuses for third, second, and first place.

Determine the reached whole-match tier from `G` when its original baseline is valid. Otherwise use only the supported observed lower bound.

The active counter's rebase must not erase valid earlier evidence. It must not make an incorrect original baseline trustworthy either.

```text
minimum_missed_bonus = min(third_bonus, second_bonus, first_bonus)
average_missed_bonus = (third_bonus + second_bonus + first_bonus) / 3
maximum_missed_bonus = max(third_bonus, second_bonus, first_bonus)
```

This average weights the three placements equally. It does not use the player's past placement frequencies.

Under the initial rules:

| In-match gain | Reached tier | Minimum missed | Average missed | Maximum missed |
| --- | --- | ---: | ---: | ---: |
| 0-499 | Below Tier 1 | 0 | 0 | 0 |
| 500-999 | Tier 1 | 600 | 700 | 800 |
| 1,000-1,499 | Tier 2 | 1,200 | 1,400 | 1,600 |
| 1,500-1,999 | Tier 3 | 1,800 | 2,100 | 2,400 |
| 2,000-2,499 | Tier 4 | 2,400 | 2,800 | 3,200 |
| 2,500+ | Tier 5 | 3,000 | 3,500 | 4,000 |

Example: Recon earns 3,000 XP, then the player leaves without a bonus. Record an average missed bonus of 3,500 XP.

Example: a class earns 1,200 XP, then the player leaves. Record an average missed bonus of 1,400 XP.

Save minimum, average, and maximum estimates. Show **Average Missed Bonus** in separate class bars and a separate total.

Missed XP never contributes to received-XP totals or pie percentages. It is an estimate of an opportunity the player did not receive.

Count it once per class per session. Later observations or corrections revise that record instead of adding duplicate missed bonuses.

With incomplete tracking, observed gains can establish a minimum tier. Show **At least...** while a higher tier remains possible.

After an observed gain reaches 2,500 under these rules, the average is 3,500 even without a later closing scan.

Keep the known interval, coverage status, outcome evidence, and rules version beside the estimate in saved history.

## 14. Seasons, files, and restarts

Each season has its own readable `.md` save. Start with Season 2.

Users can add and rename seasons without an app update. A new season starts without XP data.

Keep season baselines, sessions, pending results, history, and class toggles separate. Share hotkeys and appearance through global settings.

Do not automatically carry XP between seasons. Do not infer a new season from a decreasing OCR result.

Switching seasons preserves unfinished work. Returning to a season offers its saved session or a new session.

On app launch, offer **Resume Saved Session** and **New Session** when no pending final decision takes priority.

Pending results take priority. Restore both the old state and saved candidates, then ask the user which data to use.

Resume retains the same session baselines and readings. The app does not assume the same game match continued while it was closed.

Keep rules in separate readable files with dates and versions. Exact file names, record syntax, and the save directory remain implementation choices.

### 14.1 Save requirements

The format must remain readable for a person and stable for a parser. Include a schema version and durable identifiers.

Save important state changes promptly, including candidates before confirmation. Preserve data if a rebase or result dialog closes unexpectedly.

Use safe replacement or an equivalent recovery method so a crash does not leave the sole season save truncated.

Reject or report malformed data without silently replacing history. Future format changes need a migration or clear compatibility behavior.

Append history records where practical. Avoid a full large-file rewrite for every screen frame.

Run disk work outside the UI thread. Record changed readings and relevant transitions rather than every identical frame.

Do not save screenshots by default. Save the accepted values, source metadata, and relevant rejected-read reasons.

Per-season files reduce file growth. There is no agreed file-size cutoff. Treat 50 MB as a review checkpoint, not a proven limit.

Measure actual parse, save, and UI latency with representative files before choosing a warning or archive policy.

## 15. Data required for future analysis

Keep original observations distinct from user corrections and calculated values. Retain enough evidence to explain each chart value.

| Record area | Required information |
| --- | --- |
| File | Schema version, app version, season ID and name |
| Rules | Stable ruleset ID, version, patch date, label, exact tier and bonus values |
| Session | Stable session ID, season, rules snapshot, start and end times, lifecycle state |
| Class choice | Enabled state and changes to that state |
| Baseline | Class, exact XP, baseline version, timestamp, origin, validity, validity reason, related prior result |
| Coverage | User's before-XP confirmation, covered baseline versions, partial or unknown coverage |
| Reading | Class, exact XP, visible level and threshold, timestamp, screen type, accepted or rejected state |
| Source | Automatic OCR, F3 capture, F4 check, manual entry, or reuse of confirmed results |
| OCR evidence | Relevant recognized text, confidence if available, validation result, failure reason |
| Rebase | Old and new baseline, request and commit time, reason, cancellation or commit state |
| Outcome | Finished, Left Match, Disconnected, Unknown, plus manual or observed source |
| Placement | All relevant observed cues, manual answer, uncertainty, conflicts, and timestamps |
| Candidate results | Class totals, detection time, acceptance state, revisions, and links to the session |
| Final or closing XP | Exact per-class total, source, timestamp, confirmation, missing-class state |
| Target | Tier reached, threshold, observation that established it, timestamp, rebase warning |
| Calculation | Active gain, supported whole-match gain or lower bound, total delta, possible splits, common minimums, indeterminate amount, method and rule version |
| Missed bonus | Minimum, average, maximum, exact or lower-bound status, supporting outcome and coverage |
| Correction | Previous value, replacement, source, timestamp, affected calculations |

Store timestamps with enough information to order events and recover local display times. Do not imply exact XP-event timing between scans.

A received-XP total can be known while its source split is not. A partial record can also lack a trustworthy whole-match total.

Those are different conditions and need separate saved states. Unknown values must not become zeros.

Preserve ended sessions after a new session starts. Future analytics can compare classes, outcomes, patches, and seasons from local records.

## 16. Help and credit

### 16.1 How Bonus XP Works

Write this section from the agreed rules. Explain that each class earns its own tier from in-match XP.

Explain the 500-XP tiers, current 2,500-XP maximum, placement bonuses, and separation between target completion and bonus receipt.

Include worked examples, early-exit behavior, uncertain placement, and the meaning of Indeterminate XP and Average Missed Bonus.

Include the supplied chart with obvious adjacent credit:

> Bonus XP research and chart: [Twinsticks and Ties](https://www.youtube.com/@twinsticksandties) — Field Report 008.

The source image identifies **Season 1 — reward model from our tests**. Preserve that source context beside the chart.

The app's initial ruleset label is **Oct 15 2026 Patch XP Bonus Rules**. Keep that selected label distinct from the chart's original context.

The chart asset can enter the repository during app work. The design record does not require copying screenshots or adding runtime assets now.

### 16.2 How to Use the App

Create the app tutorial after the UI is final. Use screenshots of the actual app rather than speculative interface images.

Complete the onboarding workflow before making tutorial screenshots. See [Onboarding and tutorial plan](TUTORIAL-PLAN.md) for the dependency and current status.

Cover new sessions, optional tracks, correct UNLOCK pages, wrong completed pages, capture keys, manual entry, rebases, and baseline reuse.

Also cover final-result confirmation, pending results after restart, early exits, seasons, rulesets, and the separate Analytics page.

### 16.3 Future analytics text

Use this text in Options:

> Further analytics are planned for a future season. You can also further analyze your local save files.

Basic class bars, the received-XP pie, and missed-bonus bars are part of the initial agreed scope.

## 17. Technical choices still open

These choices do not change the agreed behavior. Resolve them during implementation and record material decisions.

| Choice | Required constraint |
| --- | --- |
| OCR engine | Local operation, bundled resources, exact-number acceptance checks |
| Capture API | Read the visible game without game-memory access or menu automation |
| Supported display modes | Test screen dimensions, scaling, fullscreen modes, and multiple monitors |
| Automatic read cadence | Catch useful screens without material game or UI slowdown |
| Scene and class detection | Reject unsupported or ambiguous pages safely |
| Result association | Keep separate matches and conflicting candidates distinct |
| File layout and syntax | Readable Markdown, stable schema, season separation, recoverable writes |
| UI styling library | WPF-compatible dark style with clear states and concise panels |
| Packaging | Simple executable with the runtime and local OCR resources included |
| Release distribution | Free download hosting and a repeatable package process |

A free hosting provider is not selected. Repository setup does not authorize publishing a release or creating an automatic update service.

The app should not claim detailed action categories such as kills or repairs. The agreed data only distinguishes in-match XP from bonuses.

## 18. Acceptance checks before release

These checks are release requirements, not claims that a prototype already passes them.

### 18.1 Capture and target checks

- Read all six supplied reference totals exactly.
- Reject Assault's and Driver's completed-page traps without replacing a baseline.
- Read Recon's current value from the next visible column despite the highlighted completed reward.
- Preserve existing state after a failed or wrong-class capture.
- Reach the initial target at exactly 2,500 observed in-match XP.
- Exclude match-end bonus totals and calculated splits from live completion checks.
- Support manual entry and remapped keys while the game has focus.
- Test exact level boundaries, maximum levels, unsupported layouts, and low-confidence digits.

### 18.2 Session and recovery checks

- Start individual classes immediately without requiring all six baselines.
- Preserve disabled-track choices and distinguish disabled from missing-baseline states.
- Require the explicit last-known-final choice before baseline reuse.
- Cancel a rebase without losing the old baseline or changing earned XP.
- Commit a rebase only after valid replacement input, then retain its warning.
- Freeze live checks at a reliable end screen while background result capture continues.
- Recover unconfirmed candidates and the original state after restart.
- Retain missing class results as unknown.
- Resume temporary disconnects without inventing a no-bonus outcome.
- Separate seasons, preserve unfinished sessions, and keep exact historical rules.

### 18.3 Analytics checks

- Match the initial bonus table at every threshold and immediately below it.
- Calculate the 6,500-XP placement examples and common minimums correctly.
- Include no bonus among unknown outcomes when completion is unconfirmed.
- Distinguish inconsistent rules from supported uncertainty.
- Show missing and partial coverage without treating unknown values as zero.
- Calculate the 1,400- and 3,500-XP average missed-bonus examples.
- Keep missed XP outside received totals and pie percentages.
- Count missed bonuses once per class per session and update them after corrections.
- Keep Analytics charts on a separate page from the default tracker.

### 18.4 Packaging and file checks

- Run the packaged executable on Windows 10 and Windows 11.
- Run without a separately installed .NET runtime, OCR package, or development environment.
- Keep reading and calculations local without an account or a paid service.
- Recover from an interrupted save without silently losing season history.
- Test representative large season files without blocking the UI.
- Include the required help, obvious creator credit, and the final UI tutorial screenshots.

## 19. Accepted prototype UI amendments: 0.2.1

The earlier sections describe the agreed full app. This section records the 0.2.1 UI decisions. Sections 20 through 22 update the prototype scope.

### 19.1 Name, logo, and navigation

Use **MadLicorice's WARDOGS XP Tracker** as the app name. Use the supplied WARDOGS logo without changing its image bytes.

Keep the six tracker cards in two columns and three rows: Assault/Medic, Recon/Support, and Driver/Pilot.

Use **Diagnostics Lab** for screenshot import, frame previews, OCR text, and read reports. Imported screenshots remain diagnostic and never change session state.

Place an **Analytics** readiness panel in the Session Tracker sidebar. It contains starting-baseline certification per class, plus **Confirm all captured baselines…** with a separate confirmation.

In 0.2.1, charts remained later work on their own Analytics page. The readiness panel's green state means only that the enabled starting baselines are confirmed. Final totals, bonus receipt, and complete analytics coverage remain separate.

When no session is active, cover the card area with a **New Session** prompt inside the app. Offer **Resume Saved Session** there when appropriate. This is an in-app session prompt, not a game overlay.

### 19.2 Defaults and save controls

Enable the completion sound by default for new profiles. Preserve the user's saved preference when an existing profile loads.

Save accepted readings and relevant state changes automatically to local files. Do not require a manual Save action during normal use.

Show **Retry save** only after a save failure. Keep the failure message and recovery action visible until the save succeeds.

### 19.3 Prototype boundaries

The prototype supports session tracking, guarded baseline capture, current XP checks, manual entry, rebases, pending finals, local history, seasons, shortcuts, and sound.

Version 0.2.1 did not include analytics charts, the complete How To pages, or a rules editor. Section 20 updates that prototype scope.

## 20. Analytics and temporary testing: 0.3.0

### 20.1 Separate Analytics tab

Show charts on a separate **Analytics** tab. Keep the starting-baseline readiness panel in the Session Tracker sidebar.

The Analytics tab includes native class bars, a source pie, exact values, coverage counts, and record explanations. Green shows supported in-match XP. Gold shows bonus XP. Gray shows **Indeterminate XP** whose source remains unresolved.

The class bars and pie use the same included received totals. Pending candidates and incomplete records remain separate. Preserve known totals with source uncertainty rather than assigning them a guessed split.

Show **Average Missed Bonus** outside received totals and the pie. Calculate each eligible record's average with equal weight for first, second, and third place. The displayed total sums those record averages. Use **At least** when incomplete evidence supports only a lower bound.

Retain schema version 1 for existing season saves. Derive analytics from the saved observations and each session's rules snapshot. **Export analytics…** includes the calculated report, derivation version, source season records, and a mock-data marker where applicable.

### 20.2 Temporary Test Tools

Section 24.2 supersedes the temporary status and visibility rules in this historical subsection.

Provide **Test Tools · temporary** for development checks without the game. Bundle exactly 12 mock PNGs, below the agreed maximum of 15. These are code-rendered functional OCR fixtures, clearly marked as mock data.

**Load fake screenshots / restart** creates an isolated mock session and establishes the Assault baseline automatically through the real local OCR reader. Only Assault is enabled initially. The app returns to Session Tracker after that baseline read.

Show **Next mock + read** in the header while screenshot mode is active. It advances the selected mock image and reads it while Session Tracker stays visible. Test Tools also contains a selector and **Read selected mock**.

The sequence covers the baseline, positive XP, every bonus tier, 2,499 versus 2,500 XP, extra XP, a rejected completed page, DEFEAT, and final progression. Reuse the same images for rebase, manual-entry, and end-outcome checks. Reset the walkthrough before another full tier sequence.

While screenshot mode is active, F3 and F4 read the selected bundled image. They follow the normal snapshot and check rules. No real game window is captured in that mode.

**Load fake stats** loads 11 synthetic session records into Analytics. It does not replace the active tracker. Mark fake analytics clearly and keep its export distinct from real data.

### 20.3 Test sound and isolation

The mock target-sound checkbox defaults to enabled and is independent of the real sound preference. **Play completion sound** plays one preview without changing XP or preferences.

Write mock sessions and extracted test files only within the versioned `mock-data/0.3.0` subfolder of the local save directory. Preserve real season records and preferences.

**Return to real data** restores the real tracker and normal preferences, then clears the fake analytics view. Normal startup loads real data. Saved mock files remain separate. The user restarts the mock walkthrough explicitly with **Load fake screenshots / restart**.

### 20.4 Onboarding before tutorial screenshots

Agree on and implement onboarding before making the screenshot tutorial. The user deleted the associated reminder on October 17, 2026. Section 22.3 records its current status.

The full How To pages, rules editor, onboarding workflow, and screenshot tutorial remain incomplete. See [Onboarding and tutorial plan](TUTORIAL-PLAN.md) before tutorial work.

## 21. Saved results and result review: 0.3.1

### 21.1 Permanent Saved Results tab

Keep a permanent **Saved Results** tab beside the tracker and Analytics. Show pending and confirmed records for the selected season there.

Pending results remain reviewable after another session starts or the app restarts. Confirmed results remain available for later inspection.

**Decide Later** hides the current end-result prompt and saves that decision. It preserves the pending totals, earlier references, and observed backups.

The player can reopen the deferred result from Saved Results. Dismissal does not confirm it, delete it, or establish a baseline for another match.

Review actions target the selected historical session by its identity. Confirming or correcting that result must not select it as the active session.

Keep the newer session's baselines, readings, completion checks, and capture context unchanged. A later confirmation date must not replace a newer last-known match.

### 21.2 Observed backups and placement choices

Preserve structured copies of changed observed final totals and placement evidence before manual corrections or confirmation choices alter the candidate.

Keep earlier observed revisions separate from later observed revisions and user choices. Repeated OCR of unchanged values must not add duplicate backups.

Store these backups as an optional addition to schema version 1. Existing saves that lack the list must still load.

Before changing a legacy pending result, preserve its available observed values. A score-only read after an upgrade must not discard its earlier XP evidence.

The first placement choice starts with **Use observed**. Its label states what the evidence supports, such as first place, second or third, or unknown placement.

Do not label observed DEFEAT evidence as wholly unknown. Do not claim an exact placement when only second or third is known.

Keep manual first-, second-, and third-place choices available. Preserve the observed placement and the manual choice with their separate sources.

A score-only result can freeze live tracking and preserve placement evidence. Recording final XP requires at least one observed or explicitly entered class total.

### 21.3 Blocking saved-match summary

Section 22.4 supersedes the verbose modal presentation in this historical amendment.

After saving confirmed results, show a modal summary that requires dismissal. The player can also reopen a saved match's summary from Saved Results.

Show each available starting total, ending total, and recorded difference. Show missing class totals as unavailable rather than zero.

Use the session's saved rules and supported evidence for the source split. Show in-match XP, bonus XP, and gray Indeterminate XP separately.

When starting coverage is unconfirmed, show the known difference with its coverage limit. Keep its source unresolved instead of showing a false zero total.

This summary does not certify incomplete coverage or add excluded records to the Analytics charts. An invalid or negative difference requires review.

Explain each uncertainty from its actual cause: placement, unknown completion, incomplete coverage, conflicting evidence, or incompatible bonus rules.

Keep known totals visible when their source remains uncertain. A summary or calculated split must never advance a live target or play its completion sound.

### 21.4 Table states and stable width

Section 22 supersedes the width cap and inactive-selection behavior in this historical amendment.

Use the dark theme for table selections with and without keyboard focus. Keep selected text legible when the player uses another control.

Show a visible sort arrow in each sorted column header. Sort numeric values and dates by their underlying values, not their formatted display text.

Keep the main window at a stable maximum width of 1,080 WPF device-independent pixels. Tab changes must not expand it beyond that limit.

Clamp the initial size to the available work area. Keep lower content reachable through scrolling when display scaling reduces the available space.

### 21.5 Clear mock labels

Keep the 12-image walkthrough and its labels explicit about what each image tests.

| Mock | Required meaning |
| --- | --- |
| 9 | Assault has 59,486 current XP, which is 3,000 above the initial baseline. The target already remains complete. |
| 10 | This is an intentionally invalid completed page. Reject it and retain the baseline, last accepted reading, and target state. |
| 12 | Assault has 62,986 final XP, which is 6,500 above the initial baseline. This final total can include a bonus. |

Mock 11 supplies DEFEAT placement evidence without final class XP. Mock 12 supplies the final total for a separate confirmation.

The 6,500-XP final difference must not become another live earned-XP reading. Keep the observed in-match lower bound and unresolved source split visible.

## 22. Responsive layout and table interaction: 0.3.2

### 22.1 Free resizing with stable bounds

Section 23 supersedes this subsection's 690-unit card surface, minimum-height assumptions, and sidebar-below-cards layout. Keep this section as the historical 0.3.2 design.

Remove the 1,080-width cap from section 21.4. Let the player resize or maximize the app to 1920-wide windows and beyond.

Use three profiles based on the root's available content width, measured in WPF device-independent units:

| Available content width | Layout |
| --- | --- |
| Below 1,100 | Two columns and three rows of cards, with the sidebar below |
| 1,100 through 1,599 | Cards beside a fixed 360-unit sidebar |
| At least 1,600 | Cards beside a fixed 420-unit sidebar |

Preserve the card order across profiles. Use the window's available space and keep lower content reachable through vertical scrolling.

Within a profile, text and state changes must not move or resize the card area, individual cards, or sidebar.

Keep the six-card area at 690 units high. Reserve bounded areas for session status, analytics readiness, mock instructions, and rejected-read feedback.

A rejection, deferred result, or pending final replaces content inside its reserved area. It must not push the cards downward.

Bound long status, forecast, and warning text. Use deliberate ellipsis and a full tooltip where needed. Keep tab labels and primary button captions readable.

Keep the initial size within the available work area. The initial clamp must not prevent later resizing to a larger display.

### 22.2 Table focus, scrolling, and arrows

Keep active table selection dark and legible. Clear the selection after an outside pointer action, focus change outside the table, or window deactivation.

Clicks on a table header, resize grip, or row action remain inside the table. Historical row actions retain their own record identity.

Use a filled vector arrow for the current sort direction. Retain numeric and date sorting and preserve the user's sort order during report refresh.

The mouse wheel first scrolls the table when that table has room in the requested direction. At its boundary, continue through the nearest scrollable parent.

Do not trap wheel input inside a short table or at the end of a longer table.

### 22.3 Onboarding and reminder status

On October 17, 2026, the user explicitly deleted **onboarding-before-the-tutorial**. No reminder automation remains active. Do not create a replacement without a new explicit user request.

Onboarding remains future work and a prerequisite for the finished screenshot tutorial. These UI corrections do not start either workflow.

### 22.4 Compact recorded-XP summary

After a successful final save, show a blocking dialog headed **MATCH XP RECORDED**. Show the known total in large text.

Show supported earned XP in green, bonus XP in gold, and Indeterminate XP in gray. Unknown source amounts remain unknown. Supported parts and unresolved XP must account for the known total.

Give only brief rows for classes with a known measured increase. Omit missing-class rows, cumulative baselines, placement, rules, and long explanations from this dialog. Keep the saved evidence and detailed analytics available elsewhere.

The dialog closes with **Continue**. It must not change session data or trigger completion sounds.

### 22.5 Waiting for final XP

A score-only end screen can establish placement and freeze live tracking. It does not contain final class XP.

Until final totals exist, label the disabled action **Waiting for final XP**. Explain that the player must show the match-end PROGRESSION screen and capture its class totals with the current check shortcut.

In mock 11, tell the player to use **Next mock + read** for mock 12. After that capture, enable **Record Final XP**. Recording confirms existing captured totals and does not capture a new frame.

## 23. Finite Session Tracker viewport: 0.3.3

### 23.1 Supported short-window target

Section 24.1 supersedes this subsection's minimum dimensions. The validation in section 23.5 describes version 0.3.3.

The user reported that the 0.3.2 tracker remained too tall, the page moved when Quick Guide opened, and read errors appeared below Analytics.

Target actual native outer windows of 1280 × 648 and 1920 × 648 physical pixels at 96 DPI. These are app-window constraints, not new OCR game-layout resolutions. Windows scaling changes the available space in WPF units.

The minimum window size must not enlarge a supported requested window. Keep outer bounds fixed during reads, tab changes, status updates, and guide expansion. The player can still resize or maximize the app.

Use a 640-unit minimum height. Also check 780 × 640 and 1280 × 640 native windows at 96 DPI, so the minimum does not clip card actions.

### 23.2 Finite tracker layout

Session Tracker uses a finite client viewport. Keep the brand and capture controls compact. Give the main tracker body the remaining bounded space instead of measuring it as an app-wide scrolling document.

Retain the two-column, three-row class order: Assault/Medic, Recon/Support, Driver/Pilot. Use compact cards with readable XP and working baseline/manual actions. Remove the fixed 690-unit surface. A large window must not turn the cards into oversized blank panels.

Keep the card grid and inactive overlay within the same bounded card region. Showing a rejection, pending result, or deferred result must not resize or move that region.

### 23.3 Pinned read status and independent sidebar

Read status belongs above the main card body and Analytics. Reserve that area so a rejected read does not push the cards down. The error must be visible without scrolling through Analytics.

Routine session feedback uses the same strip. A global shortcut registration conflict must appear in visible feedback, not only in a tooltip.

Keep the sidebar beside the cards at the supported normal window widths. Its own bounded scroll viewer contains secondary status, analytics readiness, and Quick Guide. Expanding the guide may change that scroll area's extent or offset. It must not change card bounds, overlay bounds, or the whole page's offset.

Session Tracker has no app-wide vertical scroll range. Entering it from a deeply scrolled tab must not retain an old page offset. Preserve ordinary keyboard navigation and local bring-into-view behavior; do not suppress it globally.

### 23.4 Other pages and unchanged data rules

Analytics, Saved Results, Test Tools, Diagnostics Lab, and Options keep page scrolling. Preserve the shared table selection clearing, filled sort arrows, typed sorting, and inner-to-parent wheel handoff from section 22.2.

This layout change does not alter XP reading, baselines, the earned-XP target, bonus rules, final confirmation, or local-save schema version 1. Match-end bonuses remain separate from the live target check.

### 23.5 Native-window validation

The shown native-window regression passed 4,367 checks with zero failures. Actual 1280 × 648 and 1920 × 648 outer windows at 96 DPI retained the requested height and had a 609-unit client viewport.

Track the correction in [GitHub issue #3](https://github.com/MadLicorice/wardogs-xp-tracker/issues/3).

The checks covered all six baseline/manual actions within their cards and the viewport, pinned read errors, inactive overlays, pending-final buttons, and the longest supported shortcut labels. Quick Guide expansion retained the card and overlay bounds at the top and end of the independent sidebar.

Returns from all five other tabs passed at 780-, 1280-, and 1920-wide windows. The tracker retained a finite viewport while the other pages kept scrolling. Taller and wider cases also passed. These results cover the tested 96-DPI configurations.
## 24. Readable tracker, optional Test Tools, and analytics import: 0.3.4

### 24.1 Readable cards and pinned match feedback

The user requested larger class and XP text, visible end-of-match prompts, and fixes for clipped controls and input failures.

Retain the finite Session Tracker viewport and independent sidebar from section 23. Keep the cards in the game's two-column, three-row order.

Increase the class-name font from 12 to 16. Use size 12 for the baseline, current, and detail lines that used size 10.

Allow a 441-unit maximum card region, up 5% from 420. Keep individual cards compact and all baseline/manual actions visible.

Use a minimum window size of 816 × 672 WPF units. These increases stay within 5% of the previous 780 × 640 minimum. Actual native-window checks must use the new dimensions and account for Windows display scaling.

Pin the end-of-match prompt in the same top status area as read feedback. Keep its record and defer actions visible without scrolling through Analytics ONLY.

Fix sidebar scrollbar clipping and tab-button clipping in normal, selected, and hover states. A selected tab keeps its gold state during hover.

Keep Options reachable during mock capture. Disable only the real-data options that cannot change during that mode. The game-window list and Refresh must remain usable and safely restore real capture before selecting its source.

Keep card bounds, overlay bounds, and outer window bounds stable during state changes, tab changes, and Quick Guide expansion. The user can still resize or maximize the window.

### 24.2 Permanent Test Tools opt-in

Retain Test Tools as a supported optional part of the app. Add **Enable Test Tools** in Options. It defaults to off when the saved preference is absent.

Save the choice locally. Enabling it reveals the tab with a brief gold flash and does not change the selected tab.

When disabled, hide the tab and all mock/fake-data actions. Ordinary analytics import and export remain visible.

Before disabling Test Tools, safely restore the real tracker and preserve mock records in their separate folder. If the latest mock records cannot save, keep the tools enabled and explain the failure.

Do not mutate real season records or real sound settings while testing. Keep imported analytics selected when Test Tools is disabled. Explicit successful switches to fake, mock, or real data can remove the imported selection.

This amendment does not change mock result 11's correctness. Its existing placement-only sequence remains outside this correction's scope.

### 24.3 Analytics import file picker

Keep analytics separate from the primary tracker. Rename its tracker-sidebar certification heading to **Analytics ONLY**.

Add **Import analytics…** beside **Export analytics…** on the Analytics page. The player selects a Markdown analytics export or season save with a file picker.

Use the existing strict Markdown container, format, and schema checks. Validate required season fields, records, and copied rules before selecting the data.

An analytics export must retain `Report`, `SourceSeason`, and `IsMockData`. Recalculate the report from the validated source season. Cached report amounts are not authoritative.

Keep the imported source separate in memory. Import does not merge records, select a real season, change live tracking, or write local saves.

Show the imported filename and season. Keep the source label and import/export feedback visible independently of Test Tools.

**Return to current analytics** removes only the imported source and reveals the current real or mock analytics underneath. Canceling or rejecting a file leaves the prior selection intact.

Export the same source season and mock-data label that the page displays. Received XP and missed-bonus estimates retain their separate meanings.

### 24.4 Data and validation limits

This correction preserves season schema version 1, OCR numeric rules, baseline rules, earned-XP targets, and final confirmation. Match-end bonuses never advance the live target.

The import service passed 26 checks. The integrated WPF import path passed 25 checks with Test Tools off. These checks covered source recalculation, labels, cancel/failure retention, return to current analytics, and unchanged real tracker state and file bytes.

Track this correction in [GitHub issue #4](https://github.com/MadLicorice/wardogs-xp-tracker/issues/4). The final portable native regression passed 4,282 checks. Input checks passed 203 checks, and Test Tools checks passed 30. The packaged executable initialized an isolated save and closed with exit code 0. Native windows included the 816 × 672 minimum at 96 DPI. See [UI validation](UI-INTERACTION-VALIDATION.md).

## 25. Later analytics baseline confirmation and clearer options: 0.3.5

### 25.1 Manual retrospective confirmation

Allow explicit starting-baseline confirmation after match end, including later review of a saved match. Offer individual eligible classes and a separately confirmed all-eligible action where available.

Bind the action to that session's retained original baseline, class, baseline ID, and version. Record a later user attestation that the original snapshot preceded earned XP. Preserve its original value, time, observations, and correction history.

Do not replace a baseline, add inferred earlier XP, or move a live counter. Reviewing a historical match must not activate it or alter a newer session. Repeated confirmation must not add duplicate history.

Keep eligibility limits for missing, invalid, and conflicting evidence. A rebase does not rewrite the original starting value. Starting-baseline confirmation remains separate from final-result confirmation, bonus receipt, and complete analytics coverage.

### 25.2 No automatic certification from cumulative totals

The user confirmed that the match-end screen shows cumulative XP totals only. It does not show an independent whole-match earned-XP amount.

Do not automatically certify a baseline from exact placement and final cumulative XP. This is an evidence limit, not an unimplemented feature. Do not add a new consistency-check UI.

Under the current rules, these two matches produce identical captured evidence:

| Item | On-time snapshot | Snapshot after 500 XP |
| --- | ---: | ---: |
| True starting XP | 56,486 | 55,986 |
| Captured baseline | 56,486 | 56,486 |
| Last captured UNLOCK total | 59,486 | 59,486 |
| Whole-match earned XP | 3,000 | 3,500 |
| Exact finishing place | 2nd | 2nd |
| Bonus XP | 3,500 | 3,500 |
| Final cumulative XP | 62,986 | 62,986 |

Solving `final - assumed_baseline = earned + bonus` and reconstructing the start from that solution returns the assumed baseline by construction. An exact source split does not prove that its supplied baseline covered the whole match.

Future automatic certification requires additional independent evidence that identifies the start, complete match/class association, correct saved rules, and preserved provenance. An independently known capped bonus alone remains insufficient. Such a new evidence source is outside 0.3.5.

### 25.3 Analytics warning and heading

Use the tracker-sidebar heading **For Analytics ONLY**. Keep the red readiness panel separate from the live XP target.

Keep this warning separate from the class's live target state. Its presence must not block tracking, change earned XP, or imply that final results are missing. A confirmed start does not certify final XP or bonus receipt.

The saved-match modal always uses a bright green bold **MATCH XP RECORDED!** success heading. Keep the known total white. This heading confirms that the record saved, independent of analytics coverage.

When starting baselines remain unconfirmed, show an underlined gold warning in place of **Recorded values/classes only**. Name **For Analytics ONLY** and direct the player to **Saved Results → Starting baselines…**. Hide the warning when every enabled class has a valid confirmed original starting baseline. Preserve supported source colors and unresolved XP without implying that a saved record has complete coverage.

### 25.4 Other Options and tab order

Add an **Other Options** section inside the existing Options tab. Place it between Capture Keys and Current Bonus Rules.

Move the completion-sound checkbox and Enable Test Tools checkbox into that section. Retain their existing handlers, saved preferences, and mock-mode safety rules. This is a section, not a new tab or expander.

Place the optional Test Tools tab after Options. Keep its default-off preference and safe restoration from mock data. Enabling Test Tools must not change the selected tab.

### 25.5 Mock coverage and validation

Keep the PNG walkthrough at exactly 12 images. Preserve its cases and distinct expected results. It remains within the existing 15-image limit.

Add **Load saved-baseline workflow** to Test Tools. Generate a separate one-match Assault/Medic example with unconfirmed original starting baselines. Use it to review the conditional summary warning and manually confirm starting coverage later. After confirmation, its 11,700 total must split into 4,500 in-match XP and 7,200 bonus XP.

Save this example as `history-baseline-workflow.md`. It must not overwrite the 11-match fake-stats demo or real player history. It adds no PNGs.

Do not label a cumulative final total as independent earned XP or treat a rule-based split as a directly observed bonus. Manual retrospective confirmation must remain explicit in the walkthrough.

The mathematical audit passed 32 checks against the production Core rules, source solver, SessionTracker, and AnalyticsEngine. It proved the indistinguishable-start counterexample and circular reconstruction. Core tests passed 235 checks, native viewport checks passed 4,282, and summary checks passed 224. Historical WPF workflows passed 59 checks, including real and mock save recovery. The Release executable initialized isolated saves and closed normally with exit code 0. See [UI validation](UI-INTERACTION-VALIDATION.md).

Track this scope in [GitHub issue #5](https://github.com/MadLicorice/wardogs-xp-tracker/issues/5).

## 26. First-use guide and optional real-game walkthrough: 0.3.6

### 26.1 Entry, deferral, and replay

Implement both a short setup guide and an optional interactive walkthrough before the finished How To pages or tutorial screenshots. The short guide explains existing controls. It requires no running game, capture, or baseline to advance.

Always open the short guide on first use after successful initialization. Legacy settings without guide fields follow the same first-offer path. Reserve the offer across startup work. If the app is busy, show it afterward instead of losing a one-time startup callback.

Use Back, Next, Skip for now, and Finish. Treat X and Escape as deferral. Save completed and deferred versions separately from the app version. Either saved marker suppresses automatic replay for that guide version.

Add **Reopen setup guide** in **Options → Other Options**. Replay uses the current season, source, and actual configured capture-key labels. It does not reset previous guide state merely by opening the window.

After a saved Finish, open **Try guided tracking?** with **Start guided walkthrough** and **Not now**. Do not show this offer after Skip, X, Escape, or an unsaved finish. Starting the interactive walkthrough requires an explicit choice.

### 26.2 Four short pages

| Page | Required explanation |
| --- | --- |
| Prepare | Current season and data source, Refresh and game-window selection, New Session for each match, and optional Track classes. Include a dynamic reminder when an existing session is paused. |
| Read XP | TAB → UNLOCK → class → current XP column. Show the configured snapshot and check labels. Explain Manual XP, the completed-reward-page trap, the green earned-XP target, and separate bonus receipt. |
| Record results | Match-end PROGRESSION → configured check shortcut → Record Final XP. Explain Decide Later/Saved Results and End Session after an early exit or unknown outcome. |
| Optional Analytics | Explain For Analytics ONLY confirmation of a baseline that preceded earned XP, later confirmation through Saved Results → Starting baselines…, and the separate Analytics tab. Tracking does not require analytics certification. |

The paused-session reminder must leave the saved session paused. Resume is only for the same game match. A different match requires the player's explicit New Session action after the guide closes.

### 26.3 Data and preference preservation

The guide must not create or resume sessions, capture a window, change XP, certify coverage, load fake data, change sound, or select another season/source automatically.

Preserve real, mock, and imported sources when replaying. Keep Test Tools off when its preference is absent in new or legacy settings. Preserve an explicitly saved true preference.

Use two optional settings fields for completed/deferred versions and guide content version 1. Save them through a dedicated settings-only path. Preserve all other preferences and session records. On a write failure, roll back guide-version fields and show Retry and **Close without saving**. The modal retains the requested choice for Retry. Do not falsely claim a saved completion or apply the global tracker save block for this guide-only failure.

An unsaved marker can cause the guide to appear again on the next launch. Retry must attempt a real write rather than returning through a blocked guard.

### 26.4 Owned accessible modal

Use an owned, fixed-size modal targeting 680 × 560 WPF units, clamped to the available work area. Keep the body scrollable and footer pinned. Preserve readable content, keyboard navigation, and reachable Back/Next/Skip/Finish actions.

Opening or closing the guide must not resize the main app or alter card/session bounds. Existing pending results remain pending. Global capture shortcuts and automatic reading must not mutate a session through the open guide.

### 26.5 Modeless interactive walkthrough

Use a modeless task panel at the top of the tracker sidebar, above For Analytics ONLY. Keep the app and game usable. Bound the panel within the independent sidebar. Its content must not resize the main window, move the cards, or cover final-result actions.

Offer **Pause**, **Continue**, **Restart for new match**, and **Close guide**. These actions affect the guide, not the game match. Save pause and progress state across restarts. Continue walkthrough must not act as Resume Saved Session.

Add a separate **Start guided walkthrough** action in **Options → Other Options**. Change its caption to a resume or view action when saved guide state exists. Opening or replaying the guide does not change real, mock, or imported sources.

| Step | Requirement |
| --- | --- |
| Select game | Continue requires a selected real game window. It does not capture automatically. |
| New session | Require an explicit New Session after the walkthrough starts. An existing match requires a pause until the next new match. |
| Baseline capture | Require a real UNLOCK snapshot. Bind the first captured class and original baseline. Capture every enabled class or let the player clear Track for unneeded classes. |
| Starting coverage | Require manual confirmation of all enabled original starting baselines. Explain that each original snapshot must precede earned XP. |
| Current capture | Require a real current UNLOCK read for the bound match. An unchanged XP value is valid. No actual gain or maximum-tier event is required. |
| Final capture | Require a real end-of-match PROGRESSION read. Missing final class totals remain missing. |
| Final confirmation | Require explicit Record Final XP confirmation for the bound result. Later Saved Results review can satisfy this step without activating an older match. |
| Analytics | View this match in Analytics with the real source and correct season. An imported or fake overlay cannot satisfy this step. Then mark the walkthrough complete. |

Clearly state that proper match-wide Actions/Bonus breakdowns require starting-baseline certification and confirmed final results. Point to Analytics details. Starting-baseline certification remains a manual attestation. The guide must never infer that timing from XP totals, placement, or a calculated split.

If the player earned XP before a baseline, preserve the live interval tracker. Explain the limit and offer a fresh-match walkthrough. Do not encourage confirmation of an uncertain or late start.

### 26.6 Evidence binding and saved state

Persist optional `InteractiveWalkthroughState` in settings without changing season schema version 1. Use an immutable pure helper to evaluate progress. Keep guide transitions separate from session mutations and game capture.

Bind progress to season, session, captured class, and original baseline IDs/versions. Real capture steps require selected-window capture provenance and accepted matching screens. Mock screenshots, imported images, manual XP, and reused final totals cannot fulfill them.

Source, session, and baseline mismatches show a recovery action. They must not silently associate old evidence, certify coverage, or complete a later step. Preserve legitimate historical final review for the bound session without changing the newer active session.

After explicit Continue, the guide can reconcile retained authentic OCR observations for the exact bound match, class, and original baseline. This avoids requiring another final screen when the player captured it while the guide was paused. It must not infer missing evidence, bypass manual starting coverage, or confirm final results automatically.

Do not fabricate progress when the game is unavailable. Pause and continue explicitly. The guide must not create XP, records, fake datasets, or sound changes. Unknown placement, missing cards, and unresolved analytics retain their normal uncertainty.

### 26.7 Acceptance and tutorial boundary

Both onboarding workflows are implemented in 0.3.6 pending user acceptance. Test fresh and legacy profiles, all modal exits, the saved-Finish offer, replay, remapped keys, paused sessions, source preservation, and guide-save recovery before release.

Test real-evidence gates, unchanged current XP, manual all-enabled coverage, explicit final confirmation, historical review, and the matching real Analytics source. Test Pause/Continue across restart and recovery after source, session, or baseline changes. Mock, imported, manual, and reused-final data must not create fake real-capture progress.

The full How To pages and tutorial screenshots remain future work. Create them only after the guide and final labels are accepted. Any guide images captured for validation are QA previews, not finished tutorial screenshots. Update [the tutorial plan](TUTORIAL-PLAN.md) to distinguish implementation from acceptance.

The user deleted the earlier onboarding reminder on October 17, 2026. No reminder automation belongs to this change, and none may be recreated without a new explicit request.

Track this scope in [GitHub issue #6](https://github.com/MadLicorice/wardogs-xp-tracker/issues/6).

## 27. Saved-match deletion, guide skips, and quiet automatic reads: 0.3.7

### 27.1 Whole-match deletion

Place a small **⋮** menu in the first Saved Results column so it stays reachable on narrow windows. Its **Delete** action is red, including hover. Opening the menu must not select or leave a highlighted table row.

Keep the menu header unsorted. The initial descending arrow belongs to Match started. Refresh must retain the user's actual date-sort direction and row order.

Use the exact confirmation **This action cannot be undone. Delete?** with Yes and No. No has initial focus, default-button behavior, and cancellation behavior. No, Escape, and X keep the match unchanged. Reserve busy before the dialog so game capture and result edits cannot run behind it.

Yes removes the entire session, including its observations, history, original baselines, pending/confirmed finals, and derived analytics. Recalculate analytics from remaining sessions. Do not patch aggregate totals independently.

If the deleted ID equals ActiveSessionId, clear it and the app's resume choice. Do not activate an older match or create a new one. Preserve other sessions, their XP and provenance, season preferences, rules, and imported read-only data. Existing references to a deleted source ID in another match's reused-baseline provenance remain historical metadata.

Write to the exact owner: real season, screenshot-mock session, or fake-results dataset. A failed save keeps deletion in memory, retains its owner/store/filename, and blocks further changes. Retry saves that owner without repeating deletion. Source restoration and close must resolve the same pending write, rather than substitute the selected real tracker.

Keep the explicit close choice between retrying and exiting with the previous save. Do not claim deletion persisted after a failure. Normal atomic replacements and backup files remain unchanged. No physical file wipe belongs to this action.

When a walkthrough's bound match disappears, show **Walkthrough match deleted** at every bound stage. Disable evidence actions and offer Restart, Skip step, or Close guide. Never recreate the match or infer its missing evidence.

### 27.2 Skip step records guide metadata only

Add **Skip step** to the modeless walkthrough. One click advances exactly one visible task and appends that task to optional saved `SkippedSteps` metadata. The action does not require a game or Test Tools.

Skipping creates no session, XP capture, baseline, coverage certification, final confirmation, or Analytics visit. Those fields remain evidence of actual actions. A later real read can bind actual compatible evidence without inventing the earlier skipped capture.

Disable skipping while busy, paused, awaiting a guide-save retry, unsupported, or finished. Keep skipped tasks visible after reload. If any task was skipped, use **Walkthrough finished · steps skipped** instead of an unqualified completion claim.

Viewing real Analytics can finish the guide after skips, but it does not change analytics eligibility. Missing starts and finals remain missing. Skipping the Analytics task records that skip rather than claiming a visit. Imported or fake Analytics still cannot fulfill the real-source navigation task.

The short guide still always appears on first use. Only a saved Finish opens the optional walkthrough offer. Preserve the manual starting-coverage and final-confirmation rules from section 26. Test Tools still default to off and preserve an explicit saved choice.

### 27.3 Capture mode and automatic-reading defaults

Use **Quiet · visible game only** as the default capture mode, as approved by the user. Require the selected game in the foreground, uncovered, and fully inside the visible desktop. Copy only its client rectangle. Do not create a full-display image or silently read another app.

Retain **Window capture · Windows border** as the persistent Windows Graphics Capture alternative. Reuse its session across reads of the same target. Windows can show its capture border. Stop the session when the target/mode changes or reading stops.

Enable `AutomaticReadEnabled` by default in new and legacy settings without that field. Preserve a saved false value. **Read every 2 seconds** remains optional and persists its setting. This supersedes the earlier startup-off behavior.

Automatic reads require a selected real target, a resumed active or frozen-unconfirmed session, and available local saves. Do not read mock images automatically. Do not start or resume a match. When quiet capture cannot read the foreground game, wait without repeatedly reserving busy or changing the preference.

Ignore ordinary gameplay scenes without repeated unsupported-screen notices. An end score can freeze live counters while automatic reads continue for PROGRESSION totals. Save accepted observations automatically. Never automatically confirm starting-baseline timing or final results.

Pause capture for failures or blocking dialogs without erasing the saved automatic-reading preference. Keep the F3 baseline and F4 check fallbacks. Auto-reading current XP does not establish a missing baseline.

### 27.4 Public assets and release privacy

Keep bundled test assets synthetic and local. The 12 PNG fixtures already exist. The supplied logo makes 13 public images in the current metadata audit. Do not add player captures, personal paths, or custom image metadata to public project files.

Use `tools/AuditPublicRelease.py` for eligible source/assets and the publish folder. Scan project Release assemblies as well as the single-file executable. An earlier Release scan found embedded debug-path references. Release configuration must disable debug symbols and map compiled source paths through `DebugType=none` and `PathMap`.

The source and public-image checks passed. Final compiled-output audit, package identity, and remaining regression counts belong in [UI validation](UI-INTERACTION-VALIDATION.md). Do not infer final package cleanliness from an intermediate source-only scan.

### 27.5 Validation and tutorial boundary

The isolated deletion tests passed 13 checks. The shown WPF deletion workflows passed 78 checks. The walkthrough model passed 148 pure checks, and Skip step passed 64 shown WPF checks. These suites overlap setup coverage and must not be presented as one independent total.

The focused sort/preference follow-up passed 19 shown WPF checks. Final native capture guards passed 15 checks. A controlled host fixture measured three border onsets for the earlier repeated Windows capture, one for persistent Windows capture, and zero for quiet capture. Keep those results separate from live-game acceptance and the final OCR-chain regression.

Both onboarding workflows remain pending user acceptance. The full How To pages and tutorial screenshots remain future work. QA previews remain test evidence. The earlier onboarding reminder stays deleted, and issue #6 remains open. This change does not create a GitHub release or publish a completion comment.

## 28. Saved Results actions icon: 0.3.8

Replace the clipped text glyph in the first Saved Results column with three vector dots. Keep the column at 42 pixels and center a 28-by-28 button with its own cell padding. Use a transparent normal state, a hover fill, and a visible keyboard-focus border.

This is a presentation change. Keep the existing menu, red Delete action, confirmation, match deletion, and analytics recalculation. Keep the current mock-data folder so this update retains existing test sessions. Onboarding and live-game acceptance remain pending; issue #6 stays open.

## 29. Public beta icon and privacy: 0.4.0-beta.1

Use the user-supplied orc as the app icon, with large XP letters over its face. The generated PNG and ten-size ICO contain only image structure, transparency, and pixel data. Set both the native executable icon and the shared WPF Window icon. Keep the existing WARDOGS header logo.

Remove optional image chunks from bundled mocks and the logo without changing their compressed pixel streams. Regenerated mocks must also omit those chunks. Keep private captures, profiles, logs, tools, and build artifacts outside the public Git tree. Audit current source, reachable Git history, assets, project Release assemblies, and the compressed executable bundle.

Remove private paths, account and machine details, secrets, and optional image metadata. Keep the requested branding, required license credits, file-format structures, versions, and functional code identifiers. Release builds and build.ps1 must disable debug symbols and map source paths. Do not claim that a working executable contains no metadata of any kind.

The user approved publication of the beta executable, portable ZIP, and audited source to GitHub on October 17, 2026. Publish v0.4.0-beta.1 as a prerelease after checks pass. Test Tools remain off by default. Live WARDOGS capture and end-screen acceptance remain beta test items. Issue #6 stays open.
