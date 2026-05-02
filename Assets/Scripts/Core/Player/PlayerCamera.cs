using UnityEngine;
using Unity.Cinemachine;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Gere la transition entre la vue Third Person (TPS) et First Person (FPS).
    /// En TPS, la CinemachineCamera a la racine pilote la Main Camera.
    /// En FPS, la CinemachineCamera est desactivee et la FPSCamera (enfant de
    /// Camera Look) prend le relais directement.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Cameras")]
        [Tooltip("CinemachineCamera TPS a la racine de la scene.")]
        public CinemachineCamera tpsVirtualCamera;

        [Tooltip("GameObject de la camera FPS (enfant de Camera Look).")]
        public GameObject fpsCameraObject;

        [Header("Visuels du Joueur")]
        [Tooltip("Renderers du corps a cacher en FPS.")]
        public Renderer[] playerRenderers;

        [Header("Rotation")]
        [Tooltip("Le script PlayerLook pour notifier le changement de mode.")]
        public PlayerLook playerLook;

        [Header("Systeme de Scan")]
        [Tooltip("Le script PlayerScanner, actif uniquement en FPS.")]
        public PlayerScanner playerScanner;

        [Header("Controles")]
        [Tooltip("Touche pour intervertir les cameras.")]
        public UnityEngine.InputSystem.Key switchKey = UnityEngine.InputSystem.Key.C;

        private const int PriorityActive = 10;
        private const int PriorityInactive = 0;
        private bool isFPSMode = false;

        private void Start()
        {
            SetFPSMode(false);
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current[switchKey].wasPressedThisFrame)
            {
                isFPSMode = !isFPSMode;
                SetFPSMode(isFPSMode);
            }
        }

        private void SetFPSMode(bool enableFPS)
        {
            isFPSMode = enableFPS;

            if (tpsVirtualCamera != null)
            {
                tpsVirtualCamera.Priority = enableFPS ? PriorityInactive : PriorityActive;
                tpsVirtualCamera.gameObject.SetActive(!enableFPS);
            }

            if (fpsCameraObject != null)
                fpsCameraObject.SetActive(enableFPS);

            if (playerLook != null)
                playerLook.SetFPSMode(enableFPS);

            if (playerScanner != null)
                playerScanner.enabled = enableFPS;

            if (playerRenderers != null)
            {
                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    if (playerRenderers[i] != null)
                        playerRenderers[i].enabled = !enableFPS;
                }
            }
        }
    }
}
