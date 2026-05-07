using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Runtime;
using EscapeGame.Journal.UI;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Bonus Dechiffreur : ouvre le journal et laisse le joueur choisir
    /// UNE enigme chiffree a reveler. Le bonus est consomme uniquement
    /// apres que le joueur a clique un bloc compatible.
    ///
    /// Flux :
    /// 1. Execute() verifie qu'il existe au moins un bloc eligible
    /// 2. Trouve le JournalView, l'ouvre de force
    /// 3. Active le mode selection via DecryptionTracker
    /// 4. Le joueur clique un bloc chiffre dans StageNodeView
    /// 5. Le callback consomme le bonus et ferme le journal
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Bonuses/Dechiffreur", fileName = "DechiffreurBonus")]
    public class DechiffreurBonus : BonusItem
    {
        /// <summary>
        /// Force consumeOnUse a false car le Dechiffreur gere sa propre
        /// consommation dans le callback apres selection du bloc.
        /// </summary>
        private void Reset()
        {
            consumeOnUse = false;
        }

        public override void Execute(PlayerContext context)
        {
            if (context == null) return;

            // Verifier qu'il existe au moins une enigme eligible
            if (!HasAnyEligibleStep())
            {
                Debug.Log("[DechiffreurBonus] Aucune enigme chiffree a reveler.");
                return;
            }

            // Trouver le JournalView dans la scene
            var journalView = Object.FindAnyObjectByType<JournalView>();
            if (journalView == null)
            {
                Debug.LogWarning("[DechiffreurBonus] JournalView introuvable dans la scene.");
                return;
            }

            // Entrer en mode selection AVANT d'ouvrir le journal pour que
            // le Rebuild() colore les blocs eligibles en jaune dore.
            DecryptionTracker.EnterSelectionMode(() =>
            {
                Debug.Log("[DechiffreurBonus] Bloc dechiffre par le joueur.");

                // Consommer le bonus de l'inventaire
                if (context.inventory != null)
                    context.inventory.RemoveItem(this);

                // Fermer le journal
                journalView.ExitDecryptionMode();
            });

            // Ouvrir le journal de force (le Rebuild verra IsInSelectionMode = true)
            journalView.OpenForDecryption();

            Debug.Log("[DechiffreurBonus] Journal ouvert en mode selection.");
        }

        /// <summary>
        /// Verifie qu'au moins un step dans les routes actives est
        /// chiffre et pas encore dechiffre.
        /// </summary>
        private bool HasAnyEligibleStep()
        {
            var rm = RouteManager.Instance;
            if (rm == null) return false;

            for (int r = 0; r < rm.Routes.Count; r++)
            {
                var route = rm.Routes[r];
                for (int s = 0; s < route.Steps.Count; s++)
                {
                    var step = route.Steps[s];
                    if (step == null || step.stepData == null) continue;

                    if (DecryptionTracker.IsEligibleForDecryption(
                        step.stepData.puzzleEncrypted,
                        step.stepData.puzzleEncryptedQuestion,
                        step.stepData.stepId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
