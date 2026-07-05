using System.Collections;
using UnityEngine;
using TMPro;
using EscapeGame.Inventory.Data;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Garde-fou tutoriel pour les coffres : tant que le joueur n'a pas obtenu
    /// le nombre de lettres requis, l'ouverture des coffres est REFUSEE (avec un
    /// message), au lieu du comportement normal (autoriser + verrouiller les
    /// enigmes). Present uniquement dans la scene tutoriel : en jeu normal,
    /// <see cref="Instance"/> est null et les coffres se comportent normalement.
    /// </summary>
    public class TutorialChestGate : MonoBehaviour
    {
        public static TutorialChestGate Instance { get; private set; }

        [Header("Condition d'acces")]
        [Tooltip("Inventaire du joueur.")]
        public EscapeGame.Inventory.Runtime.Inventory inventory;

        [Tooltip("Nombre de lettres a posseder avant d'autoriser les coffres.")]
        public int requiredLetters = 2;

        [Header("Message (refus)")]
        [Tooltip("Racine du panneau message a activer/desactiver.")]
        public GameObject messagePanel;

        [Tooltip("Texte du message.")]
        public TMP_Text messageText;

        [TextArea(2, 5)]
        public string blockedMessage =
            "Pas encore ! Trouve d'abord tes 2 lettres.\n" +
            "En temps normal tu pourrais ouvrir les coffres maintenant, mais cela " +
            "VERROUILLERAIT definitivement les enigmes non resolues.";

        [Tooltip("Duree d'affichage du message (secondes). <= 0 = ne se masque pas seul.")]
        public float autoHideSeconds = 5f;

        private Coroutine hideRoutine;
        // Une fois le seuil de lettres atteint, l'acces est debloque DEFINITIVEMENT
        // (deposer une lettre la consomme : il ne faut pas re-verrouiller ensuite).
        private bool unlocked;

        private void Awake()
        {
            Instance = this;
            if (messagePanel != null) messagePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Verrou : des que le joueur a possede assez de lettres, on debloque
            // pour de bon (meme si elles sont ensuite deposees/consommees).
            if (!unlocked && inventory != null
                && inventory.GetItemsOfType<LetterItem>().Count >= requiredLetters)
                unlocked = true;
        }

        /// <summary>Vrai si le joueur a (eu) assez de lettres pour toucher aux coffres.</summary>
        public bool IsChestAccessAllowed()
        {
            if (unlocked || inventory == null) return true;
            if (inventory.GetItemsOfType<LetterItem>().Count >= requiredLetters)
            {
                unlocked = true;
                return true;
            }
            return false;
        }

        /// <summary>Affiche le message de refus (transitoire, ne bloque pas les inputs).</summary>
        public void ShowBlockedMessage()
        {
            if (messagePanel == null) return;
            if (messageText != null) messageText.text = blockedMessage;
            messagePanel.SetActive(true);

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            if (autoHideSeconds > 0f)
                hideRoutine = StartCoroutine(HideAfter(autoHideSeconds));
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (messagePanel != null) messagePanel.SetActive(false);
        }
    }
}
