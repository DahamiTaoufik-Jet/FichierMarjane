# Interactables (LEGACY)

Namespaces : `EscapeGame.Interactables.Clues`, `EscapeGame.Interactables.Puzzles`

Anciens scripts utilises par le `LevelGenerator` (paires Puzzle<->Clue via GUID). A supprimer quand les prefabs `Indice.prefab` et `Radio.prefab` auront migre vers ClueStep/AudioVisualPuzzleStep.

---

## Classes

### CluePanel : MonoBehaviour, IScannable (Clues/)
Ancien panneau d'indice.
- `string pairID` : GUID lie au puzzle.
- `string clueText` : texte de l'indice.
- `bool isRevealed`.
- OnScan : revele l'indice (Debug.Log).
- `HandlePuzzleResolved()` : desactive le GO quand le puzzle lie est resolu.

### PuzzleBase : MonoBehaviour, IScannable (Puzzles/) (abstract)
Base des anciens puzzles.
- `string pairID` : GUID lie a l'indice.
- `UnityEvent OnResolved`.
- `bool isResolved`.
- `ForceResolve()` : resolution de force (bonus).
- `ResolvePuzzle()` (protected virtual) : marque resolu + leve OnResolved.

### AudioVisualPuzzle : PuzzleBase (Puzzles/)
Ancien puzzle audiovisuel (gaze + proximite). Successeur : `AudioVisualPuzzleStep`.
- `float requiredLookDuration = 3f`, `float maxValidationDistance = 2f`.
- OnHover : flag isGazingThisFrame.
- Update : timer si le joueur regarde + distance <= max.
- OnScan : valide si timer >= duree requise.

---

## Fichiers a supprimer
- `Clues/CluePanel.cs`
- `Puzzles/PuzzleBase.cs`
- `Puzzles/AudioVisualPuzzle.cs`
