# Issue tracker: GitHub

Issues and specs for this repo live in [GitHub Issues](https://github.com/MadLicorice/wardogs-xp-tracker/issues). Use the `gh` CLI for tracker operations.

## Conventions

Run commands inside the clone. Outside the clone, add `--repo MadLicorice/wardogs-xp-tracker` to issue and label commands.

- **Create an issue:** `gh issue create --title "..." --body-file <path>`.
- **Read an issue:** `gh issue view <number> --json number,title,body,labels,comments`.
- **List issues:** `gh issue list --state open --json number,title,body,labels,assignees`. Apply the relevant `--label` and `--state` filters.
- **Comment:** `gh issue comment <number> --body-file <path>`.
- **Apply or remove labels:** `gh issue edit <number> --add-label "..."` or `--remove-label "..."`.
- **Close:** `gh issue close <number>`. Add a comment first when an explanation is needed.

For multiline issue bodies and comments, write the exact text to a temporary UTF-8 file. Pass that file with `--body-file`.

Infer the repo from `git remote -v`. Check that the remote matches this tracker before a write.

## Pull requests as a triage surface

**PRs as a request surface: no.** Set this to `yes` only if the repo later treats external pull requests as feature requests.

GitHub shares one number space for issues and pull requests. Resolve the type before you act on a bare issue number.

## Skill instructions

When a skill says to publish to the issue tracker, create a GitHub issue. When a skill says to fetch a ticket, read the GitHub issue and its relevant comments.

## Wayfinding operations

The `wayfinder` skill uses one map issue with child issues as tickets.

- **Map:** one issue with the `wayfinder:map` label. Its body holds Notes, Decisions-so-far, and Fog.
- **Child ticket:** link it to the map as a GitHub sub-issue. If sub-issues are unavailable, add a task-list link in the map. Put `Part of #<map>` at the top of the child body. Use the applicable `wayfinder:research`, `wayfinder:prototype`, `wayfinder:grilling`, or `wayfinder:task` label.
- **Blocking:** use GitHub's native issue dependencies when available. Otherwise, put `Blocked by: #<number>` at the top of the child body. A ticket is unblocked when all its blockers are closed.
- **Frontier query:** list the map's open children. Exclude children with an open blocker or an assignee. Choose the first remaining child in map order.
- **Claim:** assign the ticket to the developer who will do the work. For the current authenticated developer, use `gh issue edit <number> --add-assignee @me`.
- **Resolve:** comment with the result and close the ticket. Then append a short result summary and ticket link to the map's Decisions-so-far section.

Use these API operations if the installed CLI lacks a direct command:

```text
gh api --method POST repos/MadLicorice/wardogs-xp-tracker/issues/<parent>/sub_issues -F sub_issue_id=<child-database-id>
gh api --method POST repos/MadLicorice/wardogs-xp-tracker/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-database-id>
gh api repos/MadLicorice/wardogs-xp-tracker/issues/<number> --jq .id
```

These IDs are numeric database IDs, not issue numbers or node IDs. The `issue_dependencies_summary.blocked_by` value counts open blockers.
