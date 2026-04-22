using UnityEngine;
using EscapeGame.Core.Interfaces;

namespace EscapeGame.Interactables.Clues
{
    /// <summary>
    /// Clue panel that displays information or ciphers for a specific puzzle.
    /// </summary>
    public class CluePanel : MonoBehaviour, IScannable
    {
        [Header("Clue Details")]
        public string pairID;
        [TextArea]
        public string clueText;
        
        private bool isRevealed = false;

        public void OnHover()
        {
            // Give subtle feedback, maybe an outline if hidden.
        }

        public void OnScan()
        {
            if (!isRevealed)
            {
                Reveal();
            }
        }

        public void Reveal()
        {
            isRevealed = true;
            // E.g., enable the canvas, display text on standard UI
            Debug.Log($"[CluePanel] Revealed Clue for pair {pairID} : {clueText}");
        }

        public void OnHoverExit()
        {
            // Disable subtle feedback
        }

        /// <summary>
        /// Global event listener bound dynamically. Disables the clue when its puzzle is solved.
        /// </summary>
        public void HandlePuzzleResolved()
        {
            Debug.Log($"[CluePanel] Related puzzle {pairID} solved. Disabling clue panel.");
            gameObject.SetActive(false);
        }
    }
}
