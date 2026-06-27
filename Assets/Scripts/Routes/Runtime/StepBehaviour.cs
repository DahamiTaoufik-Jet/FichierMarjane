using UnityEngine;
using UnityEngine.Events;
using EscapeGame.Core.Interfaces;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Services;

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

        // ========= Garde-fou skip =========

        protected bool IsInteractionBlocked()
        {
            return RouteManager.Instance != null && !RouteManager.Instance.CanInteract(this);
        }

        // ========= Visibilite du mesh =========

        /// <summary>
        /// Si true, le mesh de cette etape se revele (reste visible jusqu'a
        /// resolution) quand le joueur la scanne. Faux par defaut : les indices
        /// et certaines enigmes (radio, scan positionnel) restent invisibles.
        /// </summary>
        protected virtual bool RevealMeshOnScan => false;

        /// <summary>Masque le mesh (MeshRenderer/SkinnedMeshRenderer) sans toucher aux colliders.</summary>
        public void HideMesh() => SetMeshVisible(false);

        /// <summary>Affiche le mesh (utilise lors de la revelation d'une enigme).</summary>
        protected void ShowMesh() => SetMeshVisible(true);

        private void SetMeshVisible(bool visible)
        {
            var meshes = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
                if (meshes[i] != null) meshes[i].enabled = visible;

            var skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
                if (skinned[i] != null) skinned[i].enabled = visible;
        }

        // ========= IScannable =========

        public virtual void OnHover() { }

        public virtual void OnScan()
        {
            if (IsInteractionBlocked()) return;
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
            if (IsLocked) Discover();
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
            HideVisual();
            OnResolved?.Invoke();
        }

        protected void HideVisual()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;
            var col = GetComponentInChildren<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }
}
