using UnityEngine;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Type de l'emplacement, mappe directement le StepType qu'il peut accueillir.
    /// </summary>
    public enum ProceduralNodeType
    {
        Puzzle,
        Clue,
        BonusSpawn
    }

    /// <summary>
    /// Composant a placer sur un GameObject vide en scene pour declarer
    /// un emplacement candidat pour le generateur procedural.
    ///
    /// Pour le nouveau systeme de routes : seuls <see cref="nodeType"/>,
    /// <see cref="spotId"/> et <see cref="regionId"/> sont utilises.
    /// Le champ <see cref="zoneID"/> est conserve pour la compatibilite
    /// avec l'ancien <c>LevelGenerator</c> (sera supprime quand les prefabs
    /// auront migre).
    /// </summary>
    public class PlaceholderNode : MonoBehaviour
    {
        [Header("Type de placeholder")]
        [Tooltip("Type d'etape que ce placeholder peut accueillir.")]
        public ProceduralNodeType nodeType;

        [Header("Identification (systeme procedural)")]
        [Tooltip("Identifiant exact de ce spot. Utilise par les steps en placement Spot.")]
        public string spotId;

        [Tooltip("Region a laquelle appartient ce placeholder. Utilise par les steps en placement Region.")]
        public string regionId;

        [Header("Legacy (LevelGenerator par paires)")]
        [Tooltip("Ancien champ utilise par LevelGenerator. A retirer quand les anciens prefabs auront migre.")]
        public string zoneID;

        private void OnDrawGizmos()
        {
            switch (nodeType)
            {
                case ProceduralNodeType.Puzzle:     Gizmos.color = Color.red;    break;
                case ProceduralNodeType.Clue:       Gizmos.color = Color.blue;   break;
                case ProceduralNodeType.BonusSpawn: Gizmos.color = Color.yellow; break;
                default:                            Gizmos.color = Color.white;  break;
            }
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}
