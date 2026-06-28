using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Carte de revelation "recompense de coffre" (jumelle doree de
    /// <see cref="BonusRevealView"/>). Pilotee explicitement via <see cref="Show"/>
    /// (pas d'abonnement a l'inventaire) : burst + son, carte qui flotte puis
    /// s'envole. Affiche un titre + un sous-titre (le libelle de la recompense).
    /// </summary>
    public class ChestRewardRevealView : MonoBehaviour
    {
        [Header("Racine / fondu")]
        public GameObject canvasRoot;
        public CanvasGroup canvasGroup;

        [Header("Element flottant")]
        public RectTransform floatingTransform;
        public TMP_Text titleText;
        public TMP_Text subtitleText;

        [Header("Effets")]
        public ParticleSystem burstParticles;
        public AudioSource audioSource;
        public AudioClip getSound;

        [Header("Timing")]
        public float popInDuration = 0.25f;
        public float holdDuration = 2.5f;
        public float flyUpDuration = 0.6f;
        public float flyUpDistance = 400f;

        [Header("Levitation")]
        public float floatAmplitude = 15f;
        public float floatSpeed = 2f;

        [Header("Options")]
        public bool allowSkip = true;

        /// <summary>Leve quand la file de revelations est videe (plus aucune carte en cours).
        /// Utilise pour enchainer un ecran de fin sans chevauchement visuel.</summary>
        public System.Action OnRevealsFinished;

        private struct Entry { public string title; public string subtitle; }

        private Vector2 homePosition;
        private readonly Queue<Entry> queue = new Queue<Entry>();
        private bool isPlaying;

        private void Awake()
        {
            if (floatingTransform != null)
                homePosition = floatingTransform.anchoredPosition;
            if (canvasRoot != null)
                canvasRoot.SetActive(false);
        }

        /// <summary>Affiche une carte recompense (file d'attente si une autre joue).</summary>
        public void Show(string title, string subtitle)
        {
            queue.Enqueue(new Entry { title = title, subtitle = subtitle });
            if (!isPlaying)
                StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            while (queue.Count > 0)
                yield return PlaySequence(queue.Dequeue());
            isPlaying = false;
            OnRevealsFinished?.Invoke();
        }

        private IEnumerator PlaySequence(Entry e)
        {
            UIState.SetUIOpen();

            if (canvasRoot != null) canvasRoot.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            if (titleText != null) titleText.text = e.title;
            if (subtitleText != null) subtitleText.text = e.subtitle;

            if (floatingTransform != null)
            {
                floatingTransform.anchoredPosition = homePosition;
                floatingTransform.localScale = Vector3.zero;
            }

            if (burstParticles != null) { burstParticles.Clear(); burstParticles.Play(); }
            if (audioSource != null && getSound != null) audioSource.PlayOneShot(getSound);

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
            if (floatingTransform != null) floatingTransform.localScale = Vector3.one;

            float hold = 0f;
            while (hold < holdDuration)
            {
                if (allowSkip && SkipPressed()) break;
                hold += Time.deltaTime;
                ApplyFloat(popInDuration + hold);
                yield return null;
            }

            float fly = 0f;
            Vector2 start = floatingTransform != null ? floatingTransform.anchoredPosition : homePosition;
            while (fly < flyUpDuration)
            {
                fly += Time.deltaTime;
                float k = flyUpDuration > 0f ? Mathf.Clamp01(fly / flyUpDuration) : 1f;
                float eased = EaseOutCubic(k);
                if (floatingTransform != null)
                    floatingTransform.anchoredPosition = start + Vector2.up * (flyUpDistance * eased);
                if (canvasGroup != null) canvasGroup.alpha = 1f - k;
                yield return null;
            }

            if (canvasRoot != null) canvasRoot.SetActive(false);
            if (floatingTransform != null)
            {
                floatingTransform.anchoredPosition = homePosition;
                floatingTransform.localScale = Vector3.one;
            }
            UIState.SetUIClosed();
        }

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
