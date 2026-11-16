using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameUI : MonoBehaviour
{
    enum SettingsTab { General, MoveControls, Debug }

    public LevelManager levels;
    public PlayerHealth health;
    public Image[] hearts;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public Text levelText;
    public Image flash;

    // pause menu
    public GameObject pausePanel;
    public GameObject pauseCard;
    public Button pauseButton;
    public Button resumeButton;
    public Button retryButton;
    public Button restartButton;
    public Button settingsButton;
    public Button menuButton;

    // settings panel + tabs
    [Header("Settings & Move Controls")]
    public GameObject settingsPanel;
    public GameObject generalTabContent;
    public GameObject moveControlsTabContent;
    public GameObject debugTabContent;
    public Button generalTabButton;
    public Button moveControlsTabButton;
    public Button debugTabButton;
    public Button settingsBackButton;

    // general tab controls
    public Button settingsMoveModeButton;
    public Text settingsMoveModeValueText;
    public Button settingsViewModeButton;
    public Text settingsViewModeValueText;
    public Button settingsMusicButton;
    public Text settingsMusicValueText;
    public Button settingsSfxButton;
    public Text settingsSfxValueText;
    public Button settingsHapticsButton;
    public Text settingsHapticsValueText;

    // debug tab controls
    public Button debugPlaneVisualButton;
    public Text debugPlaneVisualValueText;

    // move controls tab & interactive overlay
    public ControlsLayoutManager controlsLayoutManager;
    public Button selectDpadButton;
    public Button selectJumpButton;
    public Slider settingsScaleSlider;
    public Text settingsScaleValueText;
    public Button resetControlsButton;
    public Button customizeOnScreenButton;

    public GameObject moveControlsOverlay;
    public Text overlaySelectedText;
    public Slider overlayScaleSlider;
    public Text overlayScaleValueText;
    public Button overlayResetButton;
    public Button overlayDoneButton;

    // main menu + level select
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public Button playButton;
    public Button backButton;
    public Button resetProgressButton;
    public Button[] levelButtons;
    public Text[] levelStatus;
    public Image[] levelCards;

    // everything that is only shown while playing
    [Header("Containers")]
    public GameObject hudSafeArea;
    public GameObject[] hudObjects;
    public GameObject arGuidanceHud;
    public Text coresText;
    public Text shieldText;

    float flashAlpha;
    bool paused;
    bool menuOpen;
    bool coresVisible;
    bool settingsOpen;
    bool onScreenLayoutActive;
    bool updatingSliders;

    public bool IsPaused { get { return paused; } }
    public bool IsMenuOpen { get { return menuOpen; } }

    void Start()
    {
        GameAudioManager.EnsureCreated();

        if (health != null)
        {
            health.Changed += OnHealth;
            health.Damaged += OnDamaged;
            OnHealth(health.Current, health.maxHealth);
        }
        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (menuButton != null) menuButton.onClick.AddListener(OnMainMenu);

        if (generalTabButton != null) generalTabButton.onClick.AddListener(() => SwitchSettingsTab(SettingsTab.General, true));
        if (moveControlsTabButton != null) moveControlsTabButton.onClick.AddListener(() => SwitchSettingsTab(SettingsTab.MoveControls, true));
        if (debugTabButton != null) debugTabButton.onClick.AddListener(() => SwitchSettingsTab(SettingsTab.Debug, true));
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (settingsMoveModeButton != null) settingsMoveModeButton.onClick.AddListener(OnSettingsToggleMoveMode);
        if (settingsViewModeButton != null) settingsViewModeButton.onClick.AddListener(OnSettingsToggleViewMode);
        if (settingsMusicButton != null) settingsMusicButton.onClick.AddListener(OnSettingsToggleMusic);
        if (settingsSfxButton != null) settingsSfxButton.onClick.AddListener(OnSettingsToggleSfx);
        if (settingsHapticsButton != null) settingsHapticsButton.onClick.AddListener(OnSettingsToggleHaptics);
        if (debugPlaneVisualButton != null) debugPlaneVisualButton.onClick.AddListener(OnSettingsToggleDebugPlaneVisual);

        if (selectDpadButton != null) selectDpadButton.onClick.AddListener(() => SelectControlForLayout(ControlsLayoutManager.ControlId.DPad));
        if (selectJumpButton != null) selectJumpButton.onClick.AddListener(() => SelectControlForLayout(ControlsLayoutManager.ControlId.Jump));
        if (settingsScaleSlider != null) settingsScaleSlider.onValueChanged.AddListener(OnScaleSliderChanged);
        if (resetControlsButton != null) resetControlsButton.onClick.AddListener(OnResetControlsClicked);
        if (customizeOnScreenButton != null) customizeOnScreenButton.onClick.AddListener(EnterOnScreenCustomize);

        if (overlayScaleSlider != null) overlayScaleSlider.onValueChanged.AddListener(OnScaleSliderChanged);
        if (overlayResetButton != null) overlayResetButton.onClick.AddListener(OnResetControlsClicked);
        if (overlayDoneButton != null) overlayDoneButton.onClick.AddListener(ExitOnScreenCustomize);

        if (controlsLayoutManager != null)
        {
            controlsLayoutManager.OnLayoutStateChanged += RefreshSettingsUI;
        }
        DualModeManager.OnModeChanged += HandleViewModeChanged;
        CubeController.OnStickyModeChanged += HandleMoveModeChanged;
        ARPlacementManager.OnPlacementStateChanged += HandlePlacementStateChanged;

        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (backButton != null) backButton.onClick.AddListener(OnBack);
        if (resetProgressButton != null) resetProgressButton.onClick.AddListener(OnResetProgress);
        if (levelButtons != null)
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int n = i + 1;
                if (levelButtons[i] != null) levelButtons[i].onClick.AddListener(() => OnLevelCard(n));
            }
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (moveControlsOverlay != null) moveControlsOverlay.SetActive(false);
        if (coresText != null) coresText.gameObject.SetActive(false);
        if (shieldText != null) shieldText.gameObject.SetActive(false);
        SetLevel(1);
        if (levels != null) levels.ShowMenu();
    }

    void OnDestroy()
    {
        if (health != null) { health.Changed -= OnHealth; health.Damaged -= OnDamaged; }
        if (controlsLayoutManager != null) controlsLayoutManager.OnLayoutStateChanged -= RefreshSettingsUI;
        DualModeManager.OnModeChanged -= HandleViewModeChanged;
        CubeController.OnStickyModeChanged -= HandleMoveModeChanged;
        ARPlacementManager.OnPlacementStateChanged -= HandlePlacementStateChanged;
    }

    void OnDisable() { Time.timeScale = 1f; }

    static void ClickFeedback()
    {
        GameAudioManager.Instance?.PlayUIClick();
        HapticsManager.LightTap();
    }

    void HandleViewModeChanged(DualModeManager.GameMode mode)
    {
        RefreshSettingsUI();
        if (!menuOpen && !settingsOpen)
        {
            if (mode == DualModeManager.GameMode.ARTabletop)
            {
                var pm = DualModeManager.Instance != null ? DualModeManager.Instance.arPlacementManager : null;
                bool placed = pm == null || pm.isPlaced;
                SetHud(placed);
            }
            else
            {
                bool isAR = DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop;
        var pm = DualModeManager.Instance != null ? DualModeManager.Instance.arPlacementManager : null;
        bool shouldShowHud = !isAR || (pm == null || pm.isPlaced);
        SetHud(shouldShowHud);
            }
        }
    }

    void HandlePlacementStateChanged(bool placed)
    {
        if (!menuOpen && !settingsOpen)
        {
            EnsureHudSafeAreaActive();
            SetHud(placed);
            if (placed && arGuidanceHud != null)
            {
                arGuidanceHud.SetActive(false);
            }
        }
    }

    void HandleMoveModeChanged(bool sticky)
    {
        RefreshSettingsUI();
    }

    // ---------------- menus ----------------
    public void EnsureHudSafeAreaActive()
    {
        if (hudSafeArea == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var tr = canvas.transform.Find("HUD_SafeArea");
                if (tr != null) hudSafeArea = tr.gameObject;
            }
        }
        if (hudSafeArea != null && !hudSafeArea.activeSelf)
        {
            hudSafeArea.SetActive(true);
        }
    }

    public void EnsureHudVisible()
    {
        if (menuOpen)
        {
            HideMenus();
        }
        else
        {
            EnsureHudSafeAreaActive();
            bool isAR = DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop;
            var pm = DualModeManager.Instance != null ? DualModeManager.Instance.arPlacementManager : null;
            bool shouldShowHud = !isAR || (pm == null || pm.isPlaced);
            SetHud(shouldShowHud);
            ApplyCoresVisibility();
        }
    }

    public void ShowMainMenu()
    {
        menuOpen = true;
        paused = false;
        settingsOpen = false;
        HapticsManager.CancelAll();
        GameAudioManager.Instance?.StopAllSfx();
        var cc = FindFirstObjectByType<CubeController>(FindObjectsInactive.Include);
        if (cc != null) cc.StopMovement();
        if (onScreenLayoutActive) ExitOnScreenCustomize();
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        SetHud(false);
        if (arGuidanceHud != null) arGuidanceHud.SetActive(false);
        if (hudSafeArea != null) hudSafeArea.SetActive(false);
        ApplyCoresVisibility();
        Time.timeScale = 0f;
    }

    void HideMenus()
    {
        menuOpen = false;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        EnsureHudSafeAreaActive();
        bool isAR = DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop;
        var pm = DualModeManager.Instance != null ? DualModeManager.Instance.arPlacementManager : null;
        bool shouldShowHud = !isAR || (pm == null || pm.isPlaced);
        SetHud(shouldShowHud);
        ApplyCoresVisibility();
        Time.timeScale = 1f;
    }

    void OnPlay()
    {
        ClickFeedback();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (arGuidanceHud != null) arGuidanceHud.SetActive(false);
        if (hudSafeArea != null) hudSafeArea.SetActive(false);
        RefreshLevelCards();
    }

    void OnBack()
    {
        ClickFeedback();
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (arGuidanceHud != null) arGuidanceHud.SetActive(false);
        if (hudSafeArea != null) hudSafeArea.SetActive(false);
    }

    void OnLevelCard(int n)
    {
        if (levels == null || !levels.IsUnlocked(n)) return;
        ClickFeedback();
        HideMenus();
        levels.StartLevel(n);
    }

    void OnResetProgress()
    {
        ClickFeedback();
        if (levels != null) levels.ResetProgress();
        RefreshLevelCards();
    }

    void OnMainMenu()
    {
        ClickFeedback();
        Resume();
        if (levels != null) levels.ShowMenu();
    }

    // Finished levels can be replayed, the next level is playable, later levels stay locked.
    public void RefreshLevelCards()
    {
        if (levels == null || levelButtons == null) return;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int n = i + 1;
            bool done = levels.IsCompleted(n);
            bool open = levels.IsUnlocked(n);
            if (levelButtons[i] != null) levelButtons[i].interactable = open;
            if (levelStatus != null && i < levelStatus.Length && levelStatus[i] != null)
                levelStatus[i].text = done ? "COMPLETED\nREPLAY" : (open ? "READY\nPLAY" : "LOCKED");
            if (levelCards != null && i < levelCards.Length && levelCards[i] != null)
                levelCards[i].color = done ? new Color(0.1f, 0.55f, 0.42f, 0.96f)
                                    : (open ? new Color(0.1f, 0.42f, 0.78f, 0.96f) : new Color(0.14f, 0.16f, 0.24f, 0.92f));
        }
    }

    void SetHud(bool v)
    {
        if (v) EnsureHudSafeAreaActive();
        if (hudObjects != null)
        {
            foreach (var o in hudObjects) if (o != null) o.SetActive(v);
        }
        if (DualModeManager.Instance != null && DualModeManager.Instance.repositionButton != null)
        {
            bool isAR = DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop;
            var pm = DualModeManager.Instance.arPlacementManager;
            bool arPlaced = pm == null || pm.isPlaced;
            DualModeManager.Instance.repositionButton.gameObject.SetActive(v && isAR && arPlaced);
        }
        if (v && arGuidanceHud != null && arGuidanceHud.activeSelf)
        {
            arGuidanceHud.SetActive(false);
        }
    }

    // ---------------- level 4 energy cores / shield ----------------
    public void SetCores(int cur, int total)
    {
        string label = (levels != null && levels.CurrentLevel == 5) ? "ORBS " : "CORES ";
        string status = (cur >= total) ? ((levels != null && levels.CurrentLevel == 5) ? "  -  VAULT OPEN" : "  -  GATE OPEN") : "";
        if (coresText != null) coresText.text = label + cur + " / " + total + status;
    }

    public void SetCoresVisible(bool v)
    {
        coresVisible = v;
        ApplyCoresVisibility();
    }

    void ApplyCoresVisibility()
    {
        if (coresText != null) coresText.gameObject.SetActive(coresVisible && !menuOpen);
    }

    // ---------------- hearts / level label / flash ----------------
    void OnRetry() { ClickFeedback(); Resume(); if (levels != null) levels.RetryLevel(); }
    void OnRestart() { ClickFeedback(); Resume(); if (levels != null) levels.RestartGame(); }

    void OnHealth(int cur, int max)
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            bool full = i < cur;
            hearts[i].sprite = full ? heartFull : heartEmpty;
            hearts[i].color = full ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            hearts[i].gameObject.SetActive(i < max);
        }
    }

    void OnDamaged() { flashAlpha = 0.55f; }

    public void SetLevel(int n)
    {
        if (levelText != null) levelText.text = "LEVEL " + n + " / " + LevelManager.LevelCount;
    }

    // ---------------- pause & settings ----------------
    public void TogglePause()
    {
        ClickFeedback();
        if (onScreenLayoutActive)
        {
            ExitOnScreenCustomize();
            return;
        }
        if (paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (menuOpen) return;
        paused = true;
        settingsOpen = false;
        Time.timeScale = 0f;
        HapticsManager.CancelAll();
        GameAudioManager.Instance?.StopAllSfx();
        var cc = FindFirstObjectByType<CubeController>(FindObjectsInactive.Include);
        if (cc != null) cc.StopMovement();
        if (arGuidanceHud != null) arGuidanceHud.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseCard != null) pauseCard.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (moveControlsOverlay != null) moveControlsOverlay.SetActive(false);
    }

    public void Resume()
    {
        paused = false;
        settingsOpen = false;
        if (onScreenLayoutActive)
        {
            onScreenLayoutActive = false;
            if (controlsLayoutManager != null) controlsLayoutManager.SetEditingMode(false);
            if (moveControlsOverlay != null) moveControlsOverlay.SetActive(false);
        }
        if (!menuOpen)
        {
            Time.timeScale = 1f;
            bool isAR = DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop;
            var pm = DualModeManager.Instance != null ? DualModeManager.Instance.arPlacementManager : null;
            bool shouldShowHud = !isAR || (pm == null || pm.isPlaced);
            SetHud(shouldShowHud);
            ApplyCoresVisibility();
        }
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseCard != null) pauseCard.SetActive(true);
    }

    public void OpenSettings()
    {
        ClickFeedback();
        settingsOpen = true;
        if (arGuidanceHud != null) arGuidanceHud.SetActive(false);
        if (pauseCard != null) pauseCard.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        SwitchSettingsTab(SettingsTab.General, false);
        RefreshSettingsUI();
    }

    public void CloseSettings()
    {
        ClickFeedback();
        settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseCard != null) pauseCard.SetActive(true);
    }

    void SwitchSettingsTab(SettingsTab tab, bool withSound)
    {
        if (withSound) ClickFeedback();
        if (generalTabContent != null) generalTabContent.SetActive(tab == SettingsTab.General);
        if (moveControlsTabContent != null) moveControlsTabContent.SetActive(tab == SettingsTab.MoveControls);
        if (debugTabContent != null) debugTabContent.SetActive(tab == SettingsTab.Debug);

        SetTabColor(generalTabButton, tab == SettingsTab.General);
        SetTabColor(moveControlsTabButton, tab == SettingsTab.MoveControls);
        SetTabColor(debugTabButton, tab == SettingsTab.Debug);
        RefreshSettingsUI();
    }

    static void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? new Color(0.12f, 0.55f, 0.85f, 1f) : new Color(0.12f, 0.18f, 0.34f, 0.9f);
    }

    void OnSettingsToggleMoveMode()
    {
        CubeController.PreferredStickyMode = !CubeController.PreferredStickyMode;
        HapticsManager.SuppressFor(0.25f);
        GameAudioManager.Instance?.StopAllSfx();
        GameAudioManager.Instance?.PlayUIClick();
        RefreshSettingsUI();
    }

    void OnSettingsToggleViewMode()
    {
        if (DualModeManager.Instance != null)
        {
            DualModeManager.Instance.ToggleMode();
        }
        RefreshSettingsUI();
    }

    void OnSettingsToggleMusic()
    {
        var am = GameAudioManager.Instance;
        if (am != null) am.MusicEnabled = !am.MusicEnabled;
        ClickFeedback();
        RefreshSettingsUI();
    }

    void OnSettingsToggleSfx()
    {
        var am = GameAudioManager.Instance;
        if (am != null) am.SfxEnabled = !am.SfxEnabled;
        ClickFeedback();
        RefreshSettingsUI();
    }

    void OnSettingsToggleHaptics()
    {
        HapticsManager.Enabled = !HapticsManager.Enabled;
        ClickFeedback();
        RefreshSettingsUI();
    }

    void OnSettingsToggleDebugPlaneVisual()
    {
        ClickFeedback();
        ARPlacementManager.SetDebugPlaneVisualsEnabled(!ARPlacementManager.DebugPlaneVisualsEnabled);
        RefreshSettingsUI();
    }

    void SelectControlForLayout(ControlsLayoutManager.ControlId id)
    {
        ClickFeedback();
        if (controlsLayoutManager != null) controlsLayoutManager.SelectControl(id);
        RefreshSettingsUI();
    }

    void OnScaleSliderChanged(float val)
    {
        if (updatingSliders || controlsLayoutManager == null) return;
        controlsLayoutManager.SetSelectedScaleFromSlider(val);
    }

    void OnResetControlsClicked()
    {
        ClickFeedback();
        if (controlsLayoutManager != null) controlsLayoutManager.ResetToDefaults();
        RefreshSettingsUI();
    }

    public void EnterOnScreenCustomize()
    {
        if (controlsLayoutManager == null) return;
        ClickFeedback();
        onScreenLayoutActive = true;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (arGuidanceHud != null) arGuidanceHud.SetActive(false);

        // Hide all HUD objects except DPadCluster & JumpCluster so screen is uncluttered while dragging
        SetHud(false);
        if (controlsLayoutManager.dpadCluster != null) controlsLayoutManager.dpadCluster.gameObject.SetActive(true);
        if (controlsLayoutManager.jumpButton != null) controlsLayoutManager.jumpButton.gameObject.SetActive(true);
        if (moveControlsOverlay != null) moveControlsOverlay.SetActive(true);

        controlsLayoutManager.SetEditingMode(true);
        RefreshSettingsUI();
    }

    public void ExitOnScreenCustomize()
    {
        ClickFeedback();
        onScreenLayoutActive = false;
        if (controlsLayoutManager != null) controlsLayoutManager.SetEditingMode(false);
        if (moveControlsOverlay != null) moveControlsOverlay.SetActive(false);

        bool isAR = DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop;
        var pm = DualModeManager.Instance != null ? DualModeManager.Instance.arPlacementManager : null;
        bool shouldShowHud = !isAR || (pm == null || pm.isPlaced);
        SetHud(shouldShowHud);
        ApplyCoresVisibility();
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseCard != null) pauseCard.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        SwitchSettingsTab(SettingsTab.MoveControls, false);
    }

    void RefreshSettingsUI()
    {
        if (settingsMoveModeValueText != null)
        {
            settingsMoveModeValueText.text = CubeController.PreferredStickyMode ? "TAP TO MOVE" : "HOLD TO MOVE";
        }
        if (settingsViewModeValueText != null && DualModeManager.Instance != null)
        {
            settingsViewModeValueText.text = (DualModeManager.Instance.currentMode == DualModeManager.GameMode.Normal3D)
                ? "3D NORMAL"
                : "AR TABLETOP";
        }

        var am = GameAudioManager.Instance;
        if (settingsMusicValueText != null && am != null)
        {
            settingsMusicValueText.text = am.MusicEnabled ? "ON" : "OFF";
        }
        if (settingsSfxValueText != null && am != null)
        {
            settingsSfxValueText.text = am.SfxEnabled ? "ON" : "OFF";
        }
        if (settingsHapticsValueText != null)
        {
            settingsHapticsValueText.text = HapticsManager.Enabled ? "ON" : "OFF";
        }
        if (debugPlaneVisualValueText != null)
        {
            debugPlaneVisualValueText.text = ARPlacementManager.DebugPlaneVisualsEnabled
                ? "ON  (DOTTED LIGHT GREY)"
                : "OFF  (ALPHA = 0)";
        }

        if (controlsLayoutManager != null)
        {
            bool isDpad = controlsLayoutManager.SelectedControl == ControlsLayoutManager.ControlId.DPad;
            if (selectDpadButton != null)
            {
                var img = selectDpadButton.GetComponent<Image>();
                if (img != null) img.color = isDpad ? new Color(0.10f, 0.65f, 0.55f, 1f) : new Color(0.14f, 0.20f, 0.36f, 0.9f);
            }
            if (selectJumpButton != null)
            {
                var img = selectJumpButton.GetComponent<Image>();
                if (img != null) img.color = !isDpad ? new Color(0.10f, 0.65f, 0.55f, 1f) : new Color(0.14f, 0.20f, 0.36f, 0.9f);
            }

            float pct = Mathf.RoundToInt(controlsLayoutManager.GetSelectedScale() * 100f);
            string selName = isDpad ? "D-PAD" : "STOP & JUMP";
            string scaleStr = "SIZE: " + pct + "% (75% - 135%)";
            if (settingsScaleValueText != null) settingsScaleValueText.text = scaleStr;
            if (overlayScaleValueText != null) overlayScaleValueText.text = selName + " " + pct + "%";
            if (overlaySelectedText != null) overlaySelectedText.text = "DRAG CONTROLS ON SCREEN  |  SELECTED: " + selName;

            updatingSliders = true;
            float t = controlsLayoutManager.GetNormalizedScaleSliderValue();
            if (settingsScaleSlider != null) settingsScaleSlider.value = t;
            if (overlayScaleSlider != null) overlayScaleSlider.value = t;
            updatingSliders = false;
        }
    }

    void Update()
    {
        if (flash != null)
        {
            if (flashAlpha > 0f) flashAlpha = Mathf.Max(0f, flashAlpha - Time.unscaledDeltaTime * 1.6f);
            var c = flash.color; c.a = flashAlpha; flash.color = c;
        }
        if (shieldText != null)
        {
            bool on = health != null && health.IsShielded && !menuOpen && !onScreenLayoutActive;
            if (shieldText.gameObject.activeSelf != on) shieldText.gameObject.SetActive(on);
            if (on) shieldText.text = "SHIELD  " + Mathf.CeilToInt(health.ShieldTime) + "s";
        }
        if (menuOpen) return;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null && (k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame))
        {
            if (onScreenLayoutActive) ExitOnScreenCustomize();
            else if (settingsOpen) CloseSettings();
            else TogglePause();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (onScreenLayoutActive) ExitOnScreenCustomize();
            else if (settingsOpen) CloseSettings();
            else TogglePause();
        }
#endif
    }
}
