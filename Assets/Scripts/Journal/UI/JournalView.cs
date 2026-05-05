using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EscapeGame.Journal.Runtime;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Construit la carte 2D du journal a partir des routes connues du
    /// <see cref="JournalManager"/>. Instancie des StageNode et ConnectorLine
    /// dans un WorldContainer navigable (pan/zoom gere par PanZoomController).
    /// Se rafraichit automatiquement via les evenements du JournalManager.
    /// </summary>
    public class JournalView : MonoBehaviour
    {
        [Header("Toggle")]
        [Tooltip("Touche pour ouvrir/fermer le journal.")]
        public Key toggleKey = Key.Tab;

        [Tooltip("Panel racine a activer/desactiver.")]
        public GameObject panelRoot;

        [Header("References")]
        [Tooltip("Source des donnees. Si null, recherche JournalManager.Instance.")]
        public JournalManager journalManager;

        [Tooltip("RectTransform parent dans lequel les nodes et lignes sont instanciees.")]
        public RectTransform worldContainer;

        [Tooltip("Prefab StageNode (doit posseder un StageNodeView).")]
        public GameObject stageNodePrefab;

        [Tooltip("Prefab ConnectorLine (Image, pivot 0/0.5).")]
        public GameObject connectorLinePrefab;

        [Tooltip("Slider de progression globale.")]
        public Slider progressBar;

        [Tooltip("Modal affiche au clic sur un StageNode.")]
        public StageModalView stageModal;

        [Header("Layout")]
        [Tooltip("Distance horizontale entre deux stages.")]
        public float hStep = 130f;

        [Tooltip("Amplitude verticale du zigzag.")]
        public float zigAmp = 28f;

        [Tooltip("Distance verticale entre deux routes.")]
        public float routeGap = 140f;

        [Tooltip("Position X du premier stage.")]
        public float startX = 80f;

        [Header("Couleurs lignes")]
        public Color activeLineColor = new Color(0.12f, 0.12f, 0.12f);
        public Color inactiveLineColor = new Color(0.80f, 0.80f, 0.80f);

        // Cache interne
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void Start()
        {
            if (journalManager == null) journalManager = JournalManager.Instance;
            if (journalManager == null)
            {
                Debug.LogWarning("[JournalView] Aucun JournalManager trouve.");
                return;
            }

            // Demarre ferme
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current[toggleKey].wasPressedThisFrame)
                ToggleJournal();
        }

        public void ToggleJournal()
        {
            if (panelRoot == null) return;
            bool open = !panelRoot.activeSelf;
            panelRoot.SetActive(open);

            if (open)
            {
                Rebuild();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnEnable()
        {
            if (journalManager == null) journalManager = JournalManager.Instance;
            if (journalManager != null)
            {
                journalManager.RouteAdded += OnRouteChanged;
                journalManager.RouteUpdated += OnRouteChanged;
                journalManager.RouteCompleted += OnRouteChanged;
            }

            // Rebuild a chaque reouverture pour afficher l'etat le plus recent
            Rebuild();
        }

        private void OnDisable()
        {
            if (journalManager != null)
            {
                journalManager.RouteAdded -= OnRouteChanged;
                journalManager.RouteUpdated -= OnRouteChanged;
                journalManager.RouteCompleted -= OnRouteChanged;
            }
        }

        private void OnRouteChanged(RouteRuntime route)
        {
            Rebuild();
        }

        // ====================================================================
        // Construction de la carte
        // ====================================================================

        public void Rebuild()
        {
            ClearWorld();

            if (journalManager == null) return;
            var routes = journalManager.KnownRoutes;
            if (routes.Count == 0) return;

            for (int r = 0; r < routes.Count; r++)
            {
                BuildRoute(routes[r], r);
            }

            UpdateProgressBar();
        }

        private void BuildRoute(RouteRuntime route, int routeIndex)
        {
            if (route == null || worldContainer == null) return;

            var positions = new List<Vector2>();

            for (int s = 0; s < route.Steps.Count; s++)
            {
                var step = route.Steps[s];
                if (step == null) continue;

                // Position en zigzag
                float x = startX + s * hStep;
                float y = -(routeIndex * routeGap) + (s % 2 == 0 ? zigAmp : -zigAmp);
                Vector2 pos = new Vector2(x, y);
                positions.Add(pos);

                // Instancier le StageNode
                if (stageNodePrefab == null) continue;
                var nodeGo = Instantiate(stageNodePrefab, worldContainer);
                var nodeRect = nodeGo.GetComponent<RectTransform>();
                if (nodeRect != null)
                    nodeRect.anchoredPosition = pos;

                var nodeView = nodeGo.GetComponent<StageNodeView>();
                if (nodeView != null)
                    nodeView.Init(step, s, stageModal);

                spawnedObjects.Add(nodeGo);
            }

            // Instancier les ConnectorLines entre chaque paire consecutive
            for (int i = 0; i < positions.Count - 1; i++)
            {
                if (connectorLinePrefab == null) break;

                Vector2 posA = positions[i];
                Vector2 posB = positions[i + 1];
                Vector2 dir = posB - posA;
                float dist = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // Determiner si la ligne est "active" (le step source est Resolved)
                var stepA = route.Steps[i];
                bool isActive = stepA != null && stepA.IsResolved;

                var lineGo = Instantiate(connectorLinePrefab, worldContainer);
                var lineRect = lineGo.GetComponent<RectTransform>();
                if (lineRect != null)
                {
                    lineRect.anchoredPosition = posA;
                    lineRect.sizeDelta = new Vector2(dist, isActive ? 3f : 2f);
                    lineRect.localEulerAngles = new Vector3(0, 0, angle);
                }

                var lineImg = lineGo.GetComponent<Image>();
                if (lineImg != null)
                    lineImg.color = isActive ? activeLineColor : inactiveLineColor;

                // S'assurer que les lignes sont derriere les nodes
                lineGo.transform.SetAsFirstSibling();
                spawnedObjects.Add(lineGo);
            }
        }

        // ====================================================================
        // Progress bar
        // ====================================================================

        private void UpdateProgressBar()
        {
            if (progressBar == null || journalManager == null) return;

            var routes = journalManager.KnownRoutes;
            int total = 0;
            int completed = 0;

            for (int r = 0; r < routes.Count; r++)
            {
                for (int s = 0; s < routes[r].Steps.Count; s++)
                {
                    total++;
                    if (routes[r].Steps[s] != null && routes[r].Steps[s].IsResolved)
                        completed++;
                }
            }

            progressBar.value = total > 0 ? (float)completed / total : 0f;
        }

        // ====================================================================
        // Cleanup
        // ====================================================================

        private void ClearWorld()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                    Destroy(spawnedObjects[i]);
            }
            spawnedObjects.Clear();
        }
    }
}
