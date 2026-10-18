# Analytics mock pack

These are generated test fixtures. They do not describe real matches.

1. Enable **Options → Other Options → Enable Test Tools**. Use **Load fake screenshots / restart** to create the isolated mock session.
2. Use image 01 as the Assault baseline, then images 02–09 as current XP checks.
3. Check the transitions at 500, 1,000, 1,500, 2,000, and exactly 2,500 earned XP.
4. Image 10 must reject a completed reward page. Image 11 supplies DEFEAT without an exact placement.
5. Image 12 supplies cumulative final XP of 62,986: a 6,500 increase INCLUDING the bonus. Confirm it separately.

The app's Diagnostics Lab imports images for diagnosis only. It does not change live tracking.
Use the app's explicit testing flow to exercise the session actions above.

`manifest.json` records exact values and expected reader results. `ocr-verification.json` records real OCR verification when requested.
`demo-analytics-season.md` contains 11 generated session examples. Keep it separate from personal save files.
The season covers exact placements, DEFEAT uncertainty, unknown completion, missed bonuses, partial coverage, inconsistent rules, pending finals, and a rebase.
The app shows a rejection banner for image 10 and waits for final XP after image 11. After image 12, record results for a blocking summary. Decide Later keeps the match in Saved Results with observed revisions.

**Load saved-baseline workflow** creates one completed Assault/Medic match in Saved Results. Use **Starting baselines…** to test individual confirmation, the separate Confirm all confirmation, and the updated XP summary. The separate `history-baseline-workflow.md` fixture contains the same scenario. The pack still contains exactly 12 PNGs.
