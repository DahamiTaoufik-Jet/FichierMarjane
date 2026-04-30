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
