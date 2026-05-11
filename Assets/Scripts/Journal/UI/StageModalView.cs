using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EscapeGame.Routes.Data;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Modal a onglets affiche quand le joueur clique sur un StageNode.
    /// 3 boutons en haut (Initial / Enigme / Suite) switchent entre
    /// 3 panels plein ecran superposes.
    /// </summary>
    public class StageModalView : MonoBehaviour
    {
        [Header("Panel racine")]
        [Tooltip("Le panel modal a activer/desactiver.")]
        public GameObject modalRoot;

        // ==== Onglets ====
        [Header("Onglets (boutons en haut)")]
        public Button tabInitial;
        public Button tabEnigme;
        public Button tabSuite;

        [Header("Couleurs onglets")]
        public Color tabActiveColor = Color.white;
        public Color tabInactiveColor = new Color(0.7f, 0.7f, 0.7f);
        public Color tabDisabledColor = new Color(0.4f, 0.4f, 0.4f);

        // ==== Panel Initial ====
        [Header("Contenu — Initial")]
        public GameObject contentInitial;
        public TMP_Text initialClueText;
        public Image initialClueImage;

        // ==== Panel Enigme ====
        [Header("Contenu — Enigme")]
        public GameObject contentEnigme;
        public TMP_Text puzzleQuestionText;
        public TMP_Text encryptedMessageText;
        public GameObject encryptedMessageGroup;
        [Tooltip("Image du snapshot (pour PositionalScan ou autre enigme visuelle).")]
        public Image puzzleSnapshotImage;

        // ==== Panel Suite ====
        [Header("Contenu — Suite")]
        public GameObject contentSuite;
        public TMP_Text nextClueText;
        public Image nextClueImage;

        // ==== Fermeture ====
        [Header("Fermeture")]
        public Button closeButton;

        [Tooltip("Le Scroll View (ou panel journal) a masquer quand le modal s'ouvre.")]
        public GameObject journalPanel;

        // Etat interne
        private bool hasEnigmeContent;
        private bool hasSuiteContent;

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void Awake()
        {
            if (modalRoot != null) modalRoot.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (tabInitial != null)
                tabInitial.onClick.AddListener(() => SwitchTab(0));
            if (tabEnigme != null)
                tabEnigme.onClick.AddListener(() => SwitchTab(1));
            if (tabSuite != null)
                tabSuite.onClick.AddListener(() => SwitchTab(2));
        }

        // ====================================================================
        // API publique
        // ====================================================================

        public void Show(StageModalData data)
        {
            if (data == null || modalRoot == null) return;

            // ---- Panel Initial ----
            FillCluePanel(data.InitialClue, initialClueText, initialClueImage);

            // ---- Panel Enigme ----
            hasEnigmeContent = false;

            if (data.StepType == StepType.Puzzle)
            {
                // Question textuelle
                bool hasQuestion = !string.IsNullOrEmpty(data.PuzzleQuestion);
                if (puzzleQuestionText != null)
                {
                    puzzleQuestionText.gameObject.SetActive(hasQuestion);
                    if (hasQuestion) puzzleQuestionText.text = data.PuzzleQuestion;
                }

                // Message chiffre
                bool hasEncrypted = !string.IsNullOrEmpty(data.PuzzleEncryptedQuestion);
                if (encryptedMessageGroup != null) encryptedMessageGroup.SetActive(hasEncrypted);
                if (encryptedMessageText != null && hasEncrypted)
                    encryptedMessageText.text = data.PuzzleEncryptedQuestion;

                // Snapshot (PositionalScan ou autre enigme visuelle)
                bool hasSnapshot = data.PuzzleSnapshot != null;
                if (puzzleSnapshotImage != null)
                {
                    puzzleSnapshotImage.gameObject.SetActive(hasSnapshot);
                    if (hasSnapshot) puzzleSnapshotImage.sprite = data.PuzzleSnapshot;
                }

                hasEnigmeContent = hasQuestion || hasEncrypted || hasSnapshot;
            }

            // Si pas de contenu enigme, tout masquer dans le panel
            if (!hasEnigmeContent)
            {
                if (puzzleQuestionText != null) puzzleQuestionText.gameObject.SetActive(false);
                if (encryptedMessageGroup != null) encryptedMessageGroup.SetActive(false);
                if (puzzleSnapshotImage != null) puzzleSnapshotImage.gameObject.SetActive(false);
            }

            // ---- Panel Suite ----
            hasSuiteContent = data.NextClue != null && !data.NextClue.IsEmpty;
            if (hasSuiteContent)
                FillCluePanel(data.NextClue, nextClueText, nextClueImage);
            else
            {
                if (nextClueText != null) nextClueText.gameObject.SetActive(false);
                if (nextClueImage != null) nextClueImage.gameObject.SetActive(false);
            }

            // ---- Ouvrir sur l'onglet Initial par defaut ----
            UpdateTabStates();
            SwitchTab(0);

            if (journalPanel != null) journalPanel.SetActive(false);
            modalRoot.SetActive(true);
        }

        public void Close()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            if (journalPanel != null) journalPanel.SetActive(true);
        }

        // ====================================================================
        // Onglets
        // ====================================================================

        private void SwitchTab(int index)
        {
            // Bloquer si l'onglet est desactive
            if (index == 1 && !hasEnigmeContent) return;
            if (index == 2 && !hasSuiteContent) return;

            if (contentInitial != null) contentInitial.SetActive(index == 0);
            if (contentEnigme != null) contentEnigme.SetActive(index == 1);
            if (contentSuite != null) contentSuite.SetActive(index == 2);

            // Visuels des boutons
            SetTabColor(tabInitial, index == 0);
            SetTabColor(tabEnigme, index == 1);
            SetTabColor(tabSuite, index == 2);
        }

        private void UpdateTabStates()
        {
            // Enigme : desactive si pas de contenu
            if (tabEnigme != null)
                tabEnigme.interactable = hasEnigmeContent;

            // Suite : desactive si pas resolu
            if (tabSuite != null)
                tabSuite.interactable = hasSuiteContent;
        }

        private void SetTabColor(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img != null)
            {
                if (!tab.interactable)
                    img.color = tabDisabledColor;
                else
                    img.color = active ? tabActiveColor : tabInactiveColor;
            }
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private void FillCluePanel(ClueContent clue, TMP_Text textField, Image imageField)
        {
            if (clue == null || clue.IsEmpty)
            {
                if (textField != null) textField.gameObject.SetActive(false);
                if (imageField != null) imageField.gameObject.SetActive(false);
                return;
            }

            bool hasText = !string.IsNullOrWhiteSpace(clue.text);
            if (textField != null)
            {
                textField.gameObject.SetActive(hasText);
                if (hasText) textField.text = clue.text;
            }

            bool hasImage = clue.image != null;
            if (imageField != null)
            {
                imageField.gameObject.SetActive(hasImage);
                if (hasImage) imageField.sprite = clue.image;
            }
        }
    }
}
