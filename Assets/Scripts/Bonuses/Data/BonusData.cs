using UnityEngine;
using EscapeGame.Core.Player;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Strategy Pattern for bonuses. Defines how a bonus applies to a PlayerContext.
    /// </summary>
    public abstract class BonusData : ScriptableObject
    {
        [Header("Bonus UI Data")]
        public string bonusName;
        [TextArea]
        public string description;
        public Sprite iconSprite;

        /// <summary>
        /// Executes the specific bonus logic.
        /// e.g. PathFinder generates a line renderer, Dechiffreur modifies CluePanels.
        /// </summary>
        /// <param name="context">The player invoking the bonus</param>
        public abstract void Execute(PlayerContext context);
    }
}
