using UnityEngine;

namespace EscapeGame.Journal.UI
{
    public class TutorialButtonSync : MonoBehaviour
    {
        [Tooltip("Le bouton separe a synchroniser avec ce panel.")]
        public GameObject targetButton;

        private void OnEnable()
        {
            if (targetButton != null)
                targetButton.SetActive(true);
        }

        private void OnDisable()
        {
            if (targetButton != null)
                targetButton.SetActive(false);
        }
    }
}
