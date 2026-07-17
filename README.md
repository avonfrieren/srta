# srta — SpeedrunTool Additions

[Everest](https://everestapi.github.io/) mod, **companion** to [SpeedrunTool](https://gamebanana.com/tools/6597) for Celeste: it hooks into the official mod (declared as a dependency) instead of being a fork of it, and adds practice/speedrun-oriented features.

Options and hotkeys under **Mod Options → SpeedrunTool Additions** (hotkeys unbound by default).

## Installation

1. Install [Everest](https://everestapi.github.io/) and [SpeedrunTool](https://gamebanana.com/tools/6597) (v3.27.16+), e.g. via Olympus.
2. Download/build srta (see [Build](#build)) and copy the `srta` folder (containing `srta.dll`, `everest.yaml`, `Dialog/`) into `<Celeste>/Mods/`.
3. Make sure `SpeedrunTool.zip` is **not** in `Mods/blacklist.txt` — it's a dependency.

## Build

```
dotnet build -p:CelestePrefix=<Celeste folder>
```

`CelestePrefix` is auto-detected if the repo is cloned into `<Celeste>/Mods/xxx/`. SpeedrunTool's DLL is automatically extracted from the installed `SpeedrunTool.zip`, and [Krafs.Publicizer](https://github.com/krafs/Publicizer) gives access to its internals. The `OutputAsModStructure` target generates `build/`, ready to copy into `<Celeste>/Mods/srta/`.

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

## Known limitations

- Pinned to SpeedrunTool **v3.27.16**: the addon touches internals (`lastPbTimes`, `Data_Auto`…) that may change on a SpeedrunTool update — rebuild + retest on every upstream release.
- If SpeedrunTool is hot-reloaded *after* srta, its hooks end up in front of ours again and timer hiding stops working until srta is reloaded.
- PB undo doesn't work in the debug map.

## Architecture

See [CLAUDE.md](CLAUDE.md) (architecture, conventions, new-feature checklist) and [PLAN.md](PLAN.md) (roadmap).
