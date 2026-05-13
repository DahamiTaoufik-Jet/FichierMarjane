# Journal/Runtime

Namespace : `EscapeGame.Journal.Runtime`

Gestionnaire central du journal de progression.

---

## JournalManager : MonoBehaviour (Singleton)

S'abonne au bus `RouteEvents` et tient a jour les routes connues du joueur. Emet ses propres evenements pour la UI.

### Champs
- `static Instance`.
- `List<RouteRuntime> knownRoutes` (prive).
- `IReadOnlyList<RouteRuntime> KnownRoutes` : acces en lecture.

### Evenements (pour la UI)
- `Action<RouteRuntime> RouteAdded` : nouvelle route enregistree.
- `Action<RouteRuntime> RouteUpdated` : une step a change d'etat dans la route.
- `Action<RouteRuntime> RouteCompleted` : route completee.

### Abonnements (RouteEvents)
- `RouteStarted` -> `HandleRouteStarted` : ajoute la route a knownRoutes, leve RouteAdded.
- `StepDiscovered` -> `HandleStepDiscovered` : trouve la route parente, leve RouteUpdated.
- `StepResolved` -> `HandleStepResolved` : idem.
- `RouteCompleted` -> `HandleRouteCompleted` : leve RouteCompleted.

### Helpers
- `FindOwningRoute(StepBehaviour) -> RouteRuntime` : parcourt toutes les routes pour trouver celle qui contient la step.

---

## Dependances
- `EscapeGame.Routes.Events` (RouteEvents)
- `EscapeGame.Routes.Runtime` (RouteRuntime, StepBehaviour)
