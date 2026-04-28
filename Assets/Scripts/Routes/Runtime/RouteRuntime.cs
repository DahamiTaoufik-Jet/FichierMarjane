using System.Collections.Generic;
using EscapeGame.Routes.Data;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Instance vivante d'une route construite par le generateur procedural.
    /// Ne reference plus de <c>RouteData</c> (les routes ne sont plus pre-definies) :
    /// les metadonnees sont fournies directement par le generateur.
    /// </summary>
    public class RouteRuntime
    {
        public string RouteId { get; }
        public string DisplayName { get; }

        /// <summary>Recompense distribuee a la resolution de la derniere step.</summary>
        public RewardData EndReward { get; }

        public RouteState State { get; private set; }

        private readonly List<StepBehaviour> steps;
        public IReadOnlyList<StepBehaviour> Steps => steps;

        public RouteRuntime(string routeId, string displayName,
                            IList<StepBehaviour> stepInstances, RewardData endReward)
        {
            RouteId = routeId;
            DisplayName = displayName;
            EndReward = endReward;
            steps = new List<StepBehaviour>(stepInstances);
            State = RouteState.Inactive;
        }

        public StepBehaviour GetNext(StepBehaviour current)
        {
            int index = steps.IndexOf(current);
            if (index < 0 || index >= steps.Count - 1) return null;
            return steps[index + 1];
        }

        public int IndexOf(StepBehaviour step) => steps.IndexOf(step);

        public bool IsLastStep(StepBehaviour step)
        {
            if (steps.Count == 0) return false;
            return steps[steps.Count - 1] == step;
        }

        public bool IsAllResolved
        {
            get
            {
                for (int i = 0; i < steps.Count; i++)
                    if (steps[i] == null || !steps[i].IsResolved) return false;
                return steps.Count > 0;
            }
        }

        internal void SetState(RouteState newState) => State = newState;
    }
}
