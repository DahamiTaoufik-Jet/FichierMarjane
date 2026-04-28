using UnityEngine;

namespace EscapeGame.Routes.Data
{
    /// <summary>
    /// Brique élémentaire utilisée par le générateur procédural de routes.
    /// Une StepData ne porte AUCUNE notion de rôle (entrée / intermédiaire / fin)
    /// ni de récompense : ces deux dimensions sont décidées au runtime par le
    /// ProceduralRouteGenerator selon les placeholders disponibles et le pool
    /// de récompenses configuré.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Routes/Step", fileName = "StepData")]
    public class StepData : ScriptableObject
    {
        [Header("Identite")]
        [Tooltip("Identifiant unique de la step (utilise pour debug / journal).")]
        public string stepId;

        [Header("Type")]
        public StepType type = StepType.Clue;

        [Header("Indice initial")]
        [Tooltip("Indice qui guide le joueur vers cette step. Affiche par la step " +
                 "precedente au moment de sa resolution. Pour la step d'entree d'une " +
                 "route, peut etre laisse vide.")]
        public ClueContent initialClue;

        [Header("Placement")]
        [Tooltip("Contrainte de placement (Any / Region / Spot).")]
        public PlacementData placement;

        [Header("Prefabs")]
        [Tooltip("Prefab a instancier si type == Clue. Doit porter un ClueStep.")]
        public GameObject cluePrefab;

        [Tooltip("Prefab a instancier si type == Puzzle. Doit porter un PuzzleStep.")]
        public GameObject puzzlePrefab;

        /// <summary>Renvoie le prefab adapte au type de la step.</summary>
        public GameObject ResolvePrefab()
        {
            return type == StepType.Puzzle ? puzzlePrefab : cluePrefab;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(stepId)) stepId = name;
        }
    }
}
