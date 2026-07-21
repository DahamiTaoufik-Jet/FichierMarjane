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
        /// <summary>Plafond absolu : un indice ne doit jamais rester plus longtemps.</summary>
        public const float MaxDisplayDuration = 12f;

        [Header("Comportement")]
        [Tooltip("Duree TOTALE a l'ecran, fondu de sortie inclus. Plafonnee a 12 s.")]
        public float displayDuration = 12f;

        [Tooltip("Duree du fondu de sortie, pris sur la fin de displayDuration.")]
        public float fadeDuration = 0.8f;

        [Header("Audio")]
        public AudioSource audioSource;

        private VisualElement panel;
        private VisualElement image;
        private Label text;

        // Instant (Time.unscaledTime) ou l'indice doit avoir totalement disparu.
        // Non-scale volontairement : sinon un timeScale a 0 (menu pause) gelerait
        // le compte a rebours et l'indice resterait affiche indefiniment.
        private float goneAt = -1f;
        private bool visible;

        // Contenu actuellement affiche. Sert a distinguer un NOUVEL indice d'une
        // simple re-emission du meme : PuzzleStep.OnScan() relance la revelation
        // a chaque scan tant que l'enigme n'est pas resolue, ce qui rearmait le
        // compte a rebours indefiniment et laissait l'indice a l'ecran.
        private string shownText;
        private Sprite shownSprite;

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
            if (!visible) return;

            float remaining = goneAt - Time.unscaledTime;

            if (remaining <= 0f)
            {
                Hide();
                return;
            }

            // Fondu sur la toute fin : l'opacite suit le temps restant.
            float fade = Mathf.Max(0.01f, fadeDuration);
            if (panel != null)
                panel.style.opacity = remaining < fade ? remaining / fade : 1f;
        }

        private void HandleClueRevealed(ClueContent clue, StepBehaviour by)
        {
            if (clue == null || clue.IsEmpty) return;

            // Indice d'ouverture de route : RouteManager le leve avec by == null
            // au moment de l'enregistrement, donc pour TOUTES les routes en meme
            // temps au chargement de la scene. Il n'en resterait qu'un seul a
            // l'ecran, au hasard : on les ignore.
            if (by == null) return;

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

            // Meme contenu deja a l'ecran : on NE rearme PAS le minuteur, sinon
            // un indice re-emis en boucle resterait affiche indefiniment.
            bool sameContent = visible
                && shownText == (clue.text != null ? clue.text : "")
                && shownSprite == clue.image;

            if (sameContent) return;

            // Nouvel indice : repart a pleine opacite, meme s'il arrive pendant
            // le fondu du precedent.
            if (panel != null) panel.style.opacity = 1f;

            shownText = clue.text != null ? clue.text : "";
            shownSprite = clue.image;

            float total = Mathf.Clamp(displayDuration, 0.1f, MaxDisplayDuration);
            goneAt = Time.unscaledTime + total;
            visible = true;
        }

        private void Hide()
        {
            visible = false;
            goneAt = -1f;
            shownText = null;
            shownSprite = null;
            if (panel == null) return;
            panel.AddToClassList("hidden");
            panel.style.opacity = 1f;
        }
    }
}
