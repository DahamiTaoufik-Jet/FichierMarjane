using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EscapeGame.Journal.UI
{
    public class TutorialView : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Bouton pour fermer le tutoriel et lancer le jeu.")]
        public Button playButton;

        [Header("Scene")]
        [Tooltip("Nom de la scene de jeu a charger.")]
        public string gameSceneName = "SampleScene";

        public static bool IsTutorialActive { get; set; }

        private void Start()
        {
            IsTutorialActive = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playButton != null)
                playButton.onClick.AddListener(LoadGameScene);
        }

        private void LoadGameScene()
        {
            IsTutorialActive = false;
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
