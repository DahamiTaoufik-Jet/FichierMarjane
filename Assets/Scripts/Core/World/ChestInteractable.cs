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

        [Tooltip("Avertissement affiche au premier coffre. Si null, recherche au Start.")]
        public ChestWarningView warningView;

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

            if (warningView == null)
                warningView = FindFirstObjectByType<ChestWarningView>(FindObjectsInactive.Include);

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

            // Garde-fou : le coffre reste inerte tant que le joueur ne possede
            // pas au moins une lettre en inventaire.
            if (letterPanel != null && letterPanel.inventory != null
                && letterPanel.inventory.GetItemsOfType<EscapeGame.Inventory.Data.LetterItem>().Count == 0)
            {
                Debug.Log($"[ChestInteractable:{name}] Aucune lettre en inventaire - coffre non interactif.");
                return;
            }

            // Premier coffre : avertir avant d'engager la phase coffres
            // (apres quoi les enigmes/indices sont verrouilles).
            if (!PasswordManager.Instance.ChestPhaseCommitted && warningView != null)
            {
                warningView.Show(() =>
                {
                    PasswordManager.Instance.CommitChestPhase();
                    OpenLetterPanel();
                });
                return;
            }

            PasswordManager.Instance.CommitChestPhase();
            OpenLetterPanel();
        }

        private void OpenLetterPanel()
        {
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
