# Onboarding and tutorial plan

Status: the four-page setup guide and optional real-game walkthrough are implemented in version 0.3.6, pending user acceptance. The full How To pages and screenshot tutorial remain future work.

## Dependency order

Accept both onboarding workflows before making tutorial screenshots. Confirm the final labels and behavior first, so the tutorial teaches the interface users receive.

| Stage | Required result | Status |
| --- | --- | --- |
| Onboarding decisions | Mandatory first-use short guide, saved-Finish offer, optional real-game walkthrough, pause/replay, and truthful analytics requirements | Adopted for 0.3.6 |
| Short guide | Four pages without capture gates or automatic session/data changes | Implemented in 0.3.6; user acceptance pending |
| Interactive walkthrough | Explicit new match, real captures, manual starting coverage, confirmed finals, and matching real Analytics | Implemented in 0.3.6; user acceptance pending |
| First-use test | Check settings/exits, saved-Finish offer, replay, paused state, real-evidence gates, source binding, and save recovery | Record measured results separately; user acceptance pending |
| Tutorial screenshots | Capture the final interface with clearly identified test data | Wait for onboarding acceptance |
| Tutorial text and review | Check every instruction against the screenshots and app | Blocked by screenshots |

The user explicitly deleted **onboarding-before-the-tutorial** on October 17, 2026. No reminder automation is active. Do not create another reminder without a new explicit user request.

Version 0.3.6 implements both onboarding workflows. It does not start the finished How To pages or tutorial screenshots. See [design amendment 26](APP-DESIGN.md#26-first-use-guide-and-optional-real-game-walkthrough-036) and [issue #6](https://github.com/MadLicorice/wardogs-xp-tracker/issues/6).

## Implemented short guide

1. **Prepare:** show the current season/source, Refresh and game picker, New Session per match, optional Track classes, and any paused-session reminder.
2. **Read XP:** explain the current UNLOCK column, actual configured snapshot/check shortcuts, Manual XP, the completed-reward trap, the green target, and separate bonus receipt.
3. **Record results:** explain final PROGRESSION capture, Record Final XP, Decide Later/Saved Results, and End Session after an early exit.
4. **Optional Analytics:** explain For Analytics ONLY, valid starting-baseline timing, later Saved Results → Starting baselines…, and the separate Analytics tab.

The short guide always appears on first use, including legacy profiles without markers. Startup work must not lose that first offer. The guide offers Back, Next, Skip for now, and Finish. X and Escape defer it. Two optional completed/deferred markers cover guide content version 1 and stay separate from the app version. Reopen it from **Options → Other Options → Reopen setup guide**.

No game, capture, or baseline is required to advance. The guide does not create/resume sessions, change XP or coverage, seed test data, change sound, or select another source. A saved session stays paused until the player chooses its next action. Existing explicit Test Tools preferences remain intact.

Test Tools remain an optional practice path. The guide never enables them automatically. A failed guide-state save rolls back its version markers and offers Retry or Close without saving without blocking normal tracker saves.

## Implemented optional real-game walkthrough

After a saved Finish, **Try guided tracking?** offers **Start guided walkthrough** or **Not now**. Skip, X, Escape, and an unsaved finish do not show the offer. A separate guided-walkthrough action remains in Options → Other Options. It changes to a resume or view action for saved guide state.

The modeless panel sits above For Analytics ONLY. Pause, Continue, Restart for new match, and Close guide affect only the guide. Persisted progress permits a later return when the player can play.

| Step | Required evidence |
| --- | --- |
| Select game | A selected real game window, checked when the player continues |
| Start session | An explicit New Session after the walkthrough starts |
| Capture starts | Real UNLOCK baseline capture, bound to the first captured class and original baseline versions |
| Confirm coverage | Manual confirmation of all enabled original starts, or explicit removal of unneeded tracks |
| Read current XP | A matching real UNLOCK read, including an unchanged value |
| Read final XP | A matching real PROGRESSION read |
| Confirm result | Explicit final confirmation, including later review of the bound match in Saved Results |
| View Analytics | This match's details in the real source and correct season |

Proper match-wide Actions/Bonus breakdowns require confirmed starting coverage and confirmed final results. The guide explains both requirements. It never certifies timing automatically. A late or uncertain baseline needs a fresh-match path, while live interval tracking remains available.

Mocks, imports, manual values, and reused final totals cannot satisfy real-capture steps. Bind progress to season, session, class, and original baseline IDs/versions. A mismatch must show a recovery action instead of silently completing a step. An imported or fake analytics overlay cannot count as this match's Analytics view.

An existing match does not satisfy the explicit new-session step. The player can pause until the next match. Continuing the walkthrough must not automatically resume a saved game session. Historical result review must preserve any newer active session.

## Tutorial content after onboarding

The app tutorial must cover optional tracks, completed reward traps, remapped keys, manual XP, rebase cancellation, and the persistent rebase warning.

It must also cover final candidates, Decide Later, Saved Results, placement uncertainty, early exits, restart recovery, and explicit reuse of confirmed final totals.

Explain the two uses of Analytics: the tracker sidebar confirms starting-baseline timing, and the separate tab displays recorded statistics. A green readiness panel does not establish final coverage or bonus receipt.

The chart tutorial must distinguish received XP, gray Indeterminate XP, excluded records, and Average Missed Bonus. Explain equal placement weighting and **At least** lower bounds.

Use actual app screenshots after the onboarding interface is accepted. Clearly label synthetic data. Existing development previews, onboarding QA images, and mock game images are test material, not the finished tutorial.

## Bonus XP help and credit

The separate bonus help page must explain the selected rules, per-class tiers, placement bonuses, and no-bonus outcomes. Keep maximum-tier completion separate from bonus receipt.

Include the supplied chart with obvious adjacent credit to **Twinsticks and Ties — Field Report 008**. Preserve its **Season 1 — reward model from our tests** source context. Keep that context distinct from **Oct 15 2026 Patch XP Bonus Rules**.

## Completion checks

- A new user can finish the short guide without a running game or developer explanation.
- The optional walkthrough uses real player actions and matching evidence without automatic certification or fake progress.
- Each tutorial image matches the final labels and behavior.
- Real data, mock data, and returning to real data are clear.
- Uncertain or missing data never appears as a confirmed zero.
- The bonus chart has obvious creator credit beside it.
