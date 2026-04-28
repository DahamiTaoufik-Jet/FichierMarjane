using UnityEngine;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Étape de type Énigme. Classe abstraite : chaque énigme concrète
    /// (audiovisuelle, textuelle, spatiale, …) hérite de cette base.
    /// Successeur direct de l'ancien <c>PuzzleBase</c>.
    /// </summary>
    public abstract class PuzzleStep : StepBehaviour
    {
        public override void OnHover()
        {
            base.OnHover();
            // Feedback visuel par défaut (à surcharger dans les sous-classes).
        }

        public override void OnScan()
        {
            // À la différence d'un ClueStep, le scan ne résout PAS automatiquement
            // l'énigme : il déclenche uniquement la découverte si l'étape était
            // verrouillée. Les sous-classes décident quand appeler ResolveStep().
            base.OnScan();
        }

        public override void OnHoverExit()
        {
            base.OnHoverExit();
            // Annule tout feedback visuel mis en place par OnHover.
        }
    }
}
