using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Locomotion TPS "aim / strafe" : le corps s'oriente en permanence sur le yaw
    /// de la camera (visee), et un blend tree 2D (MoveX/MoveY) anime la bonne
    /// direction de pas pendant que le perso reste face camera.
    /// Deux modes de deplacement commutables a chaud via <see cref="useRootMotion"/> :
    ///  - root motion  : la position vient de l'animation (animator.deltaPosition)
    ///  - scripte       : la position vient de vitesses fixes (walk/run)
    /// Dans les deux cas, le mouvement passe par le CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class TpsAimLocomotion : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform de la camera de rendu (yaw d'aim). Auto = Camera.main au demarrage.")]
        public Transform cameraTransform;
        [Tooltip("InputActionAsset contenant les actions Move et Sprint.")]
        public InputActionAsset actions;

        [Header("Mode")]
        [Tooltip("true = deplacement par root motion ; false = deplacement scripte (vitesses fixes).")]
        public bool useRootMotion = true;

        [Header("Vitesses (mode scripte)")]
        public float walkSpeed = 1.8f;
        public float runSpeed = 4.5f;

        [Header("Reglages")]
        [Tooltip("Reactivite de l'alignement du corps sur la camera (plus haut = plus nerveux).")]
        public float rotationSharpness = 20f;
        [Tooltip("Lissage des parametres d'animation (damp time).")]
        public float animDamp = 0.12f;
        public float gravity = -20f;

        [Header("Correction visuelle strafe")]
        [Tooltip("Modele visuel (mesh+squelette) a contre-tourner pendant le strafe.")]
        public Transform visualRoot;
        [Tooltip("Angle de correction applique au modele en strafe pur (degres).")]
        public float strafeYawCorrection = 45f;

        private CharacterController cc;
        private Animator animator;
        private InputAction moveAction;
        private InputAction sprintAction;

        private Vector2 moveInput;
        private bool sprinting;
        private float verticalVelocity;
        private float moveAnimX;
        private float moveAnimY;
        private float backwardBlend;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            animator.applyRootMotion = useRootMotion;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (actions != null)
            {
                moveAction = actions.FindAction("Move", false);
                sprintAction = actions.FindAction("Sprint", false);
            }
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            sprintAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            sprintAction?.Disable();
        }

        private void Update()
        {
            // garde la coherence si on bascule le mode dans l'inspector en play
            if (animator.applyRootMotion != useRootMotion)
                animator.applyRootMotion = useRootMotion;

            moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            sprinting = sprintAction != null && sprintAction.IsPressed();

            UpdateAimOrientation();
            UpdateAnimator(out Vector3 worldWishDir, out float inputMagnitude);
            UpdateVisualStrafeYaw();

            if (!useRootMotion)
                ScriptedMove(worldWishDir, inputMagnitude);
        }

        /// <summary>Le corps suit le yaw de la camera (aim).</summary>
        private void UpdateAimOrientation()
        {
            if (cameraTransform == null) return;
            float camYaw = cameraTransform.eulerAngles.y;
            Quaternion target = Quaternion.Euler(0f, camYaw, 0f);
            float t = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
        }

        /// <summary>
        /// Calcule MoveX/MoveY (repere local du perso) a partir de l'input camera-relative,
        /// et les pousse dans l'animator avec lissage.
        /// </summary>
        private void UpdateAnimator(out Vector3 worldWishDir, out float inputMagnitude)
        {
            Vector3 camFwd = Vector3.forward;
            Vector3 camRight = Vector3.right;
            if (cameraTransform != null)
            {
                camFwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }

            Vector3 rawDir = camFwd * moveInput.y + camRight * moveInput.x;
            inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
            worldWishDir = rawDir.sqrMagnitude > 0.0001f ? rawDir.normalized : Vector3.zero;

            float localX = Vector3.Dot(transform.right, worldWishDir);
            float localY = Vector3.Dot(transform.forward, worldWishDir);

            float speedScale = sprinting ? 2f : 1f;
            float ax = Mathf.Clamp(localX * inputMagnitude * speedScale, -1f, 1f);
            float ay = Mathf.Clamp(localY * inputMagnitude * speedScale, -2f, 2f);

            // valeurs reelles memorisees pour la rotation visuelle du modele
            moveAnimX = ax;
            moveAnimY = ay;

            // En recul : coupe progressivement le strafe du blend tree (seule l'anim de recul
            // tourne) ; l'orientation diagonale est geree par la rotation du modele.
            backwardBlend = Mathf.InverseLerp(0f, -0.6f, ay);
            float blendX = Mathf.Lerp(ax, 0f, backwardBlend);

            animator.SetFloat(MoveXHash, blendX, animDamp, Time.deltaTime);
            animator.SetFloat(MoveYHash, ay, animDamp, Time.deltaTime);
        }

        /// <summary>Deplacement scripte (vitesses fixes) + gravite, via CharacterController.</summary>
        private void ScriptedMove(Vector3 worldWishDir, float inputMagnitude)
        {
            float speed = sprinting ? runSpeed : walkSpeed;
            Vector3 horizontal = worldWishDir * (speed * inputMagnitude);

            ApplyGravity();
            Vector3 motion = horizontal;
            motion.y = verticalVelocity;
            cc.Move(motion * Time.deltaTime);
        }

        /// <summary>Deplacement par root motion : la position vient de l'animation.</summary>
        private void OnAnimatorMove()
        {
            if (!useRootMotion) return;

            Vector3 delta = animator.deltaPosition;
            ApplyGravity();
            delta.y += verticalVelocity * Time.deltaTime;
            cc.Move(delta);
        }

        /// <summary>Contre-tourne le modele pour annuler le biais directionnel des clips de strafe.</summary>
        private void UpdateVisualStrafeYaw()
        {
            if (visualRoot == null) return;
            float mx = moveAnimX;
            float my = moveAnimY;
            // Pivot avant : correction du biais directionnel des clips de strafe (strafe pur/avant).
            float total = Mathf.Abs(mx) + Mathf.Abs(my) + 0.0001f;
            float forwardPivot = strafeYawCorrection * (mx / total);
            // Pivot recul : oriente le modele vers la diagonale de recul (le modele fait face a
            // l'oppose du mouvement, donc la seule anim de recul pointe la bonne direction).
            float backOrient = Mathf.Atan2(mx, my) * Mathf.Rad2Deg - 180f;
            // Transition continue depuis le pivot avant -> aucun saut de pivot.
            float targetYaw = Mathf.LerpAngle(forwardPivot, backOrient, backwardBlend);
            Quaternion target = Quaternion.Euler(0f, targetYaw, 0f);
            float t = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, target, t);
        }

        private void ApplyGravity()
        {
            if (cc.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
}
