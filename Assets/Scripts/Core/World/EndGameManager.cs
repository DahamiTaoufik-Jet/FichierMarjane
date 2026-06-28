using UnityEngine;
using TMPro;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.UI;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Events;
using EscapeGame.Routes.Services;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Gere les recompenses de coffres et les ecrans de fin :
    ///  - a chaque coffre ouvert (PositionSolved), affiche une carte recompense
    ///    doree selon l'ORDRE d'ouverture (1er -> 300 dhs, etc.) ;
    ///  - a la completion du mot de passe (AllSolved), affiche les Felicitations ;
    ///  - a la perte par essais (GameLost), affiche le Game Over ;
    ///  - si le joueur a engage la phase coffres et epuise toutes ses lettres sans
    ///    finir, affiche un Game Over (avec la recompense max atteinte), APRES la
    ///    carte recompense en cours pour eviter le chevauchement.
    /// </summary>
    public class EndGameManager : MonoBehaviour
    {
        [Header("Recompenses (dans l'ordre d'ouverture)")]
        [Tooltip("Libelle affiche pour le Nieme coffre ouvert. Au-dela du dernier, on plafonne sur le dernier.")]
        public string[] rewardsInOrder =
        {
            "Bon de 300 dhs",
            "Bon de 700 dhs",
            "Objet d'une valeur de 1200 dhs",
            "Objet d'une valeur de 3000 dhs",
            "Objet d'une valeur de 5000 dhs"
        };

        [Tooltip("Titre affiche en haut de la carte recompense.")]
        public string rewardTitle = "RECOMPENSE !";

        [Header("References")]
        public ChestRewardRevealView rewardReveal;
        [Tooltip("Inventaire du joueur (pour detecter la rupture de lettres). Si null, recherche au demarrage.")]
        public EscapeGame.Inventory.Runtime.Inventory inventory;

        [Header("Ecran Felicitations (mot de passe complet)")]
        public GameObject felicitationsRoot;
        public TMP_Text felicitationsRewardText;
        public string felicitationsRewardPrefix = "Recompense finale : ";

        [Tooltip("Source audio pour le son de victoire.")]
        public AudioSource victoryAudioSource;
        [Tooltip("Son joue a l'affichage de l'ecran Felicitations (Total Victory).")]
        public AudioClip victorySound;

        [Header("Ecran Game Over (plus d'essais ou plus de lettres)")]
        public GameObject gameOverRoot;
        public TMP_Text gameOverRewardText;
        public string gameOverRewardPrefix = "Tu repars tout de meme avec : ";
        [Tooltip("Texte affiche quand aucun coffre n'a ete ouvert (game over brut).")]
        public string rawGameOverText = "Aucune recompense.";

        private int openedCount;
        private bool ended;
        private bool pendingOutOfLettersGameOver;

        private void Awake()
        {
            if (felicitationsRoot != null) felicitationsRoot.SetActive(false);
            if (gameOverRoot != null) gameOverRoot.SetActive(false);
            if (inventory == null)
                inventory = FindFirstObjectByType<EscapeGame.Inventory.Runtime.Inventory>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            PasswordManager.PositionSolved += OnPositionSolved;
            PasswordManager.AllSolved += OnAllSolved;
            PasswordManager.GameLost += OnGameLost;
            InventoryEvents.ItemRemoved += OnItemRemoved;
            if (rewardReveal != null) rewardReveal.OnRevealsFinished += OnRevealsFinished;
        }

        private void OnDisable()
        {
            PasswordManager.PositionSolved -= OnPositionSolved;
            PasswordManager.AllSolved -= OnAllSolved;
            PasswordManager.GameLost -= OnGameLost;
            InventoryEvents.ItemRemoved -= OnItemRemoved;
            if (rewardReveal != null) rewardReveal.OnRevealsFinished -= OnRevealsFinished;
        }

        // ====================================================================
        // Coffres
        // ====================================================================

        private void OnPositionSolved(int position, char letter)
        {
            if (ended) return;
            openedCount++;

            // Dernier coffre -> l'ecran Felicitations affichera la recompense
            // finale (evite le doublon carte + felicitations).
            if (PasswordManager.Instance != null && PasswordManager.Instance.IsAllSolved)
                return;

            if (rewardReveal != null)
                rewardReveal.Show(rewardTitle, GetRewardText(openedCount));
        }

        // Lettre retiree de l'inventaire (apres un depot reussi). Si la phase
        // coffres est engagee et qu'il ne reste plus aucune lettre sans avoir
        // tout resolu, on programme un Game Over a la fin de la carte recompense.
        private void OnItemRemoved(ItemData item)
        {
            if (ended || pendingOutOfLettersGameOver) return;
            if (!(item is LetterItem)) return;
            var pm = PasswordManager.Instance;
            if (pm == null || !pm.ChestPhaseCommitted || pm.IsAllSolved) return;

            if (LettersRemaining() == 0)
                pendingOutOfLettersGameOver = true;
        }

        private void OnRevealsFinished()
        {
            if (pendingOutOfLettersGameOver && !ended)
                ShowGameOver();
        }

        // ====================================================================
        // Ecrans de fin
        // ====================================================================

        private void OnAllSolved()
        {
            if (ended) return;
            ended = true;
            if (felicitationsRewardText != null)
                felicitationsRewardText.text = felicitationsRewardPrefix + GetRewardText(openedCount);
            if (victoryAudioSource != null && victorySound != null)
                victoryAudioSource.PlayOneShot(victorySound);
            ShowEndScreen(felicitationsRoot);
        }

        private void OnGameLost()
        {
            ShowGameOver();
        }

        private void ShowGameOver()
        {
            if (ended) return;
            ended = true;
            if (gameOverRewardText != null)
            {
                gameOverRewardText.text = openedCount > 0
                    ? gameOverRewardPrefix + GetRewardText(openedCount)
                    : rawGameOverText;
            }
            ShowEndScreen(gameOverRoot);
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private int LettersRemaining()
        {
            return inventory != null ? inventory.GetItemsOfType<LetterItem>().Count : 0;
        }

        private string GetRewardText(int count)
        {
            if (rewardsInOrder == null || rewardsInOrder.Length == 0 || count <= 0)
                return "";
            int idx = Mathf.Min(count - 1, rewardsInOrder.Length - 1);
            return rewardsInOrder[idx];
        }

        private void ShowEndScreen(GameObject root)
        {
            if (root == null) return;
            root.SetActive(true);
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
