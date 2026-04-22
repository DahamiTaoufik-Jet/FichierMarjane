using UnityEngine;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Gère la transition entre la vue Third Person (TPS) et First Person (FPS).
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Caméras")]
        [Tooltip("La caméra ou l'objet gérant la vue à la 3ème personne (ex: Cinemachine Virtual Camera)")]
        public GameObject tpsCameraObject;
        
        [Tooltip("La caméra gérant la vue à la 1ère personne")]
        public GameObject fpsCameraObject;

        [Header("Visuels du Joueur")]
        [Tooltip("Placez ici le MeshRenderer de votre Capsule et de votre Sphère pour les cacher en vue FPS.")]
        public Renderer[] playerRenderers;

        [Header("Système de Scan")]
        [Tooltip("Le script PlayerScanner, qui ne doit tourner qu'en vue FPS")]
        public PlayerScanner playerScanner;

        [Header("Contrôles")]
        [Tooltip("La touche pour intervertir les caméras")]
        public UnityEngine.InputSystem.Key switchKey = UnityEngine.InputSystem.Key.C;

        private bool isFPSMode = false;

        private void Start()
        {
            // Par défaut, le cahier des charges indique que le jeu commence en TPS
            SetFPSMode(false);
        }

        private void Update()
        {
            // Transition à la demande du joueur (New Input System)
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[switchKey].wasPressedThisFrame)
            {
                isFPSMode = !isFPSMode;
                SetFPSMode(isFPSMode);
            }
        }

        private void SetFPSMode(bool enableFPS)
        {
            isFPSMode = enableFPS;
            
            // On bascule les deux GameObjects
            if (tpsCameraObject != null) tpsCameraObject.SetActive(!enableFPS);
            if (fpsCameraObject != null) fpsCameraObject.SetActive(enableFPS);

            // Gère automatiquement l'activation sécurisée du scan
            if (playerScanner != null)
            {
                playerScanner.enabled = enableFPS;
            }

            // Micro logique : On cache le corps du joueur en FPS, on le réaffiche en TPS
            if (playerRenderers != null && playerRenderers.Length > 0)
            {
                foreach (Renderer rend in playerRenderers)
                {
                    if (rend != null)
                    {
                        // Si enableFPS est vrai, on désactive le rendu (!enableFPS = faux)
                        rend.enabled = !enableFPS; 
                    }
                }
            }
        }
    }
}
