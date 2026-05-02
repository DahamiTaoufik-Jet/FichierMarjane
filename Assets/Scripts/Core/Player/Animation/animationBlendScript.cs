using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationBlendScript : MonoBehaviour
{
    Animator animator;
    float velocityX = 0f;
    float velocityZ = 0f;

    public InputActionAsset actions;
    private InputAction forwardAction;
    private InputAction backwardAction;
    private InputAction strafeLeftAction;
    private InputAction strafeRightAction;
    private InputAction sprintAction;

    [Header("Réglages de fluidité")]
    public float accel = 4.0f;
    public float deccel = 4.0f;

    // Valeurs cibles de ton Blend Tree
    private const float WALK_VAL = 0.5f;
    private const float RUN_VAL = 2.0f;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Initialisation des actions (Vérifie bien que ces noms existent dans ton Input Action Asset)
        forwardAction = actions.FindAction("Player/Forward");
        backwardAction = actions.FindAction("Player/Backward");
        strafeLeftAction = actions.FindAction("Player/SLeft");
        strafeRightAction = actions.FindAction("Player/SRight");
        sprintAction = actions.FindAction("Player/Sprint");
    }

    void Update()
    {
        bool isSprinting = sprintAction.IsPressed();
        float currentMax = isSprinting ? RUN_VAL : WALK_VAL;

        float targetZ = 0f;
        float targetX = 0f;

        // 1. On gère l'axe Z (Avant / Arrière)
        bool isForward = forwardAction.IsPressed();
        bool isBackward = backwardAction != null && backwardAction.IsPressed();

        if (isForward)
        {
            targetZ = currentMax;
        }
        else if (isBackward)
        {
            targetZ = -currentMax;
        }

        // 2. On gère l'axe X (Strafe) UNIQUEMENT si on ne recule pas
        // Cela empêche le bug des animations qui se mélangent en arrière
        if (!isBackward)
        {
            if (strafeRightAction.IsPressed())
            {
                targetX = currentMax;
            }
            else if (strafeLeftAction.IsPressed())
            {
                targetX = -currentMax;
            }
        }

        // 3. Interpolation et Envoi
        velocityX = Mathf.MoveTowards(velocityX, targetX, (targetX == 0 ? deccel : accel) * Time.deltaTime);
        velocityZ = Mathf.MoveTowards(velocityZ, targetZ, (targetZ == 0 ? deccel : accel) * Time.deltaTime);

        animator.SetFloat("Velocity X", velocityX);
        animator.SetFloat("Velocity Z", velocityZ);
    }
}