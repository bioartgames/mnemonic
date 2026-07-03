# Desktop video capture (gfxcapture)

Mnemonic Core captures the Windows desktop with FFmpeg’s **gfxcapture** filter (Windows Graphics Capture API, monitor mode), not `ddagrab`, `gdigrab`, or `dshow`.

**Why not ddagrab?** On typical Windows 10/11 desktops with Electron/Cursor/Godot, DXGI Desktop Duplication (`ddagrab`) often returns a **flat, desaturated** frame (most pixels share the same luma). That looks like grey nothingness in playback even though audio and muxing are fine. **gfxcapture** at `monitor_idx=0` captures the composited monitor image users actually see.

Audio is separate: **NAudio WASAPI** → named pipes → FFmpeg.

---

## What gfxcapture captures

| Setting | Constant / behavior |
|---------|---------------------|
| Monitor | `monitor_idx=0` — primary display (`MnemonicConstants.CaptureMonitorIndex`) |
| Frame rate | `max_framerate=30` (`CaptureVideoFramerate`) |
| Cursor | Tray **Draw mouse** → `capture_cursor=1` or `0` |
| Pixel path | `gfxcapture` → `hwdownload` → `format=bgra` → H.264 (`yuv420p`) |

Capture is **one full monitor surface** (composited desktop), not per-window capture.

---

## Expected behavior

### Native / frequently repainting apps (e.g. Godot)

- Generally visible in the capture while the desktop compositor updates that region.

### Electron / Chromium (Cursor, browsers)

- Usually **much better** than ddagrab because WGC captures the composed result.
- Protected/HWA video may still be black or frozen in some players.

### Multi-monitor

- Only **primary** (`monitor_idx=0`) today. Future work: expose monitor index in settings.

---

## Verification

```powershell
.\third_party\ffmpeg\win-x64\bin\ffmpeg.exe -hide_banner -filters | Select-String gfxcapture
```

While recording, the `ffmpeg` command line should include `gfxcapture=monitor_idx=`, not `ddagrab`.

Deterministic regression (Windows):

```powershell
cd mnemonic-core
dotnet test --filter "FullyQualifiedName~VideoCaptureFrameProbe"
```

---

## Troubleshooting

| Symptom | Likely cause | What to try |
|---------|----------------|-------------|
| Grey / flat video in clip | Old FFmpeg without gfxcapture, or wrong monitor index | Re-run `scripts\fetch-ffmpeg.ps1`; confirm `gfxcapture` in probe |
| Black video region | Protected / overlay content | Expected limitation; test with windowed non-HW player |
| Wrong monitor | `monitor_idx=0` only | Move apps to primary display |
| No video / FFmpeg error | Missing gfxcapture | Check `logs\ffmpeg_*.log`; update bundled FFmpeg |

---

## Related docs

- [SMOKE_CORE_WINDOWS.md](SMOKE_CORE_WINDOWS.md)
- [../third_party/ffmpeg/README.md](../third_party/ffmpeg/README.md)
