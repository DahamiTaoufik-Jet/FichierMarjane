using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Directeur de tutoriel : joue une sequence de "beats" (consignes affichees)
    /// et avance selon une condition par beat (deplacement, saut, ouverture du
    /// journal, passage en FPS, resolution d'un bloc, fin de route, nombre de
    /// lettres, ou un simple delai). Data-driven : la sequence se configure dans
    /// l'Inspector.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        public enum TriggerType
        {
            Move, Jump, JournalOpen, FpsMode, StepResolved, RouteCompleted, LettersCount, Delay
        }

        [System.Serializable]
        public class Beat
        {
            [TextArea(2, 4)] public string text;
            public TriggerType trigger;
            [Tooltip("stepId (StepResolved) ou routeId (RouteCompleted).")]
            public string param;
            [Tooltip("Nombre requis (LettersCount).")]
            public int count;
            [Tooltip("Secondes (Delay).")]
            public float delay = 5f;
        }

        [Header("UI")]
        public GameObject promptPanel;
        public TMP_Text promptText;

        [Header("References")]
        public EscapeGame.Inventory.Runtime.Inventory inventory;
        [Tooltip("Panel racine du journal, pour detecter son ouverture (Tab).")]
        public GameObject journalPanel;

        [Tooltip("Modal detail du journal : les consignes se cachent des qu'on ouvre le detail d'une tuile, jusqu'a la fermeture du journal.")]
        public EscapeGame.Journal.UI.StageModalView stageModal;

        [Header("Sequence")]
        public List<Beat> beats = new List<Beat>();

        private int index = -1;
        private float beatStartTime;
        private bool fpsActivated;
        private bool promptSuppressed;
        private readonly HashSet<string> resolvedSteps = new HashSet<string>();
        private readonly HashSet<string> completedRoutes = new HashSet<string>();

        private void OnEnable()
        {
            RouteEvents.StepResolved += OnStepResolved;
            RouteEvents.RouteCompleted += OnRouteCompleted;
            PlayerCamera.FPSCameraActivated += OnFps;
        }

        private void OnDisable()
        {
            RouteEvents.StepResolved -= OnStepResolved;
            RouteEvents.RouteCompleted -= OnRouteCompleted;
            PlayerCamera.FPSCameraActivated -= OnFps;
        }

        private void Start()
        {
            if (promptPanel != null) promptPanel.SetActive(false);
            Advance();
        }

        private void Update()
        {
            if (index >= 0 && index < beats.Count && IsSatisfied(beats[index]))
                Advance();

            UpdatePromptVisibility();
        }

        /// <summary>
        /// Cache le panneau de consignes tant que le detail d'une tuile est
        /// ouvert (StageModal), jusqu'a la fermeture du journal ; sinon l'affiche
        /// si le beat courant a du texte.
        /// </summary>
        private void UpdatePromptVisibility()
        {
            if (promptPanel == null) return;

            bool journalOpen = journalPanel != null && journalPanel.activeInHierarchy;
            bool modalOpen = stageModal != null && stageModal.IsOpen;

            if (modalOpen) promptSuppressed = true;      // on a clique une tuile
            if (!journalOpen) promptSuppressed = false;  // reaffiche a la sortie du journal

            bool hasBeat = index >= 0 && index < beats.Count && !string.IsNullOrEmpty(beats[index].text);
            bool shouldShow = hasBeat && !promptSuppressed;
            if (promptPanel.activeSelf != shouldShow)
                promptPanel.SetActive(shouldShow);
        }

        // ====================================================================
        // Evenements
        // ====================================================================

        private void OnStepResolved(StepBehaviour s)
        {
            if (s != null && s.stepData != null && !string.IsNullOrEmpty(s.stepData.stepId))
                resolvedSteps.Add(s.stepData.stepId);
        }

        private void OnRouteCompleted(RouteRuntime r)
        {
            if (r != null) completedRoutes.Add(r.RouteId);
        }

        private void OnFps(Transform t) { fpsActivated = true; }

        // ====================================================================
        // Sequence
        // ====================================================================

        private bool IsSatisfied(Beat b)
        {
            switch (b.trigger)
            {
                case TriggerType.Move: return AnyMove();
                case TriggerType.Jump:
                    return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
                case TriggerType.JournalOpen:
                    return journalPanel != null && journalPanel.activeInHierarchy;
                case TriggerType.FpsMode: return fpsActivated;
                case TriggerType.StepResolved: return resolvedSteps.Contains(b.param);
                case TriggerType.RouteCompleted: return completedRoutes.Contains(b.param);
                case TriggerType.LettersCount: return LetterCount() >= b.count;
                case TriggerType.Delay: return (Time.time - beatStartTime) >= b.delay;
            }
            return false;
        }

        private void Advance()
        {
            index++;
            beatStartTime = Time.time;

            if (index >= beats.Count)
            {
                if (promptPanel != null) promptPanel.SetActive(false);
                return;
            }

            var b = beats[index];
            if (promptText != null) promptText.text = b.text;
            if (promptPanel != null) promptPanel.SetActive(!string.IsNullOrEmpty(b.text));
        }

        private bool AnyMove()
        {
            var k = Keyboard.current;
            if (k == null) return false;
            return k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed
                || k.upArrowKey.isPressed || k.downArrowKey.isPressed
                || k.leftArrowKey.isPressed || k.rightArrowKey.isPressed;
        }

        private int LetterCount()
        {
            return inventory != null ? inventory.GetItemsOfType<LetterItem>().Count : 0;
        }
    }
}
