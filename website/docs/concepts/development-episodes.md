---
sidebar_position: 1
---

# Development episodes

A **development episode** in Mnemonic is a time-bounded **segment** of your work session — not a manual note, not a screenshot, but a structured slice of real work with optional video preservation.

## Segments

Core divides capture into segments. The default segment length is **120 seconds** (configurable from 30 to 600 seconds in Hook settings).

While recording:

1. Core continuously writes to a **live scratch** buffer for the current segment.
2. The Hook dock **LIVE** row shows a countdown, segment index, and running significance score.
3. When the segment closes, Core evaluates heuristics and decides whether to **preserve** or **discard** it.

Preserved segments become **clips** in your archive. Discarded segments remain in scratch until overwritten.

## Significance scoring

Each segment receives a score based on editor and git activity during that window. Examples of high-significance signals:

| Signal | Category | Why it matters |
|--------|----------|----------------|
| Iteration cycle | Editor | Playtest shortly after a scene save — the classic fix loop |
| Runtime error | Playtest | Script failure during a playtest |
| Git commit | Git | You checkpointed work during this window |
| Rapid playtest | Playtest | Several playtests in a short span |
| Save burst | Editor | Multiple distinct scenes saved — broad refactor energy |
| Edit intensity | Editor | Sustained saves/transitions with little playtest (scripting, LD, dialog) |
| Scene hopping | Editor | Many scene tabs opened without running the game |

Each heuristic has a configurable weight and score cap. See the [heuristics guide](../guide/heuristics.md) for the full catalog.

## Preserve threshold

Segments scoring **at or above** the preserve threshold are written to the clips archive with `video.mp4`, `thumb.jpg`, and `clip.json`.

Segments below the threshold are discarded from scratch but still logged in `segment_history.jsonl` — so you can review what almost mattered.

**Manual preserve** always wins: **Save segment** in the Hook dock flags the live segment regardless of score.

## Clip metadata

A preserved clip's `clip.json` includes:

```json
{
  "id": "segment_00042",
  "created_at": 1717430400,
  "duration_seconds": 120,
  "score": 16,
  "git_commit": "a1b2c3d",
  "git_branch": "feature/player-movement",
  "commit_subject": "Fix jump buffer timing",
  "scenes_active": ["res://player/player.tscn"],
  "tags": ["playtest", "runtime_error", "iteration_cycle"]
}
```

## Segment history

Every segment close appends a line to `control/segment_history.jsonl`:

- Time range and outcome (kept / discarded)
- Final score and threshold used
- Heuristic breakdown
- Git context at close time

Open **Segment log…** from the Hook dock or Core tray to browse this history.

## Why episodes, not continuous recording

Continuous screen recording creates hours of footage nobody watches. Episodes give you:

- **Automatic triage** — keep the windows where something happened
- **Structured metadata** — search by branch, tag, or score instead of scrubbing video
- **Devlog-ready units** — each clip is a candidate paragraph in your development story

## Related

- [Automatic capture](./automatic-capture.md)
- [Narrative reconstruction](./narrative-reconstruction.md)
