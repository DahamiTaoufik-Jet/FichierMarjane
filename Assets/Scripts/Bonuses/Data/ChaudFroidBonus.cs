using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Bonus Chaud/Froid : affiche un thermometre (slider) qui change de
    /// couleur du bleu (loin) au rouge (proche) en fonction de la distance
    /// entre le joueur et la prochaine step non resolue. Dure un temps fixe.
    /// Si le step est resolu avant la fin, le thermometre pointe vers le suivant.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Bonuses/ChaudFroid", fileName = "ChaudFroidBonus")]
    public class ChaudFroidBonus : BonusItem
    {
        [Header("Chaud/Froid")]
        [Tooltip("Duree du thermometre en secondes.")]
        public float duration = 30f;

        [Tooltip("Distance max au-dela de laquelle le thermometre est totalement froid (bleu).")]
        public float maxDistance = 50f;

        [Tooltip("Prefab du slider thermometre (doit avoir un ChaudFroidTracker).")]
        public GameObject thermometerPrefab;

        public override void Execute(PlayerContext context)
        {
            if (context == null) return;

            Transform player = context.GetPlayerTransform();
            var closest = BonusUtils.FindClosestUnresolvedStep(player.position);

            if (closest == null)
            {
                Debug.Log("[ChaudFroidBonus] Aucune step non resolue trouvee.");
                return;
            }

            if (thermometerPrefab == null)
            {
                Debug.LogWarning("[ChaudFroidBonus] thermometerPrefab non assigne.");
                return;
            }

            // Trouver le Canvas principal pour y instancier le slider
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ChaudFroidBonus] Aucun Canvas trouve dans la scene.");
                return;
            }

            var go = Instantiate(thermometerPrefab, canvas.transform);
            var tracker = go.GetComponent<ChaudFroidTracker>();
            if (tracker == null)
            {
                Debug.LogWarning("[ChaudFroidBonus] Le prefab n'a pas de ChaudFroidTracker.");
                Destroy(go);
                return;
            }

            tracker.Init(player, closest.transform, duration, maxDistance);
            Debug.Log($"[ChaudFroidBonus] Thermometre actif pour {duration}s.");
        }
    }
}
