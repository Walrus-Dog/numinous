using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BrightnessController : MonoBehaviour
{
    private Volume globalVolume;
    private ColorAdjustments colorAdjustments;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Image uiOverlay;

    private const string BrightnessKey = "BrightnessValue";
    private static BrightnessController instance;

    void Awake()
    {
        // Make persistent across scenes
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        InitializeBrightness();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeBrightness();
    }

    private void InitializeBrightness()
    {
        // 1. Find Global Volume
        globalVolume = Object.FindFirstObjectByType<Volume>();
        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            // Apply saved brightness immediately
            float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 0f);
            ApplyBrightness(savedBrightness);
        }
        else
        {
            Debug.LogWarning("No Global Volume or Color Adjustments override found in this scene.");
        }

        // 2. (Optional) Automatically reconnect slider/overlay if scene reloads
        if (brightnessSlider == null)
            brightnessSlider = GameObject.Find("BrightnessSlider")?.GetComponent<Slider>();
        if (uiOverlay == null)
            uiOverlay = GameObject.Find("UIBrightnessOverlay")?.GetComponent<Image>();

        // 3. Restore slider listener
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -2f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = PlayerPrefs.GetFloat(BrightnessKey, 0f);
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
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

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
