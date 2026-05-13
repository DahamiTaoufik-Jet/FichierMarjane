# Routes/Events

Namespace : `EscapeGame.Routes.Events`

Bus d'evenements statique du systeme de routes.

---

## RouteEvents (static class)

Tous les evenements sont leves exclusivement par le `RouteManager`.

### Evenements
- `Action<StepBehaviour> StepDiscovered` : une step passe de Locked a Discovered.
- `Action<StepBehaviour> StepResolved` : une step vient d'etre resolue.
- `Action<RouteRuntime> RouteStarted` : une route est enregistree et demarree.
- `Action<RouteRuntime> RouteCompleted` : toutes les steps d'une route sont resolues.
- `Action<ClueContent, StepBehaviour> ClueRevealed` : un indice initial est revele (le 2e param est la step qui a declenche la revelation, pas celle pointee par l'indice).
- `Action ClueHidden` : le panneau d'indice doit etre masque (fin de hover).
- `Action<string, StepBehaviour> TextPuzzleShown` : question affichee (hover, lecture seule).
- `Action<string, StepBehaviour> TextPuzzleInteract` : scan de l'enigme textuelle, activer saisie.
- `Action TextPuzzleClosed` : fermer le panneau d'enigme textuelle.

### Methodes Raise (internal)
- `RaiseStepDiscovered(StepBehaviour)`
- `RaiseStepResolved(StepBehaviour)`
- `RaiseRouteStarted(RouteRuntime)`
- `RaiseRouteCompleted(RouteRuntime)`
- `RaiseClueRevealed(ClueContent, StepBehaviour)`
- `RaiseClueHidden()`
- `RaiseTextPuzzleShown(string, StepBehaviour)`
- `RaiseTextPuzzleInteract(string, StepBehaviour)`
- `RaiseTextPuzzleClosed()`

### Abonnes principaux
- `JournalManager` : RouteStarted, StepDiscovered, StepResolved, RouteCompleted.
- `CluePanelView` : ClueRevealed, ClueHidden.
- `TextPuzzlePanelView` : TextPuzzleShown, TextPuzzleInteract, TextPuzzleClosed.
- `PathFinderLineTracker` / `ChaudFroidTracker` : StepResolved.

---

## Dependances
- `EscapeGame.Routes.Data` (ClueContent)
- `EscapeGame.Routes.Runtime` (StepBehaviour, RouteRuntime)
