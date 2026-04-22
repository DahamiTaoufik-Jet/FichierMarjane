using UnityEngine;
using EscapeGame.Core.Player;

namespace EscapeGame.Interactables.Puzzles
{
    /// <summary>
    /// Puzzle requiring the player to maintain gaze (FPS scan) continuously 
    /// while being within a close proximity to the audio source.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioVisualPuzzle : PuzzleBase
    {
        [Header("AudioVisual Conditions")]
        [Tooltip("Seconds required to maintain uninterrupted gaze.")]
        public float requiredLookDuration = 3f;
        
        [Tooltip("Max distance allowed to the center of the audio source.")]
        public float maxValidationDistance = 2f;
        
        private float currentGazeTimer = 0f;
        private bool isGazingThisFrame = false;
        private Transform playerTransform;

        private void Start()
        {
            // Usually injected or found via a Game/Player Manager.
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (isResolved || playerTransform == null) return;

            // Timer increments only if OnHover() was called indicating the player is looking
            if (isGazingThisFrame)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                
                if (distanceToPlayer <= maxValidationDistance)
                {
                    currentGazeTimer += Time.deltaTime;
                    
                    // Provides option to auto-resolve or allow OnScan to resolve when ready
                    if (currentGazeTimer >= requiredLookDuration)
                    {
                        // Conditions strictes validées
                        Debug.Log("[AudioVisual] Conditions méticuleusement remplies ! Prêt pour OnScan()");
                    }
                }
                else
                {
                    // Look unbroken, but distance constraint failed
                    currentGazeTimer = 0f;
                }
                
                // Flag is reset ensuring OnHover MUST be called next frame
                isGazingThisFrame = false;
            }
            else
            {
                // Uninterrupted look broken
                currentGazeTimer = 0f;
            }
        }

        public override void OnHover()
        {
            base.OnHover();
            if (isResolved) return;
            
            // Player Scanner is pointing at this object
            isGazingThisFrame = true;
        }

        public override void OnScan()
        {
            base.OnScan();
            if (isResolved) return;

            // OnScan() valids the interaction based on the background condition logic
            if (currentGazeTimer >= requiredLookDuration)
            {
                ResolvePuzzle();
            }
            else
            {
                Debug.Log($"[AudioVisual] Impossible de valider. Regardez continument ({currentGazeTimer:F1}/{requiredLookDuration} sec) à moins de {maxValidationDistance} unités.");
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
