namespace EscapeGame.Routes.Data
{
    /// <summary>
    /// Type d'étape selon le cahier des charges :
    /// - Clue  : un simple indice (auto-résolu au scan).
    /// - Puzzle: une énigme exigeant une interaction spécifique pour être résolue.
    /// </summary>
    public enum StepType
    {
        Clue,
        Puzzle
    }
}
