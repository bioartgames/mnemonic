# Mnemonic dock

The **Mnemonic** dock is the primary interface inside the Godot editor. It lives in the **left upper** dock slot and splits into transport controls above and the clips archive below.

## Layout

```
┌─────────────────────────────────┐
│  [Start/Stop]  [⚙ settings]     │  ← transport + gear
│  [Filter clips…]  [reload]            │  ← clips toolbar
├─────────────────────────────────┤
│  LIVE  ·  3m left  ·  score 12  │  ← live row (while recording)
│  segment_00041  ·  playtest       │
│  segment_00040  ·  iteration      │  ← preserved clips
│  ...                            │
└─────────────────────────────────┘
```

Drag the splitter between panes to resize. The offset persists in Editor Settings (`mnemonic/dock_clips_split_offset`).

## Transport controls

| Control | Action |
|---------|--------|
| **Start recording** | Launch Core and begin capture |
| **Stop recording** | Graceful Core shutdown |

**Save segment** is on the LIVE row ⋮ menu (not in the transport row). It is disabled when Core is not running, a save is already pending, Core is already processing a manual preserve, or the live score is **at or above the preserve threshold** (auto-save would apply at segment end).

## LIVE row

While Core records, the LIVE row shows:

- Countdown to segment close
- Current segment index
- Running significance score

Double-click **LIVE** or choose **Save segment** from the row menu to flag manual preserve when save is allowed (same rules as the menu above). A blue accent animation runs until Core consumes the flag file.

## Clips list

- Shows the **top 50** clips by `created_at`
- Thumbnails load in batches for performance
- Double-click a preserved clip to **play** `video.mp4`
- **Reveal in file manager** opens the clip folder

Empty states show helpful icons for "no clips yet" and "no filter matches."

## Settings (gear menu)

The gear button opens two areas:

### Recording submenu

- **Start recording when Godot opens**
- **Stop recording when Godot closes**
- **Verbose logging**

These map to Editor Settings keys under `mnemonic/`.

### Editor Settings vs settings.json

- **`mnemonic/auto_launch_core`** (default **off**) — start recording when you open this project in Godot
- **`mnemonic/stop_core_on_editor_exit`** (default **off**) — stop recording and quit Mnemonic when you close Godot
- **`settings.json` → `start_recording_on_launch`** (default **on**) — written when you click **Start recording**

### Capture… panel

| Setting | Range / type | Effect |
|---------|--------------|--------|
| Segment length | 30–600 seconds | How long each episode window lasts |
| Preserve threshold | Integer | Minimum score to auto-preserve |
| Notable score min | Integer | Minimum score for Notable tier labels (display only) |
| Highlight score min | Integer | Minimum score for Highlight tier labels (display only) |
| Segment log retention | 10–1000 entries | History file trim on save |
| Capture cursor | Checkbox | Include mouse cursor in video |
| Heuristic signals | Per-type toggles | Enable/disable score contributors |

Changes write to `%LOCALAPPDATA%\Mnemonic\settings.json`. Some capture options require restarting capture to take effect.

## Segment log

Open **Segment log…** from the dock to browse closed segments:

- Newest first, last 50 lines in the dock panel
- Search by branch, score, outcome, and more
- Selectable lines for copy/paste into devlog drafts
- **Clear segment log** truncates the history file (with confirmation)

## Status display

Status messages are **hidden** when Core is healthy and idle. They appear when:

- Core is not running
- Core is waiting for `status.json`
- Core is recording, in error, or has FFmpeg issues

## Related

- [Installation](../installation.md)
- [Core tray](./core-tray.md)
- [Automatic capture](../concepts/automatic-capture.md)
