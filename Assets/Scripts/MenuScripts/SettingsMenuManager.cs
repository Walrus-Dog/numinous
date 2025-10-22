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

    [SerializeField] private float qualityApplyDelay = 0.1f;
    private Coroutine qualityApplyRoutine;
    private int? pendingQualityLevel;

    [SerializeField] private string settingsMenuRootName = "SettingsMenuScreen";

    private Camera lastActiveCamera; // Track camera changes
    private AudioListener listener;  //  Keep a reference to the listener

    void Start()
    {
        InitializeSettings();
        InitializeBrightness();
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Ensure Audio Listener is active on startup
        EnsureActiveAudioListener();

        // Start watching for camera switches
        StartCoroutine(TrackActiveCamera());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeBrightness();
        ApplySavedSensitivity();

        // Ensure Audio Listener is active after each scene load
        EnsureActiveAudioListener();
    }

    private void InitializeSettings()
    {
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

        graphicsDropdown.onValueChanged.AddListener(delegate { ChangeGraphicsQuality(); });
        masterVol.onValueChanged.AddListener(delegate { ChangeMasterVolume(); });
        musicVol.onValueChanged.AddListener(delegate { ChangeMusicVolume(); });
        sfxVol.onValueChanged.AddListener(delegate { ChangeSFXVolume(); });
        vibrateToggle.onValueChanged.AddListener(delegate { ChangeVibrate(); });

        ApplySavedSensitivity();
    }

    private void InitializeBrightness()
    {
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

        if (brightnessSlider == null)
            brightnessSlider = GameObject.Find("BrightnessSlider")?.GetComponent<Slider>();

        if (uiOverlay == null)
            uiOverlay = GameObject.Find("UIBrightnessOverlay")?.GetComponent<Image>();

        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 0f);
        ApplyBrightness(savedBrightness);

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -2f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
    }

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

    private void SetMixerVolume(string parameter, float value)
    {
        MainMixer.SetFloat(parameter, value);
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

    // === AUDIO LISTENER CHECK ===
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

    // === CAMERA TRACKER: moves listener when the active camera changes ===
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
            yield return new WaitForSeconds(0.25f); // check 4x per second
        }
    }
}
