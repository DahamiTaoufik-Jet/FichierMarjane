using UnityEngine;
using UnityEngine.UI;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Met a jour un slider thermometre en fonction de la distance
    /// joueur ↔ cible. Bleu = loin, rouge = proche.
    /// Si la cible est resolue, cherche automatiquement la suivante.
    /// Se detruit apres la duree configuree.
    /// </summary>
    public class ChaudFroidTracker : MonoBehaviour
    {
        [Tooltip("Slider du thermometre (assigne via le prefab).")]
        public Slider thermometer;

        [Tooltip("Image de remplissage du slider (pour changer la couleur).")]
        public Image fillImage;

        private Transform player;
        private Transform target;
        private float maxDistance;
        private float endTime;

        // Couleurs du gradient
        private static readonly Color coldColor = new Color(0.2f, 0.4f, 1f);    // Bleu
        private static readonly Color warmColor = new Color(1f, 0.8f, 0f);      // Orange
        private static readonly Color hotColor  = new Color(1f, 0.15f, 0.1f);   // Rouge

        public void Init(Transform player, Transform target, float duration, float maxDistance)
        {
            this.player = player;
            this.target = target;
            this.maxDistance = maxDistance;
            this.endTime = Time.time + duration;

            if (thermometer == null) thermometer = GetComponentInChildren<Slider>();
            if (fillImage == null && thermometer != null)
                fillImage = thermometer.fillRect?.GetComponent<Image>();
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

            // Chercher la prochaine cible
            var next = BonusUtils.FindClosestUnresolvedStep(player.position);
            if (next != null)
            {
                target = next.transform;
                Debug.Log($"[ChaudFroid] Nouvelle cible : {next.name}");
            }
            else
            {
                Debug.Log("[ChaudFroid] Plus de cible, destruction.");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (player == null || target == null || Time.time >= endTime)
            {
                Destroy(gameObject);
                return;
            }

            float dist = Vector3.Distance(player.position, target.position);

            // t = 0 (loin/froid) → 1 (proche/chaud)
            float t = Mathf.Clamp01(1f - dist / maxDistance);

            // Slider
            if (thermometer != null)
                thermometer.value = t;

            // Couleur : bleu → orange → rouge
            Color color;
            if (t < 0.5f)
                color = Color.Lerp(coldColor, warmColor, t * 2f);
            else
                color = Color.Lerp(warmColor, hotColor, (t - 0.5f) * 2f);

            if (fillImage != null)
                fillImage.color = color;
        }
    }
}
