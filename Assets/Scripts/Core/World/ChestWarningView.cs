using System;
using UnityEngine;
using UnityEngine.UI;
using EscapeGame.Core.Player;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Avertissement affiche au premier coffre : prevenir le joueur que s'il
    /// continue, il ne pourra plus resoudre d'enigmes/indices et devra repondre
    /// aux coffres pour finir le jeu et obtenir les recompenses.
    /// "Continuer" engage la phase coffres (verrou) via le callback, "Annuler"
    /// ferme sans rien changer.
    /// </summary>
    public class ChestWarningView : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Racine du panneau a activer/desactiver. Si null, ce GameObject.")]
        public GameObject panelRoot;

        [Tooltip("Bouton de confirmation (engage la phase coffres).")]
        public Button continueButton;

        [Tooltip("Bouton d'annulation.")]
        public Button cancelButton;

        private Action onConfirm;
        private bool isOpen;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (continueButton != null) continueButton.onClick.AddListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        }

        public void Show(Action onConfirm)
        {
            if (isOpen) return;
            this.onConfirm = onConfirm;
            isOpen = true;

            panelRoot.SetActive(true);
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Confirm()
        {
            var cb = onConfirm;
            Close();
            cb?.Invoke();
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

            panelRoot.SetActive(false);
            UIState.SetUIClosed();

            if (!UIState.IsAnyUIOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
