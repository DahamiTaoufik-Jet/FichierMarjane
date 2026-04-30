using UnityEngine;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Énigme exigeant que le joueur maintienne son regard (gaze FPS) en continu
    /// sur l'objet, à proximité immédiate d'une source audio.
    /// Successeur direct de l'ancien <c>AudioVisualPuzzle</c>, adapté à la
    /// nouvelle hiérarchie de Steps.
    ///
    /// Pendant le hover, un LineRenderer parente a la cam FPS trace une
    /// sinusoide en HUD :
    ///  - amplitude proportionnelle a la proximite,
    ///  - frequence proportionnelle au centrage du regard sur l'objet.
    /// Le LineRenderer doit etre un enfant de la Main Camera (FPS) pour suivre
    /// la vue comme un overlay canvas.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioVisualPuzzleStep : PuzzleStep
    {
        [Header("Conditions de validation")]
        [Tooltip("Durée (en secondes) de regard ininterrompu nécessaire à la résolution.")]
        public float requiredLookDuration = 3f;

        [Tooltip("Distance max a laquelle l'onde HUD commence a apparaitre quand le joueur regarde l'objet.")]
        public float maxDetectionDistance = 6f;

        [Tooltip("Distance max joueur <-> source audio pour valider (resoudre) l'enigme. Doit etre <= maxDetectionDistance.")]
        public float maxValidationDistance = 2f;

        [Header("Visualisation onde sinusoidale (HUD FPS)")]
        [Tooltip("LineRenderer qui rend l'onde. Si null, recherche WaveOverlayHUD.Instance au demarrage.")]
        public LineRenderer waveRenderer;

        [Tooltip("Nombre de segments composant la sinusoide.")]
        [Range(2, 256)] public int waveSegments = 64;

        [Tooltip("Largeur de l'onde dans le repere local du LineRenderer (~ unites ecran).")]
        public float waveLength = 0.6f;

        [Tooltip("Decalage Y local (negatif = sous le centre de l'ecran).")]
        public float waveVerticalOffset = -0.15f;

        [Tooltip("Distance Z locale devant la camera (eloignement de l'overlay).")]
        public float waveForwardDistance = 1f;

        [Tooltip("Amplitude max quand le joueur est colle a l'objet (distance = 0).")]
        public float waveMaxAmplitude = 0.08f;

        [Tooltip("Frequence (cycles sur la longueur visible) quand le regard est en bord de cible.")]
        public float waveMinFrequency = 1f;

        [Tooltip("Frequence (cycles sur la longueur visible) quand le regard est parfaitement centre.")]
        public float waveMaxFrequency = 8f;

        [Tooltip("Vitesse de defilement de la phase, en radians/seconde.")]
        public float waveSpeed = 8f;

        [Tooltip("Dot product min entre la direction de vue et la direction vers l'objet pour atteindre waveMinFrequency. 1.0 = centre parfait.")]
        [Range(0f, 1f)] public float waveCenterDotThreshold = 0.95f;

        private float currentGazeTimer = 0f;
        private bool  isGazingThisFrame = false;
        private Transform playerTransform;
        private float wavePhase = 0f;

        private void Awake()
        {
            ConfigureWaveRenderer();
        }

        private void OnValidate()
        {
            if (maxDetectionDistance < maxValidationDistance)
                maxDetectionDistance = maxValidationDistance;
        }

        private void Start()
        {
            // À terme, injecté par un PlayerManager / PlayerContext.
            if (Camera.main != null)
                playerTransform = Camera.main.transform;

            // Auto-resolution du LineRenderer via le singleton si non assigne.
            if (waveRenderer == null && WaveOverlayHUD.Instance != null)
            {
                waveRenderer = WaveOverlayHUD.Instance.Line;
                ConfigureWaveRenderer();
            }
        }

        private void Update()
        {
            if (IsResolved || playerTransform == null) return;

            if (isGazingThisFrame)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);

                // Hors de portee de DETECTION : pas d'onde et pas de timer
                if (distance > maxDetectionDistance)
                {
                    HideWave();
                    currentGazeTimer = 0f;
                    isGazingThisFrame = false;
                    return;
                }

                // Dans la zone de detection : l'onde s'affiche.
                // Le timer ne progresse que dans la zone de VALIDATION (plus restrictive).
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
                    // Visible mais trop loin pour valider : on reset.
                    currentGazeTimer = 0f;
                }

                UpdateWaveVisual(distance);

                // Le flag doit être renvoyé par OnHover à la frame suivante,
                // sinon le timer redescend.
                isGazingThisFrame = false;
            }
            else
            {
                // Pas hover : on reset notre timer mais on NE TOUCHE PAS au LineRenderer.
                // Il est partage avec les autres instances : seule celle qui est hover
                // dessine, et OnHoverExit (appele par le scanner sur changement de cible)
                // se charge de masquer l'onde.
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
            HideWave();
        }

        protected override void ResolveStep()
        {
            base.ResolveStep();
            HideWave();
        }

        // ====================================================================
        // Visualisation HUD
        // ====================================================================

        private void ConfigureWaveRenderer()
        {
            if (waveRenderer == null) return;
            // Local space pour que les positions soient relatives au transform du
            // LineRenderer (lui-meme parente sous la cam FPS = effet overlay).
            waveRenderer.useWorldSpace = false;
            waveRenderer.enabled = false;
        }

        private void UpdateWaveVisual(float distance)
        {
            if (waveRenderer == null) return;

            // Amplitude : 1 a distance 0, 0 a distance >= maxDetectionDistance
            float safeMax = Mathf.Max(0.0001f, maxDetectionDistance);
            float proximity = 1f - Mathf.Clamp01(distance / safeMax);
            float amplitude = waveMaxAmplitude * proximity;

            // Frequence : croit avec le centrage du regard sur l'objet
            Vector3 dirToObj = transform.position - playerTransform.position;
            if (dirToObj.sqrMagnitude < 0.0001f) dirToObj = playerTransform.forward;
            dirToObj.Normalize();
            float dot = Vector3.Dot(playerTransform.forward, dirToObj);
            float centered = Mathf.InverseLerp(waveCenterDotThreshold, 1f, dot);
            float frequency = Mathf.Lerp(waveMinFrequency, waveMaxFrequency, centered);

            wavePhase += waveSpeed * Time.deltaTime;
            if (wavePhase > Mathf.PI * 2f) wavePhase -= Mathf.PI * 2f;

            // Rendu en LOCAL space : x = horizontal, y = vertical, z = profondeur ecran
            waveRenderer.enabled = true;
            if (waveRenderer.positionCount != waveSegments)
                waveRenderer.positionCount = waveSegments;

            float twoPiFreq = 2f * Mathf.PI * frequency;
            for (int i = 0; i < waveSegments; i++)
            {
                float t = (float)i / (waveSegments - 1);
                float xLocal = (t - 0.5f) * waveLength;
                float yLocal = waveVerticalOffset + amplitude * Mathf.Sin(twoPiFreq * t + wavePhase);
                waveRenderer.SetPosition(i, new Vector3(xLocal, yLocal, waveForwardDistance));
            }
        }

        private void HideWave()
        {
            if (waveRenderer != null && waveRenderer.enabled)
                waveRenderer.enabled = false;
        }
    }
}
