---
sidebar_position: 2
---

# Installation

Mnemonic requires **Windows 10 or later** and **Godot 4.6+**.

## Quick install (release zip)

The release zip includes a self-contained Mnemonic Core under `addons/mnemonic/core/` — no .NET SDK or FFmpeg install required.

1. Download the [latest release zip](https://github.com/bioartgames/mnemonic/releases/latest) from GitHub Releases.
2. Extract the zip and copy `mnemonic/` into your Godot project's `addons/` folder.
3. Open the project in Godot → **Project → Plugins** → enable **Mnemonic**.
4. Click **Start recording** in the Mnemonic dock. Mnemonic launches bundled Core automatically (**Core started (bundled)**).

See also `addons/mnemonic/INSTALL.md` inside the addon folder.

### Custom Core path

Set an absolute path in **Editor → Editor Settings → mnemonic → Core Windows Exe** to override the bundled executable.

## Data location

All runtime data lives at:

```
%LOCALAPPDATA%\Mnemonic\
```

This is **not** Godot's `user://` path. See the [DataRoot guide](./guide/data-root.md) for the full layout.

## Prerequisites checklist

| Requirement | Notes |
|-------------|-------|
| Windows 10+ | Core capture uses Windows APIs |
| Godot 4.6+ | Mnemonic targets Godot 4.x editor APIs |
| Git (optional) | Enriches clips with branch and commit metadata |

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| **Start recording** disabled | Install the release zip (bundled `core/`) |
| Core already running dialog | Only one Core instance allowed (mutex). Stop the existing tray host |
| No clips appearing | Wait for a segment to close, or use **Save segment** |
| Capture settings not applied | Restart capture via tray **Settings…** or relaunch Core |

## Next steps

- [Mnemonic dock guide](./guide/mnemonic-dock.md)
- [Core tray guide](./guide/core-tray.md)
- [Development episodes](./concepts/development-episodes.md)
