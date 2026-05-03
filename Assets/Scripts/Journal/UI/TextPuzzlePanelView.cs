using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Panneau HUD pour les enigmes textuelles. Deux phases :
    /// 1. Shown (hover) : affiche la question en lecture seule
    /// 2. Interact (scan) : active le champ de saisie, immobilise la camera
    /// </summary>
    public class TextPuzzlePanelView : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Racine du panneau a activer/desactiver.")]
        public GameObject panelRoot;

        [Tooltip("Label pour la question.")]
        public TMP_Text questionLabel;

        [Tooltip("Champ de saisie pour la reponse (desactive en phase Shown).")]
        public TMP_InputField answerInput;

        [Tooltip("Texte affiche quand la reponse est fausse.")]
        public TMP_Text feedbackLabel;

        [Header("Fermeture")]
        [Tooltip("Touche pour annuler l'interaction et refermer le panneau.")]
        public Key cancelKey = Key.Escape;

        private TextPuzzleStep activeStep;
        private bool isInteracting = false;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            RouteEvents.TextPuzzleShown += HandleShown;
            RouteEvents.TextPuzzleInteract += HandleInteract;
            RouteEvents.TextPuzzleClosed += HandleClosed;
        }

        private void OnDestroy()
        {
            RouteEvents.TextPuzzleShown -= HandleShown;
            RouteEvents.TextPuzzleInteract -= HandleInteract;
            RouteEvents.TextPuzzleClosed -= HandleClosed;
        }

        private void HandleShown(string question, StepBehaviour step)
        {
            activeStep = step as TextPuzzleStep;
            if (activeStep == null) return;

            panelRoot.SetActive(true);
            if (questionLabel != null) questionLabel.text = question;
            if (feedbackLabel != null) feedbackLabel.text = "";

            if (answerInput != null)
            {
                answerInput.text = "";
                answerInput.gameObject.SetActive(false);
            }

            isInteracting = false;
        }

        private void HandleInteract(string question, StepBehaviour step)
        {
            activeStep = step as TextPuzzleStep;
            if (activeStep == null) return;

            panelRoot.SetActive(true);
            if (questionLabel != null) questionLabel.text = question;
            if (feedbackLabel != null) feedbackLabel.text = "";

            if (answerInput != null)
            {
                answerInput.gameObject.SetActive(true);
                answerInput.text = "";
                answerInput.ActivateInputField();
                answerInput.Select();
            }

            isInteracting = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandleClosed()
        {
            panelRoot.SetActive(false);
            activeStep = null;

            if (isInteracting)
            {
                isInteracting = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (activeStep == null || !panelRoot.activeSelf) return;

            if (isInteracting)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                    SubmitAnswer();

                if (Keyboard.current != null && Keyboard.current[cancelKey].wasPressedThisFrame)
                    activeStep.CancelInteraction();
            }
        }

        public void SubmitAnswer()
        {
            if (activeStep == null || answerInput == null) return;

            bool correct = activeStep.TryAnswer(answerInput.text);

            if (!correct && feedbackLabel != null)
            {
                feedbackLabel.text = "Mauvaise reponse, essayez encore.";
                answerInput.text = "";
                answerInput.ActivateInputField();
            }
        }
    }
}
