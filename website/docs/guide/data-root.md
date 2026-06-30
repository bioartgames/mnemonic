# DataRoot layout

All Mnemonic runtime data lives under **DataRoot**:

```
%LOCALAPPDATA%\Mnemonic\
```

This path is **not** Godot's `user://` directory. Hook and Core agree on DataRoot via a shared IPC contract (version 1).

## Directory structure

```
Mnemonic/
├── settings.json              # Capture and heuristic settings
├── status.json                # Core heartbeat (recording state, live preview)
├── clips/
│   ├── clips_index.json       # Index of preserved clips
│   └── segment_NNNNN/
│       ├── video.mp4
│       ├── thumb.jpg
│       └── clip.json          # Metadata for this clip
├── scratch/                   # Live / discarded segment buffers
├── events/
│   └── session_events.jsonl   # Hook → Core editor events
├── control/
│   ├── segment_history.jsonl  # Every segment close (kept + discarded)
│   ├── flag_current.json      # Manual preserve command (ephemeral)
│   └── exit_core.json         # Graceful shutdown command (ephemeral)
└── ffmpeg/                    # Bundled FFmpeg binaries
```

## Key files

### settings.json

Written by Hook **Capture…** panel and read by Core. Includes segment length, preserve threshold, heuristic toggles, and capture cursor. Audio device selection is configured in the **Core tray**, not Hook.

### status.json

Core writes this on each poll cycle. Hook reads it for dock status, LIVE row preview, and error display.

### session_events.jsonl

Append-only log of editor events from Hook. Core tail-polls this file for heuristic scoring.

Each line is a JSON object with event type, timestamp, and payload.

### segment_history.jsonl

One JSON object per line for every segment close:

- `contract_version`: 1
- Score, threshold, preserved boolean
- Heuristic breakdown
- Git branch and commit at close time
- Time range

### clips_index.json

Index of all preserved clips for fast Hook dock listing. Version 1 schema.

### clip.json

Per-clip metadata. See [Development episodes](../concepts/development-episodes.md) for field descriptions.

## Command files

Hook writes ephemeral command files in `control/`:

| File | Purpose |
|------|---------|
| `flag_current.json` | Request manual preserve of live segment |
| `exit_core.json` | Request graceful Core shutdown |

Core consumes and deletes these within seconds.

## Privacy

DataRoot stays on your local machine. No network upload occurs in current phases. Back up `%LOCALAPPDATA%\Mnemonic\` if you want to preserve your development archive across machine migrations.

## Related

- [Core tray](./core-tray.md)
- [Automatic capture](../concepts/automatic-capture.md)
