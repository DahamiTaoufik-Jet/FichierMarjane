namespace EscapeGame.Core.Player
{
    /// <summary>
    /// Etat statique des UI de gameplay. Quand un panneau est ouvert
    /// (journal, inventaire bonus, enigme textuelle en saisie...),
    /// les scripts de mouvement / visee / scan consultent
    /// <see cref="IsAnyUIOpen"/> pour se desactiver.
    ///
    /// Utilise un compteur pour supporter les UIs imbriquees
    /// (ex : dechiffreur ouvre le journal depuis l'inventaire).
    /// </summary>
    public static class UIState
    {
        private static int openCount = 0;

        /// <summary>True si au moins une UI bloquante est ouverte.</summary>
        public static bool IsAnyUIOpen => openCount > 0;

        /// <summary>True quand un champ texte capture le clavier (empeche les raccourcis).</summary>
        public static bool IsInputFieldActive { get; set; }

        /// <summary>Appele quand une UI s'ouvre. Incremente le compteur.</summary>
        public static void SetUIOpen()
        {
            openCount++;
        }

        /// <summary>Appele quand une UI se ferme. Decremente le compteur (min 0).</summary>
        public static void SetUIClosed()
        {
            openCount--;
            if (openCount < 0) openCount = 0;
        }

        /// <summary>Reset complet (changement de scene).</summary>
        public static void Clear()
        {
            openCount = 0;
            IsInputFieldActive = false;
        }
    }
}
