# srta — Contexte & architecture

Mod Everest **compagnon** de [DemoJameson/Celeste.SpeedrunTool](https://github.com/DemoJameson/Celeste.SpeedrunTool) (MIT) : il déclare SpeedrunTool en dépendance et s'y accroche, au lieu d'en être une fork. Remplace l'ancienne fork `avonfrieren/srt_additions` (supprimée) ; la phase 1 y avait été implémentée puis portée ici.
Roadmap dans `PLAN.md`. Le `README.md` sert de **changelog + instructions d'installation** — le mettre à jour à chaque feature.

## Build

- .NET 8 (`net8.0`), projet unique `srta.csproj`. Sur cette machine : `~/.dotnet/dotnet build -p:CelestePrefix="$HOME/.steam/steam/steamapps/common/Celeste"`.
- Références jeu (`Celeste.dll`, `FNA.dll`, `MMHOOK_Celeste.dll`, `MonoMod.*`) résolues via `CelestePrefix` (auto-détecté si le repo est cloné dans `<Celeste>/Mods/xxx/`).
- **`SpeedrunTool.dll` est extraite automatiquement du `SpeedrunTool.zip` officiel installé** (`<Celeste>/Mods/SpeedrunTool.zip`) par la cible `ExtractSpeedrunToolDll` → l'addon compile toujours contre la version réellement installée. Épinglé sur **v3.27.16** dans `everest.yaml`.
- `Krafs.Publicizer` publicise `SpeedrunTool` → accès compile-time aux internals (`RoomTimerManager.Data_Auto`, `RoomTimerData.lastPbTimes`…). Toute mise à jour upstream peut casser ces accès : rebuild + retest à chaque release.
- Cible `OutputAsModStructure` : génère `build/` (DLL + PDB + `everest.yaml` + `Dialog/`) → copier dans `<Celeste>/Mods/srta/`.
- ⚠️ `Mods/blacklist.txt` : `SpeedrunTool.zip` doit rester **actif** (c'est la dépendance), contrairement à l'époque de la fork où il était blacklisté.

## Architecture de l'addon

- `Source/SrtaModule.cs` — `EverestModule` classique ; `Load()`/`Unload()` délèguent aux classes de features statiques.
- `Source/SrtaSettings.cs` — settings Everest : bools + `ButtonBinding` (hotkeys rebindables via le menu auto-généré d'Everest, clés dialog `MODOPTIONS_SRTA_<PROP>`). Pas de menu manuel.
- `Source/RoomTimerTweaks.cs` — phase 1. **Pattern « pretend off »** pour masquer le room timer sans hooker les méthodes privées de SpeedrunTool : on enveloppe `On.Celeste.SpeedrunTimerDisplay.Update/Render` et `On.Celeste.TotalStrawberriesDisplay.Update`, et pendant l'appel on met temporairement `SpeedrunToolSettings.Instance.RoomTimerType = Off` — le code de SpeedrunTool (hook Render, IL `IsShowTimer`, décalage du compteur de fraises) retombe de lui-même sur le comportement vanilla. srta charge après SpeedrunTool (dépendance) donc ses hooks On enveloppent les siens ; un hot-reload de SpeedrunTool seul inverse l'ordre et désactive le masquage jusqu'au rechargement de srta.
- Popups : réutilise `PopupMessageUtils` (public) et `DialogIds.On/Off` de SpeedrunTool.
- `Dialog/English.txt` + `French.txt` — clés `MODOPTIONS_SRTA_*` (menu auto) et `SRTA_*` (popups).

## Repères dans SpeedrunTool v3.27.16 (résumé de l'analyse faite sur la fork)

### Room timer (`Source/RoomTimer/` chez SpeedrunTool)
- `RoomTimerManager` (static) — hooks : IL `SpeedrunTimerDisplay.Update` (via `IsShowTimer`), On `SpeedrunTimerDisplay.Render` (dessin du room timer, fallback `orig` si type Off), IL `TotalStrawberriesDisplay.Update` (pousse le compteur de fraises vers le bas si timer actif), IL `AutoSplitterInfo.Update` (temps autosplitter — indépendant de l'affichage). Deux instances privées `CurrentRoomTimerData`/`NextRoomTimerData`, `Data_Auto` (internal) pointe selon `RoomTimerType`.
- `RoomTimerData` (internal) — `Time`, `ThisRunTimes`, `PbTimes`, `BestSegments` (publics), `lastPbTimes` (privé : snapshot des PB au départ de la tentative — base de l'undo), `IsCompleted`, clés de temps préfixées `TimeKeyPrefix` (SID+mode+room).
- `EndPoint` — drapeau de fin manuel ; `RoomTimerType` {Off, NextRoom, CurrentRoom}.

### Save/Load (le cœur de SpeedrunTool, utile pour les phases futures)
- `StateManager` — save state par deep clone du `Level` entier ; multi-slots via `SaveSlotsManager`.
- `SaveLoadAction` — système d'extension : toute valeur statique externe mutée par le gameplay doit y être enregistrée sous peine de desync des save states. Accessible proprement via ModInterop `SpeedrunTool.SaveLoad` (`RegisterSaveLoadAction`, `RegisterStaticTypes`, `IgnoreSaveState`, `DeepClone`, `GetSlotName`).
- ModInterop `SpeedrunTool.RoomTimer` : `RoomTimerIsCompleted()`, `GetRoomTime()` (surface minimale ; DemoJameson accepte des ajouts sur demande Discord).

### Conventions reprises
- Hooks MonoMod On/IL toujours retirés dans `Unload`.
- Si une feature de srta introduit un état statique muté pendant le gameplay → l'enregistrer via l'interop `SpeedrunTool.SaveLoad` (sinon desync des save states).

## Checklist « nouvelle fonctionnalité »

1. Créer `Source/<Feature>.cs` statique avec `Load()`/`Unload()`, appelés depuis `SrtaModule`.
2. Option de menu → propriété dans `SrtaSettings` + clés `MODOPTIONS_SRTA_<PROP>` dans `Dialog/English.txt` **et** `French.txt` (le menu est auto-généré).
3. Hotkey → propriété `ButtonBinding` avec `[DefaultButtonBinding]` + clé dialog ; tester la pression dans un hook (ex. `On.Celeste.Level.Update`, après `orig`, hors pause).
4. Accès aux internals SpeedrunTool → directs grâce au Publicizer ; préférer les membres qui existent aussi dans l'interop officiel quand possible (plus stable).
5. État statique muté en gameplay → enregistrer via ModInterop `SpeedrunTool.SaveLoad`.
6. Build puis copier `build/` dans `<Celeste>/Mods/srta/` ; tester en jeu.
7. Documenter dans le changelog du `README.md` (+ bump `Version` dans `everest.yaml` si release).
