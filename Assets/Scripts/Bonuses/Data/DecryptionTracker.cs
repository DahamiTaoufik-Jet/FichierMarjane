using System;
using System.Collections.Generic;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Tracker statique des steps dont le message chiffre a ete revele
    /// par le bonus Dechiffreur. Consulte par <c>StageModalData.Build()</c>
    /// pour decider si la question claire est affichee.
    ///
    /// Gere aussi le **mode selection** : quand le joueur utilise un
    /// DechiffreurBonus, le journal s'ouvre et seuls les blocs chiffres
    /// non-dechiffres sont cliquables. Le clic appelle
    /// <see cref="SelectStep"/> qui dechiffre le bloc choisi et invoque
    /// le callback enregistre par le bonus.
    /// </summary>
    public static class DecryptionTracker
    {
        private static readonly HashSet<string> decryptedStepIds = new HashSet<string>();

        // ====================================================================
        // Mode selection (utilise par DechiffreurBonus + StageNodeView)
        // ====================================================================

        /// <summary>True quand le journal est en mode dechiffrement.</summary>
        public static bool IsInSelectionMode { get; private set; }

        /// <summary>Callback appele apres le dechiffrement d'un bloc.</summary>
        private static Action onSelectionComplete;

        /// <summary>
        /// Entre en mode selection. Le journal doit etre ouvert AVANT l'appel.
        /// <paramref name="onComplete"/> est invoque une fois le bloc dechiffre.
        /// </summary>
        public static void EnterSelectionMode(Action onComplete)
        {
            IsInSelectionMode = true;
            onSelectionComplete = onComplete;
        }

        /// <summary>
        /// Quitte le mode selection sans dechiffrer (annulation / fermeture).
        /// </summary>
        public static void ExitSelectionMode()
        {
            IsInSelectionMode = false;
            onSelectionComplete = null;
        }

        /// <summary>
        /// Appele par <c>StageNodeView</c> quand le joueur clique un bloc
        /// compatible en mode selection. Dechiffre le step et invoque le
        /// callback du bonus.
        /// </summary>
        public static void SelectStep(string stepId)
        {
            if (!IsInSelectionMode) return;
            if (string.IsNullOrEmpty(stepId)) return;

            MarkDecrypted(stepId);

            var cb = onSelectionComplete;
            IsInSelectionMode = false;
            onSelectionComplete = null;
            cb?.Invoke();
        }

        /// <summary>
        /// Verifie si un step est eligible au dechiffrement :
        /// puzzleEncrypted == true, a une question chiffree, et pas encore dechiffre.
        /// </summary>
        public static bool IsEligibleForDecryption(
            bool puzzleEncrypted, string encryptedQuestion, string stepId)
        {
            if (!puzzleEncrypted) return false;
            if (string.IsNullOrEmpty(encryptedQuestion)) return false;
            if (IsDecrypted(stepId)) return false;
            return true;
        }

        // ====================================================================
        // Tracking dechiffrement
        // ====================================================================

        /// <summary>Marque un step comme dechiffre par son stepId.</summary>
        public static void MarkDecrypted(string stepId)
        {
            if (!string.IsNullOrEmpty(stepId))
                decryptedStepIds.Add(stepId);
        }

        /// <summary>Verifie si un step a ete dechiffre.</summary>
        public static bool IsDecrypted(string stepId)
        {
            if (string.IsNullOrEmpty(stepId)) return false;
            return decryptedStepIds.Contains(stepId);
        }

        /// <summary>Reset complet (changement de scene, nouvelle partie).</summary>
        public static void Clear()
        {
            decryptedStepIds.Clear();
            ExitSelectionMode();
        }
    }
}
