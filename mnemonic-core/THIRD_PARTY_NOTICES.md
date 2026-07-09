# Third-party notices

## FFmpeg

Mnemonic Core can bundle FFmpeg binaries for Windows screen/audio capture.

- **Project:** [FFmpeg](https://ffmpeg.org/)
- **Legal:** https://ffmpeg.org/legal.html
- **Build source:** [BtbN FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds) — `win64-gpl-shared`, release `autobuild-2026-07-08-13-30`
- **Version:** N-124496 (May 2026 autobuild)
- **License text:** `third_party/ffmpeg/LICENSE-FFMPEG.txt` (after running `scripts/fetch-ffmpeg.ps1`)

FFmpeg is licensed under the LGPL/GPL depending on enabled components. Corresponding source code is available from the FFmpeg project and the BtbN build repository above.

Users may optionally set `ffmpeg_path_override` in `%LOCALAPPDATA%\Mnemonic\settings.json` to use a different FFmpeg installation. Override builds must include the **gfxcapture** filter.

## NAudio

Mnemonic Core uses **NAudio** for Windows WASAPI microphone and loopback capture.

- **Project:** [NAudio](https://github.com/naudio/NAudio)
- **Version:** 2.2.1 (NuGet)
- **License:** MIT — see https://github.com/naudio/NAudio/blob/master/license.txt
