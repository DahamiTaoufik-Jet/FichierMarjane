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

        private const string FPS_CAM_TAG = "FPSCam";

        private float currentGazeTimer = 0f;
        private bool  isGazingThisFrame = false;
        private Transform playerTransform;
        private bool fpsCameraSwitchCheckDone = false;
        private float wavePhase = 0f;

        private void Awake()
        {
            ConfigureWaveRenderer();
            EscapeGame.Core.Player.PlayerCamera.FPSCameraActivated += HandleFPSCameraActivated;
        }

        private void OnDestroy()
        {
            EscapeGame.Core.Player.PlayerCamera.FPSCameraActivated -= HandleFPSCameraActivated;
        }

        private void OnValidate()
        {
            if (maxDetectionDistance < maxValidationDistance)
                maxDetectionDistance = maxValidationDistance;
        }

        private void Start()
        {
            TryAcquireFPSCamera();
            TryAcquireWaveRenderer();
        }

        private void TryAcquireFPSCamera()
        {
            if (playerTransform != null)
            {
                Debug.Log($"[AudioVisualPuzzleStep:{name}] Camera deja acquise : {playerTransform.name}");
                return;
            }

            Debug.Log($"[AudioVisualPuzzleStep:{name}] Recherche FPSCam...");

            // Cherche la camera FPS via son tag
            GameObject fpsCamGO = GameObject.FindWithTag(FPS_CAM_TAG);
            if (fpsCamGO != null)
            {
                Debug.Log($"[AudioVisualPuzzleStep:{name}] FPSCam trouvee : {fpsCamGO.name}");
                playerTransform = fpsCamGO.transform;
                return;
            }

            // Fallback sur Camera.main
            if (Camera.main != null)
            {
                Debug.Log($"[AudioVisualPuzzleStep:{name}] FPSCam introuvable, fallback Camera.main : {Camera.main.name}");
                playerTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning($"[AudioVisualPuzzleStep:{name}] Aucune camera trouvee avec le tag '{FPS_CAM_TAG}' et Camera.main est null.");
            }
        }

        private void HandleFPSCameraActivated(Transform fpsCameraTransform)
        {
            if (fpsCameraSwitchCheckDone) return;
            fpsCameraSwitchCheckDone = true;

            if (fpsCameraTransform == null) return;

            var fpsCamera = fpsCameraTransform.GetComponentInChildren<Camera>(true);
            playerTransform = fpsCamera != null ? fpsCamera.transform : fpsCameraTransform;

            TryAcquireWaveRenderer();
            Debug.Log($"[AudioVisualPuzzleStep:{name}] FPSCam assignee apres switch : {playerTransform.name}");
        }

        private void TryAcquireWaveRenderer()
        {
            if (waveRenderer != null) return;
            if (WaveOverlayHUD.Instance != null)
            {
                waveRenderer = WaveOverlayHUD.Instance.Line;
                ConfigureWaveRenderer();
            }
        }

        private void Update()
        {
            if (IsResolved) return;

            // Lazy retry : la FPSCamera ou le WaveOverlayHUD peuvent etre
            // inactifs au Start (mode TPS au demarrage)
            if (playerTransform == null)
            {
                TryAcquireFPSCamera();
                if (playerTransform == null) return;
            }
            if (waveRenderer == null)
                TryAcquireWaveRenderer();

            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance > maxDetectionDistance)
            {
                HideWave();
                currentGazeTimer = 0f;
                isGazingThisFrame = false;
                return;
            }

            UpdateWaveVisual(distance);

            if (isGazingThisFrame)
            {
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
                    currentGazeTimer = 0f;
                }
            }
            else
            {
                currentGazeTimer = 0f;
            }

            isGazingThisFrame = false;
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
            waveRenderer.useWorldSpace = true;
            waveRenderer.enabled = false;
        }

        private void UpdateWaveVisual(float distance)
        {
            if (waveRenderer == null || playerTransform == null) return;

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

            // Rendu en WORLD space : on calcule les positions par rapport
            // a la camera active pour que la ligne soit toujours visible
            waveRenderer.enabled = true;
            if (waveRenderer.positionCount != waveSegments)
                waveRenderer.positionCount = waveSegments;

            Vector3 camPos = playerTransform.position;
            Vector3 camFwd = playerTransform.forward;
            Vector3 camRight = playerTransform.right;
            Vector3 camUp = playerTransform.up;

            // Point d'ancrage : devant la camera, decale vers le bas
            Vector3 anchor = camPos + camFwd * waveForwardDistance + camUp * waveVerticalOffset;

            float twoPiFreq = 2f * Mathf.PI * frequency;
            for (int i = 0; i < waveSegments; i++)
            {
                float t = (float)i / (waveSegments - 1);
                float xOffset = (t - 0.5f) * waveLength;
                float yOffset = amplitude * Mathf.Sin(twoPiFreq * t + wavePhase);
                waveRenderer.SetPosition(i, anchor + camRight * xOffset + camUp * yOffset);
            }
        }

        private void HideWave()
        {
            if (waveRenderer != null && waveRenderer.enabled)
                waveRenderer.enabled = false;
        }
    }
}
