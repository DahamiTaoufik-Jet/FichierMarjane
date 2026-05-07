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

        [Tooltip("Couleur du fond quand le bloc est eligible au dechiffrement (mode Dechiffreur).")]
        public Color decryptEligibleBgColor = new Color(0.9f, 0.7f, 0.1f);

        [Tooltip("Couleur du fond quand le bloc n'est PAS eligible en mode Dechiffreur.")]
        public Color decryptIneligibleBgColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        public Color completedTextColor = Color.white;
        public Color currentTextColor = new Color(0.12f, 0.12f, 0.12f);
        public Color lockedTextColor = new Color(0.73f, 0.73f, 0.73f);

        private StepBehaviour boundStep;
        private StageModalView modalView;
        private Button button;

        public void Init(StepBehaviour step, int stageIndex, StageModalView modal = null)
        {
            boundStep = step;
            modalView = modal;
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
            bool inDecryptMode = DecryptionTracker.IsInSelectionMode;
            bool isEligible = false;

            // Verifier si ce bloc est eligible au dechiffrement
            if (inDecryptMode && boundStep.stepData != null)
            {
                isEligible = DecryptionTracker.IsEligibleForDecryption(
                    boundStep.stepData.puzzleEncrypted,
                    boundStep.stepData.puzzleEncryptedQuestion,
                    boundStep.stepData.stepId);
            }

            // Fond
            if (background != null)
            {
                if (inDecryptMode)
                {
                    // Mode dechiffrement : jaune dore pour eligible, grise pour les autres
                    background.color = isEligible ? decryptEligibleBgColor : decryptIneligibleBgColor;
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
                if (inDecryptMode)
                {
                    numberText.color = isEligible ? currentTextColor : lockedTextColor;
                }
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

            // Icones
            if (doubleCheckIcon != null) doubleCheckIcon.SetActive(!inDecryptMode && state == StepState.Resolved);
            if (lockOpenIcon != null)    lockOpenIcon.SetActive(!inDecryptMode && state == StepState.Discovered);
            if (lockClosedIcon != null)  lockClosedIcon.SetActive(!inDecryptMode && state == StepState.Locked);

            // Interactivite
            if (button != null)
            {
                if (inDecryptMode)
                    button.interactable = isEligible; // Seuls les blocs eligibles sont cliquables
                else
                    button.interactable = state != StepState.Locked;
            }
        }

        private void OnClicked()
        {
            if (boundStep == null) return;

            // ---- Mode dechiffrement ----
            if (DecryptionTracker.IsInSelectionMode)
            {
                if (boundStep.stepData == null) return;

                bool eligible = DecryptionTracker.IsEligibleForDecryption(
                    boundStep.stepData.puzzleEncrypted,
                    boundStep.stepData.puzzleEncryptedQuestion,
                    boundStep.stepData.stepId);

                if (!eligible)
                {
                    Debug.Log($"[StageNode] Bloc '{boundStep.stepData.stepId}' non eligible au dechiffrement.");
                    return;
                }

                Debug.Log($"[StageNode] Dechiffrement du bloc '{boundStep.stepData.stepId}'.");
                DecryptionTracker.SelectStep(boundStep.stepData.stepId);
                return;
            }

            // ---- Mode normal ----
            Debug.Log($"[StageNode] Clic sur step {(boundStep != null ? boundStep.stepData?.stepId : "null")}, state={boundStep?.CurrentState}, modal={modalView != null}");

            if (modalView != null)
            {
                var data = StageModalData.Build(boundStep);
                if (data != null) modalView.Show(data);
            }
        }
    }
}
