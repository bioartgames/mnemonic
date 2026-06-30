---
sidebar_position: 1
slug: /intro
---

# Introduction

**Mnemonic** is a development memory system for [Godot](https://godotengine.org). It watches your editor session, captures time-bounded **development episodes**, and preserves the ones worth keeping — with video, contextual metadata, and git linkage.

Traditional devlogging asks you to stop working and write things down. Mnemonic asks nothing of you during flow. It runs beside Godot, accumulates an archival record, and gives you the material to reconstruct how a feature evolved when you are ready to tell that story.

## The two-part architecture

Mnemonic splits responsibilities between the editor and a capture host:

| Component | Role |
|-----------|------|
| **Mnemonic Hook** | Godot editor addon (`addons/mnemonic_hook/`). Emits session events, provides the dock UI, and communicates with Core over local IPC. |
| **Mnemonic Core** | Windows tray host (`Mnemonic.Windows.exe`). Captures screen and audio, closes segments, scores heuristics, writes clips, and polls git. |

The Hook never runs FFmpeg or encodes video directly. Core owns capture, retention, and the local archive.

## What you get

- **Video clips** of meaningful development windows
- **Structured metadata** — score, tags, active scenes, git branch and commit
- **Segment history** — a log of every closed segment, kept or discarded
- **A browsable archive** in the Hook dock with search, filters, and playback

## Who is it for?

Mnemonic is for Godot developers who:

- Want devlog material without interrupting creative flow
- Need to remember *how* a feature came together, not just the final commit
- Prefer local-first tooling that respects privacy
- Work on Windows with Godot 4.6 or later

## Next steps

- [Install Mnemonic](./installation.md)
- [Understand development episodes](./concepts/development-episodes.md)
- [Hook dock guide](./guide/hook-dock.md)
- [FAQ](./faq.md)
- [Explore features](/features)
- [Read the roadmap](/roadmap)
