# Mnemonic — Developer guide

Monorepo layout and commands for building Core, packaging releases, and local testing.

## Layout

| Path | Purpose |
|------|---------|
| `addons/mnemonic/` | Godot editor addon |
| `mnemonic-core/` | .NET 8 Windows tray host and CLI |
| `website/` | Documentation site (Docusaurus) |

## Prerequisites

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Godot 4.6+ (editor) for manual addon smoke tests

## Build Core

```powershell
cd mnemonic-core
dotnet build Mnemonic.sln
```

## Test Core

```powershell
cd mnemonic-core
dotnet test Mnemonic.sln
```

## Package release zip

From the repository root:

```powershell
powershell -File scripts/package-release.ps1
```

This runs tests, publishes a self-contained Core bundle, copies it to `addons/mnemonic/core/`, and writes `dist/mnemonic-<version>.zip` (version from `addons/mnemonic/plugin.cfg`).

## Smoke test (Core only)

Destructive: deletes `%LOCALAPPDATA%\Mnemonic` before running.

```powershell
powershell -File mnemonic-core/scripts/smoke-core.ps1 -CleanDataRoot
```

See [mnemonic-core/docs/SMOKE_CORE_WINDOWS.md](mnemonic-core/docs/SMOKE_CORE_WINDOWS.md) for details.

## Dev Core resolution

The addon resolves the Core executable in this order:

1. Bundled `res://addons/mnemonic/core/Mnemonic.Windows.exe` (after packaging)
2. `mnemonic-core/src/Mnemonic.Windows/bin/Release/net8.0-windows/Mnemonic.Windows.exe` relative to the Godot project parent (`res://..`)
3. Other dev paths listed in `addons/mnemonic/ipc/hook_core_launcher.gd`
4. Custom path in **Editor → Editor Settings → mnemonic → Core Windows Exe**

For monorepo dev, place your Godot project so its parent directory is this repository root, or run `package-release.ps1` and use the bundled path.

## What not to commit

- `addons/mnemonic/core/*` except `.gdignore` (Godot ignore marker for bundled binaries)
- `mnemonic-core/bin/`, `mnemonic-core/obj/`, `mnemonic-core/dist/`
- `mnemonic-core/third_party/ffmpeg/win-x64/bin/`
- `dist/` (release zips and staging)

## Release

1. Ensure `addons/mnemonic/plugin.cfg` version matches the intended tag.
2. Run `scripts/package-release.ps1` and `smoke-core.ps1 -CleanDataRoot` locally.
3. Commit and push to `main`.
4. Tag and push, e.g. `v0.1.0-p2`.

Pushing a `v*` tag triggers [`.github/workflows/release.yml`](.github/workflows/release.yml), which builds the zip and attaches it to the GitHub Release.
