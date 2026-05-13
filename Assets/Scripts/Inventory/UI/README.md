# Inventory/UI

Namespace : `EscapeGame.Inventory.UI`

HUD de l'inventaire bonus.

---

## InventoryPanelView : MonoBehaviour

Panneau HUD : s'ouvre avec Q (action ToggleBonusInventory), affiche un item a la fois. Navigation gauche/droite avec A/D. Ordre : bonus d'abord, lettres ensuite.

### Champs UI
- `GameObject panelRoot` : racine a activer/desactiver.
- `Image itemIcon`, `TMP_Text itemName`, `TMP_Text itemDescription`.
- `GameObject arrowLeft`, `GameObject arrowRight`.
- `InputActionAsset actions` : map "Game" (ToggleBonusInventory, Select) + map "Player" (Move).
- `Key closeKey = Key.Escape`.
- `Inventory inventory` : reference a l'inventaire du joueur.

### Comportement
- Open : `UIState.SetUIOpen()`, unlock curseur, affiche l'item courant.
- Close : `UIState.SetUIClosed()`, relock le curseur si aucune autre UI n'est ouverte.
- Navigation A/D : cycle circulaire dans la liste triee.
- Select (Enter) : `UseCurrentItem()` -> `inventory.UseItem(item)` puis ferme.
- S'abonne a `InventoryEvents.ItemAdded/ItemRemoved` pour rafraichir si ouvert.
- Bloque si `UIState.IsInputFieldActive`.
- `RefreshSortedItems()` : trie les items (bonus d'abord, lettres ensuite).

---

## Dependances
- `EscapeGame.Core.Player` (UIState)
- `EscapeGame.Inventory.Data` (ItemData, BonusItem, LetterItem)
- `EscapeGame.Inventory.Events` (InventoryEvents)
- `EscapeGame.Inventory.Runtime` (Inventory)
- `UnityEngine.InputSystem`
- `TMPro`
