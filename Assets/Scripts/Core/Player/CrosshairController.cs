using UnityEngine;
using UnityEngine.UI;
using EscapeGame.Core.Interfaces;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Pilote le crosshair du HUD FPS selon la cible visee (via
    /// <see cref="PlayerScanner"/>) :
    ///  - MASQUE quand une UI est ouverte (on resout une enigme, une carte de
    ///    recompense s'affiche : UIState.IsAnyUIOpen) ;
    ///  - VERT   quand on vise une etape interagible ;
    ///  - GRIS   quand on vise une etape qui existe mais n'est PAS interagible
    ///    (verrouillee / trop d'etapes sautees / phase coffres) ;
    ///  - NOIR   quand on vise une etape deja resolue ;
    ///  - couleur par defaut sinon (rien vise, ou une RADIO qu'on n'annonce pas).
    ///
    /// A poser sur le HUD FPS (fpsCanvas). En mode TPS le canvas est inactif, donc
    /// ce composant ne tourne pas.
    /// </summary>
    public class CrosshairController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Scanner du joueur, source des cibles visees.")]
        public PlayerScanner scanner;

        [Tooltip("Image du crosshair dont on change la couleur.")]
        public Image crosshairImage;

        [Tooltip("Racine visuelle du crosshair a masquer (optionnel, pour un crosshair multi-parties). " +
                 "Si null, on masque directement crosshairImage.")]
        public GameObject crosshairRoot;

        [Header("Couleurs")]
        [Tooltip("Vert : etape interagible.")]
        public Color interactableColor = Color.green;

        [Tooltip("Gris : etape non interagible (verrouillee / sautee / phase coffres).")]
        public Color blockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        [Tooltip("Noir : etape deja resolue.")]
        public Color resolvedColor = Color.black;

        // Couleur d'origine du crosshair, capturee au demarrage (etat "rien / radio").
        private Color defaultColor = Color.white;

        // Etats : -99 non initialise, 0 masque, 1 defaut, 2 vert, 3 gris, 4 noir.
        private int lastState = -99;

        private void Awake()
        {
            if (crosshairImage == null)
                crosshairImage = GetComponent<Image>();
            if (crosshairImage != null)
                defaultColor = crosshairImage.color;
        }

        private void OnEnable()
        {
            lastState = -99;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            int state = ComputeState();
            if (state == lastState) return;
            lastState = state;

            bool visible = state != 0;
            SetVisible(visible);
            if (!visible || crosshairImage == null) return;

            switch (state)
            {
                case 2: crosshairImage.color = interactableColor; break; // vert
                case 3: crosshairImage.color = blockedColor;      break; // gris
                case 4: crosshairImage.color = resolvedColor;     break; // noir
                default: crosshairImage.color = defaultColor;     break; // 1 : defaut
            }
        }

        private int ComputeState()
        {
            // UI ouverte (enigme en cours / recompense) -> masque.
            if (UIState.IsAnyUIOpen) return 0;

            if (scanner == null) return 1;

            // Ray positionnel : n'existe que depuis un spot valide -> interagible (vert).
            if (scanner.CurrentPositionalTarget != null) return 2;

            IScannable target = scanner.CurrentTarget;

            // Radio : on n'affiche rien de special.
            if (target is AudioVisualPuzzleStep) return 1;

            var step = target as StepBehaviour;
            if (step == null) return 1;                 // rien vise (ou pas une etape)

            if (step.IsResolved) return 4;              // noir
            if (step.IsInteractable) return 2;          // vert
            return 3;                                    // gris (existe mais bloquee)
        }

        private void SetVisible(bool v)
        {
            if (crosshairRoot != null)
            {
                if (crosshairRoot.activeSelf != v) crosshairRoot.SetActive(v);
                return;
            }
            if (crosshairImage != null && crosshairImage.enabled != v)
                crosshairImage.enabled = v;
        }
    }
}
