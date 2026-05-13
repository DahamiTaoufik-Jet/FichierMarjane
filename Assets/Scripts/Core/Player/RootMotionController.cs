using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Deplacement du joueur par Root Motion.
    /// Lit les inputs (Move + Sprint), envoie VelocityZ a l'Animator,
    /// et applique le deltaPosition du root motion via OnAnimatorMove().
    /// Le body est toujours oriente dans la direction du mouvement
    /// (rotation geree par PlayerLook).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class RootMotionController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("InputActionAsset contenant la map 'Player' avec Move et Sprint.")]
        public InputActionAsset actions;

        [Header("Animation")]
        [Tooltip("Acceleration de l'interpolation du parametre Animator.")]
        public float animAccel = 10f;

        [Tooltip("Deceleration quand l'input revient a zero.")]
        public float animDecel = 10f;

        private const float WALK_VAL = 0.5f;
        private const float RUN_VAL = 2.0f;

        private Animator animator;
        private Rigidbody rb;
        private InputAction moveAction;
        private InputAction sprintAction;

        private float currentVelocityZ;

        private static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("[RootMotionController] InputActionAsset non assigne.");
                return;
            }

            var map = actions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = map.FindAction("Move", throwIfNotFound: true);
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

            if (UIState.IsAnyUIOpen)
            {
                currentVelocityZ = Mathf.MoveTowards(currentVelocityZ, 0f, animDecel * Time.deltaTime);
                animator.SetFloat(VelocityZHash, currentVelocityZ);
                return;
            }

            Vector2 input = moveAction.ReadValue<Vector2>();
            bool sprinting = sprintAction != null && sprintAction.IsPressed();
            float maxVal = sprinting ? RUN_VAL : WALK_VAL;

            // N'importe quel input (W, A, S, D) = avancer dans la direction du body
            float targetZ = input.sqrMagnitude > 0.01f ? maxVal : 0f;

            float accel = targetZ < 0.01f ? animDecel : animAccel;
            currentVelocityZ = Mathf.MoveTowards(currentVelocityZ, targetZ, accel * Time.deltaTime);

            animator.SetFloat(VelocityZHash, currentVelocityZ);
        }

        private void OnAnimatorMove()
        {
            if (animator == null) return;

            transform.position += animator.deltaPosition;

            ApplyGravity();
        }

        private void ApplyGravity()
        {
            float rayOriginHeight = 1f;
            float rayLength = 2f;
            Vector3 rayOrigin = transform.position + Vector3.up * rayOriginHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y;
                transform.position = pos;
            }
        }
    }
}
