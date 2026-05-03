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

        private float hideAt = -1f;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
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

            if (textLabel != null) textLabel.text = clue.text;

            if (imageDisplay != null)
            {
                imageDisplay.sprite = clue.image;
                imageDisplay.enabled = clue.image != null;
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
