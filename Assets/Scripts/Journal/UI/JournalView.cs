using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EscapeGame.Bonuses.Data;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.UI;
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

        [Tooltip("Menu des bonus a fermer avant d'ouvrir le journal (evite le chevauchement des UI).")]
        public InventoryPanelView bonusInventory;

        [Header("Navigation")]
        [Tooltip("Bouton pour fermer le journal et revenir au jeu (comme Tab).")]
        public Button buttonRetourAuJournal;

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
        public float routeGap = 180f;

        [Tooltip("Position X du premier stage.")]
        public float startX = 80f;

        [Tooltip("Decalage vertical depuis le haut du container.")]
        public float startY = -80f;

        [Header("Couleurs lignes")]
        public Color activeLineColor = new Color(0.12f, 0.12f, 0.12f);
        public Color inactiveLineColor = new Color(0.80f, 0.80f, 0.80f);

        [Header("Deck (routes completees)")]
        [Tooltip("Decalage X entre chaque carte empilee dans le deck.")]
        public float deckOffsetX = 6f;

        [Tooltip("Decalage Y entre chaque carte empilee dans le deck.")]
        public float deckOffsetY = -4f;

        [Header("Contenu (dimensionnement auto)")]
        [Tooltip("Marge ajoutee autour du contenu pour dimensionner le WorldContainer (px). " +
                 "Le ScrollRect ne rappelle qu'au-dela de ces bornes.")]
        public float contentPadding = 160f;

        [Header("Tutoriel")]
        [Tooltip("Si vrai, a chaque ouverture le journal recentre la vue sur le premier bloc " +
                 "(la tuile blanche du tutoriel) au centre du viewport, pour laisser la place de cliquer.")]
        public bool centerFirstNodeOnOpen = false;

        [Header("Centrage")]
        [Tooltip("Si vrai, a chaque ouverture le journal centre l'ENSEMBLE des routes " +
                 "(boite englobante des blocs) au centre du viewport.")]
        public bool centerContentOnOpen = false;

        [Header("Animation ouverture/fermeture")]
        [Tooltip("Panneaux a faire glisser (bas->haut a l'ouverture, haut->bas a la fermeture). Ex: Scroll View, Stage Modal.")]
        public RectTransform[] slidePanels;

        [Tooltip("Duree du slide (secondes).")]
        public float slideDuration = 0.35f;

        [Tooltip("Distance verticale du slide (px). <= 0 = hauteur de l'ecran.")]
        public float slideDistance = 0f;

        private CanvasGroup animGroup;
        private Vector2[] slideBasePos;
        private bool slideBaseCaptured;
        private Coroutine animRoutine;

        // Cache interne
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private InputAction openJournalAction;
        private bool journalIsOpen = false;

        // Bornes du contenu (centres des nodes) calculees pendant Rebuild
        private float cMinX, cMaxX, cMinY, cMaxY;
        private bool hasContent;

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

            // Bouton retour au jeu
            if (buttonRetourAuJournal != null)
                buttonRetourAuJournal.onClick.AddListener(ToggleJournal);

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
            if (journalIsOpen) DoClose();
            else DoOpen();
        }

        private void DoOpen()
        {
            panelRoot.SetActive(true);

            // Fermer le menu bonus s'il etait ouvert (evite d'empiler deux UI).
            if (bonusInventory != null) bonusInventory.ForceClose();
            if (stageModal != null && stageModal.IsOpen) stageModal.Close();

            journalIsOpen = true;
            UIState.SetUIOpen();
            Rebuild(); // gere deja le centrage horizontal si centerContentOnOpen
            if (centerFirstNodeOnOpen && !centerContentOnOpen) CenterViewOnFirstNode();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlayOpenClose(true); // slide bas->haut + fondu en fin d'ouverture
        }

        private void DoClose()
        {
            // Annule le mode selection bonus si actif.
            if (JournalSelectionMode.IsActive)
            {
                JournalSelectionMode.Exit();
                Debug.Log("[JournalView] Mode selection annule (fermeture journal).");
            }
            if (stageModal != null && stageModal.IsOpen) stageModal.Close();

            journalIsOpen = false;
            UIState.SetUIClosed();
            if (!UIState.IsAnyUIOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            PlayOpenClose(false); // slide haut->bas (sans fondu) puis desactivation
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

            // Reset des bornes de contenu
            cMinX = cMinY = float.MaxValue;
            cMaxX = cMaxY = float.MinValue;
            hasContent = false;

            for (int r = 0; r < routes.Count; r++)
            {
                BuildRoute(routes[r], r);
            }

            // Centre l'ensemble des routes sur l'axe horizontal (decalage des blocs
            // eux-memes : insensible au clamp elastique du ScrollRect).
            if (centerContentOnOpen) CenterContentHorizontally();

            // Dimensionne le WorldContainer sur son contenu reel pour que le
            // ScrollRect connaisse sa vraie taille (fin du snap-back intempestif).
            ResizeContainerToContent();
        }

        /// <summary>
        /// Redimensionne le WorldContainer (content du ScrollRect) pour englober
        /// toutes les routes + une marge. Le container a un pivot (0,1) en haut a
        /// gauche : le contenu s'etend vers la droite (+x) et vers le bas (-y).
        /// </summary>
        /// <summary>
        /// Recentre la vue pour amener le premier bloc (StageNode) au centre du
        /// viewport. Utilise le deplacement en espace-monde (independant des
        /// ancres/pivots/echelle) : on decale le WorldContainer de sorte que le
        /// centre du node coincide avec le centre du viewport.
        /// </summary>
        private void CenterViewOnFirstNode()
        {
            if (worldContainer == null) return;
            var viewport = worldContainer.parent as RectTransform;
            if (viewport == null) return;

            RectTransform target = null;
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] == null) continue;
                if (spawnedObjects[i].GetComponent<StageNodeView>() != null)
                {
                    target = spawnedObjects[i].GetComponent<RectTransform>();
                    break;
                }
            }
            if (target == null) return;

            Canvas.ForceUpdateCanvases();
            Vector3 centerWorld = viewport.TransformPoint(viewport.rect.center);
            Vector3 nodeWorld = target.position; // pivot (0.5,0.5) -> centre du node
            worldContainer.position += (centerWorld - nodeWorld);
        }

        /// <summary>
        /// Centre l'ensemble des routes sur l'axe HORIZONTAL en decalant les blocs
        /// et les lignes a l'interieur du WorldContainer (et non en deplacant le
        /// container, ce que le ScrollRect elastique annulerait). La boite
        /// englobante horizontale du contenu est ramenee au milieu du viewport.
        /// </summary>
        private void CenterContentHorizontally()
        {
            if (worldContainer == null || !hasContent) return;
            var viewport = worldContainer.parent as RectTransform;
            if (viewport == null) return;

            float viewportW = viewport.rect.width;
            if (viewportW <= 1f) return; // panel pas encore mis en page (inactif)
            float contentCenterX = (cMinX + cMaxX) * 0.5f;
            float dx = viewportW * 0.5f - contentCenterX;
            if (Mathf.Approximately(dx, 0f)) return;

            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] == null) continue;
                var rt = spawnedObjects[i].transform as RectTransform;
                if (rt == null) continue;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + dx, rt.anchoredPosition.y);
            }

            cMinX += dx;
            cMaxX += dx;
        }

        private void ResizeContainerToContent()
        {
            if (worldContainer == null || !hasContent) return;

            float width = cMaxX + contentPadding;
            float height = (-cMinY) + contentPadding;

            // Au minimum la taille du viewport (sinon contenu plus petit que la vue).
            var viewport = worldContainer.parent as RectTransform;
            if (viewport != null)
            {
                width = Mathf.Max(width, viewport.rect.width);
                height = Mathf.Max(height, viewport.rect.height);
            }

            worldContainer.sizeDelta = new Vector2(width, height);
        }

        private void BuildRoute(RouteRuntime route, int routeIndex)
        {
            if (route == null || worldContainer == null) return;

            bool completed = route.State == RouteState.Completed;

            var positions = new List<Vector2>();
            float baseY = startY - (routeIndex * routeGap);

            for (int s = 0; s < route.Steps.Count; s++)
            {
                var step = route.Steps[s];
                if (step == null) continue;

                Vector2 pos;
                if (completed)
                {
                    pos = new Vector2(
                        startX + s * deckOffsetX,
                        baseY + s * deckOffsetY);
                }
                else
                {
                    pos = new Vector2(
                        startX + s * hStep,
                        baseY + (s % 2 == 0 ? zigAmp : -zigAmp));
                }
                positions.Add(pos);

                if (pos.x < cMinX) cMinX = pos.x;
                if (pos.x > cMaxX) cMaxX = pos.x;
                if (pos.y < cMinY) cMinY = pos.y;
                if (pos.y > cMaxY) cMaxY = pos.y;
                hasContent = true;

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
                    nodeView.Init(step, s, routeIndex, stageModal);

                spawnedObjects.Add(nodeGo);
            }

            if (completed) return;

            for (int i = 0; i < positions.Count - 1; i++)
            {
                if (connectorLinePrefab == null) break;

                Vector2 posA = positions[i];
                Vector2 posB = positions[i + 1];
                Vector2 dir = posB - posA;
                float dist = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

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

        // ====================================================================
        // Animation ouverture / fermeture (slide + fondu)
        // ====================================================================

        private void EnsureAnim()
        {
            if (panelRoot == null) return;
            if (animGroup == null)
            {
                animGroup = panelRoot.GetComponent<CanvasGroup>();
                if (animGroup == null) animGroup = panelRoot.AddComponent<CanvasGroup>();
            }
            if (!slideBaseCaptured && slidePanels != null)
            {
                slideBasePos = new Vector2[slidePanels.Length];
                for (int i = 0; i < slidePanels.Length; i++)
                    slideBasePos[i] = slidePanels[i] != null ? slidePanels[i].anchoredPosition : Vector2.zero;
                slideBaseCaptured = true;
            }
        }

        private void SetSlideOffset(float yOff)
        {
            if (slidePanels == null || slideBasePos == null) return;
            for (int i = 0; i < slidePanels.Length && i < slideBasePos.Length; i++)
                if (slidePanels[i] != null)
                    slidePanels[i].anchoredPosition = slideBasePos[i] + new Vector2(0f, yOff);
        }

        private void PlayOpenClose(bool opening)
        {
            EnsureAnim();
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(AnimRoutine(opening));
        }

        private System.Collections.IEnumerator AnimRoutine(bool opening)
        {
            float dist = slideDistance > 0f
                ? slideDistance
                : Mathf.Max(1f, ((RectTransform)panelRoot.transform).rect.height);
            float t = 0f;

            if (opening)
            {
                // Bas -> haut, avec fondu qui se termine a l'ouverture.
                SetSlideOffset(-dist);
                if (animGroup != null) animGroup.alpha = 0f;
                while (t < slideDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = slideDuration > 0f ? Mathf.Clamp01(t / slideDuration) : 1f;
                    SetSlideOffset(-dist * (1f - EaseOutCubic(k)));
                    if (animGroup != null) animGroup.alpha = k; // fondu lineaire, complet en fin d'ouverture
                    yield return null;
                }
                SetSlideOffset(0f);
                if (animGroup != null) animGroup.alpha = 1f;
            }
            else
            {
                // Haut -> bas, sans fondu.
                if (animGroup != null) animGroup.alpha = 1f;
                while (t < slideDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = slideDuration > 0f ? Mathf.Clamp01(t / slideDuration) : 1f;
                    SetSlideOffset(-dist * EaseInCubic(k));
                    yield return null;
                }
                SetSlideOffset(0f); // reset pour la prochaine ouverture
                panelRoot.SetActive(false);
            }
            animRoutine = null;
        }

        private static float EaseOutCubic(float k) { float inv = 1f - k; return 1f - inv * inv * inv; }
        private static float EaseInCubic(float k) { return k * k * k; }
    }
}
