---
sidebar_position: 3
---

# Git-linked history

Mnemonic does not replace git — it **contextualizes** it. Every preserved clip carries repository metadata so you can trace development episodes across branches and commits.

## What git adds to clips

When a git repository is detected in your project, Core polls for changes during capture. Each clip's metadata includes:

| Field | Description |
|-------|-------------|
| `git_branch` | Active branch at segment close |
| `git_commit` | HEAD commit hash |
| `commit_subject` | First line of the commit message |

These fields power filtering in the Hook dock and segment log.

## Git heuristics

Git activity also **raises segment significance**:

| Heuristic | Score impact | Meaning |
|-----------|--------------|---------|
| **Git commit** | High | A commit landed during this segment |
| **Git branch change** | Medium | You switched branches mid-session |
| **Commit after playtest** | Very high | Commit within minutes of a playtest ending |

The **commit after playtest** pattern is especially valuable for devlog reconstruction — it links the iteration loop (run, fix, save, run) to a version-control checkpoint.

## Filtering by git context

In the Hook dock:

- **Filter clips…** on the clips list supports branch and tag filters
- **Segment log…** supports search by branch, score, and outcome

This lets you answer questions like:

- "What happened on `feature/boss-fight` before the merge?"
- "Show me every preserved segment that included a commit."
- "Which episodes had runtime errors on this branch?"

## Without git

Mnemonic works without a git repository. Clips still capture video, editor events, and heuristic tags — they simply lack branch and commit fields.

## Roadmap

Future phases will attach **per-commit file lists** to clip metadata, making it easier to connect visual episodes to specific diffs.

## Related

- [Development episodes](./development-episodes.md)
- [Narrative reconstruction](./narrative-reconstruction.md)
- [Heuristics guide](../guide/heuristics.md)
