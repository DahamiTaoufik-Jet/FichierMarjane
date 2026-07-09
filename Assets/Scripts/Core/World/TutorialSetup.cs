using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Events;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Remplace le generateur procedural pour un niveau construit A LA MAIN
    /// (tutoriel). Enregistre aupres du <see cref="RouteManager"/> des routes
    /// definies dans l'Inspector, dans un ORDRE STRICT : la route N+1 n'est
    /// activee/enregistree qu'une fois la route N terminee. Configure aussi les
    /// enigmes de scan positionnel (spots + snapshot) et fixe le mot de passe.
    /// </summary>
    public class TutorialSetup : MonoBehaviour
    {
        [System.Serializable]
        public class PositionalScanConfig
        {
            [Tooltip("L'enigme de scan positionnel a configurer.")]
            public PositionalScanPuzzleStep step;

            [Tooltip("Position(s) valide(s) d'ou scanner (Transforms poses en scene). Une est piochee au hasard.")]
            public List<Transform> scanSpots = new List<Transform>();
        }

        [System.Serializable]
        public class TutorialRoute
        {
            public string routeId = "Route";
            public string displayName = "Route";

            [Tooltip("Parent des blocs de cette route. Active quand la route demarre. " +
                     "Laisse-le INACTIF en scene pour les routes 2 et 3 (ordre strict).")]
            public GameObject stepsParent;

            [Tooltip("Blocs de la route, DANS L'ORDRE (index 0 = entree, dernier = fin/recompense).")]
            public List<StepBehaviour> steps = new List<StepBehaviour>();

            [Tooltip("Recompense de fin de route (bonus Dechiffreur, ou LetterItem).")]
            public RewardData endReward;

            [Tooltip("Enigmes de scan positionnel de cette route a configurer avec leurs spots.")]
            public List<PositionalScanConfig> positionalConfigs = new List<PositionalScanConfig>();
        }

        [Header("Mot de passe des coffres")]
        [Tooltip("Ex. OR (2 lettres = 2 coffres).")]
        public string password = "OR";

        [Header("Routes (ordre strict : 1 -> 2 -> 3)")]
        public List<TutorialRoute> routes = new List<TutorialRoute>();

        private int nextRouteIndex = 0;
        private string lastRegisteredRouteId;

        private void OnEnable()
        {
            RouteEvents.RouteCompleted += OnRouteCompleted;
        }

        private void OnDisable()
        {
            RouteEvents.RouteCompleted -= OnRouteCompleted;
        }

        private void Start()
        {
            if (PasswordManager.Instance != null && !string.IsNullOrEmpty(password))
                PasswordManager.Instance.SetPassword(password);

            // Securite : desactive tous les parents de routes ; chacun sera active
            // a son tour au moment de l'enregistrement (ordre strict).
            for (int i = 0; i < routes.Count; i++)
                if (routes[i] != null && routes[i].stepsParent != null)
                    routes[i].stepsParent.SetActive(false);

            RegisterNext();
        }

        private void OnRouteCompleted(RouteRuntime route)
        {
            if (route == null) return;
            // On n'avance que si c'est bien la route en cours qui vient de se terminer.
            if (route.RouteId != lastRegisteredRouteId) return;
            RegisterNext();
        }

        private void RegisterNext()
        {
            if (nextRouteIndex >= routes.Count) return;
            if (RouteManager.Instance == null)
            {
                Debug.LogError("[TutorialSetup] RouteManager absent de la scene.");
                return;
            }

            var tr = routes[nextRouteIndex];
            nextRouteIndex++;
            if (tr == null) return;

            // Activer les blocs de la route
            if (tr.stepsParent != null) tr.stepsParent.SetActive(true);

            // Configurer les enigmes positionnelles (spots + snapshot)
            if (tr.positionalConfigs != null)
            {
                for (int i = 0; i < tr.positionalConfigs.Count; i++)
                {
                    var cfg = tr.positionalConfigs[i];
                    if (cfg == null || cfg.step == null) continue;

                    var poses = new List<Pose>();
                    if (cfg.scanSpots != null)
                        for (int j = 0; j < cfg.scanSpots.Count; j++)
                            if (cfg.scanSpots[j] != null)
                                poses.Add(new Pose(cfg.scanSpots[j].position, cfg.scanSpots[j].rotation));

                    cfg.step.Configure(poses);
                    cfg.step.CaptureSnapshot();
                }
            }

            lastRegisteredRouteId = tr.routeId;
            RouteManager.Instance.RegisterRoute(tr.routeId, tr.displayName, tr.steps, tr.endReward);

            // Masquer les meshes des blocs scannables (invisibles jusqu'au scan,
            // comme en jeu normal). Colliders conserves -> restent scannables.
            for (int i = 0; i < tr.steps.Count; i++)
                if (tr.steps[i] != null) tr.steps[i].HideMesh();

            Debug.Log($"[TutorialSetup] Route enregistree : '{tr.routeId}' ({tr.steps.Count} blocs).");
        }
    }
}
