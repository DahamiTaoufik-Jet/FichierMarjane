using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Inventory.Data;

namespace EscapeGame.Routes.Data
{
    /// <summary>
    /// Configuration du generateur procedural de routes.
    /// Centralise tous les pools (steps, bonus, mots de passe) et les parametres
    /// (longueur, max usage par step, region cible).
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Routes/RouteGeneratorConfig",
                     fileName = "RouteGeneratorConfig")]
    public class RouteGeneratorConfig : ScriptableObject
    {
        [Header("Pool de steps")]
        [Tooltip("Toutes les StepData candidates pour la generation des routes.")]
        public List<StepData> stepPool = new List<StepData>();

        [Header("Mot de passe (lettres distribuees aux fins de route)")]
        [Tooltip("Liste des mots candidats. Le generateur en choisit un dont la longueur " +
                 "est >= minPasswordLength et <= au nombre de routes generees.")]
        public List<string> passwordWords = new List<string>();

        [Tooltip("Longueur minimale exigee du mot de passe.")]
        [Min(1)] public int minPasswordLength = 4;

        [Tooltip("Alphabet (mapping char -> LetterItem). Necessaire pour distribuer les lettres.")]
        public LetterAlphabet letterAlphabet;

        [Header("Pool de bonus (recompense pour les routes sans lettre)")]
        [Tooltip("Pool aleatoire dans lequel le generateur tire un bonus pour les " +
                 "fins de route non couvertes par une lettre du mot de passe.")]
        public List<BonusItem> bonusPool = new List<BonusItem>();

        [Header("Generation")]
        [Tooltip("Longueur minimale d'une route (entree + fin = 2 minimum).")]
        [Min(2)] public int minRouteLength = 3;

        [Tooltip("Longueur maximale d'une route.")]
        [Min(2)] public int maxRouteLength = 5;

        [Tooltip("Nombre maximum d'utilisation d'une meme StepData dans toute la generation.")]
        [Min(1)] public int maxStepUsage = 2;

        [Tooltip("Si renseigne, ne generera des routes que dans cette region. Sinon, " +
                 "toutes les regions sont utilisees indifferemment.")]
        public string regionFilter;

        [Tooltip("Seed pour la generation. Si <= 0, utilise un seed aleatoire (recommande).")]
        public int seed = 0;

        private void OnValidate()
        {
            if (maxRouteLength < minRouteLength) maxRouteLength = minRouteLength;
        }
    }
}
