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

            // 1. Inventaire des placeholders
            var allNodes = FindObjectsByType<PlaceholderNode>(FindObjectsSortMode.None);
            var placeholders = new List<PlaceholderNode>(allNodes);

            // 2. Planification
            int? seed = config.seed > 0 ? (int?)config.seed : null;
            var planner = new RouteGenerationPlanner(
                config.minRouteLength, config.maxRouteLength, config.maxStepUsage, seed);

            var plans = planner.BuildPlans(config.stepPool, placeholders, config.regionFilter);
            if (plans.Count == 0)
            {
                Debug.LogWarning("[ProceduralRouteGenerator] Aucune route generee. " +
                                 "Verifie le pool de steps, la presence de PlaceholderNodes et leurs contraintes.");
                return;
            }

            // 3. Distribution des recompenses (lettres prioritaires, bonus en remplissage)
            AssignRewards(plans);

            // 4. Instanciation et enregistrement
            foreach (var plan in plans)
                InstantiateAndRegister(plan);

            Debug.Log($"[ProceduralRouteGenerator] {plans.Count} route(s) generee(s).");
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

        private BonusItem PickRandomBonus()
        {
            if (config.bonusPool == null || config.bonusPool.Count == 0) return null;
            return config.bonusPool[Random.Range(0, config.bonusPool.Count)];
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

                if (cleanupUsedPlaceholders && assignment.Placeholder != null)
                    Destroy(assignment.Placeholder.gameObject);
            }

            if (stepInstances.Count > 0)
                routeManager.RegisterRoute(plan.RouteId, plan.DisplayName, stepInstances, plan.EndReward);
        }
    }
}
