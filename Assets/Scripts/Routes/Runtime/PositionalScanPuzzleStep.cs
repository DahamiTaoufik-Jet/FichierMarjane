using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Enigme : scanner le bloc depuis une position precise au sol.
    /// Les positions valides sont injectees par le ProceduralRouteGenerator
    /// depuis les <c>PlaceholderNode</c> de type ScanSpot relies via leur
    /// <c>linkedSpotIds</c>. L'instance en pioche UNE au demarrage : c'est
    /// celle-la qui doit etre occupee pour valider.
    /// </summary>
    public class PositionalScanPuzzleStep : PuzzleStep
    {
        [Header("Tolerance de position")]
        [Tooltip("Tolerance horizontale (XZ) en metres entre le joueur et la position cible.")]
        public float horizontalTolerance = 0.7f;

        [Tooltip("Tolerance verticale (Y) en metres.")]
        public float verticalTolerance = 1.5f;

        [Header("Feedback emissive")]
        [Tooltip("Renderer dont l'emissive change quand le joueur est dans la zone valide. Auto via GetComponentInChildren si null. " +
                 "Le materiau doit avoir Emission activee pour que le changement soit visible.")]
        public Renderer feedbackRenderer;

        [Tooltip("Emissive applique tant que le joueur n'est pas dans la zone (ou ne regarde pas).")]
        public Color idleEmissive = Color.black;

        [Tooltip("Emissive applique quand le joueur est dans la zone valide ET regarde le cube.")]
        public Color readyEmissive = new Color(0.2f, 1f, 0.4f, 1f);

        [Header("Feedback events (optionnel)")]
        public UnityEvent OnEnteredScanZone;
        public UnityEvent OnExitedScanZone;

        [Header("Validation directe (sans scanner)")]
        [Tooltip("Si vrai, l'enigme est validee quand le joueur est sur le spot, vise le cube (cone de vue) et appuie sur la touche - peu importe la distance.")]
        public bool allowDirectValidation = true;

        [Tooltip("Touche de validation directe quand on est sur le spot et qu'on vise le cube.")]
        public Key validateKey = Key.E;

        [Tooltip("Obsolete - conserve pour eviter les warnings sur les prefabs existants.")]
        [HideInInspector] public float directValidationDotThreshold = 0.85f;

        [Header("Debug")]
        [Tooltip("Logs Console aux transitions zone/scan + au choix du spot.")]
        public bool verboseLogs = true;

        [Tooltip("Dessine un gizmo runtime sur le spot choisi + ligne vers le joueur.")]
        public bool drawRuntimeGizmos = true;

        /// <summary>Snapshot genere au chargement depuis le scan spot vers la cible.</summary>
        [HideInInspector] public Sprite snapshot;

        // ====================================================================
        // Runtime
        // ====================================================================

        private readonly List<Pose> validSpots = new List<Pose>();
        private Pose chosenSpot;
        private bool spotChosen = false;
        private bool configured = false;
        private Transform playerTransform;
        private Transform cameraTransform;
        private MaterialPropertyBlock mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private bool isGazingThisFrame = false;
        private bool wasInZone = false;
        private bool wasOnSpot = false;

        // Compteur statique : combien d'instances detectent le joueur sur leur spot
        private static int onSpotCount = 0;

        /// <summary>True si le joueur est sur au moins un spot de scan.</summary>
        public static bool IsPlayerOnAnySpot => onSpotCount > 0;

        /// <summary>
        /// Appele par le <c>ProceduralRouteGenerator</c> pour transmettre les
        /// positions valides extraites des ScanSpot lies. Une seule position est
        /// piochee au hasard comme cible reelle.
        /// </summary>
        public void Configure(IList<Pose> spots)
        {
            if (configured)
            {
                Debug.LogWarning($"[PositionalScanPuzzleStep:{name}] Configure deja appele - appel ignore.");
                return;
            }
            configured = true;

            if (spots == null || spots.Count == 0)
            {
                Debug.LogError($"[PositionalScanPuzzleStep:{name}] Aucun ScanSpot fourni - puzzle insolvable.");
                return;
            }

            validSpots.Clear();
            for (int i = 0; i < spots.Count; i++) validSpots.Add(spots[i]);

            int idx = Random.Range(0, validSpots.Count);
            chosenSpot = validSpots[idx];
            spotChosen = true;

            if (verboseLogs)
            {
                Debug.Log($"[PositionalScanPuzzleStep:{name}] Configure : {validSpots.Count} " +
                          $"position(s) recue(s). Choix #{idx} -> {chosenSpot.position}.");
            }
        }

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void Awake()
        {
            if (feedbackRenderer == null)
                feedbackRenderer = GetComponentInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            ApplyEmissive(idleEmissive);

            // S'abonner a l'event FPS pour capter la camera au switch
            EscapeGame.Core.Player.PlayerCamera.FPSCameraActivated += HandleFPSCameraActivated;
        }

        private void OnDestroy()
        {
            EscapeGame.Core.Player.PlayerCamera.FPSCameraActivated -= HandleFPSCameraActivated;
            if (wasOnSpot) onSpotCount = Mathf.Max(0, onSpotCount - 1);
        }

        private void HandleFPSCameraActivated(Transform fpsCameraTransform)
        {
            if (fpsCameraTransform == null) return;
            var cam = fpsCameraTransform.GetComponentInChildren<Camera>(true);
            cameraTransform = cam != null ? cam.transform : fpsCameraTransform;
        }

        private void Start()
        {
            // Cherche le joueur via le composant RootMotionController
            var rmc = FindFirstObjectByType<EscapeGame.Core.Player.RootMotionController>();
            if (rmc != null)
                playerTransform = rmc.transform;

            // Camera : tente une acquisition immediate
            if (cameraTransform == null)
            {
                GameObject fpsCamGO = GameObject.FindWithTag("FPSCam");
                if (fpsCamGO != null)
                    cameraTransform = fpsCamGO.transform;
                else if (Camera.main != null)
                    cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (IsResolved || !spotChosen || playerTransform == null) return;

            bool onSpot = IsPlayerInZone();
            bool inZone = isGazingThisFrame && onSpot;

            // Validation directe : touche d'abord (quasi gratuit), puis raycast
            // seulement si la touche est pressee ET le joueur est sur le spot.
            if (allowDirectValidation && onSpot
                && Keyboard.current != null
                && Keyboard.current[validateKey].wasPressedThisFrame
                && IsPlayerAimingAtMe())
            {
                if (verboseLogs)
                    Debug.Log($"[PositionalScanPuzzleStep:{name}] Validation directe (vise + sur le spot). Distance={DistanceToSpot():F2}m");
                ResolveStep();
                return;
            }

            // Transition position seule (independante du regard)
            if (onSpot && !wasOnSpot)
            {
                onSpotCount++;
                if (verboseLogs)
                    Debug.Log($"[PositionalScanPuzzleStep:{name}] Position OK - sur le spot. Distance={DistanceToSpot():F2}m");
            }
            else if (!onSpot && wasOnSpot)
            {
                onSpotCount = Mathf.Max(0, onSpotCount - 1);
                if (verboseLogs)
                    Debug.Log($"[PositionalScanPuzzleStep:{name}] Position perdue. Distance={DistanceToSpot():F2}m");
            }

            // Transition combinee (PRET A VALIDER = position + regard)
            if (inZone && !wasInZone)
            {
                ApplyEmissive(readyEmissive);
                OnEnteredScanZone?.Invoke();
                if (verboseLogs)
                    Debug.Log($"[PositionalScanPuzzleStep:{name}] PRET A VALIDER - bonne position + regard sur le cube. Distance={DistanceToSpot():F2}m");
            }
            else if (!inZone && wasInZone)
            {
                ApplyEmissive(idleEmissive);
                OnExitedScanZone?.Invoke();
                if (verboseLogs)
                    Debug.Log($"[PositionalScanPuzzleStep:{name}] Sortie de zone valide. Distance={DistanceToSpot():F2}m");
            }

            wasInZone = inZone;
            wasOnSpot = onSpot;
            isGazingThisFrame = false;
        }

        public override void OnHover()
        {
            base.OnHover();
            if (IsResolved) return;
            isGazingThisFrame = true;
        }

        public override void OnHoverExit()
        {
            base.OnHoverExit();
            isGazingThisFrame = false;
            if (wasInZone)
            {
                wasInZone = false;
                ApplyEmissive(idleEmissive);
                OnExitedScanZone?.Invoke();
            }
        }

        public override void OnScan()
        {
            // Discover si Locked + re-affiche l'enonce via PuzzleStep.OnScan
            base.OnScan();
            if (IsResolved) return;

            if (!spotChosen)
            {
                Debug.LogWarning($"[PositionalScanPuzzleStep:{name}] Pas de spot configure : scan impossible.");
                return;
            }

            if (IsPlayerInZone())
            {
                if (verboseLogs)
                    Debug.Log($"[PositionalScanPuzzleStep:{name}] Scan VALIDE depuis distance {DistanceToSpot():F2}m.");
                ResolveStep();
            }
            else
            {
                Debug.Log($"[PositionalScanPuzzleStep:{name}] Scan refuse - mauvaise position " +
                          $"(distance {DistanceToSpot():F2}m, tolerance H={horizontalTolerance}m V={verticalTolerance}m). Cherchez un autre angle.");
            }
        }

        protected override void ResolveStep()
        {
            // Liberer le compteur si le joueur etait sur le spot
            if (wasOnSpot)
            {
                onSpotCount = Mathf.Max(0, onSpotCount - 1);
                wasOnSpot = false;
            }
            base.ResolveStep();
            ApplyEmissive(idleEmissive);
        }

        // ====================================================================
        // Snapshot
        // ====================================================================

        [Header("Snapshot")]
        [Tooltip("Resolution du snapshot genere au chargement.")]
        public int snapshotWidth = 512;
        public int snapshotHeight = 512;

        [Tooltip("Champ de vision de la camera de capture.")]
        public float snapshotFOV = 60f;

        [Tooltip("Hauteur ajoutee a la position du spot pour simuler la vue du joueur.")]
        public float snapshotEyeOffset = 1.6f;

        /// <summary>
        /// Lance la capture du snapshot via une coroutine (necessaire pour URP).
        /// Appele par le ProceduralRouteGenerator apres Configure().
        /// </summary>
        public void CaptureSnapshot()
        {
            if (!spotChosen)
            {
                Debug.LogWarning($"[PositionalScanPuzzleStep:{name}] CaptureSnapshot appele sans spot configure.");
                return;
            }
            StartCoroutine(CaptureSnapshotCoroutine());
        }

        private IEnumerator CaptureSnapshotCoroutine()
        {
            // Attendre la fin de la frame pour que la scene soit entierement chargee
            yield return new WaitForEndOfFrame();

            // Camera temporaire avec composant URP
            var camGO = new GameObject("SnapshotCam_Temp");
            var cam = camGO.AddComponent<Camera>();
            var urpData = camGO.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderType = CameraRenderType.Base;

            cam.enabled = false;
            cam.fieldOfView = snapshotFOV;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.cullingMask = ~0;

            // Position : spot + offset vertical pour simuler les yeux
            Vector3 eyePos = chosenSpot.position + Vector3.up * snapshotEyeOffset;
            camGO.transform.position = eyePos;
            camGO.transform.LookAt(transform.position);

            // Render dans une RenderTexture
            var rt = RenderTexture.GetTemporary(snapshotWidth, snapshotHeight, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            // Lire les pixels dans une Texture2D
            RenderTexture.active = rt;
            var tex = new Texture2D(snapshotWidth, snapshotHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, snapshotWidth, snapshotHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // Nettoyage camera + RT
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(camGO);

            // Conversion en Sprite
            snapshot = Sprite.Create(tex,
                new Rect(0, 0, snapshotWidth, snapshotHeight),
                new Vector2(0.5f, 0.5f));

            // Sauvegarde debug en PNG (editeur uniquement)
#if UNITY_EDITOR
            string debugDir = Application.dataPath + "/_Debug";
            if (!System.IO.Directory.Exists(debugDir))
                System.IO.Directory.CreateDirectory(debugDir);

            string safeName = name.Replace(" ", "_").Replace("/", "_");
            string path = debugDir + "/Snapshot_" + safeName + ".png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[PositionalScanPuzzleStep:{name}] Snapshot debug sauvegarde : {path}");
#endif

            if (verboseLogs)
                Debug.Log($"[PositionalScanPuzzleStep:{name}] Snapshot capture ({snapshotWidth}x{snapshotHeight}) depuis {eyePos} vers {transform.position}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private bool IsPlayerInZone()
        {
            if (playerTransform == null) return false;
            Vector3 p = playerTransform.position;
            Vector3 t = chosenSpot.position;
            float dx = p.x - t.x;
            float dz = p.z - t.z;
            float dy = Mathf.Abs(p.y - t.y);
            return (dx * dx + dz * dz) <= horizontalTolerance * horizontalTolerance
                && dy <= verticalTolerance;
        }

        private bool IsPlayerAimingAtMe()
        {
            if (cameraTransform == null) return false;
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return false;
            return hit.collider != null && hit.collider.transform.IsChildOf(transform);
        }

        private float DistanceToSpot()
        {
            if (playerTransform == null || !spotChosen) return -1f;
            Vector3 p = playerTransform.position;
            Vector3 t = chosenSpot.position;
            float dx = p.x - t.x;
            float dz = p.z - t.z;
            float dy = p.y - t.y;
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private void ApplyEmissive(Color color)
        {
            if (feedbackRenderer == null || mpb == null) return;
            feedbackRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColorId, color);
            feedbackRenderer.SetPropertyBlock(mpb);
        }

        private void OnDrawGizmos()
        {
            if (!drawRuntimeGizmos || !Application.isPlaying || !spotChosen) return;

            // 3 etats :
            //   rouge  -> joueur pas sur le spot
            //   jaune  -> sur le spot mais ne vise pas le cube
            //   vert   -> sur le spot ET vise le cube (pret a valider)
            Color baseColor;
            if (wasInZone)        baseColor = Color.green;
            else if (wasOnSpot)   baseColor = Color.yellow;
            else                  baseColor = Color.red;

            // Sphere de rayon = horizontalTolerance pour visualiser la tolerance XZ
            Gizmos.color = baseColor;
            Gizmos.DrawWireSphere(chosenSpot.position, horizontalTolerance);

            // Petite croix au centre + segment vertical pour la tolerance Y
            float k = 0.15f;
            Gizmos.DrawLine(chosenSpot.position + Vector3.left  * k, chosenSpot.position + Vector3.right   * k);
            Gizmos.DrawLine(chosenSpot.position + Vector3.forward * k, chosenSpot.position + Vector3.back  * k);
            Gizmos.DrawLine(chosenSpot.position + Vector3.up    * verticalTolerance, chosenSpot.position + Vector3.down * verticalTolerance);

            // Ligne joueur -> spot, meme code couleur que la sphere
            if (playerTransform != null)
            {
                Gizmos.color = baseColor;
                Gizmos.DrawLine(playerTransform.position, chosenSpot.position);
            }

            // Ligne cube -> spot pour reperer la cible visuellement depuis le bloc
            Gizmos.color = baseColor;
            Gizmos.DrawLine(transform.position, chosenSpot.position);
        }
    }
}
