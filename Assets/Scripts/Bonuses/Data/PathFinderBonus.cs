using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Bonus PathFinder : dessine une ligne temporaire entre le joueur et la
    /// prochaine step non resolue la plus proche.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Bonuses/PathFinder", fileName = "PathFinderBonus")]
    public class PathFinderBonus : BonusItem
    {
        [Header("PathFinder")]
        [Tooltip("Duree d'affichage de la ligne en secondes.")]
        public float duration = 10f;

        [Tooltip("Largeur de la ligne.")]
        public float lineWidth = 0.08f;

        [Tooltip("Couleur de la ligne.")]
        public Color lineColor = new Color(0.2f, 0.8f, 1f, 0.8f);

        [Tooltip("Hauteur au-dessus du sol pour le point de depart.")]
        public float playerHeightOffset = 1f;

        public override void Execute(PlayerContext context)
        {
            if (context == null) return;

            Transform player = context.GetPlayerTransform();
            var closest = BonusUtils.FindClosestUnresolvedStep(player.position);

            if (closest == null)
            {
                Debug.Log("[PathFinderBonus] Aucune step non resolue trouvee.");
                return;
            }

            Debug.Log($"[PathFinderBonus] Pointage vers '{closest.name}'.");
            SpawnLine(player, closest.transform);
        }

        private void SpawnLine(Transform player, Transform target)
        {
            var go = new GameObject("PathFinderLine");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.useWorldSpace = true;

            var tracker = go.AddComponent<PathFinderLineTracker>();
            tracker.Init(player, target, playerHeightOffset, duration);
        }
    }
}
