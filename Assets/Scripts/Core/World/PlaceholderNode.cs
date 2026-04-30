using System.Collections.Generic;
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
        BonusSpawn,
        /// <summary>
        /// Position (sur le sol) que le joueur doit occuper pour valider une enigme
        /// de type <c>PositionalScanPuzzleStep</c>. Lie a un ou plusieurs placeholders
        /// Puzzle via <see cref="linkedSpotIds"/>.
        /// </summary>
        ScanSpot
    }

    /// <summary>
    /// Composant a placer sur un GameObject vide en scene pour declarer
    /// un emplacement candidat pour le generateur procedural.
    ///
    /// Pour le nouveau systeme de routes : seuls <see cref="nodeType"/>,
    /// <see cref="spotId"/>, <see cref="regionId"/> et <see cref="linkedSpotIds"/>
    /// sont utilises. Le champ <see cref="zoneID"/> est conserve pour la
    /// compatibilite avec l'ancien <c>LevelGenerator</c>.
    /// </summary>
    public class PlaceholderNode : MonoBehaviour
    {
        [Header("Type de placeholder")]
        [Tooltip("Type d'etape que ce placeholder peut accueillir.")]
        public ProceduralNodeType nodeType;

        [Header("Identification (systeme procedural)")]
        [Tooltip("Identifiant exact de ce spot. Utilise par les steps en placement Spot, " +
                 "et par les ScanSpot pour pointer leur(s) Puzzle(s) cible(s).")]
        public string spotId;

        [Tooltip("Region a laquelle appartient ce placeholder. Utilise par les steps en placement Region.")]
        public string regionId;

        [Header("ScanSpot (PositionalScanPuzzleStep)")]
        [Tooltip("Quand nodeType == ScanSpot : liste des spotId des placeholders Puzzle " +
                 "dont ce ScanSpot est une position de scan valide. Many-to-many : " +
                 "un meme ScanSpot peut servir plusieurs puzzles.")]
        public List<string> linkedSpotIds = new List<string>();

        [Header("Debug")]
        [Tooltip("Si vrai, dessine en editeur des lignes vers les placeholders Puzzle relies " +
                 "(uniquement quand nodeType == ScanSpot).")]
        public bool drawLinks = true;

        [Header("Legacy (LevelGenerator par paires)")]
        [Tooltip("Ancien champ utilise par LevelGenerator. A retirer quand les anciens prefabs auront migre.")]
        public string zoneID;

        private void OnDrawGizmos()
        {
            switch (nodeType)
            {
                case ProceduralNodeType.Puzzle:     Gizmos.color = Color.red;     break;
                case ProceduralNodeType.Clue:       Gizmos.color = Color.blue;    break;
                case ProceduralNodeType.BonusSpawn: Gizmos.color = Color.yellow;  break;
                case ProceduralNodeType.ScanSpot:   Gizmos.color = Color.magenta; break;
                default:                            Gizmos.color = Color.white;   break;
            }

            if (nodeType == ProceduralNodeType.ScanSpot)
            {
                // Sphere pour indiquer une position au sol distincte des cubes des autres types
                Gizmos.DrawWireSphere(transform.position, 0.35f);

                if (drawLinks && linkedSpotIds != null && linkedSpotIds.Count > 0)
                {
                    var all = FindObjectsByType<PlaceholderNode>(FindObjectsSortMode.None);
                    foreach (var other in all)
                    {
                        if (other == null || other == this) continue;
                        if (other.nodeType != ProceduralNodeType.Puzzle) continue;
                        if (string.IsNullOrEmpty(other.spotId)) continue;

                        for (int i = 0; i < linkedSpotIds.Count; i++)
                        {
                            if (linkedSpotIds[i] == other.spotId)
                            {
                                Gizmos.DrawLine(transform.position, other.transform.position);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }
    }
}
