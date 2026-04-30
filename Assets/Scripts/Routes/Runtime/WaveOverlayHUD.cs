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

        private void Awake()
        {
            Line = GetComponent<LineRenderer>();
            Line.useWorldSpace = false;
            Line.enabled = false;

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
