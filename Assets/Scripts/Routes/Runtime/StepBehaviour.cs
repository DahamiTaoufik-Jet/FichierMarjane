using UnityEngine;
using UnityEngine.Events;
using EscapeGame.Core.Interfaces;
using EscapeGame.Routes.Data;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Base unifiée des étapes de jeu. Implémente <see cref="IScannable"/> pour
    /// se brancher directement sur le <c>PlayerScanner</c> existant.
    /// Les sous-classes (<see cref="ClueStep"/>, <see cref="PuzzleStep"/>) définissent
    /// la mécanique de résolution propre à chaque type d'étape.
    /// </summary>
    public abstract class StepBehaviour : MonoBehaviour, IScannable
    {
        [Header("Step Binding")]
        [Tooltip("ScriptableObject décrivant cette étape (type, placement, récompense, …).")]
        public StepData stepData;

        [Tooltip("Identifiant de la route à laquelle appartient cette étape. Renseigné " +
                 "soit manuellement, soit par le LevelGenerator lors de l'instanciation.")]
        public string routeId;

        [Header("Events")]
        [Tooltip("Déclenché lorsque l'étape passe de Locked à Discovered.")]
        public UnityEvent OnDiscovered;

        [Tooltip("Déclenché lorsque l'étape passe à Resolved.")]
        public UnityEvent OnResolved;

        protected StepState currentState = StepState.Locked;

        public StepState CurrentState => currentState;
        public bool IsLocked     => currentState == StepState.Locked;
        public bool IsDiscovered => currentState >= StepState.Discovered;
        public bool IsResolved   => currentState == StepState.Resolved;

        // ========= IScannable =========

        /// <summary>
        /// Feedback continu (outline, hint UI, gauge de gaze…).
        /// Surchargeable par les sous-classes.
        /// </summary>
        public virtual void OnHover() { }

        /// <summary>
        /// Validation de l'interaction. Comportement de base :
        /// si l'étape est verrouillée, le scan la fait passer en Discovered.
        /// Les sous-classes ajoutent leur logique (auto-résolution pour
        /// <see cref="ClueStep"/>, vérification pour <see cref="PuzzleStep"/>, etc.).
        /// </summary>
        public virtual void OnScan()
        {
            if (currentState == StepState.Locked)
                Discover();
        }

        /// <summary>
        /// Révèle l'étape sans la résoudre (utilisé quand l'étape précédente
        /// vient d'être résolue : l'indice initial est lu et ce step devient visible).
        /// </summary>
        public virtual void Reveal()
        {
            if (currentState == StepState.Locked)
                Discover();
        }

        public virtual void OnHoverExit() { }

        // ========= Transitions d'état =========

        /// <summary>
        /// Passe l'étape de Locked à Discovered.
        /// </summary>
        public virtual void Discover()
        {
            if (currentState != StepState.Locked) return;
            currentState = StepState.Discovered;
            OnDiscovered?.Invoke();
        }

        /// <summary>
        /// Force la résolution (utilisé par les bonus type "Resolver"
        /// — pendant logique de <c>PuzzleBase.ForceResolve</c>).
        /// </summary>
        public void ForceResolve()
        {
            if (IsResolved) return;
            ResolveStep();
        }

        /// <summary>
        /// Implémentation interne de la résolution. À appeler depuis les sous-classes
        /// quand leurs conditions de résolution sont remplies.
        /// </summary>
        protected virtual void ResolveStep()
        {
            if (IsResolved) return;
            currentState = StepState.Resolved;
            OnResolved?.Invoke();
        }
    }
}
