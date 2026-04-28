using UnityEngine;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Minimal envelope passed to Bonuses and other systems to access the player state.
    /// </summary>
    public class PlayerContext : MonoBehaviour
    {
        [Header("Caméras")]
        public Camera fpsCamera;

        [Header("Modules joueur")]
        [Tooltip("Inventaire du joueur (lettres, bonus, objets de progression).")]
        public EscapeGame.Inventory.Runtime.Inventory inventory;

        [Tooltip("Scanner FPS du joueur (utilisé par certains bonus pour cibler des objets).")]
        public PlayerScanner scanner;

        // NOTE : la référence au JournalManager est ajoutée à l'étape 5
        // pour ne pas créer de dépendance circulaire avant son existence.

        public Transform GetPlayerTransform()
        {
            return transform;
        }
    }
}
