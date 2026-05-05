using EscapeGame.Routes.Data;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Donnees aggregees pour le modal du journal.
    /// Construit a partir d'un StepBehaviour quand le joueur clique sur un bloc.
    /// </summary>
    public class StageModalData
    {
        /// <summary>Etat du bloc (Locked / Discovered / Resolved).</summary>
        public StepState State;

        /// <summary>Type du step (Clue ou Puzzle).</summary>
        public StepType StepType;

        // ==== Colonne gauche : indice initial ====

        /// <summary>Indice initial de CE bloc (texte + sprite + audio).</summary>
        public ClueContent InitialClue;

        // ==== Colonne centre : zone enigme (remplie seulement si Puzzle) ====

        /// <summary>Question de l'enigme textuelle (null si pas TextPuzzle).</summary>
        public string PuzzleQuestion;

        /// <summary>Version chiffree de la question (null si non eligible).</summary>
        public string PuzzleEncryptedQuestion;

        /// <summary>
        /// Snapshot de la zone a observer (pour PositionalScanPuzzle).
        /// Ouvert via un bouton "Voir Photo" dans un panel depliable.
        /// Null tant que le systeme de snapshot n'est pas pret.
        /// </summary>
        public UnityEngine.Sprite PuzzleSnapshot;

        // ==== Colonne droite : indice vers la suite ====

        /// <summary>
        /// Indice vers le bloc suivant (= initialClue du prochain step).
        /// Rempli uniquement si ce bloc est Resolved ET qu'il existe un suivant.
        /// </summary>
        public ClueContent NextClue;

        /// <summary>
        /// Construit le DTO depuis un StepBehaviour en contexte.
        /// </summary>
        public static StageModalData Build(StepBehaviour step)
        {
            if (step == null) return null;

            var data = new StageModalData();
            data.State = step.CurrentState;
            data.StepType = step.stepData != null ? step.stepData.type : StepType.Clue;

            // Indice initial de ce bloc (texte OU image OU les deux)
            if (step.stepData != null && step.stepData.initialClue != null)
                data.InitialClue = step.stepData.initialClue;

            // Zone enigme (seulement si c'est un Puzzle)
            if (data.StepType == StepType.Puzzle && step.stepData != null)
            {
                // TextPuzzle : question + chiffre
                if (!string.IsNullOrEmpty(step.stepData.puzzleQuestion))
                {
                    data.PuzzleQuestion = step.stepData.puzzleQuestion;

                    if (step.stepData.puzzleEncrypted
                        && !string.IsNullOrEmpty(step.stepData.puzzleEncryptedQuestion))
                    {
                        data.PuzzleEncryptedQuestion = step.stepData.puzzleEncryptedQuestion;
                    }
                }

                // Snapshot (placeholder pour plus tard)
                data.PuzzleSnapshot = null;
            }

            // Indice vers le suivant (si resolu)
            if (step.CurrentState == StepState.Resolved)
            {
                var rm = RouteManager.Instance;
                if (rm != null)
                {
                    var route = FindRoute(rm, step);
                    if (route != null)
                    {
                        var next = route.GetNext(step);
                        if (next != null && next.stepData != null
                            && next.stepData.initialClue != null)
                        {
                            data.NextClue = next.stepData.initialClue;
                        }
                    }
                }
            }

            return data;
        }

        private static RouteRuntime FindRoute(RouteManager rm, StepBehaviour step)
        {
            for (int i = 0; i < rm.Routes.Count; i++)
            {
                var route = rm.Routes[i];
                for (int j = 0; j < route.Steps.Count; j++)
                {
                    if (route.Steps[j] == step) return route;
                }
            }
            return null;
        }
    }
}
