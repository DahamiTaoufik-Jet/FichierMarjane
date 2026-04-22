using UnityEngine;
using UnityEngine.Events;
using EscapeGame.Core.Interfaces;

namespace EscapeGame.Interactables.Puzzles
{
    /// <summary>
    /// Base class for all procedural puzzles in the game.
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour, IScannable
    {
        [Header("Puzzle Registration")]
        [Tooltip("Dynamically assigned by LevelGenerator to link with a Clue.")]
        public string pairID;
        
        [Tooltip("Event fired when the puzzle is successfully resolved.")]
        public UnityEvent OnResolved;

        protected bool isResolved = false;

        public virtual void OnHover()
        {
            // By default, apply visual feedback when the scanner points at the puzzle.
        }

        public virtual void OnScan()
        {
            // Overridden by child classes (Text, Spatial, etc.) for interaction logic.
        }

        public virtual void Reveal()
        {
            // Action to unfold or show the puzzle if it was hidden.
        }

        public virtual void OnHoverExit()
        {
            // Revert any visual changes made by OnHover.
        }

        /// <summary>
        /// Can be called by the resolver bonus to bypass logic.
        /// </summary>
        public void ForceResolve()
        {
            if (isResolved) return;
            ResolvePuzzle();
        }

        /// <summary>
        /// Internal implementation to mark the puzzle complete and fire events.
        /// </summary>
        protected virtual void ResolvePuzzle()
        {
            isResolved = true;
            OnResolved?.Invoke();
            Debug.Log($"[PuzzleBase] Puzzle {gameObject.name} with pairID {pairID} has been resolved!");
        }
    }
}
