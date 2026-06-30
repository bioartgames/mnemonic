# Mnemonic Hook

Godot editor addon that captures development episodes — screen, audio, and editor context — with bundled Mnemonic Core (no build tools required).

## Install

1. Download the latest release from [GitHub Releases](https://github.com/bioartgames/mnemonic-hook/releases).
2. Extract the zip and copy `mnemonic_hook/` into your Godot project's `addons/` folder.
3. Open your project in Godot → **Project → Plugins** → enable **Mnemonic Hook**.
4. Click **Start recording** in the Mnemonic dock.

See [`mnemonic_hook/INSTALL.md`](mnemonic_hook/INSTALL.md) for details.

## Documentation

https://bioartgames.github.io/mnemonic-hook/

## Requirements

- Windows 10 or later
- Godot 4.6+ (editor)

## Bundled Core

Release zips include a self-contained `Mnemonic.Windows.exe` and FFmpeg under `mnemonic_hook/core/`. Core is not built from this repository.

## License

Addon source is [MIT](LICENSE). Bundled FFmpeg in release zips is subject to `core/LICENSE-FFMPEG.txt`.
