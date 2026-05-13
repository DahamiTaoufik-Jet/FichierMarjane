# Journal/UI

Namespace : `EscapeGame.Journal.UI`

Vues UI du journal de progression : carte 2D, modal de details, panneaux HUD.

---

## Classes

### JournalView : MonoBehaviour
Construit la carte 2D du journal a partir des routes connues du JournalManager.
- Toggle : action OpenJournal (map "Game") via InputActionAsset.
- `GameObject panelRoot` : racine a activer/desactiver.
- `RectTransform worldContainer` : parent des nodes et lignes.
- `GameObject stageNodePrefab` : prefab avec StageNodeView.
- `GameObject connectorLinePrefab` : prefab Image (pivot 0/0.5).
- `Slider progressBar` : barre de progression globale.
- `StageModalView stageModal` : modal au clic.
- Layout : `hStep=130`, `zigAmp=28`, `routeGap=140`, `startX=80`, `startY=-80`.
- Couleurs lignes : `activeLineColor` (fonce), `inactiveLineColor` (gris clair).
- `ToggleJournal()` : ouvre/ferme + gestion curseur + UIState.
- `OpenForSelection()` : ouvre de force pour le mode selection bonus.
- `ExitSelectionMode()` : reconstruit et ferme apres selection.
- `Rebuild()` : detruit tous les objets, reconstruit routes en zigzag + ConnectorLines + progress bar.
- S'abonne a JournalManager.RouteAdded/Updated/Completed pour rebuild.
- OnEnable : Rebuild (rafraichit a chaque reouverture).
- Annule JournalSelectionMode si le joueur ferme le journal pendant la selection.

### StageNodeView : MonoBehaviour [RequireComponent(Button)]
Visuel d'un noeud d'etape sur la carte.
- UI : `Image background`, `TMP_Text numberText`, icones doubleCheck/lockOpen/lockClosed.
- Couleurs normales : completed (noir), current (blanc), locked (gris clair).
- Couleurs selection : `goldEligibleColor`, `greenEligibleColor`, `ineligibleBgColor`.
- `Init(StepBehaviour step, int stageIndex, StageModalView modal)` : assigne numero, refresh, listener.
- `Refresh()` : met a jour fond, texte, icones, interactivite selon StepState + JournalSelectionMode.
- Click normal : `StageModalData.Build(step)` -> `modalView.Show(data)`.
- Click selection : `JournalSelectionMode.Select(boundStep)`.

### StageModalData
DTO aggregant toutes les donnees pour le modal.
- `StepState State`, `StepType StepType`.
- `ClueContent InitialClue` : indice initial (clone pour ne pas modifier le SO).
- `string PuzzleQuestion`, `string PuzzleEncryptedQuestion`, `Sprite PuzzleSnapshot`.
- `ClueContent NextClue` : indice vers le bloc suivant (si resolu).
- `static Build(StepBehaviour step) -> StageModalData` :
  - Clone ClueContent, injecte snapshot des PositionalScan.
  - Gere les enigmes chiffrees via DecryptionTracker.
  - Cherche le step suivant via RouteManager pour remplir NextClue.

### StageModalView : MonoBehaviour
Modal a onglets affiche au clic sur un StageNode. 3 boutons (Initial/Enigme/Suite) switchent entre 3 panels plein ecran superposes.
- Onglets : `Button tabInitial/tabEnigme/tabSuite`.
- Couleurs onglets : actif (blanc), inactif (gris), desactive (gris fonce).
- Panel Initial : `TMP_Text initialClueText`, `Image initialClueImage`.
- Panel Enigme : `TMP_Text puzzleQuestionText`, `TMP_Text encryptedMessageText`, `GameObject encryptedMessageGroup`, `Image puzzleSnapshotImage`.
- Panel Suite : `TMP_Text nextClueText`, `Image nextClueImage`.
- `Button closeButton`, `GameObject journalPanel` (masque quand modal s'ouvre).
- `Show(StageModalData data)` : remplit les 3 panels, grise les onglets sans contenu, ouvre sur Initial.
- `Close()` : masque le modal, restaure le journal.
- `SwitchTab(int index)` : active le panel correspondant, bloque si pas de contenu.

### RouteRowView : MonoBehaviour (LEGACY/SECONDAIRE)
Ligne de blocs pour un affichage liste. Utilisee par l'ancien layout.
- `Text routeNameLabel`, `Transform blocksContainer`, `StepBlockView stepBlockPrefab`.
- `Image rowBackground` : normal ou dore (Completed).
- `Bind(RouteRuntime)` : construit les blocs.
- `Refresh()` : met a jour tous les blocs + nom + couleur fond.

### StepBlockView : MonoBehaviour [RequireComponent(Button)] (LEGACY/SECONDAIRE)
Bloc d'etape dans l'ancien layout liste.
- Icones : lockedIcon, discoveredIcon, resolvedIcon.
- Couleurs fond : locked, discovered, resolved.
- `Bind(StepBehaviour)` / `Refresh()`.
- Click : log debug (position si Discovered, indice si Resolved).

### CluePanelView : MonoBehaviour
Panneau HUD qui affiche un ClueContent quand un indice est revele.
- `GameObject panelRoot`, `TMP_Text textLabel`, `Image imageDisplay`, `AudioSource audioSource`.
- `float autoHideAfter = 6f`.
- S'abonne a `RouteEvents.ClueRevealed` et `ClueHidden`.
- `Show(ClueContent)` : active le panel, affiche texte/image/audio.

### TextPuzzlePanelView : MonoBehaviour
Panneau HUD pour les enigmes textuelles. Deux phases :
1. Shown (hover) : question en lecture seule.
2. Interact (scan) : champ de saisie actif.
- `TMP_Text questionLabel`, `TMP_InputField answerInput`, `TMP_Text feedbackLabel`.
- `InputActionAsset actions` (action Select), `Key cancelKey = Key.Escape`.
- HandleShown : affiche la question, masque le champ de saisie, UIState.SetUIOpen.
- HandleInteract : affiche le champ de saisie (inactif), unlock curseur.
- Premier Enter : active le champ de saisie, `UIState.IsInputFieldActive = true`.
- Enter suivant : soumet la reponse via `activeStep.TryAnswer()`.
- Escape : `activeStep.CancelInteraction()`.
- HandleClosed : ferme le panel, restaure le curseur.

---

## Dependances
- `EscapeGame.Bonuses.Data` (JournalSelectionMode, SelectionColorType, DecryptionTracker)
- `EscapeGame.Core.Player` (UIState)
- `EscapeGame.Journal.Runtime` (JournalManager)
- `EscapeGame.Routes.Data` (StepType, ClueContent)
- `EscapeGame.Routes.Events` (RouteEvents)
- `EscapeGame.Routes.Runtime` (RouteRuntime, StepBehaviour, StepState, RouteState, TextPuzzleStep, PositionalScanPuzzleStep)
- `EscapeGame.Routes.Services` (RouteManager)
- `UnityEngine.InputSystem`
- `TMPro`
