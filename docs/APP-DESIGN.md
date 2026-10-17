# WARDOGS XP Tracker: agreed app design

Status: agreed product design, saved before app development.

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

Offer an optional sound when a track reaches its target. The sound and its default setting remain UI choices.

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

Analytics has its own page and navigation button. Do not place its charts on the Session Tracker page.

The tracker contains six class panels. Each panel shows the class, tracking toggle, baseline state, current gain, target progress, and completion state.

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

Offer one confirmation for all currently captured classes:

> These baselines were captured before I earned XP this match.

Record which classes and baseline versions the answer covers. A later snapshot must not inherit an earlier coverage answer without evidence.

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
- Keep Analytics separate from the default tracker page.

### 18.4 Packaging and file checks

- Run the packaged executable on Windows 10 and Windows 11.
- Run without a separately installed .NET runtime, OCR package, or development environment.
- Keep reading and calculations local without an account or a paid service.
- Recover from an interrupted save without silently losing season history.
- Test representative large season files without blocking the UI.
- Include the required help, obvious creator credit, and the final UI tutorial screenshots.
