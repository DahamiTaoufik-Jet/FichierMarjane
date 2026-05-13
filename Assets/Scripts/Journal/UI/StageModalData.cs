using EscapeGame.Bonuses.Data;
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
        /// Conserve pour compatibilite avec StageModalView (centre).
        /// Null pour les PositionalScan : leur snapshot va dans InitialClue.image.
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
            // On clone le ClueContent pour pouvoir injecter le snapshot sans
            // modifier le ScriptableObject original.
            if (step.stepData != null && step.stepData.initialClue != null)
            {
                data.InitialClue = new ClueContent();
                data.InitialClue.text = step.stepData.initialClue.text;
                data.InitialClue.image = step.stepData.initialClue.image;
                data.InitialClue.audio = step.stepData.initialClue.audio;
            }
            else
            {
                data.InitialClue = new ClueContent();
            }

            // Si c'est un PositionalScan, le snapshot va dans PuzzleSnapshot
            // pour s'afficher dans l'onglet Enigme via enigmeImage.
            var positional = step as PositionalScanPuzzleStep;
            if (positional != null && positional.snapshot != null)
            {
                data.PuzzleSnapshot = positional.snapshot;
            }

            // Zone enigme (seulement si c'est un Puzzle avec question textuelle)
            if (data.StepType == StepType.Puzzle && step.stepData != null)
            {
                if (!string.IsNullOrEmpty(step.stepData.puzzleQuestion))
                {
                    // Si chiffre ET pas encore dechiffre par le bonus :
                    // on montre uniquement la version chiffree.
                    // Si dechiffre (ou pas chiffre) : on montre la question claire.
                    bool isEncrypted = step.stepData.puzzleEncrypted
                        && !string.IsNullOrEmpty(step.stepData.puzzleEncryptedQuestion);
                    bool isDecrypted = isEncrypted
                        && DecryptionTracker.IsDecrypted(step.stepData.stepId);

                    if (isEncrypted && !isDecrypted)
                    {
                        data.PuzzleEncryptedQuestion = step.stepData.puzzleEncryptedQuestion;
                    }
                    else
                    {
                        data.PuzzleQuestion = step.stepData.puzzleQuestion;
                    }
                }
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
                        if (next != null)
                        {
                            // Clone le ClueContent du suivant
                            data.NextClue = new ClueContent();
                            if (next.stepData != null && next.stepData.initialClue != null)
                            {
                                data.NextClue.text = next.stepData.initialClue.text;
                                data.NextClue.image = next.stepData.initialClue.image;
                                data.NextClue.audio = next.stepData.initialClue.audio;
                            }

                            // Si le suivant est un PositionalScan, son snapshot
                            // devient l'image de l'indice suivant
                            var nextPositional = next as PositionalScanPuzzleStep;
                            if (nextPositional != null && nextPositional.snapshot != null)
                            {
                                data.NextClue.image = nextPositional.snapshot;
                            }
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
