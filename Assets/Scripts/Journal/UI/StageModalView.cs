using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EscapeGame.Routes.Data;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Modal a 3 colonnes affiche quand le joueur clique sur un StageNode.
    /// Gauche  : indice initial (texte et/ou image)
    /// Centre  : zone enigme (texte question + message chiffre + bouton photo)
    ///           masque si le step n'est pas un Puzzle
    /// Droite  : indice vers la suite (si resolu)
    /// </summary>
    public class StageModalView : MonoBehaviour
    {
        [Header("Panel racine")]
        [Tooltip("Le panel modal a activer/desactiver.")]
        public GameObject modalRoot;

        // ==== Colonne gauche : Indice initial ====
        [Header("Colonne gauche — Indice initial")]
        public TMP_Text initialClueText;
        public Image initialClueImage;

        // ==== Colonne centre : Zone enigme ====
        [Header("Colonne centre — Zone enigme")]
        [Tooltip("Conteneur de toute la colonne centre (masque si pas Puzzle).")]
        public GameObject centerColumn;

        public TMP_Text puzzleQuestionText;

        [Tooltip("Zone du message chiffre.")]
        public TMP_Text encryptedMessageText;
        public GameObject encryptedMessageGroup;

        [Tooltip("Bouton pour ouvrir la photo de la zone a observer.")]
        public Button viewPhotoButton;

        [Tooltip("Panel depliable pour la photo.")]
        public GameObject photoPanel;
        public Image photoImage;

        // ==== Colonne droite : Indice vers la suite ====
        [Header("Colonne droite — Indice suivant")]
        [Tooltip("Conteneur de la colonne droite (masque si pas resolu).")]
        public GameObject rightColumn;

        public TMP_Text nextClueText;
        public Image nextClueImage;

        // ==== Bouton fermer ====
        [Header("Fermeture")]
        public Button closeButton;

        [Tooltip("Le Scroll View (ou panel journal) a masquer quand le modal s'ouvre.")]
        public GameObject journalPanel;

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void Awake()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            if (photoPanel != null) photoPanel.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (viewPhotoButton != null)
                viewPhotoButton.onClick.AddListener(TogglePhoto);
        }

        // ====================================================================
        // API publique
        // ====================================================================

        /// <summary>
        /// Ouvre le modal avec les donnees d'un bloc.
        /// Appele par StageNodeView au clic.
        /// </summary>
        public void Show(StageModalData data)
        {
            if (data == null || modalRoot == null) return;

            // Reset
            if (photoPanel != null) photoPanel.SetActive(false);

            // ---- Colonne gauche : indice initial ----
            FillClueColumn(data.InitialClue, initialClueText, initialClueImage);

            // ---- Colonne centre : zone enigme ----
            bool hasPuzzleContent = data.StepType == Routes.Data.StepType.Puzzle
                && (!string.IsNullOrEmpty(data.PuzzleQuestion)
                    || !string.IsNullOrEmpty(data.PuzzleEncryptedQuestion)
                    || data.PuzzleSnapshot != null);

            if (centerColumn != null) centerColumn.SetActive(hasPuzzleContent);

            if (hasPuzzleContent)
            {
                // Question
                if (puzzleQuestionText != null)
                {
                    bool hasQuestion = !string.IsNullOrEmpty(data.PuzzleQuestion);
                    puzzleQuestionText.gameObject.SetActive(hasQuestion);
                    if (hasQuestion) puzzleQuestionText.text = data.PuzzleQuestion;
                }

                // Message chiffre
                bool hasEncrypted = !string.IsNullOrEmpty(data.PuzzleEncryptedQuestion);
                if (encryptedMessageGroup != null) encryptedMessageGroup.SetActive(hasEncrypted);
                if (encryptedMessageText != null && hasEncrypted)
                    encryptedMessageText.text = data.PuzzleEncryptedQuestion;

                // Bouton photo
                if (viewPhotoButton != null)
                    viewPhotoButton.gameObject.SetActive(data.PuzzleSnapshot != null);

                if (photoImage != null && data.PuzzleSnapshot != null)
                    photoImage.sprite = data.PuzzleSnapshot;
            }

            // ---- Colonne droite : indice vers la suite ----
            bool hasNext = data.NextClue != null && !data.NextClue.IsEmpty;
            if (rightColumn != null) rightColumn.SetActive(hasNext);

            if (hasNext)
                FillClueColumn(data.NextClue, nextClueText, nextClueImage);

            if (journalPanel != null) journalPanel.SetActive(false);
            modalRoot.SetActive(true);
        }

        public void Close()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            if (journalPanel != null) journalPanel.SetActive(true);
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private void FillClueColumn(ClueContent clue, TMP_Text textField, Image imageField)
        {
            if (clue == null || clue.IsEmpty)
            {
                if (textField != null) textField.gameObject.SetActive(false);
                if (imageField != null) imageField.gameObject.SetActive(false);
                return;
            }

            // Texte
            bool hasText = !string.IsNullOrWhiteSpace(clue.text);
            if (textField != null)
            {
                textField.gameObject.SetActive(hasText);
                if (hasText) textField.text = clue.text;
            }

            // Image
            bool hasImage = clue.image != null;
            if (imageField != null)
            {
                imageField.gameObject.SetActive(hasImage);
                if (hasImage) imageField.sprite = clue.image;
            }
        }

        private void TogglePhoto()
        {
            if (photoPanel != null)
                photoPanel.SetActive(!photoPanel.activeSelf);
        }
    }
}
