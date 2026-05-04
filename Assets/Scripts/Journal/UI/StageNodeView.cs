using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Visuel d'un noeud d'etape sur la carte du journal.
    /// Affiche un numero, une couleur de fond et une icone selon l'etat
    /// (Resolved/Discovered/Locked → Completed/Current/Locked).
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

        [Header("Couleurs")]
        public Color completedBgColor = new Color(0.12f, 0.12f, 0.12f);
        public Color currentBgColor = Color.white;
        public Color lockedBgColor = new Color(0.87f, 0.87f, 0.87f);

        public Color completedTextColor = Color.white;
        public Color currentTextColor = new Color(0.12f, 0.12f, 0.12f);
        public Color lockedTextColor = new Color(0.73f, 0.73f, 0.73f);

        private StepBehaviour boundStep;
        private Button button;

        public void Init(StepBehaviour step, int stageIndex)
        {
            boundStep = step;
            button = GetComponent<Button>();

            // Numero affiche (1-indexed)
            if (numberText != null)
                numberText.text = (stageIndex + 1).ToString("00");

            Refresh();

            // Clic : feedback contextuel
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

            // Fond
            if (background != null)
            {
                switch (state)
                {
                    case StepState.Resolved:   background.color = completedBgColor; break;
                    case StepState.Discovered: background.color = currentBgColor;   break;
                    default:                   background.color = lockedBgColor;     break;
                }
            }

            // Texte
            if (numberText != null)
            {
                switch (state)
                {
                    case StepState.Resolved:   numberText.color = completedTextColor; break;
                    case StepState.Discovered: numberText.color = currentTextColor;   break;
                    default:                   numberText.color = lockedTextColor;     break;
                }
            }

            // Icones
            if (doubleCheckIcon != null) doubleCheckIcon.SetActive(state == StepState.Resolved);
            if (lockOpenIcon != null)    lockOpenIcon.SetActive(state == StepState.Discovered);
            if (lockClosedIcon != null)  lockClosedIcon.SetActive(state == StepState.Locked);

            // Interactivite : Locked = non cliquable
            if (button != null)
                button.interactable = state != StepState.Locked;
        }

        private void OnClicked()
        {
            if (boundStep == null) return;

            switch (boundStep.CurrentState)
            {
                case StepState.Discovered:
                    Debug.Log($"[Journal] Emplacement : {boundStep.transform.position}");
                    break;

                case StepState.Resolved:
                    if (boundStep.stepData != null && boundStep.stepData.initialClue != null
                        && !boundStep.stepData.initialClue.IsEmpty)
                    {
                        Debug.Log($"[Journal] Indice : \"{boundStep.stepData.initialClue.text}\"");
                    }
                    break;
            }
        }
    }
}
