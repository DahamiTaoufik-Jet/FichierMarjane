namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// État d'une étape, miroir des trois visuels du journal de progression :
    /// - Locked     : bloc avec cadenas (étape pas encore découverte)
    /// - Discovered : bloc avec cadenas argenté (découverte mais non résolue)
    /// - Resolved   : bloc sans cadenas (étape résolue)
    /// L'ordre numérique permet des comparaisons (state >= Discovered, etc.).
    /// </summary>
    public enum StepState
    {
        Locked = 0,
        Discovered = 1,
        Resolved = 2
    }
}
