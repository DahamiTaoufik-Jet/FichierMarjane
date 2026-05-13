# Core/World

Namespace : `EscapeGame.Core.World`

Module monde : placeholders de scene et generateur procedural.

---

## Enums

### ProceduralNodeType
Type d'emplacement pour les PlaceholderNode.
- `Puzzle` : accueille un prefab PuzzleStep.
- `Clue` : accueille un prefab ClueStep.
- `BonusSpawn` : emplacement de spawn de bonus (reserve).
- `ScanSpot` : position au sol que le joueur doit occuper pour valider un PositionalScanPuzzleStep. Lie a un ou plusieurs placeholders Puzzle via `linkedSpotIds`.

---

## Classes

### PlaceholderNode : MonoBehaviour
Composant a placer sur un Empty GameObject en scene pour declarer un emplacement candidat.
- `ProceduralNodeType nodeType` : type d'etape.
- `string spotId` : identifiant exact du spot (pour placement Spot et liaison ScanSpot).
- `string regionId` : region (pour placement Region).
- `List<string> linkedSpotIds` : (ScanSpot) liste des spotId des Puzzles cibles. Many-to-many.
- `bool drawLinks = true` : dessine des lignes editeur ScanSpot -> Puzzle.
- `string zoneID` : LEGACY (LevelGenerator), ne pas utiliser.
- Gizmos : cubes colores par type, spheres pour ScanSpot, lignes vers les Puzzles lies.

### ProceduralRouteGenerator : MonoBehaviour
Orchestrateur du systeme procedural. Genere au Start.
1. Decouvre les PlaceholderNodes en scene (+ cache les ScanSpots a part).
2. Appelle `RouteGenerationPlanner.BuildPlans()`.
3. Distribue les recompenses : lettres du mot de passe (prioritaires) + bonus en remplissage.
4. Instancie les prefabs, injecte `stepData` + `routeId`.
5. Pour les `PositionalScanPuzzleStep` : injecte les poses des ScanSpots lies + capture le snapshot.
6. Enregistre chaque route via `RouteManager.RegisterRoute()`.
7. Detruit les ScanSpots utilises.

Champs :
- `RouteGeneratorConfig config` : SO de configuration.
- `RouteManager routeManager` : singleton (auto-detect si null).
- `bool cleanupUsedPlaceholders = true` : detruit les placeholders apres instanciation.
- `bool generateOnStart = true`.

Methodes cles :
- `Generate()` : lance la generation complete (aussi via ContextMenu "Regenerate").
- `AssignRewards(List<RoutePlan>)` : distribue lettres + bonus.
- `ChoosePassword(int routeCount) -> string` : choisit un mot dans passwordWords.
- `PickRandomBonus() -> BonusItem` : tire un bonus du pool avec respect des maxCount.
- `InstantiateAndRegister(RoutePlan)` : instancie les prefabs et enregistre la route.
- `InjectScanSpots(PositionalScanPuzzleStep, PlaceholderNode)` : cherche les ScanSpots lies au spotId du placeholder.

### LevelGenerator : MonoBehaviour (LEGACY)
Ancien generateur par paires Puzzle<->Clue avec GUID. A supprimer apres migration des prefabs Indice/Radio.
- Utilise `PlaceholderNode.zoneID` (legacy).
- Instancie un seul couple puzzle+clue lie par un GUID.

---

## Dependances
- `EscapeGame.Routes.Data` (StepData, RouteGeneratorConfig)
- `EscapeGame.Routes.Generation` (RouteGenerationPlanner, RoutePlan)
- `EscapeGame.Routes.Runtime` (StepBehaviour, PositionalScanPuzzleStep)
- `EscapeGame.Routes.Services` (RouteManager)
- `EscapeGame.Inventory.Data` (BonusItem, LetterAlphabet)
- `EscapeGame.Interactables.*` (legacy, LevelGenerator seulement)
