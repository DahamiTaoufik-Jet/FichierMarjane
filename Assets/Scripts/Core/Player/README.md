# Core/Player

Namespace : `EscapeGame.Core.Player`

Module joueur : cameras, mouvement, visee, scanner, etat UI.

---

## Classes

### PlayerContext : MonoBehaviour
Enveloppe minimale passee aux bonus et autres systemes pour acceder a l'etat du joueur.
- `Camera fpsCamera` : camera FPS.
- `Inventory inventory` : inventaire du joueur.
- `PlayerScanner scanner` : scanner FPS.
- `GetPlayerTransform() -> Transform` : retourne le transform du joueur.

### UIState (static)
Etat global des UI de gameplay. Compteur pour supporter les UIs imbriquees.
- `IsAnyUIOpen -> bool` : true si openCount > 0.
- `IsInputFieldActive -> bool` : true quand un champ texte capture le clavier.
- `SetUIOpen()` : incremente le compteur.
- `SetUIClosed()` : decremente le compteur (min 0).
- `Clear()` : reset total (changement de scene).
- Consulte par : PlayerCamera, PlayerLook, PlayerScanner, PlayerControls, InventoryPanelView, JournalView, TextPuzzlePanelView.

### PlayerCamera : MonoBehaviour
Gere la transition TPS/FPS (touche C).
- `CinemachineCamera tpsVirtualCamera` : camera virtuelle TPS.
- `Camera mainCamera` : Main Camera (desactivee en FPS pour eviter le double rendu).
- `GameObject fpsCameraObject` : camera FPS (enfant de Camera Look).
- `Renderer[] playerRenderers` : renderers du corps (caches en FPS).
- `PlayerLook playerLook` : notifie du changement de mode.
- `GameObject fpsCanvas` : Canvas HUD FPS (crosshair, indices).
- `PlayerScanner playerScanner` : actif uniquement en FPS.
- `Key switchKey = Key.C` : touche de bascule.
- `static event Action<Transform> FPSCameraActivated` : event leve quand la FPSCamera s'active (ecoute par AudioVisualPuzzleStep, PositionalScanPuzzleStep).
- `SetFPSMode(bool)` : bascule cameras, renderers, canvas, scanner, priority Cinemachine.
- Bloque le switch si `UIState.IsAnyUIOpen`.

### PlayerLook : MonoBehaviour
Rotation tete/corps independante.
- `Transform playerBody` : root Player.
- `Transform playerHead` : Sphere, pilotee par souris en FPS.
- `Transform mainCameraTransform` : Main Camera (Cinemachine en TPS).
- `float mouseSensitivity = 15f`, `float bodyRotationSpeed = 8f`.
- `SetFPSMode(bool)` : synchro yaw/pitch depuis la Main Camera.
- FPS : la souris pilote playerHead, le corps s'aligne au mouvement.
- TPS : Cinemachine gere la camera, le corps s'aligne sur le yaw quand on bouge.
- Bloque si `UIState.IsAnyUIOpen`.

### PlayerScanner : MonoBehaviour
Scanner FPS : deux rays independants.
- **Ray principal** : detecte tous les IScannable sauf PositionalScanPuzzleStep.
  - `scanEffectiveRange = 8f`, `scanRadius = 0.5f`, `scannableLayer`.
- **Ray positionnel** : detecte uniquement PositionalScanPuzzleStep (active seulement si `IsPlayerOnAnySpot`).
  - `positionalScanRange = 30f`, `positionalScanLayer`.
- `Camera fpsCamera`, `Key scanKey = Key.E`.
- Input : clic gauche OU touche scanKey.
- `ClearTarget()` / `ClearPositionalTarget()` : nettoyage.
- Bloque si `UIState.IsAnyUIOpen`.

### PlayerControls : MonoBehaviour [RequireComponent(CharacterController)]
Deplacement WASD via InputSystem.
- `InputActionAsset actions` : map "Player", actions Move + Sprint.
- `float walkSpeed = 4f`, `float sprintMultiplier = 1.7f`.
- `CharacterController.SimpleMove()` en repere local.
- Bloque si `UIState.IsAnyUIOpen`.

---

## Sous-dossier Animation/

### AnimationBlendScript : MonoBehaviour
Blend Tree de locomotion (marche/course).
- Lit les actions Forward, Backward, SLeft, SRight, Sprint.
- Interpole velocityX/Z vers WALK_VAL (0.5) ou RUN_VAL (2.0).
- Envoie `Velocity X` et `Velocity Z` a l'Animator.
- Pas de namespace (global).

---

## Dependances
- `Unity.Cinemachine` (PlayerCamera)
- `UnityEngine.InputSystem` (tous)
- `EscapeGame.Core.Interfaces` (PlayerScanner -> IScannable)
- `EscapeGame.Routes.Runtime` (PlayerScanner -> PositionalScanPuzzleStep)
- `EscapeGame.Inventory.Runtime` (PlayerContext -> Inventory)
