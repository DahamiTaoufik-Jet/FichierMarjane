using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using EscapeGame.Core.Player;

// UnityEngine.UIElements definit aussi un type Cursor : on leve l'ambiguite.
using Cursor = UnityEngine.Cursor;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Version UI Toolkit du menu pause. Remplace <c>PauseMenuView</c> (uGUI).
    /// Le layout vit dans PauseMenu.uxml et le style dans theme.uss : ce script
    /// ne fait que du cablage et de la logique, aucune mise en page.
    /// Comportement identique a l'ancien : Echap, timeScale 0, UIState, et
    /// synchronisation de la sensibilite entre la vue FPS et la vue TPS.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuDocument : MonoBehaviour
    {
        [Header("References")]
        public PlayerLook playerLook;

        [Tooltip("Controleur TPS (StarterAssets) : sa sensibilite camera est synchronisee avec la FPS.")]
        public StarterAssets.ThirdPersonController thirdPersonController;

        [Tooltip("Camera de la minimap (si null, recherchee au Start).")]
        public MinimapCamera minimapCamera;

        [Header("Input")]
        public Key openKey = Key.Escape;

        // ---- Elements de l'UXML ----
        private VisualElement settingsScreen;
        private VisualElement controlsScreen;
        private Slider volumeSlider;
        private Slider sensitivitySlider;
        private Slider minimapSizeSlider;
        private Slider minimapZoomSlider;
        private Label volumeValue;
        private Label minimapSizeValue;
        private Label minimapZoomValue;
        private TextField sensitivityField;

        // ---- Etat ----
        private bool isOpen;
        private float savedTimeScale;
        private float sensitivityBaseline = 15f;

        [Header("Plage de sensibilite")]
        public float sensMin = 0.1f;
        public float sensMax = 20f;

        // Reverrouillage du curseur differe en LateUpdate, apres tous les Update :
        // garantit qu'aucun autre script de la frame ne le rouvre derriere nous.
        //
        // A NE PAS TESTER DANS L'EDITEUR : en Play mode, l'editeur libere lui-meme
        // le curseur des qu'on appuie sur Echap ("In the Editor the cursor is
        // automatically reset when escape is pressed" - doc Cursor.lockState).
        // Le curseur semblera donc toujours deverrouille apres Echap, quoi que
        // fasse ce script. Verifiable uniquement dans un build.
        private bool pendingCursorLock;

        private Camera minimapCam;
        private RectTransform minimapRootRt;
        private float minimapBaseOrtho = 18f;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            settingsScreen = root.Q<VisualElement>("settings-screen");
            controlsScreen = root.Q<VisualElement>("controls-screen");

            volumeSlider = root.Q<Slider>("volume-slider");
            sensitivitySlider = root.Q<Slider>("sensitivity-slider");
            minimapSizeSlider = root.Q<Slider>("minimap-size-slider");
            minimapZoomSlider = root.Q<Slider>("minimap-zoom-slider");

            volumeValue = root.Q<Label>("volume-value");
            minimapSizeValue = root.Q<Label>("minimap-size-value");
            minimapZoomValue = root.Q<Label>("minimap-zoom-value");
            sensitivityField = root.Q<TextField>("sensitivity-field");

            ResolveReferences();
            BindControls(root);

            // Ferme au demarrage. Le UIDocument reste actif : c'est la racine
            // qu'on masque, sinon les Q<> seraient a refaire a chaque ouverture.
            SetVisible(false);
        }

        private void OnDisable()
        {
            // Si le menu etait ouvert lors d'un changement de scene, ne pas
            // laisser le compteur UIState et le timeScale desequilibres.
            if (!isOpen) return;
            isOpen = false;
            Time.timeScale = savedTimeScale;
            UIState.SetUIClosed();
        }

        private void ResolveReferences()
        {
            if (playerLook == null)
                playerLook = FindFirstObjectByType<PlayerLook>();
            if (thirdPersonController == null)
                thirdPersonController = FindFirstObjectByType<StarterAssets.ThirdPersonController>();

            if (playerLook != null && playerLook.mouseSensitivity > 0.01f)
                sensitivityBaseline = playerLook.mouseSensitivity;
            ApplyTpsSensitivity(playerLook != null ? playerLook.mouseSensitivity : sensitivityBaseline);

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
        }

        private void BindControls(VisualElement root)
        {
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(AudioListener.volume);
                volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
                UpdateVolumeLabel(volumeSlider.value);
            }

            if (sensitivitySlider != null)
            {
                sensitivitySlider.lowValue = sensMin;
                sensitivitySlider.highValue = sensMax;

                // SetValueWithoutNotify : ne jamais reecrire la sensibilite de la
                // scene au demarrage (l'ancienne version uGUI le faisait et
                // ecrasait la valeur reglee).
                float s = playerLook != null ? playerLook.mouseSensitivity : sensitivityBaseline;
                sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(s, sensMin, sensMax));
                sensitivitySlider.RegisterValueChangedCallback(OnSensitivityChanged);
            }

            if (sensitivityField != null)
            {
                sensitivityField.RegisterCallback<FocusInEvent>(OnFieldFocusIn);
                sensitivityField.RegisterCallback<FocusOutEvent>(OnFieldFocusOut);
                // Valider aussi sur Entree, sans attendre la perte de focus.
                sensitivityField.RegisterCallback<KeyDownEvent>(OnFieldKeyDown);
                SyncSensitivityField(playerLook != null ? playerLook.mouseSensitivity : sensitivityBaseline);
            }

            if (minimapSizeSlider != null)
            {
                float v = minimapRootRt != null ? minimapRootRt.sizeDelta.x : 240f;
                minimapSizeSlider.SetValueWithoutNotify(Mathf.Clamp(v, 120f, 420f));
                minimapSizeSlider.RegisterValueChangedCallback(OnMinimapSizeChanged);
                UpdateMinimapSizeLabel(minimapSizeSlider.value);
            }

            if (minimapZoomSlider != null)
            {
                minimapZoomSlider.SetValueWithoutNotify(1f);
                minimapZoomSlider.RegisterValueChangedCallback(OnMinimapZoomChanged);
                UpdateMinimapZoomLabel(1f);
            }

            var resume = root.Q<Button>("btn-resume");
            if (resume != null) resume.clicked += Close;

            var controls = root.Q<Button>("btn-controls");
            if (controls != null) controls.clicked += ShowControls;

            var back = root.Q<Button>("btn-controls-back");
            if (back != null) back.clicked += HideControls;

            var quit = root.Q<Button>("btn-quit");
            if (quit != null) quit.clicked += QuitGame;
        }

        // ====================================================================
        // Ouverture / fermeture
        // ====================================================================

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (UIState.IsInputFieldActive && !isOpen) return;
            if (!Keyboard.current[openKey].wasPressedThisFrame) return;

            if (isOpen) { UIState.ConsumeCloseKey(); Close(); return; }

            // Une autre UI vient de se fermer avec cette meme pression : on ne
            // doit pas enchainer sur l'ouverture du menu.
            if (UIState.WasCloseKeyConsumedThisFrame) return;

            if (!UIState.IsAnyUIOpen) Open();
        }

        private void Open()
        {
            isOpen = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            UIState.SetUIOpen();

            if (minimapCamera != null) minimapCamera.forceVisible = true;

            SetVisible(true);
            ShowSettings();

            // Resynchroniser : la sensibilite a pu changer ailleurs entre-temps.
            float s = playerLook != null ? playerLook.mouseSensitivity : sensitivityBaseline;
            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(s, sensMin, sensMax));
            SyncSensitivityField(s);

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

            SetVisible(false);

            // Applique en fin de frame (LateUpdate), pas ici : voir pendingCursorLock.
            pendingCursorLock = true;
        }

        private void LateUpdate()
        {
            if (!pendingCursorLock) return;
            pendingCursorLock = false;

            if (UIState.IsAnyUIOpen) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetVisible(bool visible)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            // Ne pas intercepter les clics quand le menu est ferme.
            root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        private void ShowSettings()
        {
            if (settingsScreen != null) settingsScreen.RemoveFromClassList("hidden");
            if (controlsScreen != null) controlsScreen.AddToClassList("hidden");
        }

        private void ShowControls()
        {
            if (settingsScreen != null) settingsScreen.AddToClassList("hidden");
            if (controlsScreen != null) controlsScreen.RemoveFromClassList("hidden");
        }

        private void HideControls()
        {
            ShowSettings();
        }

        // ====================================================================
        // Callbacks
        // ====================================================================

        private void OnVolumeChanged(ChangeEvent<float> evt)
        {
            AudioListener.volume = evt.newValue;
            UpdateVolumeLabel(evt.newValue);
        }

        private void OnSensitivityChanged(ChangeEvent<float> evt)
        {
            ApplySensitivity(evt.newValue);
            SyncSensitivityField(evt.newValue);
        }

        private void OnFieldFocusIn(FocusInEvent evt)
        {
            // Empeche Tab/Echap de declencher le journal pendant la saisie.
            UIState.IsInputFieldActive = true;
        }

        private void OnFieldFocusOut(FocusOutEvent evt)
        {
            UIState.IsInputFieldActive = false;
            CommitSensitivityField();
        }

        private void OnFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
            CommitSensitivityField();
            sensitivityField.Blur();
            evt.StopPropagation();
        }

        private void CommitSensitivityField()
        {
            if (sensitivityField == null) return;

            float val;
            if (!float.TryParse(sensitivityField.value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out val))
            {
                // Saisie invalide : on remet la valeur courante.
                SyncSensitivityField(playerLook != null ? playerLook.mouseSensitivity : sensitivityBaseline);
                return;
            }

            val = Mathf.Max(sensMin * 0.1f, val);
            ApplySensitivity(val);
            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(val, sensMin, sensMax));
            SyncSensitivityField(val);
        }

        private void ApplySensitivity(float value)
        {
            if (playerLook != null) playerLook.mouseSensitivity = value;
            ApplyTpsSensitivity(value);
        }

        /// <summary>
        /// Aligne la camera TPS sur la sensibilite FPS : multiplicateur = value / reference.
        /// </summary>
        private void ApplyTpsSensitivity(float value)
        {
            if (thirdPersonController == null || sensitivityBaseline <= 0.01f) return;
            thirdPersonController.LookSensitivity = value / sensitivityBaseline;
        }

        private void OnMinimapSizeChanged(ChangeEvent<float> evt)
        {
            if (minimapRootRt != null)
                minimapRootRt.sizeDelta = new Vector2(evt.newValue, evt.newValue);
            UpdateMinimapSizeLabel(evt.newValue);
        }

        private void OnMinimapZoomChanged(ChangeEvent<float> evt)
        {
            // value > 1 => zoome (orthoSize plus petit) ; value < 1 => dezoome.
            if (minimapCam != null)
                minimapCam.orthographicSize = minimapBaseOrtho / Mathf.Max(0.01f, evt.newValue);
            UpdateMinimapZoomLabel(evt.newValue);
        }

        // ====================================================================
        // Labels
        // ====================================================================

        private void SyncSensitivityField(float value)
        {
            if (sensitivityField == null) return;
            sensitivityField.SetValueWithoutNotify(value.ToString("F1"));
        }

        private void UpdateVolumeLabel(float value)
        {
            if (volumeValue != null)
                volumeValue.text = Mathf.RoundToInt(value * 100) + "%";
        }

        private void UpdateMinimapSizeLabel(float value)
        {
            if (minimapSizeValue != null)
                minimapSizeValue.text = Mathf.RoundToInt(value).ToString();
        }

        private void UpdateMinimapZoomLabel(float value)
        {
            if (minimapZoomValue != null)
                minimapZoomValue.text = "x" + value.ToString("F1");
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
