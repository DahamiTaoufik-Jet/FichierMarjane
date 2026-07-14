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

        [Header("Style")]
        [Tooltip("Fond gris du panneau d'enigme (texte blanc centre).")]
        public Color textBackgroundColor = new Color(0.25f, 0.25f, 0.27f, 1f);

        [Tooltip("Couleur du texte de l'enigme (question / feedback).")]
        public Color textColor = Color.white;

        private TextPuzzleStep activeStep;
        private bool isInteracting = false;
        private bool inputFieldActive = false;
        private InputAction selectAction;
        private bool clickListenerAdded = false;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;

            ApplyStyle();

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

        // Fond gris + texte blanc centre (l'enigme est toujours du texte).
        private void ApplyStyle()
        {
            var bg = panelRoot != null ? panelRoot.GetComponent<UnityEngine.UI.Image>() : null;
            if (bg != null) bg.color = textBackgroundColor;

            // Question : large boite dans le HAUT du panneau (au-dessus du champ),
            // texte blanc centre.
            if (questionLabel != null)
            {
                questionLabel.color = textColor;
                questionLabel.alignment = TMPro.TextAlignmentOptions.Center;
                var rt = questionLabel.rectTransform;
                rt.anchorMin = new Vector2(0.05f, 0.60f);
                rt.anchorMax = new Vector2(0.95f, 0.97f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            // Feedback : large boite dans le BAS du panneau (sous le champ), centre.
            if (feedbackLabel != null)
            {
                feedbackLabel.color = textColor;
                feedbackLabel.alignment = TMPro.TextAlignmentOptions.Center;
                var rt = feedbackLabel.rectTransform;
                rt.anchorMin = new Vector2(0.05f, 0.05f);
                rt.anchorMax = new Vector2(0.95f, 0.42f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            // Le champ de saisie garde son fond blanc et son texte noir (lisible) :
            // on centre juste la saisie.
            if (answerInput != null && answerInput.textComponent != null)
                answerInput.textComponent.alignment = TMPro.TextAlignmentOptions.Center;
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

                if (!clickListenerAdded)
                {
                    clickListenerAdded = true;
                    answerInput.onSelect.AddListener(OnInputFieldClicked);
                }
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

        private void OnInputFieldClicked(string _)
        {
            if (!isInteracting || inputFieldActive) return;
            inputFieldActive = true;
            UIState.IsInputFieldActive = true;
            if (feedbackLabel != null) feedbackLabel.text = "";
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
