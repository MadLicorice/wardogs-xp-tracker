# Domain docs

This repo uses a single context. The agreed app design is in `docs/APP-DESIGN.md`.

## Before exploration

Read `docs/APP-DESIGN.md` before app work. Also read these files when they exist:

- Root `GLOSSARY.md` for domain terms.
- Relevant architecture decision records in `docs/adr/`.

If the glossary or decision records do not exist, proceed silently. Do not create empty files or report their absence as a problem. The `domain-modeling` skill creates them when terms or decisions are resolved.

## File layout

```text
/
  GLOSSARY.md
  docs/
    APP-DESIGN.md
    adr/
      NNNN-decision-name.md
  src/
```

This layout describes where future domain docs belong. Setup does not create a glossary or decision records.

## Use the glossary's terms

Use the glossary's terms in issues, proposals, tests, and other work. Do not replace them with synonyms that the glossary rejects.

If a term is missing, check whether it belongs in the project's vocabulary. Note a real gap for `domain-modeling`.

## Conflicts with decisions

If a proposal conflicts with an existing decision record, name the record and explain the conflict. Do not silently replace the decision. Apply the same rule to the agreed app design.
