# Test the 0.3.7 mock pack

Open `WardogsXpTracker-0.3.7.exe`. You do not need WARDOGS or a selected game window for these tests.

Complete or skip the first-launch setup guide before testing. Decline its optional real-game walkthrough for these mock checks. Skip Step can move through the guide without creating capture evidence. Automatic real-game reading stays separate from mock reads.

The pack contains exactly **12 PNGs**. They are code-rendered fixtures, clearly marked as mock data. The app reads their pixels through the real local OCR reader. It does not inject their expected numbers into the tracker.

## Run the tier sequence

1. Enable **Options → Other Options → Enable Test Tools**, then select **Load fake screenshots / restart** on its new tab.
2. Wait for Session Tracker to show the Assault baseline **56,486**. Confirm Assault's starting baseline in the sidebar if you want analytics for this mock session.
3. Use **Next mock + read** in the header through the remaining 11 images. Wait for each read to finish.
4. At the last image, select **Record Final XP**. Choose **Use observed: 2nd or 3rd place** to retain the observed second-or-third-place uncertainty.
5. Select **Real data** when finished.

Loading the walkthrough starts a new mock session and reads image 01 automatically. Only Assault is enabled. Session Tracker stays visible while the header advances the images.

| Image | Exact Assault total | Expected result |
| --- | ---: | --- |
| 01 baseline | 56,486 | Gray baseline ready, gain 0 |
| 02 plus 1 | 56,487 | Gain 1, no completion check |
| 03 plus 500 | 56,986 | Tier 1 |
| 04 plus 1,000 | 57,486 | Tier 2 |
| 05 plus 1,500 | 57,986 | Tier 3 |
| 06 plus 2,000 | 58,486 | Tier 4 |
| 07 plus 2,499 | 58,985 | No completion check yet |
| 08 plus 2,500 | 58,986 | Green completion check and target sound if enabled |
| 09 plus 3,000 | 59,486 | Gain remains 3,000, with the green check |
| 10 completed page | No accepted current total | Show the rejection banner and retain 59,486 |
| 11 DEFEAT | No class XP on this image | Freeze live checks, retain second-or-third-place evidence. Waiting for final XP explains the disabled action. Read mock 12 next |
| 12 final progression | 62,986 | Store a pending final total, keep live gain at 3,000. The 6,500 increase includes the bonus |

The final increase from the original baseline is 6,500 XP, including the bonus. It is not another 6,500 of player-action XP. With confirmed starting coverage and the DEFEAT evidence, analytics can show 3,000 in-match, 3,000 bonus, and 500 indeterminate XP. Final XP never advances the live completion check.

## Review final results later

Select **Decide Later** after mock 12. A saved-results reminder replaces the gold prompt. Open **Saved Results** and select **Review…** for that match. The same route works for saved real results after a restart or a new session.

**Observed…** shows captured XP and placement revisions. Manual corrections remain separate from those revisions. Confirming an older match does not change the current session.

After a successful final save, a compact **MATCH XP RECORDED!** dialog shows the known total and brief colored class breakdowns. Select Continue to return to the app. Without starting-baseline certification, the 6,500 measured increase has unknown sources. Missing classes remain Unknown.

The fake analytics season uses 11 matches with 11 tracked class results. Nine results qualify for received totals. One final is pending. One start is uncertified. The 55 untracked class slots do not count as tracked results.

Table headers show gold sort arrows. Click a header to change the order. Clicking outside a table clears its selection. The wheel scrolls the table while it can move, then scrolls the surrounding page at either edge.

## Confirm starting baselines after a saved match

1. Select **Test Tools → Load saved-baseline workflow**. Saved Results opens with one completed first-place match.
2. Select **Starting baselines…**. Assault starts at **10,000 XP** and Medic starts at **20,000 XP**. Both timings remain unconfirmed.
3. Select Assault's **Confirm…**, then cancel. Nothing changes. Repeat and confirm Assault.
4. Select **Confirm all eligible baselines…**. The separate confirmation now lists only Medic. Confirm it.
5. Select **View XP summary**. The total remains **11,700 XP**, split into **4,500 Actions + 7,200 Bonus + 0 Indeterminate**.

Assault's increase is **6,500 = 2,500 Actions + 4,000 Bonus**. Medic's increase is **5,200 = 2,000 Actions + 3,200 Bonus**. Before confirmation, these increases remain indeterminate. The success heading stays bright green in both states. The underlined baseline notice disappears once all tracked starts are confirmed.

Use this route for earlier real matches too. Confirm only what you know about the saved original baseline. Missing, invalid, partial, or rebased starts show a reason and cannot be certified here. The active session, captured observations, final totals, and target sounds do not change.

The workflow fixture saves as `history-baseline-workflow.md`. **Load fake stats** restores the separate 11-match dataset. The new loader creates a fresh practice match each time. No extra screenshot is needed.

The game supplies cumulative XP only. Subtracting a possible bonus from final XP gives the pre-bonus total, not the original starting XP. The app never certifies baseline timing from this calculation.

## Sound and shortcuts

In Test Tools, **Play the target sound in this mock walkthrough** defaults to enabled. It is independent of your real sound setting. Image 08 reaches the target and plays that sound once. Re-reading a completed track does not reach the target again.

**Play completion sound** plays a single preview. It does not change XP or the real sound preference.

In screenshot mode, F3 and F4 read the selected bundled image. F3 still follows the normal baseline guards. F4 is a current check. Neither shortcut captures a real window while screenshot mode is active.

F3 and F4 are the defaults. Use the configured shortcuts if you changed them. The Snapshot and Check XP buttons show the active bindings.

## Inspect fake analytics

Select **Load fake stats** on Analytics, or **Load fake stats** in Test Tools. This loads 11 synthetic sessions into the separate Analytics tab. It does not replace the real tracker or the mock walkthrough.

Check the class bars, source pie, exact tables, and record explanations. The examples include:

| Example | Expected evidence |
| --- | --- |
| Exact first, second, and third place | Supported in-match and bonus splits |
| DEFEAT with a 6,500-XP increase | 500 gray Indeterminate XP |
| Confirmed total, completion unknown | Zero bonus remains a possible outcome, with 4,000 gray XP |
| Recon left after 3,000 earned XP | Average missed bonus 3,500 |
| Support disconnected after 1,200 earned XP | Average missed bonus 1,400 |
| Incomplete starting or ending coverage | Excluded received totals and a missed-bonus lower bound where supported |
| Recorded totals contradicting bonus rules | Known XP remains unresolved with an explanation |
| Unconfirmed final candidate | Excluded from received totals |
| Rebase with a valid original baseline | Whole-match source calculation retains the original evidence |

The demo missed-bonus total is **At least 7,000 XP**. It sums the eligible record averages. Each average weights first, second, and third place equally. Missed XP is not received XP and does not enter the pie.

**Export analytics…** writes the displayed report, derivation version, and source season records to Markdown. A mock export identifies the data as mock data.

## Reuse the images for other checks

Restart with **Load fake screenshots / restart** before each check below. Restart again before another full tier sequence.

### Cancel a rebase

1. In Test Tools, choose image 03 and select **Read selected mock**.
2. On Assault, select **Re-Establish Baseline**, then **New measuring interval**.
3. Cancel the recording prompt.

The baseline stays 56,486. Current XP stays 56,986 and the gain stays 500.

### Commit a rebase

1. Read image 03, then select **Re-Establish Baseline** and **New measuring interval**.
2. With image 03 still selected, push F3 to record the replacement.
3. Select image 09 in Test Tools and select **Read selected mock**.

The replacement baseline is 56,986. The next gain is 2,500 and can turn green. The mid-match rebase warning remains.

### Enter XP manually

1. Select Assault's **Manual XP** and record **58,985** as current XP.
2. Select **Manual XP** again and record **58,986**.

The first gain is 2,499 without a completion check. The second is 2,500 with the check. Restart before reading lower values again.

### Record end outcomes

Use **End Session** to test **Finished**, **Left Match**, **Disconnected**, and **Unknown** separately. Restart between outcomes. All four end the live counter without adding a predicted bonus.

For a no-bonus analytics example, certify the starting baseline, then read image 09. Choose **Left Match**, then record **59,486** with **Manual XP** as the closing total. Analytics can record 3,000 in-match XP, zero received bonus, and a 3,500 average missed bonus.

To test a 1,200-XP disconnect, enter **57,686** as current XP after restarting. Choose **Disconnected**, then record **57,686** as closing XP. With starting coverage certified, the average missed bonus is 1,400.

Without final or closing totals, keep the record incomplete. A live completion check does not establish bonus receipt.

## Files and returning to real data

The distributed folder contains the same 12 PNGs, `manifest.json`, the demo season save, and the real OCR verification report. No additional PNGs are needed for these checks.

The app extracts its bundled test files and saves mock sessions here:

```text
%LOCALAPPDATA%\MadLicorice\WardogsXpTracker\mock-data\0.3.7
```

Test Tools shows the exact folder. Mock records stay separate from real season files. **Real data** restores the real tracker and normal preferences, and clears the fake analytics view.

Closing and reopening the app loads normal real data. Mock files remain saved separately. Use **Load fake screenshots / restart** to start the walkthrough again.

## Stable resizing

Resize or maximize the app, including 1920 × 1080 and wider screens. There is no horizontal maximum. Session Tracker keeps six compact cards beside a separate scrolling sidebar. At 100% Windows scaling, all six Manual XP buttons fit in a 816 × 672 minimum window. Read errors appear above the cards. Expanding Quick guide and changing status text do not move the cards or resize the window.

Test Tools remains available as an opt-in feature. Its fake buttons stay hidden until enabled. Ordinary analytics import/export and Diagnostics Lab remain available while it is off. Match-end mock 11 remains available for later manual testing.
