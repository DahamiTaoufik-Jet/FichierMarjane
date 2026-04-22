using UnityEngine;

namespace EscapeGame.Core.Interfaces
{
    /// <summary>
    /// Interface for any object that can be interacted with via the Player's Scanner.
    /// </summary>
    public interface IScannable
    {
        /// <summary>
        /// Called continuously every frame the object is centered in the scanner.
        /// Useful for visual feedbacks (outlines, UI hints) or gaze-based timers.
        /// </summary>
        void OnHover();

        /// <summary>
        /// Called exclusively to validate an interaction (button click/validation).
        /// </summary>
        void OnScan();

        /// <summary>
        /// Called to reveal the object permanently if it was hidden.
        /// </summary>
        void Reveal();

        /// <summary>
        /// Called when the scanner leaves the object.
        /// </summary>
        void OnHoverExit();
    }
}
