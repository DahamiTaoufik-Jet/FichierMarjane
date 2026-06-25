# Escape Game — Unity 6

Memo d'architecture pour Claude Code. Lis ce fichier en premier avant toute modification du code.

## Resume du jeu

Jeu d'escape en vue third-person (bascule FPS possible avec touche C). Le joueur explore un marche/supermarche et resout des **routes** : suites ordonnees d'**etapes** (chaque etape etant soit un *Indice* soit une *Enigme*). A la fin de chaque route, le joueur recoit une **recompense** (lettre du mot de passe final, ou bonus utilisable). Un **journal de progression** (carte 2D navigable avec zoom/pan) affiche l'avancement avec des StageNodes cliquables et des routes connectees par des lignes.

**Specificite critique** : les routes ne sont **PAS pre-definies par le designer**. Elles sont generees proceduralement au runtime a partir d'un pool de Steps + des Placeholders poses dans la scene.

## Scenes

- **`UI Only`** : scene de tutoriel affichee au lancement. Contient un Canvas avec les regles du jeu et un bouton "Jouer" qui charge SampleScene via `SceneManager.LoadScene`.
- **`SampleScene`** : scene principale du jeu. Contient le joueur, le marche, les placeholders, le journal, etc.

Le flux est : `UI Only` (tutoriel) -> bouton Jouer -> `SampleScene` (jeu).

## Arborescence des scripts

```
Assets/Scripts/
├── Bonuses/Data/
│   ├── DechiffreurBonus.cs            SO : revele l'indice d'une step selectionnee
│   ├── ResolveurBonus.cs              SO : resout directement une step selectionnee
│   ├── PathFinderBonus.cs             SO : trace une ligne vers la prochaine step
│   ├── PathFinderLineTracker.cs       MonoBehaviour : gere le LineRenderer du PathFinder
│   └── JournalSelectionMode.cs        classe statique : mode selection pour bonus journal
│
├── Core/
│   ├── Interfaces/IScannable.cs       OnHover/OnScan/Reveal/OnHoverExit
│   ├── Player/
│   │   ├── PlayerContext.cs           enveloppe : fpsCamera, inventory, scanner
│   │   ├── PlayerCamera.cs            bascule TPS/FPS (touche C)
│   │   ├── PlayerControls.cs          bindings Input System
│   │   ├── PlayerLook.cs              rotation tete/corps independante, bloque si UIState.IsAnyUIOpen
│   │   ├── PlayerScanner.cs           SphereCast FPS, OnHover continu / OnScan validation
│   │   ├── RootMotionController.cs    deplacement root motion, CapsuleCast collision, bloque si UIState.IsAnyUIOpen
│   │   ├── UIState.cs                 compteur statique SetUIOpen/SetUIClosed/IsAnyUIOpen
│   │   └── Animation/
│   │       └── animationBlendScript.cs  blend tree animation
│   └── World/
│       ├── PlaceholderNode.cs         composant a poser sur Empty GameObjects (nodeType, regionId, spotId)
│       ├── ProceduralRouteGenerator.cs  orchestrateur de la generation
│       └── LevelGenerator.cs          LEGACY (paires Puzzle-Clue, a supprimer)
│
├── Routes/                            coeur du systeme
│   ├── Data/
│   │   ├── StepType.cs                enum Clue / Puzzle
│   │   ├── PlacementMode.cs           enum Any / Region / Spot
│   │   ├── PlacementData.cs           struct: mode + regionId + spotId
│   │   ├── ClueContent.cs             text + sprite + audio (avec IsEmpty)
│   │   ├── RewardData.cs              abstract SO base (rewardName, description, icon)
│   │   ├── StepData.cs                SO: stepId, type, initialClue, placement, cluePrefab, puzzlePrefab
│   │   └── RouteGeneratorConfig.cs    SO: stepPool, passwordWords, minPasswordLength,
│   │                                      letterAlphabet, bonusPool, min/maxRouteLength,
│   │                                      maxStepUsage, regionFilter, seed
│   ├── Runtime/
│   │   ├── StepState.cs               Locked / Discovered / Resolved
│   │   ├── RouteState.cs              Inactive / Active / Completed
│   │   ├── StepBehaviour.cs           abstract MonoBehaviour : IScannable
│   │   ├── ClueStep.cs                auto-resolu au scan
│   │   ├── PuzzleStep.cs              abstract, ne s'auto-resout pas
│   │   ├── TextPuzzleStep.cs          enigme texte avec champ de saisie
│   │   ├── AudioVisualPuzzleStep.cs   gaze + proximite
│   │   ├── PositionalScanPuzzleStep.cs  enigme positionnelle par scan
│   │   ├── WaveOverlayHUD.cs          HUD overlay pour les ondes
│   │   └── RouteRuntime.cs            routeId, displayName, steps, endReward, IsLastStep()
│   ├── Generation/
│   │   ├── RoutePlan.cs               POCO : StepAssignment + RoutePlan (avec EndReward)
│   │   └── RouteGenerationPlanner.cs  algo pur (pas de MonoBehaviour), tirage par bandes
│   │                                      d'usage croissantes, match Any/Region/Spot
│   ├── Events/RouteEvents.cs          bus statique : StepDiscovered, StepResolved,
│   │                                      RouteStarted, RouteCompleted, ClueRevealed
│   └── Services/RouteManager.cs       singleton : RegisterRoute(), chainage, distribution reward
│
├── Inventory/
│   ├── Data/
│   │   ├── ItemData.cs                abstract, herite de RewardData
│   │   ├── LetterItem.cs              SO : char letter
│   │   ├── BonusItem.cs               SO : wrappe BonusData, Use(PlayerContext), consumeOnUse
│   │   └── LetterAlphabet.cs          SO : List<LetterItem> + GetLetter(char)
│   ├── Runtime/Inventory.cs           AddItem / RemoveItem / UseItem / GetItemsOfType<T>
│   ├── Events/InventoryEvents.cs      ItemAdded / ItemRemoved / ItemUsed
│   └── UI/InventoryPanelView.cs       panneau inventaire UI
│
├── Journal/
│   ├── Runtime/JournalManager.cs      singleton, s'abonne a RouteEvents
│   └── UI/
│       ├── JournalView.cs             carte 2D navigable (zoom/pan), toggle Tab, mode selection bonus
│       ├── StageNodeView.cs           node visuel d'une step sur la carte
│       ├── StageModalView.cs          modal detail d'une step (indice, etat)
│       ├── PanZoomController.cs       pan et zoom de la carte journal
│       ├── CluePanelView.cs           panneau d'affichage d'indice
│       ├── TextPuzzlePanelView.cs     panneau de saisie pour enigme texte
│       ├── TutorialView.cs            charge la scene de jeu depuis la scene tutoriel
│       ├── TutorialBootstrap.cs       INUTILISE (reste du code de l'approche in-scene)
│       └── TutorialButtonSync.cs      INUTILISE (reste du code de l'approche in-scene)
│
└── Interactables/                     LEGACY (anciens scripts, a supprimer)
    ├── Clues/CluePanel.cs
    └── Puzzles/
        ├── PuzzleBase.cs
        └── AudioVisualPuzzle.cs
```

## Scripts hors Assets/Scripts/

- **`Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs`** : controleur de mouvement principal (CharacterController + StarterAssetsInputs). Modifie pour bloquer le mouvement et la camera quand `UIState.IsAnyUIOpen`.
- **`Assets/Scripts/Core/Player/RootMotionController.cs`** : controleur root motion secondaire. Bloque aussi via `UIState.IsAnyUIOpen`.

## Systeme d'input et blocage UI

**`UIState`** (`Core/Player/UIState.cs`) est un compteur statique :
- `SetUIOpen()` : incremente le compteur
- `SetUIClosed()` : decremente le compteur
- `IsAnyUIOpen` : `true` si compteur > 0
- `IsInputFieldActive` : bloque la touche Tab quand un InputField est actif (evite d'ouvrir le journal en tapant)

Tous les scripts de mouvement (`ThirdPersonController`, `RootMotionController`, `PlayerLook`) font un early return si `UIState.IsAnyUIOpen`.

**Piege** : ne jamais desactiver l'action map `Player` ou `UI` pour bloquer les inputs. Ca desactive aussi les clics UI (Point, Click). Utiliser uniquement le pattern `UIState` avec early return.

## Decisions de design cles

**Generation procedurale unique**. Pas de `RouteData` SO. Le designer ne cree que :
- un *pool de StepData* (briques elementaires interchangeables)
- des `PlaceholderNode` poses dans la scene (composants sur Empty GameObjects)
- un `RouteGeneratorConfig` qui parametre tout

Le `ProceduralRouteGenerator` au `Start` :
1. Inventorie les `PlaceholderNode` actifs (filtres par `regionFilter` si renseigne)
2. Demande au `RouteGenerationPlanner` de batir le maximum de routes possibles, en consommant les placeholders
3. Choisit un **mot de passe** dans `passwordWords` dont la longueur in [`minPasswordLength`, nombre de routes]
4. Distribue les **lettres** aux N premieres routes (priorite absolue), via `LetterAlphabet.GetLetter(char)`
5. Comble les routes restantes avec un `BonusItem` tire aleatoirement du `bonusPool`
6. Instancie chaque step a la position de son placeholder, injecte `stepData` + `routeId`, appelle `RouteManager.RegisterRoute()`

**Steps interchangeables**. `StepData` n'a *aucun* role-flag. Toute step peut occuper n'importe quel role. Le role est implicite : la position dans la liste determine entry (index 0) / fin (dernier). Les routes finissent toujours par un Puzzle (force par le planner).

**Step usage**. Chaque `StepData` peut etre utilisee jusqu'a `maxStepUsage` fois (defaut 2) dans toute la generation. L'algo trie les candidats par usage croissant.

**Recompense au niveau route, pas step**. La `RewardData` est sur `RouteRuntime.EndReward`. `RouteManager` la distribue quand `runtime.IsLastStep(resolvedStep)` est vrai.

**Chainage des indices**. Quand une step N est resolue, `RouteManager.HandleStepResolved` :
1. Leve `RouteEvents.StepResolved`
2. Si derniere step -> distribue `EndReward` a l'inventaire
3. Sinon -> appelle `Discover()` sur la step N+1 et leve `RouteEvents.ClueRevealed` avec son `initialClue`
4. Si toutes les steps de la route sont `Resolved` -> passe la route en `Completed` et leve `RouteCompleted`

**Match step - placeholder** :
- `step.type` doit matcher `placeholder.nodeType` (Clue-Clue, Puzzle-Puzzle)
- `step.placement.mode == Any` -> tout placeholder du bon type
- `step.placement.mode == Region` -> placeholder dont `regionId == step.placement.regionId`
- `step.placement.mode == Spot` -> placeholder dont `spotId == step.placement.spotId`

**Bonus et mode selection journal** :
- `DechiffreurBonus` et `ResolveurBonus` ouvrent le journal en mode selection (`JournalSelectionMode`)
- Le joueur clique sur un StageNode pour selectionner la step cible
- `PathFinderBonus` trace un LineRenderer vers la prochaine step non resolue

## Conventions

- **Namespaces** : `EscapeGame.Core.*`, `EscapeGame.Routes.{Data,Runtime,Generation,Events,Services}`, `EscapeGame.Inventory.{Data,Runtime,Events,UI}`, `EscapeGame.Journal.{Runtime,UI}`, `EscapeGame.Bonuses.Data`, `EscapeGame.Interactables.*` (legacy).
- **ScriptableObject menus** : tous sous `Create -> EscapeGame -> ...`
- **Pas de LINQ** dans les hot paths (foreach manuels). Les `IReadOnlyList<T>` n'ont pas de `.Contains()` natif -> boucle manuelle obligatoire (sinon le compilateur tombe sur `MemoryExtensions.Contains` et plante).
- **Encodage** : UTF-8 sans BOM. Eviter les accents dans les chaines affichees (Unity Windows peut avoir des soucis).
- **Eviter les nouveaux fichiers** : la base est posee. Toute nouvelle fonctionnalite doit s'ajouter dans la structure existante.
- **Font** : Passero One (Static SDF asset). Le mode Dynamic ne serialise pas l'atlas et casse apres un git push/pull sur un autre PC. Toujours utiliser le mode Static.

## Optimisations de performance appliquees

### Rendu (URP)
- **SRP Batcher** : actif
- **GPU Instancing** : active sur 333 materiaux
- **Render scale** : 0.75 (rendu a 75% de la resolution native)
- **Shadow distance** : 20 (reduit depuis 30)
- **Shadow cascades** : 1 (reduit depuis 2)
- **Shadow map resolution** : 512 (main + additional, reduit depuis 1024)
- **Additional lights shadows** : desactive
- **Soft shadows** : desactive
- **Max additional lights** : 1 (reduit depuis 2)
- **Texture Streaming** : actif (budget 256 MB)

### Batching
- **Static Batching** : 10 463 objets flagges BatchingStatic
- **Occlusion Culling** : 10 463 objets flagges OccluderStatic + OccludeeStatic
  - Etagere layer (144 objets)
  - ToysPrefabs, ElectroPrefabs, HatsPrefabs children (9 373 objets)
  - MarketStands (936 objets)
  - Chests (10 SkinnedMeshRenderers)
- **Occlusion data** : ~291 KB bake

### Assets
- **Texture compression** : toutes compressees (BC7/Crunch, qualite 50)
- **Mipmaps** : actifs sur toutes les textures sauf sprites
- **Mesh compression** : Low sur 93 modeles
- **Physics 2D** : simulation desactivee (mode Script)

### Resultats
- Batches : 13 055 -> ~1 078
- Saved by batching : 84 -> ~18 107
- Shadow casters : 530 -> ~59
- FPS : ~15 (pire cas) -> ~115+ (stable)

## Code legacy a supprimer

Quand les prefabs `Assets/Prefab/Indice.prefab` et `Assets/Prefab/Radio.prefab` auront ete remplaces par des prefabs bases sur `ClueStep` et `AudioVisualPuzzleStep` :

- `Assets/Scripts/Interactables/Clues/CluePanel.cs`
- `Assets/Scripts/Interactables/Puzzles/PuzzleBase.cs`
- `Assets/Scripts/Interactables/Puzzles/AudioVisualPuzzle.cs`
- `Assets/Scripts/Core/World/LevelGenerator.cs`
- Le champ `zoneID` de `PlaceholderNode` (tag `Legacy`)

Scripts inutilises (restes de l'approche tutoriel in-scene, le tutoriel est maintenant dans la scene `UI Only`) :
- `Assets/Scripts/Journal/UI/TutorialBootstrap.cs`
- `Assets/Scripts/Journal/UI/TutorialButtonSync.cs`

## Setup scene minimal

1. GameObject **`RouteManager`** avec composant `RouteManager`. Drag le `PlayerContext` du Player.
2. GameObject **`JournalManager`** avec composant `JournalManager`.
3. Sur le **Player** : composant `Inventory`, croisements bidirectionnels avec `PlayerContext.inventory`.
4. Cree un **`LetterAlphabet`** + des `LetterItem` pour chaque lettre necessaire.
5. Cree un **`RouteGeneratorConfig`** (pool de steps, mots de passe candidats, alphabet, bonus, min/max length).
6. Pose des **`PlaceholderNode`** dans la scene : `nodeType` (Clue/Puzzle), `spotId`, `regionId`.
7. GameObject **`ProceduralRouteGenerator`** : drag le config + RouteManager.
8. Canvas Journal : `JournalView` -> worldContainer -> StageNode prefab + ConnectorLine prefab + `StageModalView`.

## Etat actuel

- OK Couche Data complete (StepData, PlacementData, RewardData, RouteGeneratorConfig, LetterAlphabet, ClueContent)
- OK Couche Runtime (StepBehaviour, ClueStep, PuzzleStep, TextPuzzleStep, AudioVisualPuzzleStep, PositionalScanPuzzleStep, RouteRuntime)
- OK Services (RouteManager singleton + chainage + distribution recompense)
- OK Bus d'evenements (RouteEvents, InventoryEvents)
- OK Inventaire (Inventory, ItemData, LetterItem, BonusItem, InventoryPanelView)
- OK Journal carte 2D (JournalManager, JournalView, StageNodeView, StageModalView, PanZoomController)
- OK Generation procedurale (Planner + ProceduralRouteGenerator avec mot de passe)
- OK Tutoriel (scene separee `UI Only` avec TutorialView)
- OK Blocage input quand UI ouverte (UIState + early returns dans tous les controleurs)
- OK Bonus : PathFinder, Dechiffreur, Resolveur implementes
- OK Optimisations de performance (batching, occlusion, GPU instancing, textures, shadows)
- EN COURS Tests manuels en scene pas encore valides bout-en-bout
- EN COURS Migration des prefabs Indice/Radio vers ClueStep/AudioVisualPuzzleStep
- EN COURS Pas de systeme de sauvegarde / persistance
- EN COURS Spikes GPU intermittents (RenderLoop ~97ms), bottleneck GPU sur certaines vues denses

## Pieges a eviter

- Ne pas remettre de `RouteData` SO : la generation est purement procedurale.
- Ne pas remettre `isEnd` ni `reward` sur `StepData` : ces deux notions sont gerees au niveau `RouteRuntime`.
- `PlaceholderNode.zoneID` est legacy (utilise par `LevelGenerator`). Pour le procedural, n'utiliser que `regionId` et `spotId`.
- Les UnityEvents `OnDiscovered`/`OnResolved` sur `StepBehaviour` sont *ecoutes* par `RouteManager`. Si tu desactives `RouteManager`, le chainage ne fonctionne pas.
- L'ancien `LevelGenerator` et le nouveau `ProceduralRouteGenerator` ne doivent **pas tourner simultanement** dans la meme scene.
- Ne **jamais** desactiver les action maps (Player, UI) pour bloquer les inputs. Utiliser `UIState` avec early return.
- Font TMP : toujours utiliser le mode **Static** pour le SDF asset. Le mode Dynamic ne serialise pas l'atlas et casse sur un autre PC apres push/pull.
- `UIState` est un compteur : chaque `SetUIOpen()` doit avoir un `SetUIClosed()` correspondant. Un desequilibre bloque les inputs indefiniment.
