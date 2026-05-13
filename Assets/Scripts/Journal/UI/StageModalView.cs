using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EscapeGame.Routes.Data;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Gere l'affichage des details d'un StageNode.
    /// Le contenu (textes, images) est directement dans le JournalView,
    /// pas dans un panel modal separe. Les onglets switchent la visibilite
    /// des elements individuels. ButtonRetour revient a la carte.
    /// </summary>
    public class StageModalView : MonoBehaviour
    {
        // ==== Onglets ====
        [Header("Onglets")]
        [Tooltip("Bouton onglet Indice Initial.")]
        public Button tabInitial;

        [Tooltip("Bouton onglet Enigme.")]
        public Button tabEnigme;

        [Tooltip("Bouton onglet Suite.")]
        public Button tabSuite;

        [Header("Couleurs onglets")]
        public Color tabActiveColor = Color.white;
        public Color tabInactiveColor = new Color(0.7f, 0.7f, 0.7f);
        public Color tabDisabledColor = new Color(0.4f, 0.4f, 0.4f);

        // ==== Contenu Initial ====
        [Header("Contenu — Initial")]
        [Tooltip("Texte de l'indice initial.")]
        public TMP_Text clueText;

        // ==== Contenu Enigme ====
        [Header("Contenu — Enigme")]
        [Tooltip("Texte de la question.")]
        public TMP_Text enigmeText;

        [Tooltip("Texte chiffre de l'enigme.")]
        public TMP_Text enigmeTextEncrypter;

        [Tooltip("Image/snapshot de l'enigme.")]
        public Image enigmeImage;

        // ==== Contenu Suite ====
        [Header("Contenu — Suite")]
        [Tooltip("Texte de l'indice vers le bloc suivant.")]
        public TMP_Text nextClueText;

        // ==== Navigation ====
        [Header("Navigation")]
        [Tooltip("Bouton retour vers la carte du journal.")]
        public Button buttonRetour;

        [Tooltip("Le Scroll View (carte) a masquer quand le detail s'ouvre.")]
        public GameObject scrollView;

        [Tooltip("Les boutons de zoom a masquer quand le detail s'ouvre.")]
        public GameObject zoomIn;
        public GameObject zoomOut;
        public GameObject zoomReset;

        // Etat interne
        private bool hasEnigmeContent;
        private bool hasSuiteContent;
        private bool isOpen;

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void Awake()
        {
            if (buttonRetour != null)
                buttonRetour.onClick.AddListener(Close);

            if (tabInitial != null)
                tabInitial.onClick.AddListener(() => SwitchTab(0));
            if (tabEnigme != null)
                tabEnigme.onClick.AddListener(() => SwitchTab(1));
            if (tabSuite != null)
                tabSuite.onClick.AddListener(() => SwitchTab(2));

            // Cache tout le contenu au demarrage
            HideAllContent();
            SetTabsVisible(false);
            if (buttonRetour != null) buttonRetour.gameObject.SetActive(false);
        }

        // ====================================================================
        // API publique
        // ====================================================================

        public void Show(StageModalData data)
        {
            if (data == null) return;

            // ---- Contenu Initial ----
            FillTextFromClue(data.InitialClue, clueText);

            // ---- Contenu Enigme ----
            hasEnigmeContent = false;

            if (data.StepType == StepType.Puzzle)
            {
                bool hasQuestion = !string.IsNullOrEmpty(data.PuzzleQuestion);
                if (enigmeText != null)
                {
                    enigmeText.gameObject.SetActive(hasQuestion);
                    if (hasQuestion) enigmeText.text = data.PuzzleQuestion;
                }

                bool hasEncrypted = !string.IsNullOrEmpty(data.PuzzleEncryptedQuestion);
                if (enigmeTextEncrypter != null)
                {
                    enigmeTextEncrypter.gameObject.SetActive(hasEncrypted);
                    if (hasEncrypted) enigmeTextEncrypter.text = data.PuzzleEncryptedQuestion;
                }

                bool hasSnapshot = data.PuzzleSnapshot != null;
                if (enigmeImage != null)
                {
                    enigmeImage.gameObject.SetActive(hasSnapshot);
                    if (hasSnapshot) enigmeImage.sprite = data.PuzzleSnapshot;
                }

                hasEnigmeContent = hasQuestion || hasEncrypted || hasSnapshot;
            }

            if (!hasEnigmeContent)
            {
                if (enigmeText != null) enigmeText.gameObject.SetActive(false);
                if (enigmeTextEncrypter != null) enigmeTextEncrypter.gameObject.SetActive(false);
                if (enigmeImage != null) enigmeImage.gameObject.SetActive(false);
            }

            // ---- Contenu Suite ----
            hasSuiteContent = data.NextClue != null && !data.NextClue.IsEmpty;
            if (hasSuiteContent)
                FillTextFromClue(data.NextClue, nextClueText);
            else
            {
                if (nextClueText != null) nextClueText.gameObject.SetActive(false);
            }

            // ---- Afficher l'UI ----
            if (scrollView != null) scrollView.SetActive(false);
            if (zoomIn != null) zoomIn.SetActive(false);
            if (zoomOut != null) zoomOut.SetActive(false);
            if (zoomReset != null) zoomReset.SetActive(false);

            SetTabsVisible(true);
            if (buttonRetour != null) buttonRetour.gameObject.SetActive(true);

            UpdateTabStates();
            SwitchTab(0);

            isOpen = true;
        }

        public void Close()
        {
            HideAllContent();
            SetTabsVisible(false);
            if (buttonRetour != null) buttonRetour.gameObject.SetActive(false);

            if (scrollView != null) scrollView.SetActive(true);
            if (zoomIn != null) zoomIn.SetActive(true);
            if (zoomOut != null) zoomOut.SetActive(true);
            if (zoomReset != null) zoomReset.SetActive(true);

            isOpen = false;
        }

        public bool IsOpen { get { return isOpen; } }

        // ====================================================================
        // Onglets
        // ====================================================================

        private void SwitchTab(int index)
        {
            if (index == 1 && !hasEnigmeContent) return;
            if (index == 2 && !hasSuiteContent) return;

            // Tout masquer d'abord
            HideAllContent();

            // Afficher le contenu de l'onglet actif
            switch (index)
            {
                case 0:
                    if (clueText != null) clueText.gameObject.SetActive(true);
                    break;
                case 1:
                    if (enigmeText != null && !string.IsNullOrEmpty(enigmeText.text))
                        enigmeText.gameObject.SetActive(true);
                    if (enigmeTextEncrypter != null && !string.IsNullOrEmpty(enigmeTextEncrypter.text))
                        enigmeTextEncrypter.gameObject.SetActive(true);
                    if (enigmeImage != null && enigmeImage.sprite != null)
                        enigmeImage.gameObject.SetActive(true);
                    break;
                case 2:
                    if (nextClueText != null) nextClueText.gameObject.SetActive(true);
                    break;
            }

            SetTabColor(tabInitial, index == 0);
            SetTabColor(tabEnigme, index == 1);
            SetTabColor(tabSuite, index == 2);
        }

        private void UpdateTabStates()
        {
            if (tabEnigme != null)
                tabEnigme.interactable = hasEnigmeContent;
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

        private void SetTabsVisible(bool visible)
        {
            if (tabInitial != null) tabInitial.gameObject.SetActive(visible);
            if (tabEnigme != null) tabEnigme.gameObject.SetActive(visible);
            if (tabSuite != null) tabSuite.gameObject.SetActive(visible);
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private void HideAllContent()
        {
            if (clueText != null) clueText.gameObject.SetActive(false);
            if (enigmeText != null) enigmeText.gameObject.SetActive(false);
            if (enigmeTextEncrypter != null) enigmeTextEncrypter.gameObject.SetActive(false);
            if (enigmeImage != null) enigmeImage.gameObject.SetActive(false);
            if (nextClueText != null) nextClueText.gameObject.SetActive(false);
        }

        private void FillTextFromClue(ClueContent clue, TMP_Text textField)
        {
            if (clue == null || clue.IsEmpty)
            {
                if (textField != null) textField.gameObject.SetActive(false);
                return;
            }

            bool hasText = !string.IsNullOrWhiteSpace(clue.text);
            if (textField != null)
            {
                textField.gameObject.SetActive(hasText);
                if (hasText) textField.text = clue.text;
            }
        }
    }
}
