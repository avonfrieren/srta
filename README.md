# srta — SpeedrunTool Additions

Mod Everest **compagnon** de [SpeedrunTool](https://gamebanana.com/mods/53712) : il s'accroche au mod officiel (déclaré en dépendance) au lieu d'en être une fork. Remplace la fork `srt_additions`.

## Fonctionnalités (phase 1)

- **Hotkey « Toggle Room Timer Visibility »** : masque/affiche le room timer à la volée — le timer continue de tourner en arrière-plan. Option `Show Room Timer` persistée.
- **Option « Only Show Timer When Run Completed »** : le room timer ne s'affiche qu'une fois le run terminé (`RoomTimerData.IsCompleted`).
- **Hotkey « Undo Latest Room Timer PB »** : restaure les PB tels qu'ils étaient au début de la tentative courante (via `lastPbTimes`). Distinct de `Clear Room Timer PB` de SpeedrunTool qui efface tout.

Options et hotkeys dans **Mod Options → SpeedrunTool Additions** (hotkeys non assignées par défaut).

## Comment ça s'accroche à SpeedrunTool

- Compilé contre la DLL du `SpeedrunTool.zip` **officiel installé** (extraite automatiquement de `<Celeste>/Mods/SpeedrunTool.zip` au build), avec [Krafs.Publicizer](https://github.com/krafs/Publicizer) pour accéder aux internals (`RoomTimerManager.Data_Auto`, `RoomTimerData.lastPbTimes`…).
- Masquage : hooks `On.Celeste.SpeedrunTimerDisplay.Update/Render` et `On.Celeste.TotalStrawberriesDisplay.Update` qui font croire à SpeedrunTool que `RoomTimerType == Off` le temps de l'appel (le rendu retombe sur l'affichage vanilla, le compteur de fraises reprend sa place). Aucun hook sur les méthodes privées de SpeedrunTool.
- Undo PB : réimplémente le rollback `lastPbTimes → PbTimes` sur les deux `RoomTimerData`.

## Build

```
~/.dotnet/dotnet build -p:CelestePrefix=<dossier Celeste>
```

(`CelestePrefix` auto-détecté si le repo est cloné dans `<Celeste>/Mods/xxx/`.) La cible `OutputAsModStructure` génère `build/` (DLL + `everest.yaml` + `Dialog/`), à copier dans `<Celeste>/Mods/srta/`.

## Limites connues

- Épinglé sur SpeedrunTool **v3.27.16** : les accès aux internals (`lastPbTimes`, `Data_Auto`, format des settings) peuvent casser à une mise à jour de SpeedrunTool — rebuild + retest à chaque release upstream.
- Si SpeedrunTool est rechargé à chaud *après* srta, ses hooks repassent devant les nôtres et le masquage cesse d'agir jusqu'au rechargement de srta.
- L'undo PB ne fonctionne pas dans le debug map (la fork le permettait aussi en `MapEditor`).
