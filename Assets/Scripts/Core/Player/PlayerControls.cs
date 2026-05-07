using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Deplacement basique du joueur via la map "Player" du InputSystem_Actions.
    /// Lit l'action Move (Vector2 WASD/Arrows/Gamepad) et l'action Sprint (Button).
    /// Le mouvement est applique en repere local du transform porteur :
    ///  - axe Y du Move -> avant/arriere selon transform.forward
    ///  - axe X du Move -> strafe selon transform.right
    /// PlayerLook se charge d'aligner le yaw du root sur la camera quand on bouge,
    /// donc avancer revient toujours a "marcher devant le perso".
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerControls : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("InputActionAsset contenant la map 'Player' avec les actions Move et Sprint.")]
        public InputActionAsset actions;

        [Header("Vitesse")]
        [Tooltip("Vitesse de marche en m/s.")]
        public float walkSpeed = 4f;

        [Tooltip("Multiplicateur de vitesse quand Sprint est tenu.")]
        public float sprintMultiplier = 1.7f;

        private CharacterController controller;
        private InputAction moveAction;
        private InputAction sprintAction;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("[PlayerControls] InputActionAsset non assigne.");
                return;
            }

            var map = actions.FindActionMap("Player", throwIfNotFound: true);
            moveAction   = map.FindAction("Move",   throwIfNotFound: true);
            sprintAction = map.FindAction("Sprint");
            map.Enable();
        }

        private void OnDisable()
        {
            if (actions == null) return;
            actions.FindActionMap("Player")?.Disable();
        }

        private void Update()
        {
            if (moveAction == null) return;

            // Bloque le mouvement quand une UI est ouverte
            if (UIState.IsAnyUIOpen)
            {
                controller.SimpleMove(Vector3.zero);
                return;
            }

            Vector2 input = moveAction.ReadValue<Vector2>();
            bool sprinting = sprintAction != null && sprintAction.IsPressed();
            float speed = walkSpeed * (sprinting ? sprintMultiplier : 1f);

            Vector3 dir = transform.forward * input.y + transform.right * input.x;
            controller.SimpleMove(dir * speed);
        }
    }
}
