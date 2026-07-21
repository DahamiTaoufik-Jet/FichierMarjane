using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Events;

// UnityEngine.UIElements definit aussi un type Cursor : on leve l'ambiguite.
using Cursor = UnityEngine.Cursor;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Version UI Toolkit de l'inventaire bonus. Remplace <c>InventoryPanelView</c>.
    /// S'ouvre avec Q, affiche un objet a la fois, navigation A/D, Entree pour
    /// utiliser, Echap pour fermer. Ordre d'affichage : bonus d'abord, lettres
    /// ensuite.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BonusInventoryDocument : MonoBehaviour
    {
        [Header("Input")]
        public InputActionAsset actions;
        public Key closeKey = Key.Escape;

        [Header("Reference")]
        public Runtime.Inventory inventory;

        private VisualElement panel;
        private VisualElement icon;
        private VisualElement arrowLeft;
        private VisualElement arrowRight;
        private Label itemName;
        private Label itemDesc;

        private readonly List<ItemData> sorted = new List<ItemData>();
        private int index;
        private bool isOpen;
        private bool pendingCursorLock;

        private InputAction toggleAction;
        private InputAction selectAction;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            panel = root.Q<VisualElement>("inventory-panel");
            icon = root.Q<VisualElement>("inventory-icon");
            arrowLeft = root.Q<Label>("inventory-arrow-left");
            arrowRight = root.Q<Label>("inventory-arrow-right");
            itemName = root.Q<Label>("inventory-name");
            itemDesc = root.Q<Label>("inventory-desc");

            // Purement informatif : la navigation se fait au clavier.
            root.pickingMode = PickingMode.Ignore;
            if (panel != null) panel.pickingMode = PickingMode.Ignore;

            if (inventory == null)
                inventory = FindFirstObjectByType<Runtime.Inventory>(FindObjectsInactive.Include);

            ResolveActions();
            SetVisible(false);

            InventoryEvents.ItemAdded += OnInventoryChanged;
            InventoryEvents.ItemRemoved += OnInventoryChanged;
        }

        private void OnDisable()
        {
            InventoryEvents.ItemAdded -= OnInventoryChanged;
            InventoryEvents.ItemRemoved -= OnInventoryChanged;

            // Panneau ouvert lors d'un changement de scene : ne pas laisser le
            // compteur UIState desequilibre.
            if (!isOpen) return;
            isOpen = false;
            UIState.SetUIClosed();
        }

        private void ResolveActions()
        {
            if (actions == null) return;
            var gameMap = actions.FindActionMap("Game");
            if (gameMap == null) return;

            toggleAction = gameMap.FindAction("ToggleBonusInventory");
            selectAction = gameMap.FindAction("Select");
            gameMap.Enable();
        }

        // ====================================================================
        // Input
        // ====================================================================

        private void Update()
        {
            if (UIState.IsInputFieldActive) return;

            // Ne pas s'ouvrir par-dessus une autre UI (journal, menu pause...).
            if (!isOpen && UIState.IsAnyUIOpen) return;

            if (!isOpen)
            {
                if (toggleAction != null && toggleAction.WasPressedThisFrame()) Open();
                return;
            }

            var kb = Keyboard.current;
            if (kb != null && kb[closeKey].wasPressedThisFrame) { Close(); return; }

            if (selectAction != null && selectAction.WasPressedThisFrame()) { UseCurrent(); return; }

            if (kb == null) return;
            if (kb.aKey.wasPressedThisFrame) Navigate(-1);
            else if (kb.dKey.wasPressedThisFrame) Navigate(1);
        }

        private void LateUpdate()
        {
            if (!pendingCursorLock) return;
            pendingCursorLock = false;

            if (UIState.IsAnyUIOpen) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ====================================================================
        // Ouverture / fermeture
        // ====================================================================

        private void Open()
        {
            if (inventory == null) return;

            Refresh();
            isOpen = true;
            index = 0;

            SetVisible(true);
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            DisplayCurrent();
        }

        private void Close()
        {
            if (!isOpen) return;
            isOpen = false;

            SetVisible(false);
            UIState.SetUIClosed();
            pendingCursorLock = true;
        }

        /// <summary>Ferme depuis l'exterieur (ex. le journal avant de s'ouvrir).</summary>
        public void ForceClose()
        {
            if (isOpen) Close();
        }

        // ====================================================================
        // Contenu
        // ====================================================================

        private void UseCurrent()
        {
            if (sorted.Count == 0 || inventory == null) return;
            var item = sorted[index];
            inventory.UseItem(item);
            Close();
        }

        private void Navigate(int direction)
        {
            if (sorted.Count == 0) return;

            index += direction;
            if (index < 0) index = sorted.Count - 1;
            if (index >= sorted.Count) index = 0;

            DisplayCurrent();
        }

        private void DisplayCurrent()
        {
            if (sorted.Count == 0)
            {
                Hide(icon);
                if (itemName != null) itemName.text = "Inventaire vide";
                if (itemDesc != null) itemDesc.text = "";
                Hide(arrowLeft); Hide(arrowRight);
                return;
            }

            var item = sorted[index];

            if (icon != null)
            {
                if (item.icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(item.icon);
                    icon.RemoveFromClassList("hidden");
                }
                else icon.AddToClassList("hidden");
            }

            if (itemName != null) itemName.text = item.rewardName;
            if (itemDesc != null) itemDesc.text = item.description != null ? item.description : "";

            // Les fleches n'ont de sens qu'a partir de deux objets.
            bool many = sorted.Count > 1;
            SetShown(arrowLeft, many);
            SetShown(arrowRight, many);
        }

        private void Refresh()
        {
            sorted.Clear();
            if (inventory == null) return;

            var bonuses = new List<ItemData>();
            var letters = new List<ItemData>();

            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                if (item is LetterItem) letters.Add(item);
                else bonuses.Add(item);
            }

            sorted.AddRange(bonuses);
            sorted.AddRange(letters);
        }

        private void OnInventoryChanged(ItemData item)
        {
            if (!isOpen) return;
            Refresh();

            if (sorted.Count == 0) { Close(); return; }
            if (index >= sorted.Count) index = sorted.Count - 1;

            DisplayCurrent();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static void Hide(VisualElement el)
        {
            if (el != null) el.AddToClassList("hidden");
        }

        private static void SetShown(VisualElement el, bool shown)
        {
            if (el == null) return;
            if (shown) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }

        private void SetVisible(bool visible)
        {
            if (panel == null) return;
            if (visible) panel.RemoveFromClassList("hidden");
            else panel.AddToClassList("hidden");
        }
    }
}
