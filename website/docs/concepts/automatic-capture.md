---
sidebar_position: 2
---

# Automatic capture

Mnemonic captures your development session **passively**. You do not start and stop a recorder for each interesting moment — Core runs continuously and segments the stream automatically.

## What gets captured

| Stream | Source | Purpose |
|--------|--------|---------|
| **Video** | Monitor capture via FFmpeg | Visual record of editor and playtest |
| **Microphone** | WASAPI input device | Spoken commentary and thinking aloud |
| **System audio** | WASAPI loopback (optional) | In-engine sounds during playtests |
| **Editor events** | Mnemonic addon → JSONL ingest | Scene saves, playtests, errors, transitions |

Capture settings (segment length, preserve threshold, cursor capture, heuristic toggles) write to `%LOCALAPPDATA%\Mnemonic\settings.json` — the same file Core reads.

## Mnemonic → Core event flow

```
Godot Editor (Mnemonic addon)
    │
    │  session events (JSONL append)
    ▼
%LOCALAPPDATA%\Mnemonic\events\session_events.jsonl
    │
    │  tail poll
    ▼
Mnemonic Core (tray host)
    │
    ├── Heuristic scorer
    ├── Git poll service
    └── Segment close → clip or discard
```

The Mnemonic editor addon never encodes video. It emits lightweight events and reads status from Core's IPC files.

## Starting and stopping

From the Mnemonic dock:

- **Start recording** — launches Core if needed and begins capture
- **Stop recording** — sends graceful shutdown via `exit_core`

Core is a single-instance Windows process. A second launch shows an "already running" dialog.

### Auto-launch options

In Mnemonic settings:

- **Start recording when Godot opens** — optional convenience
- **Stop recording when Godot closes** — prevents orphaned capture hosts

## Live preview

While recording, the Mnemonic dock **LIVE** row shows:

- Segment countdown (e.g. `3m left`)
- Current segment index
- Running segment score from `status.json`

Double-click **LIVE** or use **Save segment** to manually preserve the current window.

## Heuristic-driven retention

Automatic capture would be useless without automatic **selection**. Core scores each segment using heuristics derived from editor and git signals. Only segments that cross the preserve threshold — or are manually flagged — become permanent clips.

This is the core design bet: **capture everything in scratch, keep what mattered**.

## Performance

Capture and encoding run in the Core process, not inside Godot. Mnemonic defers heavy initialization (dock layout, clip thumbnails, process probes) until after the editor loading bar completes.

## Related

- [Development episodes](./development-episodes.md)
- [Mnemonic dock guide](../guide/mnemonic-dock.md)
- [Core tray guide](../guide/core-tray.md)
