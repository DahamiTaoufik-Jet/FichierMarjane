using UnityEngine;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Utilitaires partages entre les bonus (PathFinder, ChaudFroid, etc.).
    /// </summary>
    public static class BonusUtils
    {
        /// <summary>
        /// Trouve la premiere step non resolue la plus proche du joueur
        /// parmi toutes les routes actives (non completees).
        /// Retourne null si aucune step eligible.
        /// </summary>
        public static StepBehaviour FindClosestUnresolvedStep(Vector3 playerPos)
        {
            var rm = RouteManager.Instance;
            if (rm == null) return null;

            StepBehaviour closest = null;
            float closestDist = float.MaxValue;

            for (int r = 0; r < rm.Routes.Count; r++)
            {
                var route = rm.Routes[r];
                if (route.State == RouteState.Completed) continue;

                // Premiere step non resolue de cette route
                for (int s = 0; s < route.Steps.Count; s++)
                {
                    var step = route.Steps[s];
                    if (step != null && !step.IsResolved)
                    {
                        float dist = Vector3.Distance(playerPos, step.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = step;
                        }
                        break; // On ne prend que la premiere non resolue par route
                    }
                }
            }

            return closest;
        }
    }
}
