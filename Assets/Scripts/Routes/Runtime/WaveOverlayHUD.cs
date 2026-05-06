using UnityEngine;

namespace EscapeGame.Routes.Runtime
{
    /// <summary>
    /// Composant a poser sur un GameObject enfant de la cam FPS portant un
    /// LineRenderer. Expose une instance unique consultable par les
    /// <see cref="AudioVisualPuzzleStep"/> pour piloter la sinusoide HUD
    /// sans avoir a drag-drop la reference dans chaque prefab.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class WaveOverlayHUD : MonoBehaviour
    {
        public static WaveOverlayHUD Instance { get; private set; }

        public LineRenderer Line { get; private set; }

        [Header("Apparence")]
        [Tooltip("Largeur de la ligne. Augmenter si invisible.")]
        public float lineWidth = 0.05f;

        [Tooltip("Couleur de la ligne.")]
        public Color lineColor = new Color(0f, 1f, 0.94f, 1f);

        private void Awake()
        {
            Line = GetComponent<LineRenderer>();
            Line.useWorldSpace = true;
            Line.enabled = false;

            // Force un material URP-compatible si le material actuel est le Default-Line
            if (Line.sharedMaterial == null || Line.sharedMaterial.shader.name.Contains("Default"))
            {
                Line.material = new Material(Shader.Find("Sprites/Default"));
            }
            Line.startColor = lineColor;
            Line.endColor = lineColor;
            Line.widthMultiplier = lineWidth;

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WaveOverlayHUD] Une autre instance existe deja - composant ignore.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
