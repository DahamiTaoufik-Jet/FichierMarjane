using UnityEngine;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Carte "recompense de coffre" en UI Toolkit. Remplace ChestRewardRevealView.
    /// Pilotee explicitement par <c>EndGameManager</c> (pas d'abonnement a
    /// l'inventaire), et previent quand la file est vide pour que l'ecran de fin
    /// s'affiche sans chevaucher la carte en cours.
    /// </summary>
    public class ChestRewardRevealDocument : RevealCardDocument<ChestRewardRevealDocument.Entry>
    {
        public struct Entry
        {
            public string title;
            public string subtitle;
        }

        /// <summary>Leve quand plus aucune carte n'est en cours.</summary>
        public System.Action OnRevealsFinished;

        protected override void Subscribe() { }
        protected override void Unsubscribe() { }

        /// <summary>Affiche une carte recompense (mise en file si une autre joue).</summary>
        public void Show(string title, string subtitle)
        {
            Enqueue(new Entry { title = title, subtitle = subtitle });
        }

        protected override void Fill(Entry e)
        {
            if (!string.IsNullOrEmpty(e.title)) Show(header, e.title);
            if (!string.IsNullOrEmpty(e.subtitle)) Show(this.title, e.subtitle);
        }

        protected override void OnQueueDrained()
        {
            if (OnRevealsFinished != null) OnRevealsFinished();
        }
    }
}
