using System.Collections.Generic;
using UnityEngine;

namespace EscapeGame.Inventory.Data
{
    /// <summary>
    /// Mapping char -> LetterItem permettant au generateur de routes de
    /// retrouver l'asset LetterItem correspondant a chaque lettre du mot
    /// de passe choisi.
    ///
    /// Le designer cree un asset alphabet (en general un seul par jeu) et
    /// y glisse les LetterItem qu'il a prepares (avec icone, lettre, etc.).
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Inventory/LetterAlphabet", fileName = "LetterAlphabet")]
    public class LetterAlphabet : ScriptableObject
    {
        [Tooltip("Tous les LetterItem disponibles pour le jeu (un par lettre).")]
        public List<LetterItem> letters = new List<LetterItem>();

        /// <summary>
        /// Renvoie le LetterItem correspondant au char <paramref name="c"/>
        /// (insensible a la casse), ou null si introuvable.
        /// </summary>
        public LetterItem GetLetter(char c)
        {
            char target = char.ToUpperInvariant(c);
            for (int i = 0; i < letters.Count; i++)
            {
                if (letters[i] == null) continue;
                if (char.ToUpperInvariant(letters[i].letter) == target) return letters[i];
            }
            return null;
        }
    }
}
