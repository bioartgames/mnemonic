# Core-only Windows smoke test (CRG-104)

Formal E2E verification for **Mnemonic Core** without Godot: capture, IPC status, flag preserve, crash handling, and graceful shutdown.

**Linear:** [CRG-104](https://linear.app/lock-and-key/issue/CRG-104)

---

## Prerequisites

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Network access on first run (downloads bundled FFmpeg via `scripts/fetch-ffmpeg.ps1`)
- Repo clone with `mnemonic-core/` at the root

---

## Quick automated run

From `mnemonic-core/`:

```powershell
$env:MNEMONIC_SMOKE = '1'
powershell -File scripts\smoke-core.ps1 -CleanDataRoot
```

The script sets `MNEMONIC_SMOKE=1` automatically so first-run `settings.json` seeds system WASAPI mic + desktop loopback for audio checks.

**Pass:** prints `SMOKE PASS` and exits `0`.

**Options:**

| Flag | Purpose |
|------|---------|
| `-CleanDataRoot` | Delete `%LOCALAPPDATA%\Mnemonic` before run (fresh DataRoot) |
| `-SkipPublish` | Use `bin\Release\` build instead of `dist\smoke` publish |
| `-FullSegment` | Also wait 125s and assert `current_segment_index >= 1` |

---

## What the script verifies

| Phase | Checks |
|-------|--------|
| Publish | `dist\smoke\Mnemonic.Windows.exe` + `ffmpeg\bin\ffmpeg.exe` |
| Recording | `status.json`: `recording=true`, `state=recording`, `ffmpeg_ok=true` |
| DataRoot | `scratch/`, `clips/`, `control/`, `events/`, `logs/`, `settings.json` |
| Scratch | `mn_*_segment_00000.mp4` >= 100 KB + `ffmpeg` running |
| Flag | `{}` in `flag_current.json` deleted within 5s |
| Preserve | Kill `ffmpeg` after flag → `clips/segment_NNNNN/video.mp4` >= 100 KB + `clip.json` sidecar |
| Audio | Loopback beeps during capture; `volumedetect` on preserved clip (not padding-only); no errors in `audio_*_*.log` |
| Video | Preserved clip frame bulk luma span > 30 (not flat grey ddagrab-style) |
| Idle | After final host stop: no orphan processes |
| Crash | Kill `ffmpeg.exe` → `state=error`, no respawn within 6s |
| Shutdown | No orphan `Mnemonic.Windows` or `ffmpeg` |

---

## Fresh-machine checklist (manual, VM optional)

Use when validating on a machine **without** the dev repo (or for extra confidence).

1. **Prepare artifact** on a build machine:
   ```powershell
   cd mnemonic-core
   powershell -File scripts\fetch-ffmpeg.ps1
   dotnet publish src\Mnemonic.Windows\Mnemonic.Windows.csproj -c Release -o dist\smoke
   ```
2. Copy the entire `dist\smoke\` folder to the target machine.
3. Confirm **no** existing `%LOCALAPPDATA%\Mnemonic` (delete if present).
4. Install [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) on the target if not using framework-dependent publish with SDK.
5. Run `Mnemonic.Windows.exe` from the copied folder (hidden host starts capture).
6. Verify `control\status.json` under `%LOCALAPPDATA%\Mnemonic\` shows `recording=true`.
7. After ~10s, confirm `scratch\mn_*_segment_00000.mp4` exists and grows.
8. Write `{}` to `control\commands\flag_current.json`; confirm file disappears within ~2s.
9. Stop the host (Task Manager → end `Mnemonic.Windows`); confirm `clips\segment_00000\video.mp4` exists.
9a. Tray → **Segment log…** → confirm at least one history line after segment close; **Clear log** removes entries; **Settings…** → **Segment log retention** trims file on save when lowered.
10. **Optional:** Start a second `Mnemonic.Windows.exe` → expect “already running” dialog (mutex).
11. Re-run automated smoke on the build machine with `-CleanDataRoot` for regression.

---

## Expected artifacts

| Path | Pass criteria |
|------|----------------|
| `%LOCALAPPDATA%\Mnemonic\control\status.json` | Valid JSON; snake_case fields; `contract_version: 1` |
| `%LOCALAPPDATA%\Mnemonic\scratch\mn_*_segment_*.mp4` | Present while recording; non-zero size when active |
| `%LOCALAPPDATA%\Mnemonic\clips\segment_NNNNN\video.mp4` | After flag + host stop |
| `%LOCALAPPDATA%\Mnemonic\clips\segment_NNNNN\clip.json` | After preserve; `id` matches folder, `duration_seconds: 120`, `score` present; after `git commit` during recording, `files_modified` lists changed paths (non-empty when git repo available) |
| `%LOCALAPPDATA%\Mnemonic\control\segment_history.jsonl` | After each segment close; one JSON object per line (`contract_version: 1`, score, preserved, breakdown, git) |
| `%LOCALAPPDATA%\Mnemonic\logs\ffmpeg_*.log` | Per recording session; FFmpeg stderr while capture runs |
| `%LOCALAPPDATA%\Mnemonic\logs\audio_*_mic.log` / `*_desktop.log` | WASAPI pump conversion errors (throttled) |

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Build fails: FFmpeg missing | `powershell -File scripts\fetch-ffmpeg.ps1` |
| `Mnemonic.Windows` already running | `Stop-Process -Name Mnemonic.Windows, ffmpeg -Force` |
| Publish locked | Stop host; delete `dist\smoke` and retry |
| Scratch MP4 timeout (under 100 KB) | Check `%LOCALAPPDATA%\Mnemonic\logs\ffmpeg_*.log`; desktop capture permissions; ensure FFmpeg child is running |
| Capture stalls | Inspect `logs/ffmpeg_*.log`; `status.json` error shows `FFmpeg capture exited (code N)` |
| Flag not consumed | Host must be recording; wait up to 1s (command poller) |
| No audio / quiet capture after upgrade | Re-pick mic and desktop output in tray **Settings** (device IDs changed from DirectShow names to WASAPI endpoint IDs); restart Mnemonic |
| Flat AAC / silence in clip | Check `logs/audio_*_mic.log` and `*_desktop.log` for conversion errors; see `AudioPcmConverter` (IEEE float → s16le) |
| FFmpeg missing gfxcapture | Use bundled FFmpeg from `fetch-ffmpeg.ps1` or an override build that includes `gfxcapture` |
| Grey / flat video in clip | Old FFmpeg using ddagrab only; re-fetch and confirm `gfxcapture` in the running command line |
| Electron/HWA black regions | [CAPTURE_VIDEO_DDAGRAB.md](CAPTURE_VIDEO_DDAGRAB.md) |

### Manual post-smoke (capture quality)

After `SMOKE PASS`, on a machine with audio hardware:

1. Tray **Settings** — device lists populate **without** spawning a second `ffmpeg.exe`.
2. Record ~30s with mic + desktop selected; play preserved `video.mp4` — video should not stutter continuously; both audio tracks should be audible in VLC (switch tracks if needed).
3. Optional: `ffprobe -show_streams` on a clip — expect 1 video + 1–2 AAC streams; running capture `ffmpeg` command line should include `gfxcapture=monitor_idx=`, not `ddagrab`.
4. Video quirks (Electron, HWA overlays): [CAPTURE_VIDEO_DDAGRAB.md](CAPTURE_VIDEO_DDAGRAB.md).

---

## CRG-104 acceptance mapping

| Acceptance | Script phase |
|------------|----------------|
| Install → segments in `scratch/` | Phase 1–2 |
| Manual flag → `clips/.../video.mp4` | Phase 3 |
| `status.json` recording / error / idle | Phases 2–4 |
| DataRoot under `%LOCALAPPDATA%\Mnemonic` | Phase 2 |
| Kill ffmpeg → error, no restart; stop → no orphans | Phase 4 |
| Fresh machine | `-CleanDataRoot` + publish + manual checklist above |

**Out of scope:** Godot hook (CRG-112).
