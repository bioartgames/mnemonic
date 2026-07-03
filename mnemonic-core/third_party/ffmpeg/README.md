# Bundled FFmpeg (Windows x64)

Mnemonic Core ships a pinned **BtbN** `win64-gpl-shared` build for **gfxcapture** monitor capture, H.264/AAC encoding, and segment muxing without requiring PATH.

**Audio** is captured in Core via **NAudio WASAPI** and fed to FFmpeg through named pipes (`s16le` 48 kHz stereo). FFmpeg is not used for audio device enumeration or capture.

## One-time setup

From `mnemonic-core/`:

```powershell
powershell -File scripts\fetch-ffmpeg.ps1
```

This populates `win-x64/bin/` (gitignored). The Windows project copies `bin/` to `ffmpeg/bin/` beside `Mnemonic.Windows.exe` on build. All `*.dll` files must sit next to `ffmpeg.exe`.

## Pinned release

- Tag: [autobuild-2026-06-09-15-17](https://github.com/BtbN/FFmpeg-Builds/releases/tag/autobuild-2026-06-09-15-17)
- Archive: `ffmpeg-N-124881-g6028720d70-win64-gpl-shared.zip`

## Verify after fetch

```powershell
(Get-ChildItem .\third_party\ffmpeg\win-x64\bin\*.dll).Count
.\third_party\ffmpeg\win-x64\bin\ffmpeg.exe -hide_banner -version
.\third_party\ffmpeg\win-x64\bin\ffmpeg.exe -hide_banner -filters | Select-String gfxcapture
```

Bootstrap requires the **gfxcapture** filter (Windows Graphics Capture). Override FFmpeg paths must provide it as well.

## Video capture behavior

See [docs/CAPTURE_VIDEO_DDAGRAB.md](../../docs/CAPTURE_VIDEO_DDAGRAB.md) for monitor index, cursor drawing, and compositor behavior (gfxcapture vs legacy ddagrab).

## License

See [LICENSE-FFMPEG.txt](LICENSE-FFMPEG.txt) (created by fetch script) and [THIRD_PARTY_NOTICES.md](../../THIRD_PARTY_NOTICES.md).
