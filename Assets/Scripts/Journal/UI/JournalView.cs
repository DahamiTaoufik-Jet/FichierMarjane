using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EscapeGame.Bonuses.Data;
using EscapeGame.Core.Player;
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
        [Tooltip("InputActionAsset contenant la map 'Game' avec l'action OpenJournal.")]
        public InputActionAsset actions;

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

        [Tooltip("Composant StageModalView (sur ce meme GameObject).")]
        public StageModalView stageModal;

        [Header("Zoom")]
        [Tooltip("Bouton zoom avant.")]
        public Button zoomInButton;

        [Tooltip("Bouton zoom arriere.")]
        public Button zoomOutButton;

        [Tooltip("Bouton reset zoom.")]
        public Button zoomResetButton;

        [Tooltip("Vitesse du zoom.")]
        public float zoomStep = 0.2f;

        [Tooltip("Zoom minimum.")]
        public float zoomMin = 0.5f;

        [Tooltip("Zoom maximum.")]
        public float zoomMax = 2.0f;

        [Header("Layout")]
        [Tooltip("Distance horizontale entre deux stages.")]
        public float hStep = 130f;

        [Tooltip("Amplitude verticale du zigzag.")]
        public float zigAmp = 28f;

        [Tooltip("Distance verticale entre deux routes.")]
        public float routeGap = 140f;

        [Tooltip("Position X du premier stage.")]
        public float startX = 80f;

        [Tooltip("Decalage vertical depuis le haut du container.")]
        public float startY = -80f;

        [Header("Couleurs lignes")]
        public Color activeLineColor = new Color(0.12f, 0.12f, 0.12f);
        public Color inactiveLineColor = new Color(0.80f, 0.80f, 0.80f);

        // Cache interne
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private InputAction openJournalAction;
        private bool journalIsOpen = false;

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

            // Resolver l'InputAction
            if (actions != null)
            {
                var map = actions.FindActionMap("Game");
                if (map != null)
                {
                    openJournalAction = map.FindAction("OpenJournal");
                    map.Enable();
                }
            }

            // Zoom buttons
            if (zoomInButton != null)
                zoomInButton.onClick.AddListener(() => Zoom(zoomStep));
            if (zoomOutButton != null)
                zoomOutButton.onClick.AddListener(() => Zoom(-zoomStep));
            if (zoomResetButton != null)
                zoomResetButton.onClick.AddListener(() => ResetZoom());

            // Demarre ferme
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (UIState.IsInputFieldActive) return;

            if (openJournalAction != null && openJournalAction.WasPressedThisFrame())
                ToggleJournal();
        }

        public void ToggleJournal()
        {
            if (panelRoot == null) return;

            // Si on est en mode selection bonus et que le joueur ferme le journal,
            // annuler le mode pour ne pas rester bloque.
            if (panelRoot.activeSelf && JournalSelectionMode.IsActive)
            {
                JournalSelectionMode.Exit();
                Debug.Log("[JournalView] Mode selection annule (fermeture journal).");
            }

            bool open = !panelRoot.activeSelf;
            panelRoot.SetActive(open);

            // Fermer le detail si ouvert quand on toggle le journal
            if (stageModal != null && stageModal.IsOpen)
                stageModal.Close();

            if (open)
            {
                journalIsOpen = true;
                UIState.SetUIOpen();
                Rebuild();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                if (journalIsOpen) { journalIsOpen = false; UIState.SetUIClosed(); }
                if (!UIState.IsAnyUIOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        // ====================================================================
        // Mode selection bonus (Dechiffreur, Resolveur, etc.)
        // ====================================================================

        /// <summary>
        /// Ouvre le journal de force pour le mode selection d'un bonus.
        /// Si le journal est deja ouvert, le reconstruit simplement.
        /// </summary>
        public void OpenForSelection()
        {
            if (panelRoot == null) return;

            if (!panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
                if (!journalIsOpen) { journalIsOpen = true; UIState.SetUIOpen(); }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Rebuild();
            Debug.Log("[JournalView] Ouvert en mode selection bonus.");
        }

        /// <summary>
        /// Ferme le journal apres une selection reussie.
        /// Appele par le callback des bonus (Dechiffreur, Resolveur).
        /// </summary>
        public void ExitSelectionMode()
        {
            Rebuild();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                if (journalIsOpen) { journalIsOpen = false; UIState.SetUIClosed(); }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Debug.Log("[JournalView] Mode selection termine, journal ferme.");
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

            // Progress bar retiree
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
                float y = startY - (routeIndex * routeGap) + (s % 2 == 0 ? zigAmp : -zigAmp);
                Vector2 pos = new Vector2(x, y);
                positions.Add(pos);

                // Instancier le StageNode
                if (stageNodePrefab == null) continue;
                var nodeGo = Instantiate(stageNodePrefab, worldContainer);
                var nodeRect = nodeGo.GetComponent<RectTransform>();
                if (nodeRect != null)
                {
                    nodeRect.anchorMin = new Vector2(0, 1);
                    nodeRect.anchorMax = new Vector2(0, 1);
                    nodeRect.pivot = new Vector2(0.5f, 0.5f);
                    nodeRect.anchoredPosition = pos;
                }

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
                    lineRect.anchorMin = new Vector2(0, 1);
                    lineRect.anchorMax = new Vector2(0, 1);
                    lineRect.pivot = new Vector2(0, 0.5f);
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

        // ====================================================================
        // Zoom
        // ====================================================================

        private void Zoom(float delta)
        {
            if (worldContainer == null) return;
            float current = worldContainer.localScale.x;
            float target = Mathf.Clamp(current + delta, zoomMin, zoomMax);
            worldContainer.localScale = new Vector3(target, target, 1f);
        }

        private void ResetZoom()
        {
            if (worldContainer == null) return;
            worldContainer.localScale = Vector3.one;
        }
    }
}
