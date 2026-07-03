# DataRoot layout

All Mnemonic runtime data lives under **DataRoot**:

```
%LOCALAPPDATA%\Mnemonic\
```

This path is **not** Godot's `user://` directory. Mnemonic and Core agree on DataRoot via a shared IPC contract (version 1).

## Directory structure

```
Mnemonic/
├── settings.json              # Capture and heuristic settings
├── scratch/                   # Live / discarded segment buffers (mn_*_segment_*.mp4)
├── clips/
│   └── mn_<id>_segment_NNNNN/ # Preserved clip folder (id from capture prefix + index)
│       ├── video.mp4
│       ├── thumb.jpg
│       └── clip.json
├── events/
│   └── session_events.jsonl   # Mnemonic → Core editor events
├── logs/                      # Core log output
└── control/
    ├── status.json            # Core heartbeat (recording state, live preview)
    ├── segment_history.jsonl  # Every segment close (kept + discarded)
    ├── editor_scene.json      # Latest editor scene snapshot for heuristics
    ├── clips_index.json       # Index of preserved clips for dock listing
    ├── suggested_groups.json  # Suggested clip groupings
    └── commands/              # Ephemeral command files (consumed by Core)
        ├── flag_current.json
        ├── pause_capture.json
        ├── resume_capture.json
        ├── exit_core.json
        └── rebuild_clips_index.json
```

## Key files

### settings.json

Written by Mnemonic **Capture…** panel and read by Core. Includes segment length, preserve threshold, heuristic toggles, and capture cursor. Audio device selection is configured in the **Core tray**, not Mnemonic.

### status.json

Core writes `control/status.json` on each poll cycle. Mnemonic reads it for dock status, LIVE row preview, and error display.

### session_events.jsonl

Append-only log of editor events from Mnemonic. Core tail-polls this file for heuristic scoring.

Each line is a JSON object with event type, timestamp, and payload.

### segment_history.jsonl

One JSON object per line for every segment close:

- `contract_version`: 1
- Score, threshold, preserved boolean
- Heuristic breakdown
- Git branch and commit at close time
- Time range

### clips_index.json

`control/clips_index.json` is the index of all preserved clips for fast Mnemonic dock listing. Version 1 schema.

### clip.json

Per-clip metadata. See [Development episodes](../concepts/development-episodes.md) for field descriptions.

## Command files

Mnemonic writes ephemeral command files in `control/commands/`:

| File | Purpose |
|------|---------|
| `flag_current.json` | Request manual preserve of live segment |
| `pause_capture.json` | Request capture pause |
| `resume_capture.json` | Request capture resume |
| `exit_core.json` | Request graceful Core shutdown |
| `rebuild_clips_index.json` | Request clips index rebuild |

Core consumes and deletes these within seconds.

## Privacy

DataRoot stays on your local machine. No network upload occurs in current phases. Back up `%LOCALAPPDATA%\Mnemonic\` if you want to preserve your development archive across machine migrations.

## Related

- [Core tray](./core-tray.md)
- [Automatic capture](../concepts/automatic-capture.md)
