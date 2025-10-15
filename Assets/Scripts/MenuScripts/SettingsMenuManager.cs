using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

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

    private Volume globalVolume;
    private ColorAdjustments colorAdjustments;
    private const string BrightnessKey = "BrightnessValue";

    void Start()
    {
        InitializeSettings();
        InitializeBrightness();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeBrightness();
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

        // === ADD EVENT LISTENERS ===
        graphicsDropdown.onValueChanged.AddListener(delegate { ChangeGraphicsQuality(); });
        masterVol.onValueChanged.AddListener(delegate { ChangeMasterVolume(); });
        musicVol.onValueChanged.AddListener(delegate { ChangeMusicVolume(); });
        sfxVol.onValueChanged.AddListener(delegate { ChangeSFXVolume(); });
        vibrateToggle.onValueChanged.AddListener(delegate { ChangeVibrate(); });
    }

    // === BRIGHTNESS INITIALIZATION ===
    private void InitializeBrightness()
    {
        // 1. Load saved brightness before applying visuals
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 0f);

        // 2. Find Global Volume and Color Adjustments
        globalVolume = Object.FindFirstObjectByType<Volume>();
        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            // Apply immediately before the first frame renders
            colorAdjustments.postExposure.value = savedBrightness;
        }

        // 3. Auto-find UI elements if not assigned
        if (brightnessSlider == null)
            brightnessSlider = GameObject.Find("BrightnessSlider")?.GetComponent<Slider>();

        if (uiOverlay == null)
            uiOverlay = GameObject.Find("UIBrightnessOverlay")?.GetComponent<Image>();

        // 4. Configure slider
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -2f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        // 5. Apply overlay after UI initializes (1 frame delay)
        StartCoroutine(ApplyBrightnessNextFrame(savedBrightness));
    }

    private IEnumerator ApplyBrightnessNextFrame(float value)
    {
        yield return null; // wait one frame for UI to be ready
        ApplyBrightness(value);
    }

    // === SETTINGS CHANGES ===
    public void ChangeGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(graphicsDropdown.value);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsDropdown.value);
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

    // === BRIGHTNESS CONTROL ===
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
            // Convert brightness (-2..2) to overlay alpha (0..0.6)
            float alpha = Mathf.Clamp01(Mathf.InverseLerp(-2f, 2f, -value) * 0.6f);
            uiOverlay.color = new Color(0f, 0f, 0f, alpha);
        }
    }

    // === HELPER ===
    private void SetMixerVolume(string parameter, float value)
    {
        MainMixer.SetFloat(parameter, value);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
