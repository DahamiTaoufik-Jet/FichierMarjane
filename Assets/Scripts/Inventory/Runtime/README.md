# Inventory/Runtime

Namespace : `EscapeGame.Inventory.Runtime`

Inventaire du joueur.

---

## Inventory : MonoBehaviour

Stocke des `ItemData` (lettres, bonus, objets de progression). A attacher sur le GameObject Player.

### Champs
- `PlayerContext playerContext` : contexte joueur pour les bonus.
- `List<ItemData> startingItems` : items ajoutes au Start (debug/test).
- `List<ItemData> items` (prive) : contenu de l'inventaire.
- `IReadOnlyList<ItemData> Items` : acces en lecture.
- `int Count`.

### API
- `AddItem(ItemData)` : ajoute + leve `InventoryEvents.ItemAdded`.
- `RemoveItem(ItemData) -> bool` : retire le premier match + leve `ItemRemoved`.
- `Contains(ItemData) -> bool`.
- `UseItem(int index) -> bool` / `UseItem(ItemData) -> bool` :
  - BonusItem : appelle `bonus.Use(playerContext)`, consomme si `consumeOnUse`.
  - Autres : log "pas utilisable activement".
  - Leve `InventoryEvents.ItemUsed`.
- `GetItemsOfType<T>() -> List<T>` : filtre par type.

---

## Dependances
- `EscapeGame.Core.Player` (PlayerContext)
- `EscapeGame.Inventory.Data` (ItemData, BonusItem, LetterItem)
- `EscapeGame.Inventory.Events` (InventoryEvents)
