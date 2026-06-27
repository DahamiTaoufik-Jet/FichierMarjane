using System.Collections;
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
    /// Effet "nouveau bonus obtenu". Jumeau de <see cref="LetterRevealView"/>
    /// mais pour les BonusItem : burst de particules + son, la carte du bonus
    /// (icone + nom) apparait en levitation, puis s'envole vers le haut en fondu.
    /// Pas de texte de position a memoriser (specifique aux lettres).
    ///
    /// Bloque les inputs joueur pendant la revelation via UIState.
    /// </summary>
    public class BonusRevealView : MonoBehaviour
    {
        [Header("Racine / fondu")]
        [Tooltip("GameObject de l'overlay complet, active/desactive a chaque revelation.")]
        public GameObject canvasRoot;

        [Tooltip("CanvasGroup de l'overlay, utilise pour le fondu de sortie.")]
        public CanvasGroup canvasGroup;

        [Header("Element flottant")]
        [Tooltip("RectTransform de la carte du bonus : flotte puis s'envole.")]
        public RectTransform floatingTransform;

        [Tooltip("Image affichant l'icone du bonus (optionnel).")]
        public Image iconImage;

        [Tooltip("Texte TMP affichant le nom du bonus (Passero One).")]
        public TMP_Text nameText;

        [Tooltip("Texte TMP affichant la description du bonus (optionnel).")]
        public TMP_Text descriptionText;

        [Header("Effets")]
        [Tooltip("Particle System du burst d'explosion (cree dans la scene, natif).")]
        public ParticleSystem burstParticles;

        [Tooltip("Source audio pour jouer le son d'obtention.")]
        public AudioSource audioSource;

        [Tooltip("Clip joue a l'apparition.")]
        public AudioClip getSound;

        [Header("Textes affiches")]
        [Tooltip("Texte de repli si le bonus n'a pas de rewardName.")]
        public string fallbackName = "Nouveau bonus !";

        [Tooltip("Si vrai, cache l'icone quand le bonus n'en a pas (sinon laisse l'image vide visible).")]
        public bool hideIconWhenEmpty = true;

        [Header("Timing")]
        [Tooltip("Duree du pop-in (scale 0 -> 1).")]
        public float popInDuration = 0.25f;

        [Tooltip("Duree d'affichage de la carte avant l'envol automatique.")]
        public float holdDuration = 2.5f;

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
        [Tooltip("Si vrai, un clic ou une touche accelere la sortie pendant l'affichage.")]
        public bool allowSkip = true;

        private Vector2 homePosition;
        private readonly Queue<BonusItem> queue = new Queue<BonusItem>();
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
            var bonus = item as BonusItem;
            if (bonus == null) return;

            queue.Enqueue(bonus);
            if (!isPlaying)
                StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            while (queue.Count > 0)
            {
                var bonus = queue.Dequeue();
                yield return PlaySequence(bonus);
            }
            isPlaying = false;
        }

        // ====================================================================
        // Sequence de revelation
        // ====================================================================

        private IEnumerator PlaySequence(BonusItem bonus)
        {
            UIState.SetUIOpen();

            // --- Mise en place ---
            if (canvasRoot != null) canvasRoot.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(bonus.rewardName) ? fallbackName : bonus.rewardName;

            if (descriptionText != null)
                descriptionText.text = bonus.description != null ? bonus.description : "";

            if (iconImage != null)
            {
                iconImage.sprite = bonus.icon;
                if (hideIconWhenEmpty)
                    iconImage.enabled = bonus.icon != null;
            }

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

            // --- Phase affichage (skippable) ---
            float hold = 0f;
            while (hold < holdDuration)
            {
                if (allowSkip && SkipPressed()) break;
                hold += Time.deltaTime;
                ApplyFloat(popInDuration + hold);
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
