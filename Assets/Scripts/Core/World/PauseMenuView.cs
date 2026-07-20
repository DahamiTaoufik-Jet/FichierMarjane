using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;

namespace EscapeGame.Core.World
{
    public class PauseMenuView : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject panelRoot;
        public GameObject settingsPanel;
        public GameObject controlsPanel;

        [Header("Sliders")]
        public Slider volumeSlider;
        public Slider sensitivitySlider;
        public Slider minimapSizeSlider;
        public Slider minimapZoomSlider;

        [Header("Labels")]
        public TMP_Text volumeValueLabel;
        public TMP_Text minimapSizeValueLabel;
        public TMP_Text minimapZoomValueLabel;

        [Header("Minimap")]
        [Tooltip("Camera de la minimap (si null, recherchee au Start).")]
        public MinimapCamera minimapCamera;

        private Camera minimapCam;
        private RectTransform minimapRootRt;
        private float minimapBaseOrtho = 18f;

        [Header("Sensitivity Input")]
        public TMP_InputField sensitivityInputField;

        [Header("Buttons")]
        public Button resumeButton;
        public Button controlsButton;
        public Button controlsBackButton;
        public Button quitButton;

        [Header("References")]
        public PlayerLook playerLook;

        [Tooltip("Controleur TPS (StarterAssets) : sa sensibilite camera est synchronisee avec la FPS.")]
        public StarterAssets.ThirdPersonController thirdPersonController;

        // Sensibilite FPS de reference (capturee au Start) : au niveau de reference,
        // le multiplicateur TPS vaut 1 (feeling d'origine). Au-dela/en-deca, les deux
        // modes montent/descendent proportionnellement ensemble.
        private float sensitivityBaseline = 15f;

        [Header("Input")]
        public Key openKey = Key.Escape;

        private bool isOpen;
        private float savedTimeScale;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);

            if (playerLook == null)
                playerLook = FindFirstObjectByType<PlayerLook>();

            if (thirdPersonController == null)
                thirdPersonController = FindFirstObjectByType<StarterAssets.ThirdPersonController>();

            // Reference = sensibilite FPS d'origine ; on synchronise la TPS dessus.
            if (playerLook != null && playerLook.mouseSensitivity > 0.01f)
                sensitivityBaseline = playerLook.mouseSensitivity;
            ApplyTpsSensitivity(playerLook != null ? playerLook.mouseSensitivity : sensitivityBaseline);

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                UpdateVolumeLabel(volumeSlider.value);
            }

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 1f;
                sensitivitySlider.maxValue = 20f;
                sensitivitySlider.wholeNumbers = false;
                if (playerLook != null)
                    sensitivitySlider.value = Mathf.Clamp(playerLook.mouseSensitivity, 1f, 20f);
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (sensitivityInputField != null)
            {
                sensitivityInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                sensitivityInputField.onEndEdit.AddListener(OnSensitivityInputEnd);
                sensitivityInputField.onSelect.AddListener(OnSensitivityInputSelected);
                var textComp = sensitivityInputField.textComponent;
                if (textComp != null) textComp.color = Color.white;
            }

            SyncSensitivityInput(playerLook != null ? playerLook.mouseSensitivity : 15f);

            // ---- Minimap ----
            if (minimapCamera == null)
                minimapCamera = FindFirstObjectByType<MinimapCamera>(FindObjectsInactive.Include);
            if (minimapCamera != null)
            {
                minimapCam = minimapCamera.GetComponent<Camera>();
                if (minimapCamera.uiRoot != null)
                    minimapRootRt = minimapCamera.uiRoot.transform as RectTransform;
                if (minimapCam != null && minimapCam.orthographicSize > 0.01f)
                    minimapBaseOrtho = minimapCam.orthographicSize;
            }

            if (minimapSizeSlider != null)
            {
                minimapSizeSlider.minValue = 120f;
                minimapSizeSlider.maxValue = 420f;
                minimapSizeSlider.wholeNumbers = false;
                if (minimapRootRt != null)
                    minimapSizeSlider.value = Mathf.Clamp(minimapRootRt.sizeDelta.x, 120f, 420f);
                minimapSizeSlider.onValueChanged.AddListener(OnMinimapSizeChanged);
                UpdateMinimapSizeLabel(minimapSizeSlider.value);
            }

            if (minimapZoomSlider != null)
            {
                minimapZoomSlider.minValue = 0.5f;  // dezoome (voit plus large)
                minimapZoomSlider.maxValue = 3f;    // zoome (voit plus pres)
                minimapZoomSlider.wholeNumbers = false;
                minimapZoomSlider.value = 1f;
                minimapZoomSlider.onValueChanged.AddListener(OnMinimapZoomChanged);
                UpdateMinimapZoomLabel(minimapZoomSlider.value);
            }

            if (resumeButton != null) resumeButton.onClick.AddListener(Close);
            if (controlsButton != null) controlsButton.onClick.AddListener(ShowControls);
            if (controlsBackButton != null) controlsBackButton.onClick.AddListener(HideControls);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current[openKey].wasPressedThisFrame) return;

            if (isOpen)
                Close();
            else if (!UIState.IsAnyUIOpen)
                Open();
        }

        private void Open()
        {
            isOpen = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            UIState.SetUIOpen();
            if (minimapCamera != null) minimapCamera.forceVisible = true; // apercu live des reglages minimap
            panelRoot.SetActive(true);
            settingsPanel.SetActive(true);
            if (controlsPanel != null) controlsPanel.SetActive(false);

            if (sensitivitySlider != null && playerLook != null)
                sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(playerLook.mouseSensitivity, sensitivitySlider.minValue, sensitivitySlider.maxValue));
            SyncSensitivityInput(playerLook != null ? playerLook.mouseSensitivity : 15f);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            Time.timeScale = savedTimeScale;
            UIState.SetUIClosed();
            if (minimapCamera != null) minimapCamera.forceVisible = false;
            panelRoot.SetActive(false);
            if (!UIState.IsAnyUIOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void ShowControls()
        {
            settingsPanel.SetActive(false);
            controlsPanel.SetActive(true);
        }

        private void HideControls()
        {
            controlsPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            UpdateVolumeLabel(value);
        }

        private void OnSensitivityChanged(float value)
        {
            if (playerLook != null)
                playerLook.mouseSensitivity = value;
            ApplyTpsSensitivity(value);
            SyncSensitivityInput(value);
        }

        // Synchronise la sensibilite de la camera TPS avec la valeur (partagee)
        // de la sensibilite FPS : multiplicateur = value / reference.
        private void ApplyTpsSensitivity(float value)
        {
            if (thirdPersonController == null || sensitivityBaseline <= 0.01f) return;
            thirdPersonController.LookSensitivity = value / sensitivityBaseline;
        }

        private void OnSensitivityInputEnd(string text)
        {
            float val;
            if (!float.TryParse(text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out val))
            {
                SyncSensitivityInput(playerLook != null ? playerLook.mouseSensitivity : 15f);
                return;
            }
            val = Mathf.Max(0.1f, val);
            if (playerLook != null)
                playerLook.mouseSensitivity = val;
            ApplyTpsSensitivity(val);
            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(val, sensitivitySlider.minValue, sensitivitySlider.maxValue));
            SyncSensitivityInput(val);
        }

        private void OnSensitivityInputSelected(string _)
        {
            if (sensitivityInputField != null)
                sensitivityInputField.textComponent.color = Color.white;
        }

        private void SyncSensitivityInput(float value)
        {
            if (sensitivityInputField == null) return;
            string formatted = value.ToString("F1");
            sensitivityInputField.text = formatted;
            if (sensitivityInputField.textComponent != null)
            {
                sensitivityInputField.textComponent.text = formatted;
                sensitivityInputField.textComponent.color = Color.white;
            }
        }

        private void UpdateVolumeLabel(float value)
        {
            if (volumeValueLabel != null)
                volumeValueLabel.text = Mathf.RoundToInt(value * 100) + "%";
        }

        // ---- Minimap ----
        private void OnMinimapSizeChanged(float value)
        {
            if (minimapRootRt != null)
                minimapRootRt.sizeDelta = new Vector2(value, value);
            UpdateMinimapSizeLabel(value);
        }

        private void OnMinimapZoomChanged(float value)
        {
            // value > 1 => zoome (orthoSize plus petit) ; value < 1 => dezoome.
            if (minimapCam != null)
                minimapCam.orthographicSize = minimapBaseOrtho / Mathf.Max(0.01f, value);
            UpdateMinimapZoomLabel(value);
        }

        private void UpdateMinimapSizeLabel(float value)
        {
            if (minimapSizeValueLabel != null)
                minimapSizeValueLabel.text = Mathf.RoundToInt(value).ToString();
        }

        private void UpdateMinimapZoomLabel(float value)
        {
            if (minimapZoomValueLabel != null)
                minimapZoomValueLabel.text = "x" + value.ToString("F1");
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
