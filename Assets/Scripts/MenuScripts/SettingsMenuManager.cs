using UnityEngine;
using System.Collections;
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

    [Header("Audio")]
    public AudioMixer MainMixer;

    [Header("Brightness")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Image uiOverlay;

    [Header("Controls")]
    [SerializeField] private Slider sensitivitySlider;

    private Volume globalVolume;
    private ColorAdjustments colorAdjustments;

    private const string BrightnessKey = "BrightnessValue";
    private const string SensitivityKey = "MouseSensitivity";

    // === QUALITY APPLY (deferred) ===
    [SerializeField] private float qualityApplyDelay = 0.1f; // unscaled seconds
    private Coroutine qualityApplyRoutine;
    private int? pendingQualityLevel; // queued while paused

    // === UI restore target (explicit) ===
    [SerializeField] private string settingsMenuRootName = "SettingsMenuScreen";

    void Start()
    {
        InitializeSettings();
        InitializeBrightness();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeBrightness();
        ApplySavedSensitivity(); // reapply on scene load if new Player spawns
    }

    // === INITIALIZATION ===
    private void InitializeSettings()
    {
        // === LOAD SETTINGS/PLAYER PREFS ===
        graphicsDropdown.value = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(graphicsDropdown.value);

        float masterValue = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float musicValue = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfxValue = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        masterVol.value = masterValue;
        musicVol.value = musicValue;
        sfxVol.value = sfxValue;

        SetMixerVolume("MasterVolume", masterValue);
        SetMixerVolume("MusicVolume", musicValue);
        SetMixerVolume("SFXVolume", sfxValue);

        bool vibrateValue = PlayerPrefs.GetInt("Vibrate", 1) == 1;
        vibrateToggle.isOn = vibrateValue;
        isVibrate = vibrateValue;

        // === SENSITIVITY ===
        float savedSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
        if (sensitivitySlider == null)
            sensitivitySlider = GameObject.Find("SensitivitySlider")?.GetComponent<Slider>();

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 2.0f;
            sensitivitySlider.value = savedSensitivity;
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(delegate { ChangeSensitivity(); });
        }

        // === ADD EVENT LISTENERS ===
        graphicsDropdown.onValueChanged.AddListener(delegate { ChangeGraphicsQuality(); });
        masterVol.onValueChanged.AddListener(delegate { ChangeMasterVolume(); });
        musicVol.onValueChanged.AddListener(delegate { ChangeMusicVolume(); });
        sfxVol.onValueChanged.AddListener(delegate { ChangeSFXVolume(); });
        vibrateToggle.onValueChanged.AddListener(delegate { ChangeVibrate(); });

        // Apply saved sensitivity to player 
        ApplySavedSensitivity();
    }

    private void InitializeBrightness()
    {
        // 1. Find Global Volume in the active scene
        globalVolume = Object.FindFirstObjectByType<Volume>();
        if (globalVolume == null)
        {
            Debug.LogWarning("No Global Volume found in scene!");
            return;
        }

        if (!globalVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogWarning("No Color Adjustments override found in Global Volume!");
            return;
        }

        // 2. Auto-find UI elements 
        if (brightnessSlider == null)
            brightnessSlider = GameObject.Find("BrightnessSlider")?.GetComponent<Slider>();

        if (uiOverlay == null)
            uiOverlay = GameObject.Find("UIBrightnessOverlay")?.GetComponent<Image>();

        // 3. Load and apply saved brightness
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 0f);
        ApplyBrightness(savedBrightness);

        // 4. Connect slider
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -2f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
    }

    // === SETTINGS CHANGES ===
    public void ChangeGraphicsQuality()
    {
        // Close dropdown popup immediately so its blocker can’t linger.
        if (graphicsDropdown) graphicsDropdown.Hide();
        // Start a short “blocker killer” now, in case the popup just spawned.
        StartCoroutine(BlockerKillerBurst());

        int lvl = graphicsDropdown.value;

        // Persist choice immediately (so it sticks across sessions)
        PlayerPrefs.SetInt("GraphicsQuality", lvl);

        // Cancel any prior apply coroutine
        if (qualityApplyRoutine != null) StopCoroutine(qualityApplyRoutine);

        // If paused, queue for later; otherwise apply now (smoothly)
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
        SetMixerVolume("MasterVolume", masterVol.value);
        PlayerPrefs.SetFloat("MasterVolume", masterVol.value);
    }

    public void ChangeMusicVolume()
    {
        SetMixerVolume("MusicVolume", musicVol.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVol.value);
    }

    public void ChangeSFXVolume()
    {
        SetMixerVolume("SFXVolume", sfxVol.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVol.value);
    }

    public void ChangeVibrate()
    {
        isVibrate = vibrateToggle.isOn;
        PlayerPrefs.SetInt("Vibrate", isVibrate ? 1 : 0);
    }

    // === SENSITIVITY ===
    public void ChangeSensitivity()
    {
        if (sensitivitySlider == null) return;

        float value = sensitivitySlider.value;
        PlayerPrefs.SetFloat(SensitivityKey, value);
        PlayerPrefs.Save();

        ApplySensitivityToPlayer(value);
    }

    private void ApplySavedSensitivity()
    {
        float value = PlayerPrefs.GetFloat(SensitivityKey, 1f);
        ApplySensitivityToPlayer(value);
    }

    private void ApplySensitivityToPlayer(float value)
    {
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetSensitivity(value);
        }
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
    private void SetMixerVolume(string parameter, float value)
    {
        MainMixer.SetFloat(parameter, value);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // === QUALITY: Apply later when unpaused ===
    private IEnumerator ApplyQualityWhenUnpaused()
    {
        // Wait until the game is unpaused
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

    // === QUALITY APPLY (deferred; smooth; with blocker cleanup) ===
    private IEnumerator ApplyQualityDeferred(int level)
    {
        // Light step — no heavy rebuild yet
        QualitySettings.SetQualityLevel(level, applyExpensiveChanges: false);
        yield return null;

        // Brief unscaled delay so heavy step happens off the UI callback
        float t = 0f;
        while (t < qualityApplyDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Heavy step (recreates render targets/post stack)
        QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
        yield return null;

        // Rehook brightness
        InitializeBrightness();

        // Run blocker killer for a short burst after heavy apply too
        yield return StartCoroutine(BlockerKillerBurst());

        // Refresh dropdown label
        if (graphicsDropdown) graphicsDropdown.RefreshShownValue();

        qualityApplyRoutine = null;
    }

    // === Kill any dropdown popups/blockers for ~1 second and ensure SettingsMenuScreen is clickable ===
    private IEnumerator BlockerKillerBurst()
    {
        float duration = 1.0f; // unscaled seconds
        float elapsed = 0f;

        while (elapsed < duration)
        {
            RemoveAllKnownDropdownArtifacts();
            ForceRestoreSettingsMenuScreen();

            // keep cursor/UI state sane while paused
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

            yield return null; // next frame (unscaled)
            elapsed += Time.unscaledDeltaTime;
        }
    }

    // --- Removes all known dropdown artifacts that can eat raycasts ---
    private void RemoveAllKnownDropdownArtifacts()
    {
        try
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                var go = all[i];
                if (!go) continue;

                // Names used by TMP and legacy uGUI dropdowns for popups and full-screen blockers
                if (go.name == "TMP Dropdown Blocker" ||
                    go.name == "TMP Dropdown List" ||
                    go.name == "Blocker" ||
                    go.name == "Dropdown List")
                {
                    // Disable if in DontDestroyOnLoad, else destroy
                    if (string.IsNullOrEmpty(go.scene.name))
                        go.SetActive(false);
                    else
                        Object.Destroy(go);
                }
            }
        }
        catch { /* best-effort cleanup */ }
    }

    // --- Forces your SettingsMenuScreen back to a clickable state ---
    private void ForceRestoreSettingsMenuScreen()
    {
        GameObject settingsRoot = GameObject.Find(settingsMenuRootName);
        if (settingsRoot == null && graphicsDropdown != null)
            settingsRoot = graphicsDropdown.GetComponentInParent<Canvas>(true)?.gameObject;

        if (settingsRoot == null) return;

        // Make sure the root is active
        if (!settingsRoot.activeSelf) settingsRoot.SetActive(true);

        // Ensure CanvasGroup & Raycaster allow interaction
        var cg = settingsRoot.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        var ray = settingsRoot.GetComponent<GraphicRaycaster>();
        if (ray != null) ray.enabled = true;

        var canvas = settingsRoot.GetComponentInParent<Canvas>(true);
        if (canvas)
        {
            canvas.enabled = true;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 1500) canvas.sortingOrder = 1500; // above other UI
        }

        // As a safety net: disable any giant Image that might be catching rays
        var images = settingsRoot.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (!img) continue;
            if (img.raycastTarget)
            {
                var rect = img.rectTransform != null ? img.rectTransform.rect : new Rect(0, 0, 0, 0);
                if (rect.width > 1000f && rect.height > 600f) // full-screen-ish overlay
                    img.raycastTarget = false;
            }
        }
    }
}
