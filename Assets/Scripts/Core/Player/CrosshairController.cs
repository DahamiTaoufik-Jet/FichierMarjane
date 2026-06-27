using UnityEngine;
using UnityEngine.UI;
using EscapeGame.Core.Interfaces;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Change la COULEUR d'un unique crosshair selon ce que vise le joueur via le
    /// <see cref="PlayerScanner"/>. La couleur "active" remplace la couleur par
    /// defaut dans deux cas :
    ///  1. On vise de pres un indice ou une enigme (cible du ray principal),
    ///     SAUF le radio (<see cref="AudioVisualPuzzleStep"/>) qui ne change rien.
    ///  2. On regarde une zone depuis une position valide (cible du ray
    ///     positionnel, qui n'existe que sur un spot de scan valide).
    ///
    /// A poser sur le HUD FPS (fpsCanvas). En mode TPS le canvas est inactif,
    /// donc ce composant ne tourne pas.
    /// </summary>
    public class CrosshairController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Scanner du joueur, source des cibles visees.")]
        public PlayerScanner scanner;

        [Tooltip("Image du crosshair dont on change la couleur.")]
        public Image crosshairImage;

        [Header("Couleurs")]
        [Tooltip("Couleur appliquee quand on vise une cible valide (indice/enigme sauf radio, ou zone de scan valide).")]
        public Color activeColor = Color.green;

        // -1 = non initialise, force le premier rafraichissement.
        private int lastHighlight = -1;
        // Couleur d'origine du crosshair, capturee au demarrage.
        private Color defaultColor = Color.white;

        private void Awake()
        {
            if (crosshairImage == null)
                crosshairImage = GetComponent<Image>();
            if (crosshairImage != null)
                defaultColor = crosshairImage.color;
        }

        private void OnEnable()
        {
            // Force l'etat correct des l'activation du HUD (ex. passage en FPS).
            lastHighlight = -1;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            bool highlight = ShouldHighlight();
            int state = highlight ? 1 : 0;
            if (state == lastHighlight) return;
            lastHighlight = state;

            if (crosshairImage != null)
                crosshairImage.color = highlight ? activeColor : defaultColor;
        }

        private bool ShouldHighlight()
        {
            if (scanner == null) return false;

            // Cas 2 : zone visee depuis une position valide.
            if (scanner.CurrentPositionalTarget != null) return true;

            // Cas 1 : indice / enigme vise de pres, sauf le radio.
            IScannable target = scanner.CurrentTarget;
            if (target != null && !(target is AudioVisualPuzzleStep))
                return true;

            return false;
        }
    }
}
