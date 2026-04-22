using UnityEngine;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Minimal envelope passed to Bonuses and other systems to access the player state.
    /// </summary>
    public class PlayerContext : MonoBehaviour
    {
        public Camera fpsCamera;
        
        // Example extensions:
        // public Inventory inventory;
        // public PlayerScanner scanner;
        
        public Transform GetPlayerTransform()
        {
            return transform;
        }
    }
}
