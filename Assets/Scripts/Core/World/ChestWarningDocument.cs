using System;
using UnityEngine;
using UnityEngine.UIElements;
using EscapeGame.Core.Player;

// UnityEngine.UIElements definit aussi un type Cursor : on leve l'ambiguite.
using Cursor = UnityEngine.Cursor;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Version UI Toolkit de l'avertissement affiche au premier coffre.
    /// Remplace <c>ChestWarningView</c>. "Continuer" engage la phase coffres via
    /// le callback fourni, "Annuler" ferme sans rien changer.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ChestWarningDocument : MonoBehaviour
    {
        [Tooltip("Message affiche. Laisser vide pour garder celui de l'UXML.")]
        [TextArea(3, 6)]
        public string message = "";

        private VisualElement screen;
        private Action onConfirm;
        private bool isOpen;
        private bool pendingCursorLock;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            screen = root.Q<VisualElement>("warning-screen");

            if (!string.IsNullOrEmpty(message))
            {
                var label = root.Q<Label>("warning-message");
                if (label != null) label.text = message;
            }

            var cont = root.Q<Button>("btn-warning-continue");
            if (cont != null) cont.clicked += Confirm;

            var cancel = root.Q<Button>("btn-warning-cancel");
            if (cancel != null) cancel.clicked += Cancel;

            SetVisible(false);
        }

        private void OnDisable()
        {
            // Ne pas laisser le compteur UIState desequilibre si la scene change
            // pendant que l'avertissement est ouvert.
            if (!isOpen) return;
            isOpen = false;
            onConfirm = null;
            UIState.SetUIClosed();
        }

        /// <summary>Affiche l'avertissement. <paramref name="onConfirm"/> est appele sur "Continuer".</summary>
        public void Show(Action onConfirm)
        {
            if (isOpen) return;
            this.onConfirm = onConfirm;
            isOpen = true;

            SetVisible(true);
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Confirm()
        {
            var cb = onConfirm;
            Close();
            if (cb != null) cb();
        }

        private void Cancel()
        {
            Close();
        }

        private void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            onConfirm = null;

            SetVisible(false);
            UIState.SetUIClosed();

            // Applique en fin de frame, apres tous les Update, pour qu'aucun
            // autre script ne redeverrouille le curseur derriere nous.
            pendingCursorLock = true;
        }

        private void LateUpdate()
        {
            if (!pendingCursorLock) return;
            pendingCursorLock = false;

            if (UIState.IsAnyUIOpen) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetVisible(bool visible)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            if (screen == null) return;
            if (visible) screen.RemoveFromClassList("hidden");
            else screen.AddToClassList("hidden");
        }
    }
}
