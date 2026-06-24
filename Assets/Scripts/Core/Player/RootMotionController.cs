using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Deplacement du joueur par Root Motion.
    /// Lit les inputs (Move + Sprint), envoie VelocityZ a l'Animator,
    /// et applique le deltaPosition du root motion via OnAnimatorMove().
    /// Les collisions sont gerees manuellement par CapsuleCast (pas de physique).
    /// Le Rigidbody reste kinematic pour eviter tout jitter.
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

        [Header("Collision")]
        [Tooltip("Marge de securite pour le CapsuleCast (evite de coller aux murs).")]
        public float skinWidth = 0.02f;

        [Tooltip("Layers bloques par le CapsuleCast.")]
        public LayerMask collisionMask = ~0;

        private const float WALK_VAL = 0.5f;
        private const float RUN_VAL = 2.0f;

        private Animator animator;
        private Rigidbody rb;
        private CapsuleCollider capsule;
        private PlayerLook playerLook;
        private InputAction moveAction;
        private InputAction sprintAction;

        private float currentVelocityZ;

        private static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            playerLook = GetComponentInChildren<PlayerLook>(true);

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

            float targetZ = input.sqrMagnitude > 0.01f ? maxVal : 0f;

            float accel = targetZ < 0.01f ? animDecel : animAccel;
            currentVelocityZ = Mathf.MoveTowards(currentVelocityZ, targetZ, accel * Time.deltaTime);

            animator.SetFloat(VelocityZHash, currentVelocityZ);
        }

        private void OnAnimatorMove()
        {
            if (animator == null) return;

            if (!UIState.IsAnyUIOpen)
            {
                Vector3 delta = animator.deltaPosition;

                // Coupe le root motion tant que le corps n'est pas aligne sur la
                // direction voulue : evite la derive vers l'avant au spam A-D.
                if (playerLook != null)
                    delta *= playerLook.BodyMoveAlignment;
                delta = CollideAndSlide(delta);

                transform.position += delta;
            }

            ApplyGravity();
        }

        // ====================================================================
        // CapsuleCast — collision manuelle
        // ====================================================================

        private Vector3 CollideAndSlide(Vector3 movement)
        {
            if (movement.sqrMagnitude < 0.0001f) return movement;

            float radius = capsule.radius - skinWidth;
            float halfHeight = (capsule.height * 0.5f) - capsule.radius;
            Vector3 center = transform.position + capsule.center;
            Vector3 bottom = center + Vector3.down * halfHeight;
            Vector3 top = center + Vector3.up * halfHeight;

            float distance = movement.magnitude;
            Vector3 direction = movement.normalized;

            if (Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit,
                distance + skinWidth, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // Avancer jusqu'au point de contact moins la marge
                float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
                Vector3 safeDelta = direction * safeDistance;

                // Slide : projeter le reste du mouvement sur le plan du mur
                Vector3 remaining = movement - safeDelta;
                Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);

                return safeDelta + slide;
            }

            return movement;
        }

        // ====================================================================
        // Gravite par Raycast
        // ====================================================================

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
