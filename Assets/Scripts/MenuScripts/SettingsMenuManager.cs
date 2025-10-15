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

    [Header("Controls")]
    [SerializeField] private Slider sensitivitySlider;

    private Volume globalVolume;
    private ColorAdjustments colorAdjustments;

    private const string BrightnessKey = "BrightnessValue";
    private const string SensitivityKey = "MouseSensitivity";

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
}
