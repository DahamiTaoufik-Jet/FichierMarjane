using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.UI;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// OUTIL DE DEBUG TEMPORAIRE - A SUPPRIMER AVANT LE BUILD FINAL.
    ///
    /// Declenche les cartes de revelation a la demande, pour ne pas avoir a
    /// terminer une route ou ouvrir un coffre a chaque test d'UI :
    ///   F1  : ecran de victoire (Felicitations)
    ///   F2  : ecran de defaite (Game Over)
    ///   F9  : complete une route porteuse de lettre (la lettre est donc gagnee
    ///         normalement, via le circuit de recompense habituel)
    ///   F10 : ajoute un bonus tire au hasard dans le pool
    ///   F12 : affiche une carte recompense de coffre a palier aleatoire
    ///         (F11 est reservee par macOS)
    ///
    /// Les touches sont ignorees si <see cref="enableInBuild"/> est faux et que
    /// l'on n'est pas dans l'editeur, pour eviter tout accident en production.
    /// </summary>
    public class DebugRevealTrigger : MonoBehaviour
    {
        [Header("Touches")]
        public Key victoryKey = Key.F1;
        public Key gameOverKey = Key.F2;
        public Key letterKey = Key.F9;
        public Key bonusKey = Key.F10;
        // F11 est capturee par macOS (affichage du bureau) : on utilise F12.
        public Key rewardKey = Key.F12;

        [Header("Securite")]
        [Tooltip("Laisser FAUX : les raccourcis ne repondent alors que dans l'editeur.")]
        public bool enableInBuild = false;

        [Header("References (auto-resolues si vides)")]
        public EscapeGame.Inventory.Runtime.Inventory inventory;
        public EndGameManager endGameManager;
        public ChestRewardRevealDocument rewardCard;
        public EndScreensDocument endScreens;
        public RouteGeneratorConfig config;

        private void Start()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<EscapeGame.Inventory.Runtime.Inventory>(FindObjectsInactive.Include);
            if (endGameManager == null)
                endGameManager = FindFirstObjectByType<EndGameManager>(FindObjectsInactive.Include);
            if (rewardCard == null)
                rewardCard = FindFirstObjectByType<ChestRewardRevealDocument>(FindObjectsInactive.Include);
            if (endScreens == null)
                endScreens = FindFirstObjectByType<EndScreensDocument>(FindObjectsInactive.Include);
            if (config == null)
            {
                var gen = FindFirstObjectByType<ProceduralRouteGenerator>(FindObjectsInactive.Include);
                if (gen != null) config = gen.config;
            }
        }

        private void Update()
        {
            if (!enableInBuild && !Application.isEditor) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb[victoryKey].wasPressedThisFrame) ShowVictory();
            else if (kb[gameOverKey].wasPressedThisFrame) ShowGameOver();
            else if (kb[letterKey].wasPressedThisFrame) GiveLetter();
            else if (kb[bonusKey].wasPressedThisFrame) GiveBonus();
            else if (kb[rewardKey].wasPressedThisFrame) GiveChestReward();
        }

        // ====================================================================
        // F1 / F2 : ecrans de fin
        // ====================================================================

        private void ShowVictory()
        {
            if (endScreens == null)
            {
                Debug.LogWarning("[DebugReveal] F1 : aucune EndScreensDocument en scene.");
                return;
            }
            string reward = "Recompense finale : " + RandomTier();
            if (endGameManager != null) reward = endGameManager.felicitationsRewardPrefix + RandomTier();

            Debug.Log("[DebugReveal] F1 -> ecran de victoire.");
            endScreens.ShowVictory(reward);
        }

        private void ShowGameOver()
        {
            if (endScreens == null)
            {
                Debug.LogWarning("[DebugReveal] F2 : aucune EndScreensDocument en scene.");
                return;
            }
            string reward = "Tu repars tout de meme avec : " + RandomTier();
            if (endGameManager != null) reward = endGameManager.gameOverRewardPrefix + RandomTier();

            Debug.Log("[DebugReveal] F2 -> ecran de game over.");
            endScreens.ShowGameOver(reward);
        }

        /// <summary>Un palier de recompense au hasard, pour varier le contenu affiche.</summary>
        private string RandomTier()
        {
            if (endGameManager == null || endGameManager.rewardsInOrder == null
                || endGameManager.rewardsInOrder.Length == 0)
                return "Bon de 300 dhs";
            return endGameManager.rewardsInOrder[Random.Range(0, endGameManager.rewardsInOrder.Length)];
        }

        // ====================================================================
        // F9 : completer une route porteuse de lettre
        // ====================================================================

        /// <summary>
        /// Cherche une route non terminee dont la recompense est une lettre et
        /// resout sa DERNIERE step. Le RouteManager cascade automatiquement sur
        /// les steps precedentes et distribue la lettre : on emprunte donc le
        /// circuit normal, pas un raccourci qui fausserait l'etat du jeu.
        /// </summary>
        private void GiveLetter()
        {
            var rm = RouteManager.Instance;
            if (rm == null) { Debug.LogWarning("[DebugReveal] Aucun RouteManager."); return; }

            for (int i = 0; i < rm.Routes.Count; i++)
            {
                var route = rm.Routes[i];
                if (route == null || route.State == RouteState.Completed) continue;
                if (!(route.EndReward is LetterItem)) continue;

                var last = LastUnresolvedTarget(route);
                if (last == null) continue;

                Debug.Log($"[DebugReveal] F9 -> route '{route.RouteId}' completee ({route.Steps.Count} steps).");
                last.ForceResolve();
                return;
            }

            // Plus aucune route a lettre : on donne une lettre directement pour
            // pouvoir quand meme tester la carte.
            if (config != null && config.letterAlphabet != null
                && config.letterAlphabet.letters.Count > 0 && inventory != null)
            {
                var pick = config.letterAlphabet.letters[Random.Range(0, config.letterAlphabet.letters.Count)];
                Debug.Log("[DebugReveal] F9 -> plus de route a lettre, ajout direct de '" + pick.letter + "'.");
                inventory.AddItem(pick);
                return;
            }

            Debug.LogWarning("[DebugReveal] F9 : aucune route a lettre ni alphabet disponible.");
        }

        /// <summary>Derniere step de la route (sa resolution cascade sur les precedentes).</summary>
        private static StepBehaviour LastUnresolvedTarget(RouteRuntime route)
        {
            for (int s = route.Steps.Count - 1; s >= 0; s--)
            {
                var step = route.Steps[s];
                if (step != null && !step.IsResolved) return step;
            }
            return null;
        }

        // ====================================================================
        // F10 : bonus aleatoire
        // ====================================================================

        private void GiveBonus()
        {
            if (inventory == null) { Debug.LogWarning("[DebugReveal] Aucun inventaire."); return; }

            var candidates = new List<BonusItem>();

            if (config != null && config.bonusPool != null)
            {
                for (int i = 0; i < config.bonusPool.Count; i++)
                {
                    var entry = config.bonusPool[i];
                    if (entry != null && entry.bonus != null) candidates.Add(entry.bonus);
                }
            }

            // Repli : les bonus effectivement distribues par les routes generees.
            if (candidates.Count == 0 && RouteManager.Instance != null)
            {
                var rm = RouteManager.Instance;
                for (int i = 0; i < rm.Routes.Count; i++)
                {
                    var b = rm.Routes[i] != null ? rm.Routes[i].EndReward as BonusItem : null;
                    if (b != null && !candidates.Contains(b)) candidates.Add(b);
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[DebugReveal] F10 : aucun bonus trouve (pool vide ?).");
                return;
            }

            var pick = candidates[Random.Range(0, candidates.Count)];
            Debug.Log("[DebugReveal] F10 -> bonus '" + pick.rewardName + "'.");
            inventory.AddItem(pick);
        }

        // ====================================================================
        // F12 : recompense de coffre aleatoire
        // ====================================================================

        private void GiveChestReward()
        {
            if (rewardCard == null)
            {
                Debug.LogWarning("[DebugReveal] F12 : aucune ChestRewardRevealDocument en scene.");
                return;
            }

            string title = "RECOMPENSE !";
            string label = "Bon de 300 dhs";

            if (endGameManager != null)
            {
                title = endGameManager.rewardTitle;
                var tiers = endGameManager.rewardsInOrder;
                if (tiers != null && tiers.Length > 0)
                    label = tiers[Random.Range(0, tiers.Length)];
            }

            Debug.Log("[DebugReveal] F12 -> recompense '" + label + "'.");
            rewardCard.Show(title, label);
        }
    }
}
