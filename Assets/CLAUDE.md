# Escape Game — Unity 6

Mémo d'architecture pour Claude Code. Lis ce fichier en premier avant toute modification du code.

## Résumé du jeu

Jeu d'escape en vue first-person/third-person. Le joueur explore un monde et résout des **routes** : suites ordonnées d'**étapes** (chaque étape étant soit un *Indice* soit une *Énigme*). À la fin de chaque route, le joueur reçoit une **récompense** (lettre du mot de passe final, ou bonus utilisable). Un **journal de progression** affiche l'avancement avec un système de blocs cliquables (cadenas / cadenas argenté / sans cadenas) et des routes "dorées" une fois complétées.

**Spécificité critique** : les routes ne sont **PAS pré-définies par le designer**. Elles sont générées procéduralement au runtime à partir d'un pool de Steps + des Placeholders posés dans la scène.

## Arborescence des scripts

```
Assets/Scripts/
├── Bonuses/Data/
│   └── BonusData.cs                  abstract SO, Strategy pour les bonus (Execute(PlayerContext))
│
├── Core/
│   ├── Interfaces/IScannable.cs      OnHover/OnScan/Reveal/OnHoverExit
│   ├── Player/
│   │   ├── PlayerContext.cs          enveloppe : fpsCamera, inventory, scanner
│   │   ├── PlayerCamera.cs           bascule TPS/FPS (touche C)
│   │   ├── PlayerLook.cs             rotation tête/corps indépendante
│   │   └── PlayerScanner.cs          SphereCast FPS, OnHover continu / OnScan validation
│   └── World/
│       ├── PlaceholderNode.cs        composant à poser sur Empty GameObjects
│       ├── ProceduralRouteGenerator.cs  orchestrateur de la génération
│       └── LevelGenerator.cs         LEGACY (paires Puzzle↔Clue, à supprimer)
│
├── Routes/                           cœur du système
│   ├── Data/
│   │   ├── StepType.cs               enum Clue / Puzzle
│   │   ├── PlacementMode.cs          enum Any / Region / Spot
│   │   ├── PlacementData.cs          struct: mode + regionId + spotId
│   │   ├── ClueContent.cs            text + sprite + audio (avec IsEmpty)
│   │   ├── RewardData.cs             abstract SO base (rewardName, description, icon)
│   │   ├── StepData.cs               SO: stepId, type, initialClue, placement, cluePrefab, puzzlePrefab
│   │   └── RouteGeneratorConfig.cs   SO: stepPool, passwordWords, minPasswordLength,
│   │                                     letterAlphabet, bonusPool, min/maxRouteLength,
│   │                                     maxStepUsage, regionFilter, seed
│   ├── Runtime/
│   │   ├── StepState.cs              Locked / Discovered / Resolved
│   │   ├── RouteState.cs             Inactive / Active / Completed
│   │   ├── StepBehaviour.cs          abstract MonoBehaviour : IScannable
│   │   ├── ClueStep.cs               auto-résolu au scan
│   │   ├── PuzzleStep.cs             abstract, ne s'auto-résout pas
│   │   ├── AudioVisualPuzzleStep.cs  gaze + proximité
│   │   └── RouteRuntime.cs           routeId, displayName, steps, endReward, IsLastStep()
│   ├── Generation/
│   │   ├── RoutePlan.cs              POCO : StepAssignment + RoutePlan (avec EndReward)
│   │   └── RouteGenerationPlanner.cs algo pur (pas de MonoBehaviour), tirage par bandes
│   │                                     d'usage croissantes, match Any/Region/Spot
│   ├── Events/RouteEvents.cs         bus statique : StepDiscovered, StepResolved,
│   │                                     RouteStarted, RouteCompleted, ClueRevealed
│   └── Services/RouteManager.cs      singleton : RegisterRoute(), chaînage, distribution reward
│
├── Inventory/
│   ├── Data/
│   │   ├── ItemData.cs               abstract, hérite de RewardData
│   │   ├── LetterItem.cs             SO : char letter
│   │   ├── BonusItem.cs              SO : wrappe BonusData, Use(PlayerContext), consumeOnUse
│   │   └── LetterAlphabet.cs         SO : List<LetterItem> + GetLetter(char)
│   ├── Runtime/Inventory.cs          AddItem / RemoveItem / UseItem / GetItemsOfType<T>
│   └── Events/InventoryEvents.cs     ItemAdded / ItemRemoved / ItemUsed
│
├── Journal/
│   ├── Runtime/JournalManager.cs     singleton, s'abonne à RouteEvents
│   └── UI/
│       ├── JournalView.cs            panneau racine
│       ├── RouteRowView.cs           ligne de blocs + fond doré quand Completed
│       └── StepBlockView.cs          bouton avec 3 visuels selon StepState
│
└── Interactables/                    LEGACY (anciens scripts, à supprimer quand
    ├── Clues/CluePanel.cs              les prefabs Indice/Radio auront migré)
    └── Puzzles/PuzzleBase.cs
        AudioVisualPuzzle.cs
```

## Décisions de design clés

**Génération procédurale unique**. Pas de `RouteData` SO. Le designer ne crée que :
- un *pool de StepData* (briques élémentaires interchangeables)
- des `PlaceholderNode` posés dans la scène (composants sur Empty GameObjects)
- un `RouteGeneratorConfig` qui paramètre tout

Le `ProceduralRouteGenerator` au `Start` :
1. Inventorie les `PlaceholderNode` actifs (filtrés par `regionFilter` si renseigné)
2. Demande au `RouteGenerationPlanner` de bâtir le maximum de routes possibles, en consommant les placeholders
3. Choisit un **mot de passe** dans `passwordWords` dont la longueur ∈ [`minPasswordLength`, nombre de routes]
4. Distribue les **lettres** aux N premières routes (priorité absolue), via `LetterAlphabet.GetLetter(char)`
5. Comble les routes restantes avec un `BonusItem` tiré aléatoirement du `bonusPool`
6. Instancie chaque step à la position de son placeholder, injecte `stepData` + `routeId`, appelle `RouteManager.RegisterRoute()`

**Steps interchangeables**. `StepData` n'a *aucun* role-flag (pas de `canBeEntry/Intermediate/End`). Toute step peut occuper n'importe quel rôle. Le rôle est implicite : la position dans la liste détermine entry (index 0) / fin (dernier).

**Step usage**. Chaque `StepData` peut être utilisée jusqu'à `maxStepUsage` fois (défaut 2) dans toute la génération. L'algo trie les candidats par usage croissant et ne tape la 2ᵉ utilisation que si plus aucune step à 0 usage n'est disponible avec un placeholder compatible.

**Récompense au niveau route, pas step**. La `RewardData` n'est plus sur `StepData` mais sur `RouteRuntime.EndReward`. `RouteManager` la distribue quand `runtime.IsLastStep(resolvedStep)` est vrai.

**Chaînage des indices**. Quand une step N est résolue, `RouteManager.HandleStepResolved` :
1. Lève `RouteEvents.StepResolved`
2. Si dernière step → distribue `EndReward` à l'inventaire
3. Sinon → appelle `Discover()` sur la step N+1 et lève `RouteEvents.ClueRevealed` avec son `initialClue`
4. Si toutes les steps de la route sont `Resolved` → passe la route en `Completed` et lève `RouteCompleted`

**Match step ↔ placeholder** :
- `step.type` doit matcher `placeholder.nodeType` (Clue↔Clue, Puzzle↔Puzzle)
- `step.placement.mode == Any` → tout placeholder du bon type
- `step.placement.mode == Region` → placeholder dont `regionId == step.placement.regionId`
- `step.placement.mode == Spot` → placeholder dont `spotId == step.placement.spotId`

## Conventions

- **Namespaces** : `EscapeGame.Core.*`, `EscapeGame.Routes.{Data,Runtime,Generation,Events,Services}`, `EscapeGame.Inventory.{Data,Runtime,Events}`, `EscapeGame.Journal.{Runtime,UI}`, `EscapeGame.Bonuses.Data`, `EscapeGame.Interactables.*` (legacy).
- **ScriptableObject menus** : tous sous `Create → EscapeGame → ...`
- **Pas de LINQ** dans les hot paths (foreach manuels). Les `IReadOnlyList<T>` n'ont pas de `.Contains()` natif → boucle manuelle obligatoire (sinon le compilateur tombe sur `MemoryExtensions.Contains` et plante).
- **Encodage** : UTF-8 sans BOM. Éviter les accents dans les chaînes affichées (Unity Windows peut avoir des soucis sur les vieilles consoles).
- **Éviter les nouveaux fichiers** : la base est posée. Toute nouvelle fonctionnalité doit s'ajouter dans la structure existante.

## Code legacy à supprimer

Quand les prefabs `Assets/Prefab/Indice.prefab` et `Assets/Prefab/Radio.prefab` auront été remplacés par des prefabs basés sur `ClueStep` et `AudioVisualPuzzleStep` :

- `Assets/Scripts/Interactables/Clues/CluePanel.cs`
- `Assets/Scripts/Interactables/Puzzles/PuzzleBase.cs`
- `Assets/Scripts/Interactables/Puzzles/AudioVisualPuzzle.cs`
- `Assets/Scripts/Core/World/LevelGenerator.cs`
- Le champ `zoneID` de `PlaceholderNode` (tag `Legacy`)

## Setup scène minimal

1. GameObject **`RouteManager`** avec composant `RouteManager`. Drag le `PlayerContext` du Player.
2. GameObject **`JournalManager`** avec composant `JournalManager`.
3. Sur le **Player** : composant `Inventory`, croisements bidirectionnels avec `PlayerContext.inventory`.
4. Crée un **`LetterAlphabet`** + des `LetterItem` pour chaque lettre nécessaire.
5. Crée un **`RouteGeneratorConfig`** (pool de steps, mots de passe candidats, alphabet, bonus, min/max length).
6. Pose des **`PlaceholderNode`** dans la scène : `nodeType` (Clue/Puzzle), `spotId`, `regionId`.
7. GameObject **`ProceduralRouteGenerator`** : drag le config + RouteManager.
8. Canvas Journal : `JournalView` → row prefab avec `RouteRowView` → block prefab avec `StepBlockView` (3 visuels enfants).

## État actuel

- ✅ Couche Data complète (StepData, PlacementData, RewardData, RouteGeneratorConfig, LetterAlphabet, ClueContent)
- ✅ Couche Runtime (StepBehaviour, ClueStep, PuzzleStep, AudioVisualPuzzleStep, RouteRuntime)
- ✅ Services (RouteManager singleton + chaînage + distribution récompense)
- ✅ Bus d'événements (RouteEvents, InventoryEvents)
- ✅ Inventaire (Inventory, ItemData, LetterItem, BonusItem)
- ✅ Journal (JournalManager + UI views)
- ✅ Génération procédurale (Planner + ProceduralRouteGenerator avec mot de passe)
- ⏳ Tests manuels en scène pas encore validés bout-en-bout (en cours côté utilisateur)
- ⏳ Migration des prefabs Indice/Radio vers ClueStep/AudioVisualPuzzleStep
- ⏳ UI réelle pour révéler `ClueContent` au joueur (pour le moment, simple Debug.Log)
- ⏳ Pas de système de sauvegarde / persistance
- ⏳ Bonus existant : seulement la base abstraite `BonusData`. Implémentations concrètes (PathFinder, Dechiffreur, Resolver…) à écrire.

## Pièges à éviter

- Ne pas remettre de `RouteData` SO : la génération est purement procédurale.
- Ne pas remettre `isEnd` ni `reward` sur `StepData` : ces deux notions sont gérées au niveau `RouteRuntime`.
- `PlaceholderNode.zoneID` est legacy (utilisé par `LevelGenerator`). Pour la procédural, n'utiliser que `regionId` et `spotId`.
- Les UnityEvents `OnDiscovered`/`OnResolved` sur `StepBehaviour` sont *écoutés* par `RouteManager`. Si tu désactives `RouteManager`, le chaînage ne fonctionne pas.
- L'ancien `LevelGenerator` et le nouveau `ProceduralRouteGenerator` ne doivent **pas tourner simultanément** dans la même scène — ils créent deux systèmes parallèles incompatibles.
