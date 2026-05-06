using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Inventory.Data;
using EscapeGame.Routes.Data;
using EscapeGame.Routes.Generation;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Orchestrateur du systeme procedural :
    /// 1. Decouvre les PlaceholderNodes en scene,
    /// 2. Demande au RouteGenerationPlanner de planifier toutes les routes,
    /// 3. Choisit un mot de passe (priorite sur les lettres) et distribue
    ///    les recompenses de fin (lettres + bonus en remplissage),
    /// 4. Instancie les prefabs aux placeholders choisis,
    /// 5. Enregistre chaque route aupres du RouteManager.
    /// </summary>
    public class ProceduralRouteGenerator : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("ScriptableObject decrivant le pool de steps, le pool de bonus, la liste des mots candidats, etc.")]
        public RouteGeneratorConfig config;

        [Tooltip("Si null, recherche RouteManager.Instance au demarrage.")]
        public RouteManager routeManager;

        [Tooltip("Si vrai, les PlaceholderNode utilises sont detruits apres instanciation.")]
        public bool cleanupUsedPlaceholders = true;

        [Tooltip("Lance la generation au Start.")]
        public bool generateOnStart = true;

        // Cache des ScanSpots de la scene pour la duree d'une generation, pour
        // injecter leurs poses dans les PositionalScanPuzzleStep instancies.
        private List<PlaceholderNode> scanSpotsCache;

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void Start()
        {
            if (generateOnStart) Generate();
        }

        [ContextMenu("Regenerate")]
        public void Generate()
        {
            if (config == null)
            {
                Debug.LogError("[ProceduralRouteGenerator] Aucun RouteGeneratorConfig assigne.");
                return;
            }
            if (routeManager == null) routeManager = RouteManager.Instance;
            if (routeManager == null)
            {
                Debug.LogError("[ProceduralRouteGenerator] Aucun RouteManager dans la scene.");
                return;
            }

            bonusUsage = new Dictionary<BonusItem, int>();

            // 1. Inventaire des placeholders
            var allNodes = FindObjectsByType<PlaceholderNode>(FindObjectsSortMode.None);
            var placeholders = new List<PlaceholderNode>(allNodes);

            // 1.b Cache des ScanSpots (consommes plus tard par les PositionalScanPuzzleStep)
            scanSpotsCache = new List<PlaceholderNode>();
            for (int i = 0; i < allNodes.Length; i++)
            {
                if (allNodes[i] != null && allNodes[i].nodeType == ProceduralNodeType.ScanSpot)
                    scanSpotsCache.Add(allNodes[i]);
            }

            // 2. Planification
            int? seed = config.seed > 0 ? (int?)config.seed : null;
            var planner = new RouteGenerationPlanner(
                config.minRouteLength, config.maxRouteLength, config.maxStepUsage, seed);

            var plans = planner.BuildPlans(config.stepPool, placeholders, config.regionFilter);
            if (plans.Count == 0)
            {
                Debug.LogWarning("[ProceduralRouteGenerator] Aucune route generee. " +
                                 "Verifie le pool de steps, la presence de PlaceholderNodes et leurs contraintes.");
                CleanupScanSpots();
                return;
            }

            // 3. Distribution des recompenses (lettres prioritaires, bonus en remplissage)
            AssignRewards(plans);

            // 4. Instanciation et enregistrement
            foreach (var plan in plans)
                InstantiateAndRegister(plan);

            // 5. Nettoyage final des ScanSpots (markeurs uniquement, leurs poses
            //    ont ete capturees par les steps positionnels concernes)
            CleanupScanSpots();

            Debug.Log($"[ProceduralRouteGenerator] {plans.Count} route(s) generee(s).");
        }

        private void CleanupScanSpots()
        {
            if (scanSpotsCache == null) return;
            for (int i = 0; i < scanSpotsCache.Count; i++)
            {
                if (scanSpotsCache[i] != null)
                    Destroy(scanSpotsCache[i].gameObject);
            }
            scanSpotsCache = null;
        }

        // ====================================================================
        // Distribution des recompenses
        // ====================================================================

        private void AssignRewards(List<RoutePlan> plans)
        {
            string password = ChoosePassword(plans.Count);
            int letterCount = string.IsNullOrEmpty(password) ? 0 : password.Length;

            for (int i = 0; i < plans.Count; i++)
            {
                if (i < letterCount && config.letterAlphabet != null)
                {
                    var letter = config.letterAlphabet.GetLetter(password[i]);
                    if (letter != null)
                    {
                        plans[i].EndReward = letter;
                        continue;
                    }
                    Debug.LogWarning($"[ProceduralRouteGenerator] Lettre '{password[i]}' " +
                                     $"absente du LetterAlphabet : route {i} retombe sur un bonus.");
                }
                plans[i].EndReward = PickRandomBonus();
            }

            if (!string.IsNullOrEmpty(password))
                Debug.Log($"[ProceduralRouteGenerator] Mot de passe genere : {password}");
        }

        private string ChoosePassword(int routeCount)
        {
            if (config.passwordWords == null || config.passwordWords.Count == 0)
            {
                Debug.LogWarning("[ProceduralRouteGenerator] Aucun mot dans passwordWords : aucune lettre distribuee.");
                return null;
            }

            var eligible = new List<string>();
            foreach (var w in config.passwordWords)
            {
                if (string.IsNullOrEmpty(w)) continue;
                if (w.Length >= config.minPasswordLength && w.Length <= routeCount)
                    eligible.Add(w);
            }

            if (eligible.Count == 0)
            {
                Debug.LogWarning($"[ProceduralRouteGenerator] Aucun mot eligible : " +
                                 $"routes={routeCount}, minPasswordLength={config.minPasswordLength}.");
                return null;
            }

            return eligible[Random.Range(0, eligible.Count)];
        }

        private Dictionary<BonusItem, int> bonusUsage;

        private BonusItem PickRandomBonus()
        {
            if (config.bonusPool == null || config.bonusPool.Count == 0) return null;

            if (bonusUsage == null)
                bonusUsage = new Dictionary<BonusItem, int>();

            var candidates = new List<BonusItem>();
            for (int i = 0; i < config.bonusPool.Count; i++)
            {
                var entry = config.bonusPool[i];
                if (entry == null || entry.bonus == null) continue;

                int used = 0;
                bonusUsage.TryGetValue(entry.bonus, out used);

                if (used < entry.maxCount)
                    candidates.Add(entry.bonus);
            }

            if (candidates.Count == 0) return null;

            var pick = candidates[Random.Range(0, candidates.Count)];

            if (!bonusUsage.ContainsKey(pick))
                bonusUsage[pick] = 0;
            bonusUsage[pick]++;

            return pick;
        }

        // ====================================================================
        // Instanciation et enregistrement
        // ====================================================================

        private void InstantiateAndRegister(RoutePlan plan)
        {
            var stepInstances = new List<StepBehaviour>(plan.Length);

            foreach (var assignment in plan.Assignments)
            {
                var prefab = assignment.StepData.ResolvePrefab();
                if (prefab == null)
                {
                    Debug.LogError($"[ProceduralRouteGenerator] Step '{assignment.StepData.stepId}' " +
                                   $"n'a pas de prefab pour son type ({assignment.StepData.type}).");
                    continue;
                }

                var go = Instantiate(prefab, assignment.Position, assignment.Rotation);
                var step = go.GetComponent<StepBehaviour>();
                if (step == null)
                {
                    Debug.LogError($"[ProceduralRouteGenerator] Le prefab '{prefab.name}' " +
                                   $"ne possede pas de StepBehaviour.");
                    Destroy(go);
                    continue;
                }

                step.stepData = assignment.StepData;
                step.routeId = plan.RouteId;
                stepInstances.Add(step);

                // Injection des positions de scan pour les enigmes positionnelles.
                // On le fait AVANT de detruire le placeholder car on a besoin de son
                // spotId pour matcher les ScanSpots.
                var positional = step as PositionalScanPuzzleStep;
                if (positional != null)
                {
                    InjectScanSpots(positional, assignment.Placeholder);
                    positional.CaptureSnapshot();
                }

                if (cleanupUsedPlaceholders && assignment.Placeholder != null)
                    Destroy(assignment.Placeholder.gameObject);
            }

            if (stepInstances.Count > 0)
                routeManager.RegisterRoute(plan.RouteId, plan.DisplayName, stepInstances, plan.EndReward);
        }

        /// <summary>
        /// Cherche dans le cache des ScanSpots ceux qui referencent le spotId du
        /// placeholder Puzzle utilise par cette step et passe leurs poses au step.
        /// </summary>
        private void InjectScanSpots(PositionalScanPuzzleStep step, PlaceholderNode placeholder)
        {
            if (placeholder == null)
            {
                Debug.LogError($"[ProceduralRouteGenerator] PositionalScanPuzzleStep " +
                               $"'{step.name}' sans placeholder source - injection impossible.");
                return;
            }
            if (string.IsNullOrEmpty(placeholder.spotId))
            {
                Debug.LogError($"[ProceduralRouteGenerator] Le placeholder Puzzle " +
                               $"'{placeholder.name}' utilise par '{step.name}' n'a pas de spotId : " +
                               $"liaison ScanSpot impossible.");
                step.Configure(null);
                return;
            }

            var poses = new List<Pose>();
            if (scanSpotsCache != null)
            {
                for (int i = 0; i < scanSpotsCache.Count; i++)
                {
                    var ss = scanSpotsCache[i];
                    if (ss == null || ss.linkedSpotIds == null) continue;
                    for (int j = 0; j < ss.linkedSpotIds.Count; j++)
                    {
                        if (ss.linkedSpotIds[j] == placeholder.spotId)
                        {
                            poses.Add(new Pose(ss.transform.position, ss.transform.rotation));
                            break;
                        }
                    }
                }
            }

            if (poses.Count == 0)
            {
                Debug.LogWarning($"[ProceduralRouteGenerator] Aucun ScanSpot lie au " +
                                 $"spotId '{placeholder.spotId}' - puzzle '{step.name}' insolvable.");
            }

            step.Configure(poses);
        }
    }
}
