using UnityEngine;
using UnityEngine.Events;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Events;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Etape de type Indice : le joueur doit hover le cube pendant un delai
    /// pour reveler le contenu. L'indice s'affiche sur le HUD (CluePanelView)
    /// et l'etape est auto-resolue a la premiere revelation.
    /// </summary>
    public class ClueStep : StepBehaviour
    {
        [Header("Hover")]
        [Tooltip("Duree de hover continu (en secondes) avant de reveler l'indice.")]
        public float hoverDelay = 1f;

        [Header("Affichage")]
        [Tooltip("Evenement leve pour permettre a l'UI d'afficher le contenu.")]
        public UnityEvent<ClueContent> OnContentRevealed;

        private float hoverTimer = 0f;
        private bool isHovering = false;
        private bool contentShown = false;

        public override void OnHover()
        {
            base.OnHover();
            if (IsResolved && contentShown) return;

            isHovering = true;
            hoverTimer += Time.deltaTime;

            if (hoverTimer >= hoverDelay && !contentShown)
            {
                if (currentState == StepState.Locked)
                    Discover();

                DisplayOwnContent();
                contentShown = true;

                if (currentState == StepState.Discovered)
                    ResolveStep();
            }
        }

        public override void OnHoverExit()
        {
            base.OnHoverExit();
            isHovering = false;
            hoverTimer = 0f;

            if (contentShown)
            {
                contentShown = false;
                RouteEvents.RaiseClueHidden();
            }
        }

        public override void OnScan()
        {
            if (currentState == StepState.Locked)
                Discover();

            DisplayOwnContent();
            contentShown = true;

            if (currentState == StepState.Discovered)
                ResolveStep();
        }

        private void DisplayOwnContent()
        {
            if (stepData == null || stepData.initialClue == null) return;
            OnContentRevealed?.Invoke(stepData.initialClue);
            if (!stepData.initialClue.IsEmpty)
                RouteEvents.RaiseClueRevealed(stepData.initialClue, this);
        }
    }
}
