using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

// UnityEngine.UIElements definit aussi un type Cursor : on leve l'ambiguite.
using Cursor = UnityEngine.Cursor;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Version UI Toolkit du HUD des enigmes textuelles. Remplace
    /// <c>TextPuzzlePanelView</c>. Deux phases, comme avant :
    ///   1. Shown (survol)   : la question s'affiche en lecture seule
    ///   2. Interact (scan)  : le champ apparait ; un premier Entree y donne le
    ///                         focus, le suivant soumet la reponse
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TextPuzzleDocument : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("InputActionAsset contenant la map 'Game' avec l'action Select.")]
        public InputActionAsset actions;

        [Tooltip("Touche qui annule l'interaction et referme le panneau.")]
        public Key cancelKey = Key.Escape;

        private VisualElement screen;
        private Label question;
        private TextField answer;
        private Label feedback;

        private TextPuzzleStep activeStep;
        private InputAction selectAction;

        private bool isInteracting;
        private bool inputFieldActive;
        private bool shownOnly;
        private bool pendingCursorLock;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            screen = root.Q<VisualElement>("puzzle-screen");
            question = root.Q<Label>("puzzle-question");
            answer = root.Q<TextField>("puzzle-answer");
            feedback = root.Q<Label>("puzzle-feedback");

            if (answer != null)
            {
                // Cliquer dans le champ vaut activation, comme dans l'ancienne version.
                answer.RegisterCallback<FocusInEvent>(OnFieldFocusIn);
                answer.RegisterCallback<FocusOutEvent>(OnFieldFocusOut);
            }

            ResolveActions();
            SetVisible(false);

            RouteEvents.TextPuzzleShown += HandleShown;
            RouteEvents.TextPuzzleInteract += HandleInteract;
            RouteEvents.TextPuzzleClosed += HandleClosed;
        }

        private void OnDisable()
        {
            RouteEvents.TextPuzzleShown -= HandleShown;
            RouteEvents.TextPuzzleInteract -= HandleInteract;
            RouteEvents.TextPuzzleClosed -= HandleClosed;

            // Panneau ouvert lors d'un changement de scene : rendre les jetons
            // UIState, sinon les inputs restent bloques.
            ReleaseUIState();
        }

        private void ResolveActions()
        {
            if (actions == null) return;
            var gameMap = actions.FindActionMap("Game");
            if (gameMap == null) return;
            selectAction = gameMap.FindAction("Select");
            gameMap.Enable();
        }

        // ====================================================================
        // Evenements de l'enigme
        // ====================================================================

        private void HandleShown(string q, StepBehaviour step)
        {
            activeStep = step as TextPuzzleStep;
            if (activeStep == null) return;

            SetVisible(true);
            if (question != null) question.text = q;
            SetFeedback("", false);

            if (answer != null)
            {
                answer.SetValueWithoutNotify("");
                answer.AddToClassList("hidden");
            }

            isInteracting = false;
            shownOnly = true;
            UIState.SetUIOpen();
        }

        private void HandleInteract(string q, StepBehaviour step)
        {
            activeStep = step as TextPuzzleStep;
            if (activeStep == null) return;

            SetVisible(true);
            if (question != null) question.text = q;
            SetFeedback("Appuyez sur Entree pour repondre.", false);

            // Le champ devient visible mais ne prend PAS le focus : c'est le
            // premier Entree qui le donne, comme dans l'ancienne version.
            if (answer != null)
            {
                answer.SetValueWithoutNotify("");
                answer.RemoveFromClassList("hidden");
            }

            isInteracting = true;
            inputFieldActive = false;

            // SetUIOpen deja appele par HandleShown : pas de double comptage.
            if (!shownOnly) UIState.SetUIOpen();
            shownOnly = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandleClosed()
        {
            SetVisible(false);
            activeStep = null;
            ReleaseUIState();
        }

        /// <summary>Rend les jetons UIState detenus, quelle que soit la phase atteinte.</summary>
        private void ReleaseUIState()
        {
            if (isInteracting)
            {
                isInteracting = false;
                inputFieldActive = false;
                UIState.IsInputFieldActive = false;
                UIState.SetUIClosed();
                pendingCursorLock = true;
            }
            else if (shownOnly)
            {
                UIState.SetUIClosed();
            }
            shownOnly = false;
        }

        // ====================================================================
        // Input
        // ====================================================================

        private void Update()
        {
            if (activeStep == null || !isInteracting) return;

            var kb = Keyboard.current;
            if (kb != null && kb[cancelKey].wasPressedThisFrame)
            {
                activeStep.CancelInteraction();
                return;
            }

            bool selectPressed = selectAction != null
                ? selectAction.WasPressedThisFrame()
                : (kb != null && kb.enterKey.wasPressedThisFrame);

            if (!selectPressed) return;

            if (!inputFieldActive)
            {
                // Premier Entree : donner le focus au champ.
                inputFieldActive = true;
                UIState.IsInputFieldActive = true;
                if (answer != null) answer.Focus();
                SetFeedback("", false);
            }
            else
            {
                SubmitAnswer();
            }
        }

        private void LateUpdate()
        {
            if (!pendingCursorLock) return;
            pendingCursorLock = false;

            if (UIState.IsAnyUIOpen) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnFieldFocusIn(FocusInEvent evt)
        {
            if (!isInteracting || inputFieldActive) return;
            inputFieldActive = true;
            UIState.IsInputFieldActive = true;
            SetFeedback("", false);
        }

        private void OnFieldFocusOut(FocusOutEvent evt)
        {
            // Le panneau reste ouvert : on rend juste le clavier aux raccourcis.
            UIState.IsInputFieldActive = false;
            inputFieldActive = false;
        }

        public void SubmitAnswer()
        {
            if (activeStep == null || answer == null) return;

            bool correct = activeStep.TryAnswer(answer.value);
            if (correct) return;

            SetFeedback("Mauvaise reponse, essayez encore.", true);
            answer.SetValueWithoutNotify("");
            answer.Focus();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private void SetFeedback(string message, bool bad)
        {
            if (feedback == null) return;
            feedback.text = message;
            if (bad) feedback.AddToClassList("puzzle-feedback--bad");
            else feedback.RemoveFromClassList("puzzle-feedback--bad");
        }

        private void SetVisible(bool visible)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            }
            if (screen == null) return;
            if (visible) screen.RemoveFromClassList("hidden");
            else screen.AddToClassList("hidden");
        }
    }
}
