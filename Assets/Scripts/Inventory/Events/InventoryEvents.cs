using System;
using EscapeGame.Inventory.Data;

namespace EscapeGame.Inventory.Events
{
    /// <summary>
    /// Bus d'événements global pour l'inventaire. Permet à l'UI / le journal /
    /// les systèmes de progression de réagir sans coupler leurs références.
    /// </summary>
    public static class InventoryEvents
    {
        public static event Action<ItemData> ItemAdded;
        public static event Action<ItemData> ItemRemoved;
        public static event Action<ItemData> ItemUsed;

        internal static void RaiseItemAdded(ItemData item)   => ItemAdded?.Invoke(item);
        internal static void RaiseItemRemoved(ItemData item) => ItemRemoved?.Invoke(item);
        internal static void RaiseItemUsed(ItemData item)    => ItemUsed?.Invoke(item);
    }
}
