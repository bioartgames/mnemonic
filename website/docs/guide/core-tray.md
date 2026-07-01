# Core tray

**Mnemonic Core** (`Mnemonic.Windows.exe`) is the Windows tray host that owns capture, encoding, heuristic scoring, and the local archive. It runs as a separate process from Godot.

The release zip includes a self-contained Core under `addons/mnemonic/core/`. You do not install .NET or FFmpeg separately.

## Launching Core

Core starts from the Mnemonic dock:

1. **Start recording** (recommended) — Mnemonic launches bundled Core automatically. The dock shows **Core started (bundled)**.
2. **Auto-launch** — Optional Mnemonic setting to start Core when Godot opens.

To use a different executable, set **Editor → Editor Settings → mnemonic → Core Windows Exe**. See [Installation](../installation.md#custom-core-path).

Core uses a **single-instance mutex**. Launching a second copy shows a Windows "already running" dialog.

## Tray menu

Right-click the tray icon for:

| Item | Purpose |
|------|---------|
| Status | Recording state and error summary |
| **Segment log…** | Full segment history (no search — use Mnemonic dock for that) |
| **Settings…** | Audio devices and capture restart |
| Exit | Shut down Core |

The tray status menu shows recording state without a red-dot overlay on the icon.

## Settings window

Tray **Settings…** owns audio device selection and capture restart. Capture retention and heuristic toggles live in the Mnemonic dock **Capture…** panel — both read/write the same `settings.json`.

## What Core does each segment

1. **Capture** screen and audio into the current scratch buffer
2. **Tail-poll** session events from Mnemonic
3. **Poll git** for commits and branch changes
4. **Score heuristics** as events arrive
5. **Close segment** at the configured interval
6. **Preserve or discard** based on score vs threshold (or manual flag)
7. **Write** `clip.json`, `video.mp4`, `thumb.jpg` for preserved segments
8. **Append** to `segment_history.jsonl` and update `clips_index.json`

## FFmpeg

Core ships a pinned **BtbN** FFmpeg build for monitor capture and H.264/AAC encoding. No PATH setup required.

## Related

- [Installation](../installation.md)
- [DataRoot layout](./data-root.md)
- [Development episodes](../concepts/development-episodes.md)
