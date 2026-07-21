using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Journal.UI;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Bonus Resolveur : ouvre le journal et laisse le joueur choisir
    /// UNE enigme deja revelee (Discovered) pour la resoudre de force.
    /// Utilise <see cref="JournalSelectionMode"/> pour le mode selection.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Bonuses/Resolveur", fileName = "ResolveurBonus")]
    public class ResolveurBonus : BonusItem
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
                Debug.LogWarning("[ResolveurBonus] Aucun journal trouve.");
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
                    // Eligible = Discovered (revele) + pas encore resolu
                    return step.CurrentState == StepState.Discovered;
                },
                onSelected: step =>
                {
                    step.ForceResolve();
                    Debug.Log($"[ResolveurBonus] Step '{step.stepData?.stepId}' resolu de force.");
                },
                onDone: () =>
                {
                    if (context.inventory != null)
                        context.inventory.RemoveItem(this);
                    exitSelection();
                },
                colorType: SelectionColorType.Green
            );

            openForSelection();
            Debug.Log("[ResolveurBonus] Journal ouvert en mode selection.");
        }
    }
}
