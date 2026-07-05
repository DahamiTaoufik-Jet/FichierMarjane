using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using EscapeGame.Core.Player;
using EscapeGame.Bonuses.Data;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Relance la partie depuis zero : remet a plat les etats statiques qui
    /// survivent au rechargement de scene, puis recharge la scene active.
    /// Branche les boutons "Rejouer" des ecrans de victoire / defaite.
    /// </summary>
    public class GameRestarter : MonoBehaviour
    {
        [Tooltip("Boutons 'Rejouer' (Felicitations, Game Over). Cables ici, listener ajoute au demarrage.")]
        public Button[] restartButtons;

        [Header("Chargement d'une autre scene")]
        [Tooltip("Boutons qui chargent 'sceneToLoad' (ex. 'Jouer' sur la victoire du tutoriel -> scene principale).")]
        public Button[] loadSceneButtons;

        [Tooltip("Nom de la scene chargee par loadSceneButtons (ex. SampleScene). Doit etre dans les Build Settings.")]
        public string sceneToLoad = "";

        private void Awake()
        {
            if (restartButtons != null)
            {
                for (int i = 0; i < restartButtons.Length; i++)
                {
                    if (restartButtons[i] != null)
                        restartButtons[i].onClick.AddListener(RestartGame);
                }
            }

            if (loadSceneButtons != null)
            {
                for (int i = 0; i < loadSceneButtons.Length; i++)
                {
                    if (loadSceneButtons[i] != null)
                        loadSceneButtons[i].onClick.AddListener(LoadConfiguredScene);
                }
            }
        }

        /// <summary>Reset complet + rechargement de la scene active.</summary>
        public void RestartGame()
        {
            // Etats statiques a remettre a zero (ils survivent au LoadScene).
            UIState.Clear();
            JournalSelectionMode.Exit();
            DecryptionTracker.Clear();
            PositionalScanPuzzleStep.ResetSpotCount();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        /// <summary>
        /// Reset des etats statiques puis chargement de <see cref="sceneToLoad"/>
        /// (ex. bouton 'Jouer' de l'ecran de victoire du tutoriel -> scene principale).
        /// </summary>
        public void LoadConfiguredScene()
        {
            if (string.IsNullOrEmpty(sceneToLoad)) return;

            UIState.Clear();
            JournalSelectionMode.Exit();
            DecryptionTracker.Clear();
            PositionalScanPuzzleStep.ResetSpotCount();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
