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

        [Header("Labels")]
        public TMP_Text volumeValueLabel;

        [Header("Sensitivity Input")]
        public TMP_InputField sensitivityInputField;

        [Header("Buttons")]
        public Button resumeButton;
        public Button controlsButton;
        public Button controlsBackButton;
        public Button quitButton;

        [Header("References")]
        public PlayerLook playerLook;

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
            SyncSensitivityInput(value);
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
