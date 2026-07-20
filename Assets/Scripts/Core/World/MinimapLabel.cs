using UnityEngine;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Oriente une etiquette de zone pour qu'elle reste LISIBLE (droite, face a la
    /// camera) sur la minimap, meme quand la carte tourne (minimap rotative).
    /// L'etiquette doit etre sur un layer que seule la camera minimap rend.
    /// </summary>
    public class MinimapLabel : MonoBehaviour
    {
        [Tooltip("Transform de la camera minimap (qui regarde vers le bas).")]
        public Transform minimapCam;

        private void LateUpdate()
        {
            if (minimapCam == null) return;
            // Face la camera (qui regarde -Y) et reste alignee sur son 'up'.
            transform.rotation = Quaternion.LookRotation(-minimapCam.forward, minimapCam.up);
        }
    }
}
