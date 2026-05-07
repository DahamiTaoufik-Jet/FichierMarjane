using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Events;

namespace EscapeGame.Inventory.Runtime
{
    /// <summary>
    /// Inventaire du joueur. Stocke des <see cref="ItemData"/> (lettres, bonus,
    /// objets de progression). Composant à attacher sur le GameObject Player.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Contexte joueur transmis aux bonus lors de leur utilisation.")]
        public PlayerContext playerContext;

        [Header("Debug / Test")]
        [Tooltip("Items ajoutes automatiquement a l'inventaire au Start. Pratique pour tester des bonus ou lettres sans completer de route.")]
        public List<ItemData> startingItems = new List<ItemData>();

        private readonly List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<ItemData> Items => items;
        public int Count => items.Count;

        private void Start()
        {
            if (startingItems != null)
            {
                for (int i = 0; i < startingItems.Count; i++)
                {
                    if (startingItems[i] != null)
                        AddItem(startingItems[i]);
                }
            }
        }

        // ====================================================================
        // API publique
        // ====================================================================

        /// <summary>Ajoute un item à l'inventaire.</summary>
        public void AddItem(ItemData item)
        {
            if (item == null) return;
            items.Add(item);
            InventoryEvents.RaiseItemAdded(item);
            Debug.Log($"[Inventory] +1 {item.rewardName} (total: {items.Count})");
        }

        /// <summary>Retire un item (premier match) de l'inventaire.</summary>
        public bool RemoveItem(ItemData item)
        {
            if (item == null) return false;
            bool removed = items.Remove(item);
            if (removed) InventoryEvents.RaiseItemRemoved(item);
            return removed;
        }

        public bool Contains(ItemData item) => items.Contains(item);

        /// <summary>
        /// Utilise l'item à l'index donné. Pour les <see cref="BonusItem"/>,
        /// déclenche le bonus et consomme l'item si configuré ainsi.
        /// </summary>
        public bool UseItem(int index)
        {
            if (index < 0 || index >= items.Count) return false;
            return UseItem(items[index]);
        }

        /// <summary>
        /// Utilise un item présent dans l'inventaire.
        /// </summary>
        public bool UseItem(ItemData item)
        {
            if (item == null || !items.Contains(item)) return false;

            switch (item)
            {
                case BonusItem bonus:
                    bonus.Use(playerContext);
                    if (bonus.consumeOnUse) RemoveItem(bonus);
                    break;

                default:
                    // Lettres, objets passifs, etc. : pas d'effet actif par défaut.
                    Debug.Log($"[Inventory] {item.rewardName} n'est pas utilisable activement.");
                    break;
            }

            InventoryEvents.RaiseItemUsed(item);
            return true;
        }

        /// <summary>Renvoie tous les items d'un type donné.</summary>
        public List<T> GetItemsOfType<T>() where T : ItemData
        {
            var result = new List<T>();
            for (int i = 0; i < items.Count; i++)
                if (items[i] is T typed) result.Add(typed);
            return result;
        }
    }
}
