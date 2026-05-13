# Bonuses/Data

Namespace : `EscapeGame.Bonuses.Data`

Module des bonus utilisables par le joueur. Chaque bonus est un `BonusItem` (ScriptableObject) qui herite de `ItemData` et surcharge `Execute(PlayerContext)`.

---

## Classes

### BonusUtils (static)
Utilitaires partages entre les bonus.
- `FindClosestUnresolvedStep(Vector3 playerPos) -> StepBehaviour` : parcourt toutes les routes actives via `RouteManager.Instance`, retourne la premiere step non resolue la plus proche (une par route, la premiere dans l'ordre).

### DecryptionTracker (static)
Tracker des enigmes dechiffrees par le bonus Dechiffreur.
- `HashSet<string> decryptedStepIds` (prive)
- `MarkDecrypted(string stepId)` : ajoute le stepId au set.
- `IsDecrypted(string stepId) -> bool` : verifie si deja dechiffre.
- `IsEligibleForDecryption(bool puzzleEncrypted, string encryptedQuestion, string stepId) -> bool` : eligible si chiffre + a une question chiffree + pas encore dechiffre.
- `Clear()` : reset total (changement de scene).

### JournalSelectionMode (static)
Systeme generique de selection dans le journal pour les bonus (Dechiffreur, Resolveur).
- `IsActive` : true quand le journal est en mode selection.
- `ColorType` : `SelectionColorType` (Gold ou Green).
- `Enter(Func<StepBehaviour, bool> isEligible, Action<StepBehaviour> onSelected, Action onDone, SelectionColorType colorType)` : entre en mode selection.
- `Exit()` : quitte le mode selection sans agir.
- `IsEligible(StepBehaviour step) -> bool` : teste le predicat actif.
- `Select(StepBehaviour step)` : execute l'action, puis invoque le callback de fin et quitte le mode.

### SelectionColorType (enum)
- `Gold` : jaune dore (Dechiffreur).
- `Green` : vert (Resolveur).

---

### PathFinderBonus : BonusItem (SO)
Dessine une ligne temporaire entre le joueur et la step non resolue la plus proche.
- Champs : `duration` (10s), `lineWidth` (0.08), `lineColor`, `playerHeightOffset` (1).
- `Execute(PlayerContext)` : cherche la plus proche step non resolue via `BonusUtils`, spawne un `PathFinderLineTracker`.
- Menu : `EscapeGame/Bonuses/PathFinder`.

### PathFinderLineTracker : MonoBehaviour
Composant runtime attache au LineRenderer cree par PathFinderBonus.
- `Init(Transform player, Transform target, float heightOffset, float duration)` : configure.
- S'abonne a `RouteEvents.StepResolved` pour auto-rediriger vers la prochaine cible si la cible actuelle est resolue.
- Update : met a jour les positions du LineRenderer.
- Se detruit apres `duration` ou si plus de cible.

### ChaudFroidBonus : BonusItem (SO)
Affiche un slider thermometre (bleu=loin, rouge=proche) pendant une duree fixe.
- Champs : `duration` (30s), `maxDistance` (50), `thermometerPrefab`.
- `Execute(PlayerContext)` : instancie le prefab dans le Canvas, initialise le `ChaudFroidTracker`.
- Menu : `EscapeGame/Bonuses/ChaudFroid`.

### ChaudFroidTracker : MonoBehaviour
Met a jour le slider et sa couleur en fonction de la distance joueur-cible.
- `Init(Transform player, Transform target, float duration, float maxDistance)`.
- Gradient : bleu (coldColor) -> orange (warmColor) -> rouge (hotColor).
- S'abonne a `RouteEvents.StepResolved` pour auto-rediriger.
- Se detruit apres `duration`.

### DechiffreurBonus : BonusItem (SO)
Ouvre le journal en mode selection, laisse le joueur choisir une enigme chiffree a reveler.
- `consumeOnUse = false` (gere manuellement apres selection).
- `Execute(PlayerContext)` : entre en `JournalSelectionMode` avec predicat `DecryptionTracker.IsEligibleForDecryption`, action `DecryptionTracker.MarkDecrypted`, callback retire le bonus et ferme le journal.
- Menu : `EscapeGame/Bonuses/Dechiffreur`.

### ResolveurBonus : BonusItem (SO)
Ouvre le journal en mode selection, laisse le joueur choisir une enigme Discovered pour la resoudre de force.
- `consumeOnUse = false` (gere manuellement).
- `Execute(PlayerContext)` : entre en `JournalSelectionMode` avec predicat `CurrentState == Discovered`, action `step.ForceResolve()`, callback retire le bonus et ferme le journal.
- Menu : `EscapeGame/Bonuses/Resolveur`.

---

## Dependances
- `EscapeGame.Core.Player` (PlayerContext)
- `EscapeGame.Inventory.Data` (BonusItem, ItemData)
- `EscapeGame.Routes.Runtime` (StepBehaviour, StepState, RouteState)
- `EscapeGame.Routes.Services` (RouteManager)
- `EscapeGame.Routes.Events` (RouteEvents)
- `EscapeGame.Journal.UI` (JournalView, JournalSelectionMode)
