using UnityEngine;
using EscapeGame.Routes.Events;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Étape de type Énigme. Classe abstraite : chaque énigme concrète
    /// (audiovisuelle, textuelle, spatiale, …) hérite de cette base.
    /// Successeur direct de l'ancien <c>PuzzleBase</c>.
    /// </summary>
    public abstract class PuzzleStep : StepBehaviour
    {
        // Les enigmes se revelent (mesh visible) quand on les scanne. Les
        // sous-classes qui ne doivent PAS se reveler (radio, scan positionnel)
        // redefinissent ceci a false.
        protected override bool RevealMeshOnScan => true;

        public override void OnHover()
        {
            base.OnHover();
            // Feedback visuel par défaut (à surcharger dans les sous-classes).
        }

        public override void OnScan()
        {
            if (IsInteractionBlocked()) return;
            base.OnScan();

            // Revelation du mesh de l'enigme (reste visible jusqu'a resolution).
            if (RevealMeshOnScan && !IsResolved)
                ShowMesh();

            // Re-affiche l'enonce de l'enigme a chaque scan tant qu'elle n'est pas resolue.
            if (!IsResolved && stepData != null && stepData.initialClue != null
                && !stepData.initialClue.IsEmpty)
            {
                RouteEvents.RaiseClueRevealed(stepData.initialClue, this);
            }
        }

        public override void OnHoverExit()
        {
            base.OnHoverExit();
            // Annule tout feedback visuel mis en place par OnHover.
        }
    }
}
