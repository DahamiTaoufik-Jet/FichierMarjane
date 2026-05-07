using System.Collections.Generic;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Tracker statique des steps dont le message chiffre a ete revele
    /// par le bonus Dechiffreur. Consulte par <c>StageModalData.Build()</c>
    /// pour decider si la question claire est affichee.
    /// </summary>
    public static class DecryptionTracker
    {
        private static readonly HashSet<string> decryptedStepIds = new HashSet<string>();

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

        /// <summary>Reset complet (changement de scene, nouvelle partie).</summary>
        public static void Clear()
        {
            decryptedStepIds.Clear();
        }
    }
}
