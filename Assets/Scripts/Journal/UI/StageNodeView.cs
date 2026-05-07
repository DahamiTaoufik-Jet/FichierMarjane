using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EscapeGame.Bonuses.Data;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Visuel d'un noeud d'etape sur la carte du journal.
    /// Affiche un numero, une couleur de fond et une icone selon l'etat
    /// (Resolved/Discovered/Locked). Supporte le mode selection generique
    /// via <see cref="JournalSelectionMode"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StageNodeView : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Image de fond du noeud.")]
        public Image background;

        [Tooltip("Texte du numero de l'etape.")]
        public TMP_Text numberText;

        [Tooltip("Icone affichee quand l'etape est Resolved (double check).")]
        public GameObject doubleCheckIcon;

        [Tooltip("Icone affichee quand l'etape est Discovered/Current (cadenas ouvert).")]
        public GameObject lockOpenIcon;

        [Tooltip("Icone affichee quand l'etape est Locked (cadenas ferme).")]
        public GameObject lockClosedIcon;

        [Header("Couleurs normales")]
        public Color completedBgColor = new Color(0.12f, 0.12f, 0.12f);
        public Color currentBgColor = Color.white;
        public Color lockedBgColor = new Color(0.87f, 0.87f, 0.87f);

        public Color completedTextColor = Color.white;
        public Color currentTextColor = new Color(0.12f, 0.12f, 0.12f);
        public Color lockedTextColor = new Color(0.73f, 0.73f, 0.73f);

        [Header("Couleurs mode selection")]
        [Tooltip("Jaune dore (Dechiffreur).")]
        public Color goldEligibleColor = new Color(0.9f, 0.7f, 0.1f);

        [Tooltip("Vert (Resolveur).")]
        public Color greenEligibleColor = new Color(0.2f, 0.8f, 0.3f);

        [Tooltip("Grise pour les blocs non eligibles en mode selection.")]
        public Color ineligibleBgColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        private StepBehaviour boundStep;
        private StageModalView modalView;
        private Button button;

        public void Init(StepBehaviour step, int stageIndex, StageModalView modal = null)
        {
            boundStep = step;
            modalView = modal;
            button = GetComponent<Button>();

            if (numberText != null)
                numberText.text = (stageIndex + 1).ToString("00");

            Refresh();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
        }

        public void Refresh()
        {
            if (boundStep == null) return;

            StepState state = boundStep.CurrentState;
            bool inSelection = JournalSelectionMode.IsActive;
            bool isEligible = inSelection && JournalSelectionMode.IsEligible(boundStep);

            // Fond
            if (background != null)
            {
                if (inSelection)
                {
                    if (isEligible)
                    {
                        background.color = JournalSelectionMode.ColorType == SelectionColorType.Green
                            ? greenEligibleColor
                            : goldEligibleColor;
                    }
                    else
                    {
                        background.color = ineligibleBgColor;
                    }
                }
                else
                {
                    switch (state)
                    {
                        case StepState.Resolved:   background.color = completedBgColor; break;
                        case StepState.Discovered: background.color = currentBgColor;   break;
                        default:                   background.color = lockedBgColor;     break;
                    }
                }
            }

            // Texte
            if (numberText != null)
            {
                if (inSelection)
                    numberText.color = isEligible ? currentTextColor : lockedTextColor;
                else
                {
                    switch (state)
                    {
                        case StepState.Resolved:   numberText.color = completedTextColor; break;
                        case StepState.Discovered: numberText.color = currentTextColor;   break;
                        default:                   numberText.color = lockedTextColor;     break;
                    }
                }
            }

            // Icones (masquees en mode selection)
            if (doubleCheckIcon != null) doubleCheckIcon.SetActive(!inSelection && state == StepState.Resolved);
            if (lockOpenIcon != null)    lockOpenIcon.SetActive(!inSelection && state == StepState.Discovered);
            if (lockClosedIcon != null)  lockClosedIcon.SetActive(!inSelection && state == StepState.Locked);

            // Interactivite
            if (button != null)
            {
                if (inSelection)
                    button.interactable = isEligible;
                else
                    button.interactable = state != StepState.Locked;
            }
        }

        private void OnClicked()
        {
            if (boundStep == null) return;

            // ---- Mode selection (Dechiffreur, Resolveur, etc.) ----
            if (JournalSelectionMode.IsActive)
            {
                if (!JournalSelectionMode.IsEligible(boundStep))
                {
                    Debug.Log($"[StageNode] Bloc '{boundStep.stepData?.stepId}' non eligible.");
                    return;
                }

                Debug.Log($"[StageNode] Selection du bloc '{boundStep.stepData?.stepId}'.");
                JournalSelectionMode.Select(boundStep);
                return;
            }

            // ---- Mode normal ----
            if (modalView != null)
            {
                var data = StageModalData.Build(boundStep);
                if (data != null) modalView.Show(data);
            }
        }
    }
}
