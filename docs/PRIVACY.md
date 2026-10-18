# Privacy and local data

MadLicorice's WARDOGS XP Tracker processes XP on your computer. The app does not upload captures, saves, diagnostic reports, or analytics exports. It does not require an app account or a hosted service.

## Public source and release

The public source and beta package exclude player captures and runtime profiles. Bundled test images and example records are synthetic. The public audit covers tracked files, untracked nonignored files, eligible asset contents, and locally reachable Git history. A separate package audit covers the distributed executable and files.

The audit checks private absolute paths, personal account or machine identifiers, real contact addresses, credential patterns, and optional image metadata. Public image assets omit optional metadata. Required image and executable format fields remain. App versions, functional identifiers, hashes, intentional MadLicorice/WARDOGS branding, and required copyright and license notices remain.

This audit does not inspect ignored private saves or unreachable Git objects. It does not make runtime exports safe to publish automatically. The repeatable release check is [tools/AuditPublicRelease.py](../tools/AuditPublicRelease.py).

## Capture and local saves

The app reads the selected game window. The default quiet mode needs the game in front, uncovered, and fully on the visible desktop. The alternate Windows capture mode can show the Windows capture border. Neither mode falls back to a full-display capture.

Captured frames stay in memory unless you explicitly save a read report. The app keeps recent read evidence for diagnostics. It does not automatically save game screenshots.

The default data folder is `%LOCALAPPDATA%\MadLicorice\WardogsXpTracker`. **Options** shows its exact location. Local files contain settings, seasons, baselines, readings, match results, rules, history, and guide progress. Replacement saves retain a previous version as `.bak`.

Test Tools use separate mock files. Analytics import reads the chosen file without replacing the active tracker. Analytics export includes source season records and the calculated report.

## Diagnostic reports and sharing

**Save read report…** asks you to choose a local ZIP file. It can include the current frame and retained accepted or rejected frames. It also includes OCR text, numeric results, timestamps, dimensions, capture details, and the app version.

These files can contain private information. A captured game image can show chat, player names, XP, or other visible content. Read details can include window titles, process information, filenames, or paths in error messages. Imported-image source labels use the filename, but you must still review the complete report.

Keep saves and reports private unless you choose to share them. Examine every image and text file before you attach an export to a public issue. The app does not send a report when you save it.

## Licenses and required information

Privacy cleanup preserves required notices and functional data. See [LICENSE](../LICENSE), [asset provenance](../assets/PROVENANCE.md), and [dependency notices](../assets/licenses/). The app license does not grant rights to the WARDOGS game brand asset.
