using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;


using URPVolume = UnityEngine.Rendering.Volume;

public class GameBrightnessController : MonoBehaviour
{
    [Header("Scene References (Optional)")]
    [SerializeField] private URPVolume globalVolume;   
    [SerializeField] private Slider brightnessSlider;  
    [SerializeField] private Image uiOverlay;          

    private ColorAdjustments colorAdjustments;
    private const string BrightnessKey = "BrightnessValue";

    void Start()
    {
        // 1. --- Global Volume ---
        if (globalVolume == null)
            globalVolume = FindFirstObjectByType<URPVolume>();

        if (globalVolume == null)
        {
            Debug.LogError("No Global Volume found in scene!");
            return;
        }

        if (!globalVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("No Color Adjustments override found in Global Volume!");
            return;
        }

        // 2. --- Slider ---
        if (brightnessSlider == null)
            brightnessSlider = GameObject.Find("BrightnessSlider")?.GetComponent<Slider>();

        if (brightnessSlider == null)
            brightnessSlider = FindFirstObjectByType<Slider>();

        // 3. --- Load saved brightness ---
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 0f);
        ApplyGameBrightness(savedBrightness);

        // 4. --- Hook up slider ---
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -2f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetGameBrightness);
        }
        else
        {
            Debug.LogWarning("No Slider found in the scene. Brightness can only be changed via script.");
        }
    }

    public void SetGameBrightness(float value)
    {
        ApplyGameBrightness(value);
        PlayerPrefs.SetFloat(BrightnessKey, value);
        PlayerPrefs.Save();
    }

    public void ResetGameBrightness()
    {
        SetGameBrightness(0f);
        if (brightnessSlider != null) brightnessSlider.value = 0f;
    }

    private void ApplyGameBrightness(float value)
    {
        // 5. --- World brightness ---
        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = value;

        // 6. --- UI brightness ---
        if (uiOverlay != null)
        {
            float alpha = Mathf.InverseLerp(-2f, 2f, -value) * 0.6f;
            uiOverlay.color = new Color(0f, 0f, 0f, alpha);
        }

        Debug.Log("Game brightness applied: " + value);
    }
}


