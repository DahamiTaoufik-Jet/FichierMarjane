# Core/Interfaces

Namespace : `EscapeGame.Core.Interfaces`

Contient les interfaces partagees entre les modules du jeu.

---

## IScannable (interface)
Interface implementee par tout objet interactif via le PlayerScanner (FPS).

### Methodes
- `OnHover()` : appele en continu chaque frame ou l'objet est vise. Utilise pour le feedback visuel (outlines, UI hints) ou les timers de regard (gaze).
- `OnScan()` : appele pour valider une interaction (clic souris / touche E).
- `Reveal()` : revele l'objet de maniere permanente s'il etait cache.
- `OnHoverExit()` : appele quand le scanner quitte l'objet.

### Implementateurs
- `StepBehaviour` (et sous-classes : ClueStep, PuzzleStep, AudioVisualPuzzleStep, PositionalScanPuzzleStep, TextPuzzleStep)
- `CluePanel` (legacy)
- `PuzzleBase` (legacy)
