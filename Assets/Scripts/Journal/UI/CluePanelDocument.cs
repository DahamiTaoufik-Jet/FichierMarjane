using UnityEngine;
using UnityEngine.UIElements;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Version UI Toolkit du bandeau d'indice. Remplace <c>CluePanelView</c>.
    /// Affiche le contenu d'un <see cref="ClueContent"/> (texte et/ou image)
    /// quand un indice est revele, puis se masque tout seul.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CluePanelDocument : MonoBehaviour
    {
        [Header("Comportement")]
        [Tooltip("Duree d'affichage en secondes. <= 0 : pas de masquage automatique.")]
        public float autoHideAfter = 4f;

        [Header("Audio")]
        public AudioSource audioSource;

        private VisualElement panel;
        private VisualElement image;
        private Label text;

        private float hideAt = -1f;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            panel = root.Q<VisualElement>("clue-panel");
            image = root.Q<VisualElement>("clue-image");
            text = root.Q<Label>("clue-text");

            // Un indice ne doit jamais voler un clic au joueur.
            root.pickingMode = PickingMode.Ignore;
            if (panel != null) panel.pickingMode = PickingMode.Ignore;

            Hide();

            RouteEvents.ClueRevealed += HandleClueRevealed;
            RouteEvents.ClueHidden += Hide;
        }

        private void OnDisable()
        {
            RouteEvents.ClueRevealed -= HandleClueRevealed;
            RouteEvents.ClueHidden -= Hide;
        }

        private void Update()
        {
            if (autoHideAfter <= 0f || hideAt < 0f) return;
            if (Time.time < hideAt) return;
            Hide();
            hideAt = -1f;
        }

        private void HandleClueRevealed(ClueContent clue, StepBehaviour by)
        {
            if (clue == null || clue.IsEmpty) return;
            Show(clue);
        }

        private void Show(ClueContent clue)
        {
            bool isImage = clue.image != null;

            if (text != null)
            {
                text.text = clue.text != null ? clue.text : "";
                if (string.IsNullOrEmpty(clue.text)) text.AddToClassList("hidden");
                else text.RemoveFromClassList("hidden");
            }

            if (image != null)
            {
                if (isImage)
                {
                    image.style.backgroundImage = new StyleBackground(clue.image);
                    image.RemoveFromClassList("hidden");
                }
                else image.AddToClassList("hidden");
            }

            // Fond gris pour un indice texte, fond neutre pour une image :
            // le visuel se suffit et un aplat gris l'ecraserait.
            if (panel != null)
            {
                if (isImage) panel.AddToClassList("clue-panel--image");
                else panel.RemoveFromClassList("clue-panel--image");
                panel.RemoveFromClassList("hidden");
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
            if (panel != null) panel.AddToClassList("hidden");
        }
    }
}
