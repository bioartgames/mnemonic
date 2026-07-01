# Mnemonic — Install

Turnkey install for the Godot editor addon with bundled Mnemonic Core (no build tools required).

## Requirements

- **Windows 10 or later**
- **Godot 4.6+** (editor)

Core and FFmpeg are included under `core/` in this folder. No .NET SDK or separate FFmpeg install is needed.

## Install

1. Download [mnemonic-0.1.0-p2.zip](https://github.com/bioartgames/mnemonic/releases/download/v0.1.0-p2/mnemonic-0.1.0-p2.zip) and extract it.
2. Copy the `mnemonic/` folder into your Godot project's `addons/` directory (merge with any existing `addons/` folder).
3. Open your project in Godot → **Project → Plugins** → enable **Mnemonic**.

The Mnemonic dock appears in the left upper editor slot.

## Start recording

With Core stopped, click **Start recording** in the Mnemonic dock. Mnemonic launches the bundled `core/Mnemonic.Windows.exe` automatically. The dock toast shows **Core started (bundled)**.

## Data location

All runtime data lives at:

```
%LOCALAPPDATA%\Mnemonic\
```

This is not Godot's `user://` path.

## Optional: custom Core path

Set an absolute path in **Editor → Editor Settings → mnemonic → Core Windows Exe** to override the bundled executable.

## Third-party

Bundled FFmpeg is subject to the license in `core/LICENSE-FFMPEG.txt`.
