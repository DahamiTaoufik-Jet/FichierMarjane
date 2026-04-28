namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// État global d'une route :
    /// - Inactive  : la route n'a pas encore été démarrée par le RouteManager.
    /// - Active    : la route est en cours (au moins une étape découverte, pas toutes résolues).
    /// - Completed : toutes les étapes sont résolues. Le journal l'affiche en "doré".
    /// </summary>
    public enum RouteState
    {
        Inactive = 0,
        Active = 1,
        Completed = 2
    }
}
