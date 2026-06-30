---
sidebar_position: 3
---

# FAQ

Traditional devlogging asks you to stop working and write things down. Mnemonic asks nothing of you during flow — it runs beside Godot and preserves development episodes with video, metadata, and git linkage for when you are ready to tell the story.

## What is Mnemonic?

Mnemonic is a Godot development memory system. It automatically captures time-bounded segments of your work session, scores them for significance, and preserves meaningful episodes with video, editor metadata, and git context. See the [introduction](./intro.md) for an overview.

## Which platforms are supported?

Today Mnemonic targets Windows with Godot 4.6+. The release zip includes a bundled, self-contained Core tray host — no .NET SDK required. The Hook addon runs inside the Godot editor. macOS and Linux support may follow as the capture stack matures.

## Does Mnemonic upload my footage to the cloud?

No. All capture data stays on your machine under `%LOCALAPPDATA%\Mnemonic\`. There is no cloud upload in current or planned phases.

## How is this different from OBS or screen recording?

OBS records what you tell it to. Mnemonic records continuously in segments, watches Godot editor signals and git activity, scores each segment, and keeps only what matters — with structured metadata for later devlog reconstruction.

## What happens to segments that score below the preserve threshold?

They remain in scratch storage and are eventually overwritten. Every segment close is logged in `segment_history.jsonl` so you can review what was kept or discarded, even for low-scoring windows.

## Can I force-save the current segment?

Yes, when the dock allows it. Use **Save segment** in the Hook dock or double-click the LIVE row when the current score is below the preserve threshold. The dock blocks **Save segment** when the score already meets the threshold (auto-save would apply). You can still request manual preserve via the Core flag file for automation.

## Do I need to commit to git for Mnemonic to work?

No, but git integration enriches clips significantly. Without a repository, clips still capture video and editor events — they just lack branch and commit metadata.

## Will Mnemonic slow down my Godot editor?

Hook is designed to defer heavy work until after editor startup. Event emission is lightweight JSONL append; capture and encoding happen in the separate Core process.

## What are heuristics?

Heuristics are significance signals — playtest starts, scene saves, runtime errors, git commits, iteration cycles, and more. Each contributes to a segment score. Read the [heuristics guide](./guide/heuristics.md) for the full catalog.

## Where can I see the full feature list?

The [Features](/features) page walks through capture, git linkage, narrative reconstruction, and the Hook dock in detail.

## What is on the roadmap?

See the [Roadmap](/roadmap) page for planned phases and what is shipping today.

## Is Mnemonic open source?

The project lives on [GitHub](https://github.com/bioartgames/mnemonic-hook). Check the repository for the current license and contribution guidelines.
