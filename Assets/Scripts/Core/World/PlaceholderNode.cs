using UnityEngine;

namespace EscapeGame.Core.World
{
    public enum ProceduralNodeType
    {
        Puzzle,
        Clue,
        BonusSpawn
    }

    /// <summary>
    /// Attach to transforms in the scene to denote spawn points for the procedural generator.
    /// </summary>
    public class PlaceholderNode : MonoBehaviour
    {
        [Header("Node Definition")]
        public ProceduralNodeType nodeType;
        
        [Tooltip("Used to group paired elements (like puzzles and clues) in the same geographic region.")]
        public string zoneID;

        private void OnDrawGizmos()
        {
            switch (nodeType)
            {
                case ProceduralNodeType.Puzzle:
                    Gizmos.color = Color.red;
                    break;
                case ProceduralNodeType.Clue:
                    Gizmos.color = Color.blue;
                    break;
                case ProceduralNodeType.BonusSpawn:
                    Gizmos.color = Color.yellow;
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}
