namespace EscapeGame.Routes.Data
{
    /// <summary>
    /// Contrainte de placement d'une étape :
    /// - Any    : la step accepte n'importe quel placeholder du type approprié.
    /// - Region : la step doit aller sur un placeholder dont le regionId correspond.
    /// - Spot   : la step doit aller sur un placeholder dont le spotId correspond exactement.
    /// </summary>
    public enum PlacementMode
    {
        Any,
        Region,
        Spot
    }
}
