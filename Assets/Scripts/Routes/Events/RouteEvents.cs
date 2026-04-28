using System;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Routes.Events
{
    /// <summary>
    /// Bus d'événements global pour le système de routes. Permet à des systèmes
    /// indépendants (Journal, UI, Inventaire, audio…) de réagir au déroulement
    /// du jeu sans coupler les références.
    /// Les événements sont levés exclusivement par le <c>RouteManager</c>.
    /// </summary>
    public static class RouteEvents
    {
        /// <summary>Une étape vient de passer de Locked à Discovered.</summary>
        public static event Action<StepBehaviour> StepDiscovered;

        /// <summary>Une étape vient d'être résolue.</summary>
        public static event Action<StepBehaviour> StepResolved;

        /// <summary>Une route a été enregistrée et démarrée par le RouteManager.</summary>
        public static event Action<RouteRuntime> RouteStarted;

        /// <summary>Toutes les étapes d'une route ont été résolues (route dorée).</summary>
        public static event Action<RouteRuntime> RouteCompleted;

        /// <summary>
        /// Un indice initial vient d'être révélé au joueur (quand l'étape précédente
        /// vient d'être résolue). Le second paramètre est l'étape qui a déclenché
        /// la révélation (l'étape précédente, pas celle pointée par l'indice).
        /// </summary>
        public static event Action<ClueContent, StepBehaviour> ClueRevealed;

        // -------- Raise helpers (internal pour limiter la surface d'API) --------
        internal static void RaiseStepDiscovered(StepBehaviour step) => StepDiscovered?.Invoke(step);
        internal static void RaiseStepResolved(StepBehaviour step) => StepResolved?.Invoke(step);
        internal static void RaiseRouteStarted(RouteRuntime route) => RouteStarted?.Invoke(route);
        internal static void RaiseRouteCompleted(RouteRuntime route) => RouteCompleted?.Invoke(route);
        internal static void RaiseClueRevealed(ClueContent clue, StepBehaviour by) => ClueRevealed?.Invoke(clue, by);
    }
}
