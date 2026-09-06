using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EscapeGame.Core.Player;
using TMPro;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Game-view Stats equivalent for players, including non-development builds.
    /// Some Unity counters are stripped from release players: never report those as zero.
    /// No UnityEditor dependency. The overlay is passive and independent of FPSCanvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RuntimeStatsOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private bool visibleOnStart;
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.25f;

        private readonly Dictionary<string, ProfilerRecorder> recorders = new Dictionary<string, ProfilerRecorder>();
        private readonly StringBuilder text = new StringBuilder(2048);
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private readonly float[] audioSamples = new float[1024];
        private Canvas overlay;
        private TextMeshProUGUI content;
        private Animation[] animations = Array.Empty<Animation>();
        private Animator[] animators = Array.Empty<Animator>();
        private float elapsed;
        private float nextObjectRefresh;
        private int frames;
        private bool visible;
        private ulong lastTimingStamp;
        private float lastTimingReceived = float.NegativeInfinity;
        private static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

        // Names verified against the running Unity 6 profiler counter registry.
        private static readonly string[] CounterNames =
        {
            "Batches Count", "Draw Calls Count", "SetPass Calls Count", "Triangles Count", "Vertices Count",
            "Shadow Casters Count", "Visible Skinned Meshes Count",
            "Dynamic Batched Draw Calls Count", "Dynamic Batches Count",
            "Static Batched Draw Calls Count", "Static Batches Count",
            "Instanced Batched Draw Calls Count", "Instanced Batches Count",
            "Render Textures Count", "Render Textures Bytes"
        };

        public bool IsVisible => visible;

        private void Awake()
        {
            CreateOverlay();
        }

        private void OnEnable()
        {
            SetVisible(visibleOnStart);
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame && !IsTyping())
                SetVisible(!visible);

            if (!visible) return;

            if (FrameTimingManager.IsFeatureEnabled())
            {
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0 &&
                    timings[0].frameStartTimestamp != lastTimingStamp)
                {
                    lastTimingStamp = timings[0].frameStartTimestamp;
                    lastTimingReceived = Time.unscaledTime;
                }
            }

            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < Mathf.Max(0.1f, refreshInterval)) return;

            RefreshText();
            elapsed = 0f;
            frames = 0;
        }

        private static bool IsTyping()
        {
            if (UIState.IsInputFieldActive) return true;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null &&
                (selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<InputField>() != null);
        }

        public void SetVisible(bool value)
        {
            if (overlay == null) return;
            visible = value;
            overlay.gameObject.SetActive(value);
            if (!value)
            {
                foreach (var recorder in recorders.Values) recorder.Dispose();
                recorders.Clear();
                return;
            }

            if (recorders.Count == 0) StartRecorders();
            elapsed = 0f;
            frames = 0;
            nextObjectRefresh = 0f;
            lastTimingStamp = 0;
            lastTimingReceived = float.NegativeInfinity;
            content.text = "<b>STATISTICS   [P]</b>\nCollecte des mesures...";
        }

        private void StartRecorders()
        {
            // Enumerate once per opening. Missing release counters are expected, not errors.
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                for (int i = 0; i < CounterNames.Length; i++)
                {
                    if (description.Name != CounterNames[i] || recorders.ContainsKey(description.Name)) continue;
                    var recorder = ProfilerRecorder.StartNew(description.Category, description.Name, 1);
                    if (recorder.Valid) recorders.Add(description.Name, recorder);
                    else recorder.Dispose();
                    break;
                }
            }
        }

        private bool TryCount(string name, out long value)
        {
            value = 0;
            if (!recorders.TryGetValue(name, out var recorder) || !recorder.Valid || recorder.Count == 0)
                return false;
            value = recorder.LastValue;
            return true;
        }

        private string Count(string name)
        {
            return TryCount(name, out long value) ? value.ToString("N0", NumberCulture) : "N/D";
        }

        private string SavedByBatching()
        {
            long saved = 0;
            for (int i = 7; i <= 11; i += 2)
            {
                if (!TryCount(CounterNames[i], out long draws) || !TryCount(CounterNames[i + 1], out long batches))
                    return "N/D";
                saved += draws - batches;
            }
            return Math.Max(0, saved).ToString("N0", NumberCulture);
        }

        private void RefreshText()
        {
            text.Clear();
            text.Append("<b><color=#75DED0>STATISTICS</color></b>  <size=18>[P] masquer</size>\n");
            text.Append("<size=16>").Append(Application.isEditor ? "Editor" : Debug.isDebugBuild ? "Development build" : "Release build")
                .Append(" | mesures runtime | ").Append(refreshInterval.ToString("0.00", NumberCulture)).Append(" s</size>\n\n");

            text.Append("<b>GRAPHICS</b>\n");
            Row("FPS / frame", (frames / elapsed).ToString("0.0", NumberCulture) + " / " +
                (elapsed * 1000f / frames).ToString("0.00", NumberCulture) + " ms");
            bool hasTiming = Time.unscaledTime - lastTimingReceived < 1f;
            Row("CPU Main", Milliseconds(hasTiming ? timings[0].cpuMainThreadFrameTime : 0));
            Row("CPU Render", Milliseconds(hasTiming ? timings[0].cpuRenderThreadFrameTime : 0));
            Row("GPU", Milliseconds(hasTiming ? timings[0].gpuFrameTime : 0));
            Row("Batches", Count("Batches Count"));
            Row("Saved by batching", SavedByBatching());
            Row("Tris", Count("Triangles Count"));
            Row("Verts", Count("Vertices Count"));
            Row("Screen", Screen.width + " x " + Screen.height);
            // UnityStats.screenBytes is Editor-only; a backbuffer estimate would not be equivalent.
            Row("Screen memory", "N/D");
            Row("SetPass calls", Count("SetPass Calls Count"));
            Row("Draw calls", Count("Draw Calls Count"));
            Row("Shadow casters", Count("Shadow Casters Count"));
            Row("Visible skinned meshes", Count("Visible Skinned Meshes Count"));
            AppendAnimationCounts();
            Row("Render textures", Count("Render Textures Count") + " / " +
                (TryCount("Render Textures Bytes", out long bytes) ? (bytes / 1048576d).ToString("0.0", NumberCulture) + " MB" : "N/D"));

            text.Append("\n<b>AUDIO</b>\n");
            AppendAudioLevels();
            // Unity exposes these to the Editor's Stats window, but has no public player API.
            Row("DSP load", "N/D");
            Row("Stream load", "N/D");
            text.Append("\n<size=15><color=#B9C5D0>N/D : mesure non exposee par Unity dans ce mode.\n")
                .Append("~ : estimation runtime (animation / echantillon audio).\n")
                .Append("Development build : davantage de compteurs.\n")
                .Append("FPS reels, attentes VSync incluses. HUD inclus dans le rendu.</color></size>");
            content.SetText(text);
        }

        private void AppendAnimationCounts()
        {
            // Refresh references infrequently, including procedurally spawned objects.
            if (Time.unscaledTime >= nextObjectRefresh)
            {
                animations = FindObjectsByType<Animation>(FindObjectsSortMode.None);
                animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
                nextObjectRefresh = Time.unscaledTime + 2f;
            }
            int animationCount = 0;
            int animatorCount = 0;
            foreach (var animation in animations)
                if (animation != null && animation.isActiveAndEnabled && animation.isPlaying) animationCount++;
            foreach (var animator in animators)
                if (animator != null && animator.isActiveAndEnabled && animator.isInitialized &&
                    animator.runtimeAnimatorController != null && animator.speed != 0f) animatorCount++;
            // Active components are an approximation: native animation culling is not exposed.
            Row("Animation playing", "~ " + animationCount);
            Row("Animator playing", "~ " + animatorCount);
        }

        private void AppendAudioLevels()
        {
            // Sample the listener output, not microphone input. Reuse the buffer on the main thread.
            // Stats' native audio meter is unavailable in players; label this sampled estimate.
            int channels;
            switch (AudioSettings.speakerMode)
            {
                case AudioSpeakerMode.Mono: channels = 1; break;
                case AudioSpeakerMode.Quad: channels = 4; break;
                case AudioSpeakerMode.Surround: channels = 5; break;
                case AudioSpeakerMode.Mode5point1: channels = 6; break;
                case AudioSpeakerMode.Mode7point1: channels = 8; break;
                default: channels = 2; break;
            }
            double squares = 0;
            int clipped = 0;
            for (int channel = 0; channel < channels; channel++)
            {
                AudioListener.GetOutputData(audioSamples, channel);
                for (int i = 0; i < audioSamples.Length; i++)
                {
                    float sample = audioSamples[i];
                    squares += sample * sample;
                    if (Mathf.Abs(sample) >= 1f) clipped++;
                }
            }
            int samples = audioSamples.Length * channels;
            double rms = Math.Sqrt(squares / samples);
            Row("Level (RMS)", rms > 0.00001 ? "~ " + (20 * Math.Log10(rms)).ToString("0.0", NumberCulture) + " dBFS" : "~ silence");
            Row("Clipping (samples)", "~ " + (100d * clipped / samples).ToString("0.00", NumberCulture) + " %");
        }

        private void Row(string label, string value)
        {
            text.Append(label).Append("<pos=300>").Append(value).Append('\n');
        }

        private static string Milliseconds(double value)
        {
            return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value)
                ? value.ToString("0.00", NumberCulture) + " ms" : "N/D";
        }

        private void CreateOverlay()
        {
            var canvasObject = new GameObject("StatsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            overlay = canvasObject.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = 32760;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var panel = new GameObject("StatsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-16, -16);
            rect.sizeDelta = new Vector2(550, 830);
            var background = panel.GetComponent<Image>();
            background.color = new Color(0.025f, 0.04f, 0.065f, 0.98f);
            background.raycastTarget = false;

            var label = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(panel.transform, false);
            content = label.GetComponent<TextMeshProUGUI>();
            content.font = font;
            content.fontSize = 21;
            content.color = new Color(0.92f, 0.95f, 0.98f);
            content.textWrappingMode = TextWrappingModes.NoWrap;
            content.raycastTarget = false;
            var textRect = content.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 16);
            textRect.offsetMax = new Vector2(-20, -16);
            canvasObject.SetActive(false);
        }
    }
}
