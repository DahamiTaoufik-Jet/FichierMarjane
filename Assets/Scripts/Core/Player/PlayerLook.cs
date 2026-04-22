using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Gère la rotation de la tête (Caméra) indépendamment du corps.
    /// Le corps ne s'aligne sur la caméra QUE lorsque le joueur se déplace.
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Le corps du joueur (la Capsule entière)")]
        public Transform playerBody;
        
        [Tooltip("La tête du joueur (votre Sphère ou Caméra)")]
        public Transform playerHead;

        [Header("Paramètres")]
        public float mouseSensitivity = 15f;
        [Tooltip("Vitesse à laquelle le corps s'aligne sur la vision quand on marche")]
        public float bodyRotationSpeed = 8f;
        
        // Stocke les angles absolus
        private float pitch = 0f; // Rotation Haut/Bas (Axe X)
        private float yaw = 0f;   // Rotation Gauche/Droite (Axe Y)

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerHead != null)
            {
                // Initialise avec l'angle de départ
                Vector3 euler = playerHead.eulerAngles;
                pitch = euler.x;
                yaw = euler.y;
            }
        }

        private void Update()
        {
            if (Mouse.current == null || playerHead == null || playerBody == null) return;

            // 1. LECTURE DE LA SOURIS (Orientation de la tête/caméra indépendante)
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            yaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
            pitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -85f, 85f); // Limite le cou

            // On assigne la rotation en monde ABSOLU à la tête. 
            // Même si le corps tourne, la caméra ne tremblera/bougera pas toute seule.
            playerHead.rotation = Quaternion.Euler(pitch, yaw, 0f);


            // 2. LECTURE DU CLAVIER (Alignement du corps s'il y a mouvement)
            bool isMoving = false;

            if (Keyboard.current != null)
            {
                // Vérifie si l'une des touches de déplacement est pressée (Z/Q/S/D ou W/A/S/D)
                if (Keyboard.current.wKey.isPressed ||
                    Keyboard.current.aKey.isPressed ||
                    Keyboard.current.sKey.isPressed ||
                    Keyboard.current.dKey.isPressed ||
                    Keyboard.current.upArrowKey.isPressed)
                {
                    isMoving = true;
                }
            }

            if (isMoving)
            {
                // Le corps cible la rotation horizontale (Yaw) de la caméra
                Quaternion targetBodyRotation = Quaternion.Euler(0f, yaw, 0f);
                
                // Tourne le corps en douceur vers la direction regardée par la tête
                playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetBodyRotation, Time.deltaTime * bodyRotationSpeed);
            }
        }
    }
}
