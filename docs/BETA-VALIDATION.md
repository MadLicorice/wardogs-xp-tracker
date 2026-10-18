# Beta validation: 0.4.0-beta.1

This beta preserves the current tracker behavior and adds the orc XP icon and stricter public-release privacy checks.

- Core tests: 248 passed, zero failed.
- Icon checks: 29 passed. All ten PE icon frames match the sanitized ICO. Native extraction, WPF resources, MainWindow, and the actual confirmation dialog use the same icon. File and assembly version are 0.4.0.0 with the beta informational version.
- Mock generation: all 12 images passed the real OCR reader after the encoder omitted optional metadata. These remain synthetic fixtures, not player captures.
- Asset cleanup: 37 optional blocks removed from 13 images. Compressed pixel streams remain unchanged. The new icon PNG and all ten ICO frames omit optional metadata.
- Public source and reachable history: the final independent check caught generated Python bytecode containing a local build path before any push. The unpublished commits were replaced, caches were excluded, and the repeatable audit gained a bytecode guard. A final committed-tree and package check is required before publication.

The public build script no longer overrides Release configuration with embedded debug symbols. Release uses DebugType=none, DebugSymbols=false, and mapped source paths.

The release assets include SHA256SUMS.txt and PUBLIC-PRIVACY-CHECK.json for the exact package. That report identifies the audited source commit and package scopes. It excludes ignored private fixtures, local saves, and unreachable Git objects. Required format structures, versions, functional identifiers, branding, and license notices remain.

Real-game borderless capture and the complete match-end XP flow remain pending acceptance. The first-use guide and optional walkthrough also remain pending user acceptance. Issue #6 remains open. The How To page and finished tutorial screenshots remain future work.
