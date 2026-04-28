using EscapeGame.Routes.Data;

namespace EscapeGame.Inventory.Data
{
    /// <summary>
    /// Base abstraite des objets stockables dans l'inventaire du joueur.
    /// Hérite de <see cref="RewardData"/> pour pouvoir être directement
    /// référencé comme récompense de fin de route.
    /// </summary>
    public abstract class ItemData : RewardData
    {
        // Hérite de rewardName / description / icon depuis RewardData.
    }
}
