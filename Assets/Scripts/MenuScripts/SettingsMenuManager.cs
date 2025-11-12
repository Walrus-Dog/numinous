using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SettingsMenuManager : MonoBehaviour
{
    public static bool isVibrate;

    [Header("UI")]
    public TMP_Dropdown graphicsDropdown;
    public Slider masterVol, musicVol, sfxVol;
    public Toggle vibrateToggle;

    [Header("Brightness")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Image uiOverlay;

    [Header("Controls")]
    [SerializeField] private Slider sensitivitySlider;

    [Header("Audio")]
    public AudioMixer MainMixer;

    [Header("Display / Resolution")]
    public TMP_Dropdown resolutionDropdown;   // NEW
    public TMP_Dropdown displayModeDropdown;  // NEW

    private Resolution[] availableResolutions; // NEW

    private Volume globalVolume;
    private ColorAdjustments colorAdjustments;

    private const string BrightnessKey = "BrightnessValue";
    private const string SensitivityKey = "MouseSensitivity";

    [SerializeField] private float qualityApplyDelay = 0.1f;
    private Coroutine qualityApplyRoutine;
    private int? pendingQualityLevel;

    [SerializeField] private string settingsMenuRootName = "SettingsMenuScreen";

    private Camera lastActiveCamera;
    private AudioListener listener;

    // === Defaults used by Reset All ===
    private const int DefaultQualityLevel = 2;
    private const float DefaultMaster = 0.75f;
    private const float DefaultMusic = 0.75f;
    private const float DefaultSfx = 0.75f;
    private const bool DefaultVibrate = true;
    private const float DefaultBrightness = 0f;
    private const float DefaultSensitivity = 1f;

    // Display prefs keys (NEW)
    private const string PP_ResIndex = "ResolutionIndex";
    private const string PP_DisplayMode = "DisplayMode"; // 0=Fullscreen, 1=Borderless, 2=Windowed

    // === VOLUME HELPERS (linear <-> dB) ===
    private static float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return -80f;
        return Mathf.Log10(linear) * 20f;
    }

    private static float DbToLinear(float dB)
    {
        return Mathf.Pow(10f, dB / 20f);
    }

    // Load a volume from PlayerPrefs as linear [0..1], migrating legacy dB (–80..0) to linear once.
    private float LoadVolumeLinear(string key, float defaultLinear)
    {
        const float Sentinel = -999f;
        float stored = PlayerPrefs.GetFloat(key, Sentinel);

        if (stored == Sentinel)
            return defaultLinear; // not set yet

        if (stored < 0f || stored > 1f) // legacy dB
        {
            float clampedDb = Mathf.Clamp(stored, -80f, 0f);
            float linear = DbToLinear(clampedDb);
            PlayerPrefs.SetFloat(key, linear);
            PlayerPrefs.Save();
            return Mathf.Clamp01(linear);
        }

        return Mathf.Clamp01(stored);
    }

    void Start()
    {
        InitializeSettings();
        InitializeBrightness();
        InitResolutionUI();    // NEW
        InitDisplayModeUI();   // NEW

        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureActiveAudioListener();
        StartCoroutine(TrackActiveCamera());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeBrightness();
        ApplySavedSensitivity();
        EnsureActiveAudioListener();
    }

    // === INITIALIZATION ===
    private void InitializeSettings()
    {
        // Graphics
        if (graphicsDropdown)
        {
            // Auto-populate with actual quality names
            graphicsDropdown.ClearOptions();
            graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));

            graphicsDropdown.value = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
            graphicsDropdown.RefreshShownValue();
        }
        QualitySettings.SetQualityLevel(graphicsDropdown ? graphicsDropdown.value : QualitySettings.GetQualityLevel());

        // Audio (linear sliders 0..1, with migration)
        float masterValue = LoadVolumeLinear("MasterVolume", DefaultMaster);
        float musicValue = LoadVolumeLinear("MusicVolume", DefaultMusic);
        float sfxValue = LoadVolumeLinear("SFXVolume", DefaultSfx);

        if (masterVol) { masterVol.minValue = 0f; masterVol.maxValue = 1f; masterVol.value = masterValue; }
        if (musicVol) { musicVol.minValue = 0f; musicVol.maxValue = 1f; musicVol.value = musicValue; }
        if (sfxVol) { sfxVol.minValue = 0f; sfxVol.maxValue = 1f; sfxVol.value = sfxValue; }

        SetMixerVolume("MasterVolume", masterValue);
        SetMixerVolume("MusicVolume", musicValue);
        SetMixerVolume("SFXVolume", sfxValue);

        // Vibrate
        bool vibrateValue = PlayerPrefs.GetInt("Vibrate", DefaultVibrate ? 1 : 0) == 1;
        if (vibrateToggle) vibrateToggle.isOn = vibrateValue;
        isVibrate = vibrateValue;

        // Sensitivity
        float savedSensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
        if (sensitivitySlider == null)
            sensitivitySlider = GameObject.Find("SensitivitySlider")?.GetComponent<Slider>();
        if (sensitivitySlider)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 2.0f;
            sensitivitySlider.value = savedSensitivity;
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(delegate { ChangeSensitivity(); });
        }

        // Listeners
        if (graphicsDropdown) graphicsDropdown.onValueChanged.AddListener(delegate { ChangeGraphicsQuality(); });
        if (masterVol) masterVol.onValueChanged.AddListener(delegate { ChangeMasterVolume(); });
        if (musicVol) musicVol.onValueChanged.AddListener(delegate { ChangeMusicVolume(); });
        if (sfxVol) sfxVol.onValueChanged.AddListener(delegate { ChangeSFXVolume(); });
        if (vibrateToggle) vibrateToggle.onValueChanged.AddListener(delegate { ChangeVibrate(); });

        // Apply to player
        ApplySavedSensitivity();
    }

    private void InitializeBrightness()
    {
        // Reset cached refs
        globalVolume = null;
        colorAdjustments = null;

        // 1) Find a Volume in the *active scene* that actually has ColorAdjustments
        var activeScene = SceneManager.GetActiveScene();
        var volumes = Object.FindObjectsByType<Volume>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var v in volumes)
        {
            if (v == null) continue;
            if (v.gameObject.scene != activeScene) continue; // ignore other scenes / DDOL

            if (v.profile != null && v.profile.TryGet(out ColorAdjustments ca))
            {
                globalVolume = v;
                colorAdjustments = ca;
                break;
            }
        }

        if (globalVolume == null || colorAdjustments == null)
        {
            Debug.LogWarning($"[SettingsMenuManager] No Volume with ColorAdjustments found in scene '{activeScene.name}'.");
            return;
        }

        // 2) Wire up UI references (scene-local)
        if (brightnessSlider == null)
            brightnessSlider = GameObject.Find("BrightnessSlider")?.GetComponent<Slider>();
        if (uiOverlay == null)
            uiOverlay = GameObject.Find("UIBrightnessOverlay")?.GetComponent<Image>();

        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, DefaultBrightness);
        ApplyBrightness(savedBrightness);

        if (brightnessSlider)
        {
            brightnessSlider.minValue = -2f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        Debug.Log($"[SettingsMenuManager] Brightness wired to Volume '{globalVolume.name}' in scene '{activeScene.name}'.");
    }


    // === DISPLAY / RESOLUTION (NEW) ===

    private void InitResolutionUI()
    {
        if (!resolutionDropdown) return;

        // Build unique resolution list by width/height
        List<Resolution> unique = new List<Resolution>();
        foreach (var r in Screen.resolutions)
        {
            if (!unique.Exists(u => u.width == r.width && u.height == r.height))
                unique.Add(r);
        }
        availableResolutions = unique.ToArray();

        List<string> labels = new List<string>();
        int currentIndex = 0;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var r = availableResolutions[i];
            labels.Add($"{r.width} x {r.height}");
            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);

        int savedIndex = PlayerPrefs.GetInt(PP_ResIndex, currentIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, availableResolutions.Length - 1));
        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void InitDisplayModeUI()
    {
        if (!displayModeDropdown) return;

        int savedMode = PlayerPrefs.GetInt(PP_DisplayMode, 0); // 0=Fullscreen,1=Borderless,2=Windowed
        savedMode = Mathf.Clamp(savedMode, 0, 2);

        displayModeDropdown.value = savedMode;
        displayModeDropdown.RefreshShownValue();

        displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
    }

    private FullScreenMode GetFullScreenModeFromIndex(int index)
    {
        switch (index)
        {
            case 0: return FullScreenMode.ExclusiveFullScreen; // "Fullscreen"
            case 1: return FullScreenMode.FullScreenWindow;    // "Borderless"
            default: return FullScreenMode.Windowed;           // "Windowed"
        }
    }

    private int GetCurrentDisplayModeIndex()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(PP_DisplayMode, 0), 0, 2);
    }

    private void ApplyResolutionAndMode(int resIndex, int modeIndex)
    {
        if (availableResolutions == null ||
            availableResolutions.Length == 0) return;

        resIndex = Mathf.Clamp(resIndex, 0, availableResolutions.Length - 1);
        var res = availableResolutions[resIndex];
        var mode = GetFullScreenModeFromIndex(modeIndex);

#if UNITY_6000_0_OR_NEWER
        Screen.SetResolution(res.width, res.height, mode);
#else
        // Fallback: fullscreen bool based on mode (Windowed vs other)
        bool fullscreen = mode != FullScreenMode.Windowed;
        Screen.fullScreenMode = mode;
        Screen.SetResolution(res.width, res.height, fullscreen);
#endif

        PlayerPrefs.SetInt(PP_ResIndex, resIndex);
        PlayerPrefs.SetInt(PP_DisplayMode, modeIndex);
        PlayerPrefs.Save();
    }

    public void OnResolutionChanged(int index)
    {
        int modeIndex = GetCurrentDisplayModeIndex();
        ApplyResolutionAndMode(index, modeIndex);
    }

    public void OnDisplayModeChanged(int index)
    {
        int resIndex = resolutionDropdown ? resolutionDropdown.value : 0;
        ApplyResolutionAndMode(resIndex, index);
    }

    // === SETTINGS CHANGES ===
    public void ChangeGraphicsQuality()
    {
        if (graphicsDropdown) graphicsDropdown.Hide();
        StartCoroutine(BlockerKillerBurst());

        int lvl = graphicsDropdown.value;
        PlayerPrefs.SetInt("GraphicsQuality", lvl);

        if (qualityApplyRoutine != null) StopCoroutine(qualityApplyRoutine);

        bool isPaused = false;
        try { isPaused = PauseMenu.Paused; } catch { isPaused = false; }

        if (isPaused)
        {
            pendingQualityLevel = lvl;
            if (qualityApplyRoutine == null)
                qualityApplyRoutine = StartCoroutine(ApplyQualityWhenUnpaused());
        }
        else
        {
            qualityApplyRoutine = StartCoroutine(ApplyQualityDeferred(lvl));
        }
    }

    public void ChangeMasterVolume()
    {
        if (!masterVol) return;
        PlayerPrefs.SetFloat("MasterVolume", masterVol.value);
        SetMixerVolume("MasterVolume", masterVol.value);
    }

    public void ChangeMusicVolume()
    {
        if (!musicVol) return;
        PlayerPrefs.SetFloat("MusicVolume", musicVol.value);
        SetMixerVolume("MusicVolume", musicVol.value);
    }

    public void ChangeSFXVolume()
    {
        if (!sfxVol) return;
        PlayerPrefs.SetFloat("SFXVolume", sfxVol.value);
        SetMixerVolume("SFXVolume", sfxVol.value);
    }

    public void ChangeVibrate()
    {
        if (!vibrateToggle) return;
        isVibrate = vibrateToggle.isOn;
        PlayerPrefs.SetInt("Vibrate", isVibrate ? 1 : 0);
    }

    // === SENSITIVITY ===
    public void ChangeSensitivity()
    {
        if (!sensitivitySlider) return;

        float value = sensitivitySlider.value;
        PlayerPrefs.SetFloat(SensitivityKey, value);
        PlayerPrefs.Save();

        ApplySensitivityToPlayer(value);
    }

    private void ApplySavedSensitivity()
    {
        float value = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
        ApplySensitivityToPlayer(value);
    }

    private void ApplySensitivityToPlayer(float value)
    {
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null) player.SetSensitivity(value);
    }

    // === BRIGHTNESS ===
    public void SetBrightness(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(BrightnessKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness(float value)
    {
        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = value;

        if (uiOverlay != null)
        {
            float alpha = Mathf.InverseLerp(-2f, 2f, -value) * 0.6f;
            uiOverlay.color = new Color(0f, 0f, 0f, alpha);
        }
    }

    // === HELPERS ===
    private void SetMixerVolume(string parameter, float linearValue)
    {
        if (!MainMixer) return;
        MainMixer.SetFloat(parameter, LinearToDb(Mathf.Clamp01(linearValue)));
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator ApplyQualityWhenUnpaused()
    {
        while (true)
        {
            bool isPausedNow = false;
            try { isPausedNow = PauseMenu.Paused; } catch { isPausedNow = false; }
            if (!isPausedNow) break;
            yield return null;
        }

        if (pendingQualityLevel.HasValue)
        {
            int lvl = pendingQualityLevel.Value;
            pendingQualityLevel = null;
            yield return StartCoroutine(ApplyQualityDeferred(lvl));
        }

        qualityApplyRoutine = null;
    }

    private IEnumerator ApplyQualityDeferred(int level)
    {
        QualitySettings.SetQualityLevel(level, applyExpensiveChanges: false);
        yield return null;

        float t = 0f;
        while (t < qualityApplyDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
        yield return null;

        InitializeBrightness();
        yield return StartCoroutine(BlockerKillerBurst());

        if (graphicsDropdown) graphicsDropdown.RefreshShownValue();
        qualityApplyRoutine = null;
    }

    private IEnumerator BlockerKillerBurst()
    {
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            RemoveAllKnownDropdownArtifacts();
            ForceRestoreSettingsMenuScreen();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (EventSystem.current == null)
            {
                var esGO = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esGO.AddComponent<StandaloneInputModule>();
#endif
            }
            Canvas.ForceUpdateCanvases();
            EventSystem.current.SetSelectedGameObject(null);

            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
    }

    private void RemoveAllKnownDropdownArtifacts()
    {
        try
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                var go = all[i];
                if (!go) continue;

                if (go.name == "TMP Dropdown Blocker" ||
                    go.name == "TMP Dropdown List" ||
                    go.name == "Blocker" ||
                    go.name == "Dropdown List")
                {
                    if (string.IsNullOrEmpty(go.scene.name))
                        go.SetActive(false);
                    else
                        Object.Destroy(go);
                }
            }
        }
        catch { }
    }

    private void ForceRestoreSettingsMenuScreen()
    {
        GameObject settingsRoot = GameObject.Find(settingsMenuRootName);
        if (settingsRoot == null && graphicsDropdown != null)
            settingsRoot = graphicsDropdown.GetComponentInParent<Canvas>(true)?.gameObject;
        if (settingsRoot == null) return;

        if (!settingsRoot.activeSelf) settingsRoot.SetActive(true);

        var cg = settingsRoot.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        var ray = settingsRoot.GetComponent<GraphicRaycaster>();
        if (ray) ray.enabled = true;

        var canvas = settingsRoot.GetComponentInParent<Canvas>(true);
        if (canvas)
        {
            canvas.enabled = true;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 1500) canvas.sortingOrder = 1500;
        }

        var images = settingsRoot.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (!img) continue;
            if (img.raycastTarget)
            {
                var rect = img.rectTransform != null ? img.rectTransform.rect : new Rect(0, 0, 0, 0);
                if (rect.width > 1000f && rect.height > 600f)
                    img.raycastTarget = false;
            }
        }
    }

    private void EnsureActiveAudioListener()
    {
        listener = Object.FindFirstObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                listener = cam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[SettingsMenuManager] No AudioListener found. Added one to Main Camera.");
            }
            else
            {
                GameObject temp = new GameObject("TempAudioListener");
                listener = temp.AddComponent<AudioListener>();
                Debug.LogWarning("[SettingsMenuManager] No Main Camera found. Created a temporary AudioListener object.");
            }
        }
        else
        {
            if (!listener.enabled)
            {
                listener.enabled = true;
                Debug.Log("[SettingsMenuManager] AudioListener found but was disabled. Enabled it.");
            }

            AudioListener[] allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 1; i < allListeners.Length; i++)
            {
                allListeners[i].enabled = false;
                Debug.LogWarning($"[SettingsMenuManager] Disabled extra AudioListener on {allListeners[i].gameObject.name}");
            }
        }

        lastActiveCamera = Camera.main;
    }

    private IEnumerator TrackActiveCamera()
    {
        while (true)
        {
            Camera currentCam = Camera.main;
            if (currentCam != null && currentCam != lastActiveCamera)
            {
                if (listener != null)
                {
                    listener.transform.SetParent(currentCam.transform, false);
                    listener.transform.localPosition = Vector3.zero;
                    listener.transform.localRotation = Quaternion.identity;
                    Debug.Log($"[SettingsMenuManager] AudioListener moved to new active camera: {currentCam.name}");
                }
                lastActiveCamera = currentCam;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ===============================
    // === RESET ALL (Button hook) ===
    // ===============================
    public void OnClick_ResetAll()
    {
        StartCoroutine(ResetAllRoutine());
    }

    private IEnumerator ResetAllRoutine()
    {
        // 1) Reset PlayerPrefs
        PlayerPrefs.SetInt("GraphicsQuality", ClampQuality(DefaultQualityLevel));
        PlayerPrefs.SetFloat("MasterVolume", DefaultMaster);
        PlayerPrefs.SetFloat("MusicVolume", DefaultMusic);
        PlayerPrefs.SetFloat("SFXVolume", DefaultSfx);
        PlayerPrefs.SetInt("Vibrate", DefaultVibrate ? 1 : 0);
        PlayerPrefs.SetFloat(BrightnessKey, DefaultBrightness);
        PlayerPrefs.SetFloat(SensitivityKey, DefaultSensitivity);

        // Optional: reset display to current native
        PlayerPrefs.SetInt(PP_DisplayMode, 0); // Fullscreen
        PlayerPrefs.SetInt(PP_ResIndex, Mathf.Clamp(availableResolutions != null ? availableResolutions.Length - 1 : 0, 0, 100));

        PlayerPrefs.Save();

        // 2) UI -> defaults
        if (graphicsDropdown)
        {
            graphicsDropdown.value = ClampQuality(DefaultQualityLevel);
            graphicsDropdown.RefreshShownValue();
        }
        if (masterVol) masterVol.value = DefaultMaster;
        if (musicVol) musicVol.value = DefaultMusic;
        if (sfxVol) sfxVol.value = DefaultSfx;

        if (vibrateToggle) vibrateToggle.isOn = DefaultVibrate;

        if (brightnessSlider) brightnessSlider.value = DefaultBrightness;
        if (sensitivitySlider) sensitivitySlider.value = Mathf.Clamp(DefaultSensitivity, 0.1f, 2.0f);

        if (resolutionDropdown && availableResolutions != null && availableResolutions.Length > 0)
        {
            int idx = Mathf.Clamp(availableResolutions.Length - 1, 0, availableResolutions.Length - 1);
            resolutionDropdown.value = idx;
            resolutionDropdown.RefreshShownValue();
        }
        if (displayModeDropdown)
        {
            displayModeDropdown.value = 0;
            displayModeDropdown.RefreshShownValue();
        }

        // 3) Apply systems
        SetMixerVolume("MasterVolume", DefaultMaster);
        SetMixerVolume("MusicVolume", DefaultMusic);
        SetMixerVolume("SFXVolume", DefaultSfx);

        ApplyBrightness(DefaultBrightness);
        ApplySensitivityToPlayer(DefaultSensitivity);

        // Re-apply resolution+mode
        if (availableResolutions != null && availableResolutions.Length > 0)
        {
            int idx = Mathf.Clamp(availableResolutions.Length - 1, 0, availableResolutions.Length - 1);
            ApplyResolutionAndMode(idx, 0);
        }

        // 4) Apply quality safely (even if paused)
        int q = ClampQuality(DefaultQualityLevel);
        if (qualityApplyRoutine != null) StopCoroutine(qualityApplyRoutine);
        bool isPaused = false; try { isPaused = PauseMenu.Paused; } catch { isPaused = false; }
        if (isPaused)
        {
            pendingQualityLevel = q;
            if (qualityApplyRoutine == null)
                qualityApplyRoutine = StartCoroutine(ApplyQualityWhenUnpaused());
        }
        else
        {
            qualityApplyRoutine = StartCoroutine(ApplyQualityDeferred(q));
        }

        // 5) Clean TMP artifacts & ensure menu stays interactive
        yield return StartCoroutine(BlockerKillerBurst());
        ForceRestoreSettingsMenuScreen();

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    private int ClampQuality(int level)
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        return Mathf.Clamp(level, 0, max);
    }
}
