# srta — Plan d'implémentation

Feuille de route validée (2026-07-16), issue du tri des idées initiales (à l'époque de la fork `srt_additions`, remplacée depuis par cet addon).
À suivre dans l'ordre ; chaque phase livre quelque chose d'utilisable en jeu.
Voir `CLAUDE.md` pour l'architecture et la checklist « nouvelle fonctionnalité ».

## Phase 1 — Quick wins (timer & PB) — ✅ faite (v1.0.0)

1. ~~**Hotkey toggle visibilité du timer**~~ — masquer/afficher le room timer à la volée.
2. ~~**Option « timer visible seulement quand le run est fini »**~~ — s'appuie sur `RoomTimerData.IsCompleted`.
3. ~~**Hotkey « annuler le dernier PB »**~~ — restaurer la valeur précédente via `lastPbTimes`. Distinct de `ResetRoomTimerPb` qui efface tout.

## Phase 2 — Deltas

4. **Delta ±X par room** — afficher l'écart réel au PB à chaque passage de room, pas seulement le timer doré. Données déjà disponibles (`PbTimes`, `BestSegments`) ; le travail est le calcul au changement de room + rendu/formatage. En addon : détection de changement de room par hooks `Level` propres + HUD dessiné par srta (pas besoin de toucher au rendu de SpeedrunTool).
5. **Hotkey toggle des deltas** — trivial une fois le point 4 en place.

## Phase 3 — Gestion des PB

6. **Persistance des PB sur disque** — aujourd'hui `PbTimes` est un dictionnaire en mémoire perdu à la fermeture du jeu. Sauvegarder par map, survivre aux sessions. Fondation des points 7 et 8. En addon : stockage côté srta (ModSaveData/fichiers), réinjection dans `PbTimes` au chargement de map — attention, le format des clés (`TimeKeyPrefix`) est un détail interne de SpeedrunTool.
7. **Set un PB manuellement** — point délicat : la saisie d'un temps en jeu (`OuiModOptionString` d'Everest, ou import presse-papier).
8. **Temps colorés par comparaison** — colorer les temps cp/il selon des seuils de comparaison locaux, affichés dans le HUD srta. Un éventuel import CSV depuis la practice sheet d'Astro reste optionnel et **ne se fera qu'avec l'accord de son créateur** — le mod gère uniquement ses propres données.

## Phase 4 — Vidéo

9. **Lecture de `.ogv` en jeu** — via `VideoPlayer` de FNA (`libtheorafile` livré avec Celeste sur toutes les plateformes, vérifié). Pièges connus : conversion des vidéos en Theora, audio via SDL et non FMOD. Totalement indépendant de SpeedrunTool.

## Garées / abandonnées

- **Sync complète Google Sheets** — abandonnée (OAuth + API lourds et fragiles ; hors de question sans l'accord du créateur de la sheet).
- **Plus de 9 save slots** — abandonné (simple constante côté SpeedrunTool, mais lag/mémoire rendent ça contre-productif).
- **Save states permanents** — garé en exploration éventuelle tout à la fin : le deep clone contient textures GPU, instances FMOD, delegates — rien de sérialisable tel quel.
- **« Condensed time map / image in game »** — garé en attendant de retrouver l'intention d'origine.
