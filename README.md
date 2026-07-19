# Speedrun Tool Additions (srta)

[Everest](https://everestapi.github.io/) mod that **require** [Speedrun Tool](https://gamebanana.com/mods/53712) for Celeste. Adds features such as delta between split/room time, hidding the timer, cancel a pb and more.

Options and hotkeys under **Mod Options → SpeedrunTool Additions** (hotkeys unbound by default).

## Changelog

### v1.1.0 — 2026-07-17

- **Per-room deltas**: on every room change, shows the gap (±X) between this run's split and the PB split, on a third row under SpeedrunTool's room timer — green when ahead, red when behind. Compared against the PBs as they were when the attempt started, like SpeedrunTool's own end-of-run comparison. Changing SpeedrunTool's timed-room count re-targets the row to the selected room, like SpeedrunTool's own display: the delta shows whenever both the current run and the PB have data for that room, and hides otherwise. Hidden while the room timer is hidden. `Show Room Deltas` option persisted in the settings.
- **"Room Deltas Comparison" option**: compare against the **PB split** (total time since timer start) or the **PB room** (this room's time vs the same room in the PB). The displayed delta updates instantly when the mode changes.
- **"Toggle Room Deltas" hotkey**: shows/hides the deltas on the fly.
- **"Switch Room Deltas Comparison" hotkey**: cycles the comparison mode (PB split / PB room) with a popup, like SpeedrunTool's `Room Timer` hotkey.

### v1.0.0 — 2026-07-16

- **"Toggle Room Timer Visibility" hotkey**: hides/shows the room timer on the fly — the timer keeps running in the background. `Show Room Timer` option persisted in the settings.
- **"Only Show Timer When Run Completed" option**: the room timer only shows once the run is completed.
- **"Undo Latest Room Timer PB" hotkey**: restores the PBs to what they were at the start of the current attempt. Distinct from SpeedrunTool's `Clear Room Timer PB`, which clears everything.

*(resumes the "phase 1" initially developed in the `srt_additions` fork, now replaced by this addon)*
