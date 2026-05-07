using UnityEngine;
using EscapeGame.Core.Interfaces;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Handles the "Scan" mechanic from a First Person view.
    /// Distinguishes OnHover() continuous feedback from OnScan() validation.
    ///
    /// Deux rays independants :
    ///  - Ray principal : detecte tous les IScannable SAUF PositionalScanPuzzleStep
    ///  - Ray positionnel : detecte UNIQUEMENT les PositionalScanPuzzleStep,
    ///    avec sa propre portee (<see cref="positionalScanRange"/>)
    /// </summary>
    public class PlayerScanner : MonoBehaviour
    {
        [Header("Scanner principal")]
        [Tooltip("Portee du ray principal (indices, puzzles texte, radios...).")]
        public float scanEffectiveRange = 8f;
        public float scanRadius = 0.5f; // Obsolete with Raycast scan.
        public LayerMask scannableLayer;

        [Header("Scanner positionnel (PositionalScan)")]
        [Tooltip("Portee du ray dedie aux PositionalScanPuzzleStep. Permet de voir les cubes de loin.")]
        public float positionalScanRange = 30f;
        [Tooltip("Layer des PositionalScanPuzzleStep. Si None, utilise scannableLayer.")]
        public LayerMask positionalScanLayer;

        [Header("References")]
        public Camera fpsCamera;
        [Tooltip("Touche d'interaction (New Input System)")]
        public UnityEngine.InputSystem.Key scanKey = UnityEngine.InputSystem.Key.E;

        // Tracks the object currently in the center of the screen
        private IScannable currentTarget;
        private IScannable currentPositionalTarget;

        private void Update()
        {
            // Bloque le scan quand une UI est ouverte
            if (UIState.IsAnyUIOpen)
            {
                ClearTarget();
                ClearPositionalTarget();
                return;
            }

            PerformScreenCenterScan();
            PerformPositionalScan();
        }

        // ====================================================================
        // Ray principal : tout sauf PositionalScanPuzzleStep
        // ====================================================================

        private void PerformScreenCenterScan()
        {
            if (fpsCamera == null) return;

            Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, scanEffectiveRange, scannableLayer))
            {
                IScannable detectedScannable = hitInfo.collider.GetComponentInParent<IScannable>();

                // Ignorer les PositionalScanPuzzleStep — geres par le ray dedie
                if (detectedScannable is PositionalScanPuzzleStep)
                    detectedScannable = null;

                if (detectedScannable != null)
                {
                    if (currentTarget != detectedScannable)
                    {
                        if (currentTarget != null) currentTarget.OnHoverExit();
                        currentTarget = detectedScannable;
                    }

                    currentTarget.OnHover();

                    if (WasScanInputPressed())
                        currentTarget.OnScan();
                }
                else
                {
                    ClearTarget();
                }
            }
            else
            {
                ClearTarget();
            }
        }

        // ====================================================================
        // Ray positionnel : uniquement PositionalScanPuzzleStep
        // ====================================================================

        private void PerformPositionalScan()
        {
            if (fpsCamera == null) return;

            LayerMask mask = positionalScanLayer != 0 ? positionalScanLayer : scannableLayer;
            Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, positionalScanRange, mask))
            {
                var positional = hitInfo.collider.GetComponentInParent<PositionalScanPuzzleStep>();

                if (positional != null)
                {
                    IScannable detectedScannable = positional;

                    if (currentPositionalTarget != detectedScannable)
                    {
                        if (currentPositionalTarget != null) currentPositionalTarget.OnHoverExit();
                        currentPositionalTarget = detectedScannable;
                    }

                    currentPositionalTarget.OnHover();

                    if (WasScanInputPressed())
                        currentPositionalTarget.OnScan();
                }
                else
                {
                    ClearPositionalTarget();
                }
            }
            else
            {
                ClearPositionalTarget();
            }
        }

        // ====================================================================
        // Input
        // ====================================================================

        private bool WasScanInputPressed()
        {
            bool mouseClicked = UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            bool keyPressed = UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current[scanKey].wasPressedThisFrame;
            return mouseClicked || keyPressed;
        }

        // ====================================================================
        // Cleanup
        // ====================================================================

        public void ClearTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.OnHoverExit();
                currentTarget = null;
            }
        }

        private void ClearPositionalTarget()
        {
            if (currentPositionalTarget != null)
            {
                currentPositionalTarget.OnHoverExit();
                currentPositionalTarget = null;
            }
        }

        private void OnDisable()
        {
            ClearTarget();
            ClearPositionalTarget();
        }

        private void OnDrawGizmos()
        {
            if (fpsCamera == null) return;

            Vector3 origin = fpsCamera.transform.position;
            Vector3 forward = fpsCamera.transform.forward;

            // Ray principal (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin + forward * scanEffectiveRange, scanRadius);

            // Ray positionnel (jaune)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin + forward * positionalScanRange, 0.3f);
        }
    }
}
