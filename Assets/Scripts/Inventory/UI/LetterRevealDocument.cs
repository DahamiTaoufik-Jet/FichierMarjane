using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Events;
using EscapeGame.Routes.Services;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Revelation "nouvelle lettre" en UI Toolkit. Remplace LetterRevealView.
    /// Toute l'animation vient de <see cref="RevealCardDocument{T}"/> : il ne
    /// reste ici que le contenu et la revelation differee de la position.
    /// </summary>
    public class LetterRevealDocument : RevealCardDocument<LetterItem>
    {
        [Header("Textes")]
        [Tooltip("Format de la position. {0} = numero (base 1).")]
        public string positionFormat = "Position {0}";
        public string memorizeLabel = "A MEMORISER !";
        public string unknownPositionText = "Nouvelle lettre !";

        [Tooltip("Delai avant d'afficher la position a memoriser.")]
        public float revealPositionDelay = 1f;

        [Header("Tutoriel")]
        [Tooltip("Si vrai, la TOUTE PREMIERE lettre attend une touche avant de s'animer, " +
                 "le temps que le joueur lise la consigne.")]
        public bool waitForInputOnFirst = false;
        public Key confirmKey = Key.Enter;

        protected override void Subscribe()
        {
            InventoryEvents.ItemAdded += HandleItemAdded;
        }

        protected override void Unsubscribe()
        {
            InventoryEvents.ItemAdded -= HandleItemAdded;
        }

        private void HandleItemAdded(ItemData item)
        {
            var letter = item as LetterItem;
            if (letter != null) Enqueue(letter);
        }

        protected override void Fill(LetterItem letter)
        {
            Show(big, char.ToUpper(letter.letter).ToString());

            // Reserve la place du texte de position AVANT l'animation : sinon la
            // carte grandirait d'un coup au moment de sa revelation. Le gabarit
            // fait deux lignes, comme le texte final.
            Reserve(sub, string.Format(positionFormat, 0) + "\n" + memorizeLabel);
        }

        /// <summary>
        /// Apres un court flottement, revele la position de la lettre dans le
        /// mot de passe (l'information a memoriser pour ouvrir les coffres).
        /// </summary>
        protected override IEnumerator DuringHold(LetterItem letter)
        {
            float wait = 0f;
            while (wait < revealPositionDelay)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            int pos = PasswordManager.Instance != null
                ? PasswordManager.Instance.GetPositionForLetter(letter.letter)
                : -1;

            Show(sub, pos >= 0
                ? string.Format(positionFormat, pos + 1) + "\n" + memorizeLabel
                : unknownPositionText);
        }

        /// <summary>
        /// Tutoriel : attend une pression de touche avant la premiere carte.
        /// La touche qui vient de valider l'enigme ne doit pas compter, d'ou
        /// l'attente du relachement avant d'ecouter une nouvelle pression.
        /// </summary>
        protected override IEnumerator BeforeFirst()
        {
            if (!waitForInputOnFirst) yield break;

            yield return null;
            while (Keyboard.current != null && Keyboard.current[confirmKey].isPressed)
                yield return null;

            while (true)
            {
                var kb = Keyboard.current;
                if (kb != null && kb[confirmKey].wasPressedThisFrame) yield break;
                yield return null;
            }
        }
    }
}
