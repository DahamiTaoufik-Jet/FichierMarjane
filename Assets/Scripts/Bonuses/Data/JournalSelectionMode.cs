using System;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Systeme generique de selection dans le journal.
    /// Utilise par les bonus qui ouvrent le journal et laissent le joueur
    /// choisir un bloc (Dechiffreur, Resolveur, etc.).
    ///
    /// Chaque bonus fournit :
    /// - un predicat d'eligibilite (quels blocs sont cliquables)
    /// - une action a executer quand le joueur clique un bloc eligible
    /// - un callback de fin (consommer le bonus, fermer le journal)
    /// </summary>
    public static class JournalSelectionMode
    {
        /// <summary>True quand le journal est en mode selection bonus.</summary>
        public static bool IsActive { get; private set; }

        /// <summary>Couleur a utiliser pour les blocs eligibles (index dans une palette).</summary>
        public static SelectionColorType ColorType { get; private set; }

        /// <summary>Predicat : le step est-il eligible a la selection ?</summary>
        private static Func<StepBehaviour, bool> eligibilityCheck;

        /// <summary>Action executee sur le step selectionne.</summary>
        private static Action<StepBehaviour> onStepSelected;

        /// <summary>Callback de fin (consommer bonus, fermer journal).</summary>
        private static Action onComplete;

        /// <summary>
        /// Entre en mode selection.
        /// </summary>
        /// <param name="isEligible">Predicat pour filtrer les blocs cliquables.</param>
        /// <param name="onSelected">Action sur le step choisi (dechiffrer, resoudre...).</param>
        /// <param name="onDone">Callback de fin (consommer bonus, fermer journal).</param>
        /// <param name="colorType">Type de couleur pour les blocs eligibles.</param>
        public static void Enter(
            Func<StepBehaviour, bool> isEligible,
            Action<StepBehaviour> onSelected,
            Action onDone,
            SelectionColorType colorType = SelectionColorType.Gold)
        {
            IsActive = true;
            eligibilityCheck = isEligible;
            onStepSelected = onSelected;
            onComplete = onDone;
            ColorType = colorType;
        }

        /// <summary>Quitte le mode selection sans agir (annulation).</summary>
        public static void Exit()
        {
            IsActive = false;
            eligibilityCheck = null;
            onStepSelected = null;
            onComplete = null;
        }

        /// <summary>Verifie si un step est eligible selon le predicat actif.</summary>
        public static bool IsEligible(StepBehaviour step)
        {
            if (!IsActive || eligibilityCheck == null || step == null) return false;
            return eligibilityCheck(step);
        }

        /// <summary>
        /// Appele par StageNodeView quand le joueur clique un bloc eligible.
        /// Execute l'action sur le step, puis invoque le callback de fin.
        /// </summary>
        public static void Select(StepBehaviour step)
        {
            if (!IsActive || step == null) return;

            // Executer l'action specifique au bonus
            onStepSelected?.Invoke(step);

            // Callback de fin (consommer, fermer journal)
            var cb = onComplete;
            Exit();
            cb?.Invoke();
        }
    }

    /// <summary>Type de couleur pour les blocs eligibles en mode selection.</summary>
    public enum SelectionColorType
    {
        /// <summary>Jaune dore (Dechiffreur).</summary>
        Gold,
        /// <summary>Vert (Resolveur).</summary>
        Green
    }
}
