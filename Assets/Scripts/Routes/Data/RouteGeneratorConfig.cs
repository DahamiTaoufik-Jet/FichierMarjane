using System;
using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Inventory.Data;

namespace EscapeGame.Routes.Data
{
    [Serializable]
    public class BonusPoolEntry
    {
        [Tooltip("Le bonus a distribuer.")]
        public BonusItem bonus;

        [Tooltip("Nombre maximum de fois que ce bonus peut etre distribue dans une generation.")]
        [Min(1)] public int maxCount = 1;
    }

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
        [Tooltip("Pool de bonus avec quantite max par type.")]
        public List<BonusPoolEntry> bonusPool = new List<BonusPoolEntry>();

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
