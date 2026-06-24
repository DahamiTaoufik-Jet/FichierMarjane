using UnityEngine;
using EscapeGame.Core.Interfaces;
using EscapeGame.Core.Player;
using EscapeGame.Routes.Services;

namespace EscapeGame.Core.World
{
    public class ChestInteractable : MonoBehaviour, IScannable
    {
        [Header("Position dans le mot de passe")]
        [Tooltip("Index 0-based dans le mot de passe. Chest 4 (gauche) = 0, Chest (droite) = 4.")]
        public int passwordPosition;

        [Header("Interaction")]
        [Tooltip("Distance max pour interagir avec le coffre.")]
        public float maxInteractionDistance = 3f;

        [Header("References")]
        [Tooltip("Animator du coffre (bool IsOpen).")]
        public Animator chestAnimator;

        [Tooltip("UI de selection de lettre (ChestLetterPanelView). Si null, recherche au Start.")]
        public Inventory.UI.ChestLetterPanelView letterPanel;

        private Transform playerTransform;
        private bool opened;

        private void Start()
        {
            if (chestAnimator == null)
                chestAnimator = GetComponent<Animator>();

            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null)
                playerTransform = cc.transform;

            if (letterPanel == null)
                letterPanel = FindFirstObjectByType<Inventory.UI.ChestLetterPanelView>(FindObjectsInactive.Include);

            PasswordManager.PositionSolved += OnPositionSolved;
        }

        private void OnDestroy()
        {
            PasswordManager.PositionSolved -= OnPositionSolved;
        }

        private void OnPositionSolved(int position, char letter)
        {
            if (position == passwordPosition)
                OpenChest();
        }

        public void OnHover() { }

        public void OnScan()
        {
            if (opened) return;
            if (PasswordManager.Instance == null) return;
            if (PasswordManager.Instance.IsGameLost) return;
            if (PasswordManager.Instance.IsPositionSolved(passwordPosition)) return;

            if (!IsPlayerCloseEnough()) return;

            if (letterPanel != null)
                letterPanel.Open(this, passwordPosition);
        }

        public void Reveal() { }
        public void OnHoverExit() { }

        private bool IsPlayerCloseEnough()
        {
            if (playerTransform == null) return false;
            float dist = Vector3.Distance(playerTransform.position, transform.position);
            return dist <= maxInteractionDistance;
        }

        private void OpenChest()
        {
            if (opened) return;
            opened = true;
            if (chestAnimator != null)
                chestAnimator.SetBool("IsOpen", true);
            Debug.Log($"[ChestInteractable:{name}] Coffre ouvert (position {passwordPosition + 1}).");
        }
    }
}
