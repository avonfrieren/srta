# srta — SpeedrunTool Additions

Mod [Everest](https://everestapi.github.io/) **compagnon** de [SpeedrunTool](https://gamebanana.com/mods/53712) pour Celeste : il s'accroche au mod officiel (déclaré en dépendance) au lieu d'en être une fork, et lui ajoute des fonctionnalités orientées practice/speedrun.

Options et hotkeys dans **Mod Options → SpeedrunTool Additions** (hotkeys non assignées par défaut).

## Installation

1. Installer [Everest](https://everestapi.github.io/) et [SpeedrunTool](https://gamebanana.com/mods/53712) (v3.27.16+), par exemple via Olympus.
2. Télécharger/compiler srta (voir [Build](#build)) et copier le dossier `srta` (contenant `srta.dll`, `everest.yaml`, `Dialog/`) dans `<Celeste>/Mods/`.
3. Vérifier que `SpeedrunTool.zip` n'est **pas** dans `Mods/blacklist.txt` — c'est une dépendance.

## Build

```
dotnet build -p:CelestePrefix=<dossier Celeste>
```

`CelestePrefix` est auto-détecté si le repo est cloné dans `<Celeste>/Mods/xxx/`. La DLL de SpeedrunTool est extraite automatiquement du `SpeedrunTool.zip` installé, et [Krafs.Publicizer](https://github.com/krafs/Publicizer) donne accès à ses internals. La cible `OutputAsModStructure` génère `build/`, prêt à copier dans `<Celeste>/Mods/srta/`.

## Changelog

### v1.0.0 — 2026-07-16

- **Hotkey « Toggle Room Timer Visibility »** : masque/affiche le room timer à la volée — le timer continue de tourner en arrière-plan. Option `Show Room Timer` persistée dans les settings.
- **Option « Only Show Timer When Run Completed »** : le room timer ne s'affiche qu'une fois le run terminé.
- **Hotkey « Undo Latest Room Timer PB »** : restaure les PB tels qu'ils étaient au début de la tentative courante. Distinct de `Clear Room Timer PB` de SpeedrunTool qui efface tout.

*(reprend la « phase 1 » développée initialement dans la fork `srt_additions`, désormais remplacée par cet addon)*

## Limites connues

- Épinglé sur SpeedrunTool **v3.27.16** : l'addon touche des internals (`lastPbTimes`, `Data_Auto`…) qui peuvent changer à une mise à jour de SpeedrunTool — rebuild + retest à chaque release upstream.
- Si SpeedrunTool est rechargé à chaud *après* srta, ses hooks repassent devant les nôtres et le masquage du timer cesse d'agir jusqu'au rechargement de srta.
- L'undo PB ne fonctionne pas dans le debug map.

## Architecture

Voir [CLAUDE.md](CLAUDE.md) (architecture, conventions, checklist nouvelle feature) et [PLAN.md](PLAN.md) (feuille de route).
