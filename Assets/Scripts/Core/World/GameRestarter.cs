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

        private void Awake()
        {
            if (restartButtons == null) return;
            for (int i = 0; i < restartButtons.Length; i++)
            {
                if (restartButtons[i] != null)
                    restartButtons[i].onClick.AddListener(RestartGame);
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
    }
}
