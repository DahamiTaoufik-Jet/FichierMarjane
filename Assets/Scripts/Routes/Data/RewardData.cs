using UnityEngine;

namespace EscapeGame.Routes.Data
{
    /// <summary>
    /// Base abstraite des récompenses délivrées au joueur lorsqu'une étape finale
    /// (IsEnd == true) est résolue. Les implémentations concrètes (LetterReward,
    /// BonusReward, …) seront ajoutées dans le module Inventory à l'étape 4.
    /// </summary>
    public abstract class RewardData : ScriptableObject
    {
        [Header("Reward UI Data")]
        public string rewardName;
        [TextArea]
        public string description;
        public Sprite icon;
    }
}
