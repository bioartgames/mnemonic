# Heuristics

**Heuristics** are significance signals that contribute to a segment's score. Core evaluates them continuously during capture; Mnemonic lets you enable or disable individual signal types.

## Catalog

| Type | Label | Category | Default weight | Cap |
|------|-------|----------|----------------|-----|
| `playtest_start` | Playtest start | Playtest | 7 | 5 |
| `playtest_ongoing` | Playtest ongoing | Playtest | 7 | 1 |
| `playtest_stop` | Playtest stop | Playtest | 0 | 0 |
| `rapid_playtest` | Rapid playtest | Playtest | 9 | — |
| `long_playtest` | Long playtest | Playtest | 8 | — |
| `runtime_error` | Runtime error | Playtest | 9 | 3 |
| `scene_save` | Scene save | Editor | 5 | 3 |
| `scene_transition` | Scene transition | Editor | 4 | 2 |
| `resource_saved` | Resource saved | Editor | 4 | 4 |
| `editor_focus_changed` | Editor focus changed | Editor | 0 | — |
| `activity_packet` | Activity packet | Editor | 0 | — |
| `save_burst` | Save burst | Editor | 6 | — |
| `resource_burst` | Resource burst | Editor | 6 | — |
| `edit_intensity` | Edit intensity | Editor | 8 | 2 |
| `scene_hopping` | Scene hopping | Editor | 6 | 1 |
| `script_focus` | Script focus | Editor | 7 | 1 |
| `layout_focus` | Layout focus | Editor | 6 | 1 |
| `long_edit_span` | Long edit span | Editor | 7 | 1 |
| `iteration_cycle` | Iteration cycle | Editor | 10 | — |
| `git_commit` | Git commit | Git | 9 | 1 |
| `git_branch_change` | Git branch change | Git | 6 | 1 |
| `git_push` | Git push | Git | 8 | 1 |
| `debug_session_start` | Debug session start | Playtest | 6 | 1 |
| `debug_session_stop` | Debug session stop | Playtest | 0 | 0 |
| `script_save` | Script save | Editor | 5 | 4 |
| `editor_focused_session` | Editor focused session | Editor | 8 | 1 |
| `commit_after_playtest` | Commit after playtest | Git | 10 | — |
| `checkpoint_after_work` | Checkpoint after work | Git | 9 | — |

Weights and caps are configurable in Mnemonic **Capture… → Signals**. `activity_packet`, `playtest_stop`, `debug_session_stop`, and `editor_focus_changed` are hidden from that UI (pipeline-only or stop events with no score).

## Activity packets

Core builds **segment-scaled rolling activity packets** from atomic editor events (`scene_save`, `resource_saved`, `scene_transition`, focus changes, playtest spans). The packet window is `round(segment_duration / 2)` seconds, clamped to **15–120s** (e.g. 120s segment → 60s window, 60s segment → 30s).

Packets are internal (weight 0, not shown in Mnemonic Signals) but drive derived patterns like **edit intensity**, **scene hopping**, **script focus**, and **layout focus** — so scripting, level design, and dialog work can score without F5 playtests.

At segment close, Core flushes any partial in-flight packet so late-window work still contributes to derived patterns.

Mnemonic emits atomics (`resource_saved` on editor `resource_saved`, `editor_focus_changed` on `main_screen_changed`); Core aggregates and derives patterns.

## Pattern heuristics

Some heuristics fire on **patterns** rather than single events:

### Iteration cycle

A playtest starts within **120 seconds** of a scene save. This captures the classic edit-run-fix loop — often the most devlog-worthy moments.

### Save burst

**Three or more distinct scene paths** saved within **5 minutes**, with a cooldown before re-firing.

### Rapid playtest

**Three or more playtest starts** within **5 minutes** — signals intense debugging or tuning.

### Commit after playtest

A git commit within **10 minutes** of a playtest ending — links iteration to a version checkpoint.

### Long playtest

A playtest running longer than **3 minutes** — extended exploration or soak testing.

### Resource burst

**Three or more distinct non-scene files** (scripts, resources) saved within **5 minutes**.

### Edit intensity

An activity window with **four or more** saves/transitions (scaled proportionally: 4 per 60s reference window) and **under 15 seconds** of playtest — sustained editor work.

### Scene hopping

**Three or more scene transitions** in an activity window (scaled proportionally: 3 per 60s reference) with **no playtest** — common during level blockouts.

### Script focus

**Script editor focus** for at least half the activity window, **two or more resource saves**, and **under 15 seconds** of playtest — sustained scripting without playtesting.

### Layout focus

**2D or 3D editor focus** for at least half the activity window, **two or more scene transitions**, and **no playtest** — level or scene blockout work.

### Checkpoint after work

A git commit within **10 minutes** of an edit-intensity window.

### Long edit span

At segment close: **90+ seconds**, **no playtest**, and **five or more** editor events in the segment.

## Score caps and dedupe

Individual event types have **score caps** per segment to prevent one noisy signal from dominating. Pattern heuristics like `iteration_cycle` have no cap — they represent compound significance.

Runtime errors are **rate-limited** (10 per minute) to avoid flooding scores during a broken script loop.

## Tags on clips

Preserved clips carry **tags** derived from heuristics that fired during the segment — e.g. `playtest`, `runtime_error`, `iteration_cycle`, `git_commit`, `script`, `layout`. Tags power Mnemonic dock filtering.

## Tuning for your workflow

| If you want more clips preserved | If you want fewer, higher-signal clips |
|----------------------------------|----------------------------------------|
| Lower preserve threshold | Raise preserve threshold |
| Enable more heuristic signals | Disable low-value signals (e.g. scene transition) |
| Shorten segment length | Lengthen segment length |

Manual **Save segment** bypasses all automatic scoring.

## Related

- [Development episodes](../concepts/development-episodes.md)
- [Git-linked history](../concepts/git-linked-history.md)
- [Mnemonic dock](./mnemonic-dock.md)
