using System;
using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Core.World;
using EscapeGame.Routes.Data;

namespace EscapeGame.Routes.Generation
{
    /// <summary>
    /// Planificateur pur (sans MonoBehaviour) qui produit la liste des
    /// <see cref="RoutePlan"/> a partir du pool de steps et des placeholders
    /// disponibles. La generation respecte les contraintes de placement
    /// (Any/Region/Spot) et le nombre max d'usages par step.
    /// </summary>
    public class RouteGenerationPlanner
    {
        private readonly System.Random rng;
        private readonly int maxStepUsage;
        private readonly int minRouteLength;
        private readonly int maxRouteLength;

        public RouteGenerationPlanner(int minRouteLength, int maxRouteLength,
                                      int maxStepUsage, int? seed = null)
        {
            this.minRouteLength = Mathf.Max(2, minRouteLength);
            this.maxRouteLength = Mathf.Max(this.minRouteLength, maxRouteLength);
            this.maxStepUsage = Mathf.Max(1, maxStepUsage);
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// Construit toutes les routes possibles dans la limite des placeholders
        /// disponibles. Modifie <paramref name="placeholders"/> en consommant
        /// au fur et a mesure (les placeholders utilises sont retires).
        /// </summary>
        public List<RoutePlan> BuildPlans(IList<StepData> stepPool,
                                          List<PlaceholderNode> placeholders,
                                          string regionFilter = null)
        {
            var plans = new List<RoutePlan>();
            if (stepPool == null || stepPool.Count == 0 || placeholders == null) return plans;

            // Les ScanSpot sont des marqueurs de position pour les
            // PositionalScanPuzzleStep, pas des emplacements d'instanciation.
            // On les sort du pool de candidats.
            placeholders = placeholders.FindAll(p => p != null
                && p.nodeType != ProceduralNodeType.ScanSpot);

            // Filtre par region si renseigne
            if (!string.IsNullOrEmpty(regionFilter))
            {
                placeholders = placeholders.FindAll(p => p != null && p.regionId == regionFilter);
            }

            var usage = new Dictionary<StepData, int>();
            foreach (var s in stepPool) usage[s] = 0;

            int routeIndex = 0;
            while (placeholders.Count > 0)
            {
                var plan = TryBuildOne(stepPool, placeholders, usage, routeIndex);
                if (plan == null || plan.Length < 2) break; // plus rien de constructible
                plans.Add(plan);
                routeIndex++;
            }

            return plans;
        }

        // --------------------------------------------------------------------
        // Construction d'une route unique
        // --------------------------------------------------------------------

        private RoutePlan TryBuildOne(IList<StepData> stepPool,
                                      List<PlaceholderNode> placeholders,
                                      Dictionary<StepData, int> usage,
                                      int routeIndex)
        {
            int targetLength = rng.Next(minRouteLength, maxRouteLength + 1);
            targetLength = Mathf.Min(targetLength, placeholders.Count);
            if (targetLength < 2) return null;

            var plan = new RoutePlan
            {
                RouteId = Guid.NewGuid().ToString(),
                DisplayName = $"Route {routeIndex + 1}"
            };

            for (int i = 0; i < targetLength; i++)
            {
                var assignment = PickStepWithPlaceholder(stepPool, placeholders, usage);
                if (assignment == null) break;

                plan.Assignments.Add(assignment);
                placeholders.Remove(assignment.Placeholder);
                usage[assignment.StepData] = usage[assignment.StepData] + 1;
            }

            return plan;
        }

        /// <summary>
        /// Tire une step (en privilegiant celles avec le moins d'usages) et
        /// un placeholder qui satisfasse sa contrainte de placement.
        /// </summary>
        private StepAssignment PickStepWithPlaceholder(IList<StepData> stepPool,
                                                      List<PlaceholderNode> placeholders,
                                                      Dictionary<StepData, int> usage)
        {
            // 1. Trier les candidats par usage croissant pour respecter
            //    "max 2 utilisations, on ne tape la 2e qu'en dernier recours".
            var candidates = new List<StepData>();
            foreach (var s in stepPool)
                if (s != null && usage[s] < maxStepUsage) candidates.Add(s);

            candidates.Sort((a, b) => usage[a].CompareTo(usage[b]));

            // 2. Pour matcher le comportement "tirage aleatoire parmi les meilleurs",
            //    on parcourt par bandes d'usage croissantes.
            int cursor = 0;
            while (cursor < candidates.Count)
            {
                int currentUsage = usage[candidates[cursor]];

                // Limites de la bande [cursor, end)
                int end = cursor;
                while (end < candidates.Count && usage[candidates[end]] == currentUsage) end++;

                // Tirage aleatoire dans la bande, plusieurs essais tant qu'on n'a
                // pas trouve un placeholder compatible.
                var pool = candidates.GetRange(cursor, end - cursor);
                Shuffle(pool);

                foreach (var step in pool)
                {
                    var ph = FindCompatiblePlaceholder(step, placeholders);
                    if (ph != null)
                    {
                        return new StepAssignment
                        {
                            StepData = step,
                            Placeholder = ph,
                            Position = ph.transform.position,
                            Rotation = ph.transform.rotation
                        };
                    }
                }

                cursor = end;
            }

            return null;
        }

        // --------------------------------------------------------------------
        // Compatibilite step <-> placeholder
        // --------------------------------------------------------------------

        private PlaceholderNode FindCompatiblePlaceholder(StepData step, List<PlaceholderNode> placeholders)
        {
            var expected = step.type == StepType.Puzzle
                ? ProceduralNodeType.Puzzle
                : ProceduralNodeType.Clue;

            // On parcourt dans un ordre aleatoire pour ne pas toujours prendre le meme
            var indices = new int[placeholders.Count];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            Shuffle(indices);

            foreach (var idx in indices)
            {
                var ph = placeholders[idx];
                if (ph == null || ph.nodeType != expected) continue;

                switch (step.placement.mode)
                {
                    case PlacementMode.Any:
                        return ph;

                    case PlacementMode.Region:
                        if (!string.IsNullOrEmpty(step.placement.regionId)
                            && ph.regionId == step.placement.regionId)
                            return ph;
                        break;

                    case PlacementMode.Spot:
                        if (!string.IsNullOrEmpty(step.placement.spotId)
                            && ph.spotId == step.placement.spotId)
                            return ph;
                        break;
                }
            }
            return null;
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
