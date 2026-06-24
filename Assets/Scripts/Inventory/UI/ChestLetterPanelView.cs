using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EscapeGame.Core.Player;
using EscapeGame.Core.World;
using EscapeGame.Inventory.Data;
using EscapeGame.Routes.Services;

namespace EscapeGame.Inventory.UI
{
    public class ChestLetterPanelView : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Racine du panneau a activer/desactiver.")]
        public GameObject panelRoot;

        [Tooltip("Parent des boutons de lettres (Layout Group).")]
        public Transform letterButtonContainer;

        [Tooltip("Prefab du bouton de lettre. Doit avoir un Button + TMP_Text enfant.")]
        public GameObject letterButtonPrefab;

        [Tooltip("Texte affichant les essais restants.")]
        public TMP_Text attemptsText;

        [Tooltip("Texte affichant le resultat (bonne/mauvaise reponse).")]
        public TMP_Text feedbackText;

        [Header("Reference")]
        [Tooltip("Inventaire du joueur.")]
        public Runtime.Inventory inventory;

        private ChestInteractable currentChest;
        private int currentPosition;
        private bool isOpen;
        private readonly List<GameObject> spawnedButtons = new List<GameObject>();

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (!isOpen) return;

            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open(ChestInteractable chest, int position)
        {
            if (isOpen) return;
            if (inventory == null)
            {
                Debug.LogWarning("[ChestLetterPanelView] Inventory non assigne.");
                return;
            }

            currentChest = chest;
            currentPosition = position;
            isOpen = true;

            panelRoot.SetActive(true);
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateAttemptsDisplay();
            if (feedbackText != null)
                feedbackText.text = "";

            PopulateLetterButtons();
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            currentChest = null;

            ClearButtons();
            panelRoot.SetActive(false);
            UIState.SetUIClosed();

            if (!UIState.IsAnyUIOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void PopulateLetterButtons()
        {
            ClearButtons();
            if (inventory == null || letterButtonContainer == null || letterButtonPrefab == null) return;

            var letters = inventory.GetItemsOfType<LetterItem>();
            var seen = new HashSet<char>();

            for (int i = 0; i < letters.Count; i++)
            {
                char c = char.ToUpper(letters[i].letter);
                if (c == '\0' || !seen.Add(c)) continue;

                var go = Instantiate(letterButtonPrefab, letterButtonContainer);
                go.SetActive(true);
                spawnedButtons.Add(go);

                var text = go.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = c.ToString();

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    char captured = c;
                    LetterItem capturedItem = letters[i];
                    button.onClick.AddListener(() => OnLetterClicked(captured, capturedItem));
                }
            }

            if (letters.Count == 0 && feedbackText != null)
                feedbackText.text = "Aucune lettre en inventaire.";
        }

        private void OnLetterClicked(char letter, LetterItem item)
        {
            if (PasswordManager.Instance == null) return;

            var result = PasswordManager.Instance.TryLetter(currentPosition, letter);

            switch (result)
            {
                case PasswordManager.TryResult.Correct:
                    if (inventory != null)
                        inventory.RemoveItem(item);
                    if (feedbackText != null)
                        feedbackText.text = "Bonne lettre !";
                    Close();
                    break;

                case PasswordManager.TryResult.Wrong:
                    UpdateAttemptsDisplay();
                    if (feedbackText != null)
                        feedbackText.text = $"Mauvaise lettre ! ({PasswordManager.Instance.AttemptsRemaining} essais restants)";
                    break;

                case PasswordManager.TryResult.Lost:
                    UpdateAttemptsDisplay();
                    if (feedbackText != null)
                        feedbackText.text = "Plus d'essais !";
                    Close();
                    break;

                case PasswordManager.TryResult.AlreadySolved:
                    Close();
                    break;
            }
        }

        private void UpdateAttemptsDisplay()
        {
            if (attemptsText == null || PasswordManager.Instance == null) return;
            attemptsText.text = $"Essais : {PasswordManager.Instance.AttemptsRemaining}";
        }

        private void ClearButtons()
        {
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                if (spawnedButtons[i] != null)
                    Destroy(spawnedButtons[i]);
            }
            spawnedButtons.Clear();
        }
    }
}
