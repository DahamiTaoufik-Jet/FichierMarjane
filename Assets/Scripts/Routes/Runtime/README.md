# Routes/Runtime

Namespace : `EscapeGame.Routes.Runtime`

Comportements runtime des etapes et des routes.

---

## Enums

### StepState
- `Locked = 0` : pas encore decouverte (cadenas).
- `Discovered = 1` : decouverte mais non resolue (cadenas argente).
- `Resolved = 2` : resolue (sans cadenas).

### RouteState
- `Inactive = 0` : route pas encore demarree.
- `Active = 1` : en cours.
- `Completed = 2` : toutes les etapes resolues (dore dans le journal).

---

## Classes

### StepBehaviour : MonoBehaviour, IScannable (abstract)
Base unifiee des etapes. Branchee sur le PlayerScanner via IScannable.
- `StepData stepData` : SO de description.
- `string routeId` : ID de la route parente.
- `UnityEvent OnDiscovered`, `UnityEvent OnResolved` : ecoutes par RouteManager.
- `StepState CurrentState`, `IsLocked`, `IsDiscovered`, `IsResolved`.
- `OnHover()` / `OnScan()` / `Reveal()` / `OnHoverExit()` : IScannable.
- `Discover()` : Locked -> Discovered.
- `ForceResolve()` : force la resolution (bonus Resolveur).
- `ResolveStep()` (protected virtual) : Discovered -> Resolved + leve OnResolved.

### ClueStep : StepBehaviour
Indice : hover pendant `hoverDelay` (1s) pour reveler le contenu, auto-resolu.
- `float hoverDelay = 1f`.
- `UnityEvent<ClueContent> OnContentRevealed`.
- OnHover : timer, puis Discover + DisplayOwnContent + ResolveStep.
- OnScan : Discover + DisplayOwnContent + ResolveStep immediatement.
- OnHoverExit : reset timer, leve `RouteEvents.RaiseClueHidden()`.
- `DisplayOwnContent()` : leve `RouteEvents.RaiseClueRevealed()`.

### PuzzleStep : StepBehaviour (abstract)
Base des enigmes. Le scan ne resout PAS automatiquement.
- OnScan : Discover seulement + re-affiche l'enonce via ClueRevealed.

### AudioVisualPuzzleStep : PuzzleStep [RequireComponent(AudioSource)]
Enigme gaze + proximite avec sinusoide HUD.
- `float requiredLookDuration = 3f`, `float maxDetectionDistance = 6f`, `float maxValidationDistance = 2f`.
- Onde sinusoidale : parametres wave* (segments, length, amplitude, frequence, speed, etc.).
- LineRenderer en world space, ancre devant la camera FPS.
- Amplitude proportionnelle a la proximite, frequence au centrage du regard.
- S'abonne a `PlayerCamera.FPSCameraActivated` pour capter la camera FPS.
- Acquiert le WaveOverlayHUD singleton pour le LineRenderer partage.
- OnHover : flag isGazingThisFrame.
- OnScan : valide si timer >= requiredLookDuration.
- ResolveStep : cache l'onde.

### PositionalScanPuzzleStep : PuzzleStep
Enigme : scanner depuis une position precise au sol.
- Configure par `ProceduralRouteGenerator.InjectScanSpots()`.
- `float horizontalTolerance = 0.7f`, `float verticalTolerance = 1.5f`.
- Feedback emissive : `idleEmissive` (noir) / `readyEmissive` (vert).
- `static int onSpotCount` / `static bool IsPlayerOnAnySpot` : compteur consulte par PlayerScanner.
- `UnityEvent OnEnteredScanZone`, `OnExitedScanZone`.
- Validation directe : touche E + sur le spot + vise le cube (raycast).
- OnScan : valide si joueur dans la zone.
- `Configure(IList<Pose> spots)` : recoit les positions, pioche une au hasard.
- Snapshot : capture une vue depuis le spot vers le cube (camera temporaire URP).
  - `int snapshotWidth/Height = 512`, `float snapshotFOV = 60f`, `float snapshotEyeOffset = 1.6f`.
  - `CaptureSnapshot()` -> coroutine, genere un `Sprite snapshot`.
  - Debug : sauve un PNG dans `Assets/_Debug/` en editeur.
- Gizmos runtime : sphere wireframe (rouge/jaune/vert) + ligne joueur-spot.

### TextPuzzleStep : PuzzleStep
Enigme textuelle avec question/reponse.
- `float hoverDelay = 1f`.
- Proprietes : `Question`, `Answer`, `Encrypted`, `EncryptedQuestion`, `IsDeciphered`, `DisplayQuestion`.
- `IsDeciphered` : verifie `deciphered` local OU `DecryptionTracker.IsDecrypted(stepId)`.
- `DisplayQuestion` : retourne la question chiffree ou claire selon l'etat.
- OnHover : timer -> affiche la question en lecture seule via `RouteEvents.RaiseTextPuzzleShown`.
- OnScan : Discover + affiche le champ de saisie via `RouteEvents.RaiseTextPuzzleInteract`.
- OnHoverExit : reset timer, leve `RouteEvents.RaiseTextPuzzleClosed`.
- `TryAnswer(string) -> bool` : compare (insensible casse, trim). Si correct : ClosePanel + ResolveStep.
- `CancelInteraction()` : referme le panneau.
- `Decipher()` : marque localement comme dechiffre, referme si ouvert.

### WaveOverlayHUD : MonoBehaviour [RequireComponent(LineRenderer)]
Singleton exposant un LineRenderer pour les AudioVisualPuzzleStep.
- `static Instance`.
- `LineRenderer Line` : configure en world space, material Sprites/Default, desactive par defaut.
- `float lineWidth = 0.05f`, `Color lineColor` (cyan).

### RouteRuntime
Instance vivante d'une route (pas de MonoBehaviour).
- `string RouteId`, `string DisplayName`, `RewardData EndReward`.
- `RouteState State`.
- `IReadOnlyList<StepBehaviour> Steps`.
- `GetNext(StepBehaviour current) -> StepBehaviour`.
- `IndexOf(StepBehaviour) -> int`.
- `IsLastStep(StepBehaviour) -> bool`.
- `bool IsAllResolved`.
- `SetState(RouteState)` (internal).

---

## Dependances
- `EscapeGame.Core.Interfaces` (IScannable)
- `EscapeGame.Core.Player` (PlayerCamera.FPSCameraActivated, UIState)
- `EscapeGame.Routes.Data` (StepData, ClueContent, RewardData, StepType)
- `EscapeGame.Routes.Events` (RouteEvents)
- `EscapeGame.Bonuses.Data` (DecryptionTracker) — TextPuzzleStep
- `UnityEngine.InputSystem` (PositionalScanPuzzleStep, TextPuzzleStep)
- `UnityEngine.Rendering.Universal` (PositionalScanPuzzleStep — snapshot URP)
