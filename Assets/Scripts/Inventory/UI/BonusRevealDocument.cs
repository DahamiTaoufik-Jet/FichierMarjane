using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Events;
using UnityEngine;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Revelation "nouveau bonus" en UI Toolkit. Remplace BonusRevealView.
    /// </summary>
    public class BonusRevealDocument : RevealCardDocument<BonusItem>
    {
        [Header("Textes")]
        public string headerLabel = "NOUVEAU BONUS !";
        public string fallbackName = "Nouveau bonus !";

        protected override string CardVariantClass { get { return "reveal-card--bonus"; } }

        protected override void Subscribe()
        {
            InventoryEvents.ItemAdded += HandleItemAdded;
        }

        protected override void Unsubscribe()
        {
            InventoryEvents.ItemAdded -= HandleItemAdded;
        }

        private void HandleItemAdded(ItemData item)
        {
            var bonus = item as BonusItem;
            if (bonus != null) Enqueue(bonus);
        }

        protected override void Fill(BonusItem bonus)
        {
            if (!string.IsNullOrEmpty(headerLabel)) Show(header, headerLabel);

            ShowIcon(bonus.icon);

            Show(title, string.IsNullOrEmpty(bonus.rewardName) ? fallbackName : bonus.rewardName);

            if (!string.IsNullOrEmpty(bonus.description))
                Show(desc, bonus.description);
        }
    }
}
