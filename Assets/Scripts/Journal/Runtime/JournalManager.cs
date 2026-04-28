using System;
using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.Runtime
{
    /// <summary>
    /// Gestionnaire central du journal de progression.
    /// S'abonne au bus <see cref="RouteEvents"/> et tient à jour l'ensemble des
    /// routes connues du joueur. Émet ses propres événements pour informer la UI
    /// quand un état doit être rafraîchi.
    /// </summary>
    public class JournalManager : MonoBehaviour
    {
        public static JournalManager Instance { get; private set; }

        // Événements pour la UI
        public event Action<RouteRuntime> RouteAdded;
        public event Action<RouteRuntime> RouteUpdated;
        public event Action<RouteRuntime> RouteCompleted;

        private readonly List<RouteRuntime> knownRoutes = new List<RouteRuntime>();
        public IReadOnlyList<RouteRuntime> KnownRoutes => knownRoutes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            RouteEvents.RouteStarted   += HandleRouteStarted;
            RouteEvents.StepDiscovered += HandleStepDiscovered;
            RouteEvents.StepResolved   += HandleStepResolved;
            RouteEvents.RouteCompleted += HandleRouteCompleted;
        }

        private void OnDisable()
        {
            RouteEvents.RouteStarted   -= HandleRouteStarted;
            RouteEvents.StepDiscovered -= HandleStepDiscovered;
            RouteEvents.StepResolved   -= HandleStepResolved;
            RouteEvents.RouteCompleted -= HandleRouteCompleted;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ====================================================================
        // Handlers
        // ====================================================================

        private void HandleRouteStarted(RouteRuntime route)
        {
            if (route == null || knownRoutes.Contains(route)) return;
            knownRoutes.Add(route);
            RouteAdded?.Invoke(route);
        }

        private void HandleStepDiscovered(StepBehaviour step)
        {
            var route = FindOwningRoute(step);
            if (route != null) RouteUpdated?.Invoke(route);
        }

        private void HandleStepResolved(StepBehaviour step)
        {
            var route = FindOwningRoute(step);
            if (route != null) RouteUpdated?.Invoke(route);
        }

        private void HandleRouteCompleted(RouteRuntime route)
        {
            RouteCompleted?.Invoke(route);
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private RouteRuntime FindOwningRoute(StepBehaviour step)
        {
            if (step == null) return null;

            for (int i = 0; i < knownRoutes.Count; i++)
            {
                var stepsInRoute = knownRoutes[i].Steps;
                for (int j = 0; j < stepsInRoute.Count; j++)
                {
                    if (stepsInRoute[j] == step) return knownRoutes[i];
                }
            }
            return null;
        }
    }
}
