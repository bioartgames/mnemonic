---
sidebar_position: 4
---

# Narrative reconstruction

The goal of Mnemonic is not just to record — it is to help you **reconstruct the story** of how your game evolved. Narrative reconstruction turns raw development episodes into devlog-ready material.

## The archive as source material

After a few sessions, your local archive contains:

- Preserved **video clips** with thumbnails
- **Tags** summarizing what happened (playtest, error, iteration, commit)
- **Git context** tying episodes to branches and commits
- **Segment history** showing the full timeline, including discarded windows

You already did the work. Mnemonic preserved the evidence.

## Browsing and filtering

The Hook dock is your primary archive browser:

1. **Clips list** — newest preserved episodes, with LIVE row during capture
2. **Filter clips…** — narrow by branch, tags, significance, or search terms
3. **Play** — double-click a clip to open `video.mp4`
4. **Reveal** — open the clip folder in Explorer

The **Segment log…** panel shows every closed segment with score breakdowns — useful when you want the full timeline, not just the highlights.

## Reading significance tiers

Heuristic scores cluster into significance tiers (Phase 3). High-tier clips represent the moments most likely to belong in a devlog:

- First successful playtest of a new mechanic
- The session where you fixed a stubborn runtime error
- A save burst followed by rapid iteration
- A commit that landed right after a long debugging loop

Low-tier clips still exist in the archive — Mnemonic errs on the side of keeping context.

## From episodes to devlog

A typical reconstruction workflow:

1. **Pick a feature or milestone** — e.g. "player movement refactor"
2. **Filter clips** by branch, scene path, or tags like `iteration_cycle`
3. **Review segment history** for the surrounding timeline
4. **Watch key clips** — note timestamps, errors, and git subjects
5. **Draft the narrative** — each clip becomes a beat in the story

Mnemonic does not write the devlog for you today. It gives you the **ordered, searchable, contextual footage** that makes writing one feasible instead of impossible.

## Coming soon

The [roadmap](/roadmap) includes:

- **Custom clip tagging** — your own labels on preserved clips (Phase 5)
- **Selected-clip export** — bundle chosen clips to a folder you pick (Phase 6)
- **Voice transcription** — searchable spoken commentary (Phase 7, optional)

## Related

- [Development episodes](./development-episodes.md)
- [Git-linked history](./git-linked-history.md)
- [Hook dock guide](../guide/hook-dock.md)
