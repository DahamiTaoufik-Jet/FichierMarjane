using UnityEngine;

namespace EscapeGame.Routes.Data
{
    /// <summary>
    /// Contenu multimédia d'un indice (texte, image, son).
    /// Utilisé comme "indice initial" d'une étape : ce que le joueur voit / entend
    /// lorsque l'étape précédente est résolue afin de le guider vers la suivante.
    /// </summary>
    [System.Serializable]
    public class ClueContent
    {
        [TextArea(2, 6)]
        [Tooltip("Texte de l'indice affiché au joueur.")]
        public string text;

        [Tooltip("Visuel optionnel accompagnant l'indice.")]
        public Sprite image;

        [Tooltip("Son optionnel joué lors de la révélation.")]
        public AudioClip audio;

        /// <summary>
        /// Vrai si l'indice est totalement vide. Une étape avec un indice initial
        /// vide est considérée comme un début de route.
        /// </summary>
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(text) && image == null && audio == null;
    }
}
