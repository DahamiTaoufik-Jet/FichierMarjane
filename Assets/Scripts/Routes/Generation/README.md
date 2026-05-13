# Routes/Generation

Namespace : `EscapeGame.Routes.Generation`

Planification pure (sans MonoBehaviour) des routes procedurales.

---

## Classes

### StepAssignment
Couplage step <-> placeholder choisi par le planner.
- `StepData StepData`
- `PlaceholderNode Placeholder`
- `Vector3 Position`
- `Quaternion Rotation`

### RoutePlan
Plan en memoire d'une route, avant instanciation.
- `string RouteId`
- `string DisplayName`
- `List<StepAssignment> Assignments`
- `RewardData EndReward` : assigne dans une seconde passe (mot de passe / bonus).
- `int Length` : nombre d'assignments.
- `StepAssignment Last` : derniere assignment.

### RouteGenerationPlanner
Algo pur qui produit les `RoutePlan` depuis le pool de steps + placeholders.
- Constructeur : `(int minRouteLength, int maxRouteLength, int maxStepUsage, int? seed)`.
- `BuildPlans(IList<StepData> stepPool, List<PlaceholderNode> placeholders, string regionFilter) -> List<RoutePlan>` :
  - Exclut les ScanSpots du pool de placeholders.
  - Filtre par region si renseigne.
  - Boucle : construit des routes tant qu'il reste des placeholders.
- `TryBuildOne(...)` : construit une route de longueur aleatoire [min, max].
  - Appelle `EnsureLastStepIsPuzzle(plan)` avant de retourner.
- `PickStepWithPlaceholder(...)` : tire une step avec le moins d'usages, puis un placeholder compatible.
  - Tri par usage croissant, tirage par bandes (les steps a 0 usage sont privilegiees).
  - Shuffle aleatoire dans chaque bande.
- `FindCompatiblePlaceholder(StepData, List<PlaceholderNode>)` : match type + contrainte placement (Any/Region/Spot).
- `EnsureLastStepIsPuzzle(RoutePlan)` : si la derniere step n'est pas Puzzle, swap avec le dernier Puzzle trouve en remontant.
- `Shuffle<T>(IList<T>)` : Fisher-Yates.

---

## Dependances
- `EscapeGame.Core.World` (PlaceholderNode, ProceduralNodeType)
- `EscapeGame.Routes.Data` (StepData, StepType, PlacementMode, RewardData)
