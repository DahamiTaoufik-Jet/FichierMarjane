# Inventory/Events

Namespace : `EscapeGame.Inventory.Events`

Bus d'evenements global pour l'inventaire.

---

## InventoryEvents (static class)

### Evenements
- `Action<ItemData> ItemAdded` : un item a ete ajoute.
- `Action<ItemData> ItemRemoved` : un item a ete retire.
- `Action<ItemData> ItemUsed` : un item a ete utilise.

### Methodes Raise (internal)
- `RaiseItemAdded(ItemData)`
- `RaiseItemRemoved(ItemData)`
- `RaiseItemUsed(ItemData)`

### Abonnes
- `InventoryPanelView` : ItemAdded, ItemRemoved (pour rafraichir l'UI).
