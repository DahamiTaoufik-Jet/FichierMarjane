using UnityEngine;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Énigme exigeant que le joueur maintienne son regard (gaze FPS) en continu
    /// sur l'objet, à proximité immédiate d'une source audio.
    /// Successeur direct de l'ancien <c>AudioVisualPuzzle</c>, adapté à la
    /// nouvelle hiérarchie de Steps.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioVisualPuzzleStep : PuzzleStep
    {
        [Header("Conditions de validation")]
        [Tooltip("Durée (en secondes) de regard ininterrompu nécessaire à la résolution.")]
        public float requiredLookDuration = 3f;

        [Tooltip("Distance maximale joueur ↔ source audio pour valider l'énigme.")]
        public float maxValidationDistance = 2f;

        private float currentGazeTimer = 0f;
        private bool  isGazingThisFrame = false;
        private Transform playerTransform;

        private void Start()
        {
            // À terme, injecté par un PlayerManager / PlayerContext.
            if (Camera.main != null)
                playerTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (IsResolved || playerTransform == null) return;

            if (isGazingThisFrame)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);

                if (distance <= maxValidationDistance)
                {
                    currentGazeTimer += Time.deltaTime;

                    if (currentGazeTimer >= requiredLookDuration)
                    {
                        Debug.Log($"[AudioVisualPuzzleStep:{name}] Conditions remplies — prêt pour OnScan().");
                    }
                }
                else
                {
                    // Regard maintenu mais hors de portée : on reset.
                    currentGazeTimer = 0f;
                }

                // Le flag doit être renvoyé par OnHover à la frame suivante,
                // sinon le timer redescend.
                isGazingThisFrame = false;
            }
            else
            {
                currentGazeTimer = 0f;
            }
        }

        public override void OnHover()
        {
            base.OnHover();
            if (IsResolved) return;
            isGazingThisFrame = true;
        }

        public override void OnScan()
        {
            // Découverte standard si verrouillé.
            base.OnScan();
            if (IsResolved) return;

            if (currentGazeTimer >= requiredLookDuration)
            {
                ResolveStep();
            }
            else
            {
                Debug.Log($"[AudioVisualPuzzleStep:{name}] Validation impossible. " +
                          $"Regardez sans interruption ({currentGazeTimer:F1}/{requiredLookDuration}s) " +
                          $"à moins de {maxValidationDistance} unités.");
            }
        }

        public override void OnHoverExit()
        {
            base.OnHoverExit();
            isGazingThisFrame = false;
            currentGazeTimer = 0f;
        }
    }
}
