using UnityEngine;
using UnityEngine.UI;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Visuel d'un bloc d'étape dans le journal. Trois états visuels possibles
    /// (cadenas / cadenas argenté / sans cadenas) correspondant aux trois
    /// <see cref="StepState"/>. Au clic, déclenche la révélation contextuelle
    /// (coordonnées si Discovered, indice suivant si Resolved).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StepBlockView : MonoBehaviour
    {
        [Header("Visuels")]
        [Tooltip("Image affichée quand l'étape est Locked (cadenas plein).")]
        public GameObject lockedIcon;

        [Tooltip("Image affichée quand l'étape est Discovered (cadenas argenté).")]
        public GameObject discoveredIcon;

        [Tooltip("Image affichée quand l'étape est Resolved (pas de cadenas).")]
        public GameObject resolvedIcon;

        [Tooltip("Optionnel : Image principale dont on change la couleur selon l'état.")]
        public Image backgroundImage;

        public Color lockedColor     = new Color(0.25f, 0.25f, 0.25f);
        public Color discoveredColor = new Color(0.75f, 0.75f, 0.80f);
        public Color resolvedColor   = new Color(0.30f, 0.85f, 0.45f);

        private StepBehaviour boundStep;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClicked);
        }

        public void Bind(StepBehaviour step)
        {
            boundStep = step;
            Refresh();
        }

        public void Refresh()
        {
            if (boundStep == null) return;

            if (lockedIcon     != null) lockedIcon.SetActive(boundStep.CurrentState == StepState.Locked);
            if (discoveredIcon != null) discoveredIcon.SetActive(boundStep.CurrentState == StepState.Discovered);
            if (resolvedIcon   != null) resolvedIcon.SetActive(boundStep.CurrentState == StepState.Resolved);

            if (backgroundImage != null)
            {
                backgroundImage.color = boundStep.CurrentState switch
                {
                    StepState.Locked     => lockedColor,
                    StepState.Discovered => discoveredColor,
                    StepState.Resolved   => resolvedColor,
                    _                    => backgroundImage.color
                };
            }
        }

        private void OnClicked()
        {
            if (boundStep == null) return;

            switch (boundStep.CurrentState)
            {
                case StepState.Locked:
                    // Aucun feedback : l'étape n'a pas encore été découverte.
                    break;

                case StepState.Discovered:
                    // Affiche les coordonnées / l'emplacement de l'étape pour aider
                    // le joueur à la retrouver. Pour le moment, log de debug — le
                    // hook UI réel (mini-carte / ping monde) sera branché plus tard.
                    Debug.Log($"[Journal] Emplacement de \"{boundStep.stepData?.name}\" : {boundStep.transform.position}");
                    break;

                case StepState.Resolved:
                    // Réaffiche l'indice menant vers la prochaine étape.
                    if (boundStep.stepData != null && boundStep.stepData.initialClue != null)
                        Debug.Log($"[Journal] Rappel de l'indice initial : \"{boundStep.stepData.initialClue.text}\"");
                    break;
            }
        }
    }
}
