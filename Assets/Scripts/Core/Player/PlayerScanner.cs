using UnityEngine;
using EscapeGame.Core.Interfaces;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Handles the "Scan" mechanic from a First Person view.
    /// Distinguishes OnHover() continuous feedback from OnScan() validation.
    /// </summary>
    public class PlayerScanner : MonoBehaviour
    {
        [Header("Scanner Properties")]
        public float scanEffectiveRange = 8f;
        public float scanRadius = 0.5f; // Used to simulate a circular UI center.
        public LayerMask scannableLayer;
        
        [Header("References")]
        public Camera fpsCamera;
        [Tooltip("Touche d'interaction (New Input System)")]
        public UnityEngine.InputSystem.Key scanKey = UnityEngine.InputSystem.Key.E;

        // Tracks the object currently in the center of the screen
        private IScannable currentTarget;

        private void Update()
        {
            // Usually, this logic is enclosed by a condition: if(isFPSMode active)
            PerformScreenCenterScan();
        }

        private void PerformScreenCenterScan()
        {
            if (fpsCamera == null) return;

            // SphereCast from the camera center
            Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);
            
            if (Physics.SphereCast(ray, scanRadius, out RaycastHit hitInfo, scanEffectiveRange, scannableLayer))
            {
                IScannable detectedScannable = hitInfo.collider.GetComponentInParent<IScannable>();

                if (detectedScannable != null)
                {
                    // If target changed
                    if (currentTarget != detectedScannable)
                    {
                        if (currentTarget != null) currentTarget.OnHoverExit();
                        currentTarget = detectedScannable;
                    }

                    // Continuous feedback / Trigger gaze timers (e.g., AudioVisualPuzzle)
                    currentTarget.OnHover();

                    // Validation input triggers the core scan resolution
                    bool mouseClicked = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
                    bool keyPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[scanKey].wasPressedThisFrame;

                    if (keyPressed || mouseClicked)
                    {
                        currentTarget.OnScan();
                    }
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

        public void ClearTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.OnHoverExit();
                currentTarget = null;
            }
        }

        private void OnDisable()
        {
            // S'assure que si on quitte le mode FPS pendant qu'on regardait un objet,
            // l'objet (et ses chronomètres) est bien réinitialisé.
            ClearTarget();
        }

        private void OnDrawGizmos()
        {
            if (fpsCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(fpsCamera.transform.position + fpsCamera.transform.forward * scanEffectiveRange, scanRadius);
            }
        }
    }
}
