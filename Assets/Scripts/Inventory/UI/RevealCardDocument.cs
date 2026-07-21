using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;

namespace EscapeGame.Inventory.UI
{
    /// <summary>
    /// Base commune des cartes de revelation en UI Toolkit (lettre, bonus,
    /// recompense de coffre). Porte TOUTE l'animation : file d'attente, pop-in
    /// avec depassement, levitation, phase d'attente skippable, envol + fondu.
    ///
    /// Les trois ecrans uGUI d'origine dupliquaient cette sequence a l'identique.
    /// Ici elle n'existe qu'une fois : les sous-classes ne decrivent que le
    /// CONTENU de la carte via <see cref="Fill"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class RevealCardDocument<T> : MonoBehaviour
    {
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip getSound;

        [Header("Timing")]
        public float popInDuration = 0.25f;
        public float holdDuration = 2.5f;
        public float flyUpDuration = 0.6f;
        [Tooltip("Distance verticale de l'envol, en pixels de reference.")]
        public float flyUpDistance = 400f;

        [Header("Levitation")]
        public float floatAmplitude = 15f;
        public float floatSpeed = 2f;

        [Header("Options")]
        [Tooltip("Une touche ou un clic accelere la sortie pendant l'attente.")]
        public bool allowSkip = true;

        // ---- Elements de la carte ----
        protected VisualElement screen;
        protected VisualElement card;
        protected Label header;
        protected VisualElement icon;
        protected Label big;
        protected Label title;
        protected Label desc;
        protected Label sub;

        private readonly Queue<T> queue = new Queue<T>();
        private bool isPlaying;
        private int uiStateHeld;

        protected virtual void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            screen = root.Q<VisualElement>("reveal-screen");
            card = root.Q<VisualElement>("reveal-card");
            header = root.Q<Label>("reveal-header");
            icon = root.Q<VisualElement>("reveal-icon");
            big = root.Q<Label>("reveal-big");
            title = root.Q<Label>("reveal-title");
            desc = root.Q<Label>("reveal-desc");
            sub = root.Q<Label>("reveal-sub");

            // L'overlay ne doit jamais intercepter les clics : le joueur continue
            // de jouer pendant qu'une carte s'affiche.
            root.pickingMode = PickingMode.Ignore;
            if (screen != null) screen.pickingMode = PickingMode.Ignore;

            SetScreenVisible(false);
            Subscribe();
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();

            // Une carte interrompue (changement de scene) ne doit pas laisser le
            // compteur UIState desequilibre, sinon les inputs restent bloques.
            while (uiStateHeld > 0)
            {
                UIState.SetUIClosed();
                uiStateHeld--;
            }
        }

        protected abstract void Subscribe();
        protected abstract void Unsubscribe();

        /// <summary>Remplit la carte pour cette entree. Appele juste avant l'animation.</summary>
        protected abstract void Fill(T item);

        /// <summary>Met une revelation en file. Joue immediatement si rien n'est en cours.</summary>
        public void Enqueue(T item)
        {
            queue.Enqueue(item);
            if (!isPlaying) StartCoroutine(ProcessQueue());
        }

        /// <summary>Hook optionnel : attendre avant la toute premiere carte.</summary>
        protected virtual IEnumerator BeforeFirst() { yield break; }

        /// <summary>Hook optionnel : appele quand la file est entierement videe.</summary>
        protected virtual void OnQueueDrained() { }

        /// <summary>Hook optionnel : etape intermediaire (ex. reveler la position).</summary>
        protected virtual IEnumerator DuringHold(T item) { yield break; }

        private bool firstDone;

        private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (!firstDone)
                {
                    firstDone = true;
                    yield return BeforeFirst();
                }
                yield return PlaySequence(item);
            }
            isPlaying = false;
            OnQueueDrained();
        }

        private IEnumerator PlaySequence(T item)
        {
            UIState.SetUIOpen();
            uiStateHeld++;

            ResetSlots();
            Fill(item);

            SetScreenVisible(true);
            SetCardOpacity(1f);
            SetCardScale(0f);
            SetCardOffset(0f);

            if (audioSource != null && getSound != null)
                audioSource.PlayOneShot(getSound);
            if (EscapeGame.Core.World.BackgroundMusic.Instance != null && getSound != null)
                EscapeGame.Core.World.BackgroundMusic.Instance.DuckForClip(getSound);

            // --- Pop-in avec depassement ---
            float t = 0f;
            while (t < popInDuration)
            {
                t += Time.deltaTime;
                float k = popInDuration > 0f ? Mathf.Clamp01(t / popInDuration) : 1f;
                SetCardScale(OvershootEase(k));
                SetCardOffset(FloatOffset(t));
                yield return null;
            }
            SetCardScale(1f);

            // --- Etape intermediaire propre a l'ecran (ex. position a memoriser) ---
            yield return DuringHold(item);

            // --- Attente, interruptible ---
            float hold = 0f;
            while (hold < holdDuration)
            {
                if (allowSkip && SkipPressed()) break;
                hold += Time.deltaTime;
                SetCardOffset(FloatOffset(popInDuration + hold));
                yield return null;
            }

            // --- Envol + fondu ---
            float fly = 0f;
            float startOffset = FloatOffset(popInDuration + hold);
            while (fly < flyUpDuration)
            {
                fly += Time.deltaTime;
                float k = flyUpDuration > 0f ? Mathf.Clamp01(fly / flyUpDuration) : 1f;
                float eased = EaseOutCubic(k);
                SetCardOffset(startOffset - flyUpDistance * eased);
                SetCardOpacity(1f - k);
                yield return null;
            }

            SetScreenVisible(false);
            SetCardOffset(0f);
            SetCardScale(1f);
            SetCardOpacity(1f);

            UIState.SetUIClosed();
            uiStateHeld--;
        }

        // ====================================================================
        // Helpers d'affichage
        // ====================================================================

        private void ResetSlots()
        {
            Hide(header); Hide(icon); Hide(big); Hide(title); Hide(desc); Hide(sub);
        }

        protected void Show(VisualElement el, string text)
        {
            if (el == null) return;
            var label = el as Label;
            if (label != null) label.text = text;
            el.RemoveFromClassList("hidden");
        }

        protected void ShowIcon(Sprite sprite)
        {
            if (icon == null) return;
            if (sprite == null) { Hide(icon); return; }
            icon.style.backgroundImage = new StyleBackground(sprite);
            icon.RemoveFromClassList("hidden");
        }

        protected static void Hide(VisualElement el)
        {
            if (el != null) el.AddToClassList("hidden");
        }

        private void SetScreenVisible(bool visible)
        {
            if (screen == null) return;
            if (visible) screen.RemoveFromClassList("hidden");
            else screen.AddToClassList("hidden");
        }

        private void SetCardScale(float s)
        {
            if (card != null) card.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
        }

        private void SetCardOffset(float y)
        {
            if (card != null) card.style.translate = new StyleTranslate(new Translate(0, y));
        }

        private void SetCardOpacity(float a)
        {
            if (card != null) card.style.opacity = a;
        }

        private float FloatOffset(float time)
        {
            return Mathf.Sin(time * floatSpeed) * floatAmplitude;
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
