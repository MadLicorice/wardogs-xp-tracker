# WARDOGS XP Tracker

A local Windows companion app for tracking class XP in WARDOGS.

**Status: design agreed. App implementation has not started.**

Read the [full agreed design](docs/APP-DESIGN.md) before implementation.
It records the tracker workflow, capture rules, season saves, patch rulesets,
analytics, and release requirements.

## Selected stack

- C# with .NET 10 LTS and Windows Presentation Foundation (WPF).
- A self-contained Windows executable with the required runtime and local OCR assets.
- Local Markdown saves, with one save per season and separate dated ruleset files.
- Windows 10 and Windows 11 as release test targets.

## App pages

- **Session Tracker:** the default page for live class progress.
- **Analytics:** a separate page for recorded XP sources and average missed bonuses.
- **How To:** bonus XP guidance and, after the UI is final, an app tutorial.
- **Settings:** seasons, rulesets, hotkeys, and other preferences.

The full design is the source of truth for behavior. Package choices, capture
details, and the final visual layout remain implementation work.

## License

See [LICENSE](LICENSE).
