using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;
using EscapeGame.Inventory.Events;
using EscapeGame.Routes.Services;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Effet "nouvelle lettre decouverte". A l'ajout d'une LetterItem a
    /// l'inventaire, affiche un overlay : burst de particules + son, la lettre
    /// apparait en levitation sur un rectangle, puis apres un court delai sa
    /// position dans le mot de passe s'affiche ("a memoriser"), et enfin la
    /// lettre s'envole vers le haut en fondu.
    ///
    /// Bloque les inputs joueur pendant la revelation via UIState (early-return
    /// dans les controleurs de mouvement/visee).
    /// </summary>
    public class LetterRevealView : MonoBehaviour
    {
        [Header("Racine / fondu")]
        [Tooltip("GameObject de l'overlay complet, active/desactive a chaque revelation.")]
        public GameObject canvasRoot;

        [Tooltip("CanvasGroup de l'overlay, utilise pour le fondu de sortie.")]
        public CanvasGroup canvasGroup;

        [Header("Element flottant")]
        [Tooltip("RectTransform de la lettre (avec son rectangle) : flotte puis s'envole.")]
        public RectTransform floatingTransform;

        [Tooltip("Texte TMP affichant la lettre (Passero One).")]
        public TMP_Text letterText;

        [Tooltip("Texte TMP affichant la position a memoriser. Cache au depart.")]
        public TMP_Text positionText;

        [Header("Effets")]
        [Tooltip("Particle System du burst d'explosion (cree dans la scene, natif).")]
        public ParticleSystem burstParticles;

        [Tooltip("Source audio pour jouer le son d'obtention.")]
        public AudioSource audioSource;

        [Tooltip("Clip joue a l'apparition (Letter Get).")]
        public AudioClip getSound;

        [Header("Textes affiches")]
        [Tooltip("Format de la position. {0} = numero (base 1).")]
        public string positionFormat = "Position {0}";

        [Tooltip("Sous-titre affiche sous la position.")]
        public string memorizeLabel = "A MEMORISER !";

        [Tooltip("Texte si la position est inconnue (pas de mot de passe).")]
        public string unknownPositionText = "Nouvelle lettre !";

        [Header("Timing")]
        [Tooltip("Duree du pop-in (scale 0 -> 1).")]
        public float popInDuration = 0.25f;

        [Tooltip("Delai avant d'afficher la position (la consigne : ~1 seconde).")]
        public float revealPositionDelay = 1f;

        [Tooltip("Duree d'affichage de la position avant l'envol automatique.")]
        public float memorizeDuration = 2.5f;

        [Tooltip("Duree de l'envol vers le haut + fondu.")]
        public float flyUpDuration = 0.6f;

        [Tooltip("Distance verticale parcourue pendant l'envol (en pixels Canvas).")]
        public float flyUpDistance = 400f;

        [Header("Levitation")]
        [Tooltip("Amplitude du flottement vertical (pixels).")]
        public float floatAmplitude = 15f;

        [Tooltip("Vitesse du flottement.")]
        public float floatSpeed = 2f;

        [Header("Options")]
        [Tooltip("Si vrai, un clic ou une touche accelere la sortie pendant la phase memorisation.")]
        public bool allowSkip = true;

        private Vector2 homePosition;
        private readonly Queue<LetterItem> queue = new Queue<LetterItem>();
        private bool isPlaying;

        // ====================================================================
        // Cycle de vie
        // ====================================================================

        private void Awake()
        {
            if (floatingTransform != null)
                homePosition = floatingTransform.anchoredPosition;
            if (canvasRoot != null)
                canvasRoot.SetActive(false);
        }

        private void OnEnable()
        {
            InventoryEvents.ItemAdded += HandleItemAdded;
        }

        private void OnDisable()
        {
            InventoryEvents.ItemAdded -= HandleItemAdded;
        }

        // ====================================================================
        // Reaction a l'inventaire
        // ====================================================================

        private void HandleItemAdded(ItemData item)
        {
            var letter = item as LetterItem;
            if (letter == null) return;

            queue.Enqueue(letter);
            if (!isPlaying)
                StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            while (queue.Count > 0)
            {
                var letter = queue.Dequeue();
                yield return PlaySequence(letter);
            }
            isPlaying = false;
        }

        // ====================================================================
        // Sequence de revelation
        // ====================================================================

        private IEnumerator PlaySequence(LetterItem letter)
        {
            UIState.SetUIOpen();

            // --- Mise en place ---
            if (canvasRoot != null) canvasRoot.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            if (letterText != null)
                letterText.text = char.ToUpper(letter.letter).ToString();
            if (positionText != null)
                positionText.gameObject.SetActive(false);

            if (floatingTransform != null)
            {
                floatingTransform.anchoredPosition = homePosition;
                floatingTransform.localScale = Vector3.zero;
            }

            // --- Burst + son ---
            if (burstParticles != null)
            {
                burstParticles.Clear();
                burstParticles.Play();
            }
            if (audioSource != null && getSound != null)
                audioSource.PlayOneShot(getSound);

            // --- Pop-in (scale 0 -> 1 avec leger depassement) ---
            float t = 0f;
            while (t < popInDuration)
            {
                t += Time.deltaTime;
                float k = popInDuration > 0f ? Mathf.Clamp01(t / popInDuration) : 1f;
                float scale = OvershootEase(k);
                if (floatingTransform != null)
                {
                    floatingTransform.localScale = new Vector3(scale, scale, scale);
                    ApplyFloat(t);
                }
                yield return null;
            }
            if (floatingTransform != null)
                floatingTransform.localScale = Vector3.one;

            // --- Levitation jusqu'a l'affichage de la position ---
            float wait = 0f;
            while (wait < revealPositionDelay)
            {
                wait += Time.deltaTime;
                ApplyFloat(popInDuration + wait);
                yield return null;
            }

            // --- Affichage de la position a memoriser ---
            if (positionText != null)
            {
                int pos = PasswordManager.Instance != null
                    ? PasswordManager.Instance.GetPositionForLetter(letter.letter)
                    : -1;

                if (pos >= 0)
                    positionText.text = string.Format(positionFormat, pos + 1) + "\n" + memorizeLabel;
                else
                    positionText.text = unknownPositionText;

                positionText.gameObject.SetActive(true);
            }

            // --- Phase memorisation (skippable) ---
            float hold = 0f;
            while (hold < memorizeDuration)
            {
                if (allowSkip && SkipPressed()) break;
                hold += Time.deltaTime;
                ApplyFloat(popInDuration + revealPositionDelay + hold);
                yield return null;
            }

            // --- Envol vers le haut + fondu ---
            float fly = 0f;
            Vector2 start = floatingTransform != null ? floatingTransform.anchoredPosition : homePosition;
            while (fly < flyUpDuration)
            {
                fly += Time.deltaTime;
                float k = flyUpDuration > 0f ? Mathf.Clamp01(fly / flyUpDuration) : 1f;
                float eased = EaseOutCubic(k);
                if (floatingTransform != null)
                    floatingTransform.anchoredPosition = start + Vector2.up * (flyUpDistance * eased);
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - k;
                yield return null;
            }

            // --- Nettoyage ---
            if (canvasRoot != null) canvasRoot.SetActive(false);
            if (floatingTransform != null)
            {
                floatingTransform.anchoredPosition = homePosition;
                floatingTransform.localScale = Vector3.one;
            }
            UIState.SetUIClosed();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private void ApplyFloat(float time)
        {
            if (floatingTransform == null) return;
            float offset = Mathf.Sin(time * floatSpeed) * floatAmplitude;
            floatingTransform.anchoredPosition = new Vector2(homePosition.x, homePosition.y + offset);
        }

        private static bool SkipPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            return false;
        }

        // Scale avec leger depassement (effet "pop").
        private static float OvershootEase(float k)
        {
            const float s = 1.70158f;
            k -= 1f;
            return k * k * ((s + 1f) * k + s) + 1f;
        }

        private static float EaseOutCubic(float k)
        {
            float inv = 1f - k;
            return 1f - inv * inv * inv;
        }
    }
}
