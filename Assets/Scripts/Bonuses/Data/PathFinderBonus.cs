using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Bonus PathFinder : dessine une ligne temporaire entre le joueur et la
    /// prochaine step non resolue la plus proche. Cherche parmi toutes les
    /// routes actives la premiere step non resolue, puis choisit la plus
    /// proche du joueur.
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

            var rm = RouteManager.Instance;
            if (rm == null || rm.Routes.Count == 0)
            {
                Debug.Log("[PathFinderBonus] Aucune route enregistree.");
                return;
            }

            Transform player = context.GetPlayerTransform();
            Vector3 playerPos = player.position;

            StepBehaviour closest = null;
            float closestDist = float.MaxValue;

            for (int r = 0; r < rm.Routes.Count; r++)
            {
                var route = rm.Routes[r];
                if (route.State == RouteState.Completed) continue;

                StepBehaviour nextStep = FindFirstUnresolved(route);
                if (nextStep == null) continue;

                float dist = Vector3.Distance(playerPos, nextStep.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = nextStep;
                }
            }

            if (closest == null)
            {
                Debug.Log("[PathFinderBonus] Toutes les routes sont completees.");
                return;
            }

            Debug.Log($"[PathFinderBonus] Pointage vers '{closest.name}' a {closestDist:F1}m.");
            SpawnLine(player, closest.transform);
        }

        private StepBehaviour FindFirstUnresolved(RouteRuntime route)
        {
            for (int i = 0; i < route.Steps.Count; i++)
            {
                if (route.Steps[i] != null && !route.Steps[i].IsResolved)
                    return route.Steps[i];
            }
            return null;
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
