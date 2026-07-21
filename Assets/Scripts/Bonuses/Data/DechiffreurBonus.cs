using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Journal.UI;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Bonus Dechiffreur : ouvre le journal et laisse le joueur choisir
    /// UNE enigme chiffree a reveler.
    /// Utilise <see cref="JournalSelectionMode"/> pour le mode selection.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Bonuses/Dechiffreur", fileName = "DechiffreurBonus")]
    public class DechiffreurBonus : BonusItem
    {
        private void Reset() { consumeOnUse = false; }

        public override void Execute(PlayerContext context)
        {
            if (context == null) return;

            // Journal en UI Toolkit si present, sinon l'ancien (scene tutoriel).
            var journalDoc = Object.FindAnyObjectByType<JournalDocument>();
            var journalView = journalDoc == null ? Object.FindAnyObjectByType<JournalView>() : null;
            if (journalDoc == null && journalView == null)
            {
                Debug.LogWarning("[DechiffreurBonus] Aucun journal trouve.");
                return;
            }

            System.Action openForSelection = journalDoc != null
                ? (System.Action)journalDoc.OpenForSelection : journalView.OpenForSelection;
            System.Action exitSelection = journalDoc != null
                ? (System.Action)journalDoc.ExitSelectionMode : journalView.ExitSelectionMode;

            // Entrer en mode selection AVANT d'ouvrir le journal
            JournalSelectionMode.Enter(
                isEligible: step =>
                {
                    if (step.stepData == null) return false;
                    return DecryptionTracker.IsEligibleForDecryption(
                        step.stepData.puzzleEncrypted,
                        step.stepData.puzzleEncryptedQuestion,
                        step.stepData.stepId);
                },
                onSelected: step =>
                {
                    DecryptionTracker.MarkDecrypted(step.stepData.stepId);
                    Debug.Log($"[DechiffreurBonus] Bloc '{step.stepData.stepId}' dechiffre.");
                },
                onDone: () =>
                {
                    if (context.inventory != null)
                        context.inventory.RemoveItem(this);
                    exitSelection();
                },
                colorType: SelectionColorType.Gold
            );

            openForSelection();
            Debug.Log("[DechiffreurBonus] Journal ouvert en mode selection.");
        }
    }
}
