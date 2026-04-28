using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Core.World;
using EscapeGame.Routes.Data;

namespace EscapeGame.Routes.Generation
{
    /// <summary>
    /// Couplage step <-> placeholder choisi par le planner. Le RouteRuntime
    /// instanciera le prefab de la step a la position du placeholder.
    /// </summary>
    public class StepAssignment
    {
        public StepData StepData;
        public PlaceholderNode Placeholder;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    /// <summary>
    /// Plan en memoire d'une route, produit par le RouteGenerationPlanner
    /// avant toute instanciation. La recompense de fin (<see cref="EndReward"/>)
    /// est assignee dans une seconde passe par le mecanisme de distribution
    /// du mot de passe.
    /// </summary>
    public class RoutePlan
    {
        public string RouteId;
        public string DisplayName;
        public List<StepAssignment> Assignments = new List<StepAssignment>();
        public RewardData EndReward;

        public int Length => Assignments.Count;
        public StepAssignment Last => Assignments.Count == 0 ? null : Assignments[Assignments.Count - 1];
    }
}
