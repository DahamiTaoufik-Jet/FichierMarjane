using UnityEngine;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Met a jour la ligne entre le joueur et la cible. Quand la cible est
    /// resolue, cherche automatiquement la prochaine step non resolue la
    /// plus proche. Se detruit apres la duree configuree.
    /// </summary>
    public class PathFinderLineTracker : MonoBehaviour
    {
        private Transform player;
        private Transform target;
        private float heightOffset;
        private float endTime;
        private LineRenderer line;

        public void Init(Transform player, Transform target, float heightOffset, float duration)
        {
            this.player = player;
            this.target = target;
            this.heightOffset = heightOffset;
            this.endTime = Time.time + duration;
            line = GetComponent<LineRenderer>();
        }

        private void OnEnable()
        {
            RouteEvents.StepResolved += OnStepResolved;
        }

        private void OnDisable()
        {
            RouteEvents.StepResolved -= OnStepResolved;
        }

        private void OnStepResolved(StepBehaviour step)
        {
            if (target == null) return;

            bool isCurrentTarget = step.transform == target
                || step.transform.IsChildOf(target)
                || target.IsChildOf(step.transform);

            if (!isCurrentTarget) return;

            Debug.Log($"[PathFinderLineTracker] Cible resolue, recherche suivante...");
            Transform newTarget = FindClosestUnresolved();
            if (newTarget != null)
            {
                target = newTarget;
                Debug.Log($"[PathFinderLineTracker] Nouvelle cible : {newTarget.name}");
            }
            else
            {
                Debug.Log("[PathFinderLineTracker] Plus de cible, destruction.");
                Destroy(gameObject);
            }
        }

        private Transform FindClosestUnresolved()
        {
            if (player == null) return null;
            var step = BonusUtils.FindClosestUnresolvedStep(player.position);
            return step != null ? step.transform : null;
        }

        private void Update()
        {
            if (player == null || target == null || Time.time >= endTime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 start = player.position + Vector3.up * heightOffset;
            Vector3 end = target.position + Vector3.up * 0.5f;

            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
    }
}
