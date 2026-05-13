# Routes/Services

Namespace : `EscapeGame.Routes.Services`

Service central de gestion des routes.

---

## RouteManager : MonoBehaviour (Singleton)

Gere le cycle de vie de toutes les routes generees. Recoit des routes planifiees du `ProceduralRouteGenerator`, chaine les resolutions et distribue les recompenses.

### Champs
- `static Instance` : singleton.
- `PlayerContext playerContext` : pour distribuer les recompenses.
- `bool persistAcrossScenes = false`.
- `bool drawRouteGizmos = false`, `bool drawStepLabels = true`.
- `List<RouteRuntime> routes` (prive), expose via `IReadOnlyList<RouteRuntime> Routes`.

### API publique
- `RegisterRoute(string routeId, string displayName, IList<StepBehaviour> stepInstances, RewardData endReward) -> RouteRuntime` :
  - Cree un RouteRuntime, l'ajoute, bind les events.
  - Passe la route en Active, leve RouteStarted.
  - Discover la premiere step + leve ClueRevealed si elle a un indice initial.
- `FindRoute(string routeId) -> RouteRuntime`.

### Mecanisme de chainage (HandleStepResolved)
1. `ResolveAllBefore(runtime, step)` : force la resolution de toutes les steps precedentes (cascade).
2. Leve `RouteEvents.StepResolved`.
3. Si derniere step : `DeliverReward(runtime.EndReward)`.
4. Sinon : Discover la step suivante + leve ClueRevealed.
5. Si toutes resolues : passe en Completed + leve RouteCompleted.

### Distribution de recompense
- `DeliverReward(RewardData)` : si c'est un ItemData, l'ajoute a `playerContext.inventory`.

### Gizmos
- Dessine des lignes colorees entre les steps de chaque route.
- Spheres wireframe : vert (Resolved), jaune (Discovered), rouge (Locked).
- Labels `R{n} #{s}` au-dessus de chaque step.

---

## Dependances
- `EscapeGame.Core.Player` (PlayerContext)
- `EscapeGame.Routes.Data` (RewardData)
- `EscapeGame.Routes.Events` (RouteEvents)
- `EscapeGame.Routes.Runtime` (RouteRuntime, StepBehaviour, StepState, RouteState)
- `EscapeGame.Inventory.Data` (ItemData)
