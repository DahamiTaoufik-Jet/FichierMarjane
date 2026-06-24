using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace EscapeGame.Journal.UI
{
    public class TutorialBootstrap : MonoBehaviour
    {
        [Tooltip("Le panelRoot du JournalView (le panel qui est desactive au Start).")]
        public GameObject journalPanel;

        private void Awake()
        {
            TutorialView.IsTutorialActive = true;
        }

        private IEnumerator Start()
        {
            yield return null;

            var inputModule = FindAnyObjectByType<InputSystemUIInputModule>();
            if (inputModule != null && inputModule.actionsAsset != null)
            {
                var uiMap = inputModule.actionsAsset.FindActionMap("UI");
                if (uiMap != null && !uiMap.enabled)
                    uiMap.Enable();
            }

            if (journalPanel != null)
                journalPanel.SetActive(true);
        }
    }
}
