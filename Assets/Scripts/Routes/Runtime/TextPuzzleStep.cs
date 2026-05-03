using UnityEngine;
using EscapeGame.Routes.Events;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Enigme textuelle :
    /// - Hover (apres delai) : affiche la question en lecture seule sur le HUD
    /// - Scan (E/clic) : immobilise la camera, active le champ de saisie
    /// - Reponse correcte : resout l'etape
    /// La question et la reponse sont lues depuis le StepData.
    /// </summary>
    public class TextPuzzleStep : PuzzleStep
    {
        [Header("Hover")]
        [Tooltip("Duree de hover continu avant d'afficher la question.")]
        public float hoverDelay = 1f;

        private float hoverTimer = 0f;
        private bool questionShown = false;
        private bool interacting = false;
        private bool deciphered = false;

        private string Question => stepData != null ? stepData.puzzleQuestion : "";
        private string Answer => stepData != null ? stepData.puzzleAnswer : "";
        private bool Encrypted => stepData != null && stepData.puzzleEncrypted;
        private string EncryptedQuestion => stepData != null ? stepData.puzzleEncryptedQuestion : "";

        private string DisplayQuestion =>
            (Encrypted && !deciphered && !string.IsNullOrEmpty(EncryptedQuestion))
                ? EncryptedQuestion
                : Question;

        public override void OnHover()
        {
            base.OnHover();
            if (IsResolved || interacting) return;

            hoverTimer += Time.deltaTime;

            if (hoverTimer >= hoverDelay && !questionShown)
            {
                if (currentState == StepState.Locked)
                    Discover();

                questionShown = true;
                RouteEvents.RaiseTextPuzzleShown(DisplayQuestion, this);
            }
        }

        public override void OnScan()
        {
            if (currentState == StepState.Locked)
                Discover();
            if (IsResolved) return;

            if (!questionShown)
            {
                if (currentState == StepState.Locked)
                    Discover();
                questionShown = true;
            }

            if (!interacting)
            {
                interacting = true;
                RouteEvents.RaiseTextPuzzleInteract(DisplayQuestion, this);
            }
        }

        public override void OnHoverExit()
        {
            base.OnHoverExit();
            hoverTimer = 0f;

            if (!interacting && questionShown)
            {
                questionShown = false;
                RouteEvents.RaiseTextPuzzleClosed();
            }
        }

        public bool TryAnswer(string playerAnswer)
        {
            if (IsResolved) return true;
            if (string.IsNullOrEmpty(playerAnswer) || string.IsNullOrEmpty(Answer)) return false;

            bool correct = string.Equals(
                playerAnswer.Trim(), Answer.Trim(),
                System.StringComparison.OrdinalIgnoreCase);

            if (correct)
            {
                Debug.Log($"[TextPuzzleStep:{name}] Bonne reponse !");
                ClosePanel();
                ResolveStep();
            }
            else
            {
                Debug.Log($"[TextPuzzleStep:{name}] Mauvaise reponse : '{playerAnswer}'");
            }

            return correct;
        }

        public void CancelInteraction()
        {
            if (!interacting) return;
            interacting = false;
            questionShown = false;
            RouteEvents.RaiseTextPuzzleClosed();
        }

        public void Decipher()
        {
            deciphered = true;
            if (questionShown || interacting)
            {
                RouteEvents.RaiseTextPuzzleClosed();
                questionShown = false;
                interacting = false;
            }
        }

        private void ClosePanel()
        {
            questionShown = false;
            interacting = false;
            RouteEvents.RaiseTextPuzzleClosed();
        }

        protected override void ResolveStep()
        {
            base.ResolveStep();
            ClosePanel();
        }
    }
}
