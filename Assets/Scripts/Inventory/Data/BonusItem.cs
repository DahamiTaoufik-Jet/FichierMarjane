using UnityEngine;
using EscapeGame.Core.Player;

namespace EscapeGame.Inventory.Data
{
    /// <summary>
    /// Base des bonus stockables dans l'inventaire. Les implementations
    /// concretes (PathFinderBonus, etc.) heritent de cette classe et
    /// surchargent <see cref="Execute"/>.
    /// </summary>
    public class BonusItem : ItemData
    {
        [Header("Bonus")]
        [Tooltip("Si vrai, l'item est consomme apres utilisation.")]
        public bool consumeOnUse = true;

        /// <summary>
        /// Logique du bonus. Surchargee par les implementations concretes.
        /// </summary>
        public virtual void Execute(PlayerContext context)
        {
            Debug.Log($"[BonusItem:{name}] Pas d'implementation Execute.");
        }

        public void Use(PlayerContext context)
        {
            if (context == null)
            {
                Debug.LogWarning($"[BonusItem:{name}] PlayerContext manquant.");
                return;
            }
            Execute(context);
        }
    }
}
