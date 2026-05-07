using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Events;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// HUD inventaire : s'ouvre avec Q, affiche un item a la fois.
    /// Navigation gauche/droite avec A/D. Ordre : bonus d'abord, lettres ensuite.
    /// </summary>
    public class InventoryPanelView : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Racine du panneau a activer/desactiver.")]
        public GameObject panelRoot;

        [Tooltip("Icone de l'item affiche.")]
        public Image itemIcon;

        [Tooltip("Nom de l'item.")]
        public TMP_Text itemName;

        [Tooltip("Description de l'item.")]
        public TMP_Text itemDescription;

        [Tooltip("Fleche gauche (visuel indicatif).")]
        public GameObject arrowLeft;

        [Tooltip("Fleche droite (visuel indicatif).")]
        public GameObject arrowRight;

        [Header("Input")]
        public InputActionAsset actions;

        [Header("Touches")]
        [Tooltip("Touche pour fermer l'inventaire.")]
        public Key closeKey = Key.Escape;

        [Header("Reference")]
        [Tooltip("L'inventaire du joueur.")]
        public Runtime.Inventory inventory;

        private readonly List<ItemData> sortedItems = new List<ItemData>();
        private int currentIndex = 0;
        private bool isOpen = false;

        private InputAction moveAction;
        private InputAction toggleAction;
        private InputAction selectAction;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            InventoryEvents.ItemAdded += OnInventoryChanged;
            InventoryEvents.ItemRemoved += OnInventoryChanged;
        }

        private void Start()
        {
            if (actions != null)
            {
                var playerMap = actions.FindActionMap("Player");
                if (playerMap != null)
                    moveAction = playerMap.FindAction("Move");

                var gameMap = actions.FindActionMap("Game");
                if (gameMap != null)
                {
                    toggleAction = gameMap.FindAction("ToggleBonusInventory");
                    selectAction = gameMap.FindAction("Select");
                    gameMap.Enable();
                }
            }
        }

        private void OnDestroy()
        {
            InventoryEvents.ItemAdded -= OnInventoryChanged;
            InventoryEvents.ItemRemoved -= OnInventoryChanged;
        }

        private void Update()
        {
            // Bloque si un champ texte capture le clavier
            if (UIState.IsInputFieldActive) return;

            // Ouvrir avec l'action ToggleBonusInventory (Q)
            if (!isOpen && toggleAction != null && toggleAction.WasPressedThisFrame())
            {
                Open();
                return;
            }

            if (!isOpen) return;

            if (Keyboard.current != null && Keyboard.current[closeKey].wasPressedThisFrame)
            {
                Close();
                return;
            }

            // Utiliser l'item avec Select (Enter)
            if (selectAction != null && selectAction.WasPressedThisFrame())
            {
                UseCurrentItem();
                return;
            }

            // Navigation A/D
            bool left = Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame;
            bool right = Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame;

            if (left) Navigate(-1);
            if (right) Navigate(1);
        }

        private void Open()
        {
            if (inventory == null) return;

            RefreshSortedItems();
            isOpen = true;
            currentIndex = 0;
            panelRoot.SetActive(true);
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DisplayCurrent();
        }

        private void Close()
        {
            isOpen = false;
            panelRoot.SetActive(false);
            UIState.SetUIClosed();

            // Ne relock le curseur que si plus aucune UI n'est ouverte
            // (le dechiffreur peut avoir ouvert le journal entre-temps)
            if (!UIState.IsAnyUIOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void UseCurrentItem()
        {
            if (sortedItems.Count == 0 || inventory == null) return;

            var item = sortedItems[currentIndex];
            inventory.UseItem(item);
            Close();
        }

        private void Navigate(int direction)
        {
            if (sortedItems.Count == 0) return;

            currentIndex += direction;
            if (currentIndex < 0) currentIndex = sortedItems.Count - 1;
            if (currentIndex >= sortedItems.Count) currentIndex = 0;

            DisplayCurrent();
        }

        private void DisplayCurrent()
        {
            if (sortedItems.Count == 0)
            {
                if (itemIcon != null) itemIcon.enabled = false;
                if (itemName != null) itemName.text = "Inventaire vide";
                if (itemDescription != null) itemDescription.text = "";
                if (arrowLeft != null) arrowLeft.SetActive(false);
                if (arrowRight != null) arrowRight.SetActive(false);
                return;
            }

            var item = sortedItems[currentIndex];

            if (itemIcon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = item.icon != null;
            }

            if (itemName != null)
                itemName.text = item.rewardName;

            if (itemDescription != null)
                itemDescription.text = item.description;

            if (arrowLeft != null)
                arrowLeft.SetActive(sortedItems.Count > 1);

            if (arrowRight != null)
                arrowRight.SetActive(sortedItems.Count > 1);
        }

        private void RefreshSortedItems()
        {
            sortedItems.Clear();
            if (inventory == null) return;

            var bonuses = new List<ItemData>();
            var letters = new List<ItemData>();

            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                if (item is BonusItem)
                    bonuses.Add(item);
                else if (item is LetterItem)
                    letters.Add(item);
                else
                    bonuses.Add(item);
            }

            sortedItems.AddRange(bonuses);
            sortedItems.AddRange(letters);
        }

        private void OnInventoryChanged(ItemData item)
        {
            if (!isOpen) return;
            RefreshSortedItems();

            if (sortedItems.Count == 0)
            {
                Close();
                return;
            }

            if (currentIndex >= sortedItems.Count)
                currentIndex = sortedItems.Count - 1;

            DisplayCurrent();
        }
    }
}
