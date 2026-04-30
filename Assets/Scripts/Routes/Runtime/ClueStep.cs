using UnityEngine;
using UnityEngine.Events;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Events;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Étape de type Indice : aucune énigme à résoudre, le scan révèle simplement
    /// le contenu et la marque immédiatement comme résolue.
    /// Successeur direct de l'ancien <c>CluePanel</c>.
    /// </summary>
    public class ClueStep : StepBehaviour
    {
        [Header("Affichage")]
        [Tooltip("Canvas optionnel à activer lors de la révélation.")]
        public Canvas displayCanvas;

        [Tooltip("Événement levé pour permettre à l'UI d'afficher le contenu " +
                 "(texte / image / audio) extrait de StepData.initialClue.")]
        public UnityEvent<ClueContent> OnContentRevealed;

        public override void OnScan()
        {
            // 1. Découverte si nécessaire
            if (currentState == StepState.Locked)
                Discover();

            // 2. Affichage du contenu de cette étape
            DisplayOwnContent();

            // 3. Auto-résolution (un indice est par définition "résolu" dès qu'il est lu)
            if (currentState == StepState.Discovered)
                ResolveStep();
        }

        public override void Discover()
        {
            base.Discover();
            if (displayCanvas != null) displayCanvas.enabled = true;
        }

        protected override void ResolveStep()
        {
            base.ResolveStep();
            // Le RouteManager se chargera de révéler l'indice initial de l'étape
            // suivante en s'abonnant à OnResolved.
        }

        /// <summary>
        /// Affiche le contenu propre à cet indice (texte/image/son du StepData).
        /// </summary>
        private void DisplayOwnContent()
        {
            if (stepData == null || stepData.initialClue == null) return;
            OnContentRevealed?.Invoke(stepData.initialClue);
            if (!stepData.initialClue.IsEmpty)
                RouteEvents.RaiseClueRevealed(stepData.initialClue, this);
        }
    }
}
