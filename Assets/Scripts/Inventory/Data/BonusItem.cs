using UnityEngine;
using EscapeGame.Bonuses.Data;
using EscapeGame.Core.Player;

namespace EscapeGame.Inventory.Data
{
    /// <summary>
    /// Wrapper d'un <see cref="BonusData"/> existant pour le rendre stockable
    /// dans l'inventaire. Lorsque le joueur déclenche le bonus depuis l'UI,
    /// l'inventaire appelle <see cref="Use"/> qui exécute la stratégie sous-jacente.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Inventory/BonusItem", fileName = "BonusItem")]
    public class BonusItem : ItemData
    {
        [Header("Bonus")]
        [Tooltip("ScriptableObject implémentant la mécanique du bonus.")]
        public BonusData bonus;

        [Tooltip("Si vrai, l'item est consommé après utilisation (retiré de l'inventaire).")]
        public bool consumeOnUse = true;

        /// <summary>
        /// Exécute le bonus dans le contexte du joueur.
        /// </summary>
        public void Use(PlayerContext context)
        {
            if (bonus == null)
            {
                Debug.LogWarning($"[BonusItem:{name}] Aucun BonusData assigné.");
                return;
            }
            if (context == null)
            {
                Debug.LogWarning($"[BonusItem:{name}] PlayerContext manquant.");
                return;
            }
            bonus.Execute(context);
        }
    }
}
