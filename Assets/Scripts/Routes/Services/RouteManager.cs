using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Routes.Services
{
    /// <summary>
    /// Service central qui gere le cycle de vie de toutes les routes generees.
    /// Singleton accessible via <see cref="Instance"/>.
    /// Recoit des routes deja planifiees du <c>ProceduralRouteGenerator</c>,
    /// chaine les resolutions et distribue la recompense de fin.
    /// </summary>
    public class RouteManager : MonoBehaviour
    {
        public static RouteManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Contexte joueur utilise pour distribuer les recompenses.")]
        public PlayerContext playerContext;

        [Tooltip("Si true, ce manager survit aux changements de scene.")]
        public bool persistAcrossScenes = false;

        [Header("Debug Gizmos")]
        [Tooltip("Dessine les routes en gizmos (lignes entre steps, couleurs par route).")]
        public bool drawRouteGizmos = false;

        [Tooltip("Affiche le nom/index de chaque step au-dessus de l'objet.")]
        public bool drawStepLabels = true;

        private readonly List<RouteRuntime> routes = new List<RouteRuntime>();
        public IReadOnlyList<RouteRuntime> Routes => routes;

        // ====================================================================
        // Cycle de vie
        // ====================================================================
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[RouteManager] Une autre instance existe deja - destruction.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < routes.Count; i++) UnbindRoute(routes[i]);
            if (Instance == this) Instance = null;
        }

        // ====================================================================
        // API publique
        // ====================================================================

        /// <summary>
        /// Enregistre une route deja instanciee. Le generateur fournit l'id,
        /// le nom affiche, la liste ordonnee des StepBehaviours et la recompense
        /// de fin de route.
        /// </summary>
        public RouteRuntime RegisterRoute(string routeId, string displayName,
                                          IList<StepBehaviour> stepInstances,
                                          RewardData endReward)
        {
            if (stepInstances == null || stepInstances.Count == 0)
            {
                Debug.LogError("[RouteManager] RegisterRoute : aucune step fournie.");
                return null;
            }

            var runtime = new RouteRuntime(routeId, displayName, stepInstances, endReward);
            routes.Add(runtime);

            BindRoute(runtime);

            runtime.SetState(RouteState.Active);
            RouteEvents.RaiseRouteStarted(runtime);

            var entry = stepInstances[0];
            entry.Discover();
            if (entry.stepData != null && entry.stepData.initialClue != null
                && !entry.stepData.initialClue.IsEmpty)
            {
                RouteEvents.RaiseClueRevealed(entry.stepData.initialClue, null);
            }

            return runtime;
        }

        public RouteRuntime FindRoute(string routeId)
        {
            for (int i = 0; i < routes.Count; i++)
                if (routes[i].RouteId == routeId) return routes[i];
            return null;
        }

        // ====================================================================
        // Abonnements aux Steps
        // ====================================================================

        private void BindRoute(RouteRuntime runtime)
        {
            for (int i = 0; i < runtime.Steps.Count; i++)
            {
                var step = runtime.Steps[i];
                if (step == null) continue;
                step.OnDiscovered.AddListener(() => HandleStepDiscovered(runtime, step));
                step.OnResolved.AddListener(() => HandleStepResolved(runtime, step));
            }
        }

        private void UnbindRoute(RouteRuntime runtime)
        {
            if (runtime == null) return;
            for (int i = 0; i < runtime.Steps.Count; i++)
            {
                var step = runtime.Steps[i];
                if (step == null) continue;
                step.OnDiscovered.RemoveAllListeners();
                step.OnResolved.RemoveAllListeners();
            }
        }

        // ====================================================================
        // Handlers
        // ====================================================================

        private void HandleStepDiscovered(RouteRuntime runtime, StepBehaviour step)
        {
            RouteEvents.RaiseStepDiscovered(step);
        }

        private void HandleStepResolved(RouteRuntime runtime, StepBehaviour step)
        {
            ResolveAllBefore(runtime, step);

            RouteEvents.RaiseStepResolved(step);

            // Recompense uniquement si la step resolue est la derniere de la route
            if (runtime.IsLastStep(step))
            {
                DeliverReward(runtime.EndReward);
            }

            // Decouvrir la step suivante et reveler son indice initial
            var next = runtime.GetNext(step);
            if (next != null)
            {
                next.Discover();
                if (next.stepData != null && next.stepData.initialClue != null
                    && !next.stepData.initialClue.IsEmpty)
                {
                    RouteEvents.RaiseClueRevealed(next.stepData.initialClue, step);
                }
            }

            if (runtime.IsAllResolved && runtime.State != RouteState.Completed)
            {
                runtime.SetState(RouteState.Completed);
                RouteEvents.RaiseRouteCompleted(runtime);
            }
        }

        // ====================================================================
        // Resolution en cascade
        // ====================================================================

        private void ResolveAllBefore(RouteRuntime runtime, StepBehaviour resolvedStep)
        {
            int idx = runtime.IndexOf(resolvedStep);
            if (idx <= 0) return;

            for (int i = 0; i < idx; i++)
            {
                var prev = runtime.Steps[i];
                if (prev != null && !prev.IsResolved)
                    prev.ForceResolve();
            }
        }

        // ====================================================================
        // Distribution de recompense
        // ====================================================================

        // ====================================================================
        // Gizmos
        // ====================================================================

        private void OnDrawGizmos()
        {
            if (!drawRouteGizmos || !Application.isPlaying || routes.Count == 0) return;

            for (int r = 0; r < routes.Count; r++)
            {
                var route = routes[r];
                Color routeColor = ColorFromHash(route.RouteId);

                for (int s = 0; s < route.Steps.Count; s++)
                {
                    var step = route.Steps[s];
                    if (step == null) continue;

                    Color stepColor;
                    switch (step.CurrentState)
                    {
                        case Runtime.StepState.Resolved:   stepColor = Color.green;  break;
                        case Runtime.StepState.Discovered: stepColor = Color.yellow; break;
                        default:                           stepColor = Color.red;    break;
                    }

                    Gizmos.color = stepColor;
                    Gizmos.DrawWireSphere(step.transform.position, 0.4f);

                    if (s < route.Steps.Count - 1)
                    {
                        var next = route.Steps[s + 1];
                        if (next != null)
                        {
                            Gizmos.color = routeColor;
                            Gizmos.DrawLine(step.transform.position, next.transform.position);
                        }
                    }

#if UNITY_EDITOR
                    if (drawStepLabels)
                    {
                        string label = $"R{r + 1} #{s + 1}";
                        var style = new GUIStyle();
                        style.normal.textColor = routeColor;
                        style.fontSize = 12;
                        style.fontStyle = FontStyle.Bold;
                        UnityEditor.Handles.Label(
                            step.transform.position + Vector3.up * 0.8f, label, style);
                    }
#endif
                }
            }
        }

        private static Color ColorFromHash(string id)
        {
            if (string.IsNullOrEmpty(id)) return Color.white;
            int hash = id.GetHashCode();
            float h = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(h, 0.8f, 1f);
        }

        // ====================================================================
        // Distribution de recompense
        // ====================================================================

        private void DeliverReward(RewardData reward)
        {
            if (reward == null) return;
            if (playerContext == null)
            {
                Debug.LogWarning($"[RouteManager] Recompense \"{reward.rewardName}\" prete mais aucun PlayerContext assigne.");
                return;
            }

            if (reward is Inventory.Data.ItemData item && playerContext.inventory != null)
            {
                playerContext.inventory.AddItem(item);
                return;
            }

            Debug.Log($"[RouteManager] Recompense \"{reward.rewardName}\" delivree mais aucune destination configuree pour {reward.GetType().Name}.");
        }
    }
}
