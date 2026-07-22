using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;
using EscapeGame.Core.World;
using EscapeGame.Inventory.Data;
using EscapeGame.Routes.Services;

// UnityEngine.UIElements definit aussi un type Cursor : on leve l'ambiguite.
using Cursor = UnityEngine.Cursor;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Version UI Toolkit du panneau de depot de lettre dans un coffre.
    /// Remplace <c>ChestLetterPanelView</c>. Les boutons de lettres sont crees
    /// directement en VisualElement : plus de prefab a instancier ni a detruire.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ChestLetterPanelDocument : MonoBehaviour
    {
        [Header("Affichage")]
        [Tooltip("Affiche le compteur d'essais. A mettre a false en tutoriel (essais illimites).")]
        public bool showAttempts = true;

        [Header("Reference")]
        public Runtime.Inventory inventory;

        private VisualElement screen;
        private VisualElement lettersRoot;
        private Label attempts;
        private Label feedback;

        private ChestInteractable currentChest;
        private int currentPosition;
        private bool isOpen;
        private bool pendingCursorLock;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            screen = root.Q<VisualElement>("chestletter-screen");
            lettersRoot = root.Q<VisualElement>("chestletter-letters");
            attempts = root.Q<Label>("chestletter-attempts");
            feedback = root.Q<Label>("chestletter-feedback");

            if (inventory == null)
                inventory = FindFirstObjectByType<Runtime.Inventory>(FindObjectsInactive.Include);

            SetVisible(false);
        }

        private void OnDisable()
        {
            // Panneau ouvert lors d'un changement de scene : ne pas laisser le
            // compteur UIState desequilibre, sinon les inputs restent bloques.
            if (!isOpen) return;
            isOpen = false;
            currentChest = null;
            UIState.SetUIClosed();
        }

        private void Update()
        {
            if (!isOpen) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                UIState.ConsumeCloseKey();
                Close();
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

        // ====================================================================
        // Ouverture / fermeture
        // ====================================================================

        public void Open(ChestInteractable chest, int position)
        {
            if (isOpen) return;
            if (inventory == null)
            {
                Debug.LogWarning("[ChestLetterPanelDocument] Inventory non assigne.");
                return;
            }

            currentChest = chest;
            currentPosition = position;
            isOpen = true;

            SetVisible(true);
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateAttempts();
            SetFeedback("", null);
            BuildLetterButtons();
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            currentChest = null;

            if (lettersRoot != null) lettersRoot.Clear();
            SetVisible(false);
            UIState.SetUIClosed();

            pendingCursorLock = true;
        }

        // ====================================================================
        // Contenu
        // ====================================================================

        private void BuildLetterButtons()
        {
            if (lettersRoot == null || inventory == null) return;
            lettersRoot.Clear();

            var letters = inventory.GetItemsOfType<LetterItem>();
            var seen = new HashSet<char>();

            for (int i = 0; i < letters.Count; i++)
            {
                char c = char.ToUpper(letters[i].letter);
                if (c == '\0' || !seen.Add(c)) continue;

                char captured = c;
                LetterItem capturedItem = letters[i];

                var btn = new Button();
                btn.text = captured.ToString();
                btn.AddToClassList("letter-btn");
                btn.clicked += delegate { OnLetterClicked(captured, capturedItem); };
                lettersRoot.Add(btn);
            }

            if (letters.Count == 0)
                SetFeedback("Aucune lettre en inventaire.", "feedback--bad");
        }

        private void OnLetterClicked(char letter, LetterItem item)
        {
            if (PasswordManager.Instance == null) return;

            var result = PasswordManager.Instance.TryLetter(currentPosition, letter);

            switch (result)
            {
                case PasswordManager.TryResult.Correct:
                    if (inventory != null) inventory.RemoveItem(item);
                    SetFeedback("Bonne lettre !", "feedback--ok");
                    Close();
                    break;

                case PasswordManager.TryResult.Wrong:
                    UpdateAttempts();
                    SetFeedback(showAttempts
                        ? "Mauvaise lettre ! (" + PasswordManager.Instance.AttemptsRemaining + " essais restants)"
                        : "Mauvaise lettre !", "feedback--bad");
                    break;

                case PasswordManager.TryResult.Lost:
                    UpdateAttempts();
                    SetFeedback("Plus d'essais !", "feedback--bad");
                    Close();
                    break;

                case PasswordManager.TryResult.AlreadySolved:
                    Close();
                    break;
            }
        }

        private void UpdateAttempts()
        {
            if (attempts == null) return;

            if (!showAttempts)
            {
                attempts.AddToClassList("hidden");
                return;
            }

            attempts.RemoveFromClassList("hidden");
            if (PasswordManager.Instance != null)
                attempts.text = "Essais : " + PasswordManager.Instance.AttemptsRemaining;
        }

        private void SetFeedback(string message, string variantClass)
        {
            if (feedback == null) return;
            feedback.text = message;
            feedback.RemoveFromClassList("feedback--ok");
            feedback.RemoveFromClassList("feedback--bad");
            if (!string.IsNullOrEmpty(variantClass)) feedback.AddToClassList(variantClass);
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
