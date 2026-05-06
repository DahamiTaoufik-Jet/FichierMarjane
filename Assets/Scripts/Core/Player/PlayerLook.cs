using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Gere la rotation de la tete (Camera) independamment du corps.
    /// En FPS : la souris pilote la Sphere, le corps s'aligne au mouvement.
    /// En TPS : Cinemachine gere la camera, le corps s'aligne sur le yaw
    ///          de la Main Camera quand le joueur bouge.
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Le corps du joueur (root Player).")]
        public Transform playerBody;

        [Tooltip("La tete du joueur (Sphere). Pilotee par la souris en FPS uniquement.")]
        public Transform playerHead;

        [Tooltip("La Main Camera (pilotee par Cinemachine en TPS).")]
        public Transform mainCameraTransform;

        [Header("Parametres")]
        public float mouseSensitivity = 15f;
        [Tooltip("Vitesse a laquelle le corps s'aligne sur la vision quand on marche.")]
        public float bodyRotationSpeed = 8f;

        private float pitch = 0f;
        private float yaw = 0f;
        private bool isFPS = false;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerHead != null)
            {
                Vector3 euler = playerHead.eulerAngles;
                pitch = euler.x;
                yaw = euler.y;
            }

            if (mainCameraTransform == null && Camera.main != null)
                mainCameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            // Reassigne la Main Camera si la reference est perdue
            // (changement de mode TPS/FPS, camera desactivee, etc.)
            if (mainCameraTransform == null && Camera.main != null)
                mainCameraTransform = Camera.main.transform;
        }

        public void SetFPSMode(bool fps)
        {
            isFPS = fps;
            if (fps && mainCameraTransform != null)
            {
                yaw = mainCameraTransform.eulerAngles.y;
                pitch = mainCameraTransform.eulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }
        }

        private void Update()
        {
            if (playerBody == null) return;

            if (isFPS)
                UpdateFPS();
            else
                UpdateTPS();
        }

        private void UpdateFPS()
        {
            if (Mouse.current == null || playerHead == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
            pitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -85f, 85f);

            playerHead.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (IsMoving())
            {
                Quaternion target = Quaternion.Euler(0f, yaw, 0f);
                playerBody.rotation = Quaternion.Slerp(
                    playerBody.rotation, target, Time.deltaTime * bodyRotationSpeed);
            }
        }

        private void UpdateTPS()
        {
            if (!IsMoving() || mainCameraTransform == null) return;

            float camYaw = mainCameraTransform.eulerAngles.y;
            Quaternion target = Quaternion.Euler(0f, camYaw, 0f);
            playerBody.rotation = Quaternion.Slerp(
                playerBody.rotation, target, Time.deltaTime * bodyRotationSpeed);
        }

        private bool IsMoving()
        {
            if (Keyboard.current == null) return false;
            return Keyboard.current.wKey.isPressed
                || Keyboard.current.aKey.isPressed
                || Keyboard.current.sKey.isPressed
                || Keyboard.current.dKey.isPressed
                || Keyboard.current.upArrowKey.isPressed;
        }
    }
}
