# Mnemonic Core

.NET 8 solution for the Mnemonic Windows tray host and CLI.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows

## Build

From this directory:

```bash
dotnet build Mnemonic.sln
```

## Run

Primary host (single-instance, hidden message pump until tray UI):

```bash
dotnet run --project src/Mnemonic.Windows
```

CLI stub:

```bash
dotnet run --project src/Mnemonic.Cli
```

## Smoke test

Core-only Windows E2E (CRG-104):

```powershell
powershell -File scripts\smoke-core.ps1 -CleanDataRoot
```

See [docs/SMOKE_CORE_WINDOWS.md](docs/SMOKE_CORE_WINDOWS.md) for the full checklist and fresh-machine steps.
