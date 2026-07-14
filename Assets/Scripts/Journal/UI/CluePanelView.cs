using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Panneau UI qui affiche le contenu d'un ClueContent (texte / image / audio)
    /// quand un indice ou un enonce d'enigme est revele via RouteEvents.ClueRevealed.
    /// </summary>
    public class CluePanelView : MonoBehaviour
    {
        [Header("Cibles UI")]
        [Tooltip("Racine du panneau a activer/desactiver. Si null, le GameObject portant ce script est utilise.")]
        public GameObject panelRoot;

        [Tooltip("Label TextMeshPro pour le texte de l'indice.")]
        public TMP_Text textLabel;

        [Tooltip("Image optionnelle pour le visuel de l'indice.")]
        public Image imageDisplay;

        [Tooltip("Source audio optionnelle pour le son de l'indice.")]
        public AudioSource audioSource;

        [Header("Comportement")]
        [Tooltip("Duree d'affichage en secondes avant masquage automatique. Mettre <= 0 pour ne pas masquer automatiquement.")]
        public float autoHideAfter = 6f;

        [Header("Style (indice TEXTE uniquement)")]
        [Tooltip("Fond gris applique quand l'indice est un TEXTE (une image ne change rien).")]
        public Color textBackgroundColor = new Color(0.25f, 0.25f, 0.27f, 1f);

        [Tooltip("Couleur du texte quand l'indice est un texte.")]
        public Color textColor = Color.white;

        [Tooltip("Marge (px) autour du texte : le label remplit le panneau pour un vrai centrage.")]
        public float textPadding = 40f;

        private float hideAt = -1f;

        // Fond du panneau : sa couleur d'origine sert au cas IMAGE (inchange).
        private Image backgroundImage;
        private Color imageBackgroundColor;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;

            backgroundImage = panelRoot.GetComponent<Image>();
            if (backgroundImage != null) imageBackgroundColor = backgroundImage.color;

            // Le label remplit le panneau (avec marge) pour un centrage correct
            // au lieu d'une petite boite en haut a gauche.
            if (textLabel != null)
            {
                var rt = textLabel.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(textPadding, textPadding);
                rt.offsetMax = new Vector2(-textPadding, -textPadding);
                textLabel.alignment = TMPro.TextAlignmentOptions.Top;
            }

            panelRoot.SetActive(false);
            RouteEvents.ClueRevealed += HandleClueRevealed;
            RouteEvents.ClueHidden += Hide;
        }

        private void OnDestroy()
        {
            RouteEvents.ClueRevealed -= HandleClueRevealed;
            RouteEvents.ClueHidden -= Hide;
        }

        private void Update()
        {
            if (autoHideAfter > 0f && hideAt > 0f && Time.time >= hideAt)
            {
                Hide();
                hideAt = -1f;
            }
        }

        private void HandleClueRevealed(ClueContent clue, StepBehaviour by)
        {
            if (clue == null || clue.IsEmpty) return;
            Show(clue);
        }

        private void Show(ClueContent clue)
        {
            panelRoot.SetActive(true);

            bool isImage = clue.image != null;

            if (textLabel != null) textLabel.text = clue.text;

            if (imageDisplay != null)
            {
                imageDisplay.sprite = clue.image;
                imageDisplay.enabled = clue.image != null;
            }

            // TEXTE : fond gris + texte blanc centre. IMAGE : on ne change rien
            // (fond d'origine conserve).
            if (backgroundImage != null)
                backgroundImage.color = isImage ? imageBackgroundColor : textBackgroundColor;

            if (!isImage && textLabel != null)
            {
                textLabel.color = textColor;
                textLabel.alignment = TMPro.TextAlignmentOptions.Top;
            }

            if (audioSource != null && clue.audio != null)
            {
                audioSource.Stop();
                audioSource.clip = clue.audio;
                audioSource.Play();
            }

            hideAt = autoHideAfter > 0f ? Time.time + autoHideAfter : -1f;
        }

        private void Hide()
        {
            panelRoot.SetActive(false);
        }
    }
}
