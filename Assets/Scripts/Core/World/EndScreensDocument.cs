using UnityEngine;
using UnityEngine.UIElements;
using EscapeGame.Core.Player;

// UnityEngine.UIElements definit aussi un type Cursor : on leve l'ambiguite.
using Cursor = UnityEngine.Cursor;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Version UI Toolkit des ecrans de fin (victoire / game over).
    /// Remplace les canvas FelicitationsCanvas et GameOverCanvas.
    /// <see cref="EndGameManager"/> garde toute la logique de jeu et se contente
    /// d'appeler <see cref="ShowVictory"/> ou <see cref="ShowGameOver"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndScreensDocument : MonoBehaviour
    {
        [Tooltip("Relance la partie / charge la scene suivante. Si null, recherche au demarrage.")]
        public GameRestarter restarter;

        private VisualElement victoryScreen;
        private VisualElement gameoverScreen;
        private Label victoryReward;
        private Label gameoverReward;
        private Button victoryPlayButton;

        private bool isShowing;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            victoryScreen = root.Q<VisualElement>("victory-screen");
            gameoverScreen = root.Q<VisualElement>("gameover-screen");
            victoryReward = root.Q<Label>("victory-reward");
            gameoverReward = root.Q<Label>("gameover-reward");
            victoryPlayButton = root.Q<Button>("btn-victory-play");

            if (restarter == null)
                restarter = FindFirstObjectByType<GameRestarter>(FindObjectsInactive.Include);

            var vReplay = root.Q<Button>("btn-victory-replay");
            if (vReplay != null) vReplay.clicked += Restart;

            var gReplay = root.Q<Button>("btn-gameover-replay");
            if (gReplay != null) gReplay.clicked += Restart;

            if (victoryPlayButton != null) victoryPlayButton.clicked += LoadNextScene;

            SetVisible(false);
        }

        private void OnDisable()
        {
            // Un ecran de fin ouvert lors d'un changement de scene ne doit pas
            // laisser le compteur UIState desequilibre (sinon inputs bloques).
            if (!isShowing) return;
            isShowing = false;
            UIState.SetUIClosed();
        }

        // ====================================================================
        // API appelee par EndGameManager
        // ====================================================================

        public void ShowVictory(string rewardText)
        {
            if (victoryReward != null) victoryReward.text = rewardText;

            // Le bouton "Jouer au jeu" n'a de sens que si une scene suivante est
            // configuree (cas du tutoriel). Sinon il reste masque.
            bool hasNextScene = restarter != null && !string.IsNullOrEmpty(restarter.sceneToLoad);
            if (victoryPlayButton != null)
            {
                if (hasNextScene) victoryPlayButton.RemoveFromClassList("hidden");
                else victoryPlayButton.AddToClassList("hidden");
            }

            Show(victoryScreen);
        }

        public void ShowGameOver(string rewardText)
        {
            if (gameoverReward != null) gameoverReward.text = rewardText;
            Show(gameoverScreen);
        }

        // ====================================================================
        // Interne
        // ====================================================================

        private void Show(VisualElement screen)
        {
            SetVisible(true);
            if (victoryScreen != null) victoryScreen.AddToClassList("hidden");
            if (gameoverScreen != null) gameoverScreen.AddToClassList("hidden");
            if (screen != null) screen.RemoveFromClassList("hidden");

            if (!isShowing)
            {
                isShowing = true;
                UIState.SetUIOpen();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetVisible(bool visible)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        // Unity/l'OS reinitialisent l'etat du curseur au retour de focus (alt-tab).
        // Tant qu'un ecran de fin est affiche, on le reaffirme.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !isShowing) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Restart()
        {
            if (restarter != null) restarter.RestartGame();
        }

        private void LoadNextScene()
        {
            if (restarter != null) restarter.LoadConfiguredScene();
        }
    }
}
