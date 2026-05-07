using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;
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

        [Header("Input")]
        [Tooltip("InputActionAsset contenant la map 'Game' avec l'action Select.")]
        public InputActionAsset actions;

        [Header("Fermeture")]
        [Tooltip("Touche pour annuler l'interaction et refermer le panneau.")]
        public Key cancelKey = Key.Escape;

        private TextPuzzleStep activeStep;
        private bool isInteracting = false;
        private bool inputFieldActive = false;
        private InputAction selectAction;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            RouteEvents.TextPuzzleShown += HandleShown;
            RouteEvents.TextPuzzleInteract += HandleInteract;
            RouteEvents.TextPuzzleClosed += HandleClosed;

            if (actions != null)
            {
                var gameMap = actions.FindActionMap("Game");
                if (gameMap != null)
                {
                    selectAction = gameMap.FindAction("Select");
                    gameMap.Enable();
                }
            }
        }

        private void OnDestroy()
        {
            RouteEvents.TextPuzzleShown -= HandleShown;
            RouteEvents.TextPuzzleInteract -= HandleInteract;
            RouteEvents.TextPuzzleClosed -= HandleClosed;
        }

        private bool shownOnly = false;

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
            shownOnly = true;
            UIState.SetUIOpen();
        }

        private void HandleInteract(string question, StepBehaviour step)
        {
            activeStep = step as TextPuzzleStep;
            if (activeStep == null) return;

            panelRoot.SetActive(true);
            if (questionLabel != null) questionLabel.text = question;
            if (feedbackLabel != null) feedbackLabel.text = "Appuyez sur Entree pour repondre.";

            // Le champ de saisie est visible mais INACTIF.
            // Le joueur doit appuyer sur Enter (Select) pour commencer a ecrire.
            if (answerInput != null)
            {
                answerInput.gameObject.SetActive(true);
                answerInput.text = "";
                answerInput.DeactivateInputField();
            }

            isInteracting = true;
            inputFieldActive = false;
            // SetUIOpen deja appele par HandleShown, pas de double comptage
            if (!shownOnly) UIState.SetUIOpen();
            shownOnly = false;
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
                inputFieldActive = false;
                UIState.IsInputFieldActive = false;
                UIState.SetUIClosed();
                if (!UIState.IsAnyUIOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else if (shownOnly)
            {
                // Fermeture depuis la phase Shown (hover exit sans interact)
                UIState.SetUIClosed();
            }

            shownOnly = false;
        }

        private void Update()
        {
            if (activeStep == null || !panelRoot.activeSelf) return;

            if (isInteracting)
            {
                // Escape ferme le panneau
                if (Keyboard.current != null && Keyboard.current[cancelKey].wasPressedThisFrame)
                {
                    activeStep.CancelInteraction();
                    return;
                }

                bool selectPressed = selectAction != null
                    ? selectAction.WasPressedThisFrame()
                    : (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame);

                if (selectPressed)
                {
                    if (!inputFieldActive)
                    {
                        // Premier Enter : active le champ de saisie
                        inputFieldActive = true;
                        UIState.IsInputFieldActive = true;
                        if (answerInput != null)
                        {
                            answerInput.ActivateInputField();
                            answerInput.Select();
                        }
                        if (feedbackLabel != null) feedbackLabel.text = "";
                    }
                    else
                    {
                        // Enter suivant : soumet la reponse
                        SubmitAnswer();
                    }
                }
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
