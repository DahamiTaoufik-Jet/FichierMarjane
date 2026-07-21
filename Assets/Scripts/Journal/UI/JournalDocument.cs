using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using EscapeGame.Bonuses.Data;
using EscapeGame.Core.Player;
using EscapeGame.Journal.Runtime;
using EscapeGame.Routes.Runtime;
using EscapeGame.Routes.Services;

// UnityEngine.UIElements definit aussi un type Cursor : on leve l'ambiguite.
using Cursor = UnityEngine.Cursor;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Version UI Toolkit du journal. Remplace <c>JournalView</c>, <c>StageNodeView</c>
    /// et <c>StageModalView</c> : la carte, les noeuds, les lignes de liaison et le
    /// detail d'une etape vivent desormais dans un seul document.
    ///
    /// Les noeuds sont crees en VisualElement plutot qu'instancies depuis un prefab,
    /// et positionnes en absolu dans un conteneur mis a l'echelle pour le zoom.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class JournalDocument : MonoBehaviour
    {
        [Header("Toggle")]
        [Tooltip("InputActionAsset contenant la map 'Game' avec l'action OpenJournal.")]
        public InputActionAsset actions;

        [Header("References")]
        [Tooltip("Source des donnees. Si null, recherche JournalManager.Instance.")]
        public JournalManager journalManager;

        [Tooltip("Inventaire bonus a fermer avant d'ouvrir le journal.")]
        public EscapeGame.Inventory.UI.BonusInventoryDocument bonusInventory;

        [Header("Layout de la carte")]
        [Tooltip("Distance horizontale entre deux etapes.")]
        public float hStep = 130f;

        [Tooltip("Amplitude verticale du zigzag.")]
        public float zigAmp = 28f;

        [Tooltip("Distance verticale entre deux routes.")]
        public float routeGap = 180f;

        [Tooltip("Position de la premiere etape.")]
        public float startX = 60f;
        public float startY = 60f;

        [Tooltip("Marge autour du contenu.")]
        public float contentPadding = 120f;

        [Header("Deck (routes completees)")]
        [Tooltip("Decalage entre chaque carte empilee quand une route est terminee.")]
        public float deckOffsetX = 6f;
        public float deckOffsetY = 4f;

        [Header("Zoom")]
        public float zoomStep = 0.2f;
        public float zoomMin = 0.5f;
        public float zoomMax = 2f;

        [Header("Couleurs des liaisons")]
        public Color activeLineColor = new Color(0.85f, 0.85f, 0.85f);
        public Color inactiveLineColor = new Color(0.42f, 0.42f, 0.46f);

        // ---- Elements ----
        private VisualElement screen;
        private VisualElement world;
        private ScrollView scroll;
        private VisualElement map;
        private VisualElement modal;
        private Label subtitle;

        private Label modalClue;
        private Label modalEnigme;
        private Label modalEncrypted;
        private VisualElement modalImage;
        private Label modalNext;
        private Label modalEmpty;
        private VisualElement imageViewer;
        private VisualElement imageViewerImage;
        private Button tabInitial;
        private Button tabEnigme;
        private Button tabSuite;

        // ---- Etat ----
        private InputAction openAction;
        private bool isOpen;
        private bool pendingCursorLock;
        private float zoom = 1f;
        private StageModalData currentData;

        private readonly List<Button> nodeButtons = new List<Button>();

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            screen = root.Q<VisualElement>("journal-screen");
            map = root.Q<VisualElement>("journal-map");
            scroll = root.Q<ScrollView>("journal-scroll");
            world = root.Q<VisualElement>("journal-world");
            modal = root.Q<VisualElement>("journal-modal");
            subtitle = root.Q<Label>("journal-subtitle");

            modalClue = root.Q<Label>("modal-clue");
            modalEnigme = root.Q<Label>("modal-enigme");
            modalEncrypted = root.Q<Label>("modal-encrypted");
            modalImage = root.Q<VisualElement>("modal-image");
            modalNext = root.Q<Label>("modal-next");
            modalEmpty = root.Q<Label>("modal-empty");
            imageViewer = root.Q<VisualElement>("image-viewer");
            imageViewerImage = root.Q<VisualElement>("image-viewer-image");
            tabInitial = root.Q<Button>("tab-initial");
            tabEnigme = root.Q<Button>("tab-enigme");
            tabSuite = root.Q<Button>("tab-suite");

            BindButtons(root);
            ResolveAction();

            if (journalManager == null) journalManager = JournalManager.Instance;
            Subscribe();

            SetVisible(false);
        }

        private void OnDisable()
        {
            Unsubscribe();

            // Journal ouvert lors d'un changement de scene : rendre le jeton
            // UIState, sinon les inputs restent bloques.
            if (!isOpen) return;
            isOpen = false;
            UIState.SetUIClosed();
        }

        private void Subscribe()
        {
            if (journalManager == null) return;
            journalManager.RouteAdded += OnRouteChanged;
            journalManager.RouteUpdated += OnRouteChanged;
            journalManager.RouteCompleted += OnRouteChanged;
        }

        private void Unsubscribe()
        {
            if (journalManager == null) return;
            journalManager.RouteAdded -= OnRouteChanged;
            journalManager.RouteUpdated -= OnRouteChanged;
            journalManager.RouteCompleted -= OnRouteChanged;
        }

        private void OnRouteChanged(RouteRuntime route)
        {
            if (isOpen) Rebuild();
        }

        private void ResolveAction()
        {
            if (actions == null) return;
            var map = actions.FindActionMap("Game");
            if (map == null) return;
            openAction = map.FindAction("OpenJournal");
            map.Enable();
        }

        private void BindButtons(VisualElement root)
        {
            var close = root.Q<Button>("journal-close");
            if (close != null) close.clicked += Close;

            var back = root.Q<Button>("modal-back");
            if (back != null) back.clicked += CloseModal;

            var zIn = root.Q<Button>("journal-zoom-in");
            if (zIn != null) zIn.clicked += delegate { Zoom(zoomStep); };

            var zOut = root.Q<Button>("journal-zoom-out");
            if (zOut != null) zOut.clicked += delegate { Zoom(-zoomStep); };

            var zReset = root.Q<Button>("journal-zoom-reset");
            if (zReset != null) zReset.clicked += delegate { SetZoom(1f); };

            var imgBack = root.Q<Button>("image-viewer-back");
            if (imgBack != null) imgBack.clicked += CloseImageViewer;

            // L'image de l'onglet Enigme s'ouvre en plein journal au clic.
            if (modalImage != null)
            {
                modalImage.pickingMode = PickingMode.Position;
                modalImage.RegisterCallback<ClickEvent>(delegate { OpenImageViewer(); });
            }

            if (tabInitial != null) tabInitial.clicked += delegate { SwitchTab(0); };
            if (tabEnigme != null) tabEnigme.clicked += delegate { SwitchTab(1); };
            if (tabSuite != null) tabSuite.clicked += delegate { SwitchTab(2); };
        }

        // ====================================================================
        // Ouverture / fermeture
        // ====================================================================

        private void Update()
        {
            if (UIState.IsInputFieldActive) return;
            if (openAction == null || !openAction.WasPressedThisFrame()) return;

            if (isOpen) Close();
            else if (!UIState.IsAnyUIOpen) Open();
        }

        private void LateUpdate()
        {
            if (!pendingCursorLock) return;
            pendingCursorLock = false;

            if (UIState.IsAnyUIOpen) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;

            if (bonusInventory != null) bonusInventory.ForceClose();

            SetVisible(true);
            CloseModal();
            Rebuild();

            // La mise en page n'est pas encore calculee a cet instant : on
            // recentre une fois que le ScrollView connait ses dimensions.
            if (scroll != null) scroll.schedule.Execute(CenterView).ExecuteLater(60);

            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!isOpen) return;

            // Quitter le journal pendant une selection de bonus annule celle-ci,
            // sinon le mode resterait actif sans UI pour en sortir.
            if (JournalSelectionMode.IsActive) JournalSelectionMode.Exit();

            isOpen = false;
            SetVisible(false);
            UIState.SetUIClosed();
            pendingCursorLock = true;
        }

        /// <summary>Ouvre le journal pour la selection d'une cible de bonus.</summary>
        public void OpenForSelection()
        {
            if (!isOpen) Open();
            else Rebuild();
        }

        /// <summary>Ferme le journal apres une selection reussie.</summary>
        public void ExitSelectionMode()
        {
            Rebuild();
            Close();
        }

        private void SetVisible(bool visible)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            }
            if (screen == null) return;
            if (visible) screen.RemoveFromClassList("hidden");
            else screen.AddToClassList("hidden");
        }

        // ====================================================================
        // Construction de la carte
        // ====================================================================

        public void Rebuild()
        {
            if (world == null) return;

            world.Clear();
            nodeButtons.Clear();

            if (journalManager == null) journalManager = JournalManager.Instance;
            if (journalManager == null) return;

            var routes = journalManager.KnownRoutes;
            float maxX = 0f, maxY = 0f;

            for (int r = 0; r < routes.Count; r++)
                BuildRoute(routes[r], r, ref maxX, ref maxY);

            // Le conteneur doit couvrir son contenu pour que le ScrollView sache
            // jusqu'ou defiler.
            world.style.width = maxX + contentPadding;
            world.style.height = maxY + contentPadding;

            if (subtitle != null)
            {
                subtitle.text = JournalSelectionMode.IsActive
                    ? "Choisis une etape"
                    : routes.Count + (routes.Count > 1 ? " routes connues" : " route connue");
            }

            ApplyZoom();
        }

        private void BuildRoute(RouteRuntime route, int routeIndex, ref float maxX, ref float maxY)
        {
            if (route == null) return;

            bool completed = route.State == RouteState.Completed;
            float baseY = startY + routeIndex * routeGap;
            var positions = new List<Vector2>();

            for (int s = 0; s < route.Steps.Count; s++)
            {
                var step = route.Steps[s];
                if (step == null) continue;

                // Route terminee : les etapes s'empilent en deck sur la premiere,
                // au lieu de s'etaler en zigzag.
                Vector2 pos = completed
                    ? new Vector2(startX + s * deckOffsetX, baseY + s * deckOffsetY)
                    : new Vector2(startX + s * hStep, baseY + (s % 2 == 0 ? 0f : zigAmp));

                positions.Add(pos);

                var node = CreateNode(step, s, routeIndex);
                node.style.left = pos.x;
                node.style.top = pos.y;
                world.Add(node);
                nodeButtons.Add(node);

                if (pos.x + 88f > maxX) maxX = pos.x + 88f;
                if (pos.y + 88f > maxY) maxY = pos.y + 88f;
            }

            // Un deck n'affiche pas ses liaisons : elles seraient toutes empilees.
            if (completed) return;

            for (int i = 0; i < positions.Count - 1; i++)
            {
                var stepA = route.Steps[i];
                bool active = stepA != null && stepA.IsResolved;
                world.Add(CreateConnector(positions[i], positions[i + 1], active));
            }
        }

        private Button CreateNode(StepBehaviour step, int stageIndex, int routeIndex)
        {
            var btn = new Button();
            btn.AddToClassList("stage-node");
            btn.text = (stageIndex + 1).ToString("00") + "-" + (routeIndex + 1);

            ApplyNodeState(btn, step);

            var captured = step;
            btn.clicked += delegate { OnNodeClicked(captured); };
            return btn;
        }

        /// <summary>
        /// Couleur, badge et interactivite d'un noeud selon son etat, le mode
        /// selection en cours et la phase coffres.
        /// </summary>
        private void ApplyNodeState(Button btn, StepBehaviour step)
        {
            StepState state = step.CurrentState;
            bool inSelection = JournalSelectionMode.IsActive;
            bool eligible = inSelection && JournalSelectionMode.IsEligible(step);

            // Une enigme non resolue devient inclicable une fois la phase coffres
            // engagee : le jeu ne se joue plus que sur les coffres.
            bool sealed_ = !inSelection && step is PuzzleStep && state != StepState.Resolved
                           && PasswordManager.Instance != null
                           && PasswordManager.Instance.ChestPhaseCommitted;

            string cls;
            string badge = "";

            if (inSelection)
            {
                if (eligible)
                    cls = JournalSelectionMode.ColorType == SelectionColorType.Green
                        ? "stage-node--green" : "stage-node--gold";
                else cls = "stage-node--ineligible";
            }
            else if (sealed_) { cls = "stage-node--sealed"; badge = "X"; }
            else
            {
                switch (state)
                {
                    case StepState.Resolved:   cls = "stage-node--resolved"; badge = "v"; break;
                    case StepState.Discovered: cls = "stage-node--current";  badge = "!"; break;
                    default:                   cls = "stage-node--locked";   badge = "-"; break;
                }
            }

            btn.AddToClassList(cls);

            if (!string.IsNullOrEmpty(badge))
            {
                var b = new Label(badge);
                b.AddToClassList("stage-node__badge");
                b.pickingMode = PickingMode.Ignore;
                btn.Add(b);
            }

            btn.SetEnabled(inSelection ? eligible : (!sealed_ && state != StepState.Locked));
        }

        private VisualElement CreateConnector(Vector2 a, Vector2 b, bool active)
        {
            // On relie les CENTRES des noeuds (88px de cote).
            Vector2 from = a + new Vector2(88f, 44f);
            Vector2 to = b + new Vector2(0f, 44f);

            Vector2 dir = to - from;
            float dist = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var line = new VisualElement();
            line.AddToClassList("connector");
            line.pickingMode = PickingMode.Ignore;
            line.style.left = from.x;
            line.style.top = from.y;
            line.style.width = dist;
            line.style.rotate = new StyleRotate(new Rotate(angle));
            line.style.backgroundColor = active ? activeLineColor : inactiveLineColor;

            // Derriere les noeuds.
            line.SendToBack();
            return line;
        }

        private void OnNodeClicked(StepBehaviour step)
        {
            if (step == null) return;

            // ---- Mode selection (Dechiffreur, Resolveur...) ----
            if (JournalSelectionMode.IsActive)
            {
                if (!JournalSelectionMode.IsEligible(step)) return;
                JournalSelectionMode.Select(step);
                return;
            }

            var data = StageModalData.Build(step);
            if (data != null) ShowModal(data);
        }

        /// <summary>
        /// Amene le contenu de la carte au centre du viewport. Sans cela le
        /// journal s'ouvrait cale en haut a gauche.
        /// </summary>
        private void CenterView()
        {
            if (scroll == null || world == null) return;

            float vw = scroll.contentViewport.layout.width;
            float vh = scroll.contentViewport.layout.height;
            float cw = world.layout.width * zoom;
            float ch = world.layout.height * zoom;

            scroll.scrollOffset = new Vector2(
                Mathf.Max(0f, (cw - vw) * 0.5f),
                Mathf.Max(0f, (ch - vh) * 0.5f));
        }

        // ====================================================================
        // Visionneuse d'image
        // ====================================================================

        private void OpenImageViewer()
        {
            if (currentData == null || currentData.PuzzleSnapshot == null) return;
            if (imageViewerImage != null)
                imageViewerImage.style.backgroundImage = new StyleBackground(currentData.PuzzleSnapshot);
            if (imageViewer != null) imageViewer.RemoveFromClassList("hidden");
        }

        private void CloseImageViewer()
        {
            if (imageViewer != null) imageViewer.AddToClassList("hidden");
        }

        // ====================================================================
        // Zoom
        // ====================================================================

        private void Zoom(float delta)
        {
            SetZoom(zoom + delta);
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, zoomMin, zoomMax);
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (world == null) return;
            world.style.scale = new StyleScale(new Scale(new Vector2(zoom, zoom)));
            if (scroll != null) scroll.schedule.Execute(CenterView).ExecuteLater(20);
        }

        // ====================================================================
        // Detail d'une etape
        // ====================================================================

        private void ShowModal(StageModalData data)
        {
            currentData = data;

            if (map != null) map.AddToClassList("hidden");
            if (modal != null) modal.RemoveFromClassList("hidden");

            // On ouvre sur le premier onglet qui a du contenu.
            if (HasInitial()) SwitchTab(0);
            else if (HasEnigme()) SwitchTab(1);
            else if (HasSuite()) SwitchTab(2);
            else SwitchTab(0);
        }

        private void CloseModal()
        {
            CloseImageViewer();
            currentData = null;
            if (modal != null) modal.AddToClassList("hidden");
            if (map != null) map.RemoveFromClassList("hidden");
        }

        private bool HasInitial()
        {
            return currentData != null && currentData.InitialClue != null && !currentData.InitialClue.IsEmpty;
        }

        private bool HasEnigme()
        {
            if (currentData == null) return false;
            return !string.IsNullOrEmpty(currentData.PuzzleQuestion)
                || !string.IsNullOrEmpty(currentData.PuzzleEncryptedQuestion)
                || currentData.PuzzleSnapshot != null;
        }

        private bool HasSuite()
        {
            return currentData != null && currentData.NextClue != null && !currentData.NextClue.IsEmpty;
        }

        private void SwitchTab(int index)
        {
            Hide(modalClue); Hide(modalEnigme); Hide(modalEncrypted);
            Hide(modalImage); Hide(modalNext); Hide(modalEmpty);

            SetTabActive(tabInitial, index == 0);
            SetTabActive(tabEnigme, index == 1);
            SetTabActive(tabSuite, index == 2);

            // Un onglet sans contenu reste visible mais desactive : le joueur voit
            // ainsi qu'il n'y a rien, plutot qu'un onglet qui disparait.
            if (tabInitial != null) tabInitial.SetEnabled(HasInitial());
            if (tabEnigme != null) tabEnigme.SetEnabled(HasEnigme());
            if (tabSuite != null) tabSuite.SetEnabled(HasSuite());

            if (currentData == null) { Show(modalEmpty, null); return; }

            bool any = false;

            if (index == 0 && HasInitial())
            {
                Show(modalClue, currentData.InitialClue.text);
                any = true;
            }
            else if (index == 1)
            {
                if (!string.IsNullOrEmpty(currentData.PuzzleQuestion))
                { Show(modalEnigme, currentData.PuzzleQuestion); any = true; }

                if (!string.IsNullOrEmpty(currentData.PuzzleEncryptedQuestion))
                { Show(modalEncrypted, currentData.PuzzleEncryptedQuestion); any = true; }

                if (currentData.PuzzleSnapshot != null && modalImage != null)
                {
                    modalImage.style.backgroundImage = new StyleBackground(currentData.PuzzleSnapshot);
                    modalImage.RemoveFromClassList("hidden");
                    any = true;
                }
            }
            else if (index == 2 && HasSuite())
            {
                Show(modalNext, currentData.NextClue.text);
                any = true;
            }

            if (!any) Show(modalEmpty, null);
        }

        private static void SetTabActive(Button tab, bool active)
        {
            if (tab == null) return;
            if (active) tab.AddToClassList("tab-btn--active");
            else tab.RemoveFromClassList("tab-btn--active");
        }

        private static void Show(VisualElement el, string text)
        {
            if (el == null) return;
            var label = el as Label;
            if (label != null && text != null) label.text = text;
            el.RemoveFromClassList("hidden");
        }

        private static void Hide(VisualElement el)
        {
            if (el != null) el.AddToClassList("hidden");
        }
    }
}
