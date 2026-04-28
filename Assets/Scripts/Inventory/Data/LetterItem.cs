using UnityEngine;

namespace EscapeGame.Inventory.Data
{
    /// <summary>
    /// Lettre obtenue à la fin d'une route. Plusieurs lettres peuvent par exemple
    /// reconstituer un mot final dans la progression du jeu.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Inventory/LetterItem", fileName = "LetterItem")]
    public class LetterItem : ItemData
    {
        [Header("Lettre")]
        [Tooltip("Caractère effectif (utilisé par la mécanique de mot final).")]
        public char letter;
    }
}
