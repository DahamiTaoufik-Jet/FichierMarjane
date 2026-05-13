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

        [Header("Input (pour detecter le recul)")]
        [Tooltip("InputActionAsset contenant la map 'Player' avec l'action Move.")]
        public InputActionAsset actions;

        private float pitch = 0f;
        private float yaw = 0f;
        private bool isFPS = false;
        private InputAction moveAction;

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

            if (actions != null)
            {
                var map = actions.FindActionMap("Player");
                if (map != null)
                    moveAction = map.FindAction("Move");
            }
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

            // Bloque la visee quand une UI est ouverte
            if (UIState.IsAnyUIOpen) return;

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
                float inputAngle = GetInputAngle();
                float bodyYaw = yaw + inputAngle;
                Quaternion target = Quaternion.Euler(0f, bodyYaw, 0f);
                playerBody.rotation = Quaternion.Slerp(
                    playerBody.rotation, target, Time.deltaTime * bodyRotationSpeed);
            }
        }

        private void UpdateTPS()
        {
            if (!IsMoving() || mainCameraTransform == null) return;

            float camYaw = mainCameraTransform.eulerAngles.y;
            float inputAngle = GetInputAngle();
            float bodyYaw = camYaw + inputAngle;
            Quaternion target = Quaternion.Euler(0f, bodyYaw, 0f);
            playerBody.rotation = Quaternion.Slerp(
                playerBody.rotation, target, Time.deltaTime * bodyRotationSpeed);
        }

        private bool IsMoving()
        {
            if (moveAction != null)
            {
                Vector2 input = moveAction.ReadValue<Vector2>();
                return input.sqrMagnitude > 0.01f;
            }
            if (Keyboard.current == null) return false;
            return Keyboard.current.wKey.isPressed
                || Keyboard.current.aKey.isPressed
                || Keyboard.current.sKey.isPressed
                || Keyboard.current.dKey.isPressed
                || Keyboard.current.upArrowKey.isPressed;
        }

        /// <summary>
        /// Retourne l'angle (en degres) entre l'avant de la camera et la direction d'input.
        /// W = 0, D = 90, A = -90, S = 180, W+D = 45, W+A = -45, S+D = 135, S+A = -135.
        /// </summary>
        private float GetInputAngle()
        {
            Vector2 input = Vector2.zero;
            if (moveAction != null)
                input = moveAction.ReadValue<Vector2>();
            else if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            }

            if (input.sqrMagnitude < 0.01f) return 0f;
            return Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
        }
    }
}
